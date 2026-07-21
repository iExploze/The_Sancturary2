using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private const string HoldingLayerName = "Upper Body Holding";
        private const float MovingThreshold = 0.05f;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int CrouchedHash = Animator.StringToHash("Crouched");
        private static readonly int SprintingHash = Animator.StringToHash("Sprinting");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int HoldingItemHash = Animator.StringToHash("HoldingItem");

        [SerializeField] private Animator animator;
        [SerializeField] private FusionNetworkPlayer player;
        [SerializeField] private NetworkCharacterController networkController;
        [SerializeField, Tooltip("Temporary presentation toggle until equipment state exists.")]
        private bool holdingItem = true;
        [SerializeField, Min(0f)] private float parameterDampTime = 0.12f;
        [SerializeField, Min(0f)] private float holdingLayerBlendSpeed = 8f;

        private int _holdingLayerIndex = -1;
        private bool _initialized;
        private bool _reportedMissingReferences;
        private bool _deathLatched;

        public Animator Animator => animator;
        public bool HoldingItem
        {
            get => holdingItem;
            set => holdingItem = value;
        }

        public void Initialize()
        {
            ResolveReferences();
            if (animator == null || player == null || networkController == null)
            {
                if (!_reportedMissingReferences)
                {
                    Debug.LogError(
                        $"{nameof(PlayerAnimationDriver)} on '{name}' requires an Animator, " +
                        $"{nameof(FusionNetworkPlayer)}, and {nameof(NetworkCharacterController)}.",
                        this);
                    _reportedMissingReferences = true;
                }

                _initialized = false;
                return;
            }

            animator.applyRootMotion = false;
            _holdingLayerIndex = animator.GetLayerIndex(HoldingLayerName);
            if (_holdingLayerIndex < 0 && !_reportedMissingReferences)
            {
                Debug.LogError(
                    $"Animator controller on '{name}' is missing the '{HoldingLayerName}' layer.",
                    this);
                _reportedMissingReferences = true;
            }

            _initialized = true;
        }

        public void RenderAnimation(float deltaTime)
        {
            if (!_initialized)
                Initialize();
            if (!_initialized)
                return;

            Vector3 localVelocity = transform.InverseTransformDirection(networkController.Velocity);
            float horizontalSpeed = new Vector2(localVelocity.x, localVelocity.z).magnitude;
            float referenceSpeed = Mathf.Max(0.01f, player.AnimationReferenceSpeed);
            float moveX = horizontalSpeed > MovingThreshold
                ? Mathf.Clamp(localVelocity.x / referenceSpeed, -1f, 1f)
                : 0f;
            float moveY = horizontalSpeed > MovingThreshold
                ? Mathf.Clamp(localVelocity.z / referenceSpeed, -1f, 1f)
                : 0f;
            if (player.Health <= 0f)
                _deathLatched = true;
            bool dead = _deathLatched;
            bool sprinting = player.IsSprinting && horizontalSpeed > MovingThreshold;

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            animator.SetFloat(MoveXHash, moveX, parameterDampTime, safeDeltaTime);
            animator.SetFloat(MoveYHash, moveY, parameterDampTime, safeDeltaTime);
            animator.SetFloat(SpeedHash, horizontalSpeed, parameterDampTime, safeDeltaTime);
            animator.SetFloat(VerticalSpeedHash, networkController.Velocity.y, parameterDampTime, safeDeltaTime);
            animator.SetBool(GroundedHash, networkController.Grounded);
            animator.SetBool(CrouchedHash, player.IsCrouched);
            animator.SetBool(SprintingHash, sprinting);
            animator.SetBool(DeadHash, dead);
            animator.SetBool(HoldingItemHash, holdingItem && !dead);

            if (_holdingLayerIndex >= 0)
            {
                float targetWeight = holdingItem && !dead ? 1f : 0f;
                float layerWeight = Mathf.MoveTowards(
                    animator.GetLayerWeight(_holdingLayerIndex),
                    targetWeight,
                    holdingLayerBlendSpeed * safeDeltaTime);
                animator.SetLayerWeight(_holdingLayerIndex, layerWeight);
            }
        }

        private void ResolveReferences()
        {
            player ??= GetComponent<FusionNetworkPlayer>();
            networkController ??= GetComponent<NetworkCharacterController>();
            animator ??= GetComponentInChildren<Animator>(true);
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
