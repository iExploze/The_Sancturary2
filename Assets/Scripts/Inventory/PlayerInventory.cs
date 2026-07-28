using System;
using System.Collections.Generic;
using TheSancturary.FusionPrototype;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Inventory
{
    [Serializable]
    public sealed class InventoryCategoryLimit
    {
        [SerializeField] private InventoryItemCategory category = InventoryItemCategory.Firearm;
        [SerializeField, Min(0)] private int maximum = 1;

        public InventoryItemCategory Category => category;
        public int Maximum => Mathf.Max(0, maximum);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEquipment), typeof(InventoryUIController))]
    public sealed class PlayerInventory : MonoBehaviour
    {
        public const int Rows = 2;
        public const int Columns = 5;

        [Header("Carry Restrictions")]
        [SerializeField] private List<InventoryCategoryLimit> categoryLimits = new()
        {
            new InventoryCategoryLimit()
        };

        [Header("Dropping")]
        [SerializeField, Min(0.5f)] private float dropDistance = 1.5f;
        [SerializeField, Min(0.05f)] private float wallClearance = 0.35f;
        [SerializeField] private LayerMask dropCollisionMask = ~0;

        private readonly List<InventoryItemInstance> _items = new();
        private readonly InventoryItemInstance[,] _cells = new InventoryItemInstance[Rows, Columns];
        private PlayerEquipment _equipment;
        private InventoryUIController _ui;
        private Camera _ownerCamera;
        private bool _ownerInitialized;
        private InventoryItemInstance _selectedItem;
        private InventoryItemInstance _focusedItem;
        private InventoryItemInstance _movingItem;
        private Vector2Int _moveOriginalPosition;

        public event Action Changed;

        public IReadOnlyList<InventoryItemInstance> Items => _items;
        public InventoryItemInstance SelectedItem => _selectedItem;
        public InventoryItemInstance FocusedItem => _focusedItem;
        public InventoryItemInstance EquippedItem => _equipment != null ? _equipment.EquippedItem : null;
        public bool IsMenuOpen => _ui != null && _ui.IsOpen;

        public void InitializeOwner(
            PlayerInput playerInput,
            Camera ownerCamera,
            LocalInteractionTargeting targeting)
        {
            if (_ownerInitialized)
                return;

            _equipment = GetComponent<PlayerEquipment>();
            _ui = GetComponent<InventoryUIController>();
            _ownerCamera = ownerCamera;
            _equipment.Initialize(ownerCamera, playerInput);
            _ui.Initialize(this, playerInput, targeting);
            _ownerInitialized = true;
        }

        public InventoryItemInstance GetCell(int row, int column)
        {
            return row >= 0 && row < Rows && column >= 0 && column < Columns
                ? _cells[row, column]
                : null;
        }

        public bool TryAddItem(InventoryItemDefinition definition)
        {
            if (definition == null || !CanAcceptCategory(definition.Category))
            {
                _ui?.ShowMessage(definition != null && definition.Category == InventoryItemCategory.Firearm
                    ? "Only one firearm can be carried"
                    : "Inventory Full");
                return false;
            }

            if (!TryFindPlacement(definition, out Vector2Int position, out bool rotated))
            {
                _ui?.ShowMessage("Inventory Full");
                return false;
            }

            InventoryItemInstance instance = new(definition, position, rotated);
            _items.Add(instance);
            OccupyCells(instance);
            Changed?.Invoke();
            return true;
        }

        public bool CanPlace(InventoryItemDefinition definition, Vector2Int topLeft, bool rotated)
        {
            if (definition == null || rotated && !definition.CanRotate)
                return false;

            int width = rotated ? definition.Height : definition.Width;
            int height = rotated ? definition.Width : definition.Height;
            if (topLeft.x < 0 || topLeft.y < 0 ||
                topLeft.x + width > Columns || topLeft.y + height > Rows)
                return false;

            for (int row = topLeft.y; row < topLeft.y + height; row++)
            {
                for (int column = topLeft.x; column < topLeft.x + width; column++)
                {
                    if (_cells[row, column] != null)
                        return false;
                }
            }

            return true;
        }

        public bool RemoveItem(InventoryItemInstance instance)
        {
            if (instance == null || !_items.Contains(instance))
                return false;

            if (_equipment != null && _equipment.EquippedItem == instance)
                _equipment.Unequip();

            FreeCells(instance);
            _items.Remove(instance);
            if (_selectedItem == instance)
                _selectedItem = null;
            if (_focusedItem == instance)
                _focusedItem = null;
            if (_movingItem == instance)
                _movingItem = null;
            Changed?.Invoke();
            return true;
        }

        public void ActivateItem(InventoryItemInstance instance)
        {
            if (instance == null || !_items.Contains(instance))
                return;

            _focusedItem = instance;
            if (!instance.Definition.CanEquip)
            {
                Changed?.Invoke();
                return;
            }

            bool clickedEquippedItem = _equipment != null &&
                                       _equipment.EquippedItem == instance;

            if (clickedEquippedItem)
            {
                _equipment.Unequip();
                _selectedItem = null;
            }
            else if (_equipment != null && _equipment.Equip(instance))
            {
                _selectedItem = instance;
            }
            else
            {
                _selectedItem = null;
            }

            Changed?.Invoke();
        }

        public bool BeginMove(InventoryItemInstance instance)
        {
            if (instance == null || !_items.Contains(instance) || _movingItem != null)
                return false;

            _movingItem = instance;
            _moveOriginalPosition = instance.GridPosition;
            _focusedItem = instance;
            FreeCells(instance);
            return true;
        }

        public bool TryMove(InventoryItemInstance instance, Vector2Int topLeft)
        {
            if (_movingItem != instance || !CanPlace(instance.Definition, topLeft, instance.Rotated))
                return false;

            instance.SetGridPosition(topLeft);
            OccupyCells(instance);
            _movingItem = null;
            Changed?.Invoke();
            return true;
        }

        public void CancelMove(InventoryItemInstance instance)
        {
            if (_movingItem != instance)
                return;

            instance.SetGridPosition(_moveOriginalPosition);
            OccupyCells(instance);
            _movingItem = null;
            Changed?.Invoke();
        }

        public bool DropSelected()
        {
            InventoryItemInstance instance = _focusedItem;
            if (instance == null || !instance.Definition.CanDrop || instance.Definition.WorldPrefab == null)
                return false;

            InventoryItemDefinition definition = instance.Definition;
            Vector3 position = FindDropPosition();
            Quaternion rotation = Quaternion.Euler(0f, _ownerCamera != null ? _ownerCamera.transform.eulerAngles.y : transform.eulerAngles.y, 0f);

            if (!RemoveItem(instance))
                return false;

            GameObject droppedObject = Instantiate(definition.WorldPrefab, position, rotation);
            WorldInventoryItem worldItem = droppedObject.GetComponent<WorldInventoryItem>();
            if (worldItem != null)
                worldItem.SetDefinition(definition);
            return true;
        }

        private bool TryFindPlacement(
            InventoryItemDefinition definition,
            out Vector2Int position,
            out bool rotated)
        {
            if (TryFindPlacementWithRotation(definition, false, out position))
            {
                rotated = false;
                return true;
            }

            if (definition.CanRotate &&
                definition.Width != definition.Height &&
                TryFindPlacementWithRotation(definition, true, out position))
            {
                rotated = true;
                return true;
            }

            position = default;
            rotated = false;
            return false;
        }

        private bool TryFindPlacementWithRotation(
            InventoryItemDefinition definition,
            bool rotated,
            out Vector2Int position)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    Vector2Int candidate = new(column, row);
                    if (!CanPlace(definition, candidate, rotated))
                        continue;

                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool CanAcceptCategory(InventoryItemCategory category)
        {
            foreach (InventoryCategoryLimit limit in categoryLimits)
            {
                if (limit == null || limit.Category != category)
                    continue;

                int count = 0;
                foreach (InventoryItemInstance item in _items)
                {
                    if (item.Definition.Category == category)
                        count++;
                }

                return count < limit.Maximum;
            }

            return true;
        }

        private void OccupyCells(InventoryItemInstance instance)
        {
            Vector2Int position = instance.GridPosition;
            for (int row = position.y; row < position.y + instance.Height; row++)
            {
                for (int column = position.x; column < position.x + instance.Width; column++)
                    _cells[row, column] = instance;
            }
        }

        private void FreeCells(InventoryItemInstance instance)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    if (_cells[row, column] == instance)
                        _cells[row, column] = null;
                }
            }
        }

        private Vector3 FindDropPosition()
        {
            Vector3 flatForward = _ownerCamera != null
                ? Vector3.ProjectOnPlane(_ownerCamera.transform.forward, Vector3.up).normalized
                : transform.forward;
            if (flatForward.sqrMagnitude < 0.01f)
                flatForward = transform.forward;

            Vector3 candidate = transform.position + flatForward * dropDistance + Vector3.up * 0.1f;
            Vector3 wallRayOrigin = (_ownerCamera != null ? _ownerCamera.transform.position : transform.position + Vector3.up) + flatForward * 0.15f;
            if (Physics.Linecast(wallRayOrigin, candidate + Vector3.up * 0.5f, out RaycastHit wallHit, dropCollisionMask, QueryTriggerInteraction.Ignore))
                candidate = wallHit.point - flatForward * wallClearance;

            Vector3 groundRayOrigin = candidate + Vector3.up * 1.5f;
            if (Physics.Raycast(groundRayOrigin, Vector3.down, out RaycastHit groundHit, 4f, dropCollisionMask, QueryTriggerInteraction.Ignore))
                candidate.y = groundHit.point.y;

            return candidate;
        }
    }
}
