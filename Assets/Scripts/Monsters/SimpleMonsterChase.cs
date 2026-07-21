using UnityEngine;

namespace TheSancturary.Monsters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimpleMonsterChase : MonoBehaviour
    {
        private static readonly int ChasingParameter = Animator.StringToHash("Chasing");

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float chaseRadius = 12f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0f)] private float stoppingDistance = 1.25f;
        [SerializeField, Min(0f)] private float turnSpeed = 360f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private CharacterController characterController;
        private bool isChasing;
        private Transform[] poseTransforms;
        private Vector3[] idlePositions;
        private Quaternion[] idleRotations;
        private Vector3[] idleScales;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public bool IsChasing => isChasing;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            CacheIdlePose();
            SetChasing(false, true);
        }

        private void Update()
        {
            if (target == null)
            {
                SetChasing(false);
                return;
            }

            var offset = target.position - transform.position;
            offset.y = 0f;

            var sqrDistance = offset.sqrMagnitude;
            var shouldChase = sqrDistance <= chaseRadius * chaseRadius;
            SetChasing(shouldChase);

            if (!shouldChase || sqrDistance <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            var direction = offset.normalized;
            var desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                turnSpeed * Time.deltaTime);

            var downwardSpeed = characterController.isGrounded ? -2f : -9.81f;
            var velocity = direction * moveSpeed + Vector3.up * downwardSpeed;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void SetChasing(bool chasing, bool force = false)
        {
            if (!force && isChasing == chasing)
            {
                return;
            }

            isChasing = chasing;
            if (animator != null)
            {
                animator.SetBool(ChasingParameter, chasing);
                animator.enabled = chasing;

                if (!chasing)
                {
                    RestoreIdlePose();
                }
            }
        }

        private void CacheIdlePose()
        {
            if (animator == null)
            {
                return;
            }

            poseTransforms = animator.GetComponentsInChildren<Transform>(true);
            idlePositions = new Vector3[poseTransforms.Length];
            idleRotations = new Quaternion[poseTransforms.Length];
            idleScales = new Vector3[poseTransforms.Length];

            for (var index = 0; index < poseTransforms.Length; index++)
            {
                var poseTransform = poseTransforms[index];
                idlePositions[index] = poseTransform.localPosition;
                idleRotations[index] = poseTransform.localRotation;
                idleScales[index] = poseTransform.localScale;
            }
        }

        private void RestoreIdlePose()
        {
            if (poseTransforms == null)
            {
                return;
            }

            for (var index = 0; index < poseTransforms.Length; index++)
            {
                var poseTransform = poseTransforms[index];
                poseTransform.localPosition = idlePositions[index];
                poseTransform.localRotation = idleRotations[index];
                poseTransform.localScale = idleScales[index];
            }
        }

        private void OnValidate()
        {
            chaseRadius = Mathf.Max(0f, chaseRadius);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            stoppingDistance = Mathf.Clamp(stoppingDistance, 0f, chaseRadius);
            turnSpeed = Mathf.Max(0f, turnSpeed);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.1f, 0.1f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, chaseRadius);
        }
    }
}
