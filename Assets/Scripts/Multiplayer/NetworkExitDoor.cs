using TheSancturary.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheSancturary.Multiplayer
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public sealed class NetworkExitDoor : NetworkBehaviour
    {
        private bool escapeStarted;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || escapeStarted)
            {
                return;
            }

            var player = other.GetComponentInParent<NetworkPlayerController>();
            var playerObject = player != null ? player.NetworkObject : null;
            if (!ExitEligibility.IsEligible(
                    playerObject != null && playerObject.IsSpawned,
                    playerObject != null && playerObject.IsPlayerObject))
            {
                return;
            }

            escapeStarted = true;
            var loadStatus = NetworkManager.SceneManager.LoadScene(
                NetworkConstants.EndSceneName,
                LoadSceneMode.Single);
            if (loadStatus != SceneEventProgressStatus.Started)
            {
                escapeStarted = false;
                Debug.LogError($"Failed to start the synchronized escape scene load: {loadStatus}.");
            }
        }
    }
}
