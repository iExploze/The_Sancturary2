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
    /// <summary>Owns the one persistent runner and the prototype's menu, lobby, and gameplay lifecycle.</summary>
    public sealed class FusionSessionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public const string MenuScenePath = "Assets/Scenes/FusionPrototypeMenu.unity";
        public const string LobbyScenePath = "Assets/Scenes/FusionPrototypeLobby.unity";
        public const string GameplayScenePath = "Assets/Scenes/GrayboxPrototype.unity";
        public const string EscapeScenePath = "Assets/Scenes/FusionPrototypeEscape.unity";
        public const int MaximumPlayers = 4;
        public const int MaximumSessionNameLength = 32;

        private readonly Dictionary<PlayerRef, NetworkObject> _gameplayPlayers = new();
        private readonly Dictionary<PlayerRef, FusionLobbyPlayerState> _lobbyPlayers = new();
        private readonly HashSet<PlayerRef> _pendingPlayers = new();

        private NetworkObject _playerPrefab;
        private NetworkObject _lobbyPlayerStatePrefab;
        private NetworkRunner _runner;
        private FusionNetworkPlayer _localPlayer;
        private bool _sceneReady;
        private bool _starting;
        private bool _gameplayLoading;
        private bool _escapeLoading;
        private bool _lobbyLoading;
        private bool _returningToMenu;
        private bool _menuLoadStarted;
        private bool _lobbyReadyToggleQueued;
        private string _gameplayScenePath = GameplayScenePath;
        private int _nextSpawnIndex;

        public static FusionSessionManager Instance { get; private set; }
        public NetworkRunner Runner => _runner;
        public bool IsInLobby => _sceneReady && ActiveScenePath == LobbyScenePath;
        public bool IsInEscape => _sceneReady && ActiveScenePath == EscapeScenePath;
        public bool IsInGameplay => _sceneReady &&
                                    ActiveScenePath != MenuScenePath &&
                                    ActiveScenePath != LobbyScenePath &&
                                    ActiveScenePath != EscapeScenePath;
        public bool IsGameplayLoading => _gameplayLoading;
        public string LastStatus { get; private set; }
        public bool LastStatusIsError { get; private set; }
        public static bool HasFusionAppId => PhotonAppSettings.Global?.AppSettings != null &&
                                             !string.IsNullOrWhiteSpace(PhotonAppSettings.Global.AppSettings.AppIdFusion);

        public event Action<string, bool> StatusChanged;

        public static bool HasActiveRunner => NetworkRunner.Instances.Any(runner => runner != null && (runner.IsRunning || runner.IsStarting));

        public static FusionSessionManager GetOrCreate(NetworkObject playerPrefab, NetworkObject lobbyPlayerStatePrefab = null)
        {
            if (Instance == null)
            {
                GameObject root = new("FusionSessionManager");
                Instance = root.AddComponent<FusionSessionManager>();
            }

            if (playerPrefab != null)
                Instance._playerPrefab = playerPrefab;
            if (lobbyPlayerStatePrefab != null)
                Instance._lobbyPlayerStatePrefab = lobbyPlayerStatePrefab;
            return Instance;
        }

        private string ActiveScenePath => SceneManager.GetActiveScene().path;

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

        public Task<bool> StartHostAsync(string sessionName) => StartOnlineAsync(GameMode.Host, sessionName);
        public Task<bool> StartClientAsync(string sessionName) => StartOnlineAsync(GameMode.Client, sessionName);

        private Task<bool> StartOnlineAsync(GameMode mode, string sessionName)
        {
            if (!HasFusionAppId)
                return ReportMissingConfiguration();
            if (_lobbyPlayerStatePrefab == null)
            {
                PublishStatus("The lobby player-state prefab is not assigned or registered with Fusion.", true);
                return Task.FromResult(false);
            }

            return StartRunnerAsync(mode, NormalizeSessionName(sessionName), MaximumPlayers, false, LobbyScenePath);
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
            if (!HasFusionAppId)
                PublishStatus("Photon App ID is blank; direct Play is using Fusion local single-player mode.", false);
            return StartRunnerAsync(mode, $"debug-{Application.productName}", 1, true, gameplayScenePath);
        }

        public void RegisterLocalPlayer(FusionNetworkPlayer player) => _localPlayer = player;

        public IReadOnlyList<FusionLobbyPlayerState> GetLobbyPlayers()
        {
            // The host owns the spawn dictionary, while clients receive the replicated
            // objects without receiving that local bookkeeping. Querying the scene keeps
            // the presentation correct for every peer and for late joiners.
            return UnityEngine.Object
                .FindObjectsByType<FusionLobbyPlayerState>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(player => player.Object != null && player.Object.IsValid)
                .OrderBy(player => player.Slot)
                .ToArray();
        }

        public bool IsHost => _runner != null && (_runner.GameMode == GameMode.Host || _runner.GameMode == GameMode.Single);

        public bool CanStartGame => IsHost && IsInLobby && !_gameplayLoading &&
                                    LobbyRules.AreAllReady(_runner?.ActivePlayers, GetLobbyPlayers());

        public void RequestToggleLobbyReady()
        {
            if (IsInLobby && !_gameplayLoading)
                _lobbyReadyToggleQueued = true;
        }

        /// <summary>Sets the host's next lobby destination. The scene must be enabled in Build Settings.</summary>
        public void ConfigureGameplayScene(string scenePath)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
                _gameplayScenePath = scenePath;
        }

        public void RequestStartGame()
        {
            if (!CanStartGame)
            {
                PublishStatus("All connected players must be ready before the host can start.", true);
                return;
            }

            _gameplayLoading = true;
            if (SceneUtility.GetBuildIndexByScenePath(_gameplayScenePath) < 0)
            {
                _gameplayLoading = false;
                PublishStatus("The selected gameplay scene must be enabled in Build Settings.", true);
                return;
            }

            PublishStatus($"Loading {System.IO.Path.GetFileNameWithoutExtension(_gameplayScenePath)} for all players...", false);
            _runner.LoadScene(SceneRef.FromPath(_gameplayScenePath), LoadSceneMode.Single);
        }

        public bool TryLoadEscapeSceneAuthoritative(NetworkRunner requestingRunner)
        {
            if (requestingRunner == null || requestingRunner != _runner ||
                !requestingRunner.IsSceneAuthority || !IsInGameplay || _escapeLoading)
                return false;

            if (SceneUtility.GetBuildIndexByScenePath(EscapeScenePath) < 0)
            {
                PublishStatus("The escape scene must be enabled in Build Settings.", true);
                return false;
            }

            _escapeLoading = true;
            PublishStatus("The exit was opened. Loading the escape screen for all players...", false);
            requestingRunner.LoadScene(SceneRef.FromPath(EscapeScenePath), LoadSceneMode.Single);
            return true;
        }

        public bool TryReturnToLobbyAuthoritative(NetworkRunner requestingRunner)
        {
            if (requestingRunner == null || requestingRunner != _runner ||
                !requestingRunner.IsSceneAuthority || !IsInEscape || _lobbyLoading)
                return false;

            if (SceneUtility.GetBuildIndexByScenePath(LobbyScenePath) < 0)
            {
                PublishStatus("The lobby scene must be enabled in Build Settings.", true);
                return false;
            }

            _lobbyLoading = true;
            PublishStatus("Returning everyone to the lobby...", false);
            requestingRunner.LoadScene(SceneRef.FromPath(LobbyScenePath), LoadSceneMode.Single);
            return true;
        }

        public async void LeaveSessionAndReturnToMenu()
        {
            if (_returningToMenu)
                return;

            _returningToMenu = true;
            PublishStatus(IsHost ? "Stopping session..." : "Leaving session...", false);
            if (_runner != null)
                await _runner.Shutdown(destroyGameObject: false);
            await ReturnToMainMenuOnceAsync();
        }

        public void StopSessionAndReturnToMenu()
        {
            if (!IsHost)
            {
                PublishStatus("Only the host can stop this session.", true);
                return;
            }

            LeaveSessionAndReturnToMenu();
        }

        private async Task<bool> ReportMissingConfiguration()
        {
            PublishStatus("Photon Fusion App ID is missing. Configure it in Tools > Fusion > Fusion Hub before hosting or joining.", true);
            await Task.Yield();
            return false;
        }

        private async Task<bool> StartRunnerAsync(GameMode mode, string sessionName, int playerCount, bool directDebug, string scenePath)
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
            _gameplayLoading = false;
            _menuLoadStarted = false;
            PublishStatus(mode == GameMode.Client ? "Joining room..." : "Starting room...", false);

            _runner = gameObject.AddComponent<NetworkRunner>();
            NetworkSceneManagerDefault sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            NetworkObjectProviderDefault objectProvider = gameObject.AddComponent<NetworkObjectProviderDefault>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

            NetworkSceneInfo sceneInfo = new();
            sceneInfo.AddSceneRef(SceneRef.FromPath(scenePath), LoadSceneMode.Single);
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
            if (result.Ok)
            {
                PublishStatus(mode == GameMode.Client ? $"Joined {sessionName}." : $"Room {sessionName} started.", false);
                return true;
            }

            PublishStatus($"Fusion failed to start: {result.ShutdownReason}", true);
            CleanupSessionReferences();
            return false;
        }

        public static string NormalizeSessionName(string value)
        {
            string trimmed = string.IsNullOrWhiteSpace(value) ? "sancturary-prototype" : value.Trim();
            return trimmed.Length <= MaximumSessionNameLength ? trimmed : trimmed[..MaximumSessionNameLength];
        }

        private void PublishStatus(string message, bool isError)
        {
            LastStatus = message;
            LastStatusIsError = isError;
            if (isError)
                Debug.LogWarning(message, this);
            else
                Debug.Log(message, this);
            StatusChanged?.Invoke(message, isError);
        }

        private void TrySpawnPending(NetworkRunner runner)
        {
            if (!_sceneReady || !runner.CanSpawn)
                return;

            if (IsInLobby)
            {
                foreach (PlayerRef player in _pendingPlayers.ToArray())
                {
                    if (_lobbyPlayers.ContainsKey(player) || _lobbyPlayerStatePrefab == null)
                        continue;
                    int slot = LobbyRules.LowestAvailableSlot(_lobbyPlayers.Values.Select(state => state.Slot), MaximumPlayers);
                    if (slot < 1)
                        continue;
                    NetworkObject stateObject = runner.Spawn(_lobbyPlayerStatePrefab, inputAuthority: player);
                    FusionLobbyPlayerState state = stateObject != null ? stateObject.GetComponent<FusionLobbyPlayerState>() : null;
                    if (state == null)
                        continue;
                    state.SetSlotAuthoritative(slot, player == runner.LocalPlayer);
                    _lobbyPlayers[player] = state;
                    _pendingPlayers.Remove(player);
                }
                return;
            }

            if (!IsInGameplay || _playerPrefab == null)
                return;

            FusionSpawnPoint[] spawnPoints = UnityEngine.Object.FindObjectsByType<FusionSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OrderBy(point => point.index).ThenBy(point => point.name).ToArray();
            foreach (PlayerRef player in _pendingPlayers.ToArray())
            {
                if (_gameplayPlayers.ContainsKey(player) || runner.TryGetPlayerObject(player, out _))
                {
                    _pendingPlayers.Remove(player);
                    continue;
                }

                Transform spawn = spawnPoints.Length > 0 ? spawnPoints[_nextSpawnIndex % spawnPoints.Length].transform : null;
                NetworkObject playerObject = runner.Spawn(_playerPrefab, spawn != null ? spawn.position : Vector3.up, spawn != null ? spawn.rotation : Quaternion.identity, player);
                if (playerObject == null)
                    continue;
                runner.SetPlayerObject(player, playerObject);
                _gameplayPlayers[player] = playerObject;
                _pendingPlayers.Remove(player);
                _nextSpawnIndex++;
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.CanSpawn)
            {
                _pendingPlayers.Add(player);
                TrySpawnPending(runner);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            _pendingPlayers.Remove(player);
            if (_lobbyPlayers.Remove(player, out FusionLobbyPlayerState lobbyState) && lobbyState != null && runner.CanSpawn)
                runner.Despawn(lobbyState.Object);
            if (_gameplayPlayers.Remove(player, out NetworkObject playerObject) && playerObject != null && runner.CanSpawn)
                runner.Despawn(playerObject);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (IsInLobby)
            {
                FusionPlayerInput lobbyInput = default;
                lobbyInput.Buttons.Set(FusionPlayerButton.LobbyReady, _lobbyReadyToggleQueued);
                _lobbyReadyToggleQueued = false;
                input.Set(lobbyInput);
                return;
            }

            if (_localPlayer != null && _localPlayer.HasInputAuthority)
                input.Set(_localPlayer.BuildNetworkInput());
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            bool wasInLobby = IsInLobby;
            bool wasInGameplay = IsInGameplay;
            _sceneReady = false;
            _pendingPlayers.Clear();
            if (wasInLobby && runner.CanSpawn)
            {
                foreach (FusionLobbyPlayerState state in _lobbyPlayers.Values.Where(state => state != null))
                    runner.Despawn(state.Object);
            }
            _lobbyPlayers.Clear();

            if (wasInGameplay && runner.CanSpawn)
            {
                foreach (KeyValuePair<PlayerRef, NetworkObject> entry in _gameplayPlayers)
                {
                    if (entry.Value == null || !entry.Value.IsValid)
                        continue;

                    runner.SetPlayerObject(entry.Key, null);
                    runner.Despawn(entry.Value);
                }
            }
            _gameplayPlayers.Clear();
            _localPlayer = null;
            _nextSpawnIndex = 0;
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            _sceneReady = true;
            if (IsInGameplay)
                _gameplayLoading = false;
            if (IsInEscape)
                _escapeLoading = false;
            if (IsInLobby)
                _lobbyLoading = false;
            foreach (PlayerRef player in runner.ActivePlayers)
                _pendingPlayers.Add(player);
            TrySpawnPending(runner);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            bool wasRequested = _returningToMenu;
            CleanupSessionReferences();
            PublishStatus(wasRequested ? "Returned to main menu." : $"Session ended: {shutdownReason}", !wasRequested && shutdownReason != ShutdownReason.Ok);
            _ = ReturnToMainMenuOnceAsync();
        }

        private void CleanupSessionReferences()
        {
            _gameplayPlayers.Clear();
            _lobbyPlayers.Clear();
            _pendingPlayers.Clear();
            _localPlayer = null;
            _sceneReady = false;
            _gameplayLoading = false;
            _escapeLoading = false;
            _lobbyLoading = false;
            _lobbyReadyToggleQueued = false;
            _starting = false;
            _nextSpawnIndex = 0;
            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                Destroy(_runner);
                _runner = null;
            }
        }

        private async Task ReturnToMainMenuOnceAsync()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_menuLoadStarted)
                return;
            _menuLoadStarted = true;
            _returningToMenu = false;
            if (ActiveScenePath != MenuScenePath)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(MenuScenePath, LoadSceneMode.Single);
                if (load != null)
                    while (!load.isDone)
                        await Task.Yield();
            }
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => PublishStatus($"Disconnected: {reason}", true);
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
