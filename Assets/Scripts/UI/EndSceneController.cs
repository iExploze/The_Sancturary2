using TheSancturary.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheSancturary.UI
{
    public sealed class EndSceneController : MonoBehaviour
    {
        [SerializeField] private Button returnToMenuButton;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        private void OnDestroy()
        {
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }
        }

        public void ReturnToMainMenu()
        {
            var sessionController = FindFirstObjectByType<NetworkSessionController>();
            if (sessionController != null)
            {
                sessionController.DisconnectAndReturnToMenu();
                return;
            }

            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            SceneManager.LoadScene(NetworkConstants.MainMenuSceneName, LoadSceneMode.Single);
        }
    }
}
