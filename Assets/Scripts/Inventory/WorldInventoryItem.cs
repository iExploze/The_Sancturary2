using TheSancturary.FusionPrototype;
using UnityEngine;

namespace TheSancturary.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractionTarget))]
    public sealed class WorldInventoryItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private InventoryItemDefinition definition;
        [SerializeField] private Transform labelTransform;

        public InventoryItemDefinition Definition => definition;

        private void Awake()
        {
            ConfigureInteractionTarget();
            SetTargeted(false);
        }

        private void OnEnable()
        {
            SetTargeted(false);
        }

        private void OnValidate()
        {
            ConfigureInteractionTarget();
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
            actionText = definition != null ? "F \u2014 Pick up" : null;
            return definition != null;
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (definition == null || requestingPlayer == null)
                return false;

            InteractionTarget promptTarget = GetComponent<InteractionTarget>();
            LocalInteractionTargeting targeting = requestingPlayer.GetComponent<LocalInteractionTargeting>();
            if (targeting == null || targeting.CurrentTarget != promptTarget)
                return false;

            PlayerInventory playerInventory = requestingPlayer.GetComponent<PlayerInventory>();
            if (playerInventory == null || !playerInventory.TryAddItem(definition))
                return false;

            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }

        public void SetTargeted(bool targeted)
        {
            if (labelTransform != null)
                labelTransform.gameObject.SetActive(targeted);
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (labelTransform != null && camera != null)
                labelTransform.rotation = Quaternion.LookRotation(labelTransform.position - camera.transform.position, Vector3.up);
        }

        private void ConfigureInteractionTarget()
        {
            InteractionTarget target = GetComponent<InteractionTarget>();
            if (target == null)
                return;

            target.Configure(
                definition != null ? definition.DisplayName : "Inventory Item",
                "Pick up",
                InteractionTargetStatus.Available,
                null,
                this);
        }
    }
}
