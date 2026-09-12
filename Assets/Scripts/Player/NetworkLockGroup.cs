using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// One replicated lock shared by one or more openables, such as every drawer in a cabinet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkLockGroup : NetworkBehaviour
    {
        [Header("Key Requirement")]
        [SerializeField] private bool needsKey;
        [SerializeField] private string requiredKeyId;
        [SerializeField] private string requiredKeyDisplayName;
        [SerializeField] private bool consumeKeyOnUnlock;

        [Networked] public NetworkBool IsUnlocked { get; private set; }

        public bool NeedsKey => needsKey;
        public bool IsLocked => needsKey && !IsUnlocked;
        public string RequiredKeyId => requiredKeyId;
        public string RequiredKeyDisplayName => requiredKeyDisplayName;
        public bool ConsumeKeyOnUnlock => consumeKeyOnUnlock;

        public void ResetSandboxLock()
        {
            if (HasStateAuthority && SandboxSession.IsActiveFor(this)) IsUnlocked = !needsKey;
        }

        public override void Spawned()
        {
            if (HasStateAuthority && !needsKey)
                IsUnlocked = true;
        }

        public bool TryUnlockAuthoritative(NetworkPlayerInventory requestingInventory)
        {
            if (!HasStateAuthority)
                return false;
            if (!needsKey || IsUnlocked)
                return true;
            if (requestingInventory == null || !requestingInventory.HasItem(requiredKeyId))
                return false;
            if (consumeKeyOnUnlock && !requestingInventory.TryRemoveItemAuthoritative(requiredKeyId))
                return false;

            IsUnlocked = true;
            return true;
        }

        public void Configure(
            bool configuredNeedsKey,
            string configuredRequiredKeyId,
            string configuredRequiredKeyDisplayName,
            bool configuredConsumeKeyOnUnlock = false)
        {
            needsKey = configuredNeedsKey;
            requiredKeyId = configuredRequiredKeyId;
            requiredKeyDisplayName = configuredRequiredKeyDisplayName;
            consumeKeyOnUnlock = configuredConsumeKeyOnUnlock;
        }

        private void OnValidate()
        {
            requiredKeyId = NormalizeId(requiredKeyId);
        }

        public static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
