using NUnit.Framework;
using TheSancturary.Player;

namespace TheSancturary.Tests
{
    public sealed class PlayerStaminaStateTests
    {
        [Test]
        public void SprintingDrainsStamina()
        {
            var state = CreateState();

            var sprinting = state.Advance(0.4f, true, true, false);

            Assert.That(sprinting, Is.True);
            Assert.That(state.CurrentStamina, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void WalkingDoesNotDrainStamina()
        {
            var state = CreateState();
            state.Advance(0.4f, true, true, false);

            state.Advance(0.5f, false, true, false);

            Assert.That(state.CurrentStamina, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void CrouchingPreventsSprinting()
        {
            var state = CreateState();

            var sprinting = state.Advance(1f, true, true, true);

            Assert.That(sprinting, Is.False);
            Assert.That(state.CurrentStamina, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void RecoveryBeginsOnlyAfterDelay()
        {
            var state = CreateState();
            state.Advance(1f, true, true, false);

            state.Advance(0.99f, false, true, false);
            Assert.That(state.CurrentStamina, Is.EqualTo(75f).Within(0.001f));

            state.Advance(0.02f, false, true, false);
            Assert.That(state.CurrentStamina, Is.EqualTo(75.2f).Within(0.001f));
        }

        [Test]
        public void StaminaRemainsClampedToValidRange()
        {
            var state = CreateState();

            state.Advance(10f, true, true, false);
            Assert.That(state.CurrentStamina, Is.EqualTo(0f));

            state.Advance(20f, false, false, false);
            Assert.That(state.CurrentStamina, Is.EqualTo(100f));
        }

        [Test]
        public void ExhaustionPreventsImmediateResprinting()
        {
            var state = CreateState();
            state.Advance(4f, true, true, false);

            var sprinting = state.Advance(0.1f, true, true, false);

            Assert.That(state.IsExhausted, Is.True);
            Assert.That(sprinting, Is.False);
        }

        [Test]
        public void SprintBecomesAvailableAtExhaustedRecoveryThreshold()
        {
            var state = CreateState();
            state.Advance(4f, true, true, false);
            state.Advance(2f, false, false, false);

            Assert.That(state.CurrentStamina, Is.EqualTo(20f).Within(0.001f));
            Assert.That(state.IsExhausted, Is.False);

            var sprinting = state.Advance(0.1f, true, true, false);
            Assert.That(sprinting, Is.True);
        }

        private static PlayerStaminaState CreateState()
        {
            return new PlayerStaminaState(100f, 25f, 20f, 1f, 20f);
        }
    }
}
