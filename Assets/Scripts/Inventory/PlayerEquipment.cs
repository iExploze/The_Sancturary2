using System;
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
        [SerializeField] private FirstPersonArms firstPersonArmsPrefab;
        private FirstPersonArms _firstPersonArms;

        [Header("Action Audio")]
        [SerializeField] private AudioSource ownerAudioSource;
        [SerializeField] private AudioSource spatialAudioSource;

        [Header("Flashlight Beam")]
        [SerializeField] private string flashlightItemId = "flashlight";
        [SerializeField] private Transform flashlightOrigin;
        [SerializeField] private FlashlightController flashlight;

        private Transform _ownerCamera;
        private GameObject _firstPersonHeldObject;
        private HeldItemVisual _firstPersonHeldVisual;
        private GameObject _firstPersonHeldPrefab;
        private GameObject _thirdPersonHeldObject;
        private HeldItemVisual _thirdPersonHeldVisual;
        private GameObject _thirdPersonHeldPrefab;
        private Renderer[] _thirdPersonRenderers = Array.Empty<Renderer>();
        private Light[] _thirdPersonLights = Array.Empty<Light>();
        private bool[] _thirdPersonLightEnabledBeforeSuppression =
            Array.Empty<bool>();
        private bool _ownerCameraThirdPersonSuppressed;
        private InventoryItemDefinition _heldDefinition;
        private ushort _heldInstanceId;
        private ushort _lastPresentedActionSequence;
        private ushort _presentedActiveUseInstanceId;
        private InventoryItemUseKind _presentedActiveUseKind;
        private float _unequipUntil;

        public HeldItemVisual CurrentHeldVisual =>
            _firstPersonHeldVisual != null
                ? _firstPersonHeldVisual
                : _thirdPersonHeldVisual;
        public HeldItemVisual FirstPersonHeldVisual => _firstPersonHeldVisual;
        public HeldItemVisual ThirdPersonHeldVisual => _thirdPersonHeldVisual;
        public Renderer[] ThirdPersonRenderers => _thirdPersonRenderers;

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

            bool hasInputAuthority = inventory.HasInputAuthority;
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

            Transform firstPersonAnchor = ownerFirstPersonAnchor != null
                ? ownerFirstPersonAnchor
                : _ownerCamera;
            if (definition.ThirdPersonEquippedPrefab == null ||
                thirdPersonAnchor == null ||
                hasInputAuthority &&
                (definition.EquippedPrefab == null || firstPersonAnchor == null))
            {
                ClearPresentation();
                return;
            }

            bool itemChanged = _heldDefinition != definition ||
                               _heldInstanceId != equippedId;
            if (!itemChanged && _unequipUntil > 0f)
            {
                _unequipUntil = 0f;
                _firstPersonHeldVisual?.CancelUnequip();
            }
            if (itemChanged && hasInputAuthority && _firstPersonHeldVisual != null)
            {
                if (_unequipUntil <= 0f)
                {
                    _unequipUntil = Time.unscaledTime + HeldItemVisual.UnequipDuration;
                    _firstPersonHeldVisual.BeginUnequip();
                    flashlight?.SetLightEnabled(false);
                }
                // Keep exactly one local representation while lowering the outgoing item.
                if (Time.unscaledTime < _unequipUntil) return;
            }
            if (itemChanged)
            {
                ClearPresentation();
                _heldDefinition = definition;
                _heldInstanceId = equippedId;
                _lastPresentedActionSequence =
                    player.ItemUseController != null
                        ? player.ItemUseController.ActionSequence
                        : (ushort)0;
            }

            if (hasInputAuthority)
            {
                if (!EnsureHeldVisual(
                        ref _firstPersonHeldObject,
                        ref _firstPersonHeldVisual,
                        ref _firstPersonHeldPrefab,
                        definition.EquippedPrefab,
                        firstPersonAnchor,
                        definition,
                        true,
                        "First-person"))
                {
                    ClearPresentation();
                    return;
                }
                if (_firstPersonArms == null && firstPersonArmsPrefab != null && _ownerCamera != null)
                    _firstPersonArms = Instantiate(firstPersonArmsPrefab, _ownerCamera, false);
                _firstPersonArms?.Bind(_firstPersonHeldVisual);
                _firstPersonHeldVisual.SetAim(_ownerCamera, player.ItemUseController != null && player.ItemUseController.IsAiming);
            }
            else
            {
                ClearFirstPersonPresentation();
            }

            if (!EnsureThirdPersonHeldVisual(definition))
            {
                ClearPresentation();
                return;
            }

            equipmentRig?.Configure(
                this,
                _thirdPersonHeldVisual.RightHandGrip,
                _thirdPersonHeldVisual.LeftHandGrip,
                definition.HoldStyle,
                definition.HoldPose);
            ApplyFlashlightPresentation(
                inventory,
                definition,
                hasInputAuthority);
            PresentNewAction(equippedId, definition);
            SyncActiveUsePresentation(equippedId, definition);
        }

        public void ClearPresentation()
        {
            _unequipUntil = 0f;
            flashlight?.SetLightEnabled(false);
            equipmentRig?.Clear(this);
            ClearFirstPersonPresentation();
            ClearThirdPersonPresentation();
            _heldDefinition = null;
            _heldInstanceId = 0;
            _presentedActiveUseInstanceId = 0;
            _presentedActiveUseKind = InventoryItemUseKind.None;
        }

        public void SetOwnerCameraThirdPersonSuppressed(bool suppressed)
        {
            if (_ownerCameraThirdPersonSuppressed == suppressed)
                return;

            _ownerCameraThirdPersonSuppressed = suppressed;
            for (int index = 0; index < _thirdPersonRenderers.Length; index++)
            {
                Renderer itemRenderer = _thirdPersonRenderers[index];
                if (itemRenderer != null)
                    itemRenderer.forceRenderingOff = suppressed;
            }

            if (suppressed)
            {
                for (int index = 0; index < _thirdPersonLights.Length; index++)
                {
                    Light itemLight = _thirdPersonLights[index];
                    if (itemLight == null)
                        continue;

                    _thirdPersonLightEnabledBeforeSuppression[index] =
                        itemLight.enabled;
                    itemLight.enabled = false;
                }
            }
            else
            {
                for (int index = 0; index < _thirdPersonLights.Length; index++)
                {
                    Light itemLight = _thirdPersonLights[index];
                    if (itemLight != null)
                    {
                        itemLight.enabled =
                            _thirdPersonLightEnabledBeforeSuppression[index];
                    }
                }
            }
        }

        private bool EnsureThirdPersonHeldVisual(
            InventoryItemDefinition definition)
        {
            bool rebuilt = _thirdPersonHeldObject == null ||
                           _thirdPersonHeldPrefab !=
                           definition.ThirdPersonEquippedPrefab;
            if (!EnsureHeldVisual(
                    ref _thirdPersonHeldObject,
                    ref _thirdPersonHeldVisual,
                    ref _thirdPersonHeldPrefab,
                    definition.ThirdPersonEquippedPrefab,
                    thirdPersonAnchor,
                    definition,
                    false,
                    "Third-person"))
                return false;

            if (rebuilt)
                CacheThirdPersonEffects();
            return true;
        }

        private bool EnsureHeldVisual(
            ref GameObject heldObject,
            ref HeldItemVisual heldVisual,
            ref GameObject heldPrefab,
            GameObject prefab,
            Transform anchor,
            InventoryItemDefinition definition,
            bool ownerPresentation,
            string presentationName)
        {
            bool rebuild = heldObject == null || heldPrefab != prefab;
            if (rebuild)
            {
                DestroyHeldVisual(ref heldObject, ref heldVisual, ref heldPrefab);
                heldObject = Instantiate(prefab, anchor, false);
                heldObject.name = $"{presentationName} Held {definition.DisplayName}";
                heldObject.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                heldVisual = heldObject.GetComponent<HeldItemVisual>();
                if (heldVisual == null)
                {
                    Debug.LogError(
                        $"Held prefab '{prefab.name}' requires {nameof(HeldItemVisual)}.",
                        prefab);
                    DestroyHeldVisual(
                        ref heldObject,
                        ref heldVisual,
                        ref heldPrefab);
                    return false;
                }

                heldPrefab = prefab;
                heldVisual.Configure(definition, ownerPresentation);
            }
            else if (heldObject.transform.parent != anchor)
            {
                heldObject.transform.SetParent(anchor, false);
                heldObject.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
            }

            return heldVisual != null;
        }

        private void CacheThirdPersonEffects()
        {
            bool wasSuppressed = _ownerCameraThirdPersonSuppressed;
            if (wasSuppressed)
                SetOwnerCameraThirdPersonSuppressed(false);

            _thirdPersonRenderers = _thirdPersonHeldObject != null
                ? _thirdPersonHeldObject.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
            _thirdPersonLights = _thirdPersonHeldObject != null
                ? _thirdPersonHeldObject.GetComponentsInChildren<Light>(true)
                : Array.Empty<Light>();
            _thirdPersonLightEnabledBeforeSuppression =
                new bool[_thirdPersonLights.Length];

            if (wasSuppressed)
                SetOwnerCameraThirdPersonSuppressed(true);
        }

        private void ClearFirstPersonPresentation()
        {
            if (_firstPersonArms != null)
            {
                _firstPersonArms.gameObject.SetActive(false);
                Destroy(_firstPersonArms.gameObject);
                _firstPersonArms = null;
            }
            DestroyHeldVisual(
                ref _firstPersonHeldObject,
                ref _firstPersonHeldVisual,
                ref _firstPersonHeldPrefab);
        }

        private void ClearThirdPersonPresentation()
        {
            SetOwnerCameraThirdPersonSuppressed(false);
            DestroyHeldVisual(
                ref _thirdPersonHeldObject,
                ref _thirdPersonHeldVisual,
                ref _thirdPersonHeldPrefab);
            _thirdPersonRenderers = Array.Empty<Renderer>();
            _thirdPersonLights = Array.Empty<Light>();
            _thirdPersonLightEnabledBeforeSuppression = Array.Empty<bool>();
        }

        private static void DestroyHeldVisual(
            ref GameObject heldObject,
            ref HeldItemVisual heldVisual,
            ref GameObject heldPrefab)
        {
            if (heldObject != null)
            {
                heldObject.SetActive(false);
                Destroy(heldObject);
            }

            heldObject = null;
            heldVisual = null;
            heldPrefab = null;
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
            if (action == ItemActionPresentation.Reload)
                _firstPersonHeldVisual?.PlayReloadFromElapsed(0);
            else if (action != ItemActionPresentation.Complete)
            {
                bool dryFire = action == ItemActionPresentation.DryFire;
                _firstPersonHeldVisual?.PlayUse(dryFire);
                if (action == ItemActionPresentation.Fire || action == ItemActionPresentation.DryFire)
                    _thirdPersonHeldVisual?.PresentRemoteFire(dryFire);
            }

            // Audio is emitted once by the authoritative action event, including completion after consumption.
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
                {
                    _firstPersonHeldVisual?.CancelUse();
                }

                _presentedActiveUseInstanceId = 0;
                _presentedActiveUseKind = InventoryItemUseKind.None;
                return;
            }

            if (_presentedActiveUseInstanceId == equippedInstanceId &&
                _presentedActiveUseKind == controller.ActiveUseKind)
                return;

            float elapsedSeconds = Mathf.Max(
                0f,
                controller.ActiveDuration -
                controller.ActiveUseRemainingSeconds);
            if (controller.IsReloading) _firstPersonHeldVisual?.PlayReloadFromElapsed(elapsedSeconds);
            else _firstPersonHeldVisual?.PlayUseFromElapsed(false, elapsedSeconds);
            _presentedActiveUseInstanceId = equippedInstanceId;
            _presentedActiveUseKind = controller.ActiveUseKind;
        }

        public void PlayActionAudio(AudioClip clip)
        {
            ResolveReferences();
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
