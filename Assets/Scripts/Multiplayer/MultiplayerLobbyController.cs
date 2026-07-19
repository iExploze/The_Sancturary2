using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheSancturary.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class MultiplayerLobbyController : MonoBehaviour, INetworkRunnerCallbacks
    {
        private const int SessionCapacity = 2;

        [Header("Fusion Session")]
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private string lobbySceneName = "MultiplayerLobby";
        [SerializeField] private string gameplaySceneName = "Demo Level";
        [SerializeField, Tooltip("Allows the host to start alone only in the Editor or a Development Build.")]
        private bool allowSoloStartInDevelopment;

        [Header("UI Panels")]
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private GameObject sessionPanel;

        [Header("Setup UI")]
        [SerializeField] private TMP_InputField sessionNameInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button backButton;

        [Header("Session UI")]
        [SerializeField] private TextMeshProUGUI sessionCodeText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        private NetworkRunner runner;
        private bool isConnecting;
        private bool isLeaving;
        private bool handingOffToGameplay;

        private void Awake()
        {
            hostButton?.onClick.AddListener(HostSession);
            joinButton?.onClick.AddListener(JoinSession);
            backButton?.onClick.AddListener(BackToMainMenuOrQuit);
            startGameButton?.onClick.AddListener(StartGame);
            leaveButton?.onClick.AddListener(LeaveSession);

            ShowSetupPanel();
            SetStatus("Ready to host or join.");
        }

        private void OnDestroy()
        {
            hostButton?.onClick.RemoveListener(HostSession);
            joinButton?.onClick.RemoveListener(JoinSession);
            backButton?.onClick.RemoveListener(BackToMainMenuOrQuit);
            startGameButton?.onClick.RemoveListener(StartGame);
            leaveButton?.onClick.RemoveListener(LeaveSession);

            if (runner != null)
            {
                runner.RemoveCallbacks(this);
                if (!runner.IsRunning && !handingOffToGameplay)
                {
                    Destroy(runner.gameObject);
                }

                runner = null;
            }
        }

        public async void HostSession()
        {
            if (isConnecting || isLeaving)
            {
                return;
            }

            var sessionCode = sessionNameInput == null ? string.Empty : sessionNameInput.text.Trim();
            if (string.IsNullOrWhiteSpace(sessionCode))
            {
                sessionCode = GenerateSessionCode();
                if (sessionNameInput != null)
                {
                    sessionNameInput.text = sessionCode;
                }
            }

            await StartRunnerSession(sessionCode, true);
        }

        public async void JoinSession()
        {
            if (isConnecting || isLeaving)
            {
                return;
            }

            var sessionCode = sessionNameInput == null ? string.Empty : sessionNameInput.text.Trim();
            if (string.IsNullOrWhiteSpace(sessionCode))
            {
                SetStatus("Enter the exact session code to join.", true);
                return;
            }

            await StartRunnerSession(sessionCode, false);
        }

        public async void LeaveSession()
        {
            if (isLeaving)
            {
                return;
            }

            isLeaving = true;
            SetSessionButtonsInteractable(false);
            SetStatus("Leaving session...");
            var sessionRunner = runner;
            if (sessionRunner != null && sessionRunner.IsRunning)
            {
                await sessionRunner.Shutdown();
            }

            CleanupRunner();
            isLeaving = false;
            ShowSetupPanel();
            SetStatus("Left the session.");
        }

        public async void StartGame()
        {
            if (runner == null || !runner.IsRunning || !runner.IsSceneAuthority)
            {
                return;
            }

            var playerCount = runner.ActivePlayers.Count();
            var developmentSoloStart = allowSoloStartInDevelopment &&
                                       (Application.isEditor || Debug.isDebugBuild);
            if (playerCount < SessionCapacity && !developmentSoloStart)
            {
                SetStatus("Waiting for both players before starting.");
                return;
            }

            startGameButton.interactable = false;
            SetStatus("Loading Demo Level for the session...");
            handingOffToGameplay = true;
            try
            {
                await runner.LoadScene(
                    gameplaySceneName,
                    LoadSceneMode.Single,
                    LocalPhysicsMode.None,
                    true);
            }
            catch (Exception exception)
            {
                handingOffToGameplay = false;
                Debug.LogError($"Fusion failed to load {gameplaySceneName}: {exception.Message}", this);
                SetStatus("Unable to start the game. See the Console for details.", true);
                UpdateSessionUI();
            }
        }

        private async Task StartRunnerSession(string sessionCode, bool hosting)
        {
            if (!ValidateSessionConfiguration())
            {
                return;
            }

            if (NetworkRunner.Instances.Any(instance => instance != null && instance.IsRunning))
            {
                SetStatus("Another Fusion session is already active. Leave it before starting a new one.", true);
                return;
            }

            DestroyUnusedRunners();
            isConnecting = true;
            SetSetupButtonsInteractable(false);
            SetStatus(hosting ? "Creating Photon session..." : "Joining Photon session...");

            var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
            if (string.IsNullOrWhiteSpace(appSettings.AppIdFusion))
            {
                SetStatus("Fusion AppId is not configured on this machine.", true);
                ShowSetupPanel();
                return;
            }

            runner = Instantiate(runnerPrefab);
            runner.name = "The Sancturary Fusion Runner";
            runner.ProvideInput = true;
            DontDestroyOnLoad(runner.gameObject);
            runner.AddCallbacks(this);

            var sessionManager = runner.GetComponent<FusionSessionManager>();
            if (sessionManager == null)
            {
                sessionManager = runner.gameObject.AddComponent<FusionSessionManager>();
            }

            sessionManager.Configure(runner, playerPrefab, lobbySceneName, gameplaySceneName);

            var startArguments = new StartGameArgs
            {
                SessionName = sessionCode,
                PlayerCount = SessionCapacity,
                GameMode = hosting ? GameMode.Host : GameMode.Client,
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
                CustomPhotonAppSettings = appSettings,
                EnableClientSessionCreation = hosting
            };

            var result = await runner.StartGame(startArguments);
            if (!result.Ok)
            {
                HandleConnectionFailure(result.ShutdownReason);
                return;
            }

            isConnecting = false;
            ShowSessionPanel();
            SetStatus(hosting
                ? "Session created. Share the code with the second player."
                : "Connected. Waiting for the host to start.");
        }

        private bool ValidateSessionConfiguration()
        {
            if (runnerPrefab == null)
            {
                SetStatus("Fusion runner prefab is not assigned.", true);
                return false;
            }

            if (playerPrefab == null)
            {
                SetStatus("Fusion player prefab is not assigned.", true);
                return false;
            }

            if (runnerPrefab.GetComponent<NetworkSceneManagerDefault>() == null)
            {
                SetStatus("Fusion runner prefab is missing NetworkSceneManagerDefault.", true);
                return false;
            }

            return true;
        }

        private void HandleConnectionFailure(ShutdownReason reason)
        {
            var message = reason switch
            {
                ShutdownReason.GameIdAlreadyExists => "That session code is already in use.",
                ShutdownReason.GameNotFound => "No session exists with that code.",
                ShutdownReason.GameIsFull => "That session already has two players.",
                ShutdownReason.ConnectionTimeout => "The connection timed out.",
                _ => $"Connection failed: {reason}."
            };
            CleanupRunner();
            ShowSetupPanel();
            SetStatus(message, true);
        }

        private void CleanupRunner()
        {
            if (runner == null)
            {
                return;
            }

            runner.RemoveCallbacks(this);
            Destroy(runner.gameObject);
            runner = null;
            handingOffToGameplay = false;
            isConnecting = false;
        }

        private static void DestroyUnusedRunners()
        {
            foreach (var instance in NetworkRunner.Instances.ToArray())
            {
                if (instance != null && !instance.IsRunning)
                {
                    Destroy(instance.gameObject);
                }
            }
        }

        private void ShowSetupPanel()
        {
            isConnecting = false;
            setupPanel?.SetActive(true);
            sessionPanel?.SetActive(false);
            SetSetupButtonsInteractable(true);
        }

        private void ShowSessionPanel()
        {
            isConnecting = false;
            setupPanel?.SetActive(false);
            sessionPanel?.SetActive(true);
            SetSessionButtonsInteractable(true);
            UpdateSessionUI();
        }

        private void SetSetupButtonsInteractable(bool interactable)
        {
            if (hostButton != null) hostButton.interactable = interactable;
            if (joinButton != null) joinButton.interactable = interactable;
            if (backButton != null) backButton.interactable = interactable;
            if (sessionNameInput != null) sessionNameInput.interactable = interactable;
        }

        private void SetSessionButtonsInteractable(bool interactable)
        {
            if (startGameButton != null) startGameButton.interactable = interactable;
            if (leaveButton != null) leaveButton.interactable = interactable;
        }

        private void UpdateSessionUI()
        {
            if (runner == null || !runner.IsRunning)
            {
                return;
            }

            if (sessionCodeText != null)
            {
                sessionCodeText.text = $"Session Code: <color=yellow>{runner.SessionInfo.Name}</color>";
            }

            var playerCount = runner.ActivePlayers.Count();
            if (playerCountText != null)
            {
                playerCountText.text = $"{playerCount} / {SessionCapacity} Players";
            }

            var isHost = runner.IsSceneAuthority;
            var developmentSoloStart = allowSoloStartInDevelopment &&
                                       (Application.isEditor || Debug.isDebugBuild);
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(isHost);
                startGameButton.interactable = isHost &&
                                               (playerCount >= SessionCapacity || developmentSoloStart) &&
                                               !isLeaving;
            }
        }

        private static string GenerateSessionCode()
        {
            const string characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new char[5];
            for (var i = 0; i < code.Length; i++)
            {
                code[i] = characters[UnityEngine.Random.Range(0, characters.Length)];
            }

            return new string(code);
        }

        private void BackToMainMenuOrQuit()
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (!path.EndsWith("/MainMenu.unity", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetStatus(string message, bool error = false)
        {
            if (statusText != null)
            {
                statusText.text = error ? $"<color=#FF7777>{message}</color>" : message;
            }
        }

        public void OnPlayerJoined(NetworkRunner sessionRunner, PlayerRef player)
        {
            if (sessionRunner == runner) UpdateSessionUI();
        }

        public void OnPlayerLeft(NetworkRunner sessionRunner, PlayerRef player)
        {
            if (sessionRunner == runner) UpdateSessionUI();
        }

        public void OnShutdown(NetworkRunner sessionRunner, ShutdownReason reason)
        {
            if (sessionRunner != runner)
            {
                return;
            }

            var normalLeave = isLeaving || reason == ShutdownReason.Ok;
            CleanupRunner();
            ShowSetupPanel();
            SetStatus(normalLeave ? "Left the session." : $"Disconnected: {reason}.", !normalLeave);
            isLeaving = false;
        }

        public void OnDisconnectedFromServer(NetworkRunner sessionRunner, NetDisconnectReason reason)
        {
            if (sessionRunner != runner)
            {
                return;
            }

            CleanupRunner();
            ShowSetupPanel();
            SetStatus($"Disconnected: {reason}.", true);
        }

        public void OnConnectFailed(NetworkRunner sessionRunner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            if (sessionRunner != runner)
            {
                return;
            }

            CleanupRunner();
            ShowSetupPanel();
            SetStatus($"Connection failed: {reason}.", true);
        }

        public void OnSceneLoadStart(NetworkRunner sessionRunner)
        {
            if (sessionRunner == runner)
            {
                handingOffToGameplay = true;
                SetStatus("Loading Demo Level for the session...");
            }
        }

        public void OnSceneLoadDone(NetworkRunner sessionRunner) { }
        public void OnInput(NetworkRunner sessionRunner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner sessionRunner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner sessionRunner) { }
        public void OnConnectRequest(NetworkRunner sessionRunner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
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
