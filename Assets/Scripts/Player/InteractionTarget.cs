using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    public enum InteractionTargetStatus : byte
    {
        Available,
        Locked,
        Unavailable
    }

    public readonly struct InteractionPrompt
    {
        public InteractionPrompt(string displayName, string actionText)
        {
            DisplayName = displayName;
            ActionText = actionText;
        }

        public string DisplayName { get; }
        public string ActionText { get; }
    }

    public interface IInteractionTarget
    {
        string DisplayName { get; }
        string InteractionVerb { get; }
        InteractionTargetStatus Status { get; }
        string RequiredKeyDisplayName { get; }
        bool TryGetPrompt(NetworkPlayerInventory viewerInventory, out InteractionPrompt prompt);
        bool RequestInteraction(FusionNetworkPlayer requestingPlayer);
    }

    public interface IInteractable
    {
        bool TryGetActionText(NetworkPlayerInventory viewerInventory, string interactionVerb, out string actionText);
        bool RequestInteraction(FusionNetworkPlayer requestingPlayer);
    }

    /// <summary>
    /// Implemented by Fusion behaviours that can receive an authoritative player interaction.
    /// The requesting player is resolved from the RPC source, not supplied by the client.
    /// </summary>
    public interface IAuthoritativeInteractable
    {
        InteractionTarget PromptTarget { get; }
        bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer);
    }

    /// <summary>
    /// Local prompt metadata for an interactable object. Place this on the logical object;
    /// child colliders resolve this parent component automatically.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [Header("Interaction Prompt")]
        [SerializeField] private string displayName;
        [SerializeField] private string interactionVerb = "Interact";
        [SerializeField] private InteractionTargetStatus status = InteractionTargetStatus.Available;
        [SerializeField] private string requiredKeyDisplayName;
        [SerializeField] private MonoBehaviour interactionBehaviour;

        private string _lockedActionText;
        private IInteractable _interactable;

        public string DisplayName => displayName;
        public string InteractionVerb => interactionVerb;
        public InteractionTargetStatus Status => status;
        public string RequiredKeyDisplayName => requiredKeyDisplayName;

        private void Awake()
        {
            ResolveInteractable();
            RebuildPromptText();
        }

        private void OnValidate()
        {
            RebuildPromptText();
        }

        public bool TryGetPrompt(NetworkPlayerInventory viewerInventory, out InteractionPrompt prompt)
        {
            if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(displayName) || status == InteractionTargetStatus.Unavailable)
            {
                prompt = default;
                return false;
            }

            if (_interactable == null)
                ResolveInteractable();

            if (status == InteractionTargetStatus.Locked)
            {
                if (string.IsNullOrEmpty(_lockedActionText))
                {
                    prompt = default;
                    return false;
                }

                prompt = new InteractionPrompt(displayName, _lockedActionText);
                return true;
            }

            if (_interactable == null ||
                !_interactable.TryGetActionText(viewerInventory, interactionVerb, out string actionText) ||
                string.IsNullOrEmpty(actionText))
            {
                prompt = default;
                return false;
            }

            prompt = new InteractionPrompt(displayName, actionText);
            return true;
        }

        public void SetStatus(InteractionTargetStatus value, string keyDisplayName = null)
        {
            status = value;
            if (keyDisplayName != null)
                requiredKeyDisplayName = keyDisplayName;
            RebuildPromptText();
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (status == InteractionTargetStatus.Unavailable)
                return false;

            if (_interactable == null)
                ResolveInteractable();
            return _interactable != null && _interactable.RequestInteraction(requestingPlayer);
        }

        public void Configure(
            string configuredDisplayName,
            string configuredVerb,
            InteractionTargetStatus configuredStatus,
            string configuredRequiredKeyDisplayName,
            MonoBehaviour configuredInteractionBehaviour)
        {
            displayName = configuredDisplayName;
            interactionVerb = configuredVerb;
            status = configuredStatus;
            requiredKeyDisplayName = configuredRequiredKeyDisplayName;
            interactionBehaviour = configuredInteractionBehaviour;
            ResolveInteractable();
            RebuildPromptText();
        }

        private void ResolveInteractable()
        {
            _interactable = interactionBehaviour as IInteractable;
        }

        private void RebuildPromptText()
        {
            _lockedActionText = string.IsNullOrWhiteSpace(requiredKeyDisplayName)
                ? "Locked"
                : $"Locked \u2014 Requires {requiredKeyDisplayName}";
        }
    }
}
