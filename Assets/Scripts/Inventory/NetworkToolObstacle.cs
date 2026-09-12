using Fusion;
using UnityEngine;

namespace TheSancturary.Inventory
{
    public enum NetworkToolObstacleEffect : byte
    {
        RotateOpen,
        DisableVisual
    }

    /// <summary>
    /// State-authoritative validation obstacle for the concrete crowbar and fire
    /// axe item checks. The caller owns range and line-of-sight validation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkToolObstacle : NetworkBehaviour
    {
        [Header("Requirement")]
        [SerializeField] private string requiredItemId =
            ItemGameplayRules.CrowbarItemId;
        [SerializeField, Range(1, byte.MaxValue)] private int requiredHits = 1;

        [Header("Completion Presentation")]
        [SerializeField] private NetworkToolObstacleEffect completionEffect =
            NetworkToolObstacleEffect.RotateOpen;
        [SerializeField] private Transform effectTransform;
        [SerializeField] private Vector3 completedLocalEulerOffset =
            new Vector3(0f, 90f, 0f);
        [SerializeField] private GameObject blockingVisual;
        [SerializeField] private Collider blockingCollider;

        [Header("Accepted Hit Audio")]
        [SerializeField] private AudioSource impactAudioSource;
        [SerializeField] private AudioClip impactAudioClip;

        [Networked] public byte AcceptedHits { get; private set; }
        [Networked] public NetworkBool IsCompleted { get; private set; }
        [Networked] public ushort ImpactSequence { get; private set; }

        private Quaternion _closedLocalRotation;
        private ushort _lastPresentedImpactSequence;
        private bool _presentationInitialized;

        public string RequiredItemId => requiredItemId;

        public void ResetSandboxTarget()
        {
            if (!HasStateAuthority || !TheSancturary.FusionPrototype.SandboxSession.IsActiveFor(this)) return;
            AcceptedHits = 0;
            IsCompleted = false;
            ApplyReplicatedPresentation();
        }
        public int RequiredHits => Mathf.Clamp(requiredHits, 1, byte.MaxValue);

        public override void Spawned()
        {
            ResolveReferences();
            InitializePresentation();

            if (HasStateAuthority)
            {
                ItemGameplayRules.ToolObstacleState state =
                    ItemGameplayRules.NormalizeToolObstacleState(
                    AcceptedHits,
                    RequiredHits,
                    IsCompleted);
                AcceptedHits = state.AcceptedHits;
                IsCompleted = state.IsCompleted;
            }

            _lastPresentedImpactSequence = ImpactSequence;
            ApplyReplicatedPresentation();
        }

        public override void Render()
        {
            ResolveReferences();
            InitializePresentation();

            ushort impactSequence = ImpactSequence;
            if (impactSequence != _lastPresentedImpactSequence)
            {
                _lastPresentedImpactSequence = impactSequence;
                PlayImpactAudio();
            }

            ApplyReplicatedPresentation();
        }

        /// <summary>
        /// Accepts one validated tool impact. Call only after the authoritative
        /// item-use flow has validated its actor, equipped instance, range, and
        /// line of sight.
        /// </summary>
        public bool TryApplyHit(string equippedItemId)
        {
            if (!HasStateAuthority ||
                !ItemGameplayRules.TryApplyToolHit(
                    equippedItemId,
                    requiredItemId,
                    AcceptedHits,
                    RequiredHits,
                    IsCompleted,
                    out ItemGameplayRules.ToolObstacleState state))
                return false;

            AcceptedHits = state.AcceptedHits;
            IsCompleted = state.IsCompleted;
            ImpactSequence = unchecked((ushort)(ImpactSequence + 1));
            return true;
        }

        public void Configure(
            string configuredRequiredItemId,
            int configuredRequiredHits,
            NetworkToolObstacleEffect configuredEffect,
            Transform configuredEffectTransform,
            Vector3 configuredCompletedLocalEulerOffset,
            GameObject configuredBlockingVisual,
            Collider configuredBlockingCollider,
            AudioSource configuredImpactAudioSource,
            AudioClip configuredImpactAudioClip)
        {
            requiredItemId = ItemGameplayRules.NormalizeItemId(
                configuredRequiredItemId);
            requiredHits = Mathf.Clamp(
                configuredRequiredHits,
                1,
                byte.MaxValue);
            completionEffect = configuredEffect;
            effectTransform = configuredEffectTransform;
            completedLocalEulerOffset = configuredCompletedLocalEulerOffset;
            blockingVisual = configuredBlockingVisual;
            blockingCollider = configuredBlockingCollider;
            impactAudioSource = configuredImpactAudioSource;
            impactAudioClip = configuredImpactAudioClip;
            _presentationInitialized = false;
            ResolveReferences();
        }

        private void ApplyReplicatedPresentation()
        {
            bool completed = IsCompleted;
            if (completionEffect == NetworkToolObstacleEffect.RotateOpen)
            {
                if (effectTransform != null)
                {
                    effectTransform.localRotation = completed
                        ? _closedLocalRotation *
                          Quaternion.Euler(completedLocalEulerOffset)
                        : _closedLocalRotation;
                }

            }
            else if (blockingVisual != null && blockingVisual != gameObject)
            {
                blockingVisual.SetActive(!completed);
            }

            if (blockingCollider != null)
                blockingCollider.enabled = !completed;
        }

        private void PlayImpactAudio()
        {
            if (impactAudioSource != null && impactAudioClip != null)
                impactAudioSource.PlayOneShot(impactAudioClip);
        }

        private void InitializePresentation()
        {
            if (_presentationInitialized)
                return;

            if (effectTransform != null)
                _closedLocalRotation = effectTransform.localRotation;
            _presentationInitialized = true;
        }

        private void ResolveReferences()
        {
            requiredItemId = ItemGameplayRules.NormalizeItemId(requiredItemId);
            requiredHits = Mathf.Clamp(requiredHits, 1, byte.MaxValue);
            if (effectTransform == null)
            {
                effectTransform = blockingVisual != null
                    ? blockingVisual.transform
                    : transform;
            }

            if (blockingCollider == null)
                blockingCollider = GetComponentInChildren<Collider>(true);
            if (impactAudioSource == null)
                impactAudioSource = GetComponent<AudioSource>();
            if (impactAudioSource != null)
            {
                impactAudioSource.playOnAwake = false;
                impactAudioSource.spatialBlend = 1f;
            }
        }

        private void OnValidate()
        {
            ResolveReferences();
            _presentationInitialized = false;
        }
    }
}
