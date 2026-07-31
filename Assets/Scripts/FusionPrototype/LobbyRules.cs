using System.Collections.Generic;
using System.Linq;
using Fusion;

namespace TheSancturary.FusionPrototype
{
    public static class LobbyRules
    {
        public static bool AreAllReady(IEnumerable<PlayerRef> players, IEnumerable<FusionLobbyPlayerState> states)
        {
            if (players == null || states == null)
                return false;
            HashSet<PlayerRef> connected = players.ToHashSet();
            if (connected.Count == 0)
                return false;
            Dictionary<PlayerRef, FusionLobbyPlayerState> byPlayer = states.Where(state => state != null).ToDictionary(state => state.Player, state => state);
            return connected.All(player => byPlayer.TryGetValue(player, out FusionLobbyPlayerState state) && state.Ready);
        }

        public static bool AreAllReady(IEnumerable<PlayerRef> players, IEnumerable<PlayerRef> readyPlayers)
        {
            if (players == null || readyPlayers == null)
                return false;
            HashSet<PlayerRef> connected = players.ToHashSet();
            return connected.Count > 0 && connected.All(readyPlayers.Contains);
        }

        public static bool IsHostControlVisible(bool isHost) => isHost;

        public static int LowestAvailableSlot(IEnumerable<int> occupiedSlots, int maximumSlots)
        {
            HashSet<int> occupied = occupiedSlots?.Where(slot => slot > 0).ToHashSet() ?? new HashSet<int>();
            for (int slot = 1; slot <= maximumSlots; slot++)
                if (!occupied.Contains(slot))
                    return slot;
            return -1;
        }
    }
}
