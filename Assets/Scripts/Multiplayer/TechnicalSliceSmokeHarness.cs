using System;
using System.Collections;
using System.Linq;
using TheSancturary.Monsters;
using TheSancturary.Player;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheSancturary.Multiplayer
{
    [RequireComponent(typeof(NetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    public sealed class TechnicalSliceSmokeHarness : MonoBehaviour
    {
        private const string RoleArgument = "-sancturarySmokeRole";
        private const string PortArgument = "-sancturarySmokePort";
        private const float TimeoutSeconds = 30f;

        private NetworkManager networkManager;
        private UnityTransport transport;

        private IEnumerator Start()
        {
            if (!TryGetArgument(RoleArgument, out var role))
            {
                yield break;
            }

            Application.runInBackground = true;
            networkManager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            var port = ReadPort();

            if (string.Equals(role, "host", StringComparison.OrdinalIgnoreCase))
            {
                yield return RunHost(port);
                yield break;
            }

            if (string.Equals(role, "client", StringComparison.OrdinalIgnoreCase))
            {
                yield return RunClient(port);
                yield break;
            }

            Fail($"Unknown smoke role '{role}'.");
        }

        private IEnumerator RunHost(ushort port)
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            if (!networkManager.StartHost())
            {
                Fail("Host failed to start.");
                yield break;
            }

            var loadStatus = networkManager.SceneManager.LoadScene(
                NetworkConstants.GrayboxSceneName,
                LoadSceneMode.Single);
            Debug.Log($"[TechnicalSliceSmoke] Host scene load status: {loadStatus}");

            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var playerCount = NetworkPlayerController.ActivePlayers.Count;
                var hasMonster = FindFirstObjectByType<MonsterController>() != null;
                if (networkManager.ConnectedClientsIds.Count >= 2 && playerCount >= 2 && hasMonster)
                {
                    Debug.Log("TECHNICAL_SLICE_HOST_SMOKE_SUCCESS players=2 monster=1");
                    yield return new WaitForSecondsRealtime(1f);
                    networkManager.Shutdown();
                    Application.Quit(0);
                    yield break;
                }

                yield return null;
            }

            Fail($"Host timed out. clients={networkManager.ConnectedClientsIds.Count} players={NetworkPlayerController.ActivePlayers.Count}");
        }

        private IEnumerator RunClient(ushort port)
        {
            yield return new WaitForSecondsRealtime(1f);
            transport.SetConnectionData("127.0.0.1", port);
            if (!networkManager.StartClient())
            {
                Fail("Client failed to start.");
                yield break;
            }

            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var hasOwnedPlayer = NetworkPlayerController.ActivePlayers.Values.Any(player => player.IsOwner);
                var hasMonster = FindFirstObjectByType<MonsterController>() != null;
                if (networkManager.IsConnectedClient &&
                    SceneManager.GetActiveScene().name == NetworkConstants.GrayboxSceneName &&
                    hasOwnedPlayer &&
                    hasMonster)
                {
                    Debug.Log("TECHNICAL_SLICE_CLIENT_SMOKE_SUCCESS ownedPlayer=1 monster=1");
                    yield return new WaitForSecondsRealtime(1f);
                    networkManager.Shutdown();
                    Application.Quit(0);
                    yield break;
                }

                yield return null;
            }

            Fail($"Client timed out. connected={networkManager.IsConnectedClient} scene={SceneManager.GetActiveScene().name}");
        }

        private ushort ReadPort()
        {
            return TryGetArgument(PortArgument, out var value) &&
                   ushort.TryParse(value, out var port) &&
                   port > 0
                ? port
                : NetworkConstants.DefaultPort;
        }

        private static bool TryGetArgument(string argumentName, out string value)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    value = arguments[index + 1];
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }

        private static void Fail(string reason)
        {
            Debug.LogError($"TECHNICAL_SLICE_SMOKE_FAILURE {reason}");
            Application.Quit(1);
        }
    }
}
