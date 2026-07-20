using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    public readonly struct StaminaStep
    {
        public StaminaStep(float stamina, float recoveryElapsed, bool sprintLocked)
        {
            Stamina = stamina;
            RecoveryElapsed = recoveryElapsed;
            SprintLocked = sprintLocked;
        }

        public float Stamina { get; }
        public float RecoveryElapsed { get; }
        public bool SprintLocked { get; }
    }

    public readonly struct HealthStep
    {
        public HealthStep(float health, float timeSinceDamage)
        {
            Health = health;
            TimeSinceDamage = timeSinceDamage;
        }

        public float Health { get; }
        public float TimeSinceDamage { get; }
    }

    public static class PlayerVitalsMath
    {
        public static StaminaStep UpdateStamina(
            float stamina,
            float recoveryElapsed,
            bool sprintLocked,
            bool isActuallySprinting,
            float deltaTime,
            float maximum,
            float drainRate,
            float recoveryDelay,
            float recoveryRate,
            float restartThreshold)
        {
            maximum = Mathf.Max(0.01f, maximum);
            stamina = Mathf.Clamp(stamina, 0f, maximum);
            deltaTime = Mathf.Max(0f, deltaTime);

            if (isActuallySprinting && !sprintLocked)
            {
                stamina = Mathf.Max(0f, stamina - Mathf.Max(0f, drainRate) * deltaTime);
                recoveryElapsed = 0f;
                if (stamina <= 0f)
                    sprintLocked = true;
            }
            else
            {
                recoveryElapsed += deltaTime;
                if (recoveryElapsed >= Mathf.Max(0f, recoveryDelay))
                    stamina = Mathf.Min(maximum, stamina + Mathf.Max(0f, recoveryRate) * deltaTime);

                if (sprintLocked && stamina >= Mathf.Clamp(restartThreshold, 0f, maximum))
                    sprintLocked = false;
            }

            return new StaminaStep(stamina, recoveryElapsed, sprintLocked);
        }

        public static HealthStep RegenerateHealth(
            float health,
            float timeSinceDamage,
            float deltaTime,
            float maximum,
            float regenerationDelay,
            float regenerationRate)
        {
            maximum = Mathf.Max(1f, maximum);
            health = Mathf.Clamp(health, 0f, maximum);
            deltaTime = Mathf.Max(0f, deltaTime);
            timeSinceDamage += deltaTime;

            if (timeSinceDamage >= Mathf.Max(0f, regenerationDelay))
                health = Mathf.Min(maximum, health + Mathf.Max(0f, regenerationRate) * deltaTime);

            return new HealthStep(health, timeSinceDamage);
        }
    }
}
