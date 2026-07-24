using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;

namespace TheSancturary.FusionPrototype
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkCharacterController), typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAnimationDriver))]
    public sealed class FusionNetworkPlayer : NetworkBehaviour
    {
        private const string OwnerPostProcessingLayerName = "OwnerPostProcessing";
        private const float AirborneCharacterControllerHeightReduction = 0.5f;

        private enum MovementAudioEvent : byte
        {
            None,
            Footstep,
            Jump,
            Land,
            Crouch,
            Stand
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
        [Networked] private NetworkBool WasGrounded { get; set; }
        [Networked] private float AccumulatedStepDistance { get; set; }
        [Networked] private byte AudioEventSequence { get; set; }
        [Networked] private byte AudioEventCode { get; set; }

        private readonly Collider[] _standingHits = new Collider[16];
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private Vector2 _pendingNetworkLookDelta;
        private float _localLookYaw;
        private float _localLookPitch;
        private VolumeProfile _runtimeVolumeProfile;
        private Vignette _vignette;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _debugExhaustionRequested;
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

        public bool IsDeadOrPending => IsDead || Health - _pendingDamage <= 0f;

        public float AnimationReferenceSpeed => IsCrouched ? crouchSpeed : walkSpeed;

        public override void Spawned()
        {
            ResolveReferences();

            if (HasStateAuthority)
            {
                Stamina = maximumStamina;
                Health = maximumHealth;
                IsDead = false;
                LookYaw = transform.eulerAngles.y;
                WasGrounded = networkController.Grounded;
            }

            bool isOwner = HasInputAuthority;
            playerInput.enabled = isOwner;
            playerCamera.enabled = isOwner;
            audioListener.enabled = isOwner;
            SetCharacterVisibility(true);
            SetCharacterRenderingSuppressed(false);
            animationDriver.Initialize();

            if (isOwner)
            {
                SubscribeToOwnerCameraRendering();
                CacheInputActions();
                playerInput.ActivateInput();
                _localLookYaw = transform.eulerAngles.y;
                _localLookPitch = Mathf.Clamp(LookPitch, pitchLimits.x, pitchLimits.y);
                ApplyOwnerCameraLook();
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
        }

        private void ResolveReferences()
        {
            networkController ??= GetComponent<NetworkCharacterController>();
            characterController ??= GetComponent<CharacterController>();
            playerInput ??= GetComponent<PlayerInput>();
            animationDriver ??= GetComponent<PlayerAnimationDriver>();
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
        }

        private void Update()
        {
            if (!HasInputAuthority || playerInput == null || !playerInput.enabled)
                return;

            HandleCursorDebugging();
            SampleOwnerLook();
            ApplyOwnerCameraLook();
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
            _pendingNetworkLookDelta += lookDelta;
        }

        private void ApplyOwnerCameraLook()
        {
            if (cameraRoot != null)
                cameraRoot.rotation = Quaternion.Euler(_localLookPitch, _localLookYaw, 0f);
        }

        public FusionPlayerInput BuildNetworkInput()
        {
            FusionPlayerInput input = default;
            if (!HasInputAuthority || !playerInput.enabled)
                return input;

            input.Move = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            input.Look = _pendingNetworkLookDelta;
            _pendingNetworkLookDelta = Vector2.zero;
            input.Buttons.Set(FusionPlayerButton.Jump, _jumpAction.IsPressed());
            input.Buttons.Set(FusionPlayerButton.Sprint, _sprintAction.IsPressed());

            bool keyboardCrouch = Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
            input.Buttons.Set(FusionPlayerButton.Crouch, _crouchAction.IsPressed() || keyboardCrouch);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool debugExhaustion = Keyboard.current != null && Keyboard.current.f7Key.isPressed;
            input.Buttons.Set(FusionPlayerButton.DebugExhaustion, debugExhaustion);
#endif
            return input;
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

            FusionPlayerInput input = default;
            GetInput(out input);
            NetworkButtons pressed = input.Buttons.GetPressed(PreviousButtons);
            PreviousButtons = input.Buttons;

            LookYaw = Mathf.Repeat(LookYaw + input.Look.x * lookSensitivity, 360f);
            LookPitch = Mathf.Clamp(LookPitch - input.Look.y * lookSensitivity, pitchLimits.x, pitchLimits.y);

            if (pressed.IsSet(FusionPlayerButton.Crouch))
            {
                if (IsCrouched || CanStand())
                {
                    IsCrouched = !IsCrouched;
                    EmitAudioEvent(IsCrouched ? MovementAudioEvent.Crouch : MovementAudioEvent.Stand);
                }
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
                IsDead = true;
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
            animationDriver.RenderAnimation(Time.deltaTime);

            if (AudioEventSequence != _lastAudioEventSequence)
            {
                _lastAudioEventSequence = AudioEventSequence;
                PlayMovementAudio((MovementAudioEvent)AudioEventCode, HasInputAuthority);
            }

            if (!HasInputAuthority)
                return;

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

        private static void HandleCursorDebugging()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
        }

        private void OnDestroy()
        {
            DisposeDeathVideoPlayer();
            UnsubscribeFromOwnerCameraRendering();
            if (_runtimeVolumeProfile != null)
                Destroy(_runtimeVolumeProfile);
        }
    }
}
