using TheSancturary.FusionPrototype;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

namespace TheSancturary.Monsters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public sealed class GeoMonsterController : MonoBehaviour
    {
        public enum GeoMonsterState
        {
            IdleScan,
            Patrol,
            Chase,
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

        [Header("Attack")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
        [SerializeField, Min(0.1f)] private float attackDuration = 1.867f;
        [SerializeField, Range(0f, 1f)] private float attackImpactNormalizedTime = 0.45f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.5f;
        [SerializeField] private int attackDamage = 50;
        [SerializeField] private VideoClip jumpscareVideo;

        [Header("Audio")]
        [SerializeField] private AudioSource movementAudioSource;
        [SerializeField] private AudioSource attackAudioSource;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip attackClip;
        [SerializeField, Min(0.05f)] private float walkFootstepInterval = 0.62f;
        [SerializeField, Min(0.05f)] private float runFootstepInterval = 0.34f;

        private NavMeshAgent _agent;
        private FusionNetworkPlayer _target;
        private GeoMonsterState _state;
        private int _lastWaypointIndex = -1;
        private float _stateElapsed;
        private float _lostTargetElapsed;
        private float _nextAttackAllowedTime;
        private float _footstepElapsed;
        private float _nextPlayerSearchTime;
        private int _footstepIndex;
        private bool _damageApplied;
        private bool _warnedMissingReferences;

        public GeoMonsterState CurrentState => _state;
        public Transform CurrentTarget => _target != null ? _target.transform : null;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            animator ??= GetComponentInChildren<Animator>(true);
            ConfigureAudioSource(movementAudioSource);
            ConfigureAudioSource(attackAudioSource);
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            EnterState(GeoMonsterState.IdleScan);
        }

        private void Update()
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return;

            _stateElapsed += Time.deltaTime;

            if (_target == null || _target.IsDeadOrPending)
                FindLivingPlayer();

            switch (_state)
            {
                case GeoMonsterState.IdleScan:
                    UpdateIdleScan();
                    break;
                case GeoMonsterState.Patrol:
                    UpdatePatrol();
                    break;
                case GeoMonsterState.Chase:
                    UpdateChase();
                    break;
                case GeoMonsterState.Attack:
                    UpdateAttack();
                    break;
            }

            UpdateMovementAudio();
        }

        private void UpdateIdleScan()
        {
            float normalized = Mathf.Clamp01(_stateElapsed / ScanDuration);
            float yaw;
            if (normalized < 0.25f)
                yaw = Mathf.Lerp(0f, -scanAngle, Smooth01(normalized / 0.25f));
            else if (normalized < 0.75f)
                yaw = Mathf.Lerp(-scanAngle, scanAngle, Smooth01((normalized - 0.25f) / 0.5f));
            else
                yaw = Mathf.Lerp(scanAngle, 0f, Smooth01((normalized - 0.75f) / 0.25f));

            detectionOrigin.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (TryAcquireVisiblePlayer())
                return;

            if (_stateElapsed >= ScanDuration)
            {
                detectionOrigin.localRotation = Quaternion.identity;
                SelectRandomReachableWaypoint();
            }
        }

        private void UpdatePatrol()
        {
            detectionOrigin.localRotation = Quaternion.identity;
            if (TryAcquireVisiblePlayer())
                return;

            if (!_agent.pathPending && (_agent.remainingDistance <= waypointStoppingDistance || _agent.pathStatus != NavMeshPathStatus.PathComplete))
                EnterState(GeoMonsterState.IdleScan);
        }

        private void UpdateChase()
        {
            detectionOrigin.localRotation = Quaternion.identity;
            if (_target == null || _target.IsDeadOrPending)
            {
                ClearTargetAndScan();
                return;
            }

            Vector3 targetPosition = _target.transform.position;
            float distance = HorizontalDistance(attackOrigin.position, targetPosition);
            if (distance <= attackRange && Time.time >= _nextAttackAllowedTime)
            {
                EnterState(GeoMonsterState.Attack);
                return;
            }

            _agent.SetDestination(targetPosition);
            if (HasChaseLineOfSight(_target))
                _lostTargetElapsed = 0f;
            else
                _lostTargetElapsed += Time.deltaTime;

            if (_lostTargetElapsed >= lostTargetGracePeriod)
                ClearTargetAndScan();
        }

        private void UpdateAttack()
        {
            if (_target == null || _target.IsDeadOrPending)
            {
                ClearTargetAndScan();
                return;
            }

            FaceTarget(_target.transform.position);
            float normalized = Mathf.Clamp01(_stateElapsed / Mathf.Max(0.01f, attackDuration));
            if (!_damageApplied && normalized >= attackImpactNormalizedTime)
            {
                _damageApplied = true;
                _target.TakeDamage(attackDamage);
                if (_target.IsDeadOrPending)
                {
                    _target.BeginLocalDeathSequence(jumpscareVideo);
                    ClearTargetAndScan();
                    return;
                }
            }

            if (_stateElapsed < attackDuration)
                return;

            _nextAttackAllowedTime = Time.time + attackCooldown;
            float distance = HorizontalDistance(attackOrigin.position, _target.transform.position);
            EnterState(distance <= attackRange && Time.time >= _nextAttackAllowedTime
                ? GeoMonsterState.Attack
                : GeoMonsterState.Chase);
        }

        private void EnterState(GeoMonsterState newState)
        {
            _state = newState;
            _stateElapsed = 0f;
            _footstepElapsed = 0f;

            switch (newState)
            {
                case GeoMonsterState.IdleScan:
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    animator.CrossFade(IdleScanState, 0.08f, 0, 0f);
                    break;
                case GeoMonsterState.Patrol:
                    _agent.isStopped = false;
                    _agent.speed = patrolSpeed;
                    _agent.stoppingDistance = waypointStoppingDistance;
                    animator.CrossFade(WalkState, 0.1f, 0, 0f);
                    break;
                case GeoMonsterState.Chase:
                    _agent.isStopped = false;
                    _agent.speed = chaseSpeed;
                    _agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.8f);
                    animator.CrossFade(RunState, 0.08f, 0, 0f);
                    break;
                case GeoMonsterState.Attack:
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _damageApplied = false;
                    animator.CrossFade(AttackState, 0.05f, 0, 0f);
                    if (attackAudioSource != null && attackClip != null)
                        attackAudioSource.PlayOneShot(attackClip);
                    break;
            }
        }

        private bool TryAcquireVisiblePlayer()
        {
            if (_target == null || _target.IsDeadOrPending)
                FindLivingPlayer();
            if (_target == null || !CanSee(_target, true))
                return false;

            _lostTargetElapsed = 0f;
            EnterState(GeoMonsterState.Chase);
            return true;
        }

        private void FindLivingPlayer()
        {
            _target = null;
            if (Time.time < _nextPlayerSearchTime)
                return;

            _nextPlayerSearchTime = Time.time + 0.25f;
            FusionNetworkPlayer[] players = Object.FindObjectsByType<FusionNetworkPlayer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            float nearestDistance = float.PositiveInfinity;
            foreach (FusionNetworkPlayer candidate in players)
            {
                if (candidate.IsDeadOrPending)
                    continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _target = candidate;
                }
            }
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

        private bool HasUnobstructedRay(Vector3 targetPoint, Transform playerRoot)
        {
            Vector3 direction = targetPoint - detectionOrigin.position;
            float distance = direction.magnitude;
            RaycastHit[] hits = Physics.RaycastAll(detectionOrigin.position, direction / distance, distance, obstructionMask, QueryTriggerInteraction.Ignore);
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
                EnterState(GeoMonsterState.IdleScan);
                return;
            }

            int startIndex = Random.Range(0, patrolWaypoints.Length);
            for (int offset = 0; offset < patrolWaypoints.Length; offset++)
            {
                int index = (startIndex + offset) % patrolWaypoints.Length;
                if (patrolWaypoints.Length > 1 && index == _lastWaypointIndex)
                    continue;
                Transform waypoint = patrolWaypoints[index];
                if (waypoint == null)
                    continue;
                if (HorizontalDistance(transform.position, waypoint.position) <= waypointStoppingDistance * 1.5f)
                    continue;

                NavMeshPath path = new();
                if (!_agent.CalculatePath(waypoint.position, path) || path.status != NavMeshPathStatus.PathComplete)
                    continue;

                _lastWaypointIndex = index;
                EnterState(GeoMonsterState.Patrol);
                _agent.SetPath(path);
                return;
            }

            EnterState(GeoMonsterState.IdleScan);
        }

        private void ClearTargetAndScan()
        {
            _target = null;
            _lostTargetElapsed = 0f;
            EnterState(GeoMonsterState.IdleScan);
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 540f * Time.deltaTime);
        }

        private void UpdateMovementAudio()
        {
            bool moving = (_state == GeoMonsterState.Patrol || _state == GeoMonsterState.Chase)
                && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.04f;
            if (!moving || movementAudioSource == null || footstepClips == null || footstepClips.Length == 0)
            {
                _footstepElapsed = 0f;
                return;
            }

            _footstepElapsed += Time.deltaTime;
            float interval = _state == GeoMonsterState.Chase ? runFootstepInterval : walkFootstepInterval;
            if (_footstepElapsed < interval)
                return;

            _footstepElapsed %= interval;
            AudioClip clip = footstepClips[_footstepIndex++ % footstepClips.Length];
            movementAudioSource.pitch = _state == GeoMonsterState.Chase ? 1.05f : 0.92f;
            movementAudioSource.PlayOneShot(clip, _state == GeoMonsterState.Chase ? 1f : 0.78f);
        }

        private bool ValidateReferences()
        {
            bool valid = animator != null && detectionOrigin != null && attackOrigin != null;
            if (!valid && !_warnedMissingReferences)
            {
                Debug.LogError($"{nameof(GeoMonsterController)} on '{name}' is missing its Animator, DetectionOrigin, or AttackOrigin reference.", this);
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

            if (_target != null)
            {
                Gizmos.color = CanSee(_target, false) ? Color.green : Color.red;
                Gizmos.DrawLine(origin.position, _target.transform.position + Vector3.up * 1.1f);
            }
        }
    }
}
