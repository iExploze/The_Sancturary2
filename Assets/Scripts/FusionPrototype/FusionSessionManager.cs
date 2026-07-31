using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheSancturary.FusionPrototype
{
    public sealed class FusionSessionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public const string GameplayScenePath = "Assets/Scenes/MapLevel.unity";
        public const int MaximumPlayers = 4;

        private readonly Dictionary<PlayerRef, NetworkObject> _players = new();
        private readonly HashSet<PlayerRef> _pendingPlayers = new();

        private NetworkObject _playerPrefab;
        private NetworkRunner _runner;
        private FusionNetworkPlayer _localPlayer;
        private bool _sceneReady;
        private bool _starting;
        private int _nextSpawnIndex;

        public static FusionSessionManager Instance { get; private set; }
        public static bool HasFusionAppId =>
            PhotonAppSettings.Global != null &&
            PhotonAppSettings.Global.AppSettings != null &&
            !string.IsNullOrWhiteSpace(PhotonAppSettings.Global.AppSettings.AppIdFusion);

        public event Action<string, bool> StatusChanged;

        public static bool HasActiveRunner
        {
            get
            {
                foreach (NetworkRunner runner in NetworkRunner.Instances)
                {
                    if (runner != null && (runner.IsRunning || runner.IsStarting))
                        return true;
                }

                return false;
            }
        }

        public static FusionSessionManager GetOrCreate(NetworkObject playerPrefab)
        {
            if (Instance != null)
            {
                if (playerPrefab != null)
                    Instance._playerPrefab = playerPrefab;
                return Instance;
            }

            GameObject root = new("FusionSessionManager");
            FusionSessionManager manager = root.AddComponent<FusionSessionManager>();
            manager._playerPrefab = playerPrefab;
            return manager;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Task<bool> StartHostAsync(string sessionName)
        {
            if (!HasFusionAppId)
                return ReportMissingConfiguration();

            return StartRunnerAsync(GameMode.Host, NormalizeSessionName(sessionName), MaximumPlayers, false);
        }

        public Task<bool> StartClientAsync(string sessionName)
        {
            if (!HasFusionAppId)
                return ReportMissingConfiguration();

            return StartRunnerAsync(GameMode.Client, NormalizeSessionName(sessionName), MaximumPlayers, false);
        }

        public Task<bool> StartDirectDebugAsync(string gameplayScenePath)
        {
            if (HasActiveRunner)
                return Task.FromResult(true);

            if (string.IsNullOrWhiteSpace(gameplayScenePath))
            {
                PublishStatus("The active scene must be saved before starting a direct debug session.", true);
                return Task.FromResult(false);
            }

            GameMode mode = HasFusionAppId ? GameMode.Host : GameMode.Single;
            string session = $"debug-{Application.productName}";
            if (!HasFusionAppId)
                PublishStatus("Photon App ID is blank; direct Play is using Fusion local single-player mode.", false);

            return StartRunnerAsync(mode, session, 1, true, gameplayScenePath);
        }

        public void RegisterLocalPlayer(FusionNetworkPlayer player)
        {
            _localPlayer = player;
        }

        private async Task<bool> ReportMissingConfiguration()
        {
            PublishStatus("Photon Fusion App ID is missing. Configure it in Tools > Fusion > Fusion Hub before hosting or joining.", true);
            await Task.Yield();
            return false;
        }

        private async Task<bool> StartRunnerAsync(
            GameMode mode,
            string sessionName,
            int playerCount,
            bool directDebug,
            string gameplayScenePath = GameplayScenePath)
        {
            if (_starting || HasActiveRunner)
                return HasActiveRunner;

            if (_playerPrefab == null)
            {
                PublishStatus("The canonical Fusion player prefab is not assigned.", true);
                return false;
            }

            _starting = true;
            _sceneReady = false;
            PublishStatus(mode == GameMode.Client ? "Joining session..." : "Starting session...", false);

            _runner = gameObject.AddComponent<NetworkRunner>();
            NetworkSceneManagerDefault sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            NetworkObjectProviderDefault objectProvider = gameObject.AddComponent<NetworkObjectProviderDefault>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

            NetworkSceneInfo sceneInfo = new();
            sceneInfo.AddSceneRef(SceneRef.FromPath(gameplayScenePath), LoadSceneMode.Single);

            StartGameResult result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = sessionName,
                PlayerCount = playerCount,
                IsOpen = true,
                IsVisible = !directDebug,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                ObjectProvider = objectProvider
            });

            _starting = false;
            if (!result.Ok)
            {
                PublishStatus($"Fusion failed to start: {result.ShutdownReason}", true);
                return false;
            }

            PublishStatus(mode == GameMode.Client ? $"Joined {sessionName}." : $"Session {sessionName} started.", false);
            return true;
        }

        private void PublishStatus(string message, bool isError)
        {
            if (isError)
                Debug.LogError(message, this);
            else
                Debug.Log(message, this);
            StatusChanged?.Invoke(message, isError);
        }

        private static string NormalizeSessionName(string value)
        {
            string trimmed = string.IsNullOrWhiteSpace(value) ? "sancturary-prototype" : value.Trim();
            return trimmed.Length <= 32 ? trimmed : trimmed[..32];
        }

        private void TrySpawnPending(NetworkRunner runner)
        {
            if (!_sceneReady || !runner.CanSpawn || _playerPrefab == null)
                return;

            FusionSpawnPoint[] spawnPoints = UnityEngine.Object
                .FindObjectsByType<FusionSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OrderBy(point => point.index)
                .ThenBy(point => point.name)
                .ToArray();

            foreach (PlayerRef player in _pendingPlayers.ToArray())
            {
                if (_players.ContainsKey(player) || runner.TryGetPlayerObject(player, out _))
                {
                    _pendingPlayers.Remove(player);
                    continue;
                }

                Transform spawn = spawnPoints.Length > 0 ? spawnPoints[_nextSpawnIndex % spawnPoints.Length].transform : null;
                Vector3 position = spawn != null ? spawn.position : Vector3.up;
                Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;
                NetworkObject playerObject = runner.Spawn(_playerPrefab, position, rotation, player);
                if (playerObject == null)
                    continue;

                runner.SetPlayerObject(player, playerObject);
                _players[player] = playerObject;
                _pendingPlayers.Remove(player);
                _nextSpawnIndex++;
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.CanSpawn)
                return;

            _pendingPlayers.Add(player);
            TrySpawnPending(runner);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            _pendingPlayers.Remove(player);
            if (_players.Remove(player, out NetworkObject playerObject) && playerObject != null && runner.CanSpawn)
                runner.Despawn(playerObject);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (_localPlayer != null && _localPlayer.HasInputAuthority)
                input.Set(_localPlayer.BuildNetworkInput());
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            _sceneReady = true;
            foreach (PlayerRef player in runner.ActivePlayers)
                _pendingPlayers.Add(player);
            TrySpawnPending(runner);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            _players.Clear();
            _pendingPlayers.Clear();
            _localPlayer = null;
            _sceneReady = false;
            PublishStatus($"Fusion stopped: {shutdownReason}", shutdownReason != ShutdownReason.Ok);
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => PublishStatus($"Disconnected: {reason}", true);
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => PublishStatus($"Connection failed: {reason}", true);
#pragma warning disable CS0618
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
#pragma warning restore CS0618
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadStart(NetworkRunner runner) => _sceneReady = false;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
