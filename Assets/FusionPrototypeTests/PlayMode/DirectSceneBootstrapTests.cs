using System.Collections;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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
