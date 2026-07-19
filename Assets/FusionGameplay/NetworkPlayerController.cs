using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Player
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Fusion.NetworkObject))]
    [RequireComponent(typeof(SimpleKCC))]
    public sealed class NetworkPlayerController : NetworkBehaviour, IBeforeUpdate
    {
        private const float MovingInputThreshold = 0.0001f;
        private const float GroundAcceleration = 45f;
        private const float GroundDeceleration = 30f;
        private const float AirAcceleration = 18f;
        private const float AirDeceleration = 2f;

        private static readonly Dictionary<ulong, NetworkPlayerController> activePlayers = new();

        [Header("Shared Movement")]
        [SerializeField] private PlayerMovementDefinition movementDefinition;
        [SerializeField] private SimpleKCC kcc;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.35f;
        [SerializeField] private LayerMask standingCollisionMask = ~0;

        [Header("Local Presentation")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener playerAudioListener;
        [SerializeField] private LocalTirednessVignette tirednessVignette;

        [Header("Shared Presentation")]
        [SerializeField] private Transform bodyVisual;
        [SerializeField] private GameObject flashlightRoot;
        [SerializeField] private Light flashlightLight;

        [Networked] private NetworkButtons PreviousButtons { get; set; }
        [Networked] private Vector3 MoveVelocity { get; set; }
        [Networked] private bool IsCrouching { get; set; }
        [Networked] private bool IsExhausted { get; set; }
        [Networked] private bool IsFlashlightOn { get; set; }
        [Networked] private float CurrentStamina { get; set; }
        [Networked] private float TimeSinceSprintStopped { get; set; }

        private readonly Collider[] standingOverlaps = new Collider[16];
        private readonly Vector2Accumulator lookRotationAccumulator = new(0.02f, true);

        private FusionPlayerInput accumulatedInput;
        private NetworkEvents networkEvents;
        private bool inputRegistered;
        private bool cursorLocked;
        private ulong activePlayerKey;
        private Vector3 bodyStandingLocalPosition;
        private Vector3 bodyStandingLocalScale;

        public static IReadOnlyDictionary<ulong, NetworkPlayerController> ActivePlayers => activePlayers;
        public bool IsOwner => HasInputAuthority;
        public bool IsSpawned => Object != null;
        public float NormalizedStamina => movementDefinition == null
            ? 0f
            : TirednessVignetteRules.NormalizeStamina(CurrentStamina, movementDefinition.MaximumStamina);

        public override void Spawned()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            name = $"FusionPlayer {Object.InputAuthority}";
            activePlayerKey = (ulong)Mathf.Max(0, Object.InputAuthority.PlayerId);
            activePlayers[activePlayerKey] = this;
            PlayerAvatarRegistry.Register(activePlayerKey, transform, HasInputAuthority);
            bodyStandingLocalPosition = bodyVisual.localPosition;
            bodyStandingLocalScale = bodyVisual.localScale;

            playerCamera.enabled = HasInputAuthority;
            playerAudioListener.enabled = HasInputAuthority;
            bodyVisual.gameObject.SetActive(!HasInputAuthority);
            tirednessVignette.ConfigureForOwner(HasInputAuthority);

            if (HasStateAuthority)
            {
                CurrentStamina = movementDefinition.MaximumStamina;
                TimeSinceSprintStopped = movementDefinition.StaminaRecoveryDelay;
                IsCrouching = false;
                IsExhausted = false;
                IsFlashlightOn = false;
            }

            ApplyCrouchHeight(IsCrouching);
            ApplyPresentation(false);
            ApplyFlashlightState();

            if (HasInputAuthority)
            {
                networkEvents = Runner.GetComponent<NetworkEvents>();
                if (networkEvents == null)
                {
                    Debug.LogError("The active Fusion runner is missing NetworkEvents, so player input cannot be submitted.", this);
                    enabled = false;
                    return;
                }

                networkEvents.OnInput.AddListener(ProvideInput);
                inputRegistered = true;
                SetCursorLocked(true);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (activePlayers.TryGetValue(activePlayerKey, out var player) && player == this)
            {
                activePlayers.Remove(activePlayerKey);
            }

            PlayerAvatarRegistry.Unregister(activePlayerKey, transform);

            UnregisterInput();
            tirednessVignette?.ConfigureForOwner(false);
            if (HasInputAuthority)
            {
                SetCursorLocked(false);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out FusionPlayerInput input))
            {
                MovePlayer(Vector3.zero, 0f);
                return;
            }

            kcc.AddLookRotation(input.LookRotationDelta, -85f, 85f);
            UpdateCrouch(input.Buttons.IsSet(PlayerInputButton.Crouch));

            if (input.Buttons.WasPressed(PreviousButtons, PlayerInputButton.Flashlight))
            {
                IsFlashlightOn = !IsFlashlightOn;
            }

            var isMoving = input.MoveDirection.sqrMagnitude > MovingInputThreshold;
            var stamina = CurrentStamina;
            var exhausted = IsExhausted;
            var timeSinceSprintStopped = TimeSinceSprintStopped;
            var isSprinting = PlayerStaminaSimulation.Advance(
                Runner.DeltaTime,
                input.Buttons.IsSet(PlayerInputButton.Sprint),
                isMoving,
                IsCrouching,
                movementDefinition.MaximumStamina,
                movementDefinition.SprintDrainPerSecond,
                movementDefinition.StaminaRecoveryPerSecond,
                movementDefinition.StaminaRecoveryDelay,
                movementDefinition.ExhaustedRecoveryThreshold,
                ref stamina,
                ref exhausted,
                ref timeSinceSprintStopped);
            CurrentStamina = stamina;
            IsExhausted = exhausted;
            TimeSinceSprintStopped = timeSinceSprintStopped;

            var speed = IsCrouching
                ? movementDefinition.CrouchSpeed
                : isSprinting
                    ? movementDefinition.SprintSpeed
                    : movementDefinition.WalkSpeed;
            var inputDirection = kcc.TransformRotation *
                                 new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
            var jumpImpulse = 0f;
            if (input.Buttons.WasPressed(PreviousButtons, PlayerInputButton.Jump) && kcc.IsGrounded)
            {
                jumpImpulse = Mathf.Sqrt(movementDefinition.JumpHeight * -2f * movementDefinition.Gravity);
            }

            kcc.SetGravity(movementDefinition.Gravity);
            MovePlayer(inputDirection * speed, jumpImpulse);
            PreviousButtons = input.Buttons;
        }

        public override void Render()
        {
            if (kcc == null)
            {
                return;
            }

            var lookRotation = kcc.GetLookRotation(true, false);
            cameraPivot.localRotation = Quaternion.Euler(lookRotation);
            ApplyPresentation(true);
            ApplyFlashlightState();

            if (HasInputAuthority)
            {
                tirednessVignette.SetStaminaState(NormalizedStamina, IsExhausted);
            }
        }

        private void LateUpdate()
        {
            // Fusion 2.0.4 does not schedule NetworkBehaviour.Render callbacks for
            // proxies in Unity 6000.3, although SimpleKCC receives valid snapshots.
            // LateUpdate is outside the simulation loop and safely drives the add-on's
            // own interpolation routine after Fusion has applied the latest snapshot.
            if (Object != null && kcc != null && kcc.IsProxy)
            {
                kcc.Render();
            }
        }

        void IBeforeUpdate.BeforeUpdate()
        {
            if (!inputRegistered || !HasInputAuthority)
            {
                return;
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

            var mouse = Mouse.current;
            if (!cursorLocked)
            {
                if (mouse?.leftButton.wasPressedThisFrame == true)
                {
                    SetCursorLocked(true);
                }

                accumulatedInput.MoveDirection = Vector2.zero;
                accumulatedInput.Buttons = default;
                return;
            }

            var moveDirection = Vector2.zero;
            if (keyboard.wKey.isPressed) moveDirection += Vector2.up;
            if (keyboard.sKey.isPressed) moveDirection += Vector2.down;
            if (keyboard.aKey.isPressed) moveDirection += Vector2.left;
            if (keyboard.dKey.isPressed) moveDirection += Vector2.right;
            accumulatedInput.MoveDirection = Vector2.ClampMagnitude(moveDirection, 1f);

            accumulatedInput.Buttons.Set(PlayerInputButton.Jump, keyboard.spaceKey.isPressed);
            accumulatedInput.Buttons.Set(PlayerInputButton.Sprint, keyboard.leftShiftKey.isPressed);
            accumulatedInput.Buttons.Set(
                PlayerInputButton.Crouch,
                keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed);
            accumulatedInput.Buttons.Set(PlayerInputButton.Flashlight, keyboard.fKey.isPressed);

            if (mouse != null)
            {
                var mouseDelta = mouse.delta.ReadValue() * movementDefinition.MouseSensitivity;
                lookRotationAccumulator.Accumulate(new Vector2(-mouseDelta.y, mouseDelta.x));
            }
        }

        private void ProvideInput(NetworkRunner runner, NetworkInput input)
        {
            accumulatedInput.LookRotationDelta = lookRotationAccumulator.ConsumeTickAligned(runner);
            input.Set(accumulatedInput);
        }

        private void UpdateCrouch(bool crouchRequested)
        {
            if (crouchRequested)
            {
                if (!IsCrouching)
                {
                    IsCrouching = true;
                    ApplyCrouchHeight(true);
                }

                return;
            }

            if (IsCrouching && HasStandingClearance())
            {
                IsCrouching = false;
                ApplyCrouchHeight(false);
            }
        }

        private void ApplyCrouchHeight(bool crouching)
        {
            kcc.SetHeight(crouching
                ? movementDefinition.CrouchingControllerHeight
                : movementDefinition.StandingControllerHeight);
        }

        private bool HasStandingClearance()
        {
            var up = transform.up;
            var radius = Mathf.Min(collisionRadius, movementDefinition.StandingControllerHeight * 0.5f);
            var bottom = transform.position + up * radius;
            var top = transform.position + up * (movementDefinition.StandingControllerHeight - radius);
            var overlapCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                standingOverlaps,
                standingCollisionMask,
                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < overlapCount; i++)
            {
                var overlap = standingOverlaps[i];
                standingOverlaps[i] = null;
                if (overlap == null || overlap.transform == transform || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void MovePlayer(Vector3 desiredVelocity, float jumpImpulse)
        {
            var acceleration = desiredVelocity.sqrMagnitude <= MovingInputThreshold
                ? kcc.IsGrounded ? GroundDeceleration : AirDeceleration
                : kcc.IsGrounded ? GroundAcceleration : AirAcceleration;
            MoveVelocity = Vector3.Lerp(MoveVelocity, desiredVelocity, acceleration * Runner.DeltaTime);
            kcc.Move(MoveVelocity, jumpImpulse);
        }

        private void ApplyPresentation(bool smooth)
        {
            var response = smooth
                ? 1f - Mathf.Exp(-movementDefinition.CrouchTransitionSpeed * Time.deltaTime)
                : 1f;
            var targetCameraHeight = IsCrouching
                ? movementDefinition.CrouchingCameraHeight
                : movementDefinition.StandingCameraHeight;
            var cameraPosition = cameraPivot.localPosition;
            cameraPosition.y = Mathf.Lerp(cameraPosition.y, targetCameraHeight, response);
            cameraPivot.localPosition = cameraPosition;

            var heightRatio = IsCrouching
                ? movementDefinition.CrouchingControllerHeight / movementDefinition.StandingControllerHeight
                : 1f;
            var targetScale = bodyStandingLocalScale;
            targetScale.y *= heightRatio;
            var targetPosition = bodyStandingLocalPosition;
            targetPosition.y -= (bodyStandingLocalScale.y - targetScale.y) * 0.5f;
            bodyVisual.localScale = Vector3.Lerp(bodyVisual.localScale, targetScale, response);
            bodyVisual.localPosition = Vector3.Lerp(bodyVisual.localPosition, targetPosition, response);
        }

        private void ApplyFlashlightState()
        {
            if (flashlightRoot != null && flashlightRoot.activeSelf != IsFlashlightOn)
            {
                flashlightRoot.SetActive(IsFlashlightOn);
            }

            if (flashlightLight != null)
            {
                flashlightLight.enabled = IsFlashlightOn;
            }
        }

        private bool ValidateConfiguration()
        {
            if (movementDefinition != null &&
                kcc != null &&
                cameraPivot != null &&
                playerCamera != null &&
                playerAudioListener != null &&
                tirednessVignette != null &&
                bodyVisual != null)
            {
                return true;
            }

            Debug.LogError("Fusion player movement or presentation references are incomplete.", this);
            return false;
        }

        private void UnregisterInput()
        {
            if (!inputRegistered || networkEvents == null)
            {
                return;
            }

            networkEvents.OnInput.RemoveListener(ProvideInput);
            inputRegistered = false;
            networkEvents = null;
        }

        private void OnDestroy()
        {
            if (activePlayers.TryGetValue(activePlayerKey, out var player) && player == this)
            {
                activePlayers.Remove(activePlayerKey);
            }

            PlayerAvatarRegistry.Unregister(activePlayerKey, transform);

            UnregisterInput();
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
