using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Fusion;
using NUnit.Framework;
using TheSancturary.Inventory;
using TheSancturary.Monsters;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Assert = NUnit.Framework.Assert;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class ItemCombatRuntimeTests
    {
        private FusionNetworkPlayer _player;
        private SandboxSession _sandbox;
        private NetworkItemUseController Use => _player.ItemUseController;
        private NetworkPlayerInventory Inventory => _player.Inventory;

        [UnitySetUp]
        public IEnumerator StartSandbox()
        {
            yield return FusionPlayModeTestSession.ResetExistingSession();
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SandboxPrototype.unity");
            yield return Until(() => (_player = Object.FindFirstObjectByType<FusionNetworkPlayer>()) != null &&
                _player.Object != null && _player.Object.IsValid, 40, "Sandbox player did not spawn.");
            _sandbox = SandboxSession.For(_player);
            Assert.That(_sandbox, Is.Not.Null);
            Assert.That(_player.HasStateAuthority, Is.True);
        }

        [UnityTearDown]
        public IEnumerator StopSandbox()
        {
            yield return FusionPlayModeTestSession.ResetExistingSession();
        }

        [UnityTest]
        public IEnumerator SharedReceiverHasExactHitCountsAndOneDeath()
        {
            InventoryItemDefinition revolver = Definition("old_revolver");
            foreach (bool henry in new[] { false, true })
            {
                MonsterCombatState monster = SpawnMonster(henry);
                int hits = henry ? 3 : 2 * revolver.AmmunitionCapacity;
                int damage = revolver.CombatSettings.normalHitDamage;
                Assert.That(monster.Health, Is.EqualTo(hits * damage));
                for (int hit = 1; hit <= hits; hit++)
                {
                    Assert.That(Contact(monster, revolver), Is.True);
                    Assert.That(monster.Health, Is.EqualTo((hits - hit) * damage));
                    Assert.That((bool)monster.IsDead, Is.EqualTo(hit == hits));
                }
                Assert.That(Contact(monster, revolver), Is.False, "Dead monster accepted another hit.");
                yield return Until(() => monster == null || monster.Object == null || !monster.Object.IsValid,
                    3, "Dead monster was not despawned.");

                monster = SpawnMonster(henry);
                int health = monster.Health;
                Assert.That(Contact(monster, Definition("fire_axe")), Is.True);
                Assert.That(monster.Health, Is.EqualTo(health - damage));
                Assert.That(Contact(monster, Definition("crowbar")), Is.True);
                Assert.That(monster.Health, Is.EqualTo(health - damage * 2));
                Assert.That(Contact(monster, Definition("sawed_off_shotgun")), Is.True);
                Assert.That((bool)monster.IsDead, Is.True);
                Assert.That(monster.Health, Is.Zero);
                yield return Until(() => monster == null || monster.Object == null || !monster.Object.IsValid, 3,
                    "Shotgun death did not clean up.");
            }
        }

        [UnityTest]
        public IEnumerator BothMonstersSleepSixtySecondsRefreshWakeAndCannotWakeAfterDeath()
        {
            var monsters = new[] { SpawnMonster(false), SpawnMonster(true) };
            InventoryItemDefinition dart = Definition("tranq_gun");
            Assert.That(dart.CombatSettings.sleepSeconds, Is.EqualTo(60));
            Vector3[] positions = monsters.Select(m => m.transform.position).ToArray();
            foreach (var monster in monsters)
            {
                int health = monster.Health;
                Assert.That(Contact(monster, dart), Is.True);
                Assert.That(monster.Health, Is.EqualTo(health), "Dart dealt bullet damage.");
                Assert.That(monster.IsSleeping, Is.True);
                Assert.That(monster.SleepRemainingSeconds, Is.InRange(59.9f, 60.1f));
            }
            yield return new WaitForSeconds(5);
            foreach (var monster in monsters)
            {
                Assert.That(Contact(monster, dart), Is.True);
                Assert.That(monster.SleepRemainingSeconds, Is.InRange(59.9f, 60.1f), "Refresh stacked or shortened sleep.");
                Assert.That(Contact(monster, Definition("old_revolver")), Is.True);
                Assert.That(monster.IsSleeping, Is.True, "Ordinary damage woke the monster.");
            }
            float playerHealth = _player.Health;
            // Fusion's authoritative clock continues through long Editor frames;
            // Unity's scaled WaitForSeconds can lag behind it during asset imports.
            int wakeTick = Use.Runner.Tick.Raw + Mathf.RoundToInt(60f / Use.Runner.DeltaTime);
            float timeout = Time.realtimeSinceStartup + 75f;
            while (Use.Runner.Tick.Raw < wakeTick - 1 && Time.realtimeSinceStartup < timeout)
            {
                for (int i = 0; i < monsters.Length; i++)
                {
                    Assert.That(monsters[i].IsSleeping, Is.True, "Monster woke before 60 seconds after refresh.");
                    Assert.That(Vector3.Distance(positions[i], monsters[i].transform.position), Is.LessThan(.01f), "Sleeping AI moved.");
                }
                Assert.That(_player.Health, Is.EqualTo(playerHealth));
                yield return null;
            }
            Assert.That(Use.Runner.Tick.Raw, Is.GreaterThanOrEqualTo(wakeTick - 1), "Authoritative clock stopped advancing.");
            yield return Until(() => monsters.All(m => !m.IsSleeping), 3, "Monster did not wake on the authoritative clock.");
            foreach (var monster in monsters)
            {
                Assert.That(Contact(monster, dart), Is.True);
                Assert.That(Contact(monster, Definition("sawed_off_shotgun")), Is.True);
                Assert.That((bool)monster.IsDead, Is.True);
                Assert.That(monster.SleepTimer.IsRunning, Is.False);
                Assert.That(monster.IsSleeping, Is.False);
                Assert.That(Contact(monster, dart), Is.False, "Dart reactivated a dead monster.");
            }
            yield return Until(() => monsters.All(m => m == null || m.Object == null || !m.Object.IsValid), 3,
                "Dead sleeping monster remained networked.");
        }

        [UnityTest]
        public IEnumerator SwitchingInterruptsReloadAndTreatmentWithoutFreeOutcomes()
        {
            ushort gun = Give("old_revolver"), ammo = Give("revolver_ammo"), light = Give("flashlight");
            Equip(light);
            yield return new WaitForSeconds(.3f);
            Assert.That(Use.TryUseEquippedAuthoritative(), Is.True);
            Assert.That((bool)Inventory.FlashlightEnabled, Is.True);
            Equip(gun);
            Assert.That((bool)Inventory.FlashlightEnabled, Is.False);
            yield return new WaitForSeconds(.4f);
            Assert.That(Use.TryBeginReloadAuthoritative(ammo, gun), Is.True);
            Assert.That((bool)Use.IsReloading, Is.True);
            Assert.That(Use.TryUseEquippedAuthoritative(), Is.False);
            yield return new WaitForSeconds(.5f);
            Equip(light);
            Assert.That(Use.HasActiveUse, Is.False);
            Assert.That(Inventory.ContainsInstanceAuthoritative(ammo), Is.True);
            Assert.That(Inventory.TryGetEntry(gun, out var entry), Is.True);
            Assert.That(entry.LoadedAmmunition, Is.Zero);
            Equip(gun);
            yield return new WaitForSeconds(.3f);
            Assert.That(Use.TryBeginReloadAuthoritative(ammo, gun), Is.True);
            yield return Until(() => !Use.HasActiveUse, 8, "Reload never completed.");
            Assert.That(Inventory.ContainsInstanceAuthoritative(ammo), Is.False);
            Assert.That(Inventory.TryGetEntry(gun, out entry), Is.True);
            Assert.That(entry.LoadedAmmunition, Is.EqualTo(Definition("old_revolver").AmmunitionCapacity));
            Assert.That(Use.TryBeginReloadAuthoritative(ammo, gun), Is.False);

            ushort medkit = Give("med_kit");
            _player.TakeDamage(30);
            yield return Until(() => _player.Health < _player.MaximumHealth, 2, "Setup damage was not applied.");
            float injuredHealth = _player.Health;
            Equip(medkit);
            yield return new WaitForSeconds(.3f);
            Assert.That(Use.TryUseEquippedAuthoritative(), Is.True);
            Equip(gun);
            yield return new WaitForSeconds(Definition("med_kit").UseDuration + .1f);
            Assert.That(_player.Health, Is.EqualTo(injuredHealth));
            Assert.That(Inventory.ContainsInstanceAuthoritative(medkit), Is.True);
            Equip(medkit);
            yield return new WaitForSeconds(.3f);
            Assert.That(Use.TryUseEquippedAuthoritative(), Is.True);
            yield return Until(() => !Use.HasActiveUse, 5, "Treatment did not finish.");
            Assert.That(_player.Health, Is.EqualTo(_player.MaximumHealth));
            Assert.That(Inventory.ContainsInstanceAuthoritative(medkit), Is.False);
            Assert.That(Inventory.EquippedInstanceId, Is.Zero);
        }

        private InventoryItemDefinition Definition(string id)
        {
            Assert.That(Inventory.TryResolveDefinition(id, out var definition), Is.True);
            return definition;
        }
        private ushort Give(string id)
        {
            Assert.That(Inventory.TryAddItemAuthoritative(id, out InventoryRequestRejection rejection), Is.True, rejection.ToString());
            return Inventory.Entries.Last(e => e.IsOccupied && e.ItemId.ToString() == id).InstanceId;
        }
        private void Equip(ushort id) => Inventory.ProcessInputCommandAuthoritative(InventoryInputCommandType.Equip, id, 0, 0, false);
        private MonsterCombatState SpawnMonster(bool henry)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var prefabs = (NetworkObject[])typeof(SandboxSession).GetField("monsterPrefabs", flags).GetValue(_sandbox);
            var points = (Transform[])typeof(SandboxSession).GetField("monsterSpawns", flags).GetValue(_sandbox);
            NetworkObject prefab = prefabs.First(p => p.name.Contains(henry ? "Henry" : "Geo"));
            return _player.Runner.Spawn(prefab, points[henry && points.Length > 1 ? 1 : 0].position, Quaternion.identity).GetComponent<MonsterCombatState>();
        }
        private bool Contact(MonsterCombatState monster, InventoryItemDefinition definition) =>
            monster.TryReceiveHitAuthoritative(Use, definition, monster.transform.position + Vector3.up, Vector3.back);
        private static IEnumerator Until(Func<bool> condition, float seconds, string message)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(condition(), Is.True, message);
        }
    }
}
