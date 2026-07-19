using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimpleCapsuleMovement : MonoBehaviour
    {
        private const float GroundedDownwardVelocity = -2f;
        private const float MovingInputThreshold = 0.0001f;

        [Header("Standalone Preview Safety")]
        [SerializeField, Tooltip("Allows this non-networked prefab to become controllable when no Fusion runner is active.")]
        private bool enableStandalonePreview = true;

        [SerializeField, Tooltip("When enabled, this preview is disabled outside the Unity Editor.")]
        private bool editorOnly = true;

        [Header("Shared Movement")]
        [SerializeField] private PlayerMovementDefinition movementDefinition;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private LayerMask standingCollisionMask = ~0;

        [Header("Local Presentation")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener playerAudioListener;
        [SerializeField] private GameObject bodyVisual;
        [SerializeField] private GameObject flashlightRoot;
        [SerializeField] private Light flashlightLight;

        private readonly Collider[] standingOverlaps = new Collider[16];

        private PlayerStaminaState staminaState;
        private Vector3 standingControllerCenter;
        private Vector3 crouchingControllerCenter;
        private float cameraPitch;
        private float verticalVelocity;
        private bool isCrouching;
        private bool flashlightOn;
        private bool cursorLocked;
        private bool previewActive;

        public bool EnableStandalonePreview => enableStandalonePreview;
        public float NormalizedStamina => staminaState?.NormalizedStamina ?? 1f;

        private void Awake()
        {
            characterController ??= GetComponent<CharacterController>();
            SetPreviewComponentsEnabled(false);

            if (HasActiveRunner())
            {
                Destroy(gameObject);
                return;
            }

            if (movementDefinition != null && characterController != null)
            {
                CacheControllerCenters();
            }
        }

        private void Start()
        {
            TryActivatePreview();
        }

        private void Update()
        {
            if (HasActiveRunner())
            {
                SetPreviewComponentsEnabled(false);
                Destroy(gameObject);
                return;
            }

            if (!previewActive)
            {
                TryActivatePreview();
                if (!previewActive)
                {
                    return;
                }
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(!cursorLocked);
            }

            if (!cursorLocked && Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                SetCursorLocked(true);
            }

            UpdateLook();
            UpdateCrouch(keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed);

            if (keyboard.fKey.wasPressedThisFrame)
            {
                flashlightOn = !flashlightOn;
                ApplyFlashlightState();
            }

            var moveInput = Vector2.zero;
            if (cursorLocked)
            {
                if (keyboard.wKey.isPressed) moveInput += Vector2.up;
                if (keyboard.sKey.isPressed) moveInput += Vector2.down;
                if (keyboard.aKey.isPressed) moveInput += Vector2.left;
                if (keyboard.dKey.isPressed) moveInput += Vector2.right;
            }

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            var isMoving = moveInput.sqrMagnitude > MovingInputThreshold;
            var isSprinting = staminaState.Advance(
                Time.deltaTime,
                keyboard.leftShiftKey.isPressed,
                isMoving,
                isCrouching);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = GroundedDownwardVelocity;
            }

            if (keyboard.spaceKey.wasPressedThisFrame && characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(
                    movementDefinition.JumpHeight * -2f * movementDefinition.Gravity);
            }

            verticalVelocity += movementDefinition.Gravity * Time.deltaTime;
            var speed = isCrouching
                ? movementDefinition.CrouchSpeed
                : isSprinting
                    ? movementDefinition.SprintSpeed
                    : movementDefinition.WalkSpeed;
            var planarDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            characterController.Move(
                (planarDirection * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
            UpdateCameraHeight();
        }

        private void TryActivatePreview()
        {
            if (!enableStandalonePreview ||
                (editorOnly && !Application.isEditor) ||
                HasActiveRunner() ||
                !ValidateConfiguration())
            {
                SetPreviewComponentsEnabled(false);
                return;
            }

            staminaState = new PlayerStaminaState(
                movementDefinition.MaximumStamina,
                movementDefinition.SprintDrainPerSecond,
                movementDefinition.StaminaRecoveryPerSecond,
                movementDefinition.StaminaRecoveryDelay,
                movementDefinition.ExhaustedRecoveryThreshold);
            isCrouching = false;
            flashlightOn = false;
            cameraPitch = 0f;
            verticalVelocity = 0f;
            ApplyControllerHeight(false);
            SetPreviewComponentsEnabled(true);
            ApplyFlashlightState();
            SetCursorLocked(true);
            previewActive = true;
        }

        private void UpdateLook()
        {
            var mouse = Mouse.current;
            if (!cursorLocked || mouse == null)
            {
                return;
            }

            var mouseDelta = mouse.delta.ReadValue() * movementDefinition.MouseSensitivity;
            transform.Rotate(Vector3.up, mouseDelta.x);
            cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y, -85f, 85f);
            cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void UpdateCrouch(bool crouchRequested)
        {
            if (crouchRequested && !isCrouching)
            {
                isCrouching = true;
                ApplyControllerHeight(true);
                return;
            }

            if (!crouchRequested && isCrouching && HasStandingClearance())
            {
                isCrouching = false;
                ApplyControllerHeight(false);
            }
        }

        private void CacheControllerCenters()
        {
            var existingBottom = characterController.center.y - characterController.height * 0.5f;
            standingControllerCenter = characterController.center;
            standingControllerCenter.y = existingBottom + movementDefinition.StandingControllerHeight * 0.5f;
            crouchingControllerCenter = standingControllerCenter +
                                        Vector3.up * (movementDefinition.CrouchingControllerHeight -
                                                      movementDefinition.StandingControllerHeight) * 0.5f;
        }

        private void ApplyControllerHeight(bool crouching)
        {
            characterController.height = crouching
                ? movementDefinition.CrouchingControllerHeight
                : movementDefinition.StandingControllerHeight;
            characterController.center = crouching
                ? crouchingControllerCenter
                : standingControllerCenter;
        }

        private bool HasStandingClearance()
        {
            var radius = Mathf.Max(0.01f, characterController.radius - characterController.skinWidth);
            var halfHeight = Mathf.Max(
                radius,
                movementDefinition.StandingControllerHeight * 0.5f - characterController.skinWidth);
            var center = transform.TransformPoint(standingControllerCenter);
            var segment = Mathf.Max(0f, halfHeight - radius);
            var pointA = center + transform.up * segment;
            var pointB = center - transform.up * segment;
            var overlapCount = Physics.OverlapCapsuleNonAlloc(
                pointA,
                pointB,
                radius,
                standingOverlaps,
                standingCollisionMask,
                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < overlapCount; i++)
            {
                var overlap = standingOverlaps[i];
                standingOverlaps[i] = null;
                if (overlap == null || overlap == characterController || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void UpdateCameraHeight()
        {
            var targetHeight = isCrouching
                ? movementDefinition.CrouchingCameraHeight
                : movementDefinition.StandingCameraHeight;
            var response = 1f - Mathf.Exp(-movementDefinition.CrouchTransitionSpeed * Time.deltaTime);
            var position = cameraPivot.localPosition;
            position.y = Mathf.Lerp(position.y, targetHeight, response);
            cameraPivot.localPosition = position;
        }

        private void ApplyFlashlightState()
        {
            if (flashlightRoot != null)
            {
                flashlightRoot.SetActive(flashlightOn);
            }

            if (flashlightLight != null)
            {
                flashlightLight.enabled = flashlightOn;
            }
        }

        private void SetPreviewComponentsEnabled(bool active)
        {
            previewActive = active;
            if (characterController != null) characterController.enabled = active;
            if (playerCamera != null) playerCamera.enabled = active;
            if (playerAudioListener != null) playerAudioListener.enabled = active;
            if (bodyVisual != null) bodyVisual.SetActive(false);
            if (flashlightRoot != null && !active) flashlightRoot.SetActive(false);
            if (flashlightLight != null && !active) flashlightLight.enabled = false;

            if (!active && cursorLocked)
            {
                SetCursorLocked(false);
            }
        }

        private bool ValidateConfiguration()
        {
            if (movementDefinition != null &&
                characterController != null &&
                cameraPivot != null &&
                playerCamera != null &&
                playerAudioListener != null)
            {
                return true;
            }

            Debug.LogError("Standalone player preview references are incomplete.", this);
            return false;
        }

        private static bool HasActiveRunner()
        {
            return NetworkRunner.Instances.Any(runner => runner != null && runner.IsRunning);
        }

        private void OnDisable()
        {
            if (cursorLocked)
            {
                SetCursorLocked(false);
            }
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnValidate()
        {
            characterController ??= GetComponent<CharacterController>();
        }
    }
}
