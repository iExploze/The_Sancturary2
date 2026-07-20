using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    public enum FusionPlayerButton
    {
        Jump,
        Sprint,
        Crouch,
        DebugDamage
    }

    public struct FusionPlayerInput : INetworkInput
    {
        public Vector2 Move;
        public Vector2 Look;
        public NetworkButtons Buttons;
    }
}
