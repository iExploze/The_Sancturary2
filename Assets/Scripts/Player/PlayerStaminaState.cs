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
            deltaTime = Mathf.Max(0f, deltaTime);
            if (IsExhausted && CurrentStamina >= exhaustedRecoveryThreshold)
            {
                IsExhausted = false;
            }

            IsSprinting = sprintRequested &&
                          isMoving &&
                          !isCrouching &&
                          !IsExhausted &&
                          CurrentStamina > 0f;

            if (IsSprinting)
            {
                CurrentStamina -= sprintDrainPerSecond * deltaTime;
                timeSinceSprintStopped = 0f;

                if (CurrentStamina <= 0f)
                {
                    CurrentStamina = 0f;
                    IsExhausted = true;
                }
            }
            else
            {
                var previousTimeSinceSprint = timeSinceSprintStopped;
                timeSinceSprintStopped += deltaTime;
                var recoverableTime = Mathf.Max(0f, timeSinceSprintStopped - recoveryDelay) -
                                      Mathf.Max(0f, previousTimeSinceSprint - recoveryDelay);
                CurrentStamina += staminaRecoveryPerSecond * recoverableTime;
            }

            CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, maximumStamina);
            if (IsExhausted && CurrentStamina >= exhaustedRecoveryThreshold)
            {
                IsExhausted = false;
            }

            return IsSprinting;
        }
    }
}
