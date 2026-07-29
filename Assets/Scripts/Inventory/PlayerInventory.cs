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
        [SerializeField] private InventoryItemCategory category =
            InventoryItemCategory.Firearm;
        [SerializeField, Min(0)] private int maximum = 1;

        public InventoryItemCategory Category => category;
        public int Maximum => Mathf.Max(0, maximum);
    }

    /// <summary>
    /// Owner-only projection and UI controller for NetworkPlayerInventory.
    /// It never commits shared gameplay state directly.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEquipment), typeof(InventoryUIController))]
    public sealed class PlayerInventory : MonoBehaviour
    {
        public const int Rows = 2;
        public const int Columns = 5;

        private readonly List<InventoryItemInstance> _items = new();
        private readonly InventoryItemInstance[,] _cells =
            new InventoryItemInstance[Rows, Columns];
        private NetworkPlayerInventory _networkInventory;
        private InventoryUIController _ui;
        private bool _ownerInitialized;
        private bool _hasSnapshot;
        private ushort _renderedRevision;
        private ushort _renderedEquippedInstanceId;
        private InventoryItemInstance _selectedItem;
        private InventoryItemInstance _focusedItem;
        private InventoryItemInstance _movingItem;
        private Vector2Int _moveOriginalPosition;
        private bool _moveOriginalRotated;

        public event Action Changed;

        public IReadOnlyList<InventoryItemInstance> Items => _items;
        public InventoryItemInstance SelectedItem => _selectedItem;
        public InventoryItemInstance FocusedItem => _focusedItem;
        public InventoryItemInstance EquippedItem => _selectedItem;
        public bool IsMenuOpen => _ui != null && _ui.IsOpen;

        public void InitializeOwner(
            PlayerInput playerInput,
            Camera ownerCamera,
            LocalInteractionTargeting targeting,
            NetworkPlayerInventory networkInventory)
        {
            if (_ownerInitialized)
                return;

            _networkInventory = networkInventory;
            _ui = GetComponent<InventoryUIController>();
            _ui.Initialize(this, playerInput, targeting);
            _networkInventory?.InitializeOwner(this);
            _ownerInitialized = true;
        }

        public void SynchronizeFromNetwork(
            NetworkPlayerInventory networkInventory,
            bool force = false)
        {
            if (networkInventory == null || !networkInventory.HasInputAuthority)
                return;

            bool equipmentChanged =
                !_hasSnapshot ||
                _renderedEquippedInstanceId != networkInventory.EquippedInstanceId;
            if (!force &&
                _hasSnapshot &&
                _renderedRevision == networkInventory.Revision &&
                !equipmentChanged)
                return;

            ushort focusedId = _focusedItem?.InstanceId ?? 0;
            Dictionary<ushort, InventoryItemInstance> existing = new();
            for (int index = 0; index < _items.Count; index++)
                existing[_items[index].InstanceId] = _items[index];

            _items.Clear();
            ClearCells();
            for (int index = 0;
                 index < NetworkPlayerInventory.MaximumItems;
                 index++)
            {
                NetworkInventoryEntry entry = networkInventory.Entries.Get(index);
                if (!entry.IsOccupied ||
                    !networkInventory.TryResolveDefinition(
                        entry.ItemId.ToString(),
                        out InventoryItemDefinition definition))
                    continue;

                if (!existing.TryGetValue(
                        entry.InstanceId,
                        out InventoryItemInstance instance))
                {
                    instance = new InventoryItemInstance(
                        entry.InstanceId,
                        definition,
                        new Vector2Int(entry.Column, entry.Row),
                        entry.Rotated);
                }
                else
                {
                    instance.ApplyReplicatedState(
                        definition,
                        new Vector2Int(entry.Column, entry.Row),
                        entry.Rotated);
                }

                _items.Add(instance);
                OccupyCells(instance);
            }

            _movingItem = null;
            _focusedItem = FindInstance(focusedId);
            _selectedItem = FindInstance(networkInventory.EquippedInstanceId);
            _renderedRevision = networkInventory.Revision;
            _renderedEquippedInstanceId = networkInventory.EquippedInstanceId;
            _hasSnapshot = true;
            Changed?.Invoke();
        }

        public InventoryItemInstance GetCell(int row, int column)
        {
            return row >= 0 && row < Rows && column >= 0 && column < Columns
                ? _cells[row, column]
                : null;
        }

        public bool CanPlace(
            InventoryItemDefinition definition,
            Vector2Int topLeft,
            bool rotated)
        {
            if (definition == null || rotated && !definition.CanRotate)
                return false;

            int width = rotated ? definition.Height : definition.Width;
            int height = rotated ? definition.Width : definition.Height;
            if (topLeft.x < 0 || topLeft.y < 0 ||
                topLeft.x + width > Columns ||
                topLeft.y + height > Rows)
                return false;

            for (int row = topLeft.y; row < topLeft.y + height; row++)
            {
                for (int column = topLeft.x;
                     column < topLeft.x + width;
                     column++)
                {
                    if (_cells[row, column] != null)
                        return false;
                }
            }

            return true;
        }

        public void ActivateItem(InventoryItemInstance instance)
        {
            if (instance == null || !_items.Contains(instance))
                return;

            _focusedItem = instance;
            if (instance.Definition.CanEquip)
                _networkInventory?.RequestEquip(instance.InstanceId);
            Changed?.Invoke();
        }

        public bool BeginMove(InventoryItemInstance instance)
        {
            if (instance == null || !_items.Contains(instance) || _movingItem != null)
                return false;

            _movingItem = instance;
            _moveOriginalPosition = instance.GridPosition;
            _moveOriginalRotated = instance.Rotated;
            _focusedItem = instance;
            FreeCells(instance);
            return true;
        }

        public bool TryMove(InventoryItemInstance instance, Vector2Int topLeft)
        {
            if (_movingItem != instance ||
                !CanPlace(instance.Definition, topLeft, instance.Rotated))
                return false;

            RestoreMovingItem();
            _networkInventory?.RequestMove(
                instance.InstanceId,
                topLeft,
                instance.Rotated);
            return true;
        }

        public void CancelMove(InventoryItemInstance instance)
        {
            if (_movingItem == instance)
                RestoreMovingItem();
        }

        public bool DropSelected()
        {
            InventoryItemInstance instance = _focusedItem;
            if (instance == null || !instance.Definition.CanDrop)
                return false;

            _networkInventory?.RequestDrop(instance.InstanceId);
            return _networkInventory != null;
        }

        public void ShowRejection(InventoryRequestRejection rejection)
        {
            string message = rejection switch
            {
                InventoryRequestRejection.InventoryFull => "Inventory Full",
                InventoryRequestRejection.CategoryLimitReached =>
                    "Category Limit Reached",
                InventoryRequestRejection.ItemTaken => "Item Taken",
                _ => "Action Rejected"
            };
            _ui?.ShowMessage(message);
        }

        private void RestoreMovingItem()
        {
            if (_movingItem == null)
                return;

            _movingItem.ApplyReplicatedState(
                _movingItem.Definition,
                _moveOriginalPosition,
                _moveOriginalRotated);
            OccupyCells(_movingItem);
            _movingItem = null;
            Changed?.Invoke();
        }

        private InventoryItemInstance FindInstance(ushort instanceId)
        {
            if (instanceId == 0)
                return null;

            for (int index = 0; index < _items.Count; index++)
            {
                if (_items[index].InstanceId == instanceId)
                    return _items[index];
            }

            return null;
        }

        private void OccupyCells(InventoryItemInstance instance)
        {
            Vector2Int position = instance.GridPosition;
            for (int row = position.y; row < position.y + instance.Height; row++)
            {
                for (int column = position.x;
                     column < position.x + instance.Width;
                     column++)
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

        private void ClearCells()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                    _cells[row, column] = null;
            }
        }
    }
}
