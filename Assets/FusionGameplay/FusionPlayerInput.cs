using Fusion;
using UnityEngine;

namespace TheSancturary.Player
{
    public enum PlayerInputButton
    {
        Jump,
        Sprint,
        Crouch,
        Flashlight
    }

    /// <summary>Per-tick local input submitted to Fusion for prediction and authority validation.</summary>
    public struct FusionPlayerInput : INetworkInput
    {
        public Vector2 MoveDirection;
        public Vector2 LookRotationDelta;
        public NetworkButtons Buttons;
    }
}
