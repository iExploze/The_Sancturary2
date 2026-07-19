using System.Collections.Generic;
using UnityEngine;

namespace TheSancturary.Player
{
    public readonly struct PlayerAvatarPresence
    {
        public PlayerAvatarPresence(Transform transform, bool isLocallyOwned)
        {
            Transform = transform;
            IsLocallyOwned = isLocallyOwned;
        }

        public Transform Transform { get; }
        public bool IsLocallyOwned { get; }
        public bool IsSpawned => Transform != null;
    }

    public static class PlayerAvatarRegistry
    {
        private static readonly Dictionary<ulong, PlayerAvatarPresence> activePlayers = new();

        public static IReadOnlyDictionary<ulong, PlayerAvatarPresence> ActivePlayers => activePlayers;

        public static void Register(ulong playerId, Transform transform, bool isLocallyOwned)
        {
            activePlayers[playerId] = new PlayerAvatarPresence(transform, isLocallyOwned);
        }

        public static void Unregister(ulong playerId, Transform expectedTransform)
        {
            if (activePlayers.TryGetValue(playerId, out var player) && player.Transform == expectedTransform)
            {
                activePlayers.Remove(playerId);
            }
        }
    }
}
