using Fusion;

namespace TheSancturary.FusionPrototype
{
    /// <summary>One host-authoritative lobby record. Its input authority identifies the represented player.</summary>
    public sealed class FusionLobbyPlayerState : NetworkBehaviour
    {
        [Networked] public int Slot { get; private set; }
        [Networked] public NetworkBool Ready { get; private set; }
        [Networked] public NetworkBool IsHost { get; private set; }
        public PlayerRef Player => Object.InputAuthority;

        public void SetSlotAuthoritative(int slot, bool isHost)
        {
            if (HasStateAuthority)
            {
                Slot = slot;
                IsHost = isHost;
            }
        }

        public void ToggleReady()
        {
            if (HasInputAuthority)
                RPC_SetReady(!Ready);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetReady(NetworkBool ready)
        {
            Ready = ready;
        }
    }
}
