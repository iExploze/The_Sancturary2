using NUnit.Framework;
using TheSancturary.Inventory;
using UnityEditor;
using UnityEngine;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class ItemCombatTests
    {
        [TestCase("RevolverAmmo")]
        [TestCase("ShotgunShell")]
        [TestCase("TranqDart")]
        public void AmmunitionOffersForgivingPickupAndConsistentDropBounds(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Inventory/Prefabs/World" + name + ".prefab");
            var box = prefab.GetComponent<BoxCollider>();
            Assert.That(box, Is.Not.Null);
            Assert.That(box.isTrigger, Is.False, "Both local targeting and authority raycast solid colliders.");
            Assert.That(box.size.x, Is.GreaterThanOrEqualTo(.18f));
            Assert.That(box.size.y, Is.GreaterThanOrEqualTo(.10f));
            Assert.That(box.size.z, Is.GreaterThanOrEqualTo(.18f));
            var physics = prefab.GetComponent<WorldItemPhysics>();
            Assert.That(physics.LocalBounds.size, Is.EqualTo(box.size));
            Assert.That(physics.PhysicalColliders, Does.Contain(box));
            var item = new SerializedObject(prefab.GetComponent<WorldInventoryItem>());
            Assert.That(item.FindProperty("pickupColliders").GetArrayElementAtIndex(0).objectReferenceValue,
                Is.EqualTo(box));
        }

        [Test]
        public void AuthoredBalanceUsesActualCylinderAndThreeHenryHits()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ItemCombatSettings>(
                "Assets/Inventory/Definitions/ItemCombatSettings.asset");
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.revolver.AmmunitionCapacity, Is.EqualTo(6));
            Assert.That(settings.GeoHealth / settings.normalHitDamage, Is.EqualTo(12));
            Assert.That(settings.HenryHealth / settings.normalHitDamage, Is.EqualTo(3));
            foreach (int hits in new[] { 12, 3 })
            {
                int health = ItemCombatRules.FullHealth(settings.normalHitDamage, hits);
                for (int i = 1; i < hits; i++)
                {
                    health = ItemCombatRules.DamageResult(health, settings.normalHitDamage, false);
                    Assert.That(health, Is.GreaterThan(0), "Premature lethal normal hit " + i);
                }
                Assert.That(ItemCombatRules.DamageResult(health, settings.normalHitDamage, false), Is.Zero);
            }
            Assert.That(ItemCombatRules.DamageResult(settings.GeoHealth, 1, true), Is.Zero);
            Assert.That(ItemCombatRules.DamageResult(0, settings.normalHitDamage, false), Is.Zero);
        }

        [Test]
        public void ACommittedSwingAcceptsAtMostOneContactInItsStrikeWindow()
        {
            bool hit = false;
            int contacts = 0;
            for (int frame = 0; frame <= 120; frame++)
            {
                float progress = frame / 120f;
                if (!ItemCombatRules.CanStrike(progress, .42f, .62f, hit)) continue;
                Assert.That(progress, Is.InRange(.42f, .62f));
                hit = true;
                contacts++;
            }
            Assert.That(contacts, Is.EqualTo(1));
            Assert.That(ItemCombatRules.CanStrike(.3f, .42f, .62f, false), Is.False);
            Assert.That(ItemCombatRules.CanStrike(.8f, .42f, .62f, false), Is.False);
        }

        [TestCase(false, false, true, false, true)]
        [TestCase(true, true, true, false, true)]
        [TestCase(true, false, false, false, true)]
        [TestCase(true, false, true, true, true)]
        [TestCase(true, false, true, false, false)]
        public void RestrictedEquipmentCannotAim(bool alive, bool restricted, bool firearm, bool busy, bool requested)
        {
            Assert.That(ItemCombatRules.CanAim(alive, restricted, firearm, busy, requested), Is.False);
        }

        [Test]
        public void AdsClampsEvenBoostedMovementAndReleaseRestoresItsCurrentLimit()
        {
            Assert.That(ItemCombatRules.CanAim(true, false, true, false, true), Is.True);
            foreach (float speed in new[] { .8f, 3f, 7f, 14f })
            {
                Assert.That(ItemCombatRules.MovementLimit(speed, 1.1f, true), Is.LessThanOrEqualTo(1.1f));
                Assert.That(ItemCombatRules.MovementLimit(speed, 1.1f, true), Is.LessThanOrEqualTo(speed));
                Assert.That(ItemCombatRules.MovementLimit(speed, 1.1f, false), Is.EqualTo(speed));
            }
        }
    }
}
