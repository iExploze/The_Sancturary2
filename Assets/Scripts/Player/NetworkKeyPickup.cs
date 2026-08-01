using Fusion;
using TheSancturary.Inventory;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// Authoritative one-owner key pickup. Collection is a replicated logical state,
    /// so contention and late joiners are handled without synchronizing transforms.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkKeyPickup : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        [Header("Key")]
        [SerializeField] private string itemId = "maintenance_key";
        [SerializeField] private string itemDisplayName = "Maintenance Key";

        [Header("Interaction")]
        [SerializeField] private InteractionTarget interactionTarget;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Collider[] pickupColliders;
        [SerializeField] private WorldItemPhysics worldItemPhysics;
        [SerializeField] private float visualRotationSpeed = 35f;

        [Networked] public NetworkBool IsCollected { get; private set; }

        private bool _lastRenderedCollected;
        private bool _spawned;

        public InteractionTarget PromptTarget => interactionTarget;
        public string ItemId => itemId;
        public string ItemDisplayName => itemDisplayName;
        public bool IsAvailable => _spawned && !IsCollected;

        public override void Spawned()
        {
            ResolveReferences();
            _spawned = true;
            _lastRenderedCollected = IsCollected;
            ApplyCollectedState(_lastRenderedCollected);
        }

        private void Update()
        {
            if (!_spawned)
                return;

            bool collected = IsCollected;
            if (collected != _lastRenderedCollected)
            {
                _lastRenderedCollected = collected;
                ApplyCollectedState(collected);
            }

            if (!collected && visualRoot != null && visualRotationSpeed != 0f)
                visualRoot.transform.Rotate(0f, visualRotationSpeed * Time.deltaTime, 0f, Space.Self);
        }

        public bool TryGetActionText(
            NetworkPlayerInventory viewerInventory,
            string interactionVerb,
            out string actionText)
        {
            if (!_spawned || IsCollected)
            {
                actionText = null;
                return false;
            }

            string verb = string.IsNullOrWhiteSpace(interactionVerb) ? "Pick Up" : interactionVerb;
            actionText = $"F \u2014 {verb}";
            return true;
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (!_spawned || IsCollected || requestingPlayer == null || !requestingPlayer.HasInputAuthority)
                return false;

            requestingPlayer.RequestInteraction(this);
            return true;
        }

        public bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer)
        {
            if (!HasStateAuthority || requestingPlayer == null ||
                requestingPlayer.Inventory == null)
                return false;

            if (IsCollected)
            {
                requestingPlayer.Inventory.SendOwnerRejection(
                    InventoryRequestRejection.ItemTaken);
                return false;
            }

            if (!requestingPlayer.Inventory.TryAddItemAuthoritative(
                    itemId,
                    out InventoryRequestRejection rejection))
            {
                requestingPlayer.Inventory.SendOwnerRejection(rejection);
                return false;
            }

            if (Object.NetworkTypeId.IsSceneObject)
            {
                IsCollected = true;
                ApplyCollectedState(true);
            }
            else
                Runner.Despawn(Object);

            return true;
        }

        public void Configure(
            string configuredItemId,
            string configuredItemDisplayName,
            InteractionTarget configuredInteractionTarget,
            GameObject configuredVisualRoot,
            Collider[] configuredColliders,
            float configuredVisualRotationSpeed = 35f)
        {
            itemId = NetworkLockGroup.NormalizeId(configuredItemId);
            itemDisplayName = configuredItemDisplayName;
            interactionTarget = configuredInteractionTarget;
            visualRoot = configuredVisualRoot;
            pickupColliders = configuredColliders;
            visualRotationSpeed = configuredVisualRotationSpeed;
        }

        private void ApplyCollectedState(bool collected)
        {
            worldItemPhysics?.ApplyAvailableState(!collected);

            if (visualRoot != null)
                visualRoot.SetActive(!collected);

            if (pickupColliders == null)
                return;
            for (int i = 0; i < pickupColliders.Length; i++)
            {
                if (pickupColliders[i] != null)
                    pickupColliders[i].enabled = !collected;
            }
        }

        private void OnValidate()
        {
            itemId = NetworkLockGroup.NormalizeId(itemId);
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            interactionTarget ??= GetComponent<InteractionTarget>();
            worldItemPhysics ??= GetComponent<WorldItemPhysics>();
            if (pickupColliders == null || pickupColliders.Length == 0)
                pickupColliders = GetComponentsInChildren<Collider>(true);
        }
    }
}
