using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Presentation-only controller for an instantiated held-item wrapper.
    /// It animates authored local transforms and an optional muzzle light, but
    /// never reads input or applies inventory, networking, or gameplay effects.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class HeldItemVisual : MonoBehaviour
    {
        private enum MotionProfile : byte
        {
            None,
            FlashlightToggle,
            RevivalInjection,
            MedKitBottle,
            AdrenalineBottle,
            CrowbarSwing,
            FireAxeSwing,
            RevolverRecoil,
            TranqRecoil,
            ShotgunRecoil,
            DryFire,
            Reload
        }

        [Header("Visual References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform rightHandGrip;
        [SerializeField] private Transform leftHandGrip;
        [SerializeField] private Transform effectOrigin;
        [SerializeField] private Light muzzleLight;

        [Header("Equip Motion")]
        [SerializeField, Min(0.01f)] private float equipDuration = 0.2f;
        [SerializeField] private Vector3 ownerEquipPositionOffset =
            new(0f, -0.12f, -0.08f);
        [SerializeField] private Vector3 remoteEquipPositionOffset =
            new(0f, -0.08f, -0.04f);
        [SerializeField] private Vector3 equipRotationOffset =
            new(12f, 0f, 4f);

        [Header("Third-person Reload")]
        [SerializeField, Min(0.1f)] private float remoteReloadDuration = 1.1f;
        [SerializeField] private Vector3 remoteReloadPositionOffset = new(0f, -0.10f, -0.04f);
        [SerializeField] private Vector3 remoteReloadRotationOffset = new(18f, -15f, -25f);

        [Header("Muzzle Flash")]
        [SerializeField, Min(0.01f)] private float muzzleFlashDuration = 0.055f;
        [SerializeField, Min(0f)] private float muzzleFlashIntensity = 8f;

        private InventoryItemDefinition _definition;
        private bool _ownerPresentation;
        private bool _configured;
        private bool _poseCaptured;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private Vector3 _baseLocalScale;
        private bool _equipActive;
        private float _equipElapsed;
        private bool _useActive;
        private float _useElapsed;
        private float _useDuration;
        private MotionProfile _motionProfile;
        private float _muzzleFlashRemaining;
        private float _authoredMuzzleIntensity;

        public InventoryItemDefinition Definition => _definition;
        public bool IsOwnerPresentation => _ownerPresentation;
        public Transform RightHandGrip => rightHandGrip;
        public Transform LeftHandGrip => leftHandGrip;
        public Transform EffectOrigin => effectOrigin != null ? effectOrigin : visualRoot;
        public bool IsUsePresentationActive => _useActive;

        private void Awake()
        {
            ResolveReferences();
            if (muzzleLight != null)
            {
                _authoredMuzzleIntensity = muzzleLight.intensity;
                muzzleLight.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_configured)
                BeginEquipMotion();
        }

        /// <summary>
        /// Configures this visual after it has been parented and positioned by
        /// PlayerEquipment. Repeating the same configuration is intentionally a
        /// no-op so a Render call cannot restart the equip motion.
        /// </summary>
        public void Configure(
            InventoryItemDefinition definition,
            bool ownerPresentation)
        {
            bool changed = !_configured ||
                           _definition != definition ||
                           _ownerPresentation != ownerPresentation;
            if (!changed)
                return;

            RestoreAuthoredPose();
            _definition = definition;
            _ownerPresentation = ownerPresentation;
            _configured = definition != null;
            ResolveReferences();
            CaptureAuthoredPose();
            CancelUse();

            if (_configured && isActiveAndEnabled)
                BeginEquipMotion();
        }

        /// <summary>
        /// Starts one non-looping use motion. The caller should invoke this once
        /// when it observes a new authoritative action sequence.
        /// </summary>
        public void PlayUse(bool dryFire)
        {
            PlayUseFromElapsed(dryFire, 0f);
        }

        /// <summary>
        /// Starts a use motion at an authoritative elapsed time. This lets a
        /// late joiner reconstruct an in-progress timed action without
        /// replaying its one-shot audio.
        /// </summary>
        public void PlayUseFromElapsed(bool dryFire, float elapsedSeconds)
        {
            if (!_configured || _definition == null)
                return;

            MotionProfile profile = ResolveMotionProfile(_definition, dryFire);
            if (profile == MotionProfile.None)
                return;

            _motionProfile = profile;
            _useDuration = ResolveUseDuration(_definition, profile);
            _useElapsed = Mathf.Clamp(
                elapsedSeconds,
                0f,
                _useDuration);
            _useActive = _useElapsed < _useDuration;

            if (!dryFire &&
                _definition.UseKind == InventoryItemUseKind.Firearm &&
                _useElapsed <= 0.01f)
                BeginMuzzleFlash();
        }

        /// <summary>Rough external reload, driven only by a confirmed action sequence.
        /// This does not alter ammunition or the owner's existing presentation.</summary>
        public void PlayRemoteReload()
        {
            if (!_configured || _ownerPresentation || _definition == null ||
                _definition.UseKind != InventoryItemUseKind.Firearm) return;
            CancelUse();
            _motionProfile = MotionProfile.Reload;
            _useDuration = Mathf.Max(.1f, remoteReloadDuration);
            _useElapsed = 0f;
            _useActive = true;
        }

        /// <summary>
        /// Stops only local presentation. It has no effect on an authoritative
        /// action that may still be running elsewhere.
        /// </summary>
        public void CancelUse()
        {
            _useActive = false;
            _useElapsed = 0f;
            _useDuration = 0f;
            _motionProfile = MotionProfile.None;
            StopMuzzleFlash();
        }

        private void Update()
        {
            float deltaTime = Mathf.Max(0f, Time.deltaTime);
            if (_equipActive)
            {
                _equipElapsed += deltaTime;
                if (_equipElapsed >= Mathf.Max(0.01f, equipDuration))
                    _equipActive = false;
            }

            if (_useActive)
            {
                // Keep confirmed external actions readable across a slow render frame.
                // This clock is cosmetic; gameplay and owner timing remain unchanged.
                _useElapsed += _ownerPresentation
                    ? deltaTime
                    : Mathf.Min(deltaTime, _useDuration * 0.25f);
                if (_useElapsed >= _useDuration)
                {
                    _useElapsed = _useDuration;
                    _useActive = false;
                }
            }

            if (_muzzleFlashRemaining > 0f)
            {
                _muzzleFlashRemaining = Mathf.Max(
                    0f,
                    _muzzleFlashRemaining - deltaTime);
                if (_muzzleFlashRemaining <= 0f)
                    StopMuzzleFlash();
            }

            // Evaluate the held pose before PlayerEquipmentRigController copies
            // its grip transforms into the persistent rig targets.
            if (!_poseCaptured || visualRoot == null)
                return;

            EvaluateEquipMotion(out Vector3 equipPosition, out Vector3 equipEuler);
            EvaluateUseMotion(out Vector3 usePosition, out Vector3 useEuler);
            visualRoot.localPosition =
                _baseLocalPosition + equipPosition + usePosition;
            visualRoot.localRotation = _baseLocalRotation *
                                       Quaternion.Euler(equipEuler + useEuler);
            visualRoot.localScale = _baseLocalScale;
        }

        private void EvaluateEquipMotion(
            out Vector3 positionOffset,
            out Vector3 eulerOffset)
        {
            if (!_equipActive)
            {
                positionOffset = Vector3.zero;
                eulerOffset = Vector3.zero;
                return;
            }

            float normalized = Mathf.Clamp01(
                _equipElapsed / Mathf.Max(0.01f, equipDuration));
            float remaining = 1f - EaseOutCubic(normalized);
            positionOffset = (_ownerPresentation
                    ? ownerEquipPositionOffset
                    : remoteEquipPositionOffset) *
                remaining;
            eulerOffset = equipRotationOffset * remaining;
        }

        private void EvaluateUseMotion(
            out Vector3 positionOffset,
            out Vector3 eulerOffset)
        {
            if (!_useActive || _useDuration <= 0f)
            {
                positionOffset = Vector3.zero;
                eulerOffset = Vector3.zero;
                return;
            }

            float normalized = Mathf.Clamp01(_useElapsed / _useDuration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            float readableScale = _ownerPresentation ? 1f : 1.1f;

            switch (_motionProfile)
            {
                case MotionProfile.Reload:
                    positionOffset = remoteReloadPositionOffset * pulse;
                    eulerOffset = remoteReloadRotationOffset * pulse;
                    break;

                case MotionProfile.FlashlightToggle:
                    positionOffset = new Vector3(0f, -0.006f, 0.012f) * pulse;
                    eulerOffset = new Vector3(-3f, 2f, -9f) * pulse;
                    break;

                case MotionProfile.RevivalInjection:
                {
                    float reach = pulse;
                    float plunger = SmoothRange(0.42f, 0.7f, normalized) *
                                    (1f - SmoothRange(0.78f, 1f, normalized));
                    positionOffset = new Vector3(
                        0.015f * reach,
                        -0.025f * plunger,
                        (_ownerPresentation ? 0.13f : 0.20f) * reach);
                    eulerOffset = new Vector3(
                        18f * reach,
                        -5f * reach,
                        -7f * plunger);
                    break;
                }

                case MotionProfile.MedKitBottle:
                    positionOffset = new Vector3(-0.015f, 0.17f, -0.035f) * pulse;
                    eulerOffset = new Vector3(-58f, 3f, 9f) * pulse;
                    break;

                case MotionProfile.AdrenalineBottle:
                {
                    float urgency = Mathf.Clamp01(pulse * 1.18f);
                    positionOffset = new Vector3(0.02f, 0.2f, -0.02f) * urgency;
                    eulerOffset = new Vector3(-70f, -4f, -12f) * urgency;
                    break;
                }

                case MotionProfile.CrowbarSwing:
                {
                    float arc = Mathf.Sin(normalized * Mathf.PI);
                    positionOffset = new Vector3(0.035f, 0.025f, 0.075f) * arc;
                    eulerOffset = new Vector3(-68f, 15f, -24f) * arc * readableScale;
                    break;
                }

                case MotionProfile.FireAxeSwing:
                {
                    float arc = Mathf.Sin(normalized * Mathf.PI);
                    positionOffset = new Vector3(0.02f, 0.055f, 0.1f) * arc;
                    eulerOffset = new Vector3(-105f, 12f, -30f) * arc * readableScale;
                    break;
                }

                case MotionProfile.RevolverRecoil:
                {
                    float kick = EarlyKick(normalized);
                    positionOffset = new Vector3(0f, 0.012f, -0.055f) * kick;
                    eulerOffset = new Vector3(-18f, 0f, 2f) * kick * readableScale;
                    break;
                }

                case MotionProfile.TranqRecoil:
                {
                    float kick = EarlyKick(normalized);
                    positionOffset = new Vector3(0f, 0.006f, -0.028f) * kick;
                    eulerOffset = new Vector3(-7f, 0f, 1f) * kick * readableScale;
                    break;
                }

                case MotionProfile.ShotgunRecoil:
                {
                    float kick = EarlyKick(normalized);
                    positionOffset = new Vector3(0f, 0.025f, -0.13f) * kick;
                    eulerOffset = new Vector3(-32f, 0f, 4f) * kick * readableScale;
                    break;
                }

                case MotionProfile.DryFire:
                {
                    float click = EarlyKick(normalized);
                    positionOffset = new Vector3(0f, 0f, -0.006f) * click;
                    eulerOffset = new Vector3(-2.5f, 0f, 1.5f) * click;
                    break;
                }

                default:
                    positionOffset = Vector3.zero;
                    eulerOffset = Vector3.zero;
                    break;
            }
        }

        private void BeginEquipMotion()
        {
            _equipElapsed = 0f;
            _equipActive = true;
        }

        private void BeginMuzzleFlash()
        {
            if (muzzleLight == null)
                return;

            _muzzleFlashRemaining = Mathf.Max(0.01f, muzzleFlashDuration);
            muzzleLight.intensity = Mathf.Max(
                _authoredMuzzleIntensity,
                muzzleFlashIntensity);
            muzzleLight.enabled = true;
        }

        private void StopMuzzleFlash()
        {
            _muzzleFlashRemaining = 0f;
            if (muzzleLight == null)
                return;

            muzzleLight.enabled = false;
            muzzleLight.intensity = _authoredMuzzleIntensity;
        }

        private static MotionProfile ResolveMotionProfile(
            InventoryItemDefinition definition,
            bool dryFire)
        {
            if (dryFire && definition.UseKind == InventoryItemUseKind.Firearm)
                return MotionProfile.DryFire;

            switch (definition.UseKind)
            {
                case InventoryItemUseKind.FlashlightToggle:
                    return MotionProfile.FlashlightToggle;
                case InventoryItemUseKind.RevivalSyringe:
                    return MotionProfile.RevivalInjection;
                case InventoryItemUseKind.FullHeal:
                    return MotionProfile.MedKitBottle;
                case InventoryItemUseKind.Adrenaline:
                    return MotionProfile.AdrenalineBottle;
                case InventoryItemUseKind.Crowbar:
                    return MotionProfile.CrowbarSwing;
                case InventoryItemUseKind.FireAxe:
                    return MotionProfile.FireAxeSwing;
                case InventoryItemUseKind.Firearm:
                {
                    string itemId = definition.ItemId?.Trim().ToLowerInvariant();
                    return itemId switch
                    {
                        "tranq_gun" => MotionProfile.TranqRecoil,
                        "sawed_off_shotgun" => MotionProfile.ShotgunRecoil,
                        _ => MotionProfile.RevolverRecoil
                    };
                }
                default:
                    return MotionProfile.None;
            }
        }

        private static float ResolveUseDuration(
            InventoryItemDefinition definition,
            MotionProfile profile)
        {
            if (definition.UseDuration > 0.01f)
                return definition.UseDuration;

            return profile switch
            {
                MotionProfile.FlashlightToggle => 0.2f,
                MotionProfile.RevivalInjection => 1.5f,
                MotionProfile.MedKitBottle => 1.25f,
                MotionProfile.AdrenalineBottle => 1f,
                MotionProfile.CrowbarSwing => 0.7f,
                MotionProfile.FireAxeSwing => 0.9f,
                MotionProfile.RevolverRecoil => 0.28f,
                MotionProfile.TranqRecoil => 0.3f,
                MotionProfile.ShotgunRecoil => 0.48f,
                MotionProfile.DryFire => 0.2f,
                _ => 0.25f
            };
        }

        private void CaptureAuthoredPose()
        {
            if (visualRoot == null)
                return;

            _baseLocalPosition = visualRoot.localPosition;
            _baseLocalRotation = visualRoot.localRotation;
            _baseLocalScale = visualRoot.localScale;
            _poseCaptured = true;
        }

        private void RestoreAuthoredPose()
        {
            if (!_poseCaptured || visualRoot == null)
                return;

            visualRoot.localPosition = _baseLocalPosition;
            visualRoot.localRotation = _baseLocalRotation;
            visualRoot.localScale = _baseLocalScale;
        }

        private void ResolveReferences()
        {
            visualRoot ??= transform;
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private static float SmoothRange(float start, float end, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
        }

        private static float EarlyKick(float normalized)
        {
            float attack = Mathf.Sin(
                Mathf.Clamp01(normalized / 0.35f) * Mathf.PI);
            float decay = 1f - SmoothRange(0.18f, 1f, normalized);
            return Mathf.Clamp01(attack * decay);
        }

        private void OnDisable()
        {
            _equipActive = false;
            CancelUse();
            RestoreAuthoredPose();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            equipDuration = Mathf.Max(0.01f, equipDuration);
            muzzleFlashDuration = Mathf.Max(0.01f, muzzleFlashDuration);
            muzzleFlashIntensity = Mathf.Max(0f, muzzleFlashIntensity);
        }
#endif
    }
}
