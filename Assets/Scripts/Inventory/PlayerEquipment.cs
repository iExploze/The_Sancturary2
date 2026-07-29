using TheSancturary.FusionPrototype;
using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Local presentation of replicated equipment state. The owner receives a
    /// first-person object; proxies receive a third-person hand object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("First-Person Held Pose")]
        [SerializeField] private Vector3 heldLocalPosition =
            new(0.28f, -0.25f, 0.55f);
        [SerializeField] private Vector3 heldLocalEulerAngles;
        [SerializeField] private Vector3 heldLocalScale = Vector3.one;

        [Header("Third-Person Held Pose")]
        [SerializeField] private Transform thirdPersonHeldAnchor;
        [SerializeField] private Vector3 thirdPersonLocalPosition;
        [SerializeField] private Vector3 thirdPersonLocalEulerAngles =
            new(0f, 90f, 0f);
        [SerializeField] private Vector3 thirdPersonLocalScale = Vector3.one;

        private Transform _firstPersonHeldAnchor;
        private GameObject _heldObject;
        private ushort _presentedInstanceId;
        private bool _presentedAsOwner;
        private bool _missingThirdPersonAnchorLogged;

        public void InitializeOwner(Camera ownerCamera)
        {
            if (_firstPersonHeldAnchor != null || ownerCamera == null)
                return;

            GameObject anchorObject = new("Held Item Anchor");
            _firstPersonHeldAnchor = anchorObject.transform;
            _firstPersonHeldAnchor.SetParent(ownerCamera.transform, false);
            _firstPersonHeldAnchor.localPosition = heldLocalPosition;
            _firstPersonHeldAnchor.localRotation =
                Quaternion.Euler(heldLocalEulerAngles);
            _firstPersonHeldAnchor.localScale = heldLocalScale;
        }

        public void ApplyReplicatedState(NetworkPlayerInventory inventory)
        {
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
                    out InventoryItemDefinition definition))
            {
                ClearPresentation();
                return;
            }

            if (_heldObject == null ||
                _presentedInstanceId != equippedId ||
                _presentedAsOwner != ownerPresentation)
            {
                CreatePresentation(
                    definition,
                    equippedId,
                    ownerPresentation);
            }

            FlashlightController flashlight =
                _heldObject != null
                    ? _heldObject.GetComponentInChildren<FlashlightController>(true)
                    : null;
            flashlight?.SetLightEnabled(inventory.FlashlightEnabled);
        }

        public void ClearPresentation()
        {
            if (_heldObject != null)
            {
                FlashlightController flashlight =
                    _heldObject.GetComponentInChildren<FlashlightController>(true);
                flashlight?.SetLightEnabled(false);
                Destroy(_heldObject);
            }

            _heldObject = null;
            _presentedInstanceId = 0;
        }

        private void CreatePresentation(
            InventoryItemDefinition definition,
            ushort instanceId,
            bool ownerPresentation)
        {
            ClearPresentation();
            Transform anchor = ownerPresentation
                ? _firstPersonHeldAnchor
                : ResolveThirdPersonAnchor();
            GameObject prefab = ownerPresentation
                ? definition.EquippedPrefab
                : definition.ThirdPersonEquippedPrefab;
            if (anchor == null || prefab == null)
                return;

            _heldObject = Instantiate(prefab, anchor);
            _heldObject.name =
                $"{definition.DisplayName} " +
                (ownerPresentation ? "(First Person)" : "(Third Person)");
            if (ownerPresentation)
            {
                _heldObject.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                _heldObject.transform.localScale = Vector3.one;
            }
            else
            {
                _heldObject.transform.localPosition = thirdPersonLocalPosition;
                _heldObject.transform.localRotation =
                    Quaternion.Euler(thirdPersonLocalEulerAngles);
                _heldObject.transform.localScale = thirdPersonLocalScale;
            }

            foreach (Collider heldCollider in
                     _heldObject.GetComponentsInChildren<Collider>(true))
                heldCollider.enabled = false;

            _presentedInstanceId = instanceId;
            _presentedAsOwner = ownerPresentation;
        }

        private Transform ResolveThirdPersonAnchor()
        {
            if (thirdPersonHeldAnchor != null)
                return thirdPersonHeldAnchor;

            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
                thirdPersonHeldAnchor =
                    animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (thirdPersonHeldAnchor == null &&
                !_missingThirdPersonAnchorLogged)
            {
                _missingThirdPersonAnchorLogged = true;
                Debug.LogError(
                    "Third-person equipment requires a right-hand anchor. " +
                    "Assign PlayerEquipment.thirdPersonHeldAnchor if the " +
                    "character avatar is not Humanoid.",
                    this);
            }

            return thirdPersonHeldAnchor;
        }

        private void OnDisable()
        {
            ClearPresentation();
        }
    }
}
