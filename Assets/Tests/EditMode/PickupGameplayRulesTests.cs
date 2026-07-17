using NUnit.Framework;
using TheSancturary.Player;
using Unity.Netcode;
using UnityEngine;

namespace TheSancturary.Tests
{
    public sealed class PickupGameplayRulesTests
    {
        [Test]
        public void EligiblePickupPassesEveryServerValidationRule()
        {
            Assert.That(PickupInteractionRules.CanPickUp(
                requesterAlreadyHolding: false,
                targetIsHeld: false,
                targetIsConfigured: true,
                hasLineOfSight: true,
                distance: 2.5f,
                maximumDistance: 2.75f), Is.True);
        }

        [TestCase(true, false, true, true, 1f)]
        [TestCase(false, true, true, true, 1f)]
        [TestCase(false, false, false, true, 1f)]
        [TestCase(false, false, true, false, 1f)]
        [TestCase(false, false, true, true, 3f)]
        public void InvalidPickupIsRejected(
            bool alreadyHolding,
            bool targetHeld,
            bool configured,
            bool lineOfSight,
            float distance)
        {
            Assert.That(PickupInteractionRules.CanPickUp(
                alreadyHolding,
                targetHeld,
                configured,
                lineOfSight,
                distance,
                2.75f), Is.False);
        }

        [Test]
        public void ReplicatedReleasedStateHasNoHolder()
        {
            var released = PickupNetworkState.Released;
            var nullHolder = new NetworkObjectReference((NetworkObject)null);

            Assert.That(released.IsHeld, Is.False);
            Assert.That(released.Holder, Is.EqualTo(nullHolder));
        }

        [Test]
        public void HeldPhysicsDisablesSimulationAndInterpolation()
        {
            var authored = CreateAuthoredSettings();

            var held = PickupPhysicsRules.Resolve(authored, isHeld: true, isServer: true);

            Assert.That(held.UseGravity, Is.False);
            Assert.That(held.IsKinematic, Is.True);
            Assert.That(held.Interpolation, Is.EqualTo(RigidbodyInterpolation.None));
            Assert.That(held.CollisionDetection, Is.EqualTo(CollisionDetectionMode.ContinuousSpeculative));
        }

        [Test]
        public void DroppedServerRestoresAuthoredPhysics()
        {
            var authored = CreateAuthoredSettings();

            var dropped = PickupPhysicsRules.Resolve(authored, isHeld: false, isServer: true);

            Assert.That(dropped.UseGravity, Is.True);
            Assert.That(dropped.IsKinematic, Is.False);
            Assert.That(dropped.Interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
            Assert.That(dropped.CollisionDetection, Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
        }

        [Test]
        public void DroppedReplicaRemainsKinematicWithInterpolation()
        {
            var dropped = PickupPhysicsRules.Resolve(
                CreateAuthoredSettings(),
                isHeld: false,
                isServer: false);

            Assert.That(dropped.UseGravity, Is.True);
            Assert.That(dropped.IsKinematic, Is.True);
            Assert.That(dropped.Interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
            Assert.That(dropped.CollisionDetection, Is.EqualTo(CollisionDetectionMode.ContinuousSpeculative));
        }

        [Test]
        public void HolderDisconnectReleasesOnlyItsHeldObject()
        {
            Assert.That(PickupReleaseRules.ShouldReleaseAfterDisconnect(true, true, 7, 7), Is.True);
            Assert.That(PickupReleaseRules.ShouldReleaseAfterDisconnect(true, true, 7, 8), Is.False);
            Assert.That(PickupReleaseRules.ShouldReleaseAfterDisconnect(false, true, 7, 7), Is.False);
            Assert.That(PickupReleaseRules.ShouldReleaseAfterDisconnect(true, false, 7, 7), Is.False);
        }

        private static PickupPhysicsSettings CreateAuthoredSettings()
        {
            return new PickupPhysicsSettings(
                true,
                false,
                RigidbodyInterpolation.Interpolate,
                CollisionDetectionMode.ContinuousDynamic);
        }
    }
}
