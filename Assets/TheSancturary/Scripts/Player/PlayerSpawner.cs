using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TheSancturary.Player
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject playerPrefab;

        private PlayerSpawnPoint[] spawnPoints = Array.Empty<PlayerSpawnPoint>();

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            spawnPoints = FindObjectsByType<PlayerSpawnPoint>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .OrderBy(point => point.name, StringComparer.Ordinal)
                .ToArray();

            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                SpawnPlayerIfNeeded(clientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            SpawnPlayerIfNeeded(clientId);
        }

        private void SpawnPlayerIfNeeded(ulong clientId)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) ||
                client.PlayerObject != null)
            {
                return;
            }

            var position = Vector3.up;
            var rotation = Quaternion.identity;
            if (spawnPoints.Length > 0)
            {
                var point = spawnPoints[clientId % (ulong)spawnPoints.Length].transform;
                position = point.position;
                rotation = point.rotation;
            }

            var playerInstance = Instantiate(playerPrefab, position, rotation);
            playerInstance.SpawnAsPlayerObject(clientId, true);
        }
    }
}
