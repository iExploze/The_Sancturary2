using UnityEngine;
using UnityEngine.UI;

namespace TheSancturary.FusionPrototype
{
    /// <summary>Owner-only pause presentation. It intentionally never pauses Fusion or Unity time.</summary>
    public sealed class GameplayPauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button stopSessionButton;
        [SerializeField] private Button leaveSessionButton;
        [SerializeField] private Text statusText;

        private FusionNetworkPlayer _owner;
        public static GameplayPauseMenu Instance { get; private set; }
        public bool IsOpen => pausePanel != null && pausePanel.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (pausePanel == null || resumeButton == null || stopSessionButton == null || leaveSessionButton == null || statusText == null)
            {
                Debug.LogError("Pause menu has missing serialized UI references.", this);
                return;
            }

            pausePanel.SetActive(false);
            resumeButton.onClick.AddListener(Resume);
            stopSessionButton.onClick.AddListener(StopSession);
            leaveSessionButton.onClick.AddListener(LeaveSession);
        }

        public void Toggle(FusionNetworkPlayer owner)
        {
            if (owner == null)
                return;
            if (IsOpen)
                Resume();
            else
                Open(owner);
        }

        private void Open(FusionNetworkPlayer owner)
        {
            _owner = owner;
            FusionSessionManager manager = FusionSessionManager.Instance;
            bool host = manager != null && manager.IsHost;
            stopSessionButton.gameObject.SetActive(host);
            leaveSessionButton.gameObject.SetActive(!host);
            statusText.text = host ? "Stopping the session returns everyone to the main menu." : "Leaving returns only you to the main menu.";
            pausePanel.SetActive(true);
            _owner.SetLocalPauseInputBlocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Resume()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (_owner != null)
                _owner.SetLocalPauseInputBlocked(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void StopSession() => FusionSessionManager.Instance?.StopSessionAndReturnToMenu();
        private void LeaveSession() => FusionSessionManager.Instance?.LeaveSessionAndReturnToMenu();

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
