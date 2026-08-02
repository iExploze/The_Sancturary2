using System.Collections;
using System.Linq;
using NUnit.Framework;
using TheSancturary.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class EscapeFlowTests
    {
        private const string ReferenceScenePath = "Assets/Scenes/Reference Playable.unity";

        [UnityTest]
        public IEnumerator ExitDoorLoadsEscapeScreenAndReturnsSessionToLobby()
        {
            yield return SceneManager.LoadSceneAsync(ReferenceScenePath, LoadSceneMode.Single);

            FusionNetworkPlayer player = null;
            yield return WaitUntil(
                () => (player = Object.FindFirstObjectByType<FusionNetworkPlayer>()) != null,
                30f,
                "Direct Play did not spawn a Fusion player in Reference Playable.");

            NetworkExitDoor exitDoor = Object.FindFirstObjectByType<NetworkExitDoor>();
            Assert.That(exitDoor, Is.Not.Null);
            Assert.That(player.HasStateAuthority, Is.True);
            AssertDoorCanBeTargetedFromBothSides(exitDoor, player.Inventory);
            Assert.That(
                player.Inventory.TryAddItemAuthoritative(
                    "maintenance_key",
                    out InventoryRequestRejection rejection),
                Is.True,
                $"Failed to give the authoritative test player the exit key: {rejection}");
            Assert.That(exitDoor.TryInteractAuthoritative(player), Is.True);

            yield return WaitUntil(
                () => SceneManager.GetActiveScene().path == FusionSessionManager.EscapeScenePath,
                10f,
                "The authoritative exit interaction did not load the escape scene.");

            Text escapedMessage = Object.FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(text => text.text == "YOU'VE ESCAPED");
            Assert.That(escapedMessage, Is.Not.Null);

            yield return WaitUntil(
                () => SceneManager.GetActiveScene().path == FusionSessionManager.LobbyScenePath,
                10f,
                "The escape countdown did not return the session to the lobby.");

            yield return WaitUntil(
                () => Object.FindFirstObjectByType<FusionLobbyPlayerState>() != null,
                5f,
                "The direct-debug player did not receive a lobby player state after returning.");
        }

        private static void AssertDoorCanBeTargetedFromBothSides(
            NetworkExitDoor exitDoor,
            NetworkPlayerInventory inventory)
        {
            Collider doorCollider = exitDoor.GetComponent<Collider>();
            Assert.That(doorCollider, Is.Not.Null);

            Vector3 center = doorCollider.bounds.center;
            Vector3[] origins =
            {
                center - Vector3.forward * 1.5f,
                center + Vector3.forward * 1.5f
            };
            Vector3[] directions = { Vector3.forward, Vector3.back };
            for (int index = 0; index < origins.Length; index++)
            {
                Ray ray = new(origins[index], directions[index]);
                Assert.That(
                    Physics.Raycast(ray, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore),
                    Is.True,
                    $"ExitDoor side {index} did not have a targetable collider.");

                InteractionTarget resolvedTarget = InteractionTarget.ResolveFromCollider(hit.collider, ray);
                Assert.That(resolvedTarget, Is.SameAs(exitDoor.PromptTarget));
                Assert.That(resolvedTarget.TryGetPrompt(inventory, out InteractionPrompt prompt), Is.True);
                Assert.That(prompt.DisplayName, Is.EqualTo("Exit Door"));
                Assert.That(prompt.ActionText, Does.Contain("Escape"));
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
