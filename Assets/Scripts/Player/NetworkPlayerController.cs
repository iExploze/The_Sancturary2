using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerController : NetworkBehaviour
    {
        private const float MouseSensitivity = 0.12f;
        private const float MovingInputThreshold = 0.0001f;

        private static readonly Dictionary<ulong, NetworkPlayerController> activePlayers = new();

        [Header("Definition")]
        [SerializeField] private PlayerMovementDefinition movementDefinition;

        [Header("Authoritative Movement")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private LayerMask standingCollisionMask = ~0;

        [Header("Local Presentation")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener playerAudioListener;
        [SerializeField] private LocalTirednessVignette tirednessVignette;

        [Header("Shared Presentation")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform bodyVisual;

        private readonly NetworkVariable<bool> synchronizedCrouching = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> ownerStaminaNormalized = new(
            1f,
            NetworkVariableReadPermission.Owner,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> ownerExhausted = new(
            false,
            NetworkVariableReadPermission.Owner,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> synchronizedCameraPitch = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Vector2 serverMoveInput;
        private float serverYaw;
        private bool serverJumpRequested;
        private bool serverSprintRequested;
        private bool serverCrouchRequested;
        private float verticalVelocity;
        private float cameraPitch;
        private bool cursorLocked;
        private PlayerStaminaState staminaState;

        private Vector3 standingControllerCenter;
        private Vector3 crouchingControllerCenter;
        private Vector3 bodyStandingLocalPosition;
        private Vector3 bodyStandingLocalScale;

        public static IReadOnlyDictionary<ulong, NetworkPlayerController> ActivePlayers => activePlayers;

        public override void OnNetworkSpawn()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            activePlayers[OwnerClientId] = this;
            CacheStandingPresentation();

            characterController.enabled = IsServer;
            playerCamera.enabled = IsOwner;
            playerAudioListener.enabled = IsOwner;
            bodyRenderer.enabled = !IsOwner;
            tirednessVignette.ConfigureForOwner(IsOwner);

            if (IsServer)
            {
                staminaState = new PlayerStaminaState(
                    movementDefinition.MaximumStamina,
                    movementDefinition.SprintDrainPerSecond,
                    movementDefinition.StaminaRecoveryPerSecond,
                    movementDefinition.StaminaRecoveryDelay,
                    movementDefinition.ExhaustedRecoveryThreshold);
                synchronizedCrouching.Value = false;
                ownerStaminaNormalized.Value = 1f;
                ownerExhausted.Value = false;
                ApplyAuthoritativeControllerDimensions(false);
            }

            ApplyPresentationImmediately(synchronizedCrouching.Value);
            if (IsOwner)
            {
                SetCursorLocked(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (activePlayers.TryGetValue(OwnerClientId, out var player) && player == this)
            {
                activePlayers.Remove(OwnerClientId);
            }

            tirednessVignette?.ConfigureForOwner(false);
            if (IsOwner)
            {
                SetCursorLocked(false);
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsOwner)
            {
                ReadOwnerInput();
                UpdateOwnerPresentation();
            }
            else
            {
                cameraPivot.localRotation = Quaternion.Euler(synchronizedCameraPitch.Value, 0f, 0f);
            }

            UpdateBodyPresentation();
            if (IsServer)
            {
                ApplyServerMovement();
            }
        }

        private void ReadOwnerInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(!cursorLocked);
            }

            var moveInput = Vector2.zero;
            if (keyboard.wKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed) moveInput.y -= 1f;
            if (keyboard.dKey.isPressed) moveInput.x += 1f;
            if (keyboard.aKey.isPressed) moveInput.x -= 1f;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            var yaw = transform.eulerAngles.y;
            if (cursorLocked && mouse != null)
            {
                var mouseDelta = mouse.delta.ReadValue() * MouseSensitivity;
                yaw += mouseDelta.x;
                cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y, -85f, 85f);
                cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }

            SubmitInputRpc(
                moveInput,
                yaw,
                cameraPitch,
                keyboard.spaceKey.wasPressedThisFrame,
                keyboard.leftShiftKey.isPressed,
                keyboard.leftCtrlKey.isPressed);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitInputRpc(
            Vector2 moveInput,
            float yaw,
            float pitch,
            bool jumpRequested,
            bool sprintRequested,
            bool crouchRequested)
        {
            serverMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
            serverYaw = yaw;
            synchronizedCameraPitch.Value = Mathf.Clamp(pitch, -85f, 85f);
            serverJumpRequested |= jumpRequested;
            serverSprintRequested = sprintRequested;
            serverCrouchRequested = crouchRequested;
        }

        private void ApplyServerMovement()
        {
            UpdateAuthoritativeCrouchState();
            transform.rotation = Quaternion.Euler(0f, serverYaw, 0f);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (serverJumpRequested && characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(movementDefinition.JumpHeight * -2f * movementDefinition.Gravity);
            }

            serverJumpRequested = false;
            verticalVelocity += movementDefinition.Gravity * Time.deltaTime;

            var isMoving = serverMoveInput.sqrMagnitude > MovingInputThreshold;
            var isSprinting = staminaState.Advance(
                Time.deltaTime,
                serverSprintRequested,
                isMoving,
                synchronizedCrouching.Value);

            ownerStaminaNormalized.Value = staminaState.NormalizedStamina;
            ownerExhausted.Value = staminaState.IsExhausted;

            var movementSpeed = synchronizedCrouching.Value
                ? movementDefinition.CrouchSpeed
                : isSprinting
                    ? movementDefinition.SprintSpeed
                    : movementDefinition.WalkSpeed;
            var planarMove = transform.right * serverMoveInput.x + transform.forward * serverMoveInput.y;
            var velocity = planarMove * movementSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void UpdateAuthoritativeCrouchState()
        {
            if (serverCrouchRequested)
            {
                SetAuthoritativeCrouching(true);
                return;
            }

            if (synchronizedCrouching.Value && HasStandingClearance())
            {
                SetAuthoritativeCrouching(false);
            }
        }

        private void SetAuthoritativeCrouching(bool crouching)
        {
            if (synchronizedCrouching.Value == crouching)
            {
                return;
            }

            synchronizedCrouching.Value = crouching;
            ApplyAuthoritativeControllerDimensions(crouching);
        }

        private void ApplyAuthoritativeControllerDimensions(bool crouching)
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
            var scale = transform.lossyScale;
            var verticalScale = Mathf.Abs(scale.y);
            var horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var skinWidth = characterController.skinWidth * horizontalScale;
            var radius = Mathf.Max(0.01f, characterController.radius * horizontalScale - skinWidth);
            var halfHeight = Mathf.Max(
                radius,
                movementDefinition.StandingControllerHeight * verticalScale * 0.5f - skinWidth);
            var center = transform.TransformPoint(standingControllerCenter);
            var segmentHalfLength = Mathf.Max(0f, halfHeight - radius);
            var pointA = center + transform.up * segmentHalfLength;
            var pointB = center - transform.up * segmentHalfLength;

            foreach (var overlap in Physics.OverlapCapsule(
                         pointA,
                         pointB,
                         radius,
                         standingCollisionMask,
                         QueryTriggerInteraction.Ignore))
            {
                if (overlap == characterController || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void UpdateOwnerPresentation()
        {
            var cameraPosition = cameraPivot.localPosition;
            var targetCameraHeight = synchronizedCrouching.Value
                ? movementDefinition.CrouchingCameraHeight
                : movementDefinition.StandingCameraHeight;
            var response = GetTransitionResponse();
            cameraPosition.y = Mathf.Lerp(cameraPosition.y, targetCameraHeight, response);
            cameraPivot.localPosition = cameraPosition;

            tirednessVignette.SetStaminaState(ownerStaminaNormalized.Value, ownerExhausted.Value);
        }

        private void UpdateBodyPresentation()
        {
            var heightRatio = synchronizedCrouching.Value
                ? movementDefinition.CrouchingControllerHeight / movementDefinition.StandingControllerHeight
                : 1f;
            var targetScale = bodyStandingLocalScale;
            targetScale.y *= heightRatio;
            var targetPosition = bodyStandingLocalPosition;
            targetPosition.y -= bodyStandingLocalScale.y - targetScale.y;

            var response = GetTransitionResponse();
            bodyVisual.localScale = Vector3.Lerp(bodyVisual.localScale, targetScale, response);
            bodyVisual.localPosition = Vector3.Lerp(bodyVisual.localPosition, targetPosition, response);
        }

        private float GetTransitionResponse()
        {
            return 1f - Mathf.Exp(-movementDefinition.CrouchTransitionSpeed * Time.deltaTime);
        }

        private void CacheStandingPresentation()
        {
            var existingBottom = characterController.center.y - characterController.height * 0.5f;
            standingControllerCenter = characterController.center;
            standingControllerCenter.y = existingBottom + movementDefinition.StandingControllerHeight * 0.5f;
            crouchingControllerCenter = standingControllerCenter +
                                        Vector3.up * (movementDefinition.CrouchingControllerHeight -
                                                      movementDefinition.StandingControllerHeight) * 0.5f;
            bodyStandingLocalPosition = bodyVisual.localPosition;
            bodyStandingLocalScale = bodyVisual.localScale;
        }

        private void ApplyPresentationImmediately(bool crouching)
        {
            var cameraPosition = cameraPivot.localPosition;
            cameraPosition.y = crouching
                ? movementDefinition.CrouchingCameraHeight
                : movementDefinition.StandingCameraHeight;
            cameraPivot.localPosition = cameraPosition;

            var heightRatio = crouching
                ? movementDefinition.CrouchingControllerHeight / movementDefinition.StandingControllerHeight
                : 1f;
            var scale = bodyStandingLocalScale;
            scale.y *= heightRatio;
            var position = bodyStandingLocalPosition;
            position.y -= bodyStandingLocalScale.y - scale.y;
            bodyVisual.localScale = scale;
            bodyVisual.localPosition = position;
        }

        private bool ValidateConfiguration()
        {
            if (movementDefinition != null &&
                characterController != null &&
                cameraPivot != null &&
                playerCamera != null &&
                playerAudioListener != null &&
                tirednessVignette != null &&
                bodyRenderer != null &&
                bodyVisual != null)
            {
                return true;
            }

            Debug.LogError("Network player movement or presentation references are incomplete.", this);
            return false;
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
