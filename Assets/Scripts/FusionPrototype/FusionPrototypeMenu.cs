using Fusion;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TheSancturary.FusionPrototype
{
    /// <summary>Scene-authored launch screen for the prototype Fusion flow.</summary>
    public sealed class FusionPrototypeMenu : MonoBehaviour
    {
        [SerializeField] private NetworkObject canonicalPlayerPrefab;
        [SerializeField] private NetworkObject lobbyPlayerStatePrefab;
#if UNITY_EDITOR
        [SerializeField, Tooltip("Scene the host loads after every connected player is ready. It must be enabled in Build Settings.")]
        private SceneAsset gameplayScene;
#endif
        [SerializeField, HideInInspector] private string gameplayScenePath = FusionSessionManager.GameplayScenePath;
        [SerializeField] private InputField roomNameInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Text statusText;

        private FusionSessionManager _sessionManager;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (!HasRequiredReferences())
                return;

            roomNameInput.characterLimit = FusionSessionManager.MaximumSessionNameLength;
            _sessionManager = FusionSessionManager.GetOrCreate(canonicalPlayerPrefab, lobbyPlayerStatePrefab);
            _sessionManager.ConfigureGameplayScene(gameplayScenePath);
            _sessionManager.StatusChanged += OnStatusChanged;
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);

            OnStatusChanged(
                string.IsNullOrWhiteSpace(_sessionManager.LastStatus)
                    ? FusionSessionManager.HasFusionAppId
                        ? "Enter a room name, then Create Room or Join Room."
                        : "Photon Fusion App ID is missing. Configure it in Tools > Fusion > Fusion Hub."
                    : _sessionManager.LastStatus,
                !FusionSessionManager.HasFusionAppId || _sessionManager.LastStatusIsError);
        }

        private bool HasRequiredReferences()
        {
            if (canonicalPlayerPrefab != null && lobbyPlayerStatePrefab != null &&
                roomNameInput != null && createRoomButton != null &&
                joinRoomButton != null && statusText != null)
                return true;

            Debug.LogError("Fusion prototype menu is missing serialized UI or network prefab references.", this);
            if (statusText != null)
                statusText.text = "Menu setup is incomplete. Check serialized references.";
            return false;
        }

        private async void CreateRoom()
        {
            SetButtonsInteractable(false);
            if (!await _sessionManager.StartHostAsync(roomNameInput.text))
                SetButtonsInteractable(true);
        }

        private async void JoinRoom()
        {
            SetButtonsInteractable(false);
            if (!await _sessionManager.StartClientAsync(roomNameInput.text))
                SetButtonsInteractable(true);
        }

        private void OnStatusChanged(string message, bool isError)
        {
            if (statusText == null)
                return;

            statusText.text = message;
            statusText.color = isError ? new Color(1f, 0.35f, 0.32f) : new Color(0.8f, 0.86f, 0.9f);
        }

        private void SetButtonsInteractable(bool value)
        {
            createRoomButton.interactable = value;
            joinRoomButton.interactable = value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (gameplayScene != null)
                gameplayScenePath = AssetDatabase.GetAssetPath(gameplayScene);
        }
#endif

        private void OnDestroy()
        {
            if (_sessionManager == null)
                return;

            _sessionManager.StatusChanged -= OnStatusChanged;
            if (createRoomButton != null)
                createRoomButton.onClick.RemoveListener(CreateRoom);
            if (joinRoomButton != null)
                joinRoomButton.onClick.RemoveListener(JoinRoom);
        }
    }
}
