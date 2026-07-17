using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TheSancturary.Multiplayer
{
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkSessionController : MonoBehaviour
    {
        private NetworkManager networkManager;
        private bool returningToMenu;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
        }

        private void OnEnable()
        {
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void Update()
        {
            if (Keyboard.current?.f10Key.wasPressedThisFrame == true && networkManager.IsListening)
            {
                DisconnectAndReturnToMenu();
            }
        }

        public void DisconnectAndReturnToMenu()
        {
            if (returningToMenu)
            {
                return;
            }

            returningToMenu = true;
            if (networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(NetworkConstants.MainMenuSceneName, LoadSceneMode.Single);
            returningToMenu = false;
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!networkManager.IsServer && clientId == networkManager.LocalClientId)
            {
                DisconnectAndReturnToMenu();
            }
        }
    }
}
