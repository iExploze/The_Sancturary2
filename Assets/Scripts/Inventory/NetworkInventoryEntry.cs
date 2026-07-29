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
    }

    public enum InventoryRequestRejection : byte
    {
        None,
        InventoryFull,
        CategoryLimitReached,
        ItemTaken,
        InvalidRequest
    }
}
