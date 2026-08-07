using System;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Pure rules for the provisional authored items. Network behaviours remain
    /// responsible for ownership, timing, raycasts, and replicated state changes.
    /// </summary>
    public static class ItemGameplayRules
    {
        public const string CrowbarItemId = "crowbar";
        public const string FireAxeItemId = "fire_axe";
        public const float AdrenalineDurationSeconds = 30f;
        public const float RevivalMaximumDistance = 2f;
        public const float RevivalHealthFraction = 0.5f;

        public readonly struct ReloadTransition
        {
            public ReloadTransition(
                ushort consumedSourceInstanceId,
                ushort targetInstanceId,
                byte loadedAmmunition)
            {
                ConsumedSourceInstanceId = consumedSourceInstanceId;
                TargetInstanceId = targetInstanceId;
                LoadedAmmunition = loadedAmmunition;
            }

            public ushort ConsumedSourceInstanceId { get; }
            public ushort TargetInstanceId { get; }
            public byte LoadedAmmunition { get; }
            public int ConsumedInstanceCount =>
                ConsumedSourceInstanceId == 0 ? 0 : 1;
        }

        public readonly struct MedKitUseTransition
        {
            public MedKitUseTransition(float health)
            {
                Health = health;
                _applied = true;
            }

            private readonly bool _applied;
            public float Health { get; }
            public int PendingDamage => 0;
            public float TimeSinceDamage => 0f;
            public int ConsumedInstanceCount => _applied ? 1 : 0;
        }

        public readonly struct AdrenalineUseTransition
        {
            public AdrenalineUseTransition(bool applied)
            {
                _applied = applied;
            }

            private readonly bool _applied;
            public float DurationSeconds =>
                _applied ? AdrenalineDurationSeconds : 0f;
            public int ConsumedInstanceCount => _applied ? 1 : 0;
        }

        public readonly struct RevivalUseTransition
        {
            public RevivalUseTransition(float health, float stamina)
            {
                Health = health;
                Stamina = stamina;
                _applied = true;
            }

            private readonly bool _applied;
            public float Health { get; }
            public float Stamina { get; }
            public bool IsDead => false;
            public bool ClearRespawnTimer => _applied;
            public int PendingDamage => 0;
            public float TimeSinceDamage => 0f;
            public int ConsumedInstanceCount => _applied ? 1 : 0;
        }

        public readonly struct ToolObstacleState
        {
            public ToolObstacleState(byte acceptedHits, bool isCompleted)
            {
                AcceptedHits = acceptedHits;
                IsCompleted = isCompleted;
            }

            public byte AcceptedHits { get; }
            public bool IsCompleted { get; }
        }

        public static byte ClampLoadedAmmunition(int loadedRounds, int capacity)
        {
            int clampedCapacity = ClampToByte(capacity);
            return (byte)Math.Min(Math.Max(loadedRounds, 0), clampedCapacity);
        }

        public static bool IsCompatibleAmmunition(
            string ammunitionItemId,
            string compatibleAmmunitionItemId)
        {
            string ammunition = NormalizeItemId(ammunitionItemId);
            string compatible = NormalizeItemId(compatibleAmmunitionItemId);
            return ammunition.Length > 0 &&
                   compatible.Length > 0 &&
                   string.Equals(ammunition, compatible, StringComparison.Ordinal);
        }

        public static bool CanReload(
            string ammunitionItemId,
            string compatibleAmmunitionItemId,
            int capacity)
        {
            return capacity > 0 &&
                   capacity <= byte.MaxValue &&
                   IsCompatibleAmmunition(
                       ammunitionItemId,
                       compatibleAmmunitionItemId);
        }

        public static bool TryApplyReload(
            ushort sourceInstanceId,
            bool sourceExists,
            bool sourceIsAmmunition,
            string sourceItemId,
            ushort targetInstanceId,
            bool targetExists,
            bool targetIsFirearm,
            string compatibleAmmunitionItemId,
            int currentLoadedRounds,
            int capacity,
            out ReloadTransition transition)
        {
            transition = default;
            if (!sourceExists ||
                !targetExists ||
                !sourceIsAmmunition ||
                !targetIsFirearm ||
                sourceInstanceId == 0 ||
                targetInstanceId == 0 ||
                sourceInstanceId == targetInstanceId ||
                !CanReload(
                    sourceItemId,
                    compatibleAmmunitionItemId,
                    capacity))
                return false;

            transition = new ReloadTransition(
                sourceInstanceId,
                targetInstanceId,
                ApplyReload(currentLoadedRounds, capacity));
            return true;
        }

        /// <summary>
        /// Reloading deliberately discards any rounds already loaded and fills the
        /// firearm to its configured capacity.
        /// </summary>
        public static byte ApplyReload(int currentLoadedRounds, int capacity)
        {
            // The old value is intentionally discarded by the authored reload rule.
            return ClampLoadedAmmunition(capacity, capacity);
        }

        public static byte CaptureLoadedAmmunitionForWorld(
            int loadedRounds,
            int capacity)
        {
            return ClampLoadedAmmunition(loadedRounds, capacity);
        }

        public static byte RestoreLoadedAmmunitionFromWorld(
            int loadedRounds,
            int capacity)
        {
            return ClampLoadedAmmunition(loadedRounds, capacity);
        }

        public static bool TryFireRound(
            int loadedRounds,
            int capacity,
            out byte remainingRounds)
        {
            byte clampedLoaded = ClampLoadedAmmunition(loadedRounds, capacity);
            if (clampedLoaded == 0)
            {
                remainingRounds = 0;
                return false;
            }

            remainingRounds = (byte)(clampedLoaded - 1);
            return true;
        }

        public static bool CanUseMedKit(
            bool isAlive,
            float currentHealth,
            float maximumHealth)
        {
            return isAlive &&
                   IsFinite(currentHealth) &&
                   IsFinite(maximumHealth) &&
                   maximumHealth > 0f &&
                   currentHealth < maximumHealth;
        }

        public static float ApplyFullHeal(float maximumHealth)
        {
            return IsFinite(maximumHealth) ? Math.Max(0f, maximumHealth) : 0f;
        }

        public static bool TryApplyMedKitUse(
            bool isAlive,
            float currentHealth,
            float maximumHealth,
            out MedKitUseTransition transition)
        {
            transition = default;
            if (!CanUseMedKit(isAlive, currentHealth, maximumHealth))
                return false;

            transition = new MedKitUseTransition(
                ApplyFullHeal(maximumHealth));
            return true;
        }

        public static bool CanApplyAdrenaline(bool isAlive, bool isAlreadyActive)
        {
            return isAlive && !isAlreadyActive;
        }

        public static bool TryStartAdrenaline(
            bool isAlive,
            bool isAlreadyActive,
            out AdrenalineUseTransition transition)
        {
            transition = default;
            if (!CanApplyAdrenaline(isAlive, isAlreadyActive))
                return false;

            transition = new AdrenalineUseTransition(true);
            return true;
        }

        public static bool IsAdrenalineActive(float remainingSeconds)
        {
            return IsFinite(remainingSeconds) && remainingSeconds > 0f;
        }

        public static float ResolveStamina(
            float currentStamina,
            float maximumStamina,
            bool adrenalineActive)
        {
            float maximum = IsFinite(maximumStamina)
                ? Math.Max(0f, maximumStamina)
                : 0f;
            if (adrenalineActive)
                return maximum;

            if (!IsFinite(currentStamina))
                return 0f;

            return Math.Min(Math.Max(currentStamina, 0f), maximum);
        }

        public static bool ResolveSprintLocked(
            bool ordinarySprintLocked,
            bool adrenalineActive)
        {
            return adrenalineActive ? false : ordinarySprintLocked;
        }

        public static bool CanRevive(
            bool requesterIsAlive,
            bool targetIsValid,
            bool targetIsOnSameRunner,
            bool targetIsDead,
            bool targetIsRequester,
            float distance,
            bool hasClearLineOfSight,
            float maximumDistance = RevivalMaximumDistance)
        {
            return requesterIsAlive &&
                   targetIsValid &&
                   targetIsOnSameRunner &&
                   targetIsDead &&
                   !targetIsRequester &&
                   IsFinite(distance) &&
                   distance >= 0f &&
                   IsFinite(maximumDistance) &&
                   maximumDistance >= 0f &&
                   distance <= maximumDistance &&
                   hasClearLineOfSight;
        }

        public static float ApplyRevivalHealth(float maximumHealth)
        {
            float maximum = IsFinite(maximumHealth)
                ? Math.Max(0f, maximumHealth)
                : 0f;
            return maximum * RevivalHealthFraction;
        }

        public static bool TryApplyRevival(
            bool targetIsDead,
            float maximumHealth,
            float maximumStamina,
            out RevivalUseTransition transition)
        {
            transition = default;
            if (!targetIsDead)
                return false;

            float stamina = IsFinite(maximumStamina)
                ? Math.Max(0f, maximumStamina)
                : 0f;
            transition = new RevivalUseTransition(
                ApplyRevivalHealth(maximumHealth),
                stamina);
            return true;
        }

        public static bool CanApplyToolHit(
            string equippedItemId,
            string requiredItemId,
            int acceptedHits,
            int requiredHits,
            bool isCompleted)
        {
            string equipped = NormalizeItemId(equippedItemId);
            string required = NormalizeItemId(requiredItemId);
            return !isCompleted &&
                   requiredHits > 0 &&
                   acceptedHits >= 0 &&
                   acceptedHits < requiredHits &&
                   IsSupportedTool(required) &&
                   string.Equals(equipped, required, StringComparison.Ordinal);
        }

        public static int ApplyAcceptedToolHit(int acceptedHits, int requiredHits)
        {
            if (requiredHits <= 0)
                return 0;

            int clampedHits = Math.Min(Math.Max(acceptedHits, 0), requiredHits);
            return Math.Min(clampedHits + 1, requiredHits);
        }

        public static ToolObstacleState NormalizeToolObstacleState(
            int acceptedHits,
            int requiredHits,
            bool isCompleted)
        {
            int clampedRequiredHits = Math.Min(
                Math.Max(requiredHits, 1),
                byte.MaxValue);
            int clampedAcceptedHits = Math.Min(
                Math.Max(acceptedHits, 0),
                clampedRequiredHits);
            bool completed = isCompleted ||
                IsToolObstacleCompleted(
                    clampedAcceptedHits,
                    clampedRequiredHits);
            if (completed)
                clampedAcceptedHits = clampedRequiredHits;

            return new ToolObstacleState(
                (byte)clampedAcceptedHits,
                completed);
        }

        public static bool TryApplyToolHit(
            string equippedItemId,
            string requiredItemId,
            int acceptedHits,
            int requiredHits,
            bool isCompleted,
            out ToolObstacleState state)
        {
            state = NormalizeToolObstacleState(
                acceptedHits,
                requiredHits,
                isCompleted);
            if (!CanApplyToolHit(
                    equippedItemId,
                    requiredItemId,
                    state.AcceptedHits,
                    requiredHits,
                    state.IsCompleted))
                return false;

            int updatedHits = ApplyAcceptedToolHit(
                state.AcceptedHits,
                requiredHits);
            state = NormalizeToolObstacleState(
                updatedHits,
                requiredHits,
                false);
            return true;
        }

        public static bool IsToolObstacleCompleted(int acceptedHits, int requiredHits)
        {
            return requiredHits > 0 && acceptedHits >= requiredHits;
        }

        public static bool IsSupportedTool(string itemId)
        {
            string normalized = NormalizeItemId(itemId);
            return normalized == CrowbarItemId || normalized == FireAxeItemId;
        }

        public static string NormalizeItemId(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId)
                ? string.Empty
                : itemId.Trim().ToLowerInvariant().Replace(' ', '_');
        }

        private static int ClampToByte(int value)
        {
            return Math.Min(Math.Max(value, 0), byte.MaxValue);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
