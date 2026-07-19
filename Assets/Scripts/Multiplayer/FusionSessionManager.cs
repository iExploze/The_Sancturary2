using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using TheSancturary.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TheSancturary.Multiplayer
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class FusionSessionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        private NetworkRunner runner;
        private NetworkObject playerPrefab;
        private string lobbySceneName;
        private string gameplaySceneName;
        private bool callbacksRegistered;
        private bool shuttingDown;

        public void Configure(
            NetworkRunner sessionRunner,
            NetworkObject networkPlayerPrefab,
            string lobbyScene,
            string gameplayScene)
        {
            runner = sessionRunner;
            playerPrefab = networkPlayerPrefab;
            lobbySceneName = lobbyScene;
            gameplaySceneName = gameplayScene;

            if (!callbacksRegistered)
            {
                runner.AddCallbacks(this);
                callbacksRegistered = true;
            }
        }

        private void Update()
        {
            if (!shuttingDown &&
                runner != null &&
                runner.IsRunning &&
                Keyboard.current?.f10Key.wasPressedThisFrame == true)
            {
                ShutdownAndReturnToLobby();
            }
        }

        public async void ShutdownAndReturnToLobby()
        {
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            if (runner != null && runner.IsRunning)
            {
                await runner.Shutdown();
            }

            ReturnToLobbyIfNeeded();
        }

        public void OnPlayerJoined(NetworkRunner sessionRunner, PlayerRef player)
        {
            if (sessionRunner.IsSceneAuthority && IsGameplaySceneLoaded())
            {
                SpawnPlayerIfNeeded(player);
            }
        }

        public void OnPlayerLeft(NetworkRunner sessionRunner, PlayerRef player)
        {
            if (!sessionRunner.IsSceneAuthority)
            {
                return;
            }

            var playerObject = sessionRunner.GetPlayerObject(player);
            if (playerObject != null)
            {
                sessionRunner.Despawn(playerObject);
            }
        }

        public void OnSceneLoadDone(NetworkRunner sessionRunner)
        {
            if (!sessionRunner.IsSceneAuthority || !IsGameplaySceneLoaded())
            {
                return;
            }

            foreach (var player in sessionRunner.ActivePlayers.OrderBy(value => value.PlayerId))
            {
                SpawnPlayerIfNeeded(player);
            }
        }

        public void OnShutdown(NetworkRunner sessionRunner, ShutdownReason shutdownReason)
        {
            callbacksRegistered = false;
            shuttingDown = true;
            ReturnToLobbyIfNeeded();
        }

        private void SpawnPlayerIfNeeded(PlayerRef player)
        {
            if (runner == null || !runner.IsRunning || !runner.IsSceneAuthority || playerPrefab == null)
            {
                return;
            }

            if (runner.GetPlayerObject(player) != null)
            {
                return;
            }

            var spawnPoints = runner.SimulationUnityScene
                .GetComponents<PlayerSpawnPoint>(false)
                .OrderBy(point => point.name, StringComparer.Ordinal)
                .ToArray();
            var position = new Vector3((player.PlayerId - 1) * 1.5f, 0.1f, 0f);
            var rotation = Quaternion.identity;
            if (spawnPoints.Length > 0)
            {
                var orderedPlayers = runner.ActivePlayers.OrderBy(value => value.PlayerId).ToList();
                var playerIndex = Mathf.Max(0, orderedPlayers.IndexOf(player));
                var spawnPoint = spawnPoints[playerIndex % spawnPoints.Length].transform;
                position = spawnPoint.position;
                rotation = spawnPoint.rotation;
            }

            var instance = runner.Spawn(playerPrefab, position, rotation, player);
            runner.SetPlayerObject(player, instance);
        }

        private bool IsGameplaySceneLoaded()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return false;
            }

            var gameplayScene = SceneManager.GetSceneByName(gameplaySceneName);
            return gameplayScene.IsValid() && gameplayScene.isLoaded;
        }

        private void ReturnToLobbyIfNeeded()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (string.IsNullOrWhiteSpace(lobbySceneName))
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name == lobbySceneName)
            {
                return;
            }

            SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            if (callbacksRegistered && runner != null)
            {
                runner.RemoveCallbacks(this);
            }
        }

        public void OnSceneLoadStart(NetworkRunner sessionRunner) { }
        public void OnInput(NetworkRunner sessionRunner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner sessionRunner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner sessionRunner) { }
        public void OnDisconnectedFromServer(NetworkRunner sessionRunner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner sessionRunner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner sessionRunner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner sessionRunner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner sessionRunner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner sessionRunner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner sessionRunner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner sessionRunner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner sessionRunner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnObjectExitAOI(NetworkRunner sessionRunner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner sessionRunner, NetworkObject obj, PlayerRef player) { }
    }
}
