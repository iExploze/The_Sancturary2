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
        UseEquipped,
        LobbyReady,
        DebugExhaustion,
        Aim,
        Reload
    }

    public enum InventoryInputCommandType : byte
    {
        None,
        Move,
        Equip,
        Drop,
        Reload
    }

    public struct InventoryInputCommand
    {
        public InventoryInputCommandType Type;
        public ushort InstanceId;
        public ushort TargetInstanceId;
        public byte Column;
        public byte Row;
        public NetworkBool Rotated;
    }

    public struct FusionPlayerInput : INetworkInput
    {
        public Vector2 Move;
        public Vector2 LookAngles;
        public NetworkBehaviourId InteractionTarget;
        public byte InteractionCommandSequence;
        public byte InventoryCommand;
        public byte InventoryCommandSequence;
        public ushort InventoryInstanceId;
        public ushort InventoryTargetInstanceId;
        public byte InventoryColumn;
        public byte InventoryRow;
        public NetworkBool InventoryRotated;
        public NetworkButtons Buttons;
    }
}
