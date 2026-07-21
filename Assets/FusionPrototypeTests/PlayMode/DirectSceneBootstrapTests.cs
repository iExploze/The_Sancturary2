using System.Collections;
using System.Reflection;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnitAssert = NUnit.Framework.Assert;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class DirectSceneBootstrapTests
    {
        [UnityTest]
        public IEnumerator DirectSceneSpawnsOneOwnedCanonicalPlayer()
        {
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
            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            ownerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
            Transform cameraRoot = player.transform.Find("CameraRoot");
            NUnitAssert.That(cameraRoot, Is.Not.Null);
            Cursor.lockState = CursorLockMode.Locked;

            FieldInfo sensitivityField = typeof(FusionNetworkPlayer).GetField("lookSensitivity", BindingFlags.Instance | BindingFlags.NonPublic);
            NUnitAssert.That(sensitivityField, Is.Not.Null);
            float sensitivity = (float)sensitivityField.GetValue(player);
            const float horizontalMouseDelta = 40f;
            float yawBeforeInput = cameraRoot.eulerAngles.y;

            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(horizontalMouseDelta, 0f));
            yield return null;

            float renderedYawDelta = Mathf.DeltaAngle(yawBeforeInput, cameraRoot.eulerAngles.y);
            NUnitAssert.That(
                renderedYawDelta,
                Is.EqualTo(horizontalMouseDelta * sensitivity).Within(0.75f),
                "The owner camera should consume raw mouse delta in the next rendered frame without tick delay or delta-time scaling.");

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

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            float timeout = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < timeout)
                yield return null;

            NUnitAssert.That(condition(), Is.True, failureMessage);
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
