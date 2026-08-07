using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Presentation-only third-person hand rig. Grip transforms remain owned by
    /// the equipped visual; this component copies them into persistent targets
    /// before the persistent Animation Rigging graph evaluates.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(RigBuilder))]
    public sealed class PlayerEquipmentRigController : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Animator animator;
        [SerializeField] private RigBuilder rigBuilder;
        [SerializeField] private Rig rightArmRig;
        [SerializeField] private Rig leftArmRig;
        [SerializeField] private TwoBoneIKConstraint rightArmConstraint;
        [SerializeField] private TwoBoneIKConstraint leftArmConstraint;

        [Header("Persistent Targets")]
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightElbowHint;
        [SerializeField] private Transform leftElbowHint;

        [Header("Blend")]
        [SerializeField, Min(0f)] private float blendSpeed = 10f;

        private PlayerEquipment _equipment;
        private Transform _rightGripSource;
        private Transform _leftGripSource;
        private InventoryHoldStyle _holdStyle;
        private float _currentRightWeight;
        private float _currentLeftWeight;
        private Vector3 _rightBlendStartPosition;
        private Quaternion _rightBlendStartRotation = Quaternion.identity;
        private Vector3 _leftBlendStartPosition;
        private Quaternion _leftBlendStartRotation = Quaternion.identity;
        private bool _activationPending;
        private int _inactiveResetFrames;

        public PlayerEquipment Equipment => _equipment;
        public Transform RightGripSource => _rightGripSource;
        public Transform LeftGripSource => _leftGripSource;
        public InventoryHoldStyle HoldStyle => _holdStyle;
        public float CurrentRightWeight => _currentRightWeight;
        public float CurrentLeftWeight => _currentLeftWeight;
        public float DesiredRightWeight => IsValidSource(_rightGripSource) ? 1f : 0f;
        public float DesiredLeftWeight =>
            _holdStyle == InventoryHoldStyle.TwoHanded &&
            IsValidSource(_leftGripSource)
                ? 1f
                : 0f;

        private void Awake()
        {
            ResolveReferences();
            InitializeTargetsFromHands();
        }

        private void OnEnable()
        {
            ResolveReferences();
            InitializeTargetsFromHands();
            SetArmLayersActive(false, false);
        }

        public void Configure(
            PlayerEquipment equipment,
            Transform rightGripSource,
            Transform leftGripSource,
            InventoryHoldStyle holdStyle)
        {
            bool changed = _equipment != equipment ||
                           _rightGripSource != rightGripSource ||
                           _leftGripSource != leftGripSource ||
                           _holdStyle != holdStyle;
            _equipment = equipment;
            _rightGripSource = rightGripSource;
            _leftGripSource = leftGripSource;
            _holdStyle = equipment != null
                ? holdStyle
                : InventoryHoldStyle.None;
            if (!changed)
                return;

            _currentRightWeight = 0f;
            _currentLeftWeight = 0f;
            _activationPending = equipment != null;
            _inactiveResetFrames = _activationPending ? 1 : 0;
            SetArmLayersActive(false, false);
            CaptureBlendStartsFromHands();
        }

        public void Clear(PlayerEquipment equipment = null)
        {
            if (equipment != null && equipment != _equipment)
                return;

            _equipment = null;
            _rightGripSource = null;
            _leftGripSource = null;
            _holdStyle = InventoryHoldStyle.None;
            _currentRightWeight = 0f;
            _currentLeftWeight = 0f;
            _activationPending = false;
            _inactiveResetFrames = 0;
            SetArmLayersActive(false, false);
        }

        private void Update()
        {
            if (_equipment == null)
                return;

            if (_activationPending && _inactiveResetFrames > 0)
            {
                _inactiveResetFrames--;
                CaptureBlendStartsFromHands();
                return;
            }

            if (_activationPending)
            {
                CaptureBlendStartsFromHands();
                SetArmLayersActive(
                    DesiredRightWeight > 0f,
                    DesiredLeftWeight > 0f);
                _activationPending = false;
            }

            float delta = blendSpeed <= 0f
                ? 1f
                : Mathf.Max(0f, Time.deltaTime) * blendSpeed;
            _currentRightWeight = Mathf.MoveTowards(
                _currentRightWeight,
                DesiredRightWeight,
                delta);
            _currentLeftWeight = Mathf.MoveTowards(
                _currentLeftWeight,
                DesiredLeftWeight,
                delta);

            UpdateBlendedTarget(
                _rightGripSource,
                rightHandTarget,
                _rightBlendStartPosition,
                _rightBlendStartRotation,
                _currentRightWeight);
            UpdateBlendedTarget(
                _leftGripSource,
                leftHandTarget,
                _leftBlendStartPosition,
                _leftBlendStartRotation,
                _currentLeftWeight);

            if (DesiredRightWeight <= 0f)
                SetArmLayerActive(rightArmRig, false);
            if (DesiredLeftWeight <= 0f)
                SetArmLayerActive(leftArmRig, false);
        }

        private void CaptureBlendStartsFromHands()
        {
            if (animator == null || !animator.isHuman)
                return;

            CaptureBlendStart(
                animator.GetBoneTransform(HumanBodyBones.RightHand),
                rightHandTarget,
                out _rightBlendStartPosition,
                out _rightBlendStartRotation);
            CaptureBlendStart(
                animator.GetBoneTransform(HumanBodyBones.LeftHand),
                leftHandTarget,
                out _leftBlendStartPosition,
                out _leftBlendStartRotation);
        }

        private static void CaptureBlendStart(
            Transform hand,
            Transform target,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = hand != null ? hand.position : Vector3.zero;
            rotation = hand != null ? hand.rotation : Quaternion.identity;
            if (target != null)
                target.SetPositionAndRotation(position, rotation);
        }

        private static void UpdateBlendedTarget(
            Transform source,
            Transform target,
            Vector3 startPosition,
            Quaternion startRotation,
            float weight)
        {
            if (!IsValidSource(source) || target == null)
                return;

            float easedWeight = weight * weight * (3f - 2f * weight);
            target.SetPositionAndRotation(
                Vector3.Lerp(startPosition, source.position, easedWeight),
                Quaternion.Slerp(startRotation, source.rotation, easedWeight));
        }

        private void InitializeTargetsFromHands()
        {
            CaptureBlendStartsFromHands();
        }

        private void SetArmLayersActive(bool rightActive, bool leftActive)
        {
            // Independent persistent layers let one-handed equipment and clear
            // paths release the left arm without rebuilding the rig graph.
            SetArmLayerActive(rightArmRig, rightActive);
            SetArmLayerActive(leftArmRig, leftActive);
        }

        private void SetArmLayerActive(Rig armRig, bool active)
        {
            if (rigBuilder == null || armRig == null)
                return;

            for (int index = 0; index < rigBuilder.layers.Count; index++)
            {
                RigLayer layer = rigBuilder.layers[index];
                if (layer.rig == armRig)
                {
                    layer.active = active;
                    return;
                }
            }
        }

        private static bool IsValidSource(Transform source)
        {
            return source != null && source.gameObject.activeInHierarchy;
        }

        private void ResolveReferences()
        {
            animator ??= GetComponent<Animator>();
            rigBuilder ??= GetComponent<RigBuilder>();
        }

        private void OnDisable()
        {
            Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            blendSpeed = Mathf.Max(0f, blendSpeed);
        }
#endif
    }
}
