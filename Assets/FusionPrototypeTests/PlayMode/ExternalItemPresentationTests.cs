using System.Collections;
using System.Linq;
using Fusion.Photon.Realtime;
using NUnit.Framework;
using TheSancturary.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class ExternalItemPresentationTests
    {
        private FusionNetworkPlayer _player;
        private string _appId;
        private NetworkPlayerInventory Inventory => _player.Inventory;
        private PlayerEquipment Equipment => _player.GetComponent<PlayerEquipment>();
        private PlayerEquipmentRigController Rig => _player.GetComponentInChildren<PlayerEquipmentRigController>();

        [UnitySetUp]
        public IEnumerator StartSandbox()
        {
            _appId = PhotonAppSettings.Global.AppSettings.AppIdFusion;
            PhotonAppSettings.Global.AppSettings.AppIdFusion = string.Empty;
            yield return FusionPlayModeTestSession.ResetExistingSession();
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SandboxPrototype.unity");
            float timeout = Time.realtimeSinceStartup + 40f;
            while ((_player = Object.FindFirstObjectByType<FusionNetworkPlayer>()) == null ||
                   _player.Object == null || !_player.Object.IsValid)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Sandbox startup timed out.");
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator StopSandbox()
        {
            yield return FusionPlayModeTestSession.ResetExistingSession();
            PhotonAppSettings.Global.AppSettings.AppIdFusion = _appId;
        }

        private ushort GiveAndEquip(InventoryItemDefinition definition)
        {
            foreach (var entry in Inventory.Entries.ToArray())
                if (entry.IsOccupied) Inventory.TryConsumeInstanceAuthoritative(entry.InstanceId);
            Assert.That(Inventory.TryAddItemAuthoritative(definition.ItemId, out var rejection), Is.True, rejection.ToString());
            ushort id = Inventory.Entries.First(e => e.IsOccupied).InstanceId;
            Inventory.ProcessInputCommandAuthoritative(InventoryInputCommandType.Equip, id, 0, 0, false);
            return id;
        }

        [UnityTest]
        public IEnumerator EveryEquipableItemReconstructsEditableGripsAndReleasesUnusedArms()
        {
            var definitions = Inventory.Catalog.Definitions.Where(d => d.CanEquip).ToArray();
            Assert.That(definitions, Is.Not.Empty);
            foreach (var definition in definitions)
            {
                ushort id = GiveAndEquip(definition);
                yield return new WaitForSeconds(.4f);
                yield return new WaitForEndOfFrame();
                var remote = Equipment.ThirdPersonHeldVisual;
                Assert.That(remote, Is.Not.Null, definition.ItemId);
                Assert.That(remote.Definition, Is.SameAs(definition));
                Assert.That(remote.IsOwnerPresentation, Is.False);
                Assert.That(Equipment.FirstPersonHeldVisual, Is.Not.SameAs(remote));
                Assert.That(Rig.RightGripSource, Is.SameAs(remote.RightHandGrip));
                Assert.That(Rig.HoldStyle, Is.EqualTo(definition.HoldStyle));
                Assert.That(Rig.HoldPose, Is.EqualTo(definition.HoldPose));
                Assert.That(Rig.CurrentRightWeight, Is.EqualTo(1f).Within(.01f));
                Assert.That(Rig.CurrentLeftWeight, Is.EqualTo(
                    definition.HoldStyle == InventoryHoldStyle.TwoHanded ? 1f : 0f).Within(.01f));
                var animator = Rig.GetComponent<Animator>();
                Assert.That(Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position,
                    remote.RightHandGrip.position), Is.LessThan(.24f), definition.ItemId + " right hand cannot reach its grip.");
                if (definition.HoldStyle == InventoryHoldStyle.TwoHanded)
                    Assert.That(Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position,
                        remote.LeftHandGrip.position), Is.LessThan(.24f), definition.ItemId + " left hand cannot reach its grip.");
                Equipment.SetOwnerCameraThirdPersonSuppressed(true);
                Assert.That(Equipment.ThirdPersonRenderers.All(r => r.forceRenderingOff), Is.True);
                Equipment.SetOwnerCameraThirdPersonSuppressed(false);
                Inventory.ProcessInputCommandAuthoritative(InventoryInputCommandType.Equip, id, 0, 0, false);
                yield return null;
                Assert.That(Equipment.ThirdPersonHeldVisual, Is.Null, "Unequip left a visual.");
                Assert.That(Rig.RightGripSource, Is.Null);
                Assert.That(Rig.LeftGripSource, Is.Null);
                Assert.That(Rig.CurrentRightWeight + Rig.CurrentLeftWeight, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator ExternalUseMotionsDoNotMutateGameplayAndReloadStaysExternal()
        {
            foreach (var definition in Inventory.Catalog.Definitions.Where(d => d.CanEquip))
            {
                ushort id = GiveAndEquip(definition);
                yield return new WaitForSeconds(.3f);
                var remote = Equipment.ThirdPersonHeldVisual;
                var root = remote.transform.Find("VisualRoot");
                Quaternion rest = root.localRotation;
                float health = _player.Health;
                Inventory.TryGetEntry(id, out var before);
                remote.PlayUse(false);
                float duration = Mathf.Max(.2f, definition.UseDuration);
                yield return new WaitForSeconds(Mathf.Min(duration * .3f, .15f));
                Assert.That(Quaternion.Angle(rest, root.localRotation), Is.GreaterThan(.1f), definition.ItemId);
                Assert.That(_player.Health, Is.EqualTo(health));
                Inventory.TryGetEntry(id, out var after);
                Assert.That(after.LoadedAmmunition, Is.EqualTo(before.LoadedAmmunition));
                remote.CancelUse();
                if (definition.UseKind != InventoryItemUseKind.Firearm) continue;
                // Route through the same confirmed event consumed by remote players.
                _player.ItemUseController.PresentReloadAuthoritative(id);
                yield return new WaitForSeconds(.15f);
                Assert.That(remote.IsUsePresentationActive, Is.True, definition.ItemId + " external reload missing.");
                Assert.That(Equipment.FirstPersonHeldVisual.IsUsePresentationActive, Is.False,
                    "External reload changed the owner's pre-existing presentation.");
                yield return new WaitForSeconds(1.3f);
                Assert.That(remote.IsUsePresentationActive, Is.False, "Repeated Render restarted one event.");
            }
        }

        [UnityTest]
        public IEnumerator ExternalRecoilSurvivesASlowPresentationFrame()
        {
            GiveAndEquip(Inventory.Catalog.Definitions.First(d =>
                d.CanEquip && d.UseKind == InventoryItemUseKind.Firearm));
            yield return new WaitForSeconds(.3f);
            float previousCaptureDelta = Time.captureDeltaTime;
            try
            {
                Time.captureDeltaTime = .5f;
                Equipment.ThirdPersonHeldVisual.PlayUse(false);
                yield return null;
                yield return null;
                Assert.That(Equipment.ThirdPersonHeldVisual.IsUsePresentationActive, Is.True,
                    "A slow frame skipped the entire confirmed external recoil.");
            }
            finally
            {
                Time.captureDeltaTime = previousCaptureDelta;
            }
        }

        [UnityTest]
        public IEnumerator ConsumptionDropAndDeathClearEquipmentAndRig()
        {
            var definition = Inventory.Catalog.Definitions.First(d => d.UseKind == InventoryItemUseKind.FlashlightToggle);
            ushort id = GiveAndEquip(definition);
            yield return new WaitForSeconds(.3f);
            Inventory.TryConsumeInstanceAuthoritative(id);
            yield return null;
            AssertCleared();
            id = GiveAndEquip(definition);
            yield return new WaitForSeconds(.3f);
            Inventory.ProcessInputCommandAuthoritative(InventoryInputCommandType.Drop, id, 0, 0, false);
            yield return new WaitForSeconds(.2f);
            AssertCleared();
            GiveAndEquip(definition);
            yield return new WaitForSeconds(.3f);
            _player.KillInstantlyAuthoritative();
            yield return null;
            AssertCleared();
        }

        private void AssertCleared()
        {
            Assert.That(Equipment.FirstPersonHeldVisual, Is.Null);
            Assert.That(Equipment.ThirdPersonHeldVisual, Is.Null);
            Assert.That(Rig.RightGripSource, Is.Null);
            Assert.That(Rig.LeftGripSource, Is.Null);
            Assert.That(Rig.CurrentRightWeight + Rig.CurrentLeftWeight, Is.Zero);
        }
    }
}
