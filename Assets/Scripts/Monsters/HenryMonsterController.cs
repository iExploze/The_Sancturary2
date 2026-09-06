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
    public sealed class HenryMonsterController : NetworkBehaviour
    {
        public enum HenryMonsterState : byte
        {
            Idle,
            Patrol,
            TransformToAngry,
            Chase,
            Attack,
            TransformToCalm
        }

        private const float IdleDuration = 1f;
        private static readonly int CalmIdleState = Animator.StringToHash("CalmIdle");
        private static readonly int CalmWalkState = Animator.StringToHash("CalmWalk");
        private static readonly int TransformToAngryState = Animator.StringToHash("TransformToAngry");
        private static readonly int AngryWalkState = Animator.StringToHash("AngryWalk");
        private static readonly int AngryAttackState = Animator.StringToHash("AngryAttack");
        private static readonly int AttackTimeParameter = Animator.StringToHash("AttackTime");
        private static readonly int LocomotionSpeedParameter = Animator.StringToHash("LocomotionSpeed");

        [Header("Required References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform detectionOrigin;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private Transform[] patrolWaypoints;

        [Header("Navigation")]
        [SerializeField, Min(0.1f)] private float patrolSpeed = 3.2f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 4f;
        [SerializeField, Min(0.05f)] private float waypointStoppingDistance = 0.4f;

        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float detectionDistance = 7f;
        [SerializeField, Range(1f, 180f)] private float horizontalFieldOfView = 180f;
        [SerializeField, Min(0.001f)] private float movementThreshold = 0.12f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Range(0.25f, 0.5f)] private float lostTargetGracePeriod = 0.35f;

        [Header("Transformation")]
        [SerializeField, Min(0.05f)] private float transformationDuration = 3.292f;
        [SerializeField] private AnimationClip transformationClip;
        [SerializeField, Range(-180f, 180f)] private float angryVisualYawOffset = -90f;

        [Header("Attack")]
        [SerializeField, Range(0.9f, 1.2f)] private float attackRange = 1.05f;
        [SerializeField] private AnimationClip attackAnimation;
        [SerializeField, Range(0f, 1f)] private float attackImpactNormalizedTime = 0.6f;
        [Tooltip("Time of the audible strike within the sound file, aligned with the animation impact.")]
        [SerializeField, Min(0f)] private float attackSoundImpactTime = 0.3f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.9f;
        [SerializeField] private int attackDamage = 25;
        [SerializeField] private VideoClip jumpscareVideo;

        [Header("Audio")]
        [SerializeField] private AudioSource movementAudioSource;
        [SerializeField] private AudioSource attackAudioSource;
        [SerializeField] private AudioSource transformationAudioSource;
        [SerializeField] private AudioClip walkingClip;
        [SerializeField] private AudioClip attackClip;
        [SerializeField] private AudioClip transformationGrowlClip;
        [SerializeField, Range(0f, 1f)] private float transformationGrowlVolume = 0.8f;

        [Networked] public HenryMonsterState CurrentState { get; private set; }
        [Networked] public PlayerRef TargetPlayer { get; private set; }
        [Networked] public Vector3 PatrolDestination { get; private set; }
        [Networked] public NetworkBool IsMoving { get; private set; }
        [Networked] private TickTimer StateTimer { get; set; }
        [Networked] private TickTimer LostTargetTimer { get; set; }
        [Networked] private TickTimer AttackCooldownTimer { get; set; }
        [Networked] private NetworkBool DamageApplied { get; set; }
        [Networked] private NetworkBool LethalAttack { get; set; }
        [Networked] private byte AttackSequence { get; set; }
        [Networked] private byte JumpscareSequence { get; set; }
        [Networked] private PlayerRef JumpscareVictim { get; set; }

        private NavMeshAgent _agent;
        private NetworkTransform _networkTransform;
        private int _lastWaypointIndex = -1;
        private byte _lastPresentedAttackSequence;
        private byte _lastPresentedAttackAudioSequence;
        private byte _lastPresentedJumpscareSequence;
        private HenryMonsterState _presentedState;
        private bool _presentationInitialized;
        private bool _warnedMissingReferences;
        private bool _attackAudioPlayed;
        private HenryMonsterState _presentedTransformationAudioState;
        private int? _presentedTransformationAudioEndTick;
        private Quaternion _calmVisualLocalRotation;

        private float AttackDuration => attackAnimation.length;

        public Transform CurrentTarget =>
            TryResolvePlayer(TargetPlayer, out FusionNetworkPlayer player) ? player.transform : null;

        private void Awake()
        {
            ResolveReferences();
            if (animator != null)
                _calmVisualLocalRotation = animator.transform.localRotation;
            ConfigureAudioSource(movementAudioSource, true);
            ConfigureAudioSource(attackAudioSource, false);
            ConfigureAudioSource(transformationAudioSource, false, 2f);
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
                EnterAuthorityState(HenryMonsterState.Idle);
            }

            _lastPresentedAttackSequence = AttackSequence;
            _lastPresentedAttackAudioSequence = AttackSequence;
            _lastPresentedJumpscareSequence = JumpscareSequence;
            _attackAudioPlayed = false;
            PresentState(true);
            PresentMovementAudio(true);
            PresentTransformationAudio(true);
            PresentAttackEvent();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            switch (CurrentState)
            {
                case HenryMonsterState.Idle:
                    UpdateAuthorityIdle();
                    break;
                case HenryMonsterState.Patrol:
                    UpdateAuthorityPatrol();
                    break;
                case HenryMonsterState.TransformToAngry:
                    UpdateAuthorityTransformToAngry();
                    break;
                case HenryMonsterState.Chase:
                    UpdateAuthorityChase();
                    break;
                case HenryMonsterState.Attack:
                    UpdateAuthorityAttack();
                    break;
                case HenryMonsterState.TransformToCalm:
                    UpdateAuthorityTransformToCalm();
                    break;
            }

            AdvanceAuthorityMovement();
            IsMoving = (CurrentState == HenryMonsterState.Patrol || CurrentState == HenryMonsterState.Chase) &&
                !_agent.isStopped && _agent.desiredVelocity.sqrMagnitude > 0.04f;
        }

        public override void Render()
        {
            PresentState(false);
            PresentMovementAudio(false);
            PresentTransformationAudio(false);
            PresentAttackEvent();
            PresentJumpscareEvent();
        }

        private void UpdateAuthorityIdle()
        {
            if (TryAcquireClosestMovingPlayer())
                return;
            if (StateTimer.Expired(Runner))
                SelectRandomReachableWaypoint();
        }

        private void UpdateAuthorityPatrol()
        {
            if (TryAcquireClosestMovingPlayer())
                return;
            if (!_agent.pathPending &&
                (_agent.remainingDistance <= waypointStoppingDistance || _agent.pathStatus != NavMeshPathStatus.PathComplete))
                EnterAuthorityState(HenryMonsterState.Idle);
        }

        private void UpdateAuthorityTransformToAngry()
        {
            if (!TryResolveChaseablePlayer(TargetPlayer, out FusionNetworkPlayer target))
            {
                BeginCalmTransition();
                return;
            }

            FaceTarget(target.transform.position);
            if (!TrackLineOfSightGrace(target))
                return;
            if (StateTimer.Expired(Runner))
                EnterAuthorityState(HenryMonsterState.Chase);
        }

        private void UpdateAuthorityChase()
        {
            if (!TryResolveChaseablePlayer(TargetPlayer, out FusionNetworkPlayer target))
            {
                BeginCalmTransition();
                return;
            }

            bool hasLineOfSight = HasChaseLineOfSight(target);
            float distance = HorizontalDistance(attackOrigin.position, target.transform.position);
            if (hasLineOfSight && distance <= attackRange && AttackCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                FaceTargetImmediately(target.transform.position);
                EnterAuthorityState(HenryMonsterState.Attack);
                return;
            }

            _agent.SetDestination(target.transform.position);
            TrackLineOfSightGrace(hasLineOfSight);
        }

        private void UpdateAuthorityAttack()
        {
            bool hasTarget = TryResolveChaseablePlayer(TargetPlayer, out FusionNetworkPlayer target);
            if (hasTarget)
                FaceTargetImmediately(target.transform.position);
            float normalized = GetStateElapsed(AttackDuration) / AttackDuration;
            if (!DamageApplied && normalized >= attackImpactNormalizedTime)
            {
                // Consume the contact once, including misses. Recovery is not another hit window.
                DamageApplied = true;
                if (hasTarget && HorizontalDistance(attackOrigin.position, target.transform.position) <= attackRange * 1.15f &&
                    HasChaseLineOfSight(target))
                {
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
            }

            if (!StateTimer.Expired(Runner))
                return;

            AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
            if (LethalAttack || !TryResolveChaseablePlayer(TargetPlayer, out _))
                BeginCalmTransition();
            else
                EnterAuthorityState(HenryMonsterState.Chase);
        }

        private void UpdateAuthorityTransformToCalm()
        {
            if (StateTimer.Expired(Runner))
                EnterAuthorityState(HenryMonsterState.Idle);
        }

        private bool TryAcquireClosestMovingPlayer()
        {
            PlayerRef bestPlayer = PlayerRef.None;
            float bestDistance = float.PositiveInfinity;
            foreach (PlayerRef playerRef in Runner.ActivePlayers)
            {
                bool valid = TryResolvePlayer(playerRef, out FusionNetworkPlayer candidate);
                bool alive = valid && !candidate.IsDeadOrPending;
                bool hidden = valid && candidate.IsHiddenInLocker;
                float distance = valid ? HorizontalDistance(transform.position, candidate.transform.position) : -1f;
                bool inRange = valid && distance <= detectionDistance;
                bool inFov = valid && IsInAcquisitionFieldOfView(candidate);
                bool lineOfSight = valid && HasAcquisitionLineOfSight(candidate);
                Vector3 velocity = valid ? candidate.AuthoritativeVelocity : Vector3.zero;
                bool canAcquire = HenryMonsterRules.CanAcquire(
                    valid, alive, hidden, inRange, inFov, lineOfSight, velocity, movementThreshold);
                if (HenryMonsterRules.ShouldSelectCandidate(canAcquire, distance, bestDistance))
                {
                    bestDistance = distance;
                    bestPlayer = playerRef;
                }
            }

            if (bestPlayer == PlayerRef.None)
                return false;

            TargetPlayer = bestPlayer;
            LostTargetTimer = TickTimer.None;
            EnterAuthorityState(HenryMonsterState.TransformToAngry);
            return true;
        }

        private bool IsInAcquisitionFieldOfView(FusionNetworkPlayer player)
        {
            Vector3 direction = Vector3.ProjectOnPlane(player.ReplicatedViewPosition - detectionOrigin.position, Vector3.up);
            return direction.sqrMagnitude <= Mathf.Epsilon ||
                Vector3.Angle(detectionOrigin.forward, direction) <= horizontalFieldOfView * 0.5f;
        }

        private bool HasAcquisitionLineOfSight(FusionNetworkPlayer player)
        {
            return player != null && HasUnobstructedRay(player.ReplicatedViewPosition, player.transform.root);
        }

        private bool HasChaseLineOfSight(FusionNetworkPlayer player)
        {
            return player != null && !player.IsHiddenInLocker &&
                HasUnobstructedRay(player.ReplicatedViewPosition, player.transform.root);
        }

        private bool TrackLineOfSightGrace(FusionNetworkPlayer target)
        {
            return TrackLineOfSightGrace(HasChaseLineOfSight(target));
        }

        private bool TrackLineOfSightGrace(bool hasLineOfSight)
        {
            if (hasLineOfSight)
            {
                LostTargetTimer = TickTimer.None;
                return true;
            }

            if (!LostTargetTimer.IsRunning)
                LostTargetTimer = TickTimer.CreateFromSeconds(Runner, lostTargetGracePeriod);
            if (LostTargetTimer.Expired(Runner))
            {
                BeginCalmTransition();
                return false;
            }
            return true;
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

        private bool TryResolveChaseablePlayer(PlayerRef playerRef, out FusionNetworkPlayer player)
        {
            bool valid = TryResolvePlayer(playerRef, out player);
            return HenryMonsterRules.CanMaintainChase(
                valid,
                valid && !player.IsDeadOrPending,
                valid && player.IsHiddenInLocker);
        }

        private bool TryResolvePlayer(PlayerRef playerRef, out FusionNetworkPlayer player)
        {
            player = null;
            if (playerRef == PlayerRef.None || Runner == null ||
                !Runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject))
                return false;
            player = playerObject.GetComponent<FusionNetworkPlayer>();
            return player != null;
        }

        private void SelectRandomReachableWaypoint()
        {
            if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            {
                EnterAuthorityState(HenryMonsterState.Idle);
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
                    continue;

                NavMeshPath path = new();
                if (!_agent.CalculatePath(waypoint.position, path) || path.status != NavMeshPathStatus.PathComplete)
                    continue;
                _lastWaypointIndex = index;
                PatrolDestination = waypoint.position;
                EnterAuthorityState(HenryMonsterState.Patrol);
                _agent.SetPath(path);
                return;
            }
            EnterAuthorityState(HenryMonsterState.Idle);
        }

        private void BeginCalmTransition()
        {
            TargetPlayer = PlayerRef.None;
            LostTargetTimer = TickTimer.None;
            if (CurrentState == HenryMonsterState.Idle ||
                CurrentState == HenryMonsterState.Patrol ||
                CurrentState == HenryMonsterState.TransformToCalm)
                EnterAuthorityState(HenryMonsterState.Idle);
            else
                EnterAuthorityState(HenryMonsterState.TransformToCalm);
        }

        private void EnterAuthorityState(HenryMonsterState newState)
        {
            CurrentState = newState;
            IsMoving = false;
            switch (newState)
            {
                case HenryMonsterState.Idle:
                    StateTimer = TickTimer.CreateFromSeconds(Runner, IdleDuration);
                    LostTargetTimer = TickTimer.None;
                    PatrolDestination = transform.position;
                    StopAgent();
                    break;
                case HenryMonsterState.Patrol:
                    StateTimer = TickTimer.None;
                    _agent.isStopped = false;
                    _agent.speed = patrolSpeed;
                    _agent.stoppingDistance = waypointStoppingDistance;
                    break;
                case HenryMonsterState.TransformToAngry:
                case HenryMonsterState.TransformToCalm:
                    StateTimer = TickTimer.CreateFromSeconds(Runner, transformationDuration);
                    StopAgent();
                    break;
                case HenryMonsterState.Chase:
                    StateTimer = TickTimer.None;
                    _agent.isStopped = false;
                    _agent.speed = chaseSpeed;
                    _agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.8f);
                    break;
                case HenryMonsterState.Attack:
                    StateTimer = TickTimer.CreateFromSeconds(Runner, AttackDuration);
                    StopAgent();
                    DamageApplied = false;
                    LethalAttack = false;
                    AttackSequence++;
                    break;
            }
        }

        private void StopAgent()
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.nextPosition = transform.position;
        }

        private void AdvanceAuthorityMovement()
        {
            if ((CurrentState != HenryMonsterState.Patrol && CurrentState != HenryMonsterState.Chase) ||
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

            float speed = CurrentState == HenryMonsterState.Chase ? chaseSpeed : patrolSpeed;
            transform.position += Vector3.ClampMagnitude(velocity, speed) * Runner.DeltaTime;
            _agent.nextPosition = transform.position;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(velocity),
                _agent.angularSpeed * Runner.DeltaTime);
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    _agent.angularSpeed * Runner.DeltaTime);
        }

        private void FaceTargetImmediately(Vector3 targetPosition)
        {
            Vector3 direction = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        private void PresentState(bool force)
        {
            PresentVisualFacing();
            bool stateChanged = !_presentationInitialized || CurrentState != _presentedState;
            bool attackRestarted = CurrentState == HenryMonsterState.Attack &&
                AttackSequence != _lastPresentedAttackSequence;
            if (CurrentState == HenryMonsterState.TransformToCalm)
            {
                _presentationInitialized = true;
                _presentedState = CurrentState;
                if (animator.enabled)
                    animator.enabled = false;
                SampleReverseTransformationPose();
                return;
            }

            if (!animator.enabled)
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
                force = true;
            }
            animator.SetFloat(LocomotionSpeedParameter, IsMoving ? 1f : 0f);
            if (CurrentState == HenryMonsterState.Attack)
                animator.SetFloat(AttackTimeParameter, GetPresentationElapsed(AttackDuration) / AttackDuration);
            if (!force && !stateChanged && !attackRestarted)
                return;

            _presentationInitialized = true;
            _presentedState = CurrentState;
            _lastPresentedAttackSequence = AttackSequence;
            float duration = GetStateDuration(CurrentState);
            float elapsed = GetPresentationElapsed(duration);
            int stateHash = CurrentState switch
            {
                HenryMonsterState.Idle => CalmIdleState,
                HenryMonsterState.Patrol => CalmWalkState,
                HenryMonsterState.TransformToAngry => TransformToAngryState,
                HenryMonsterState.Chase => AngryWalkState,
                HenryMonsterState.Attack => AngryAttackState,
                _ => CalmIdleState
            };
            float normalizedStart = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
            float fixedTimeOffset = CurrentState == HenryMonsterState.TransformToAngry
                ? normalizedStart * transformationDuration
                : 0f;
            animator.CrossFadeInFixedTime(
                stateHash,
                CurrentState == HenryMonsterState.Attack ? 0.03f : 0.06f,
                0,
                fixedTimeOffset);
        }

        private void PresentVisualFacing()
        {
            float angryWeight = CurrentState switch
            {
                HenryMonsterState.TransformToAngry =>
                    GetPresentationElapsed(transformationDuration) / transformationDuration,
                HenryMonsterState.Chase or HenryMonsterState.Attack => 1f,
                HenryMonsterState.TransformToCalm =>
                    1f - GetPresentationElapsed(transformationDuration) / transformationDuration,
                _ => 0f
            };
            float yaw = Mathf.LerpAngle(0f, angryVisualYawOffset, Mathf.Clamp01(angryWeight));
            animator.transform.localRotation = _calmVisualLocalRotation * Quaternion.Euler(0f, yaw, 0f);
        }

        private void SampleReverseTransformationPose()
        {
            if (transformationClip == null)
                return;

            float elapsed = GetPresentationElapsed(transformationDuration);
            float normalizedTime = 1f - Mathf.Clamp01(elapsed / transformationDuration);
            transformationClip.SampleAnimation(animator.gameObject, normalizedTime * transformationClip.length);
        }

        private void PresentMovementAudio(bool force)
        {
            if (movementAudioSource == null || walkingClip == null)
                return;
            bool shouldPlay = IsMoving &&
                (CurrentState == HenryMonsterState.Patrol || CurrentState == HenryMonsterState.Chase);
            if (shouldPlay && (!movementAudioSource.isPlaying || movementAudioSource.clip != walkingClip))
            {
                movementAudioSource.clip = walkingClip;
                movementAudioSource.loop = true;
                movementAudioSource.Play();
            }
            else if (!shouldPlay && (force || movementAudioSource.isPlaying))
            {
                movementAudioSource.Stop();
                movementAudioSource.clip = walkingClip;
                movementAudioSource.loop = true;
            }
        }

        private void PresentTransformationAudio(bool force)
        {
            if (transformationAudioSource == null || transformationGrowlClip == null)
                return;

            bool transforming = CurrentState == HenryMonsterState.TransformToAngry ||
                CurrentState == HenryMonsterState.TransformToCalm;
            if (!transforming)
            {
                if (transformationAudioSource.isPlaying)
                    transformationAudioSource.Stop();
                _presentedTransformationAudioEndTick = null;
                return;
            }

            // Both directions use the same authoritative timeline as the transformation pose.
            float elapsed = GetPresentationElapsed(transformationDuration);
            if (elapsed >= transformationDuration)
            {
                transformationAudioSource.Stop();
                return;
            }
            bool newTransition = force || _presentedTransformationAudioState != CurrentState ||
                _presentedTransformationAudioEndTick != StateTimer.TargetTick;
            if (!newTransition && transformationAudioSource.isPlaying)
                return;

            _presentedTransformationAudioState = CurrentState;
            _presentedTransformationAudioEndTick = StateTimer.TargetTick;
            transformationAudioSource.Stop();
            if (attackAudioSource != null)
                attackAudioSource.Stop();
            transformationAudioSource.clip = transformationGrowlClip;
            transformationAudioSource.loop = false;
            transformationAudioSource.pitch = transformationGrowlClip.length / transformationDuration;
            transformationAudioSource.volume = transformationGrowlVolume *
                (CurrentState == HenryMonsterState.TransformToCalm ? 0.55f : 1f);
            // Late joiners and audio-device recovery resume the current growl rather than replaying it.
            transformationAudioSource.time = elapsed * transformationAudioSource.pitch;
            transformationAudioSource.Play();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (transformationAudioSource != null)
                transformationAudioSource.Stop();
            if (attackAudioSource != null)
                attackAudioSource.Stop();
            if (movementAudioSource != null)
                movementAudioSource.Stop();
        }

        private void PresentAttackEvent()
        {
            if (AttackSequence != _lastPresentedAttackAudioSequence)
            {
                _lastPresentedAttackAudioSequence = AttackSequence;
                _attackAudioPlayed = false;
            }
            if (CurrentState != HenryMonsterState.Attack)
            {
                _attackAudioPlayed = true;
                return;
            }
            if (_attackAudioPlayed || attackAudioSource == null || attackClip == null)
                return;

            float soundTime = GetPresentationElapsed(AttackDuration) -
                AttackDuration * attackImpactNormalizedTime + attackSoundImpactTime;
            if (soundTime < 0f)
                return;
            _attackAudioPlayed = true;
            if (soundTime >= attackClip.length)
                return;

            // Seek to the same attack phase on late snapshots/joins; never stack old one-shots.
            attackAudioSource.Stop();
            attackAudioSource.clip = attackClip;
            attackAudioSource.loop = false;
            attackAudioSource.pitch = 1f;
            attackAudioSource.time = soundTime;
            attackAudioSource.Play();
        }

        private float GetPresentationElapsed(float duration)
        {
            if (duration <= 0f || Runner == null || !StateTimer.TargetTick.HasValue)
                return 0f;
            // Match the object's interpolated timeline rather than the client's simulation lead.
            double endTime = StateTimer.TargetTick.Value * (double)Runner.DeltaTime;
            double renderTime = HasStateAuthority ? Runner.LocalRenderTime : Object.RenderTime;
            return Mathf.Clamp((float)(duration - (endTime - renderTime)), 0f, duration);
        }

        private void PresentJumpscareEvent()
        {
            if (JumpscareSequence == _lastPresentedJumpscareSequence)
                return;
            _lastPresentedJumpscareSequence = JumpscareSequence;
            if (TryResolvePlayer(JumpscareVictim, out FusionNetworkPlayer player) && player.HasInputAuthority)
                player.BeginLocalDeathSequence(jumpscareVideo);
        }

        private float GetStateElapsed(float duration)
        {
            if (duration <= 0f || Runner == null)
                return 0f;
            float remaining = StateTimer.RemainingTime(Runner) ?? 0f;
            return Mathf.Clamp(duration - remaining, 0f, duration);
        }

        private float GetStateDuration(HenryMonsterState state)
        {
            return state switch
            {
                HenryMonsterState.Idle => IdleDuration,
                HenryMonsterState.TransformToAngry => transformationDuration,
                HenryMonsterState.Attack => AttackDuration,
                HenryMonsterState.TransformToCalm => transformationDuration,
                _ => 0f
            };
        }

        private void ResolveReferences()
        {
            _agent ??= GetComponent<NavMeshAgent>();
            _networkTransform ??= GetComponent<NetworkTransform>();
            animator ??= GetComponentInChildren<Animator>(true);
        }

        private bool ValidateReferences()
        {
            bool valid = _agent != null && _networkTransform != null && animator != null && transformationClip != null &&
                attackAnimation != null && attackAnimation.length > 0f &&
                detectionOrigin != null && attackOrigin != null;
            if (!valid && !_warnedMissingReferences)
            {
                Debug.LogError(
                    $"{nameof(HenryMonsterController)} on '{name}' requires a NetworkTransform, NavMeshAgent, Animator, transformation and attack clips, DetectionOrigin, and AttackOrigin.",
                    this);
                _warnedMissingReferences = true;
            }
            return valid;
        }

        private void EnsureAgentOnNavMesh()
        {
            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                _agent.Warp(hit.position);
        }

        private static void ConfigureAudioSource(AudioSource source, bool linearRolloff, float minDistance = 0.8f)
        {
            if (source == null)
                return;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = linearRolloff ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = 16f;
            source.dopplerLevel = 0f;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = detectionOrigin != null ? detectionOrigin : transform;
            Gizmos.color = new Color(0.7f, 0.1f, 0.1f, 0.75f);
            Gizmos.DrawWireSphere(origin.position, detectionDistance);
            Vector3 left = Quaternion.Euler(0f, -horizontalFieldOfView * 0.5f, 0f) * origin.forward;
            Vector3 right = Quaternion.Euler(0f, horizontalFieldOfView * 0.5f, 0f) * origin.forward;
            Gizmos.DrawRay(origin.position, left * detectionDistance);
            Gizmos.DrawRay(origin.position, right * detectionDistance);
        }
    }
}
