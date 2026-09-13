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
        public const string HoldPoseParameterName = "HoldPoseId";
        public const string OneHandPoseLayerName = "Item Pose - One Hand";
        public const string TwoHandPoseLayerName = "Item Pose - Two Hand";
        private static readonly int HoldPoseHash =
            Animator.StringToHash(HoldPoseParameterName);

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
        [SerializeField, Range(0f, 1f)] private float rightHandPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float rightHandRotationWeight = 0.92f;
        [SerializeField, Range(0f, 1f)] private float leftHandPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float leftHandRotationWeight = 0.84f;
        [SerializeField, Range(0f, 1f)] private float holdPoseLayerWeight = 0.9f;

        [Header("Elbow Hint Profiles")]
        [SerializeField] private Vector3 oneHandedRightHint =
            new(0.52f, 1.25f, 0.18f);
        [SerializeField] private Vector3 pistolRightHint =
            new(0.45f, 1.36f, 0.08f);
        [SerializeField] private Vector3 longGunRightHint =
            new(0.5f, 1.27f, 0.08f);
        [SerializeField] private Vector3 longGunLeftHint =
            new(-0.5f, 1.26f, 0.12f);
        [SerializeField] private Vector3 toolRightHint =
            new(0.48f, 1.18f, 0.1f);
        [SerializeField] private Vector3 toolLeftHint =
            new(-0.46f, 1.39f, 0.16f);

        private PlayerEquipment _equipment;
        private Transform _rightGripSource;
        private Transform _leftGripSource;
        private InventoryHoldStyle _holdStyle;
        private InventoryHoldPose _holdPose;
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
        public InventoryHoldPose HoldPose => _holdPose;
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
            ApplyConstraintWeights();
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
            InventoryHoldStyle holdStyle,
            InventoryHoldPose holdPose)
        {
            bool changed = _equipment != equipment ||
                           _rightGripSource != rightGripSource ||
                           _leftGripSource != leftGripSource ||
                           _holdStyle != holdStyle ||
                           _holdPose != holdPose;
            _equipment = equipment;
            _rightGripSource = rightGripSource;
            _leftGripSource = leftGripSource;
            _holdStyle = equipment != null
                ? holdStyle
                : InventoryHoldStyle.None;
            _holdPose = equipment != null
                ? holdPose
                : InventoryHoldPose.None;
            if (!changed)
                return;

            _currentRightWeight = 0f;
            _currentLeftWeight = 0f;
            ApplyHoldPose();
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
            _holdPose = InventoryHoldPose.None;
            ApplyHoldPose();
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

            ApplyHoldPose();
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

        private void ApplyHoldPose()
        {
            if (animator != null)
            {
                animator.SetInteger(HoldPoseHash, (int)_holdPose);
                int oneHandLayer = animator.GetLayerIndex(OneHandPoseLayerName);
                int twoHandLayer = animator.GetLayerIndex(TwoHandPoseLayerName);
                if (oneHandLayer >= 0)
                {
                    animator.SetLayerWeight(
                        oneHandLayer,
                        _holdPose is InventoryHoldPose.OneHandedCarry or
                            InventoryHoldPose.Pistol
                            ? holdPoseLayerWeight * _currentRightWeight
                            : 0f);
                }

                if (twoHandLayer >= 0)
                {
                    animator.SetLayerWeight(
                        twoHandLayer,
                        _holdPose is InventoryHoldPose.LongGun or
                            InventoryHoldPose.TwoHandedTool
                            ? holdPoseLayerWeight * _currentRightWeight
                            : 0f);
                }
            }

            if (rightElbowHint == null || leftElbowHint == null)
                return;

            switch (_holdPose)
            {
                case InventoryHoldPose.Pistol:
                    rightElbowHint.localPosition = pistolRightHint;
                    break;
                case InventoryHoldPose.LongGun:
                    rightElbowHint.localPosition = longGunRightHint;
                    leftElbowHint.localPosition = longGunLeftHint;
                    break;
                case InventoryHoldPose.TwoHandedTool:
                    rightElbowHint.localPosition = toolRightHint;
                    leftElbowHint.localPosition = toolLeftHint;
                    break;
                default:
                    rightElbowHint.localPosition = oneHandedRightHint;
                    break;
            }
        }

        private void ApplyConstraintWeights()
        {
            ApplyConstraintWeights(
                rightArmConstraint,
                rightHandPositionWeight,
                rightHandRotationWeight);
            ApplyConstraintWeights(
                leftArmConstraint,
                leftHandPositionWeight,
                leftHandRotationWeight);
        }

        private static void ApplyConstraintWeights(
            TwoBoneIKConstraint constraint,
            float positionWeight,
            float rotationWeight)
        {
            if (constraint == null)
                return;

            TwoBoneIKConstraintData data = constraint.data;
            data.targetPositionWeight = Mathf.Clamp01(positionWeight);
            data.targetRotationWeight = Mathf.Clamp01(rotationWeight);
            constraint.data = data;
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
            rightHandPositionWeight = Mathf.Clamp01(rightHandPositionWeight);
            rightHandRotationWeight = Mathf.Clamp01(rightHandRotationWeight);
            leftHandPositionWeight = Mathf.Clamp01(leftHandPositionWeight);
            leftHandRotationWeight = Mathf.Clamp01(leftHandRotationWeight);
            holdPoseLayerWeight = Mathf.Clamp01(holdPoseLayerWeight);
            ApplyConstraintWeights();
        }
#endif
    }
}
