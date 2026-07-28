using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Inventory
{
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("First-Person Held Pose")]
        [SerializeField] private Vector3 heldLocalPosition = new(0.28f, -0.25f, 0.55f);
        [SerializeField] private Vector3 heldLocalEulerAngles;
        [SerializeField] private Vector3 heldLocalScale = Vector3.one;

        private Transform _heldAnchor;
        private PlayerInput _playerInput;
        private GameObject _heldObject;

        public InventoryItemInstance EquippedItem { get; private set; }

        public void Initialize(Camera ownerCamera, PlayerInput playerInput)
        {
            _playerInput = playerInput;
            if (_heldAnchor != null || ownerCamera == null)
                return;

            GameObject anchorObject = new("Held Item Anchor");
            _heldAnchor = anchorObject.transform;
            _heldAnchor.SetParent(ownerCamera.transform, false);
            _heldAnchor.localPosition = heldLocalPosition;
            _heldAnchor.localRotation = Quaternion.Euler(heldLocalEulerAngles);
            _heldAnchor.localScale = heldLocalScale;
        }

        public bool Equip(InventoryItemInstance instance)
        {
            if (instance == null ||
                !instance.Definition.CanEquip ||
                instance.Definition.EquippedPrefab == null ||
                _heldAnchor == null)
                return false;

            Unequip();
            EquippedItem = instance;
            _heldObject = Instantiate(instance.Definition.EquippedPrefab, _heldAnchor);
            _heldObject.name = $"{instance.Definition.DisplayName} (Held)";
            _heldObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Collider heldCollider in _heldObject.GetComponentsInChildren<Collider>(true))
                heldCollider.enabled = false;

            FlashlightController flashlight = _heldObject.GetComponentInChildren<FlashlightController>(true);
            if (flashlight != null)
                flashlight.Initialize(_playerInput, GetComponent<PlayerInventory>());
            return true;
        }

        public void Unequip()
        {
            if (_heldObject != null)
            {
                FlashlightController flashlight = _heldObject.GetComponentInChildren<FlashlightController>(true);
                if (flashlight != null)
                    flashlight.SetLightEnabled(false);
                Destroy(_heldObject);
            }

            _heldObject = null;
            EquippedItem = null;
        }

        private void OnDisable()
        {
            Unequip();
        }
    }
}
