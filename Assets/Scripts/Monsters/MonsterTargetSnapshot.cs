using UnityEngine;

namespace TheSancturary.Monsters
{
    public readonly struct MonsterTargetSnapshot
    {
        public MonsterTargetSnapshot(ulong clientId, Vector3 position, bool isValid = true)
        {
            ClientId = clientId;
            Position = position;
            IsValid = isValid;
        }

        public ulong ClientId { get; }

        public Vector3 Position { get; }

        public bool IsValid { get; }
    }
}
