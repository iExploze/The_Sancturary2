using NUnit.Framework;
using TheSancturary.Monsters;
using UnityEngine;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class HenryMonsterRulesTests
    {
        private const float MovementThreshold = 0.12f;

        [Test]
        public void VisibleMovingEligiblePlayerCanBeAcquired()
        {
            Assert.That(CanAcquire(Vector3.forward, true, true, true), Is.True);
        }

        [Test]
        public void VisibleStationaryPlayerCannotBeAcquired()
        {
            Assert.That(CanAcquire(Vector3.zero, true, true, true), Is.False);
        }

        [Test]
        public void CameraRotationWithoutTranslationCannotBeAcquired()
        {
            Assert.That(CanAcquire(Vector3.zero, true, true, true), Is.False);
        }

        [TestCase(false, true, true, TestName = "MovingOutsideRangeCannotBeAcquired")]
        [TestCase(true, false, true, TestName = "MovingBehindHenryCannotBeAcquired")]
        [TestCase(true, true, false, TestName = "MovingBehindWallCannotBeAcquired")]
        public void MovingPlayerMustSatisfyEveryVisionRule(bool inRange, bool inFov, bool hasLineOfSight)
        {
            Assert.That(CanAcquire(Vector3.forward, inRange, inFov, hasLineOfSight), Is.False);
        }

        [Test]
        public void ClosestEligiblePlayerWinsOverCloserStationaryPlayer()
        {
            float bestDistance = float.PositiveInfinity;
            bool movingSelected = HenryMonsterRules.ShouldSelectCandidate(
                CanAcquire(Vector3.forward, true, true, true), 5f, bestDistance);
            Assert.That(movingSelected, Is.True);
            bestDistance = 5f;
            bool stationarySelected = HenryMonsterRules.ShouldSelectCandidate(
                CanAcquire(Vector3.zero, true, true, true), 2f, bestDistance);
            Assert.That(stationarySelected, Is.False);
        }

        [Test]
        public void CurrentTargetStoppingDoesNotEndChase()
        {
            Assert.That(HenryMonsterRules.CanMaintainChase(true, true, false), Is.True);
        }

        [Test]
        public void LineOfSightLossUsesOnlyConfiguredShortGrace()
        {
            Assert.That(HenryMonsterRules.HasExceededLineOfSightGrace(false, 0.34f, 0.35f), Is.False);
            Assert.That(HenryMonsterRules.HasExceededLineOfSightGrace(false, 0.35f, 0.35f), Is.True);
        }

        [Test]
        public void LockerImmediatelyMakesCurrentTargetInvalid()
        {
            Assert.That(HenryMonsterRules.CanMaintainChase(true, true, true), Is.False);
        }

        [Test]
        public void OneHitDealsTwentyFiveAndFourHitsKillFullHealthPlayer()
        {
            Assert.That(HenryMonsterRules.HealthAfterHits(100f, 25, 1), Is.EqualTo(75f));
            Assert.That(HenryMonsterRules.HealthAfterHits(100f, 25, 4), Is.Zero);
        }

        private static bool CanAcquire(Vector3 velocity, bool inRange, bool inFov, bool hasLineOfSight)
        {
            return HenryMonsterRules.CanAcquire(
                true, true, false, inRange, inFov, hasLineOfSight, velocity, MovementThreshold);
        }
    }
}
