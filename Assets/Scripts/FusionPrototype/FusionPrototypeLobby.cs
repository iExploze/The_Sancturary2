using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TheSancturary.FusionPrototype
{
    /// <summary>Scene-authored lobby presentation. Network state remains in FusionLobbyPlayerState objects.</summary>
    public sealed class FusionPrototypeLobby : MonoBehaviour
    {
        [SerializeField] private Text roomNameText;
        [SerializeField] private Text[] playerSlotTexts;
        [SerializeField] private Button readyButton;
        [SerializeField] private Text readyButtonText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveLobbyButton;
        [SerializeField] private Text statusText;

        private FusionSessionManager _sessionManager;
        private bool _boundToSession;

        private void Awake()
        {
            if (roomNameText == null || playerSlotTexts == null || playerSlotTexts.Length != FusionSessionManager.MaximumPlayers ||
                readyButton == null || readyButtonText == null || startGameButton == null || leaveLobbyButton == null || statusText == null)
            {
                Debug.LogError("Fusion lobby has missing serialized UI references.", this);
                if (statusText != null)
                    statusText.text = "Lobby setup is incomplete. Check serialized references.";
                return;
            }

            readyButton.onClick.AddListener(ToggleReady);
            startGameButton.onClick.AddListener(RequestStartGame);
            leaveLobbyButton.onClick.AddListener(LeaveLobby);
            TryBindSession();
        }

        private void Update()
        {
            TryBindSession();
            if (_sessionManager == null)
                return;

            var states = _sessionManager.GetLobbyPlayers();
            for (int slot = 1; slot <= FusionSessionManager.MaximumPlayers; slot++)
            {
                FusionLobbyPlayerState state = states.FirstOrDefault(player => player.Slot == slot);
                playerSlotTexts[slot - 1].text = state == null
                    ? $"Player {slot} — Empty"
                    : $"Player {slot}{(state.IsHost ? " (Host)" : string.Empty)} — {(state.Ready ? "Ready" : "Not Ready")}";
            }

            FusionLobbyPlayerState local = states.FirstOrDefault(player => player.Object.HasInputAuthority);
            readyButton.interactable = local != null && !_sessionManager.IsGameplayLoading;
            readyButtonText.text = local != null && local.Ready ? "Not Ready" : "Ready";
            startGameButton.gameObject.SetActive(_sessionManager.IsHost);
            startGameButton.interactable = _sessionManager.CanStartGame;
            leaveLobbyButton.interactable = !_sessionManager.IsGameplayLoading;
        }

        private void TryBindSession()
        {
            if (_boundToSession || FusionSessionManager.Instance == null)
                return;

            _sessionManager = FusionSessionManager.Instance;
            _sessionManager.StatusChanged += OnStatusChanged;
            roomNameText.text = _sessionManager.Runner?.SessionInfo.Name ?? "Room";
            _boundToSession = true;
        }

        private void ToggleReady()
        {
            FusionLobbyPlayerState local = _sessionManager?.GetLobbyPlayers().FirstOrDefault(player => player.Object.HasInputAuthority);
            local?.ToggleReady();
        }

        private void RequestStartGame() => _sessionManager?.RequestStartGame();

        private void LeaveLobby() => _sessionManager?.LeaveSessionAndReturnToMenu();

        private void OnStatusChanged(string message, bool isError)
        {
            statusText.text = message;
            statusText.color = isError ? new Color(1f, 0.35f, 0.32f) : new Color(0.8f, 0.86f, 0.9f);
        }

        private void OnDestroy()
        {
            if (_sessionManager == null)
                return;
            if (_boundToSession)
                _sessionManager.StatusChanged -= OnStatusChanged;
            readyButton.onClick.RemoveListener(ToggleReady);
            startGameButton.onClick.RemoveListener(RequestStartGame);
            leaveLobbyButton.onClick.RemoveListener(LeaveLobby);
        }
    }
}
