namespace TheSancturary.Player
{
    public static class PickupReleaseRules
    {
        public static bool ShouldReleaseAfterDisconnect(
            bool isHeld,
            bool holderResolved,
            ulong holderClientId,
            ulong disconnectedClientId)
        {
            return isHeld && holderResolved && holderClientId == disconnectedClientId;
        }
    }
}
