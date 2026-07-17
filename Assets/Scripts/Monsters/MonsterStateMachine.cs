using System.Collections.Generic;
using UnityEngine;

namespace TheSancturary.Monsters
{
    public sealed class MonsterStateMachine
    {
        public const ulong NoTarget = ulong.MaxValue;

        public MonsterState CurrentState { get; private set; } = MonsterState.Idle;

        public ulong TargetClientId { get; private set; } = NoTarget;

        public void Evaluate(
            Vector3 origin,
            IReadOnlyList<MonsterTargetSnapshot> candidates,
            float detectionRange)
        {
            if (MonsterTargetSelector.TrySelectNearest(origin, candidates, detectionRange, out var target))
            {
                CurrentState = MonsterState.Chase;
                TargetClientId = target.ClientId;
                return;
            }

            CurrentState = MonsterState.Idle;
            TargetClientId = NoTarget;
        }
    }
}
