using UnityEngine;

namespace TheSancturary.Monsters
{
    public static class HenryMonsterRules
    {
        public const float DistanceEpsilon = 0.01f;

        public static bool IsMeaningfullyMoving(Vector3 authoritativeVelocity, float movementThreshold)
        {
            float threshold = Mathf.Max(0f, movementThreshold);
            return authoritativeVelocity.sqrMagnitude >= threshold * threshold;
        }

        public static bool CanAcquire(
            bool isValid,
            bool isAlive,
            bool isHidden,
            bool isInRange,
            bool isInFieldOfView,
            bool hasLineOfSight,
            Vector3 authoritativeVelocity,
            float movementThreshold)
        {
            return isValid &&
                isAlive &&
                !isHidden &&
                isInRange &&
                isInFieldOfView &&
                hasLineOfSight &&
                IsMeaningfullyMoving(authoritativeVelocity, movementThreshold);
        }

        public static bool ShouldSelectCandidate(bool canAcquire, float candidateDistance, float currentBestDistance)
        {
            return canAcquire &&
                candidateDistance >= 0f &&
                candidateDistance + DistanceEpsilon < currentBestDistance;
        }

        public static bool CanMaintainChase(bool isValid, bool isAlive, bool isHidden)
        {
            return isValid && isAlive && !isHidden;
        }

        public static bool HasExceededLineOfSightGrace(bool hasLineOfSight, float occludedSeconds, float graceSeconds)
        {
            return !hasLineOfSight && occludedSeconds >= Mathf.Max(0f, graceSeconds);
        }

        public static float HealthAfterHits(float startingHealth, int damagePerHit, int hitCount)
        {
            if (startingHealth <= 0f || damagePerHit <= 0 || hitCount <= 0)
                return Mathf.Max(0f, startingHealth);
            return Mathf.Max(0f, startingHealth - damagePerHit * hitCount);
        }
    }
}
