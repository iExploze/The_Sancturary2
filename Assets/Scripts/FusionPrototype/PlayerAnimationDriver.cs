using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private const float MovingThreshold = 0.05f;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int CrouchedHash = Animator.StringToHash("Crouched");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        [SerializeField] private Animator animator;
        [SerializeField] private FusionNetworkPlayer player;
        [SerializeField] private NetworkCharacterController networkController;
        [SerializeField, Tooltip("Movement/network root used to convert world velocity into Animator coordinates.")]
        private Transform movementRoot;
        [SerializeField, Min(0f)] private float parameterDampTime = 0.12f;

        private bool _initialized;
        private bool _reportedMissingReferences;
        private bool _deathLatched;

        public Animator Animator => animator;

        public void Initialize()
        {
            ResolveReferences();
            if (animator == null || player == null || networkController == null || movementRoot == null)
            {
                if (!_reportedMissingReferences)
                {
                    Debug.LogError(
                        $"{nameof(PlayerAnimationDriver)} on '{name}' requires an Animator, " +
                        $"{nameof(FusionNetworkPlayer)}, {nameof(NetworkCharacterController)}, and a movement root.",
                        this);
                    _reportedMissingReferences = true;
                }

                _initialized = false;
                return;
            }

            animator.applyRootMotion = false;
            _initialized = true;
        }

        public void RenderAnimation(float deltaTime)
        {
            if (!_initialized)
                Initialize();
            if (!_initialized)
                return;

            Vector3 localVelocity = Quaternion.Inverse(movementRoot.rotation) * networkController.Velocity;
            float horizontalSpeed = new Vector2(localVelocity.x, localVelocity.z).magnitude;
            float referenceSpeed = Mathf.Max(0.01f, player.AnimationReferenceSpeed);
            float moveX = horizontalSpeed > MovingThreshold
                ? Mathf.Clamp(localVelocity.x / referenceSpeed, -1f, 1f)
                : 0f;
            float moveY = horizontalSpeed > MovingThreshold
                ? Mathf.Clamp(localVelocity.z / referenceSpeed, -1f, player.IsCrouched ? 1f : 2f)
                : 0f;
            if (player.IsDead || player.Health <= 0f)
                _deathLatched = true;
            bool dead = _deathLatched;

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            animator.SetFloat(MoveXHash, moveX, parameterDampTime, safeDeltaTime);
            animator.SetFloat(MoveYHash, moveY, parameterDampTime, safeDeltaTime);
            animator.SetBool(IsGroundedHash, networkController.Grounded);
            animator.SetBool(CrouchedHash, player.IsCrouched);
            animator.SetBool(DeadHash, dead);
        }

        private void ResolveReferences()
        {
            player ??= GetComponent<FusionNetworkPlayer>();
            networkController ??= GetComponent<NetworkCharacterController>();
            animator ??= GetComponentInChildren<Animator>(true);
            movementRoot ??= player != null ? player.transform : transform;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            if (animator != null)
                animator.applyRootMotion = false;
        }
#endif
    }
}
