using System.Collections.Generic;
using NUnit.Framework;
using TheSancturary.Monsters;
using UnityEngine;

namespace TheSancturary.Tests
{
    public sealed class MonsterStateMachineTests
    {
        [Test]
        public void TargetSelectionChoosesNearestValidPlayer()
        {
            var candidates = new List<MonsterTargetSnapshot>
            {
                new(1, new Vector3(8f, 0f, 0f)),
                new(2, new Vector3(3f, 0f, 0f)),
                new(3, new Vector3(1f, 0f, 0f), false)
            };

            var selected = MonsterTargetSelector.TrySelectNearest(
                Vector3.zero,
                candidates,
                20f,
                out var nearest);

            Assert.That(selected, Is.True);
            Assert.That(nearest.ClientId, Is.EqualTo(2));
        }

        [Test]
        public void ValidPlayerTransitionsIdleToChase()
        {
            var stateMachine = new MonsterStateMachine();

            stateMachine.Evaluate(
                Vector3.zero,
                new[] { new MonsterTargetSnapshot(7, Vector3.forward * 4f) },
                10f);

            Assert.That(stateMachine.CurrentState, Is.EqualTo(MonsterState.Chase));
            Assert.That(stateMachine.TargetClientId, Is.EqualTo(7));
        }

        [Test]
        public void NoPlayersTransitionsChaseToIdle()
        {
            var stateMachine = new MonsterStateMachine();
            stateMachine.Evaluate(
                Vector3.zero,
                new[] { new MonsterTargetSnapshot(7, Vector3.forward * 4f) },
                10f);

            stateMachine.Evaluate(Vector3.zero, new MonsterTargetSnapshot[0], 10f);

            Assert.That(stateMachine.CurrentState, Is.EqualTo(MonsterState.Idle));
            Assert.That(stateMachine.TargetClientId, Is.EqualTo(MonsterStateMachine.NoTarget));
        }

        [Test]
        public void RemovedTargetIsHandledWithoutException()
        {
            var stateMachine = new MonsterStateMachine();
            stateMachine.Evaluate(
                Vector3.zero,
                new[] { new MonsterTargetSnapshot(42, Vector3.right * 2f) },
                10f);

            Assert.DoesNotThrow(() => stateMachine.Evaluate(
                Vector3.zero,
                new[] { new MonsterTargetSnapshot(42, Vector3.right * 2f, false) },
                10f));
            Assert.That(stateMachine.CurrentState, Is.EqualTo(MonsterState.Idle));
            Assert.That(stateMachine.TargetClientId, Is.EqualTo(MonsterStateMachine.NoTarget));
        }
    }
}
