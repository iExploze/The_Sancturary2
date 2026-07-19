using UnityEngine;

namespace TheSancturary.Player
{
    public sealed class PlayerStaminaState
    {
        private readonly float maximumStamina;
        private readonly float sprintDrainPerSecond;
        private readonly float staminaRecoveryPerSecond;
        private readonly float recoveryDelay;
        private readonly float exhaustedRecoveryThreshold;

        private float timeSinceSprintStopped;

        public PlayerStaminaState(
            float maximumStamina,
            float sprintDrainPerSecond,
            float staminaRecoveryPerSecond,
            float recoveryDelay,
            float exhaustedRecoveryThreshold)
        {
            this.maximumStamina = Mathf.Max(0.01f, maximumStamina);
            this.sprintDrainPerSecond = Mathf.Max(0f, sprintDrainPerSecond);
            this.staminaRecoveryPerSecond = Mathf.Max(0f, staminaRecoveryPerSecond);
            this.recoveryDelay = Mathf.Max(0f, recoveryDelay);
            this.exhaustedRecoveryThreshold = Mathf.Clamp(
                exhaustedRecoveryThreshold,
                0f,
                this.maximumStamina);

            CurrentStamina = this.maximumStamina;
            timeSinceSprintStopped = this.recoveryDelay;
        }

        public float CurrentStamina { get; private set; }
        public float NormalizedStamina => CurrentStamina / maximumStamina;
        public bool IsExhausted { get; private set; }
        public bool IsSprinting { get; private set; }

        public bool Advance(
            float deltaTime,
            bool sprintRequested,
            bool isMoving,
            bool isCrouching)
        {
            var stamina = CurrentStamina;
            var exhausted = IsExhausted;
            IsSprinting = PlayerStaminaSimulation.Advance(
                deltaTime,
                sprintRequested,
                isMoving,
                isCrouching,
                maximumStamina,
                sprintDrainPerSecond,
                staminaRecoveryPerSecond,
                recoveryDelay,
                exhaustedRecoveryThreshold,
                ref stamina,
                ref exhausted,
                ref timeSinceSprintStopped);
            CurrentStamina = stamina;
            IsExhausted = exhausted;
            return IsSprinting;
        }
    }

    public static class PlayerStaminaSimulation
    {
        public static bool Advance(
            float deltaTime,
            bool sprintRequested,
            bool isMoving,
            bool isCrouching,
            float maximumStamina,
            float sprintDrainPerSecond,
            float staminaRecoveryPerSecond,
            float recoveryDelay,
            float exhaustedRecoveryThreshold,
            ref float currentStamina,
            ref bool isExhausted,
            ref float timeSinceSprintStopped)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            maximumStamina = Mathf.Max(0.01f, maximumStamina);
            exhaustedRecoveryThreshold = Mathf.Clamp(exhaustedRecoveryThreshold, 0f, maximumStamina);

            if (isExhausted && currentStamina >= exhaustedRecoveryThreshold)
            {
                isExhausted = false;
            }

            var isSprinting = sprintRequested &&
                              isMoving &&
                              !isCrouching &&
                              !isExhausted &&
                              currentStamina > 0f;
            if (isSprinting)
            {
                currentStamina -= Mathf.Max(0f, sprintDrainPerSecond) * deltaTime;
                timeSinceSprintStopped = 0f;
                if (currentStamina <= 0f)
                {
                    currentStamina = 0f;
                    isExhausted = true;
                }
            }
            else
            {
                var previousTime = timeSinceSprintStopped;
                timeSinceSprintStopped += deltaTime;
                var recoverableTime = Mathf.Max(0f, timeSinceSprintStopped - Mathf.Max(0f, recoveryDelay)) -
                                      Mathf.Max(0f, previousTime - Mathf.Max(0f, recoveryDelay));
                currentStamina += Mathf.Max(0f, staminaRecoveryPerSecond) * recoverableTime;
            }

            currentStamina = Mathf.Clamp(currentStamina, 0f, maximumStamina);
            if (isExhausted && currentStamina >= exhaustedRecoveryThreshold)
            {
                isExhausted = false;
            }

            return isSprinting;
        }
    }
}
