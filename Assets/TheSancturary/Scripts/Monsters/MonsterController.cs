using System.Collections.Generic;
using TheSancturary.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace TheSancturary.Monsters
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class MonsterController : NetworkBehaviour
    {
        [SerializeField] private MonsterDefinition definition;
        [SerializeField] private NavMeshAgent agent;

        private readonly NetworkVariable<MonsterState> currentState = new(
            MonsterState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly MonsterStateMachine stateMachine = new();
        private readonly List<MonsterTargetSnapshot> targetSnapshots = new();
        private float nextTargetRefreshTime;

        public MonsterState CurrentState => currentState.Value;

        public override void OnNetworkSpawn()
        {
            agent.enabled = IsServer;
            if (!IsServer)
            {
                return;
            }

            agent.speed = definition.MoveSpeed;
            agent.acceleration = definition.Acceleration;
            agent.stoppingDistance = definition.StoppingDistance;
            RefreshTarget();
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            if (Time.time >= nextTargetRefreshTime)
            {
                RefreshTarget();
            }

            if (stateMachine.CurrentState == MonsterState.Chase &&
                NetworkPlayerController.ActivePlayers.TryGetValue(
                    stateMachine.TargetClientId,
                    out var target) &&
                target != null &&
                target.IsSpawned)
            {
                agent.SetDestination(target.transform.position);
            }
            else if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }

        private void RefreshTarget()
        {
            nextTargetRefreshTime = Time.time + definition.TargetRefreshInterval;
            targetSnapshots.Clear();

            foreach (var pair in NetworkPlayerController.ActivePlayers)
            {
                var target = pair.Value;
                targetSnapshots.Add(new MonsterTargetSnapshot(
                    pair.Key,
                    target != null ? target.transform.position : Vector3.zero,
                    target != null && target.IsSpawned));
            }

            stateMachine.Evaluate(transform.position, targetSnapshots, definition.DetectionRange);
            currentState.Value = stateMachine.CurrentState;
        }
    }
}
