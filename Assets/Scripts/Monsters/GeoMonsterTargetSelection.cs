namespace TheSancturary.Monsters
{
    public static class GeoMonsterTargetSelection
    {
        public const float DistanceEpsilon = 0.01f;

        public static bool ShouldSelectCandidate(
            bool isValid,
            bool isAlive,
            bool isVisible,
            float candidateDistance,
            float currentBestDistance)
        {
            return isValid &&
                isAlive &&
                isVisible &&
                candidateDistance >= 0f &&
                candidateDistance + DistanceEpsilon < currentBestDistance;
        }
    }
}
