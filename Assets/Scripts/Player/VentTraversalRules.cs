using System;
using System.Collections.Generic;

namespace TheSancturary.FusionPrototype
{
    public static class VentTraversalRules
    {
        public static bool CanEnter(bool isDeadOrPending, bool isInVent, bool isUsingLocker)
        {
            return !isDeadOrPending && !isInVent && !isUsingLocker;
        }

        public static bool CanExit(bool isDeadOrPending, bool isInVent, bool isUsingLocker)
        {
            return !isDeadOrPending && isInVent && !isUsingLocker;
        }

        public static bool CanSprint(bool isInVent, bool isCrouched, bool hasForwardInput, bool sprintLocked)
        {
            return !isInVent && !isCrouched && hasForwardInput && !sprintLocked;
        }

        public static bool CanJump(bool isInVent, bool isCrouched, bool isGrounded)
        {
            return !isInVent && !isCrouched && isGrounded;
        }
    }

    public static class VentDestinationSelector
    {
        public static bool TrySelectFirstClear<T>(
            IReadOnlyList<T> candidates,
            Predicate<T> isClear,
            out T selected)
        {
            if (candidates != null && isClear != null)
            {
                for (int index = 0; index < candidates.Count; index++)
                {
                    T candidate = candidates[index];
                    if (candidate is null || !isClear(candidate))
                        continue;

                    selected = candidate;
                    return true;
                }
            }

            selected = default;
            return false;
        }
    }
}
