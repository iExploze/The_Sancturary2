using System;
using Unity.Netcode;

namespace TheSancturary.Player
{
    public struct PickupNetworkState : INetworkSerializable, IEquatable<PickupNetworkState>
    {
        public PickupNetworkState(bool isHeld, NetworkObjectReference holder)
        {
            IsHeld = isHeld;
            Holder = holder;
        }

        public bool IsHeld;
        public NetworkObjectReference Holder;

        public static PickupNetworkState Released => new(false, new NetworkObjectReference((NetworkObject)null));

        public static PickupNetworkState HeldBy(NetworkObject holder)
        {
            return new PickupNetworkState(true, new NetworkObjectReference(holder));
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref IsHeld);
            serializer.SerializeValue(ref Holder);
        }

        public bool Equals(PickupNetworkState other)
        {
            return IsHeld == other.IsHeld && Holder.Equals(other.Holder);
        }

        public override bool Equals(object obj)
        {
            return obj is PickupNetworkState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsHeld, Holder);
        }
    }
}
