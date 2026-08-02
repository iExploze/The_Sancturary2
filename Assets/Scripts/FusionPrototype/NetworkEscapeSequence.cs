using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>Runs the escape-screen countdown on state authority and returns the session to its lobby.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkEscapeSequence : NetworkBehaviour
    {
        [SerializeField, Min(0.1f)] private float returnDelaySeconds = 5f;

        [Networked] public TickTimer ReturnTimer { get; private set; }

        private bool _returnRequested;

        public override void Spawned()
        {
            if (HasStateAuthority)
                ReturnTimer = TickTimer.CreateFromSeconds(Runner, returnDelaySeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _returnRequested || !ReturnTimer.Expired(Runner))
                return;

            FusionSessionManager manager = FusionSessionManager.Instance;
            if (manager == null || !manager.TryReturnToLobbyAuthoritative(Runner))
                return;

            _returnRequested = true;
        }

        public void Configure(float delaySeconds)
        {
            returnDelaySeconds = Mathf.Max(0.1f, delaySeconds);
        }
    }
}
