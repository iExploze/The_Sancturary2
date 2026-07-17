using System.Collections.Generic;
using UnityEngine;

namespace TheSancturary.Monsters
{
    public static class MonsterTargetSelector
    {
        public static bool TrySelectNearest(
            Vector3 origin,
            IReadOnlyList<MonsterTargetSnapshot> candidates,
            float detectionRange,
            out MonsterTargetSnapshot nearest)
        {
            nearest = default;
            var nearestSqrDistance = detectionRange * detectionRange;
            var foundTarget = false;

            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (!candidate.IsValid)
                {
                    continue;
                }

                var sqrDistance = (candidate.Position - origin).sqrMagnitude;
                if (sqrDistance > nearestSqrDistance)
                {
                    continue;
                }

                if (!foundTarget || sqrDistance < nearestSqrDistance)
                {
                    nearest = candidate;
                    nearestSqrDistance = sqrDistance;
                    foundTarget = true;
                }
            }

            return foundTarget;
        }
    }
}
