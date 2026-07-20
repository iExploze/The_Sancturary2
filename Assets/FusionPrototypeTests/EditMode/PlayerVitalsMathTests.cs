using NUnit.Framework;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class PlayerVitalsMathTests
    {
        [Test]
        public void FiveSecondsOfSprintingExhaustsAndLocksStamina()
        {
            StaminaStep result = new(100f, 0f, false);
            for (int i = 0; i < 50; i++)
                result = PlayerVitalsMath.UpdateStamina(result.Stamina, result.RecoveryElapsed, result.SprintLocked, true, 0.1f, 100f, 20f, 1.25f, 24f, 30f);

            Assert.That(result.Stamina, Is.EqualTo(0f).Within(0.001f));
            Assert.That(result.SprintLocked, Is.True);
        }

        [Test]
        public void RecoveryWaitsForDelayAndUnlocksAtThreshold()
        {
            StaminaStep result = new(0f, 0f, true);
            result = PlayerVitalsMath.UpdateStamina(result.Stamina, result.RecoveryElapsed, result.SprintLocked, false, 1f, 100f, 20f, 1.25f, 24f, 30f);
            Assert.That(result.Stamina, Is.Zero);
            Assert.That(result.SprintLocked, Is.True);

            result = PlayerVitalsMath.UpdateStamina(result.Stamina, result.RecoveryElapsed, result.SprintLocked, false, 1f, 100f, 20f, 1.25f, 24f, 30f);
            result = PlayerVitalsMath.UpdateStamina(result.Stamina, result.RecoveryElapsed, result.SprintLocked, false, 0.25f, 100f, 20f, 1.25f, 24f, 30f);
            Assert.That(result.Stamina, Is.EqualTo(30f).Within(0.001f));
            Assert.That(result.SprintLocked, Is.False);
        }

        [Test]
        public void HealthRegenerationStartsOnlyAfterDelay()
        {
            HealthStep result = PlayerVitalsMath.RegenerateHealth(50f, 0f, 4f, 100f, 5f, 8f);
            Assert.That(result.Health, Is.EqualTo(50f));

            result = PlayerVitalsMath.RegenerateHealth(result.Health, result.TimeSinceDamage, 1f, 100f, 5f, 8f);
            Assert.That(result.Health, Is.EqualTo(58f).Within(0.001f));
        }

        [Test]
        public void HealthRegenerationNeverExceedsMaximum()
        {
            HealthStep result = PlayerVitalsMath.RegenerateHealth(99f, 10f, 1f, 100f, 5f, 8f);
            Assert.That(result.Health, Is.EqualTo(100f));
        }
    }
}
