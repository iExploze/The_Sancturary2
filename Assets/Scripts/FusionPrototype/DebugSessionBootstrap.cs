using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    public sealed class DebugSessionBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkObject canonicalPlayerPrefab;

        private async void Start()
        {
            await System.Threading.Tasks.Task.Yield();
            if (FusionSessionManager.HasActiveRunner)
                return;

            FusionSessionManager manager = FusionSessionManager.GetOrCreate(canonicalPlayerPrefab);
            await manager.StartDirectDebugAsync();
        }
    }
}
