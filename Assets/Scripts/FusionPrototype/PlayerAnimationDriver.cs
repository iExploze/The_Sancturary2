using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private const float MovingThreshold = 0.05f;
        private const float JumpTakeoffVelocityThreshold = 0.25f;
        private const float JumpApexVelocityThreshold = 0.15f;
        private const float AirborneConfirmationTime = 0.08f;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int CrouchedHash = Animator.StringToHash("Crouched");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int IsAirborneHash = Animator.StringToHash("IsAirborne");
        private static readonly int JumpTakeoffHash = Animator.StringToHash("JumpTakeoff");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int GroundedPassThroughStateHash =
            Animator.StringToHash("Airborne.Grounded Pass-Through");

        [SerializeField] private Animator animator;
        [SerializeField] private FusionNetworkPlayer player;
        [SerializeField] private NetworkCharacterController networkController;
        [SerializeField, Tooltip("Movement/network root used to convert world velocity into Animator coordinates.")]
        private Transform movementRoot;
        [SerializeField, Min(0f)] private float parameterDampTime = 0.12f;

        private bool _initialized;
        private bool _reportedMissingReferences;
        private bool _deathLatched;
        private bool _hasGroundSample;
        private bool _wasGrounded;
        private bool _confirmedAirborne;
        private bool _jumpTakeoff;
        private bool _airborneSuppressedForDeath;
        private float _ungroundedTime;
        private int _airborneLayerIndex = -1;

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
            _airborneLayerIndex = animator.GetLayerIndex("Airborne");
            ResetAirborneState();
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
            UpdateAirborneAnimation(safeDeltaTime, dead);
            animator.SetFloat(MoveXHash, moveX, parameterDampTime, safeDeltaTime);
            animator.SetFloat(MoveYHash, moveY, parameterDampTime, safeDeltaTime);
            animator.SetBool(CrouchedHash, player.IsCrouched);
            animator.SetBool(DeadHash, dead);
        }

        private void UpdateAirborneAnimation(float deltaTime, bool dead)
        {
            float verticalVelocity = networkController.Velocity.y;
            animator.SetFloat(VerticalVelocityHash, verticalVelocity);

            if (dead)
            {
                _confirmedAirborne = false;
                _jumpTakeoff = false;
                _ungroundedTime = 0f;
                animator.SetBool(IsAirborneHash, false);
                animator.SetBool(JumpTakeoffHash, false);

                if (!_airborneSuppressedForDeath && _airborneLayerIndex >= 0)
                {
                    animator.Play(GroundedPassThroughStateHash, _airborneLayerIndex, 0f);
                    _airborneSuppressedForDeath = true;
                }

                return;
            }

            bool grounded = networkController.Grounded;
            if (!_hasGroundSample)
            {
                _hasGroundSample = true;
                _wasGrounded = grounded;
                animator.SetBool(IsAirborneHash, false);
                animator.SetBool(JumpTakeoffHash, false);
                return;
            }

            if (grounded)
            {
                _ungroundedTime = 0f;
                _confirmedAirborne = false;
                _jumpTakeoff = false;
            }
            else
            {
                bool acceptedJumpTakeoff = _wasGrounded
                    && verticalVelocity > JumpTakeoffVelocityThreshold;
                if (acceptedJumpTakeoff)
                {
                    _confirmedAirborne = true;
                    _jumpTakeoff = true;
                    _ungroundedTime = 0f;
                }
                else if (!_confirmedAirborne)
                {
                    _ungroundedTime += deltaTime;
                    if (_ungroundedTime >= AirborneConfirmationTime)
                    {
                        _confirmedAirborne = true;
                        _jumpTakeoff = false;
                    }
                }
                else if (_jumpTakeoff && verticalVelocity <= JumpApexVelocityThreshold)
                {
                    _jumpTakeoff = false;
                }
            }

            animator.SetBool(IsAirborneHash, _confirmedAirborne);
            animator.SetBool(JumpTakeoffHash, _jumpTakeoff);
            _wasGrounded = grounded;
        }

        private void ResetAirborneState()
        {
            _hasGroundSample = false;
            _wasGrounded = false;
            _confirmedAirborne = false;
            _jumpTakeoff = false;
            _airborneSuppressedForDeath = false;
            _ungroundedTime = 0f;

            animator.SetBool(IsAirborneHash, false);
            animator.SetBool(JumpTakeoffHash, false);
            animator.SetFloat(VerticalVelocityHash, 0f);
            if (_airborneLayerIndex >= 0)
                animator.Play(GroundedPassThroughStateHash, _airborneLayerIndex, 0f);
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
