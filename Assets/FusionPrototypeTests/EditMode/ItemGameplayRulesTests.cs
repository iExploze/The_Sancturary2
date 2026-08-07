using System;
using NUnit.Framework;
using TheSancturary.Inventory;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class ItemGameplayRulesTests
    {
        [TestCase(-1, 6, 0)]
        [TestCase(3, 6, 3)]
        [TestCase(7, 6, 6)]
        [TestCase(1, 0, 0)]
        public void LoadedAmmunitionIsClampedToCapacity(
            int loaded,
            int capacity,
            int expected)
        {
            Assert.That(
                ItemGameplayRules.ClampLoadedAmmunition(loaded, capacity),
                Is.EqualTo(expected));
        }

        [Test]
        public void ReloadConsumesTheExactSourceOnceFillsAndDiscardsOldRounds()
        {
            Assert.That(
                ItemGameplayRules.TryApplyReload(
                    101,
                    true,
                    true,
                    "revolver_ammo",
                    202,
                    true,
                    true,
                    "revolver_ammo",
                    3,
                    6,
                    out ItemGameplayRules.ReloadTransition transition),
                Is.True);
            Assert.That(transition.ConsumedSourceInstanceId, Is.EqualTo(101));
            Assert.That(transition.TargetInstanceId, Is.EqualTo(202));
            Assert.That(transition.ConsumedInstanceCount, Is.EqualTo(1));
            Assert.That(transition.LoadedAmmunition, Is.EqualTo(6));

            Assert.That(
                ItemGameplayRules.TryApplyReload(
                    101,
                    false,
                    true,
                    "revolver_ammo",
                    202,
                    true,
                    true,
                    "revolver_ammo",
                    transition.LoadedAmmunition,
                    6,
                    out ItemGameplayRules.ReloadTransition repeated),
                Is.False,
                "A source instance that was already removed cannot reload twice.");
            Assert.That(repeated.ConsumedInstanceCount, Is.Zero);
        }

        [Test]
        public void ReloadRejectsIncompatibleAmmunitionWithoutConsumption()
        {
            Assert.That(
                ItemGameplayRules.TryApplyReload(
                    301,
                    true,
                    true,
                    "shotgun_shell",
                    302,
                    true,
                    true,
                    "revolver_ammo",
                    2,
                    6,
                    out ItemGameplayRules.ReloadTransition transition),
                Is.False);
            Assert.That(transition.ConsumedInstanceCount, Is.Zero);
        }

        [Test]
        public void FiringConsumesExactlyOneRoundAndCannotUnderflow()
        {
            Assert.That(
                ItemGameplayRules.TryFireRound(2, 6, out byte remaining),
                Is.True);
            Assert.That(remaining, Is.EqualTo(1));

            Assert.That(
                ItemGameplayRules.TryFireRound(0, 6, out remaining),
                Is.False);
            Assert.That(remaining, Is.Zero);
        }

        [Test]
        public void LoadedAmmunitionSurvivesDropAndPickupStateRoundTrip()
        {
            byte dropped = ItemGameplayRules.CaptureLoadedAmmunitionForWorld(
                4,
                6);
            byte pickedUp =
                ItemGameplayRules.RestoreLoadedAmmunitionFromWorld(
                    dropped,
                    6);

            Assert.That(dropped, Is.EqualTo(4));
            Assert.That(pickedUp, Is.EqualTo(4));
            Assert.That(
                ItemGameplayRules.RestoreLoadedAmmunitionFromWorld(9, 6),
                Is.EqualTo(6),
                "Transferred world state remains clamped to capacity.");
        }

        [Test]
        public void MedKitRequiresMissingHealthAndHealsToFull()
        {
            Assert.That(
                ItemGameplayRules.TryApplyMedKitUse(
                    true,
                    50f,
                    100f,
                    out ItemGameplayRules.MedKitUseTransition transition),
                Is.True);
            Assert.That(transition.Health, Is.EqualTo(100f));
            Assert.That(transition.PendingDamage, Is.Zero);
            Assert.That(transition.TimeSinceDamage, Is.Zero);
            Assert.That(transition.ConsumedInstanceCount, Is.EqualTo(1));
            Assert.That(
                ItemGameplayRules.TryApplyMedKitUse(
                    true,
                    100f,
                    100f,
                    out transition),
                Is.False);
            Assert.That(transition.ConsumedInstanceCount, Is.Zero);
            Assert.That(
                ItemGameplayRules.CanUseMedKit(false, 50f, 100f),
                Is.False);
        }

        [Test]
        public void AdrenalineCannotStackAndOverridesOnlyStaminaLockRules()
        {
            Assert.That(
                ItemGameplayRules.TryStartAdrenaline(
                    true,
                    false,
                    out ItemGameplayRules.AdrenalineUseTransition transition),
                Is.True);
            Assert.That(transition.ConsumedInstanceCount, Is.EqualTo(1));
            Assert.That(transition.DurationSeconds, Is.EqualTo(30f));
            Assert.That(
                ItemGameplayRules.TryStartAdrenaline(
                    true,
                    true,
                    out transition),
                Is.False);
            Assert.That(transition.ConsumedInstanceCount, Is.Zero);
            Assert.That(
                ItemGameplayRules.ResolveStamina(20f, 100f, true),
                Is.EqualTo(100f));
            Assert.That(
                ItemGameplayRules.ResolveSprintLocked(true, true),
                Is.False);
            Assert.That(
                ItemGameplayRules.ResolveStamina(20f, 100f, false),
                Is.EqualTo(20f));
            Assert.That(
                ItemGameplayRules.ResolveSprintLocked(true, false),
                Is.True);
            Assert.That(
                ItemGameplayRules.AdrenalineDurationSeconds,
                Is.EqualTo(30f));
            Assert.That(ItemGameplayRules.IsAdrenalineActive(0.1f), Is.True);
            Assert.That(ItemGameplayRules.IsAdrenalineActive(0f), Is.False);
        }

        [Test]
        public void RevivalRejectsSelfLivingDistantAndBlockedTargets()
        {
            Assert.That(CanRevive(targetIsDead: true), Is.True);
            Assert.That(
                CanRevive(targetIsDead: true, targetIsRequester: true),
                Is.False);
            Assert.That(CanRevive(targetIsDead: false), Is.False);
            Assert.That(
                CanRevive(targetIsDead: true, distance: 2.01f),
                Is.False);
            Assert.That(
                CanRevive(targetIsDead: true, hasClearLineOfSight: false),
                Is.False);
        }

        [Test]
        public void SuccessfulRevivalSetsHealthAndClearsDeathAndRespawnState()
        {
            Assert.That(
                ItemGameplayRules.TryApplyRevival(
                    true,
                    100f,
                    100f,
                    out ItemGameplayRules.RevivalUseTransition transition),
                Is.True);
            Assert.That(transition.Health, Is.EqualTo(50f));
            Assert.That(transition.Stamina, Is.EqualTo(100f));
            Assert.That(transition.IsDead, Is.False);
            Assert.That(transition.ClearRespawnTimer, Is.True);
            Assert.That(transition.PendingDamage, Is.Zero);
            Assert.That(transition.TimeSinceDamage, Is.Zero);
            Assert.That(transition.ConsumedInstanceCount, Is.EqualTo(1));

            Assert.That(
                ItemGameplayRules.TryApplyRevival(
                    false,
                    100f,
                    100f,
                    out transition),
                Is.False,
                "A living target cannot be revived or consume a syringe.");
            Assert.That(transition.ConsumedInstanceCount, Is.Zero);
        }

        [Test]
        public void ToolHitsRequireTheExplicitMatchingTool()
        {
            Assert.That(
                ItemGameplayRules.CanApplyToolHit(
                    "crowbar",
                    "crowbar",
                    0,
                    1,
                    false),
                Is.True);
            Assert.That(
                ItemGameplayRules.CanApplyToolHit(
                    "fire_axe",
                    "crowbar",
                    0,
                    1,
                    false),
                Is.False);
            Assert.That(
                ItemGameplayRules.CanApplyToolHit(
                    "crowbar",
                    "fire_axe",
                    0,
                    2,
                    false),
                Is.False);
        }

        [Test]
        public void AxeObstacleCompletesAfterExactlyTwoAcceptedHits()
        {
            Assert.That(
                ItemGameplayRules.TryApplyToolHit(
                    "fire_axe",
                    "fire_axe",
                    0,
                    2,
                    false,
                    out ItemGameplayRules.ToolObstacleState state),
                Is.True);
            Assert.That(state.AcceptedHits, Is.EqualTo(1));
            Assert.That(state.IsCompleted, Is.False);

            Assert.That(
                ItemGameplayRules.TryApplyToolHit(
                    "fire_axe",
                    "fire_axe",
                    state.AcceptedHits,
                    2,
                    state.IsCompleted,
                    out state),
                Is.True);
            Assert.That(state.AcceptedHits, Is.EqualTo(2));
            Assert.That(state.IsCompleted, Is.True);

            ItemGameplayRules.ToolObstacleState lateJoinState =
                ItemGameplayRules.NormalizeToolObstacleState(
                    state.AcceptedHits,
                    2,
                    state.IsCompleted);
            Assert.That(lateJoinState.AcceptedHits, Is.EqualTo(2));
            Assert.That(lateJoinState.IsCompleted, Is.True);
            Assert.That(
                ItemGameplayRules.TryApplyToolHit(
                    "fire_axe",
                    "fire_axe",
                    lateJoinState.AcceptedHits,
                    2,
                    lateJoinState.IsCompleted,
                    out ItemGameplayRules.ToolObstacleState repeated),
                Is.False);
            Assert.That(repeated.AcceptedHits, Is.EqualTo(2));
            Assert.That(repeated.IsCompleted, Is.True);
        }

        [Test]
        public void InventoryHasNoArbitraryCategoryLimitRejection()
        {
            CollectionAssert.DoesNotContain(
                Enum.GetNames(typeof(InventoryRequestRejection)),
                "CategoryLimitReached");
        }

        private static bool CanRevive(
            bool targetIsDead,
            bool targetIsRequester = false,
            float distance = 2f,
            bool hasClearLineOfSight = true)
        {
            return ItemGameplayRules.CanRevive(
                requesterIsAlive: true,
                targetIsValid: true,
                targetIsOnSameRunner: true,
                targetIsDead: targetIsDead,
                targetIsRequester: targetIsRequester,
                distance: distance,
                hasClearLineOfSight: hasClearLineOfSight);
        }
    }
}
