using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Presentation-only Humanoid IK bridge. Place this component on the same
    /// GameObject as the player's Animator and configure it from PlayerEquipment.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerEquipmentAnimatorIK : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float blendSpeed = 14f;
        [SerializeField, Range(0f, 1f)] private float rightPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float rightRotationWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float leftPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float leftRotationWeight = 1f;

        private PlayerEquipment _equipment;
        private Transform _rightGripTarget;
        private Transform _leftGripTarget;
        private InventoryHoldStyle _holdStyle;
        private float _currentRightWeight;
        private float _currentLeftWeight;

        public PlayerEquipment Equipment => _equipment;
        public Transform RightGripTarget => _rightGripTarget;
        public Transform LeftGripTarget => _leftGripTarget;
        public InventoryHoldStyle HoldStyle => _holdStyle;

        private void Awake()
        {
            ResolveAnimator();
        }

        public void Configure(
            PlayerEquipment equipment,
            Transform rightGripTarget,
            Transform leftGripTarget,
            InventoryHoldStyle holdStyle)
        {
            bool changed = _equipment != equipment ||
                           _rightGripTarget != rightGripTarget ||
                           _leftGripTarget != leftGripTarget ||
                           _holdStyle != holdStyle;
            _equipment = equipment;
            _rightGripTarget = rightGripTarget;
            _leftGripTarget = leftGripTarget;
            _holdStyle = equipment != null
                ? holdStyle
                : InventoryHoldStyle.None;
            if (changed)
            {
                _currentRightWeight = 0f;
                _currentLeftWeight = 0f;
            }
        }

        public void Clear(PlayerEquipment equipment = null)
        {
            if (equipment != null && equipment != _equipment)
                return;

            _equipment = null;
            _rightGripTarget = null;
            _leftGripTarget = null;
            _holdStyle = InventoryHoldStyle.None;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            ResolveAnimator();
            if (animator == null || !animator.isActiveAndEnabled || !animator.isHuman)
            {
                _currentRightWeight = 0f;
                _currentLeftWeight = 0f;
                return;
            }

            bool hasEquipment = _equipment != null &&
                                _holdStyle != InventoryHoldStyle.None;
            bool rightValid = hasEquipment && IsValidTarget(_rightGripTarget);
            bool leftValid = hasEquipment &&
                             _holdStyle == InventoryHoldStyle.TwoHanded &&
                             IsValidTarget(_leftGripTarget);
            float delta = blendSpeed <= 0f
                ? 1f
                : Mathf.Max(0f, Time.deltaTime) * blendSpeed;
            _currentRightWeight = rightValid
                ? Mathf.MoveTowards(_currentRightWeight, 1f, delta)
                : 0f;
            _currentLeftWeight = leftValid
                ? Mathf.MoveTowards(_currentLeftWeight, 1f, delta)
                : 0f;

            ApplyGoal(
                AvatarIKGoal.RightHand,
                _rightGripTarget,
                rightValid,
                _currentRightWeight * rightPositionWeight,
                _currentRightWeight * rightRotationWeight,
                HumanBodyBones.RightHand);
            ApplyGoal(
                AvatarIKGoal.LeftHand,
                _leftGripTarget,
                leftValid,
                _currentLeftWeight * leftPositionWeight,
                _currentLeftWeight * leftRotationWeight,
                HumanBodyBones.LeftHand);
        }

        private void ApplyGoal(
            AvatarIKGoal goal,
            Transform target,
            bool targetValid,
            float positionWeight,
            float rotationWeight,
            HumanBodyBones fallbackBone)
        {
            animator.SetIKPositionWeight(goal, Mathf.Clamp01(positionWeight));
            animator.SetIKRotationWeight(goal, Mathf.Clamp01(rotationWeight));

            if (targetValid)
            {
                animator.SetIKPosition(goal, target.position);
                animator.SetIKRotation(goal, target.rotation);
                return;
            }

            Transform hand = animator.GetBoneTransform(fallbackBone);
            if (hand != null)
            {
                animator.SetIKPosition(goal, hand.position);
                animator.SetIKRotation(goal, hand.rotation);
            }
        }

        private static bool IsValidTarget(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy;
        }

        private void ResolveAnimator()
        {
            animator ??= GetComponent<Animator>();
        }

        private void OnDisable()
        {
            Clear();
            _currentRightWeight = 0f;
            _currentLeftWeight = 0f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveAnimator();
            blendSpeed = Mathf.Max(0f, blendSpeed);
        }
#endif
    }
}
