using Fusion;

namespace TheSancturary.Inventory
{
    public struct NetworkInventoryEntry : INetworkStruct
    {
        public NetworkBool IsOccupied;
        public ushort InstanceId;
        public NetworkString<_32> ItemId;
        public byte Column;
        public byte Row;
        public NetworkBool Rotated;
        public byte LoadedAmmunition;
    }

    public enum InventoryRequestRejection : byte
    {
        None = 0,
        InventoryFull = 1,
        ItemTaken = 3,
        InvalidRequest = 4
    }
}
