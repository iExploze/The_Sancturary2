using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// Authoritative level exit. A valid interaction loads the shared escape scene once,
    /// while the persistent Fusion runner keeps every connected player together.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkExitDoor : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        [SerializeField] private InteractionTarget interactionTarget;
        [SerializeField] private NetworkLockGroup lockGroup;

        [Networked] public NetworkBool EscapeTriggered { get; private set; }

        private bool _spawned;

        public InteractionTarget PromptTarget => interactionTarget;

        public override void Spawned()
        {
            ResolveReferences();
            _spawned = true;
        }

        public bool TryGetActionText(
            NetworkPlayerInventory viewerInventory,
            string interactionVerb,
            out string actionText)
        {
            if (!_spawned || EscapeTriggered)
            {
                actionText = null;
                return false;
            }

            if (lockGroup != null && lockGroup.IsLocked &&
                (viewerInventory == null || !viewerInventory.HasItem(lockGroup.RequiredKeyId)))
            {
                actionText = string.IsNullOrWhiteSpace(lockGroup.RequiredKeyDisplayName)
                    ? "Locked"
                    : $"Locked — Requires {lockGroup.RequiredKeyDisplayName}";
                return true;
            }

            string verb = string.IsNullOrWhiteSpace(interactionVerb) ? "Escape" : interactionVerb;
            actionText = $"F — {verb}";
            return true;
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (!_spawned || EscapeTriggered || requestingPlayer == null ||
                !requestingPlayer.HasInputAuthority)
                return false;

            requestingPlayer.RequestInteraction(this);
            return true;
        }

        public bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer)
        {
            if (!HasStateAuthority || EscapeTriggered || requestingPlayer == null)
                return false;

            if (lockGroup != null &&
                !lockGroup.TryUnlockAuthoritative(requestingPlayer.Inventory))
                return false;

            FusionSessionManager manager = FusionSessionManager.Instance;
            if (manager == null || !manager.TryLoadEscapeSceneAuthoritative(Runner))
                return false;

            EscapeTriggered = true;
            return true;
        }

        public void Configure(InteractionTarget target, NetworkLockGroup configuredLockGroup)
        {
            interactionTarget = target;
            lockGroup = configuredLockGroup;
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            interactionTarget ??= GetComponent<InteractionTarget>();
            lockGroup ??= GetComponent<NetworkLockGroup>();
        }
    }
}
