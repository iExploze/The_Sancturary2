using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheSancturary.FusionPrototype
{
    public sealed class DebugSessionBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkObject canonicalPlayerPrefab;
        [SerializeField] private NetworkObject lobbyPlayerStatePrefab;

        private async void Start()
        {
            await System.Threading.Tasks.Task.Yield();
            if (FusionSessionManager.HasActiveRunner)
                return;

            FusionSessionManager manager = FusionSessionManager.GetOrCreate(
                canonicalPlayerPrefab,
                lobbyPlayerStatePrefab);
            await manager.StartDirectDebugAsync(SceneManager.GetActiveScene().path);
        }
    }
}
