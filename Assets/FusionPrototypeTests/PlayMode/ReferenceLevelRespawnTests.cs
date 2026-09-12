using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class ReferenceLevelRespawnTests
    {
        private const string ReferenceScenePath = "Assets/Scenes/Reference Playable.unity";

        [UnityTest]
        public IEnumerator DeadPlayerRespawnsAtSpawnAreaAfterFiveSeconds()
        {
            yield return FusionPlayModeTestSession.ResetExistingSession();
            yield return SceneManager.LoadSceneAsync(ReferenceScenePath, LoadSceneMode.Single);

            FusionNetworkPlayer player = null;
            yield return WaitUntil(
                () => (player = Object.FindFirstObjectByType<FusionNetworkPlayer>()) != null,
                30f,
                "Reference Playable did not spawn a Fusion player.");

            FusionSpawnPoint[] spawnPoints = Object.FindObjectsByType<FusionSpawnPoint>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(spawnPoints, Has.Length.EqualTo(4));

            Vector3 deathPosition = new(25f, 3f, 25f);
            player.GetComponent<Fusion.NetworkCharacterController>().Teleport(deathPosition);
            player.TakeDamage(int.MaxValue);
            yield return WaitUntil(() => player.IsDead, 2f, "Lethal damage did not kill the player.");

            PlayerInput input = player.GetComponent<PlayerInput>();
            player.BeginLocalDeathSequence(null);
            Assert.That(input.enabled, Is.False);

            yield return new WaitForSeconds(4f);
            Assert.That((bool)player.IsDead, Is.True, "The player respawned before the five-second delay elapsed.");

            yield return WaitUntil(() => !player.IsDead, 2f, "The player did not respawn after five seconds.");
            Assert.That(player.Health, Is.GreaterThan(0f));
            Assert.That(input.enabled, Is.True);
            Assert.That(input.inputIsActive, Is.True);
            yield return WaitUntil(
                () => spawnPoints.Any(point =>
                    Vector3.Distance(player.transform.position, point.transform.position) < 0.25f),
                2f,
                $"Respawn position {player.transform.position} was not in the configured spawn area.");
        }

        [UnityTest]
        public IEnumerator LockerDeathsReleaseOccupancyAndAutomaticallyRespawn()
        {
            yield return FusionPlayModeTestSession.ResetExistingSession();
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SandboxPrototype.unity", LoadSceneMode.Single);
            FusionNetworkPlayer player = null;
            yield return WaitUntil(
                () => (player = Object.FindFirstObjectByType<FusionNetworkPlayer>()) != null,
                30f, "Sandbox did not spawn a Fusion player.");

            // Shorten only this runtime test; the authored revival window stays unchanged.
            Object.FindFirstObjectByType<LevelRespawnSettings>().Configure(1f);
            LockerController locker = Object.FindObjectsByType<LockerController>(FindObjectsSortMode.None)
                .First(candidate => candidate.HasStateAuthority);
            PlayerInput input = player.GetComponent<PlayerInput>();
            for (int deathKind = 0; deathKind < 3; deathKind++)
            {
                bool entered = false;
                yield return WaitUntil(() => entered || (entered = locker.TryInteractAuthoritative(player)), 5f,
                    "Locker was not available for the next attempt.");
                yield return WaitUntil(() => player.IsHiddenInLocker, 5f, "Player did not enter the locker.");

                if (deathKind == 0)
                    Assert.That(player.KillInstantlyAuthoritative(), Is.True);
                else if (deathKind == 1)
                    player.ForceEjectFromLockerAuthoritative(locker, new Vector3(10, 1, 10), Quaternion.identity);
                else
                    player.TakeDamage(int.MaxValue);

                yield return WaitUntil(() => player.IsDead, 2f, "Locker death did not complete.");
                Assert.That(player.RespawnTimer.IsRunning, Is.True, "Death did not schedule respawn.");
                Assert.That(player.CurrentLocker.IsValid, Is.False);
                Assert.That((bool)player.IsHiddenInLocker, Is.False);
                yield return WaitUntil(() => locker.CurrentOccupant == Fusion.PlayerRef.None, 0.5f,
                    "Dead player still reserved the locker.");
                player.BeginLocalDeathSequence(null);

                yield return WaitUntil(() => !player.IsDead, 3f, "Locker victim did not automatically respawn.");
                yield return null; // Allow owner presentation to restore input.
                Assert.That(player.Health, Is.GreaterThan(0f));
                Assert.That(input.enabled && input.inputIsActive, Is.True);
                yield return WaitUntil(() => SandboxSession.IsProtected(player), 2f,
                    $"Respawn did not return the player to safe staging (death kind {deathKind}).");
                Assert.That(player.RespawnTimer.IsRunning, Is.False);
            }
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            float timeout = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(condition(), Is.True, failureMessage);
        }
    }
}
