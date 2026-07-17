using Unity.Netcode;
using UnityEngine;

namespace TheSancturary.Multiplayer
{
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class PersistentNetworkManager : MonoBehaviour
    {
        private static PersistentNetworkManager instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
