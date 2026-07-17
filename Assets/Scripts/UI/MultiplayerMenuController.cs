using System;
using TheSancturary.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheSancturary.UI
{
    public sealed class MultiplayerMenuController : MonoBehaviour
    {
        [SerializeField] private InputField addressInput;
        [SerializeField] private InputField portInput;
        [SerializeField] private Text statusText;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button disconnectButton;

        private NetworkManager networkManager;
        private UnityTransport transport;

        private void Awake()
        {
            networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                SetStatus("NetworkManager is unavailable.");
                SetButtons(false);
                return;
            }

            transport = networkManager.GetComponent<UnityTransport>();
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            hostButton.onClick.AddListener(Host);
            joinButton.onClick.AddListener(Join);
            disconnectButton.onClick.AddListener(Disconnect);
            RefreshButtons();
            SetStatus("Ready for direct-IP host or join.");
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            hostButton?.onClick.RemoveListener(Host);
            joinButton?.onClick.RemoveListener(Join);
            disconnectButton?.onClick.RemoveListener(Disconnect);
        }

        public void Host()
        {
            if (!TryReadEndpoint(out var address, out var port))
            {
                return;
            }

            transport.SetConnectionData(address, port, "0.0.0.0");
            SetStatus($"Starting host on port {port}...");
            if (!networkManager.StartHost())
            {
                SetStatus("Host startup failed. See the player log for details.");
                RefreshButtons();
                return;
            }

            var loadStatus = networkManager.SceneManager.LoadScene(
                NetworkConstants.GrayboxSceneName,
                LoadSceneMode.Single);
            SetStatus($"Host started. Loading graybox ({loadStatus}).");
            RefreshButtons();
        }

        public void Join()
        {
            if (!TryReadEndpoint(out var address, out var port))
            {
                return;
            }

            transport.SetConnectionData(address, port);
            SetStatus($"Connecting to {address}:{port}...");
            if (!networkManager.StartClient())
            {
                SetStatus("Client startup failed. See the player log for details.");
            }

            RefreshButtons();
        }

        public void Disconnect()
        {
            var sessionController = networkManager != null
                ? networkManager.GetComponent<NetworkSessionController>()
                : null;
            if (sessionController != null)
            {
                sessionController.DisconnectAndReturnToMenu();
            }
        }

        public void Quit()
        {
            Application.Quit();
        }

        private bool TryReadEndpoint(out string address, out ushort port)
        {
            address = string.IsNullOrWhiteSpace(addressInput.text)
                ? "127.0.0.1"
                : addressInput.text.Trim();

            if (!ushort.TryParse(portInput.text, out port) || port == 0)
            {
                SetStatus("Enter a port from 1 to 65535.");
                return false;
            }

            return true;
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (clientId != networkManager.LocalClientId)
            {
                return;
            }

            SetStatus(networkManager.IsHost ? "Hosting." : "Connected. Synchronizing scene...");
            RefreshButtons();
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (clientId != networkManager.LocalClientId)
            {
                return;
            }

            var reason = string.IsNullOrWhiteSpace(networkManager.DisconnectReason)
                ? "Disconnected."
                : $"Disconnected: {networkManager.DisconnectReason}";
            SetStatus(reason);
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            SetButtons(networkManager != null && !networkManager.IsListening);
            if (disconnectButton != null)
            {
                disconnectButton.interactable = networkManager != null && networkManager.IsListening;
            }
        }

        private void SetButtons(bool canStart)
        {
            if (hostButton != null)
            {
                hostButton.interactable = canStart;
            }

            if (joinButton != null)
            {
                joinButton.interactable = canStart;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log($"[MultiplayerMenu] {message}");
        }
    }
}
