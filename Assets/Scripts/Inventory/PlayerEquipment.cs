using TheSancturary.FusionPrototype;
using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Mesh-free flashlight presentation derived from replicated equipment state.
    /// Input and flashlight authority remain on the Fusion inventory/player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("Flashlight Presentation")]
        [SerializeField] private string flashlightItemId = "flashlight";
        [SerializeField] private FusionNetworkPlayer player;
        [SerializeField] private Transform flashlightOrigin;
        [SerializeField] private FlashlightController flashlight;

        private Transform _ownerCamera;

        public void InitializeOwner(Camera ownerCamera)
        {
            _ownerCamera = ownerCamera != null ? ownerCamera.transform : null;
        }

        public void ApplyReplicatedState(NetworkPlayerInventory inventory)
        {
            ResolveReferences();
            if (inventory == null || !inventory.Object || !inventory.Object.IsValid)
            {
                ClearPresentation();
                return;
            }

            bool ownerPresentation = inventory.HasInputAuthority;
            ushort equippedId = inventory.EquippedInstanceId;
            if (equippedId == 0 ||
                !inventory.TryGetEntry(
                    equippedId,
                    out NetworkInventoryEntry entry) ||
                !inventory.TryResolveDefinition(
                    entry.ItemId.ToString(),
                    out InventoryItemDefinition definition) ||
                NetworkLockGroup.NormalizeId(definition.ItemId) !=
                NetworkLockGroup.NormalizeId(flashlightItemId))
            {
                ClearPresentation();
                return;
            }

            Transform origin = ownerPresentation && _ownerCamera != null
                ? _ownerCamera
                : flashlightOrigin;
            if (flashlight == null || origin == null)
            {
                ClearPresentation();
                return;
            }

            Transform lightTransform = flashlight.transform;
            lightTransform.position = ownerPresentation || player == null
                ? origin.position
                : player.ReplicatedViewPosition;
            lightTransform.rotation = ownerPresentation
                ? origin.rotation
                : player != null
                    ? Quaternion.Euler(player.LookPitch, player.LookYaw, 0f)
                    : origin.rotation;
            flashlight.SetLightEnabled(inventory.FlashlightEnabled);
        }

        public void ClearPresentation()
        {
            flashlight?.SetLightEnabled(false);
        }

        private void ResolveReferences()
        {
            player ??= GetComponent<FusionNetworkPlayer>();
            flashlight ??= GetComponentInChildren<FlashlightController>(true);
            if (flashlightOrigin == null && flashlight != null)
                flashlightOrigin = flashlight.transform.parent;
        }

        private void OnDisable()
        {
            ClearPresentation();
        }
    }
}
