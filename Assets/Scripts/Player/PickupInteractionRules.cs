namespace TheSancturary.Player
{
    public static class PickupInteractionRules
    {
        public static bool CanPickUp(
            bool requesterAlreadyHolding,
            bool targetIsHeld,
            bool targetIsConfigured,
            bool hasLineOfSight,
            float distance,
            float maximumDistance)
        {
            return !requesterAlreadyHolding &&
                   !targetIsHeld &&
                   targetIsConfigured &&
                   hasLineOfSight &&
                   distance >= 0f &&
                   distance <= maximumDistance;
        }
    }
}
