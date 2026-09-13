using System.Collections;
using System.Reflection;
using Fusion;
using NUnit.Framework;
using TheSancturary.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnitAssert = NUnit.Framework.Assert;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class DirectSceneBootstrapTests
    {
        private static IEnumerator ValidateLocomotionUsesRootRelativeDirectionsAndForwardOnlySprint(
            FusionNetworkPlayer player,
            NetworkCharacterController controller,
            Keyboard keyboard,
            Animator animator)
        {
            NUnitAssert.That(controller, Is.Not.Null);
            NUnitAssert.That(animator, Is.Not.Null);
            NUnitAssert.That(animator.applyRootMotion, Is.False);
            NUnitAssert.That(animator.layerCount, Is.EqualTo(4));
            NUnitAssert.That(animator.GetLayerName(1), Is.EqualTo("Airborne"));
            NUnitAssert.That(
                animator.GetLayerName(2),
                Is.EqualTo("Item Pose - One Hand"));
            NUnitAssert.That(
                animator.GetLayerName(3),
                Is.EqualTo("Item Pose - Two Hand"));
            // The spawn faces Door_A; a sprint reaches its collider before the
            // velocity assertion. Measure locomotion on an isolated temporary floor.
            Vector3 originalPosition = player.transform.position;
            AnimatorCullingMode originalCulling = animator.cullingMode;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            GameObject movementFloor = new GameObject("Temporary locomotion test floor");
            movementFloor.transform.position = new Vector3(0f, 30f, 0f);
            movementFloor.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);
            Physics.SyncTransforms();
            controller.Teleport(new Vector3(0f, 30.6f, 0f));
            yield return WaitUntil(() => controller.Grounded, 5f, "Player did not begin grounded.");

            yield return HoldKeys(keyboard, 0.6f, Key.W);
            Vector3 localVelocity = player.transform.InverseTransformDirection(controller.Velocity);
            NUnitAssert.That(localVelocity.z, Is.GreaterThan(0.5f), "W must create positive local forward velocity.");
            NUnitAssert.That(animator.GetFloat("MoveY"), Is.GreaterThan(0.2f), "W must drive positive MoveY.");
            NUnitAssert.That((bool)player.IsSprinting, Is.False, "W without Sprint must remain walking.");
            yield return ReleaseAndSettle(keyboard, controller);

            yield return HoldKeys(keyboard, 0.6f, Key.S);
            localVelocity = player.transform.InverseTransformDirection(controller.Velocity);
            NUnitAssert.That(localVelocity.z, Is.LessThan(-0.5f), "S must create negative local forward velocity.");
            NUnitAssert.That(animator.GetFloat("MoveY"), Is.LessThan(-0.2f), "S must drive negative MoveY.");
            yield return ReleaseAndSettle(keyboard, controller);

            yield return HoldKeys(keyboard, 0.6f, Key.A);
            localVelocity = player.transform.InverseTransformDirection(controller.Velocity);
            NUnitAssert.That(localVelocity.x, Is.LessThan(-0.5f), "A must create negative local lateral velocity.");
            NUnitAssert.That(animator.GetFloat("MoveX"), Is.LessThan(-0.2f), "A must drive negative MoveX.");
            yield return ReleaseAndSettle(keyboard, controller);

            yield return HoldKeys(keyboard, 0.6f, Key.D);
            localVelocity = player.transform.InverseTransformDirection(controller.Velocity);
            NUnitAssert.That(localVelocity.x, Is.GreaterThan(0.5f), "D must create positive local lateral velocity.");
            NUnitAssert.That(animator.GetFloat("MoveX"), Is.GreaterThan(0.2f), "D must drive positive MoveX.");
            yield return ReleaseAndSettle(keyboard, controller);

            float sprintStaminaBefore = player.Stamina;
            yield return HoldKeys(keyboard, 0.8f, Key.LeftShift, Key.W);
            localVelocity = player.transform.InverseTransformDirection(controller.Velocity);
            NUnitAssert.That((bool)player.IsSprinting, Is.True, "Shift+W must activate sprinting.");
            NUnitAssert.That(localVelocity.z, Is.GreaterThan(2.5f), $"Shift+W must reach forward sprint velocity. Position={player.transform.position}, maxSpeed={controller.maxSpeed}, aiming={player.ItemUseController.IsAiming}.");
            NUnitAssert.That(Mathf.Abs(localVelocity.x), Is.LessThan(0.2f), "Forward sprint must not add lateral velocity.");
            NUnitAssert.That(animator.GetFloat("MoveY"), Is.GreaterThan(1.1f), "Forward sprint must occupy the >1 MoveY range.");
            NUnitAssert.That(player.Stamina, Is.LessThan(sprintStaminaBefore), "Valid forward sprint must drain stamina.");
            yield return ReleaseAndSettle(keyboard, controller);

            yield return AssertInvalidSprintDirection(keyboard, player, controller, animator, Key.A, "Shift+A");
            yield return AssertInvalidSprintDirection(keyboard, player, controller, animator, Key.D, "Shift+D");
            yield return AssertInvalidSprintDirection(keyboard, player, controller, animator, Key.S, "Shift+S");

            yield return HoldKeys(keyboard, 0.8f, Key.LeftShift, Key.W, Key.A);
            localVelocity = player.transform.InverseTransformDirection(controller.Velocity);
            NUnitAssert.That((bool)player.IsSprinting, Is.True, "Shift+W+A must remain a valid forward sprint.");
            NUnitAssert.That(localVelocity.z, Is.GreaterThan(2.5f));
            NUnitAssert.That(Mathf.Abs(localVelocity.x), Is.LessThan(0.2f), "Sprint must ignore diagonal lateral input.");
            yield return ReleaseAndSettle(keyboard, controller);

            int airborneLayer = animator.GetLayerIndex("Airborne");
            NUnitAssert.That(animator.GetCurrentAnimatorStateInfo(airborneLayer).IsName("Grounded Pass-Through"), Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            NUnitAssert.That(
                animator.GetCurrentAnimatorStateInfo(airborneLayer).IsName("Grounded Pass-Through"),
                Is.True,
                "Jump input alone must not pre-empt the grounded animation state.");
            yield return WaitUntil(() => !controller.Grounded, 2f, "Jump did not make the controller airborne.");
            yield return WaitUntil(
                () => (animator.GetCurrentAnimatorStateInfo(airborneLayer).IsName("Jump Start") || animator.GetCurrentAnimatorStateInfo(airborneLayer).IsName("Falling")),
                1f,
                "Animator did not enter Airborne after the controller left the ground.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return WaitUntil(() => controller.Grounded, 3f, "Player did not land after jumping.");
            yield return WaitUntil(
                () => animator.GetCurrentAnimatorStateInfo(airborneLayer).IsName("Grounded Pass-Through"),
                1f,
                "Animator did not return to locomotion after landing.");

            controller.Teleport(player.transform.position + Vector3.up * 2f);
            yield return WaitUntil(() => !controller.Grounded, 1f, "Ungrounded teleport did not begin falling.");
            yield return WaitUntil(
                () => (animator.GetCurrentAnimatorStateInfo(airborneLayer).IsName("Jump Start") || animator.GetCurrentAnimatorStateInfo(airborneLayer).IsName("Falling")),
                1f,
                "Airborne animation must activate without jump input when the controller leaves the ground.");
            yield return WaitUntil(() => controller.Grounded, 3f, "Player did not land after the unprompted fall.");
            controller.Teleport(originalPosition);
            animator.cullingMode = originalCulling;
            Object.Destroy(movementFloor);
        }

        [UnityTest]
        public IEnumerator DirectSceneSpawnsOneOwnedCanonicalPlayer()
        {
            yield return FusionPlayModeTestSession.ResetExistingSession();
            yield return SceneManager.LoadSceneAsync(FusionSessionManager.GameplayScenePath, LoadSceneMode.Single);

            FusionNetworkPlayer player = null;
            float timeout = Time.realtimeSinceStartup + 30f;
            while (player == null && Time.realtimeSinceStartup < timeout)
            {
                player = Object.FindFirstObjectByType<FusionNetworkPlayer>();
                yield return null;
            }

            NUnitAssert.That(player, Is.Not.Null, "Direct scene Play did not spawn the canonical Fusion player within 30 seconds.");
            NUnitAssert.That(player.HasInputAuthority, Is.True, "The direct-debug player must own input authority.");
            NUnitAssert.That(player.HasStateAuthority, Is.True, "Fusion Single mode should give the direct-debug player state authority.");
            NUnitAssert.That(CountEnabled<Camera>(), Is.EqualTo(1), "Exactly one Camera should be active for the local player.");
            NUnitAssert.That(CountEnabled<AudioListener>(), Is.EqualTo(1), "Exactly one AudioListener should be active for the local player.");
            NUnitAssert.That(CountEnabled<PlayerInput>(), Is.EqualTo(1), "Exactly one PlayerInput should be active for the local player.");

            NetworkRunner runner = Object.FindFirstObjectByType<NetworkRunner>();
            NUnitAssert.That(runner, Is.Not.Null);
            NUnitAssert.That(runner.IsRunning, Is.True);
            NUnitAssert.That(player.Object.InputAuthority, Is.EqualTo(runner.LocalPlayer));

            PlayerInput ownerInput = player.GetComponent<PlayerInput>();
            Animator animator = player.GetComponentInChildren<Animator>(true);
            Renderer[] characterRenderers = player.GetComponentsInChildren<Renderer>(true);
            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            ownerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
            Transform cameraRoot = player.transform.Find("CameraRoot");
            NUnitAssert.That(cameraRoot, Is.Not.Null);
            NUnitAssert.That(characterRenderers, Is.Not.Empty, "The canonical player must contain visible character renderers.");
            foreach (Renderer characterRenderer in characterRenderers)
            {
                NUnitAssert.That(characterRenderer.enabled, Is.True, $"{characterRenderer.name} must remain enabled for external and Scene cameras.");
                NUnitAssert.That(characterRenderer.forceRenderingOff, Is.False, $"{characterRenderer.name} must not remain globally suppressed outside the owner-camera render pass.");
            }

            Cursor.lockState = CursorLockMode.Locked;

            FieldInfo sensitivityField = typeof(FusionNetworkPlayer).GetField("lookSensitivity", BindingFlags.Instance | BindingFlags.NonPublic);
            NUnitAssert.That(sensitivityField, Is.Not.Null);
            float sensitivity = (float)sensitivityField.GetValue(player);
            const float horizontalMouseDelta = 40f;
            float yawBeforeInput = cameraRoot.eulerAngles.y;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                InputSystem.QueueDeltaStateEvent(
                    mouse.delta,
                    new Vector2(horizontalMouseDelta, 0f));
                yield return null;

                float renderedYawDelta = Mathf.DeltaAngle(
                    yawBeforeInput,
                    cameraRoot.eulerAngles.y);
                NUnitAssert.That(
                    renderedYawDelta,
                    Is.EqualTo(horizontalMouseDelta * sensitivity)
                        .Within(0.75f),
                    "The owner camera should consume raw mouse delta in the " +
                    "next rendered frame without tick delay or delta-time " +
                    "scaling.");
            }
            else
            {
                NUnitAssert.That(
                    Application.isBatchMode,
                    Is.True,
                    "Interactive PlayMode must support the locked cursor used by camera-look validation.");
            }

            // The headless editor does not provide stable rendered-frame input timing.
            // Keep this end-to-end movement check for interactive PlayMode runs.
            if (!Application.isBatchMode)
            {
                yield return
                    ValidateLocomotionUsesRootRelativeDirectionsAndForwardOnlySprint(
                        player,
                        player.GetComponent<NetworkCharacterController>(),
                        keyboard,
                        animator);
            }

            Camera ownerCamera = cameraRoot.GetComponentInChildren<Camera>(true);
            UniversalAdditionalCameraData cameraData = ownerCamera.GetComponent<UniversalAdditionalCameraData>();
            Volume ownerVolume = ownerCamera.GetComponentInChildren<Volume>(true);
            NUnitAssert.That(cameraData, Is.Not.Null);
            NUnitAssert.That(cameraData.renderPostProcessing, Is.True, "The owning gameplay camera must render URP post-processing.");
            NUnitAssert.That(ownerVolume, Is.Not.Null, "The owning camera must contain its runtime-only vignette Volume.");
            NUnitAssert.That(cameraData.volumeLayerMask.value, Is.EqualTo(1 << ownerVolume.gameObject.layer));
            NUnitAssert.That(ownerVolume.isGlobal, Is.True);
            NUnitAssert.That(ownerVolume.profile, Is.Not.Null);
            NUnitAssert.That(ownerVolume.HasInstantiatedProfile(), Is.True, "The owner must modify a unique runtime Volume profile.");
            NUnitAssert.That(Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));

            NUnitAssert.That(ownerVolume.profile.TryGet(out Vignette vignette), Is.True);
            player.DebugRequestExhaustion();
            yield return WaitUntil(() => player.Stamina <= 0.01f, 1f, "Development exhaustion was not applied during Fusion simulation.");
            yield return new WaitForSeconds(0.2f);
            NUnitAssert.That(vignette.intensity.value, Is.GreaterThan(0.35f), "Exhaustion should create a clearly visible edge vignette.");
            NUnitAssert.That(vignette.color.value.r, Is.LessThan(0.08f), "Stamina exhaustion should remain black.");

            player.TakeDamage(75);
            yield return WaitUntil(() => player.Health <= 25.01f, 1f, "Queued integer damage was not applied during Fusion simulation.");
            yield return new WaitForSeconds(0.15f);
            NUnitAssert.That(vignette.color.value.r, Is.GreaterThan(vignette.color.value.g + 0.25f), "Damage should produce an obvious red pulse even while exhausted.");
            float combinedIntensity = vignette.intensity.value;
            yield return new WaitForSeconds(1.5f);
            NUnitAssert.That(vignette.intensity.value, Is.LessThan(combinedIntensity), "The initial damage flash should settle smoothly into the persistent health vignette.");
            NUnitAssert.That(vignette.intensity.value, Is.GreaterThan(0.5f), "Low health should keep an obvious edge vignette without covering the center of the screen.");
            NUnitAssert.That(vignette.color.value.r, Is.GreaterThan(vignette.color.value.g + 0.15f), "Missing health should retain a persistent red vignette after the initial flash.");
        }

        [UnityTest]
        public IEnumerator OwnedPlayerMaintainsBothEquipmentPresentations()
        {
            yield return FusionPlayModeTestSession.ResetExistingSession();
            yield return SceneManager.LoadSceneAsync(
                FusionSessionManager.GameplayScenePath,
                LoadSceneMode.Single);

            FusionNetworkPlayer player = null;
            yield return WaitUntil(
                () => (player = Object.FindFirstObjectByType<
                    FusionNetworkPlayer>()) != null,
                30f,
                "Direct scene Play did not spawn the owned Fusion player.");
            NUnitAssert.That(player.HasInputAuthority, Is.True);
            NUnitAssert.That(player.HasStateAuthority, Is.True);

            NUnitAssert.That(
                player.Inventory.TryAddItemAuthoritative(
                    "sawed_off_shotgun",
                    out InventoryRequestRejection addRejection),
                Is.True,
                $"Failed to add the owner-presentation test shotgun: {addRejection}");
            ushort shotgunInstanceId = 0;
            for (int index = 0;
                 index < NetworkPlayerInventory.MaximumItems;
                 index++)
            {
                NetworkInventoryEntry entry =
                    player.Inventory.Entries.Get(index);
                if (entry.IsOccupied &&
                    entry.ItemId.ToString() == "sawed_off_shotgun")
                {
                    shotgunInstanceId = entry.InstanceId;
                    break;
                }
            }

            NUnitAssert.That(shotgunInstanceId, Is.Not.Zero);
            player.Inventory.RequestEquip(shotgunInstanceId);
            yield return WaitUntil(
                () => player.Inventory.EquippedInstanceId == shotgunInstanceId,
                3f,
                "The owner-presentation test shotgun was not equipped.");

            PlayerEquipment equipment = player.GetComponent<PlayerEquipment>();
            PlayerEquipmentRigController equipmentRig =
                player.GetComponentInChildren<PlayerEquipmentRigController>(true);
            yield return WaitUntil(
                () => equipment.FirstPersonHeldVisual != null &&
                      equipment.ThirdPersonHeldVisual != null,
                2f,
                "The local owner must build first- and third-person held visuals together.");
            NUnitAssert.That(
                equipment.FirstPersonHeldVisual.IsOwnerPresentation,
                Is.True);
            NUnitAssert.That(
                equipment.ThirdPersonHeldVisual.IsOwnerPresentation,
                Is.False);
            NUnitAssert.That(equipment.ThirdPersonRenderers, Is.Not.Empty);
            NUnitAssert.That(equipmentRig.Equipment, Is.SameAs(equipment));
            NUnitAssert.That(equipmentRig.DesiredRightWeight, Is.EqualTo(1f));
            NUnitAssert.That(equipmentRig.DesiredLeftWeight, Is.EqualTo(1f));

            Renderer[] firstPersonRenderers =
                equipment.FirstPersonHeldVisual.GetComponentsInChildren<
                    Renderer>(true);
            equipment.SetOwnerCameraThirdPersonSuppressed(true);
            foreach (Renderer thirdPersonRenderer in
                     equipment.ThirdPersonRenderers)
            {
                NUnitAssert.That(
                    thirdPersonRenderer.forceRenderingOff,
                    Is.True,
                    "Only the local third-person held item should be suppressed for the owner camera.");
            }

            foreach (Renderer firstPersonRenderer in firstPersonRenderers)
            {
                NUnitAssert.That(
                    firstPersonRenderer.forceRenderingOff,
                    Is.False,
                    "Owner suppression must leave the first-person item visible.");
            }

            equipment.SetOwnerCameraThirdPersonSuppressed(false);
            foreach (Renderer thirdPersonRenderer in
                     equipment.ThirdPersonRenderers)
            {
                NUnitAssert.That(
                    thirdPersonRenderer.forceRenderingOff,
                    Is.False,
                    "Scene and external cameras must see the local third-person item.");
            }

            player.Inventory.RequestEquip(shotgunInstanceId);
            yield return WaitUntil(
                () => player.Inventory.EquippedInstanceId == 0,
                3f,
                "The owner-presentation test shotgun was not unequipped.");
            yield return WaitUntil(
                () => equipment.FirstPersonHeldVisual == null &&
                      equipment.ThirdPersonHeldVisual == null,
                2f,
                "Unequipping must clear both local presentation objects.");
            NUnitAssert.That(equipment.ThirdPersonRenderers, Is.Empty);
            NUnitAssert.That(equipmentRig.Equipment, Is.Null);
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            float timeout = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < timeout)
                yield return null;

            NUnitAssert.That(condition(), Is.True, failureMessage);
        }

        private static IEnumerator HoldKeys(Keyboard keyboard, float seconds, params Key[] keys)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                yield return null;
            }
        }

        private static IEnumerator ReleaseAndSettle(Keyboard keyboard, NetworkCharacterController controller)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return WaitUntil(
                () => new Vector2(controller.Velocity.x, controller.Velocity.z).magnitude < 0.12f,
                2f,
                "Player did not settle after movement input was released.");
        }

        private static IEnumerator AssertInvalidSprintDirection(
            Keyboard keyboard,
            FusionNetworkPlayer player,
            NetworkCharacterController controller,
            Animator animator,
            Key direction,
            string label)
        {
            float staminaBefore = player.Stamina;
            yield return HoldKeys(keyboard, 0.5f, Key.LeftShift, direction);
            NUnitAssert.That((bool)player.IsSprinting, Is.False, $"{label} must not activate sprinting.");
            NUnitAssert.That(player.Stamina, Is.GreaterThanOrEqualTo(staminaBefore - 0.05f), $"{label} must not drain stamina.");
            NUnitAssert.That(animator.GetFloat("MoveY"), Is.LessThanOrEqualTo(1.05f), $"{label} must not send sprint animation values.");
            yield return ReleaseAndSettle(keyboard, controller);
        }

        private static int CountEnabled<T>() where T : UnityEngine.Behaviour
        {
            T[] behaviours = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (T behaviour in behaviours)
            {
                if (behaviour != null && behaviour.enabled && behaviour.gameObject.activeInHierarchy)
                    count++;
            }

            return count;
        }
    }
}
