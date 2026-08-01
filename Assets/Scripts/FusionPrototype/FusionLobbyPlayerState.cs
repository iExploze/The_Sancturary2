using Fusion;

namespace TheSancturary.FusionPrototype
{
    /// <summary>One host-authoritative lobby record. Its input authority identifies the represented player.</summary>
    public sealed class FusionLobbyPlayerState : NetworkBehaviour
    {
        [Networked] public int Slot { get; private set; }
        [Networked] public NetworkBool Ready { get; private set; }
        [Networked] public NetworkBool IsHost { get; private set; }
        [Networked] private NetworkButtons PreviousButtons { get; set; }
        public PlayerRef Player => Object.InputAuthority;

        public void SetSlotAuthoritative(int slot, bool isHost)
        {
            if (HasStateAuthority)
            {
                Slot = slot;
                IsHost = isHost;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !GetInput(out FusionPlayerInput input))
                return;

            NetworkButtons pressed = input.Buttons.GetPressed(PreviousButtons);
            PreviousButtons = input.Buttons;
            if (pressed.IsSet(FusionPlayerButton.LobbyReady))
                Ready = !Ready;
        }
    }
}
