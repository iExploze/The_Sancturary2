using System;
using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    [Serializable]
    public sealed class TransformOpenMotion
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 openLocalPositionOffset;
        [SerializeField] private Vector3 openLocalEulerOffset;

        [NonSerialized] private Vector3 _closedPosition;
        [NonSerialized] private Quaternion _closedRotation;
        [NonSerialized] private Vector3 _openPosition;
        [NonSerialized] private Quaternion _openRotation;

        public TransformOpenMotion(Transform target, Vector3 positionOffset, Vector3 eulerOffset)
        {
            this.target = target;
            openLocalPositionOffset = positionOffset;
            openLocalEulerOffset = eulerOffset;
        }

        public bool IsValid => target != null;

        public void CaptureClosedState()
        {
            if (target == null)
                return;

            _closedPosition = target.localPosition;
            _closedRotation = target.localRotation;
            _openPosition = _closedPosition + openLocalPositionOffset;
            _openRotation = _closedRotation * Quaternion.Euler(openLocalEulerOffset);
        }

        public void Apply(float progress)
        {
            if (target == null)
                return;

            target.localPosition = Vector3.LerpUnclamped(_closedPosition, _openPosition, progress);
            target.localRotation = Quaternion.SlerpUnclamped(_closedRotation, _openRotation, progress);
        }

        public void ApplyClosedState()
        {
            if (target == null)
                return;

            target.localPosition = _closedPosition;
            target.localRotation = _closedRotation;
        }

        public void ApplyOpenState()
        {
            if (target == null)
                return;

            target.localPosition = _openPosition;
            target.localRotation = _openRotation;
        }
    }

    /// <summary>
    /// Replicates only the logical open state. Every peer independently animates the
    /// configured local transforms, so doors and drawers do not stream transforms.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TransformOpenable : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        [SerializeField] private InteractionTarget interactionTarget;
        [SerializeField] private NetworkLockGroup lockGroup;
        [SerializeField, Min(0.05f)] private float duration = 0.65f;
        [SerializeField] private TransformOpenMotion[] motions;

        [Networked] public NetworkBool IsOpen { get; private set; }

        private float _elapsed;
        private bool _openingVisual;
        private bool _visualTargetOpen;
        private bool _captured;
        private bool _spawned;

        public InteractionTarget PromptTarget => interactionTarget;

        private void Awake()
        {
            CaptureClosedState();
        }

        public override void Spawned()
        {
            CaptureClosedState();
            _spawned = true;
            _visualTargetOpen = IsOpen;
            ApplyExactState(_visualTargetOpen);
        }

        private void Update()
        {
            if (!_spawned)
                return;

            bool shouldBeOpen = IsOpen;
            if (shouldBeOpen != _visualTargetOpen)
            {
                _visualTargetOpen = shouldBeOpen;
                _elapsed = 0f;
                _openingVisual = true;
            }

            if (!_openingVisual)
                return;
            if (motions == null || motions.Length == 0)
            {
                _openingVisual = false;
                return;
            }

            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / duration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            if (!_visualTargetOpen)
                easedProgress = 1f - easedProgress;

            for (int i = 0; i < motions.Length; i++)
                motions[i]?.Apply(easedProgress);

            if (progress < 1f)
                return;

            ApplyExactState(_visualTargetOpen);
            _openingVisual = false;
        }

        public bool TryGetActionText(
            NetworkPlayerInventory viewerInventory,
            string interactionVerb,
            out string actionText)
        {
            if (!_spawned || IsOpen || !HasValidMotion())
            {
                actionText = null;
                return false;
            }

            if (lockGroup != null && lockGroup.IsLocked &&
                (viewerInventory == null || !viewerInventory.HasItem(lockGroup.RequiredKeyId)))
            {
                actionText = string.IsNullOrWhiteSpace(lockGroup.RequiredKeyDisplayName)
                    ? "Locked"
                    : $"Locked \u2014 Requires {lockGroup.RequiredKeyDisplayName}";
                return true;
            }

            actionText = string.IsNullOrWhiteSpace(interactionVerb)
                ? null
                : $"F \u2014 {interactionVerb}";
            return !string.IsNullOrEmpty(actionText);
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (!_spawned || IsOpen || requestingPlayer == null || !requestingPlayer.HasInputAuthority)
                return false;

            requestingPlayer.RequestInteraction(this);
            return true;
        }

        public bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer)
        {
            if (!HasStateAuthority || IsOpen || requestingPlayer == null || !HasValidMotion())
                return false;

            if (lockGroup != null && !lockGroup.TryUnlockAuthoritative(requestingPlayer.Inventory))
                return false;

            IsOpen = true;
            return true;
        }

        public void Configure(
            InteractionTarget target,
            float openingDuration,
            params TransformOpenMotion[] configuredMotions)
        {
            interactionTarget = target;
            duration = Mathf.Max(0.05f, openingDuration);
            motions = configuredMotions;
            _captured = false;
        }

        public void SetLockGroup(NetworkLockGroup configuredLockGroup)
        {
            lockGroup = configuredLockGroup;
        }

        private void CaptureClosedState()
        {
            if (motions == null)
                return;

            for (int i = 0; i < motions.Length; i++)
                motions[i]?.CaptureClosedState();
            _captured = true;
        }

        private void ApplyExactState(bool open)
        {
            if (!_captured)
                CaptureClosedState();
            if (motions == null)
                return;

            for (int i = 0; i < motions.Length; i++)
            {
                if (open)
                    motions[i]?.ApplyOpenState();
                else
                    motions[i]?.ApplyClosedState();
            }
        }

        private bool HasValidMotion()
        {
            if (motions == null || motions.Length == 0)
                return false;

            for (int i = 0; i < motions.Length; i++)
            {
                if (motions[i] != null && motions[i].IsValid)
                    return true;
            }

            return false;
        }
    }
}
