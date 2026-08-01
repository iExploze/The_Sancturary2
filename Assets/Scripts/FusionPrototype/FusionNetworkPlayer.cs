using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;
using TheSancturary.Inventory;

namespace TheSancturary.FusionPrototype
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkCharacterController), typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAnimationDriver))]
    [RequireComponent(typeof(LocalInteractionTargeting), typeof(NetworkPlayerInventory), typeof(PlayerInventory))]
    public sealed class FusionNetworkPlayer : NetworkBehaviour
    {
        private const string OwnerPostProcessingLayerName = "OwnerPostProcessing";
        private const float AirborneCharacterControllerHeightReduction = 0.5f;
        // Covers modest interpolation and transit drift without turning nearby
        // walls or objects outside the player's aim into valid targets.
        private const float InteractionDistanceTolerance = 0.35f;
        private const float InteractionAimToleranceDegrees = 12f;
        private const float InteractionSurfaceTolerance = 0.05f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float LookYawMismatchThreshold = 10f;
        private const float LookYawMismatchDuration = 0.5f;
        private const float LookYawMismatchWarningInterval = 5f;
#endif

        private enum MovementAudioEvent : byte
        {
            None,
            Footstep,
            Jump,
            Land,
            Crouch,
            Stand
        }

        private enum InteractionRejectionReason : byte
        {
            None,
            InvalidPlayer,
            PlayerUnavailable,
            InvalidTargetId,
            TargetNotFound,
            WrongRunner,
            TargetNotSpawned,
            UnsupportedTarget,
            TargetUnavailable,
            MissingPromptTarget,
            MissingCollider,
            TooFar,
            LookingAway,
            LineOfSightBlocked,
            TargetRejected
        }

        [Header("Required References")]
        [SerializeField] private NetworkCharacterController networkController;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform cameraMotion;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private PlayerAnimationDriver animationDriver;
        [SerializeField] private LocalInteractionTargeting localInteractionTargeting;
        [SerializeField] private NetworkPlayerInventory inventory;
        [SerializeField] private PlayerInventory gridInventory;
        [SerializeField] private Renderer[] characterRenderers;
        [SerializeField] private AudioSource localAudioSource;
        [SerializeField] private AudioSource spatialAudioSource;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0f)] private float crouchSpeed = 1.4f;
        [SerializeField, Min(0f)] private float walkAcceleration = 12f;
        [SerializeField, Min(0f)] private float sprintAcceleration = 22f;
        [SerializeField, Min(0f)] private float braking = 16f;
        [SerializeField] private float gravity = -24f;
        [SerializeField, Min(0f)] private float jumpImpulse = 6.3f;
        [SerializeField, Min(0.001f)] private float lookSensitivity = 0.6f;
        [SerializeField] private Vector2 pitchLimits = new(-85f, 85f);

        [Header("Crouching")]
        [SerializeField, Min(0.5f)] private float standingHeight = 1.65f;
        [SerializeField, Min(0.5f)] private float crouchingHeight = 1.1f;
        [SerializeField, Min(0f)] private float standingCameraHeight = 1.62f;
        [SerializeField, Min(0f)] private float crouchingCameraHeight = 0.92f;
        [SerializeField, Min(0.1f)] private float crouchTransitionSpeed = 5f;
        [SerializeField, Range(0.08f, 0.15f), Tooltip("Seconds used to restore the airborne collider to its grounded height.")]
        private float characterControllerRestoreDuration = 0.1f;
        [SerializeField] private LayerMask standingCollisionMask = ~0;

        [Header("Stamina")]
        [SerializeField, Min(0.01f)] private float maximumStamina = 100f;
        [SerializeField, Min(0f)] private float jumpStaminaCost = 5f;
        [SerializeField, Min(0f)] private float staminaDrainRate = 20f;
        [SerializeField, Min(0f)] private float staminaRecoveryDelay = 1.25f;
        [SerializeField, Min(0f)] private float staminaRecoveryRate = 24f;
        [SerializeField, Min(0f)] private float staminaRestartThreshold = 30f;

        [Header("Health")]
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float healthRegenerationDelay = 5f;
        [SerializeField, Min(0f)] private float healthRegenerationRate = 8f;
        [SerializeField, Min(1), Tooltip("Integer health removed when Subtract Inspector Damage is clicked in Play Mode.")]
        private int inspectorDamageToSubtract = 25;

        [Header("Concrete Movement Audio")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip landingClip;
        [SerializeField] private AudioClip crouchClip;
        [SerializeField] private AudioClip standClip;
        [SerializeField, Range(0f, 1f)] private float localFootstepVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float remoteVolumeMultiplier = 0.3f;
        [SerializeField, Min(0.1f)] private float walkStepDistance = 1.45f;
        [SerializeField, Min(0.1f)] private float sprintStepDistance = 1.75f;
        [SerializeField, Min(0.1f)] private float crouchStepDistance = 1.05f;

        [Header("Owner Camera Motion")]
        [SerializeField] private Vector2 walkBobAmount = new(0.018f, 0.028f);
        [SerializeField, Min(0f)] private float walkBobFrequency = 8f;
        [SerializeField] private Vector2 sprintBobAmount = new(0.045f, 0.075f);
        [SerializeField, Min(0f)] private float sprintBobFrequency = 13f;
        [SerializeField] private Vector2 crouchBobAmount = new(0.009f, 0.014f);
        [SerializeField, Min(0f)] private float crouchBobFrequency = 5f;
        [SerializeField, Min(0.01f)] private float bobBlendSpeed = 8f;

        [Networked] public float Stamina { get; private set; }
        [Networked] public float Health { get; private set; }
        [Networked] public NetworkBool IsDead { get; private set; }
        [Networked] public NetworkBool IsCrouched { get; private set; }
        [Networked] public NetworkBool IsSprinting { get; private set; }
        [Networked] public float LookYaw { get; private set; }
        [Networked] public float LookPitch { get; private set; }
        [Networked] private NetworkBool SprintLocked { get; set; }
        [Networked] private float StaminaRecoveryElapsed { get; set; }
        [Networked] private float TimeSinceDamage { get; set; }
        [Networked] private NetworkButtons PreviousButtons { get; set; }
        [Networked] private byte PreviousInteractionCommandSequence { get; set; }
        [Networked] private byte PreviousInventoryCommandSequence { get; set; }
        [Networked] private NetworkBool WasGrounded { get; set; }
        [Networked] private float AccumulatedStepDistance { get; set; }
        [Networked] private byte AudioEventSequence { get; set; }
        [Networked] private byte AudioEventCode { get; set; }
        [Networked] public NetworkBehaviourId CurrentLocker { get; private set; }
        [Networked] public NetworkBool IsHiddenInLocker { get; private set; }

        private readonly Collider[] _standingHits = new Collider[16];
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _attackAction;
        private readonly Queue<NetworkBehaviourId> _pendingInteractionTargets = new();
        private NetworkBehaviourId _submittedInteractionTarget;
        private byte _submittedInteractionCommandSequence;
        private byte _nextInteractionCommandSequence;
        private byte _nextInventoryCommandSequence;
        private float _localLookYaw;
        private float _localLookPitch;
        private VolumeProfile _runtimeVolumeProfile;
        private Vignette _vignette;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _debugExhaustionRequested;
        private float _lookYawMismatchStartedAt = -1f;
        private float _nextLookYawMismatchWarningTime;
#endif
        private int _pendingDamage;
        private byte _lastAudioEventSequence;
        private float _lastRenderedHealth;
        private float _damagePulse;
        private float _cameraHeightVelocity;
        private float _currentCameraHeight;
        private float _bobBlend;
        private float _bobTime;
        private bool _ownerCameraRenderingSubscribed;
        private VideoPlayer _deathVideoPlayer;
        private bool _deathSequenceStarted;
        private bool _airborneCharacterControllerShrunk;
        private bool _airborneCharacterControllerHasClearedGround;
        private float _airborneCharacterControllerHeight;
        private Vector3 _airborneCharacterControllerCenter;
        private bool _lastPresentedLockerHidden;
        private bool _localPauseInputBlocked;

        public bool IsDeadOrPending => IsDead || Health - _pendingDamage <= 0f;
        public bool IsLockerInputLocked => CurrentLocker.IsValid;
        public bool IsLocalPauseInputBlocked => HasInputAuthority && _localPauseInputBlocked;
        public NetworkPlayerInventory Inventory => inventory;
        public Vector3 ReplicatedViewPosition =>
            transform.position +
            Vector3.up *
            (IsCrouched ? crouchingCameraHeight : standingCameraHeight);

        public float AnimationReferenceSpeed => IsCrouched ? crouchSpeed : walkSpeed;

        public override void Spawned()
        {
            ResolveReferences();

            if (HasStateAuthority)
            {
                Stamina = maximumStamina;
                Health = maximumHealth;
                IsDead = false;
                CurrentLocker = default;
                IsHiddenInLocker = false;
                LookYaw = transform.eulerAngles.y;
                WasGrounded = networkController.Grounded;
            }

            bool isOwner = HasInputAuthority;
            playerInput.enabled = isOwner;
            playerCamera.enabled = isOwner;
            audioListener.enabled = isOwner;
            localInteractionTargeting.enabled = isOwner;
            SetCharacterVisibility(true);
            SetCharacterRenderingSuppressed(false);
            _lastPresentedLockerHidden = IsHiddenInLocker;
            animationDriver.Initialize();

            if (isOwner)
            {
                SubscribeToOwnerCameraRendering();
                CacheInputActions();
                playerInput.ActivateInput();
                _localLookYaw = LookYaw;
                _localLookPitch = Mathf.Clamp(LookPitch, pitchLimits.x, pitchLimits.y);
                ApplyOwnerCameraLook();
                localInteractionTargeting.Initialize(playerCamera, playerInput, this, inventory);
                gridInventory.GetComponent<PlayerEquipment>().InitializeOwner(playerCamera);
                gridInventory.InitializeOwner(
                    playerInput,
                    playerCamera,
                    localInteractionTargeting,
                    inventory);
                FusionSessionManager.Instance?.RegisterLocalPlayer(this);
                CreateOwnerVignette();
                LockCursor();
            }

            localAudioSource.spatialBlend = 0f;
            spatialAudioSource.spatialBlend = 1f;
            spatialAudioSource.minDistance = 1f;
            spatialAudioSource.maxDistance = 14f;
            spatialAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _lastAudioEventSequence = AudioEventSequence;
            _lastRenderedHealth = Health;
            _currentCameraHeight = standingCameraHeight;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogNetworkDiagnostics();
#endif
        }

        private void ResolveReferences()
        {
            networkController ??= GetComponent<NetworkCharacterController>();
            characterController ??= GetComponent<CharacterController>();
            playerInput ??= GetComponent<PlayerInput>();
            animationDriver ??= GetComponent<PlayerAnimationDriver>();
            localInteractionTargeting ??= GetComponent<LocalInteractionTargeting>();
            inventory ??= GetComponent<NetworkPlayerInventory>();
            gridInventory ??= GetComponent<PlayerInventory>();
            localAudioSource ??= GetComponent<AudioSource>();
            if (spatialAudioSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                spatialAudioSource = sources.Length > 1 ? sources[1] : localAudioSource;
            }

            if (characterRenderers == null || characterRenderers.Length == 0)
                characterRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void CacheInputActions()
        {
            InputActionMap map = playerInput.actions.FindActionMap("Player", true);
            _moveAction = map.FindAction("Move", true);
            _lookAction = map.FindAction("Look", true);
            _jumpAction = map.FindAction("Jump", true);
            _sprintAction = map.FindAction("Sprint", true);
            _crouchAction = map.FindAction("Crouch", true);
            _attackAction = map.FindAction("Attack", true);
        }

        private void Update()
        {
            if (!HasInputAuthority || playerInput == null || !playerInput.enabled)
                return;

            if (HandlePauseInput())
                return;

            if (gridInventory != null && gridInventory.IsMenuOpen)
                return;
            SampleOwnerLook();
            ApplyOwnerCameraLook();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MonitorOwnerLookYaw();
#endif
        }

        private void LateUpdate()
        {
            if (HasInputAuthority)
                ApplyOwnerCameraLook();
        }

        private void SampleOwnerLook()
        {
            if (_lookAction == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector2 lookDelta = _lookAction.ReadValue<Vector2>();
            if (lookDelta.sqrMagnitude <= Mathf.Epsilon)
                return;

            _localLookYaw = Mathf.Repeat(_localLookYaw + lookDelta.x * lookSensitivity, 360f);
            _localLookPitch = Mathf.Clamp(_localLookPitch - lookDelta.y * lookSensitivity, pitchLimits.x, pitchLimits.y);
        }

        private void ApplyOwnerCameraLook()
        {
            if (cameraRoot == null)
                return;

            if (TryGetLockerViewAnchor(out Transform viewAnchor))
                cameraRoot.SetPositionAndRotation(viewAnchor.position, Quaternion.Euler(_localLookPitch, _localLookYaw, 0f));
            else
                cameraRoot.rotation = Quaternion.Euler(_localLookPitch, _localLookYaw, 0f);
        }

        public FusionPlayerInput BuildNetworkInput()
        {
            FusionPlayerInput input = default;
            if (!HasInputAuthority || playerInput == null || !playerInput.enabled)
                return input;

            if (_localPauseInputBlocked)
                return input;

            input.LookAngles = new Vector2(_localLookYaw, _localLookPitch);
            if (!IsLockerInputLocked && inventory != null &&
                inventory.TryDequeueInputCommand(
                    out InventoryInputCommand inventoryCommand))
            {
                _nextInventoryCommandSequence++;
                if (_nextInventoryCommandSequence == 0)
                    _nextInventoryCommandSequence = 1;

                input.InventoryCommand = (byte)inventoryCommand.Type;
                input.InventoryCommandSequence =
                    _nextInventoryCommandSequence;
                input.InventoryInstanceId = inventoryCommand.InstanceId;
                input.InventoryColumn = inventoryCommand.Column;
                input.InventoryRow = inventoryCommand.Row;
                input.InventoryRotated = inventoryCommand.Rotated;
            }

            if (gridInventory != null && gridInventory.IsMenuOpen)
                return input;

            if (!IsLockerInputLocked)
                input.Move = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            if (_pendingInteractionTargets.Count > 0)
            {
                _submittedInteractionTarget = _pendingInteractionTargets.Dequeue();
                _nextInteractionCommandSequence++;
                if (_nextInteractionCommandSequence == 0)
                    _nextInteractionCommandSequence = 1;
                _submittedInteractionCommandSequence =
                    _nextInteractionCommandSequence;
            }

            // Repeat the latest command in subsequent inputs. The state authority
            // consumes each sequence once, so a missing input tick cannot erase a tap.
            input.InteractionTarget = _submittedInteractionTarget;
            input.InteractionCommandSequence =
                _submittedInteractionCommandSequence;
            input.Buttons.Set(FusionPlayerButton.Jump, !IsLockerInputLocked && _jumpAction.IsPressed());
            input.Buttons.Set(FusionPlayerButton.Sprint, !IsLockerInputLocked && _sprintAction.IsPressed());
            input.Buttons.Set(FusionPlayerButton.UseEquipped, !IsLockerInputLocked && _attackAction.IsPressed());

            bool keyboardCrouch = !IsLockerInputLocked && Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
            input.Buttons.Set(FusionPlayerButton.Crouch, !IsLockerInputLocked && (_crouchAction.IsPressed() || keyboardCrouch));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool debugExhaustion = Keyboard.current != null && Keyboard.current.f7Key.isPressed;
            input.Buttons.Set(FusionPlayerButton.DebugExhaustion, debugExhaustion);
#endif
            return input;
        }

        public void SetLocalPauseInputBlocked(bool blocked)
        {
            if (!HasInputAuthority)
                return;

            _localPauseInputBlocked = blocked;
            if (blocked)
                ClearInteractionCommands();
        }

        private bool HandlePauseInput()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return _localPauseInputBlocked;

            if (gridInventory != null && gridInventory.IsMenuOpen)
            {
                gridInventory.CloseMenu();
                return true;
            }

            GameplayPauseMenu.Instance?.Toggle(this);
            return true;
        }

        public void RequestInteraction(NetworkBehaviour targetBehaviour)
        {
            if (!HasInputAuthority || targetBehaviour == null || !targetBehaviour.Id.IsValid)
                return;

            _pendingInteractionTargets.Enqueue(targetBehaviour.Id);
        }

        private void ClearInteractionCommands()
        {
            _pendingInteractionTargets.Clear();
            _submittedInteractionTarget = default;
            _submittedInteractionCommandSequence = 0;
        }

        public bool TryReserveLockerAuthoritative(LockerController locker)
        {
            if (!HasStateAuthority || locker == null || IsDeadOrPending)
                return false;

            if (CurrentLocker.IsValid)
                return false;

            CurrentLocker = locker.Id;
            IsHiddenInLocker = false;
            return true;
        }

        public bool IsUsingLocker(LockerController locker)
        {
            return locker != null && CurrentLocker.IsValid && CurrentLocker == locker.Id;
        }

        public bool TryGetHiddenLocker(out LockerController locker)
        {
            locker = null;
            return IsHiddenInLocker && CurrentLocker.IsValid && Runner != null &&
                Runner.TryFindBehaviour(CurrentLocker, out NetworkBehaviour behaviour) &&
                (locker = behaviour as LockerController) != null;
        }

        public void FinishEnteringLockerAuthoritative(LockerController locker, Vector3 hiddenPosition)
        {
            if (!HasStateAuthority || !IsUsingLocker(locker) || IsDeadOrPending)
                return;

            TeleportAuthoritative(hiddenPosition);
            IsHiddenInLocker = true;
        }

        public void FinishLeavingLockerAuthoritative(LockerController locker, Vector3 exitPosition, Quaternion exitRotation)
        {
            if (!HasStateAuthority || !IsUsingLocker(locker))
                return;

            TeleportAuthoritative(exitPosition, exitRotation);
            IsHiddenInLocker = false;
            CurrentLocker = default;
        }

        public void ForceEjectFromLockerAuthoritative(LockerController locker, Vector3 exitPosition, Quaternion exitRotation)
        {
            FinishLeavingLockerAuthoritative(locker, exitPosition, exitRotation);
            KillInstantlyAuthoritative();
        }

        public void ClearLockerStateAuthoritative(LockerController locker)
        {
            if (!HasStateAuthority || !IsUsingLocker(locker))
                return;

            IsHiddenInLocker = false;
            CurrentLocker = default;
        }

        public void TeleportAuthoritative(Vector3 position)
        {
            if (!HasStateAuthority || networkController == null)
                return;

            networkController.Velocity = Vector3.zero;
            networkController.Teleport(position);
        }

        public void TeleportAuthoritative(Vector3 position, Quaternion rotation)
        {
            TeleportAuthoritative(position);
            transform.rotation = rotation;
            LookYaw = rotation.eulerAngles.y;
        }

        public bool KillInstantlyAuthoritative()
        {
            if (!HasStateAuthority || IsDead)
                return false;

            _pendingDamage = 0;
            Health = 0f;
            TimeSinceDamage = 0f;
            IsDead = true;
            ReleaseCurrentLockerAfterInvalidation();
            return true;
        }

        public void PresentLocalInteractionFeedback(string message, AudioClip sound)
        {
            if (!HasInputAuthority)
                return;

            gridInventory?.ShowMessage(message);
            if (sound != null && localAudioSource != null)
                localAudioSource.PlayOneShot(sound);
        }

        private void ProcessInteractionRequest(NetworkBehaviourId targetId)
        {
            if (!HasStateAuthority ||
                inventory == null ||
                !Runner.TryFindBehaviour(targetId, out NetworkBehaviour resolvedBehaviour) ||
                resolvedBehaviour is not IAuthoritativeInteractable interactable)
                return;

            bool isItemInteraction = resolvedBehaviour is WorldInventoryItem ||
                                     resolvedBehaviour is NetworkKeyPickup;
            if (!isItemInteraction)
            {
                if (ValidateInteractionRequest(interactable))
                    interactable.TryInteractAuthoritative(this);
                return;
            }

            if (!TryValidateItemInteractionRequest(
                    targetId,
                    out resolvedBehaviour,
                    out interactable,
                    out InteractionRejectionReason rejection,
                    out float interactionDistance,
                    out string blockingCollider))
            {
                LogInteractionRejection(
                    targetId,
                    resolvedBehaviour,
                    rejection,
                    interactionDistance,
                    blockingCollider);
                return;
            }

            if (!interactable.TryInteractAuthoritative(this))
            {
                LogInteractionRejection(
                    targetId,
                    resolvedBehaviour,
                    InteractionRejectionReason.TargetRejected,
                    interactionDistance,
                    blockingCollider);
            }
        }

        private bool TryValidateItemInteractionRequest(
            NetworkBehaviourId targetId,
            out NetworkBehaviour resolvedBehaviour,
            out IAuthoritativeInteractable interactable,
            out InteractionRejectionReason rejection,
            out float interactionDistance,
            out string blockingCollider)
        {
            resolvedBehaviour = null;
            interactable = null;
            rejection = InteractionRejectionReason.None;
            interactionDistance = -1f;
            blockingCollider = null;

            NetworkObject playerObject = Object;
            if (inventory == null || playerObject == null ||
                !playerObject.IsValid || !playerObject.IsInSimulation ||
                playerObject.Runner != Runner ||
                playerObject.InputAuthority == PlayerRef.None ||
                !Runner.TryGetPlayerObject(
                    playerObject.InputAuthority,
                    out NetworkObject registeredPlayerObject) ||
                registeredPlayerObject != playerObject)
            {
                rejection = InteractionRejectionReason.InvalidPlayer;
                return false;
            }

            if (IsDead)
            {
                rejection = InteractionRejectionReason.PlayerUnavailable;
                return false;
            }

            if (!targetId.IsValid)
            {
                rejection = InteractionRejectionReason.InvalidTargetId;
                return false;
            }

            if (!Runner.TryFindBehaviour(
                    targetId,
                    out resolvedBehaviour))
            {
                rejection = InteractionRejectionReason.TargetNotFound;
                return false;
            }

            NetworkObject targetObject = resolvedBehaviour.Object;
            if (targetObject == null || targetObject.Runner != Runner)
            {
                rejection = InteractionRejectionReason.WrongRunner;
                return false;
            }

            if (!targetObject.IsValid || !targetObject.IsInSimulation ||
                !targetObject.HasStateAuthority)
            {
                rejection = InteractionRejectionReason.TargetNotSpawned;
                return false;
            }

            if (resolvedBehaviour is not IAuthoritativeInteractable resolvedInteractable ||
                resolvedBehaviour is not WorldInventoryItem &&
                resolvedBehaviour is not NetworkKeyPickup)
            {
                rejection = InteractionRejectionReason.UnsupportedTarget;
                return false;
            }

            interactable = resolvedInteractable;
            bool itemAvailable = resolvedBehaviour switch
            {
                WorldInventoryItem worldItem => worldItem.IsAvailable,
                NetworkKeyPickup keyPickup => keyPickup.IsAvailable,
                _ => false
            };
            if (!itemAvailable)
            {
                rejection = InteractionRejectionReason.TargetUnavailable;
                return false;
            }

            if (IsLockerInputLocked)
            {
                rejection = InteractionRejectionReason.PlayerUnavailable;
                return false;
            }

            InteractionTarget requestedTarget = interactable.PromptTarget;
            if (requestedTarget == null ||
                !requestedTarget.isActiveAndEnabled ||
                localInteractionTargeting == null)
            {
                rejection = InteractionRejectionReason.MissingPromptTarget;
                return false;
            }

            Vector3 origin = ReplicatedViewPosition;
            Vector3 direction =
                Quaternion.Euler(LookPitch, LookYaw, 0f) * Vector3.forward;
            float maximumDistance =
                localInteractionTargeting.InteractionDistance +
                InteractionDistanceTolerance;
            Collider[] targetColliders =
                requestedTarget.GetComponentsInChildren<Collider>(true);
            Collider closestCollider = null;
            Vector3 closestPoint = default;
            float closestDistanceSquared = float.PositiveInfinity;
            bool forwardRayHitTarget = false;
            RaycastHit forwardTargetHit = default;
            Ray authoritativeViewRay = new(origin, direction);

            for (int index = 0; index < targetColliders.Length; index++)
            {
                Collider targetCollider = targetColliders[index];
                if (targetCollider == null || !targetCollider.enabled ||
                    !targetCollider.gameObject.activeInHierarchy ||
                    targetCollider.GetComponentInParent<InteractionTarget>() !=
                    requestedTarget)
                    continue;

                Vector3 point = targetCollider.ClosestPoint(origin);
                float distanceSquared = (point - origin).sqrMagnitude;
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestPoint = point;
                    closestCollider = targetCollider;
                }

                if (targetCollider.Raycast(
                        authoritativeViewRay,
                        out RaycastHit targetHit,
                        maximumDistance) &&
                    (!forwardRayHitTarget ||
                     targetHit.distance < forwardTargetHit.distance))
                {
                    forwardRayHitTarget = true;
                    forwardTargetHit = targetHit;
                }
            }

            if (closestCollider == null)
            {
                rejection = InteractionRejectionReason.MissingCollider;
                return false;
            }

            interactionDistance = Mathf.Sqrt(closestDistanceSquared);
            if (interactionDistance > maximumDistance)
            {
                rejection = InteractionRejectionReason.TooFar;
                return false;
            }

            Vector3 validationPoint = forwardRayHitTarget
                ? forwardTargetHit.point
                : closestPoint;
            Vector3 toTarget = validationPoint - origin;
            float validationRayDistance = toTarget.magnitude;
            if (validationRayDistance > InteractionSurfaceTolerance &&
                Vector3.Angle(direction, toTarget) >
                InteractionAimToleranceDegrees)
            {
                rejection = InteractionRejectionReason.LookingAway;
                return false;
            }

            if (validationRayDistance <= InteractionSurfaceTolerance)
                return true;

            RaycastHit[] sightHits = Physics.RaycastAll(
                origin,
                toTarget / validationRayDistance,
                validationRayDistance + InteractionSurfaceTolerance,
                localInteractionTargeting.InteractionRaycastMask,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(
                sightHits,
                (first, second) => first.distance.CompareTo(second.distance));
            for (int index = 0; index < sightHits.Length; index++)
            {
                Collider hitCollider = sightHits[index].collider;
                if (hitCollider == null ||
                    hitCollider.transform.IsChildOf(transform))
                    continue;

                InteractionTarget hitTarget =
                    hitCollider.GetComponentInParent<InteractionTarget>();
                if (hitTarget == requestedTarget)
                    return true;

                blockingCollider = hitCollider.name;
                rejection = InteractionRejectionReason.LineOfSightBlocked;
                return false;
            }

            rejection = InteractionRejectionReason.LineOfSightBlocked;
            return false;
        }

        private bool ValidateInteractionRequest(IAuthoritativeInteractable interactable)
        {
            if (interactable is LockerController locker && IsHiddenInLocker && IsUsingLocker(locker))
                return true;

            if (IsLockerInputLocked)
                return false;

            InteractionTarget requestedTarget = interactable.PromptTarget;
            if (requestedTarget == null || !requestedTarget.isActiveAndEnabled ||
                localInteractionTargeting == null || cameraRoot == null)
                return false;

            float distance = localInteractionTargeting.InteractionDistance;
            Vector3 origin = cameraRoot.position;
            Vector3 direction = Quaternion.Euler(LookPitch, LookYaw, 0f) * Vector3.forward;
            if (!Physics.Raycast(
                    origin,
                    direction,
                    out RaycastHit hit,
                    distance,
                    localInteractionTargeting.InteractionRaycastMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            InteractionTarget hitTarget =
                InteractionTarget.ResolveFromCollider(
                    hit.collider,
                    new Ray(origin, direction));
            return hitTarget == requestedTarget;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogInteractionRejection(
            NetworkBehaviourId targetId,
            NetworkBehaviour resolvedBehaviour,
            InteractionRejectionReason rejection,
            float interactionDistance,
            string blockingCollider)
        {
            string resolvedName = resolvedBehaviour != null
                ? $"{resolvedBehaviour.GetType().Name}/{resolvedBehaviour.name}"
                : "unresolved";
            NetworkObject targetObject = resolvedBehaviour != null
                ? resolvedBehaviour.Object
                : null;
            string targetAuthority = targetObject != null && targetObject.IsValid
                ? $"state={targetObject.StateAuthority}, input={targetObject.InputAuthority}"
                : "detached";
            PlayerRef requester = Object != null && Object.IsValid
                ? Object.InputAuthority
                : PlayerRef.None;
            Vector3 origin = ReplicatedViewPosition;
            Vector3 direction =
                Quaternion.Euler(LookPitch, LookYaw, 0f) * Vector3.forward;
            Debug.LogWarning(
                $"Fusion interaction rejected: reason={rejection}, requester={requester}, " +
                $"targetId={targetId}, target={resolvedName}, " +
                $"targetAuthority={targetAuthority}, distance={interactionDistance:F2}, " +
                $"origin={origin}, direction={direction}, blocker={blockingCollider ?? "none"}.",
                this);
        }

        public override void FixedUpdateNetwork()
        {
            if (IsProxy)
                return;

            if (IsDead)
            {
                IsSprinting = false;
                networkController.Move(Vector3.zero);
                UpdateCharacterControllerHeight(networkController.Grounded);
                return;
            }

            bool hasInput = GetInput(out FusionPlayerInput input);
            NetworkButtons pressed = default;
            if (hasInput)
            {
                LookYaw = Mathf.Repeat(input.LookAngles.x, 360f);
                LookPitch = Mathf.Clamp(input.LookAngles.y, pitchLimits.x, pitchLimits.y);
                pressed = input.Buttons.GetPressed(PreviousButtons);
                PreviousButtons = input.Buttons;

                if (!IsLockerInputLocked && inventory != null &&
                    inventory.HasStateAuthority &&
                    input.InventoryCommand !=
                    (byte)InventoryInputCommandType.None &&
                    input.InventoryCommandSequence !=
                    PreviousInventoryCommandSequence)
                {
                    PreviousInventoryCommandSequence =
                        input.InventoryCommandSequence;
                    inventory.ProcessInputCommandAuthoritative(
                        (InventoryInputCommandType)input.InventoryCommand,
                        input.InventoryInstanceId,
                        input.InventoryColumn,
                        input.InventoryRow,
                        input.InventoryRotated);
                }

                if (input.InteractionCommandSequence != 0 &&
                    input.InteractionCommandSequence !=
                    PreviousInteractionCommandSequence)
                {
                    PreviousInteractionCommandSequence =
                        input.InteractionCommandSequence;
                    ProcessInteractionRequest(input.InteractionTarget);
                }

                if (!IsLockerInputLocked && pressed.IsSet(FusionPlayerButton.UseEquipped))
                    inventory?.ToggleEquippedUseAuthoritative();

                if (!IsLockerInputLocked && pressed.IsSet(FusionPlayerButton.Crouch))
                {
                    if (IsCrouched || CanStand())
                    {
                        IsCrouched = !IsCrouched;
                        EmitAudioEvent(IsCrouched ? MovementAudioEvent.Crouch : MovementAudioEvent.Stand);
                    }
                }
            }

            if (IsLockerInputLocked)
            {
                IsSprinting = false;
                networkController.Velocity = Vector3.zero;
                networkController.Move(Vector3.zero);
                UpdateCharacterControllerHeight(networkController.Grounded);
                return;
            }

            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            bool hasForwardInput = moveInput.y > 0.01f;
            bool wantsToSprint = input.Buttons.IsSet(FusionPlayerButton.Sprint)
                && !IsCrouched
                && hasForwardInput
                && !SprintLocked;
            Vector2 effectiveMoveInput = wantsToSprint
                ? new Vector2(0f, Mathf.Clamp01(moveInput.y))
                : moveInput;
            Vector3 movement = Quaternion.Euler(0f, LookYaw, 0f)
                * new Vector3(effectiveMoveInput.x, 0f, effectiveMoveInput.y);
            bool wantsToMove = movement.sqrMagnitude > 0.01f;

            IsSprinting = wantsToSprint;
            networkController.maxSpeed = IsCrouched ? crouchSpeed : wantsToSprint ? sprintSpeed : walkSpeed;
            networkController.acceleration = wantsToSprint ? sprintAcceleration : walkAcceleration;
            networkController.braking = braking;
            networkController.gravity = gravity;
            networkController.jumpImpulse = jumpImpulse;

            bool wasGrounded = networkController.Grounded;
            if (pressed.IsSet(FusionPlayerButton.Jump) && wasGrounded && !IsCrouched)
            {
                networkController.Jump();
                BeginAirborneCharacterControllerShrink();
                Stamina = PlayerVitalsMath.SpendStamina(Stamina, jumpStaminaCost);
                StaminaRecoveryElapsed = 0f;
                EmitAudioEvent(MovementAudioEvent.Jump);
            }

            float bodyRotationSpeed = networkController.rotationSpeed;
            networkController.rotationSpeed = 0f;
            networkController.Move(movement);
            networkController.rotationSpeed = bodyRotationSpeed;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0f, LookYaw, 0f),
                Mathf.Max(0f, bodyRotationSpeed) * Runner.DeltaTime);

            bool isGrounded = networkController.Grounded;
            UpdateCharacterControllerHeight(isGrounded);
            if (!WasGrounded && isGrounded)
                EmitAudioEvent(MovementAudioEvent.Land);
            WasGrounded = isGrounded;

            float actualHorizontalSpeed = new Vector2(networkController.Velocity.x, networkController.Velocity.z).magnitude;
            bool actuallyMoving = wantsToMove && actualHorizontalSpeed > 0.08f;
            bool actuallySprinting = actuallyMoving && wantsToSprint;

            StaminaStep staminaStep = PlayerVitalsMath.UpdateStamina(
                Stamina,
                StaminaRecoveryElapsed,
                SprintLocked,
                actuallySprinting,
                Runner.DeltaTime,
                maximumStamina,
                staminaDrainRate,
                staminaRecoveryDelay,
                staminaRecoveryRate,
                staminaRestartThreshold);
            Stamina = staminaStep.Stamina;
            StaminaRecoveryElapsed = staminaStep.RecoveryElapsed;
            SprintLocked = staminaStep.SprintLocked;
            IsSprinting = actuallySprinting && !SprintLocked;

            if (isGrounded && actuallyMoving)
            {
                AccumulatedStepDistance += actualHorizontalSpeed * Runner.DeltaTime;
                float cadence = IsCrouched ? crouchStepDistance : IsSprinting ? sprintStepDistance : walkStepDistance;
                if (AccumulatedStepDistance >= cadence)
                {
                    AccumulatedStepDistance -= cadence;
                    EmitAudioEvent(MovementAudioEvent.Footstep);
                }
            }

            if (HasStateAuthority)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (pressed.IsSet(FusionPlayerButton.DebugExhaustion))
                    DebugRequestExhaustion();
                if (_debugExhaustionRequested)
                {
                    _debugExhaustionRequested = false;
                    Stamina = 0f;
                    StaminaRecoveryElapsed = 0f;
                    SprintLocked = true;
                }
#endif
                if (_pendingDamage > 0)
                {
                    int damage = _pendingDamage;
                    _pendingDamage = 0;
                    ApplyDamageAuthoritative(damage);
                }

                HealthStep healthStep = PlayerVitalsMath.RegenerateHealth(
                    Health,
                    TimeSinceDamage,
                    Runner.DeltaTime,
                    maximumHealth,
                    healthRegenerationDelay,
                    healthRegenerationRate);
                Health = healthStep.Health;
                TimeSinceDamage = healthStep.TimeSinceDamage;
            }
        }

        public void TakeDamage(int healthToSubtract)
        {
            if (!HasStateAuthority || IsDead || healthToSubtract <= 0)
                return;

            _pendingDamage = healthToSubtract > int.MaxValue - _pendingDamage
                ? int.MaxValue
                : _pendingDamage + healthToSubtract;
        }

        private void ApplyDamageAuthoritative(int healthToSubtract)
        {
            Health = Mathf.Max(0f, Health - healthToSubtract);
            TimeSinceDamage = 0f;
            if (Health <= 0f)
            {
                IsDead = true;
                ReleaseCurrentLockerAfterInvalidation();
            }
        }

        private void ReleaseCurrentLockerAfterInvalidation()
        {
            if (!HasStateAuthority || !CurrentLocker.IsValid || Runner == null)
                return;

            NetworkBehaviourId lockerId = CurrentLocker;
            CurrentLocker = default;
            if (Runner.TryFindBehaviour(lockerId, out NetworkBehaviour behaviour) &&
                behaviour is LockerController locker)
            {
                locker.ReleaseInvalidOccupantAuthoritative(this);
            }
        }

        public void BeginLocalDeathSequence(VideoClip jumpscareClip)
        {
            if (!HasInputAuthority || _deathSequenceStarted)
                return;

            _deathSequenceStarted = true;
            if (playerInput != null)
            {
                playerInput.DeactivateInput();
                playerInput.enabled = false;
            }
            IsSprinting = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (jumpscareClip == null || playerCamera == null)
                return;

            _deathVideoPlayer = playerCamera.gameObject.AddComponent<VideoPlayer>();
            _deathVideoPlayer.playOnAwake = false;
            _deathVideoPlayer.isLooping = false;
            _deathVideoPlayer.skipOnDrop = false;
            _deathVideoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            _deathVideoPlayer.targetCamera = playerCamera;
            _deathVideoPlayer.targetCameraAlpha = 1f;
            _deathVideoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            _deathVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _deathVideoPlayer.clip = jumpscareClip;
            _deathVideoPlayer.loopPointReached += HandleDeathVideoFinished;
            _deathVideoPlayer.errorReceived += HandleDeathVideoError;
            _deathVideoPlayer.Play();
        }

        private void HandleDeathVideoFinished(VideoPlayer source)
        {
            DisposeDeathVideoPlayer();
        }

        private void HandleDeathVideoError(VideoPlayer source, string message)
        {
            Debug.LogWarning($"Geo Monster jumpscare video could not finish: {message}", this);
            DisposeDeathVideoPlayer();
        }

        private void DisposeDeathVideoPlayer()
        {
            if (_deathVideoPlayer == null)
                return;
            _deathVideoPlayer.loopPointReached -= HandleDeathVideoFinished;
            _deathVideoPlayer.errorReceived -= HandleDeathVideoError;
            Destroy(_deathVideoPlayer);
            _deathVideoPlayer = null;
        }

#if UNITY_EDITOR
        public void TriggerInspectorDamage()
        {
            TakeDamage(inspectorDamageToSubtract);
        }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugRequestExhaustion()
        {
            if (HasStateAuthority)
                _debugExhaustionRequested = true;
        }

#endif

        private bool CanStand()
        {
            float radius = Mathf.Max(0.05f, characterController.radius * 0.95f);
            float floorClearance = Mathf.Max(0.01f, characterController.skinWidth);
            Vector3 bottom = transform.position + Vector3.up * (radius + floorClearance);
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _standingHits, standingCollisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _standingHits[i];
                if (hit != null && hit.transform.root != transform.root)
                    return false;
            }

            return true;
        }

        private void UpdateCharacterControllerHeight(bool grounded)
        {
            if (!grounded)
            {
                BeginAirborneCharacterControllerShrink();
                _airborneCharacterControllerHasClearedGround = true;
                return;
            }

            if (_airborneCharacterControllerShrunk
                && (!_airborneCharacterControllerHasClearedGround || networkController.Velocity.y > 0f))
            {
                characterController.height = _airborneCharacterControllerHeight;
                characterController.center = _airborneCharacterControllerCenter;
                return;
            }

            _airborneCharacterControllerShrunk = false;
            _airborneCharacterControllerHasClearedGround = false;
            float targetHeight = IsCrouched ? crouchingHeight : standingHeight;
            if (!IsCrouched
                && characterController.height < standingHeight
                && !CanStand())
            {
                characterController.center = Vector3.up * (characterController.height * 0.5f);
                return;
            }

            float restoreDistance = Mathf.Max(
                0.01f,
                Mathf.Abs(targetHeight - _airborneCharacterControllerHeight));
            float restoreSpeed = restoreDistance / Mathf.Max(0.01f, characterControllerRestoreDuration);
            characterController.height = Mathf.MoveTowards(
                characterController.height,
                targetHeight,
                restoreSpeed * Runner.DeltaTime);
            characterController.center = Vector3.up * (characterController.height * 0.5f);
        }

        private void BeginAirborneCharacterControllerShrink()
        {
            if (_airborneCharacterControllerShrunk)
                return;

            _airborneCharacterControllerHeight = Mathf.Max(
                characterController.radius * 2f,
                characterController.height - AirborneCharacterControllerHeightReduction);
            _airborneCharacterControllerCenter = Vector3.up * (_airborneCharacterControllerHeight * 0.5f);
            _airborneCharacterControllerShrunk = true;
            _airborneCharacterControllerHasClearedGround = false;
            characterController.height = _airborneCharacterControllerHeight;
            characterController.center = _airborneCharacterControllerCenter;
        }

        private void EmitAudioEvent(MovementAudioEvent audioEvent)
        {
            AudioEventCode = (byte)audioEvent;
            AudioEventSequence++;
        }

        public override void Render()
        {
            if (IsHiddenInLocker != _lastPresentedLockerHidden)
            {
                _lastPresentedLockerHidden = IsHiddenInLocker;
                SetCharacterVisibility(!IsHiddenInLocker);
            }

            animationDriver.RenderAnimation(Time.deltaTime);

            if (AudioEventSequence != _lastAudioEventSequence)
            {
                _lastAudioEventSequence = AudioEventSequence;
                PlayMovementAudio((MovementAudioEvent)AudioEventCode, HasInputAuthority);
            }

            if (!HasInputAuthority)
                return;

            gridInventory?.SetLockerInputLocked(IsLockerInputLocked);
            RenderOwnerCamera();
            RenderOwnerVignette();
        }

        private void SetCharacterVisibility(bool visible)
        {
            if (characterRenderers == null)
                return;

            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null)
                    characterRenderers[i].enabled = visible;
            }
        }

        private void SubscribeToOwnerCameraRendering()
        {
            if (_ownerCameraRenderingSubscribed)
                return;

            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
            _ownerCameraRenderingSubscribed = true;
        }

        private void UnsubscribeFromOwnerCameraRendering()
        {
            if (!_ownerCameraRenderingSubscribed)
                return;

            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
            _ownerCameraRenderingSubscribed = false;
            SetCharacterRenderingSuppressed(false);
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (renderingCamera == playerCamera)
                SetCharacterRenderingSuppressed(true);
        }

        private void HandleEndCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (renderingCamera == playerCamera)
                SetCharacterRenderingSuppressed(false);
        }

        private void SetCharacterRenderingSuppressed(bool suppressed)
        {
            if (characterRenderers == null)
                return;

            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null)
                    characterRenderers[i].forceRenderingOff = suppressed;
            }
        }

        private void RenderOwnerCamera()
        {
            if (TryGetLockerViewAnchor(out Transform viewAnchor))
            {
                _currentCameraHeight = 0f;
                cameraRoot.SetPositionAndRotation(
                    viewAnchor.position,
                    Quaternion.Euler(_localLookPitch, _localLookYaw, 0f));
                cameraMotion.localPosition = Vector3.zero;
                cameraMotion.localRotation = Quaternion.identity;
                return;
            }

            float targetHeight = IsCrouched ? crouchingCameraHeight : standingCameraHeight;
            _currentCameraHeight = Mathf.SmoothDamp(_currentCameraHeight, targetHeight, ref _cameraHeightVelocity, 1f / crouchTransitionSpeed);
            cameraRoot.localPosition = Vector3.up * _currentCameraHeight;

            float horizontalSpeed = new Vector2(networkController.Velocity.x, networkController.Velocity.z).magnitude;
            bool moving = networkController.Grounded && horizontalSpeed > 0.08f;
            float targetBlend = moving ? 1f : 0f;
            _bobBlend = Mathf.MoveTowards(_bobBlend, targetBlend, bobBlendSpeed * Time.deltaTime);

            Vector2 amount = IsCrouched ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount;
            float frequency = IsCrouched ? crouchBobFrequency : IsSprinting ? sprintBobFrequency : walkBobFrequency;
            _bobTime += Time.deltaTime * frequency * Mathf.Lerp(0.25f, 1f, _bobBlend);
            Vector3 bob = new(
                Mathf.Sin(_bobTime * 0.5f) * amount.x,
                Mathf.Abs(Mathf.Sin(_bobTime)) * amount.y,
                0f);
            cameraMotion.localPosition = Vector3.Lerp(cameraMotion.localPosition, bob * _bobBlend, bobBlendSpeed * Time.deltaTime);
            cameraMotion.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_bobTime * 0.5f) * amount.x * 12f * _bobBlend);
        }

        private bool TryGetLockerViewAnchor(out Transform viewAnchor)
        {
            viewAnchor = null;
            if (!IsHiddenInLocker || !CurrentLocker.IsValid || Runner == null ||
                !Runner.TryFindBehaviour(CurrentLocker, out NetworkBehaviour behaviour) ||
                behaviour is not LockerController locker)
                return false;

            viewAnchor = locker.PlayerViewAnchor;
            return viewAnchor != null;
        }

        private void CreateOwnerVignette()
        {
            UniversalAdditionalCameraData cameraData = playerCamera.GetComponent<UniversalAdditionalCameraData>();
            int volumeLayer = LayerMask.NameToLayer(OwnerPostProcessingLayerName);
            if (cameraData == null || volumeLayer < 0)
            {
                Debug.LogError($"Owner vignette requires URP camera data and the '{OwnerPostProcessingLayerName}' layer.", this);
                return;
            }

            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1 << volumeLayer;
            cameraData.volumeTrigger = playerCamera.transform;

            _runtimeVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeVolumeProfile.name = $"{name}_OwnerRuntimeVignette";
            _vignette = _runtimeVolumeProfile.Add<Vignette>(true);
            _vignette.active = true;
            _vignette.intensity.Override(0f);
            _vignette.smoothness.Override(0.88f);
            _vignette.rounded.Override(false);

            GameObject volumeObject = new($"{name}_OwnerCameraEffects");
            volumeObject.layer = volumeLayer;
            volumeObject.transform.SetParent(playerCamera.transform, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;
            volume.profile = _runtimeVolumeProfile;
        }

        private void RenderOwnerVignette()
        {
            if (_vignette == null)
                return;

            if (Health + 0.01f < _lastRenderedHealth)
                _damagePulse = 1f;
            _lastRenderedHealth = Health;
            _damagePulse = Mathf.MoveTowards(_damagePulse, 0f, Time.deltaTime * 0.65f);

            float staminaRatio = maximumStamina > 0f ? Stamina / maximumStamina : 1f;
            float healthRatio = maximumHealth > 0f ? Health / maximumHealth : 1f;
            float exhaustion = Mathf.Clamp01(Mathf.InverseLerp(0.55f, 0f, staminaRatio));
            if (SprintLocked)
                exhaustion = Mathf.Max(exhaustion, 0.9f);
            float lowHealth = Mathf.Clamp01(Mathf.InverseLerp(0.65f, 0.15f, healthRatio));
            float missingHealth = 1f - Mathf.Clamp01(healthRatio);

            float staminaIntensity = exhaustion * 0.46f;
            float persistentDamageIntensity = missingHealth > 0.001f
                ? Mathf.Lerp(0.18f, 0.28f, missingHealth)
                : 0f;
            float lowHealthIntensity = lowHealth * 0.38f;
            float healthIntensity = Mathf.Max(persistentDamageIntensity, lowHealthIntensity, _damagePulse * 0.62f);
            float targetIntensity = 1f - (1f - staminaIntensity) * (1f - healthIntensity);
            targetIntensity = Mathf.Min(targetIntensity, 0.65f);

            float redWeight = healthIntensity / Mathf.Max(0.001f, staminaIntensity + healthIntensity);
            redWeight = Mathf.Sqrt(Mathf.Clamp01(redWeight));
            Color targetColor = Color.Lerp(Color.black, new Color(0.9f, 0.01f, 0.01f), redWeight);
            float intensitySpeed = targetIntensity > _vignette.intensity.value ? 6f : 1.5f;
            _vignette.intensity.value = Mathf.MoveTowards(_vignette.intensity.value, targetIntensity, Time.deltaTime * intensitySpeed);
            _vignette.color.value = Color.Lerp(_vignette.color.value, targetColor, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        private void PlayMovementAudio(MovementAudioEvent audioEvent, bool localOwner)
        {
            AudioClip clip = audioEvent switch
            {
                MovementAudioEvent.Footstep => SelectFootstepClip(),
                MovementAudioEvent.Jump => jumpClip,
                MovementAudioEvent.Land => landingClip,
                MovementAudioEvent.Crouch => crouchClip,
                MovementAudioEvent.Stand => standClip,
                _ => null
            };
            if (clip == null)
                return;

            AudioSource source = localOwner ? localAudioSource : spatialAudioSource;
            float stanceVolume = IsCrouched ? 0.55f : IsSprinting ? 1f : 0.82f;
            source.pitch = 0.96f + (AudioEventSequence % 7) * 0.012f;
            float volume = localFootstepVolume * stanceVolume * (localOwner ? 1f : remoteVolumeMultiplier);
            if (audioEvent == MovementAudioEvent.Land)
                volume *= 1.1f;
            source.PlayOneShot(clip, volume);
        }

        private AudioClip SelectFootstepClip()
        {
            if (footstepClips == null || footstepClips.Length == 0)
                return null;
            return footstepClips[AudioEventSequence % footstepClips.Length];
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void MonitorOwnerLookYaw()
        {
            float yawDifference = Mathf.Abs(Mathf.DeltaAngle(_localLookYaw, LookYaw));
            if (yawDifference <= LookYawMismatchThreshold)
            {
                _lookYawMismatchStartedAt = -1f;
                return;
            }

            float now = Time.unscaledTime;
            if (_lookYawMismatchStartedAt < 0f)
            {
                _lookYawMismatchStartedAt = now;
                return;
            }

            if (now - _lookYawMismatchStartedAt < LookYawMismatchDuration ||
                now < _nextLookYawMismatchWarningTime)
                return;

            _nextLookYawMismatchWarningTime = now + LookYawMismatchWarningInterval;
            Debug.LogWarning(
                $"Owner look yaw differs from network yaw by {Mathf.DeltaAngle(_localLookYaw, LookYaw):F1} degrees.",
                this);
        }

        private void LogNetworkDiagnostics()
        {
            if (!HasInputAuthority && !HasStateAuthority)
                return;

            PlayerRef inputAuthority = Object.InputAuthority;
            double playerRttMs = inputAuthority.IsValid
                ? Runner.GetPlayerRtt(inputAuthority) * 1000d
                : 0d;
            string region = Runner.SessionInfo.IsValid
                ? Runner.SessionInfo.Region
                : "local";
            Debug.Log(
                $"Fusion player network: mode={Runner.GameMode}, region={region}, " +
                $"playerRtt={playerRttMs:F0} ms, inputAuthority={HasInputAuthority}, " +
                $"stateAuthority={HasStateAuthority}.",
                this);
        }
#endif

        private void OnDestroy()
        {
            DisposeDeathVideoPlayer();
            UnsubscribeFromOwnerCameraRendering();
            if (_runtimeVolumeProfile != null)
                Destroy(_runtimeVolumeProfile);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (hasState && CurrentLocker.IsValid &&
                runner.TryFindBehaviour(CurrentLocker, out NetworkBehaviour behaviour) &&
                behaviour is LockerController locker)
            {
                locker.ReleaseInvalidOccupantAuthoritative(this);
            }

            CurrentLocker = default;
            IsHiddenInLocker = false;
        }
    }
}
