using Fusion;
using TheSancturary.FusionPrototype;
using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Authoritative shared world item. Scene items retain a replicated collected
    /// flag for late joiners; dynamically dropped items despawn through Fusion.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(InteractionTarget))]
    public sealed class WorldInventoryItem : NetworkBehaviour,
        IInteractable,
        IAuthoritativeInteractable
    {
        [Header("Item")]
        [SerializeField] private InventoryItemDefinition definition;

        [Header("Presentation")]
        [SerializeField] private InteractionTarget interactionTarget;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Transform labelTransform;
        [SerializeField] private Collider[] pickupColliders;
        [SerializeField] private WorldItemPhysics worldItemPhysics;

        [Networked] public NetworkBool IsCollected { get; private set; }

        private bool _spawned;
        private bool _lastRenderedCollected;

        public InventoryItemDefinition Definition => definition;
        public string ItemId => definition != null ? definition.ItemId : string.Empty;
        public bool IsAvailable => _spawned && !IsCollected;
        public InteractionTarget PromptTarget => interactionTarget;

        public override void Spawned()
        {
            ResolveReferences();
            ConfigureInteractionTarget();
            _spawned = true;
            _lastRenderedCollected = IsCollected;
            ApplyCollectedState(_lastRenderedCollected);
            SetTargeted(false);
        }

        public override void Render()
        {
            bool collected = IsCollected;
            if (collected == _lastRenderedCollected)
                return;

            _lastRenderedCollected = collected;
            ApplyCollectedState(collected);
        }

        public void SetDefinition(InventoryItemDefinition value)
        {
            definition = value;
            ConfigureInteractionTarget();
        }

        public bool TryGetActionText(
            NetworkPlayerInventory viewerInventory,
            string interactionVerb,
            out string actionText)
        {
            if (!IsAvailable || definition == null)
            {
                actionText = null;
                return false;
            }

            actionText = "F \u2014 Pick up";
            return true;
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (!IsAvailable ||
                requestingPlayer == null ||
                !requestingPlayer.HasInputAuthority)
                return false;

            requestingPlayer.RequestInteraction(this);
            return true;
        }

        public bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer)
        {
            if (!HasStateAuthority || requestingPlayer == null)
                return false;

            NetworkPlayerInventory inventory = requestingPlayer.Inventory;
            if (!IsAvailable)
            {
                inventory?.SendOwnerRejection(
                    InventoryRequestRejection.ItemTaken);
                return false;
            }

            if (inventory == null)
                return false;

            if (!inventory.TryCollectAuthoritative(
                    this,
                    out InventoryRequestRejection rejection))
            {
                inventory.SendOwnerRejection(rejection);
                return false;
            }

            if (Object.NetworkTypeId.IsSceneObject)
            {
                IsCollected = true;
                ApplyCollectedState(true);
            }
            else
            {
                Runner.Despawn(Object);
            }

            return true;
        }

        public void SetTargeted(bool targeted)
        {
            if (labelTransform != null)
                labelTransform.gameObject.SetActive(targeted && IsAvailable);
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (labelTransform != null &&
                labelTransform.gameObject.activeInHierarchy &&
                camera != null)
            {
                labelTransform.rotation = Quaternion.LookRotation(
                    labelTransform.position - camera.transform.position,
                    Vector3.up);
            }
        }

        private void ApplyCollectedState(bool collected)
        {
            worldItemPhysics?.ApplyAvailableState(!collected);

            if (visualRoot != null)
                visualRoot.SetActive(!collected);
            if (interactionTarget != null)
            {
                interactionTarget.SetStatus(
                    collected
                        ? InteractionTargetStatus.Unavailable
                        : InteractionTargetStatus.Available);
            }

            if (pickupColliders != null)
            {
                for (int index = 0; index < pickupColliders.Length; index++)
                {
                    if (pickupColliders[index] != null)
                        pickupColliders[index].enabled = !collected;
                }
            }

            if (collected)
                SetTargeted(false);
        }

        private void ResolveReferences()
        {
            interactionTarget ??= GetComponent<InteractionTarget>();
            worldItemPhysics ??= GetComponent<WorldItemPhysics>();
            if (pickupColliders == null || pickupColliders.Length == 0)
                pickupColliders = GetComponentsInChildren<Collider>(true);
        }

        private void ConfigureInteractionTarget()
        {
            interactionTarget ??= GetComponent<InteractionTarget>();
            if (interactionTarget == null)
                return;

            interactionTarget.Configure(
                definition != null ? definition.DisplayName : "Inventory Item",
                "Pick up",
                InteractionTargetStatus.Available,
                null,
                this);
        }

        private void OnValidate()
        {
            ResolveReferences();
            ConfigureInteractionTarget();
        }
    }
}
