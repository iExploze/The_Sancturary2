using TheSancturary.FusionPrototype;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Reconstructs limited third-person vertical look from the replicated view
    /// pitch. This component owns presentation only; camera and gameplay aim
    /// continue to use the player's full replicated look angles.
    /// </summary>
    [DefaultExecutionOrder(-75)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(RigBuilder))]
    public sealed class PlayerThirdPersonLookPresentation : MonoBehaviour
    {
        public const float DefaultPitchLimit = 45f;

        [Header("Player and Humanoid")]
        [SerializeField] private FusionNetworkPlayer player;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform torsoBone;
        [SerializeField] private Transform headBone;

        [Header("Held Item Presentation")]
        [SerializeField] private Transform itemBodyAnchor;
        [SerializeField] private Transform itemPitchPivot;

        [Header("Head Look Rig")]
        [SerializeField] private Rig headLookRig;
        [SerializeField] private MultiAimConstraint headLookConstraint;
        [SerializeField] private Transform headLookTarget;

        [Header("Upper Chest Aim Rig")]
        [SerializeField] private Rig chestAimRig;
        [SerializeField] private MultiAimConstraint chestAimConstraint;
        [SerializeField] private Transform chestAimTarget;

        [Header("Look Tuning")]
        [SerializeField, Range(1f, 89f)] private float pitchLimit =
            DefaultPitchLimit;
        [SerializeField, Min(0.01f)] private float pitchSmoothTime = 0.1f;
        [SerializeField, Min(0.25f)] private float headTargetDistance = 3f;
        [SerializeField, Range(0f, 1f)] private float headRigWeight = 0.85f;
        [SerializeField, Range(0f, 0.5f)] private float chestPitchFraction = 0.25f;
        [SerializeField, Range(0f, 1f)] private float chestRigWeight = 0.65f;
        [SerializeField, Min(0.25f)] private float chestTargetDistance = 3f;

        private Quaternion _itemPitchRestRotation = Quaternion.identity;
        private float _smoothedPitch;
        private float _pitchVelocity;
        private bool _presentationWasValid;

        public Transform TorsoBone => torsoBone;
        public Transform HeadBone => headBone;
        public Transform ItemBodyAnchor => itemBodyAnchor;
        public Transform ItemPitchPivot => itemPitchPivot;
        public Transform HeadLookTarget => headLookTarget;
        public Rig HeadLookRig => headLookRig;
        public MultiAimConstraint HeadLookConstraint => headLookConstraint;
        public Transform ChestAimTarget => chestAimTarget;
        public Rig ChestAimRig => chestAimRig;
        public MultiAimConstraint ChestAimConstraint => chestAimConstraint;
        public float PitchLimit => pitchLimit;
        public float SmoothedPitch => _smoothedPitch;

        public static float ClampPresentationPitch(
            float rawLookPitch,
            float limit = DefaultPitchLimit)
        {
            return Mathf.Clamp(rawLookPitch, -Mathf.Abs(limit), Mathf.Abs(limit));
        }

        private void Awake()
        {
            ResolveReferences();
            if (itemPitchPivot != null)
                _itemPitchRestRotation = itemPitchPivot.localRotation;
            DisablePresentation();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (itemPitchPivot != null)
                _itemPitchRestRotation = itemPitchPivot.localRotation;
            _presentationWasValid = false;
        }

        private void Update()
        {
            bool presentationValid =
                player != null &&
                player.Object != null &&
                player.Object.IsValid &&
                animator != null &&
                animator.isHuman &&
                headBone != null &&
                !player.IsDead &&
                !player.IsHiddenInLocker;
            if (!presentationValid)
            {
                DisablePresentation();
                return;
            }

            float targetPitch = ClampPresentationPitch(
                player.LookPitch,
                pitchLimit);
            if (!_presentationWasValid)
            {
                _smoothedPitch = targetPitch;
                _pitchVelocity = 0f;
                _presentationWasValid = true;
            }
            else
            {
                _smoothedPitch = Mathf.SmoothDampAngle(
                    _smoothedPitch,
                    targetPitch,
                    ref _pitchVelocity,
                    pitchSmoothTime,
                    Mathf.Infinity,
                    Mathf.Max(0f, Time.deltaTime));
            }

            float chestPitch = _smoothedPitch * chestPitchFraction;
            ApplyItemPitch(_smoothedPitch - chestPitch);
            ApplyChestLook(chestPitch);
            ApplyHeadLook(_smoothedPitch);
        }

        private void ApplyItemPitch(float pitch)
        {
            if (itemPitchPivot == null)
                return;

            itemPitchPivot.localRotation =
                _itemPitchRestRotation * Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyHeadLook(float pitch)
        {
            if (headLookRig != null)
                headLookRig.weight = headRigWeight;
            if (headLookConstraint != null)
                headLookConstraint.weight = 1f;
            if (headLookTarget == null || headBone == null || player == null)
                return;

            Vector3 direction =
                Quaternion.AngleAxis(pitch, player.transform.right) *
                player.transform.forward;
            headLookTarget.position =
                headBone.position + direction * headTargetDistance;
            headLookTarget.rotation = Quaternion.LookRotation(
                direction,
                player.transform.up);
        }

        private void ApplyChestLook(float pitch)
        {
            if (chestAimRig != null)
                chestAimRig.weight = chestRigWeight;
            if (chestAimConstraint != null)
                chestAimConstraint.weight = 1f;
            if (chestAimTarget == null || torsoBone == null || player == null)
                return;

            Vector3 direction =
                Quaternion.AngleAxis(pitch, player.transform.right) *
                player.transform.forward;
            chestAimTarget.position =
                torsoBone.position + direction * chestTargetDistance;
            chestAimTarget.rotation = Quaternion.LookRotation(
                direction,
                player.transform.up);
        }

        private void DisablePresentation()
        {
            _presentationWasValid = false;
            _smoothedPitch = 0f;
            _pitchVelocity = 0f;
            ApplyItemPitch(0f);
            if (chestAimRig != null)
                chestAimRig.weight = 0f;
            if (chestAimConstraint != null)
                chestAimConstraint.weight = 1f;
            if (headLookRig != null)
                headLookRig.weight = 0f;
            if (headLookConstraint != null)
                headLookConstraint.weight = 1f;

            if (headLookTarget == null || headBone == null)
            {
                ResetChestTarget();
                return;
            }

            Transform facing = player != null ? player.transform : transform;
            headLookTarget.position =
                headBone.position + facing.forward * headTargetDistance;
            headLookTarget.rotation = Quaternion.LookRotation(
                facing.forward,
                facing.up);
            ResetChestTarget();
        }

        private void ResetChestTarget()
        {
            if (chestAimTarget == null || torsoBone == null)
                return;

            Transform facing = player != null ? player.transform : transform;
            chestAimTarget.position =
                torsoBone.position + facing.forward * chestTargetDistance;
            chestAimTarget.rotation = Quaternion.LookRotation(
                facing.forward,
                facing.up);
        }

        private void ResolveReferences()
        {
            animator ??= GetComponent<Animator>();
            player ??= GetComponentInParent<FusionNetworkPlayer>();
            if (animator == null || !animator.isHuman)
                return;

            torsoBone ??= animator.GetBoneTransform(HumanBodyBones.UpperChest);
            torsoBone ??= animator.GetBoneTransform(HumanBodyBones.Chest);
            headBone ??= animator.GetBoneTransform(HumanBodyBones.Head);
        }

        private void OnDisable()
        {
            DisablePresentation();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            pitchLimit = Mathf.Clamp(Mathf.Abs(pitchLimit), 1f, 89f);
            pitchSmoothTime = Mathf.Max(0.01f, pitchSmoothTime);
            headTargetDistance = Mathf.Max(0.25f, headTargetDistance);
            headRigWeight = Mathf.Clamp01(headRigWeight);
            chestPitchFraction = Mathf.Clamp(chestPitchFraction, 0f, 0.5f);
            chestRigWeight = Mathf.Clamp01(chestRigWeight);
            chestTargetDistance = Mathf.Max(0.25f, chestTargetDistance);
        }
#endif
    }
}
