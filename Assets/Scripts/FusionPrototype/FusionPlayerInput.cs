using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    public enum FusionPlayerButton
    {
        Jump,
        Sprint,
        Crouch,
        Interact,
        DebugExhaustion
    }

    public struct FusionPlayerInput : INetworkInput
    {
        public Vector2 Move;
        public Vector2 LookAngles;
        public NetworkBehaviourId InteractionTarget;
        public NetworkButtons Buttons;
    }
}
