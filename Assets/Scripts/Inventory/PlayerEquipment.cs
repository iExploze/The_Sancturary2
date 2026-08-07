using Fusion;
using TheSancturary.FusionPrototype;
using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Local presentation projection for the replicated equipped instance. It
    /// owns no gameplay state and never reads input.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("Replicated Equipment")]
        [SerializeField] private FusionNetworkPlayer player;
        [SerializeField] private Transform ownerFirstPersonAnchor;
        [SerializeField] private Transform thirdPersonAnchor;
        [SerializeField] private PlayerEquipmentRigController equipmentRig;

        [Header("Action Audio")]
        [SerializeField] private AudioSource ownerAudioSource;
        [SerializeField] private AudioSource spatialAudioSource;

        [Header("Flashlight Beam")]
        [SerializeField] private string flashlightItemId = "flashlight";
        [SerializeField] private Transform flashlightOrigin;
        [SerializeField] private FlashlightController flashlight;

        private Transform _ownerCamera;
        private GameObject _heldObject;
        private HeldItemVisual _heldVisual;
        private GameObject _heldPrefab;
        private InventoryItemDefinition _heldDefinition;
        private ushort _heldInstanceId;
        private bool _heldIsOwnerPresentation;
        private ushort _lastPresentedActionSequence;
        private ushort _presentedActiveUseInstanceId;
        private InventoryItemUseKind _presentedActiveUseKind;

        public HeldItemVisual CurrentHeldVisual => _heldVisual;

        public void InitializeOwner(Camera ownerCamera)
        {
            _ownerCamera = ownerCamera != null ? ownerCamera.transform : null;
        }

        public void ApplyReplicatedState(NetworkPlayerInventory inventory)
        {
            ResolveReferences();
            if (inventory == null || !inventory.Object ||
                !inventory.Object.IsValid || player == null ||
                player.IsDead || player.IsHiddenInLocker)
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
                    out InventoryItemDefinition definition))
            {
                ClearPresentation();
                return;
            }

            GameObject prefab = ownerPresentation
                ? definition.EquippedPrefab
                : definition.ThirdPersonEquippedPrefab;
            Transform anchor = ownerPresentation
                ? ownerFirstPersonAnchor != null
                    ? ownerFirstPersonAnchor
                    : _ownerCamera
                : thirdPersonAnchor;
            if (prefab == null || anchor == null)
            {
                ClearPresentation();
                return;
            }

            EnsureHeldVisual(
                equippedId,
                definition,
                prefab,
                anchor,
                ownerPresentation);
            ApplyFlashlightPresentation(
                inventory,
                definition,
                ownerPresentation);
            PresentNewAction(equippedId, definition);
            SyncActiveUsePresentation(equippedId, definition);
        }

        public void ClearPresentation()
        {
            flashlight?.SetLightEnabled(false);
            equipmentRig?.Clear(this);
            if (_heldObject != null)
            {
                _heldObject.SetActive(false);
                Destroy(_heldObject);
            }

            _heldObject = null;
            _heldVisual = null;
            _heldPrefab = null;
            _heldDefinition = null;
            _heldInstanceId = 0;
            _heldIsOwnerPresentation = false;
            _presentedActiveUseInstanceId = 0;
            _presentedActiveUseKind = InventoryItemUseKind.None;
        }

        private void EnsureHeldVisual(
            ushort instanceId,
            InventoryItemDefinition definition,
            GameObject prefab,
            Transform anchor,
            bool ownerPresentation)
        {
            bool rebuild = _heldObject == null ||
                           _heldPrefab != prefab ||
                           _heldDefinition != definition ||
                           _heldInstanceId != instanceId ||
                           _heldIsOwnerPresentation != ownerPresentation;
            if (rebuild)
            {
                ClearPresentation();
                _heldObject = Instantiate(prefab, anchor, false);
                _heldObject.name = $"Held {definition.DisplayName}";
                _heldObject.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                _heldVisual = _heldObject.GetComponent<HeldItemVisual>();
                if (_heldVisual == null)
                {
                    Debug.LogError(
                        $"Held prefab '{prefab.name}' requires {nameof(HeldItemVisual)}.",
                        prefab);
                    ClearPresentation();
                    return;
                }

                _heldPrefab = prefab;
                _heldDefinition = definition;
                _heldInstanceId = instanceId;
                _heldIsOwnerPresentation = ownerPresentation;
                _heldVisual.Configure(definition, ownerPresentation);
                _lastPresentedActionSequence =
                    player.ItemUseController != null
                        ? player.ItemUseController.ActionSequence
                        : (ushort)0;
            }
            else if (_heldObject.transform.parent != anchor)
            {
                _heldObject.transform.SetParent(anchor, false);
                _heldObject.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
            }

            if (ownerPresentation || _heldVisual == null)
            {
                equipmentRig?.Clear(this);
            }
            else
            {
                equipmentRig?.Configure(
                    this,
                    _heldVisual.RightHandGrip,
                    _heldVisual.LeftHandGrip,
                    definition.HoldStyle);
            }
        }

        private void PresentNewAction(
            ushort equippedInstanceId,
            InventoryItemDefinition definition)
        {
            NetworkItemUseController controller =
                player != null ? player.ItemUseController : null;
            if (controller == null ||
                controller.ActionSequence == _lastPresentedActionSequence)
                return;

            _lastPresentedActionSequence = controller.ActionSequence;
            if (controller.ActionInstanceId != equippedInstanceId)
                return;

            ItemActionPresentation action = controller.LastAction;
            if (action != ItemActionPresentation.Reload)
            {
                _heldVisual?.PlayUse(
                    action == ItemActionPresentation.DryFire);
            }

            AudioClip clip = action switch
            {
                ItemActionPresentation.DryFire =>
                    definition.DryFireAudioClip,
                ItemActionPresentation.Reload =>
                    definition.ReloadAudioClip,
                ItemActionPresentation.Fire =>
                    definition.UseAudioClip,
                ItemActionPresentation.Use =>
                    definition.UseAudioClip,
                _ => null
            };
            PlayActionAudio(clip);
        }

        private void SyncActiveUsePresentation(
            ushort equippedInstanceId,
            InventoryItemDefinition definition)
        {
            NetworkItemUseController controller =
                player != null ? player.ItemUseController : null;
            bool isThisItemActive =
                controller != null &&
                controller.HasActiveUse &&
                controller.ActiveUseInstanceId == equippedInstanceId &&
                controller.ActiveUseKind == definition.UseKind;

            if (!isThisItemActive)
            {
                if (_presentedActiveUseInstanceId != 0)
                    _heldVisual?.CancelUse();

                _presentedActiveUseInstanceId = 0;
                _presentedActiveUseKind = InventoryItemUseKind.None;
                return;
            }

            if (_presentedActiveUseInstanceId == equippedInstanceId &&
                _presentedActiveUseKind == controller.ActiveUseKind)
                return;

            float elapsedSeconds = Mathf.Max(
                0f,
                definition.UseDuration -
                controller.ActiveUseRemainingSeconds);
            _heldVisual?.PlayUseFromElapsed(false, elapsedSeconds);
            _presentedActiveUseInstanceId = equippedInstanceId;
            _presentedActiveUseKind = controller.ActiveUseKind;
        }

        private void PlayActionAudio(AudioClip clip)
        {
            if (clip == null)
                return;

            AudioSource source = player != null && player.HasInputAuthority
                ? ownerAudioSource
                : spatialAudioSource;
            source?.PlayOneShot(clip);
        }

        private void ApplyFlashlightPresentation(
            NetworkPlayerInventory inventory,
            InventoryItemDefinition definition,
            bool ownerPresentation)
        {
            bool isFlashlight =
                NetworkLockGroup.NormalizeId(definition.ItemId) ==
                NetworkLockGroup.NormalizeId(flashlightItemId);
            if (!isFlashlight || flashlight == null)
            {
                flashlight?.SetLightEnabled(false);
                return;
            }

            Transform origin = ownerPresentation && _ownerCamera != null
                ? _ownerCamera
                : flashlightOrigin;
            if (origin == null)
            {
                flashlight.SetLightEnabled(false);
                return;
            }

            Transform beam = flashlight.transform;
            beam.position = ownerPresentation
                ? origin.position
                : player.ReplicatedViewPosition;
            beam.rotation = ownerPresentation
                ? origin.rotation
                : Quaternion.Euler(player.LookPitch, player.LookYaw, 0f);
            flashlight.SetLightEnabled(inventory.FlashlightEnabled);
        }

        private void ResolveReferences()
        {
            player ??= GetComponent<FusionNetworkPlayer>();
            equipmentRig ??= GetComponentInChildren<PlayerEquipmentRigController>(
                true);
            flashlight ??= GetComponentInChildren<FlashlightController>(true);
            if (flashlightOrigin == null && flashlight != null)
                flashlightOrigin = flashlight.transform.parent;

            AudioSource[] sources = GetComponents<AudioSource>();
            ownerAudioSource ??= sources.Length > 0 ? sources[0] : null;
            spatialAudioSource ??= sources.Length > 1
                ? sources[1]
                : ownerAudioSource;
        }

        private void OnDisable()
        {
            ClearPresentation();
        }
    }
}
