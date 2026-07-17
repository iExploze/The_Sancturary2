namespace TheSancturary.Multiplayer
{
    public static class ExitEligibility
    {
        public static bool IsEligible(bool isSpawned, bool isPlayerObject)
        {
            return isSpawned && isPlayerObject;
        }
    }
}
