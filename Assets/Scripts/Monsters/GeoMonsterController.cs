using Fusion;
using TheSancturary.FusionPrototype;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

namespace TheSancturary.Monsters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public sealed class GeoMonsterController : NetworkBehaviour
    {
        public enum GeoMonsterState : byte
        {
            IdleScan,
            Patrol,
            Chase,
            LockerKill,
            Attack
        }

        private enum MonsterAudioEvent : byte
        {
            None,
            WalkFootstep,
            RunFootstep,
            Attack
        }

        private const float ScanDuration = 1.633f;
        private static readonly int IdleScanState = Animator.StringToHash("IdleScan");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int RunState = Animator.StringToHash("Run");
        private static readonly int AttackState = Animator.StringToHash("Attack");

        [Header("Required References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform detectionOrigin;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private Transform[] patrolWaypoints;

        [Header("Navigation")]
        [SerializeField, Min(0.1f)] private float patrolSpeed = 2.2f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 4.8f;
        [SerializeField, Min(0.05f)] private float waypointStoppingDistance = 0.45f;

        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float detectionDistance = 14f;
        [SerializeField, Range(1f, 179f)] private float horizontalFieldOfView = 85f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Min(0f)] private float lostTargetGracePeriod = 2.25f;
        [SerializeField, Range(0f, 90f)] private float scanAngle = 55f;
        [SerializeField, Min(0.05f)] private float retargetInterval = 0.25f;

        [Header("Attack")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
        [SerializeField, Min(0.1f)] private float attackDuration = 1.867f;
        [SerializeField, Range(0f, 1f)] private float attackImpactNormalizedTime = 0.45f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.5f;
        [SerializeField] private int attackDamage = 50;
        [SerializeField] private VideoClip jumpscareVideo;

        [Header("Witnessed Locker Kill")]
        [SerializeField, Min(0.1f)] private float lockerKillRange = 1.8f;

        [Header("Audio")]
        [SerializeField] private AudioSource movementAudioSource;
        [SerializeField] private AudioSource attackAudioSource;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip attackClip;
        [SerializeField, Min(0.05f)] private float walkFootstepInterval = 0.62f;
        [SerializeField, Min(0.05f)] private float runFootstepInterval = 0.34f;

        [Networked] public GeoMonsterState CurrentState { get; private set; }
        [Networked] public PlayerRef TargetPlayer { get; private set; }
        [Networked] public Vector3 PatrolDestination { get; private set; }
        [Networked] private TickTimer StateTimer { get; set; }
        [Networked] private TickTimer LostTargetTimer { get; set; }
        [Networked] private TickTimer RetargetTimer { get; set; }
        [Networked] private TickTimer AttackCooldownTimer { get; set; }
        [Networked] private NetworkBool DamageApplied { get; set; }
        [Networked] private NetworkBool LethalAttack { get; set; }
        [Networked] private float FootstepElapsed { get; set; }
        [Networked] private byte AudioEventSequence { get; set; }
        [Networked] private byte AudioEventCode { get; set; }
        [Networked] private byte AttackSequence { get; set; }
        [Networked] private byte JumpscareSequence { get; set; }
        [Networked] private PlayerRef JumpscareVictim { get; set; }
        [Networked] private NetworkBehaviourId WitnessedLocker { get; set; }
        [Networked] private PlayerRef WitnessedLockerPlayer { get; set; }

        private NavMeshAgent _agent;
        private NetworkTransform _networkTransform;
        private int _lastWaypointIndex = -1;
        private byte _lastPresentedAudioSequence;
        private byte _lastPresentedAttackSequence;
        private byte _lastPresentedJumpscareSequence;
        private GeoMonsterState _presentedState;
        private float _renderStateElapsed;
        private bool _presentationInitialized;
        private bool _warnedMissingReferences;

        public Transform CurrentTarget
        {
            get
            {
                return TryResolvePlayer(TargetPlayer, out FusionNetworkPlayer player)
                    ? player.transform
                    : null;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureAudioSource(movementAudioSource);
            ConfigureAudioSource(attackAudioSource);
        }

        public override void Spawned()
        {
            ResolveReferences();
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            Object.EnableInterpolation = !HasStateAuthority;
            Object.RenderSource = HasStateAuthority ? RenderSource.Latest : RenderSource.Interpolated;
            PhysicsSettings physicsSettings = _networkTransform.PhysicsSettings;
            physicsSettings.ForecastEnabled = false;
            _networkTransform.PhysicsSettings = physicsSettings;
            _agent.enabled = HasStateAuthority;
            if (HasStateAuthority)
            {
                EnsureAgentOnNavMesh();
                _agent.updatePosition = false;
                _agent.updateRotation = false;
                _agent.nextPosition = transform.position;
                TargetPlayer = PlayerRef.None;
                PatrolDestination = transform.position;
                AudioEventCode = (byte)MonsterAudioEvent.None;
                EnterAuthorityState(GeoMonsterState.IdleScan);
            }

            _lastPresentedAudioSequence = AudioEventSequence;
            _lastPresentedAttackSequence = AttackSequence;
            _lastPresentedJumpscareSequence = JumpscareSequence;
            PresentState(true);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            switch (CurrentState)
            {
                case GeoMonsterState.IdleScan:
                    UpdateAuthorityIdleScan();
                    break;
                case GeoMonsterState.Patrol:
                    UpdateAuthorityPatrol();
                    break;
                case GeoMonsterState.Chase:
                    UpdateAuthorityChase();
                    break;
                case GeoMonsterState.LockerKill:
                    UpdateAuthorityLockerKill();
                    break;
                case GeoMonsterState.Attack:
                    UpdateAuthorityAttack();
                    break;
            }

            AdvanceAuthorityMovement();
            UpdateAuthorityMovementAudio();
        }

        public override void Render()
        {
            PresentState(false);
            PresentAudioEvents();
            PresentJumpscareEvent();

            if (CurrentState == GeoMonsterState.IdleScan)
            {
                _renderStateElapsed = Mathf.Min(ScanDuration, _renderStateElapsed + Time.deltaTime);
                ApplyScanPivot(_renderStateElapsed / ScanDuration);
            }
            else if (detectionOrigin != null)
            {
                detectionOrigin.localRotation = Quaternion.identity;
            }
        }

        private void UpdateAuthorityIdleScan()
        {
            ApplyScanPivot(GetStateElapsed(ScanDuration) / ScanDuration);
            if (TryAcquireClosestVisiblePlayer())
                return;

            if (StateTimer.Expired(Runner))
            {
                detectionOrigin.localRotation = Quaternion.identity;
                SelectRandomReachableWaypoint();
            }
        }

        private void UpdateAuthorityPatrol()
        {
            detectionOrigin.localRotation = Quaternion.identity;
            if (TryAcquireClosestVisiblePlayer())
                return;

            if (!_agent.pathPending &&
                (_agent.remainingDistance <= waypointStoppingDistance || _agent.pathStatus != NavMeshPathStatus.PathComplete))
            {
                EnterAuthorityState(GeoMonsterState.IdleScan);
            }
        }

        private void UpdateAuthorityChase()
        {
            detectionOrigin.localRotation = Quaternion.identity;
            if (!TryResolveLivingPlayer(TargetPlayer, out FusionNetworkPlayer target))
            {
                if (!TryAcquireClosestVisiblePlayer())
                    ClearTargetAndScan();
                return;
            }

            if (RetargetTimer.ExpiredOrNotRunning(Runner))
            {
                TryRetargetToCloserVisiblePlayer(target);
                RetargetTimer = TickTimer.CreateFromSeconds(Runner, retargetInterval);
                TryResolveLivingPlayer(TargetPlayer, out target);
            }

            if (target == null)
            {
                ClearTargetAndScan();
                return;
            }

            Vector3 targetPosition = target.transform.position;
            float distance = HorizontalDistance(attackOrigin.position, targetPosition);
            bool hasLineOfSight = HasChaseLineOfSight(target);
            if (hasLineOfSight &&
                distance <= attackRange &&
                AttackCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                EnterAuthorityState(GeoMonsterState.Attack);
                return;
            }

            _agent.SetDestination(targetPosition);
            if (hasLineOfSight)
            {
                LostTargetTimer = TickTimer.None;
            }
            else
            {
                if (!LostTargetTimer.IsRunning)
                    LostTargetTimer = TickTimer.CreateFromSeconds(Runner, lostTargetGracePeriod);
                if (LostTargetTimer.Expired(Runner))
                    ClearTargetAndScan();
            }
        }

        private void UpdateAuthorityAttack()
        {
            bool hasLivingTarget = TryResolveLivingPlayer(TargetPlayer, out FusionNetworkPlayer target);
            if (hasLivingTarget)
                FaceTarget(target.transform.position);

            float normalized = GetStateElapsed(attackDuration) / Mathf.Max(0.01f, attackDuration);
            if (hasLivingTarget && !DamageApplied && normalized >= attackImpactNormalizedTime)
            {
                DamageApplied = true;
                PlayerRef victim = TargetPlayer;
                target.TakeDamage(attackDamage);
                if (target.IsDeadOrPending)
                {
                    LethalAttack = true;
                    TargetPlayer = PlayerRef.None;
                    JumpscareVictim = victim;
                    JumpscareSequence++;
                }
            }

            if (!StateTimer.Expired(Runner))
                return;

            AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
            if (LethalAttack)
            {
                ClearTargetAndScan();
                return;
            }

            if (hasLivingTarget)
            {
                TryRetargetToCloserVisiblePlayer(target);
                EnterAuthorityState(GeoMonsterState.Chase);
                return;
            }

            if (!TryAcquireClosestVisiblePlayer())
                ClearTargetAndScan();
        }

        private void UpdateAuthorityLockerKill()
        {
            if (!TryResolveWitnessedLocker(out LockerController locker) ||
                !TryResolveLivingPlayer(WitnessedLockerPlayer, out FusionNetworkPlayer player) ||
                !locker.IsPlayerInside(player))
            {
                CancelLockerKillAndResume();
                return;
            }

            Transform attackPoint = locker.MonsterAttackPoint;
            if (attackPoint == null)
            {
                CancelLockerKillAndResume();
                return;
            }

            _agent.SetDestination(attackPoint.position);
            if (HorizontalDistance(transform.position, attackPoint.position) > lockerKillRange)
                return;

            PlayerRef victim = WitnessedLockerPlayer;
            if (!player.KillInstantlyAuthoritative())
            {
                CancelLockerKillAndResume();
                return;
            }

            ClearWitnessedLocker();
            TargetPlayer = PlayerRef.None;
            JumpscareVictim = victim;
            JumpscareSequence++;
            ClearTargetAndScan();
        }

        private void EnterAuthorityState(GeoMonsterState newState)
        {
            CurrentState = newState;
            FootstepElapsed = 0f;

            switch (newState)
            {
                case GeoMonsterState.IdleScan:
                    StateTimer = TickTimer.CreateFromSeconds(Runner, ScanDuration);
                    LostTargetTimer = TickTimer.None;
                    RetargetTimer = TickTimer.None;
                    PatrolDestination = transform.position;
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    break;
                case GeoMonsterState.Patrol:
                    StateTimer = TickTimer.None;
                    _agent.isStopped = false;
                    _agent.speed = patrolSpeed;
                    _agent.stoppingDistance = waypointStoppingDistance;
                    break;
                case GeoMonsterState.Chase:
                    StateTimer = TickTimer.None;
                    RetargetTimer = TickTimer.CreateFromSeconds(Runner, retargetInterval);
                    _agent.isStopped = false;
                    _agent.speed = chaseSpeed;
                    _agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.8f);
                    break;
                case GeoMonsterState.LockerKill:
                    StateTimer = TickTimer.None;
                    LostTargetTimer = TickTimer.None;
                    RetargetTimer = TickTimer.None;
                    _agent.isStopped = false;
                    _agent.speed = chaseSpeed;
                    _agent.stoppingDistance = Mathf.Max(0.05f, lockerKillRange * 0.8f);
                    break;
                case GeoMonsterState.Attack:
                    StateTimer = TickTimer.CreateFromSeconds(Runner, attackDuration);
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    DamageApplied = false;
                    LethalAttack = false;
                    AttackSequence++;
                    EmitAudioEvent(MonsterAudioEvent.Attack);
                    break;
            }
        }

        private bool TryAcquireClosestVisiblePlayer()
        {
            PlayerRef bestPlayer = PlayerRef.None;
            float bestDistance = float.PositiveInfinity;

            foreach (PlayerRef playerRef in Runner.ActivePlayers)
            {
                if (!TryResolveLivingPlayer(playerRef, out FusionNetworkPlayer candidate))
                    continue;

                float distance = HorizontalDistance(transform.position, candidate.transform.position);
                bool visible = CanSee(candidate, true);
                if (GeoMonsterTargetSelection.ShouldSelectCandidate(true, true, visible, distance, bestDistance))
                {
                    bestDistance = distance;
                    bestPlayer = playerRef;
                }
            }

            if (bestPlayer == PlayerRef.None)
                return false;

            TargetPlayer = bestPlayer;
            LostTargetTimer = TickTimer.None;
            EnterAuthorityState(GeoMonsterState.Chase);
            return true;
        }

        private bool TryRetargetToCloserVisiblePlayer(FusionNetworkPlayer currentTarget)
        {
            if (currentTarget == null)
                return false;

            float bestDistance = HorizontalDistance(transform.position, currentTarget.transform.position);
            PlayerRef bestPlayer = TargetPlayer;

            foreach (PlayerRef playerRef in Runner.ActivePlayers)
            {
                if (playerRef == TargetPlayer || !TryResolveLivingPlayer(playerRef, out FusionNetworkPlayer candidate))
                {
                    continue;
                }

                float candidateDistance = HorizontalDistance(transform.position, candidate.transform.position);
                bool visible = CanSee(candidate, true);
                if (GeoMonsterTargetSelection.ShouldSelectCandidate(true, true, visible, candidateDistance, bestDistance))
                {
                    bestDistance = candidateDistance;
                    bestPlayer = playerRef;
                }
            }

            if (bestPlayer == TargetPlayer)
                return false;

            TargetPlayer = bestPlayer;
            LostTargetTimer = TickTimer.None;
            return true;
        }

        private bool TryResolveLivingPlayer(PlayerRef playerRef, out FusionNetworkPlayer player)
        {
            if (!TryResolvePlayer(playerRef, out player))
                return false;
            return !player.IsDeadOrPending;
        }

        private bool TryResolvePlayer(PlayerRef playerRef, out FusionNetworkPlayer player)
        {
            player = null;
            if (playerRef == PlayerRef.None || Runner == null || !Runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject))
                return false;

            player = playerObject.GetComponent<FusionNetworkPlayer>();
            return player != null;
        }

        private bool CanSee(FusionNetworkPlayer player, bool requireFieldOfView)
        {
            if (player == null || player.IsDeadOrPending)
                return false;

            Vector3 targetPoint = player.transform.position + Vector3.up * 1.1f;
            Vector3 toTarget = targetPoint - detectionOrigin.position;
            if (toTarget.sqrMagnitude > detectionDistance * detectionDistance)
                return false;

            if (requireFieldOfView)
            {
                Vector3 planarDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
                if (Vector3.Angle(detectionOrigin.forward, planarDirection) > horizontalFieldOfView * 0.5f)
                    return false;
            }

            return HasUnobstructedRay(targetPoint, player.transform.root);
        }

        private bool HasChaseLineOfSight(FusionNetworkPlayer player)
        {
            if (player == null)
                return false;
            Vector3 targetPoint = player.transform.position + Vector3.up * 1.1f;
            if ((targetPoint - detectionOrigin.position).sqrMagnitude > detectionDistance * detectionDistance * 2.25f)
                return false;
            return HasUnobstructedRay(targetPoint, player.transform.root);
        }

        public void TryWitnessLockerEntryAuthoritative(
            LockerController locker,
            FusionNetworkPlayer enteringPlayer)
        {
            if (!HasStateAuthority ||
                CurrentState != GeoMonsterState.Chase ||
                locker == null ||
                enteringPlayer == null)
            {
                return;
            }

            PlayerRef enteringPlayerRef = enteringPlayer.Object.InputAuthority;
            if (TargetPlayer != enteringPlayerRef ||
                !locker.IsPlayerInside(enteringPlayer) ||
                !HasChaseLineOfSight(enteringPlayer))
            {
                return;
            }

            WitnessedLocker = locker.Id;
            WitnessedLockerPlayer = enteringPlayerRef;
            TargetPlayer = enteringPlayerRef;
            EnterAuthorityState(GeoMonsterState.LockerKill);
        }

        public void CancelWitnessedLockerAuthoritative(
            LockerController locker,
            PlayerRef playerRef)
        {
            if (!HasStateAuthority ||
                locker == null ||
                WitnessedLocker != locker.Id ||
                WitnessedLockerPlayer != playerRef)
            {
                return;
            }

            CancelLockerKillAndResume();
        }

        private void CancelLockerKillAndResume()
        {
            PlayerRef previousPlayer = WitnessedLockerPlayer;
            ClearWitnessedLocker();
            if (TryResolveLivingPlayer(previousPlayer, out FusionNetworkPlayer player) &&
                HasChaseLineOfSight(player))
            {
                TargetPlayer = previousPlayer;
                EnterAuthorityState(GeoMonsterState.Chase);
                return;
            }

            ClearTargetAndScan();
        }

        private bool TryResolveWitnessedLocker(out LockerController locker)
        {
            locker = null;
            if (!WitnessedLocker.IsValid ||
                Runner == null ||
                !Runner.TryFindBehaviour(WitnessedLocker, out NetworkBehaviour behaviour))
            {
                return false;
            }

            locker = behaviour as LockerController;
            return locker != null;
        }

        private void ClearWitnessedLocker()
        {
            WitnessedLocker = default;
            WitnessedLockerPlayer = PlayerRef.None;
        }

        private bool HasUnobstructedRay(Vector3 targetPoint, Transform playerRoot)
        {
            Vector3 direction = targetPoint - detectionOrigin.position;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
                return true;

            RaycastHit[] hits = Physics.RaycastAll(
                detectionOrigin.position,
                direction / distance,
                distance,
                obstructionMask,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(transform))
                    continue;
                return hit.transform.root == playerRoot;
            }

            return true;
        }

        private void SelectRandomReachableWaypoint()
        {
            if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            {
                EnterAuthorityState(GeoMonsterState.IdleScan);
                return;
            }

            int startIndex = Random.Range(0, patrolWaypoints.Length);
            for (int offset = 0; offset < patrolWaypoints.Length; offset++)
            {
                int index = (startIndex + offset) % patrolWaypoints.Length;
                if (patrolWaypoints.Length > 1 && index == _lastWaypointIndex)
                    continue;

                Transform waypoint = patrolWaypoints[index];
                if (waypoint == null ||
                    HorizontalDistance(transform.position, waypoint.position) <= waypointStoppingDistance * 1.5f)
                {
                    continue;
                }

                NavMeshPath path = new();
                if (!_agent.CalculatePath(waypoint.position, path) || path.status != NavMeshPathStatus.PathComplete)
                    continue;

                _lastWaypointIndex = index;
                PatrolDestination = waypoint.position;
                EnterAuthorityState(GeoMonsterState.Patrol);
                _agent.SetPath(path);
                return;
            }

            EnterAuthorityState(GeoMonsterState.IdleScan);
        }

        private void ClearTargetAndScan()
        {
            TargetPlayer = PlayerRef.None;
            LostTargetTimer = TickTimer.None;
            EnterAuthorityState(GeoMonsterState.IdleScan);
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    540f * Runner.DeltaTime);
            }
        }

        private void UpdateAuthorityMovementAudio()
        {
            bool moving = (CurrentState == GeoMonsterState.Patrol ||
                    CurrentState == GeoMonsterState.Chase ||
                    CurrentState == GeoMonsterState.LockerKill) &&
                !_agent.isStopped &&
                _agent.velocity.sqrMagnitude > 0.04f;
            if (!moving)
            {
                FootstepElapsed = 0f;
                return;
            }

            FootstepElapsed += Runner.DeltaTime;
            float interval = CurrentState == GeoMonsterState.Chase ||
                CurrentState == GeoMonsterState.LockerKill
                    ? runFootstepInterval
                    : walkFootstepInterval;
            if (FootstepElapsed < interval)
                return;

            FootstepElapsed %= interval;
            EmitAudioEvent(CurrentState == GeoMonsterState.Chase
                ? MonsterAudioEvent.RunFootstep
                : MonsterAudioEvent.WalkFootstep);
        }

        private void AdvanceAuthorityMovement()
        {
            if ((CurrentState != GeoMonsterState.Patrol &&
                    CurrentState != GeoMonsterState.Chase &&
                    CurrentState != GeoMonsterState.LockerKill) ||
                _agent.isStopped)
            {
                _agent.nextPosition = transform.position;
                return;
            }

            Vector3 velocity = Vector3.ProjectOnPlane(_agent.desiredVelocity, Vector3.up);
            if (velocity.sqrMagnitude <= 0.0001f)
            {
                _agent.nextPosition = transform.position;
                return;
            }

            float configuredSpeed = CurrentState == GeoMonsterState.Chase ||
                CurrentState == GeoMonsterState.LockerKill
                    ? chaseSpeed
                    : patrolSpeed;
            Vector3 movement = Vector3.ClampMagnitude(velocity, configuredSpeed) * Runner.DeltaTime;
            transform.position += movement;
            _agent.nextPosition = transform.position;

            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _agent.angularSpeed * Runner.DeltaTime);
        }

        private void EmitAudioEvent(MonsterAudioEvent audioEvent)
        {
            AudioEventCode = (byte)audioEvent;
            AudioEventSequence++;
        }

        private void PresentState(bool force)
        {
            bool stateChanged = !_presentationInitialized || CurrentState != _presentedState;
            bool attackRestarted = CurrentState == GeoMonsterState.Attack && AttackSequence != _lastPresentedAttackSequence;
            if (!force && !stateChanged && !attackRestarted)
                return;

            _presentationInitialized = true;
            _presentedState = CurrentState;
            _lastPresentedAttackSequence = AttackSequence;
            _renderStateElapsed = GetStateElapsed(GetStateDuration(CurrentState));

            int stateHash = CurrentState switch
            {
                GeoMonsterState.IdleScan => IdleScanState,
                GeoMonsterState.Patrol => WalkState,
                GeoMonsterState.Chase => RunState,
                GeoMonsterState.LockerKill => RunState,
                GeoMonsterState.Attack => AttackState,
                _ => IdleScanState
            };
            float transitionDuration = CurrentState == GeoMonsterState.Attack ? 0.05f : 0.08f;
            float normalizedStart = GetStateDuration(CurrentState) > 0f
                ? Mathf.Clamp01(_renderStateElapsed / GetStateDuration(CurrentState))
                : 0f;
            animator.CrossFade(stateHash, transitionDuration, 0, normalizedStart);
        }

        private void PresentAudioEvents()
        {
            if (AudioEventSequence == _lastPresentedAudioSequence)
                return;

            _lastPresentedAudioSequence = AudioEventSequence;
            MonsterAudioEvent audioEvent = (MonsterAudioEvent)AudioEventCode;
            if (audioEvent == MonsterAudioEvent.Attack)
            {
                if (attackAudioSource != null && attackClip != null)
                    attackAudioSource.PlayOneShot(attackClip);
                return;
            }

            if (movementAudioSource == null || footstepClips == null || footstepClips.Length == 0)
                return;

            int clipIndex = AudioEventSequence % footstepClips.Length;
            AudioClip clip = footstepClips[clipIndex];
            bool running = audioEvent == MonsterAudioEvent.RunFootstep;
            movementAudioSource.pitch = running ? 1.05f : 0.92f;
            movementAudioSource.PlayOneShot(clip, running ? 1f : 0.78f);
        }

        private void PresentJumpscareEvent()
        {
            if (JumpscareSequence == _lastPresentedJumpscareSequence)
                return;

            _lastPresentedJumpscareSequence = JumpscareSequence;
            if (!TryResolvePlayer(JumpscareVictim, out FusionNetworkPlayer player) || !player.HasInputAuthority)
                return;

            player.BeginLocalDeathSequence(jumpscareVideo);
        }

        private float GetStateElapsed(float duration)
        {
            if (duration <= 0f || Runner == null)
                return 0f;
            float remaining = StateTimer.RemainingTime(Runner) ?? 0f;
            return Mathf.Clamp(duration - remaining, 0f, duration);
        }

        private float GetStateDuration(GeoMonsterState state)
        {
            return state switch
            {
                GeoMonsterState.IdleScan => ScanDuration,
                GeoMonsterState.Attack => attackDuration,
                _ => 0f
            };
        }

        private void ApplyScanPivot(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            float yaw;
            if (normalized < 0.25f)
                yaw = Mathf.Lerp(0f, -scanAngle, Smooth01(normalized / 0.25f));
            else if (normalized < 0.75f)
                yaw = Mathf.Lerp(-scanAngle, scanAngle, Smooth01((normalized - 0.25f) / 0.5f));
            else
                yaw = Mathf.Lerp(scanAngle, 0f, Smooth01((normalized - 0.75f) / 0.25f));
            detectionOrigin.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void ResolveReferences()
        {
            _agent ??= GetComponent<NavMeshAgent>();
            _networkTransform ??= GetComponent<NetworkTransform>();
            animator ??= GetComponentInChildren<Animator>(true);
        }

        private void EnsureAgentOnNavMesh()
        {
            if (_agent.isOnNavMesh)
                return;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                _agent.Warp(hit.position);
        }

        private bool ValidateReferences()
        {
            bool valid = _agent != null &&
                _networkTransform != null &&
                animator != null &&
                detectionOrigin != null &&
                attackOrigin != null;
            if (!valid && !_warnedMissingReferences)
            {
                Debug.LogError(
                    $"{nameof(GeoMonsterController)} on '{name}' requires a NetworkTransform, NavMeshAgent, Animator, DetectionOrigin, and AttackOrigin.",
                    this);
                _warnedMissingReferences = true;
            }
            return valid;
        }

        private static void ConfigureAudioSource(AudioSource source)
        {
            if (source == null)
                return;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1.5f;
            source.maxDistance = 22f;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = detectionOrigin != null ? detectionOrigin : transform;
            Vector3 forward = origin.forward;
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.75f);
            Gizmos.DrawWireSphere(origin.position, detectionDistance);
            Gizmos.DrawRay(origin.position, Quaternion.AngleAxis(-horizontalFieldOfView * 0.5f, Vector3.up) * forward * detectionDistance);
            Gizmos.DrawRay(origin.position, Quaternion.AngleAxis(horizontalFieldOfView * 0.5f, Vector3.up) * forward * detectionDistance);

            Transform attack = attackOrigin != null ? attackOrigin : transform;
            Gizmos.color = new Color(0.9f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(attack.position, attackRange);

            Transform target = Application.isPlaying ? CurrentTarget : null;
            if (target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin.position, target.position + Vector3.up * 1.1f);
            }
        }
    }
}
