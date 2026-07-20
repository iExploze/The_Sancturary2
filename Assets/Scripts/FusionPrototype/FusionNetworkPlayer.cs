using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheSancturary.FusionPrototype
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkCharacterController), typeof(CharacterController))]
    public sealed class FusionNetworkPlayer : NetworkBehaviour
    {
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
        [SerializeField] private Transform visibleBody;
        [SerializeField] private Renderer visibleBodyRenderer;
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
        [SerializeField, Min(0.001f)] private float lookSensitivity = 0.08f;
        [SerializeField] private Vector2 pitchLimits = new(-85f, 85f);

        [Header("Crouching")]
        [SerializeField, Min(0.5f)] private float standingHeight = 1.8f;
        [SerializeField, Min(0.5f)] private float crouchingHeight = 1.1f;
        [SerializeField, Min(0f)] private float standingCameraHeight = 1.62f;
        [SerializeField, Min(0f)] private float crouchingCameraHeight = 0.92f;
        [SerializeField, Min(0.1f)] private float crouchTransitionSpeed = 5f;
        [SerializeField] private LayerMask standingCollisionMask = ~0;

        [Header("Stamina")]
        [SerializeField, Min(0.01f)] private float maximumStamina = 100f;
        [SerializeField, Min(0f)] private float staminaDrainRate = 20f;
        [SerializeField, Min(0f)] private float staminaRecoveryDelay = 1.25f;
        [SerializeField, Min(0f)] private float staminaRecoveryRate = 24f;
        [SerializeField, Min(0f)] private float staminaRestartThreshold = 30f;

        [Header("Health")]
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float healthRegenerationDelay = 5f;
        [SerializeField, Min(0f)] private float healthRegenerationRate = 8f;
        [SerializeField, Min(0f)] private float debugDamageAmount = 25f;

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
        private VolumeProfile _runtimeVolumeProfile;
        private Vignette _vignette;
        private byte _lastAudioEventSequence;
        private float _lastRenderedHealth;
        private float _damagePulse;
        private float _cameraHeightVelocity;
        private float _currentCameraHeight;
        private float _renderedBodyHeight;
        private float _bobBlend;
        private float _bobTime;

        public override void Spawned()
        {
            ResolveReferences();

            if (HasStateAuthority)
            {
                Stamina = maximumStamina;
                Health = maximumHealth;
                LookYaw = transform.eulerAngles.y;
                WasGrounded = networkController.Grounded;
            }

            bool isOwner = HasInputAuthority;
            playerInput.enabled = isOwner;
            playerCamera.enabled = isOwner;
            audioListener.enabled = isOwner;
            visibleBodyRenderer.enabled = !isOwner;

            if (isOwner)
            {
                CacheInputActions();
                playerInput.ActivateInput();
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
            _renderedBodyHeight = standingHeight;
        }

        private void ResolveReferences()
        {
            networkController ??= GetComponent<NetworkCharacterController>();
            characterController ??= GetComponent<CharacterController>();
            playerInput ??= GetComponent<PlayerInput>();
            localAudioSource ??= GetComponent<AudioSource>();
            if (spatialAudioSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                spatialAudioSource = sources.Length > 1 ? sources[1] : localAudioSource;
            }
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

        public FusionPlayerInput BuildNetworkInput()
        {
            FusionPlayerInput input = default;
            if (!HasInputAuthority || !playerInput.enabled)
                return input;

            input.Move = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            input.Look = _lookAction.ReadValue<Vector2>();
            input.Buttons.Set(FusionPlayerButton.Jump, _jumpAction.IsPressed());
            input.Buttons.Set(FusionPlayerButton.Sprint, _sprintAction.IsPressed());

            bool keyboardCrouch = Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
            input.Buttons.Set(FusionPlayerButton.Crouch, _crouchAction.IsPressed() || keyboardCrouch);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool debugDamage = Keyboard.current != null && Keyboard.current.f8Key.isPressed;
            input.Buttons.Set(FusionPlayerButton.DebugDamage, debugDamage);
#endif
            return input;
        }

        public override void FixedUpdateNetwork()
        {
            if (IsProxy)
                return;

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

            float targetHeight = IsCrouched ? crouchingHeight : standingHeight;
            characterController.height = Mathf.MoveTowards(characterController.height, targetHeight, crouchTransitionSpeed * Runner.DeltaTime);
            characterController.center = Vector3.up * (characterController.height * 0.5f);

            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 movement = Quaternion.Euler(0f, LookYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
            bool wantsToMove = movement.sqrMagnitude > 0.01f;
            bool wantsToSprint = input.Buttons.IsSet(FusionPlayerButton.Sprint) && !IsCrouched && wantsToMove && !SprintLocked;

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
                EmitAudioEvent(MovementAudioEvent.Jump);
            }

            networkController.Move(movement);
            transform.rotation = Quaternion.Euler(0f, LookYaw, 0f);

            bool isGrounded = networkController.Grounded;
            if (!WasGrounded && isGrounded)
                EmitAudioEvent(MovementAudioEvent.Land);
            WasGrounded = isGrounded;

            float actualHorizontalSpeed = new Vector2(networkController.Velocity.x, networkController.Velocity.z).magnitude;
            bool actuallyMoving = isGrounded && wantsToMove && actualHorizontalSpeed > 0.08f;
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

            if (actuallyMoving)
            {
                AccumulatedStepDistance += actualHorizontalSpeed * Runner.DeltaTime;
                float cadence = IsCrouched ? crouchStepDistance : wantsToSprint ? sprintStepDistance : walkStepDistance;
                if (AccumulatedStepDistance >= cadence)
                {
                    AccumulatedStepDistance -= cadence;
                    EmitAudioEvent(MovementAudioEvent.Footstep);
                }
            }

            if (HasStateAuthority)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (pressed.IsSet(FusionPlayerButton.DebugDamage))
                    ApplyDamage(debugDamageAmount);
#endif
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

        public void ApplyDamage(float amount)
        {
            if (!HasStateAuthority || amount <= 0f)
                return;

            Health = Mathf.Max(0f, Health - amount);
            TimeSinceDamage = 0f;
        }

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

        private void EmitAudioEvent(MovementAudioEvent audioEvent)
        {
            AudioEventCode = (byte)audioEvent;
            AudioEventSequence++;
        }

        public override void Render()
        {
            float targetBodyHeight = IsCrouched ? crouchingHeight : standingHeight;
            _renderedBodyHeight = Mathf.MoveTowards(_renderedBodyHeight, targetBodyHeight, crouchTransitionSpeed * Time.deltaTime);
            visibleBody.localScale = new Vector3(0.68f, _renderedBodyHeight * 0.5f, 0.68f);
            visibleBody.localPosition = Vector3.up * (_renderedBodyHeight * 0.5f);

            if (AudioEventSequence != _lastAudioEventSequence)
            {
                _lastAudioEventSequence = AudioEventSequence;
                PlayMovementAudio((MovementAudioEvent)AudioEventCode, HasInputAuthority);
            }

            if (!HasInputAuthority)
                return;

            RenderOwnerCamera();
            RenderOwnerVignette();
            HandleCursorDebugging();
        }

        private void RenderOwnerCamera()
        {
            float targetHeight = IsCrouched ? crouchingCameraHeight : standingCameraHeight;
            _currentCameraHeight = Mathf.SmoothDamp(_currentCameraHeight, targetHeight, ref _cameraHeightVelocity, 1f / crouchTransitionSpeed);
            cameraRoot.localPosition = Vector3.up * _currentCameraHeight;
            cameraRoot.localRotation = Quaternion.Euler(LookPitch, 0f, 0f);

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
            _runtimeVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeVolumeProfile.name = $"{name}_OwnerRuntimeVignette";
            _vignette = _runtimeVolumeProfile.Add<Vignette>(true);
            _vignette.active = true;
            _vignette.intensity.Override(0f);
            _vignette.smoothness.Override(0.88f);
            _vignette.rounded.Override(false);

            Volume volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.profile = _runtimeVolumeProfile;
        }

        private void RenderOwnerVignette()
        {
            if (_vignette == null)
                return;

            if (Health + 0.01f < _lastRenderedHealth)
                _damagePulse = 1f;
            _lastRenderedHealth = Health;
            _damagePulse = Mathf.MoveTowards(_damagePulse, 0f, Time.deltaTime * 1.7f);

            float staminaRatio = maximumStamina > 0f ? Stamina / maximumStamina : 1f;
            float healthRatio = maximumHealth > 0f ? Health / maximumHealth : 1f;
            float exhaustion = Mathf.Clamp01(Mathf.InverseLerp(0.55f, 0f, staminaRatio));
            if (SprintLocked)
                exhaustion = Mathf.Max(exhaustion, 0.9f);
            float lowHealth = Mathf.Clamp01(Mathf.InverseLerp(0.65f, 0.15f, healthRatio));
            float redWeight = Mathf.Clamp01(Mathf.Max(lowHealth * 0.7f, _damagePulse));

            float targetIntensity = Mathf.Clamp(exhaustion * 0.48f + lowHealth * 0.22f + _damagePulse * 0.3f, 0f, 0.62f);
            Color targetColor = Color.Lerp(Color.black, new Color(0.55f, 0.015f, 0.015f), redWeight);
            _vignette.intensity.value = Mathf.MoveTowards(_vignette.intensity.value, targetIntensity, Time.deltaTime * 1.8f);
            _vignette.color.value = Color.Lerp(_vignette.color.value, targetColor, Time.deltaTime * 5f);
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
            if (_runtimeVolumeProfile != null)
                Destroy(_runtimeVolumeProfile);
        }
    }
}
