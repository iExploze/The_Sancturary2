using System.Collections.Generic;
using Fusion;
using TheSancturary.Inventory;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// State-authoritative backing store for the player's 2x5 grid inventory.
    /// Unity object references remain local and are resolved through the shared catalog.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerInventory : NetworkBehaviour
    {
        public const int MaximumItems = PlayerInventory.Rows * PlayerInventory.Columns;

        [Header("Definitions")]
        [SerializeField] private InventoryItemCatalog catalog;
        [SerializeField] private string flashlightItemId = "flashlight";

        [Header("Carry Restrictions")]
        [SerializeField] private List<InventoryCategoryLimit> categoryLimits = new()
        {
            new InventoryCategoryLimit()
        };

        [Header("Authoritative Dropping")]
        [SerializeField, Min(0.5f)] private float dropDistance = 1.5f;
        [SerializeField, Min(0.05f)] private float wallClearance = 0.35f;
        [SerializeField, Min(0.1f)] private float dropOriginHeight = 1.35f;
        [SerializeField] private LayerMask dropCollisionMask = ~0;

        private const float DropPlacementStep = 0.15f;
        private const float DropBoundsPadding = 0.02f;

        [Networked, Capacity(MaximumItems)]
        public NetworkArray<NetworkInventoryEntry> Entries => default;
        [Networked] public ushort NextInstanceId { get; private set; }
        [Networked] public ushort EquippedInstanceId { get; private set; }
        [Networked] public NetworkBool FlashlightEnabled { get; private set; }
        [Networked] public ushort Revision { get; private set; }
        [Networked] public byte LastRejectionCode { get; private set; }
        [Networked] public ushort RejectionRevision { get; private set; }

        private PlayerInventory _projection;
        private PlayerEquipment _equipment;
        private FusionNetworkPlayer _player;
        private bool _catalogValid;
        private readonly Queue<InventoryInputCommand> _pendingInputCommands = new();

        public InventoryItemCatalog Catalog => catalog;
        public InventoryRequestRejection LastRejection =>
            (InventoryRequestRejection)LastRejectionCode;

        public override void Spawned()
        {
            ResolveReferences();
            flashlightItemId = NetworkLockGroup.NormalizeId(flashlightItemId);
            _catalogValid = catalog != null && catalog.ValidateCatalog(this);
            if (!_catalogValid)
                Debug.LogError("Network grid inventory requires a valid item catalog.", this);

            if (HasStateAuthority && NextInstanceId == 0)
                NextInstanceId = 1;

            ApplyPresentation();
        }

        public override void Render()
        {
            ApplyPresentation();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _pendingInputCommands.Clear();
            _equipment?.ClearPresentation();
        }

        public void InitializeOwner(PlayerInventory projection)
        {
            if (!HasInputAuthority)
                return;

            _projection = projection;
            _projection?.SynchronizeFromNetwork(this, true);
        }

        public bool TryResolveDefinition(
            string itemId,
            out InventoryItemDefinition definition)
        {
            definition = null;
            return catalog != null && catalog.TryGet(itemId, out definition);
        }

        public bool TryGetEntry(ushort instanceId, out NetworkInventoryEntry entry)
        {
            int index = FindEntryIndex(instanceId);
            if (index >= 0)
            {
                entry = Entries.Get(index);
                return true;
            }

            entry = default;
            return false;
        }

        public bool HasItem(string itemId)
        {
            string normalizedId = NetworkLockGroup.NormalizeId(itemId);
            if (string.IsNullOrEmpty(normalizedId))
                return false;

            for (int index = 0; index < MaximumItems; index++)
            {
                NetworkInventoryEntry entry = Entries.Get(index);
                if (entry.IsOccupied && entry.ItemId == normalizedId)
                    return true;
            }

            return false;
        }

        public bool CanAcceptItem(string itemId)
        {
            if (!TryResolveDefinition(itemId, out InventoryItemDefinition definition))
                return false;

            return CanAcceptCategory(definition.Category) &&
                   TryFindFirstPlacement(definition, out _, out _, out _);
        }

        public bool TryAddItemAuthoritative(string itemId, string displayName)
        {
            if (!HasStateAuthority)
                return false;

            return TryAddItemAuthoritative(itemId, out _, out _);
        }

        public bool TryAddItemAuthoritative(
            string itemId,
            out InventoryRequestRejection rejection)
        {
            if (!HasStateAuthority)
            {
                rejection = InventoryRequestRejection.InvalidRequest;
                return false;
            }

            return TryAddItemAuthoritative(itemId, out _, out rejection);
        }

        public bool TryRemoveItemAuthoritative(string itemId)
        {
            if (!HasStateAuthority)
                return false;

            string normalizedId = NetworkLockGroup.NormalizeId(itemId);
            for (int index = 0; index < MaximumItems; index++)
            {
                NetworkInventoryEntry entry = Entries.Get(index);
                if (!entry.IsOccupied || entry.ItemId != normalizedId)
                    continue;

                RemoveEntryAt(index, entry.InstanceId);
                return true;
            }

            return false;
        }

        public bool TryCollectAuthoritative(
            WorldInventoryItem worldItem,
            out InventoryRequestRejection rejection)
        {
            if (!HasStateAuthority || worldItem == null || !worldItem.IsAvailable)
            {
                rejection = worldItem != null && !worldItem.IsAvailable
                    ? InventoryRequestRejection.ItemTaken
                    : InventoryRequestRejection.InvalidRequest;
                return false;
            }

            return TryAddItemAuthoritative(worldItem.ItemId, out _, out rejection);
        }

        public void RequestMove(
            ushort instanceId,
            Vector2Int topLeft,
            bool rotated)
        {
            if (!HasInputAuthority)
                return;

            _pendingInputCommands.Enqueue(new InventoryInputCommand
            {
                Type = InventoryInputCommandType.Move,
                InstanceId = instanceId,
                Column = (byte)Mathf.Clamp(topLeft.x, 0, byte.MaxValue),
                Row = (byte)Mathf.Clamp(topLeft.y, 0, byte.MaxValue),
                Rotated = rotated
            });
        }

        public void RequestEquip(ushort instanceId)
        {
            if (HasInputAuthority)
            {
                _pendingInputCommands.Enqueue(new InventoryInputCommand
                {
                    Type = InventoryInputCommandType.Equip,
                    InstanceId = instanceId
                });
            }
        }

        public void RequestDrop(ushort instanceId)
        {
            if (HasInputAuthority)
            {
                _pendingInputCommands.Enqueue(new InventoryInputCommand
                {
                    Type = InventoryInputCommandType.Drop,
                    InstanceId = instanceId
                });
            }
        }

        public bool TryDequeueInputCommand(out InventoryInputCommand command)
        {
            if (!HasInputAuthority || _pendingInputCommands.Count == 0)
            {
                command = default;
                return false;
            }

            command = _pendingInputCommands.Dequeue();
            return true;
        }

        public void ProcessInputCommandAuthoritative(
            InventoryInputCommandType commandType,
            ushort instanceId,
            byte column,
            byte row,
            NetworkBool rotated)
        {
            if (!HasStateAuthority)
                return;

            switch (commandType)
            {
                case InventoryInputCommandType.Move:
                    ProcessMoveAuthoritative(
                        instanceId,
                        column,
                        row,
                        rotated);
                    break;
                case InventoryInputCommandType.Equip:
                    ProcessEquipAuthoritative(instanceId);
                    break;
                case InventoryInputCommandType.Drop:
                    ProcessDropAuthoritative(instanceId);
                    break;
                case InventoryInputCommandType.None:
                    break;
                default:
                    SendOwnerRejection(
                        InventoryRequestRejection.InvalidRequest);
                    break;
            }
        }

        public void ToggleEquippedUseAuthoritative()
        {
            if (!HasStateAuthority ||
                EquippedInstanceId == 0 ||
                !TryGetEntry(EquippedInstanceId, out NetworkInventoryEntry entry) ||
                entry.ItemId != flashlightItemId)
                return;

            FlashlightEnabled = !FlashlightEnabled;
        }

        public void SendOwnerRejection(InventoryRequestRejection rejection)
        {
            if (HasStateAuthority && rejection != InventoryRequestRejection.None)
            {
                LastRejectionCode = (byte)rejection;
                RejectionRevision++;
            }
        }

        private void ProcessMoveAuthoritative(
            ushort instanceId,
            byte column,
            byte row,
            NetworkBool rotated)
        {
            int index = FindEntryIndex(instanceId);
            if (index < 0)
            {
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            NetworkInventoryEntry entry = Entries.Get(index);
            if (!TryResolveDefinition(entry.ItemId.ToString(), out InventoryItemDefinition definition) ||
                rotated && !definition.CanRotate ||
                !CanPlace(definition, column, row, rotated, instanceId))
            {
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            entry.Column = column;
            entry.Row = row;
            entry.Rotated = rotated;
            Entries.Set(index, entry);
            Revision++;
        }

        private void ProcessEquipAuthoritative(ushort instanceId)
        {
            if (instanceId == EquippedInstanceId)
            {
                EquippedInstanceId = 0;
                FlashlightEnabled = false;
                Revision++;
                return;
            }

            if (!TryGetEntry(instanceId, out NetworkInventoryEntry entry) ||
                !TryResolveDefinition(entry.ItemId.ToString(), out InventoryItemDefinition definition) ||
                !definition.CanEquip)
            {
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            EquippedInstanceId = instanceId;
            FlashlightEnabled = entry.ItemId == flashlightItemId;
            Revision++;
        }

        private void ProcessDropAuthoritative(ushort instanceId)
        {
            int index = FindEntryIndex(instanceId);
            if (index < 0)
            {
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            NetworkInventoryEntry entry = Entries.Get(index);
            if (!TryResolveDefinition(entry.ItemId.ToString(), out InventoryItemDefinition definition) ||
                !definition.CanDrop ||
                definition.WorldPrefab == null ||
                definition.WorldPrefab.GetComponent<NetworkObject>() == null ||
                definition.WorldPrefab.GetComponent<NetworkTransform>() == null ||
                definition.WorldPrefab.GetComponent<Rigidbody>() == null ||
                definition.WorldPrefab.GetComponent<WorldItemPhysics>() is not { HasValidLocalBounds: true } ||
                (definition.WorldPrefab.GetComponent<WorldInventoryItem>() == null &&
                 definition.WorldPrefab.GetComponent<NetworkKeyPickup>() == null))
            {
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            Quaternion rotation = Quaternion.Euler(
                0f,
                _player != null ? _player.LookYaw : transform.eulerAngles.y,
                0f);
            WorldItemPhysics prefabPhysics =
                definition.WorldPrefab.GetComponent<WorldItemPhysics>();
            if (!TryFindDropPosition(prefabPhysics, rotation, out Vector3 position))
            {
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            NetworkObject spawned = Runner.Spawn(
                definition.WorldPrefab,
                position,
                rotation);
            if (spawned == null)
            {
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            WorldItemPhysics spawnedPhysics =
                spawned.GetComponent<WorldItemPhysics>();
            if (spawnedPhysics == null)
            {
                Runner.Despawn(spawned);
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            spawnedPhysics.PrepareForDrop();

            const float positionTolerance = 0.01f;
            const float rotationTolerance = 1f;
            if ((spawned.transform.position - position).sqrMagnitude >
                positionTolerance * positionTolerance ||
                Quaternion.Angle(spawned.transform.rotation, rotation) >
                rotationTolerance)
            {
                Runner.Despawn(spawned);
                SendOwnerRejection(InventoryRequestRejection.InvalidRequest);
                return;
            }

            RemoveEntryAt(index, instanceId);
        }

        private void ResolveReferences()
        {
            _player ??= GetComponent<FusionNetworkPlayer>();
            _projection ??= GetComponent<PlayerInventory>();
            _equipment ??= GetComponent<PlayerEquipment>();
        }

        private void ApplyPresentation()
        {
            ResolveReferences();
            if (HasInputAuthority)
                _projection?.SynchronizeFromNetwork(this);
            _equipment?.ApplyReplicatedState(this);
        }

        private bool TryAddItemAuthoritative(
            string itemId,
            out ushort instanceId,
            out InventoryRequestRejection rejection)
        {
            instanceId = 0;
            if (!HasStateAuthority || !_catalogValid ||
                !TryResolveDefinition(itemId, out InventoryItemDefinition definition))
            {
                rejection = InventoryRequestRejection.InvalidRequest;
                return false;
            }

            if (!CanAcceptCategory(definition.Category))
            {
                rejection = InventoryRequestRejection.CategoryLimitReached;
                return false;
            }

            if (!TryFindFirstPlacement(
                    definition,
                    out byte column,
                    out byte row,
                    out NetworkBool rotated))
            {
                rejection = InventoryRequestRejection.InventoryFull;
                return false;
            }

            int emptyIndex = FindEmptyEntryIndex();
            instanceId = AllocateInstanceId();
            if (emptyIndex < 0 || instanceId == 0)
            {
                rejection = InventoryRequestRejection.InventoryFull;
                return false;
            }

            Entries.Set(emptyIndex, new NetworkInventoryEntry
            {
                IsOccupied = true,
                InstanceId = instanceId,
                ItemId = NetworkLockGroup.NormalizeId(definition.ItemId),
                Column = column,
                Row = row,
                Rotated = rotated
            });
            Revision++;
            rejection = InventoryRequestRejection.None;
            return true;
        }

        private bool TryFindFirstPlacement(
            InventoryItemDefinition definition,
            out byte column,
            out byte row,
            out NetworkBool rotated)
        {
            for (int candidateRow = 0; candidateRow < PlayerInventory.Rows; candidateRow++)
            {
                for (int candidateColumn = 0;
                     candidateColumn < PlayerInventory.Columns;
                     candidateColumn++)
                {
                    if (!CanPlace(
                            definition,
                            candidateColumn,
                            candidateRow,
                            false,
                            0))
                        continue;

                    column = (byte)candidateColumn;
                    row = (byte)candidateRow;
                    rotated = false;
                    return true;
                }
            }

            if (definition.CanRotate && definition.Width != definition.Height)
            {
                for (int candidateRow = 0; candidateRow < PlayerInventory.Rows; candidateRow++)
                {
                    for (int candidateColumn = 0;
                         candidateColumn < PlayerInventory.Columns;
                         candidateColumn++)
                    {
                        if (!CanPlace(
                                definition,
                                candidateColumn,
                                candidateRow,
                                true,
                                0))
                            continue;

                        column = (byte)candidateColumn;
                        row = (byte)candidateRow;
                        rotated = true;
                        return true;
                    }
                }
            }

            column = 0;
            row = 0;
            rotated = false;
            return false;
        }

        private bool CanPlace(
            InventoryItemDefinition definition,
            int column,
            int row,
            bool rotated,
            ushort ignoredInstanceId)
        {
            if (definition == null || rotated && !definition.CanRotate)
                return false;

            int width = rotated ? definition.Height : definition.Width;
            int height = rotated ? definition.Width : definition.Height;
            if (column < 0 || row < 0 ||
                column + width > PlayerInventory.Columns ||
                row + height > PlayerInventory.Rows)
                return false;

            for (int index = 0; index < MaximumItems; index++)
            {
                NetworkInventoryEntry other = Entries.Get(index);
                if (!other.IsOccupied || other.InstanceId == ignoredInstanceId ||
                    !TryResolveDefinition(
                        other.ItemId.ToString(),
                        out InventoryItemDefinition otherDefinition))
                    continue;

                int otherWidth = other.Rotated
                    ? otherDefinition.Height
                    : otherDefinition.Width;
                int otherHeight = other.Rotated
                    ? otherDefinition.Width
                    : otherDefinition.Height;
                bool separated =
                    column + width <= other.Column ||
                    other.Column + otherWidth <= column ||
                    row + height <= other.Row ||
                    other.Row + otherHeight <= row;
                if (!separated)
                    return false;
            }

            return true;
        }

        private bool CanAcceptCategory(InventoryItemCategory category)
        {
            for (int limitIndex = 0; limitIndex < categoryLimits.Count; limitIndex++)
            {
                InventoryCategoryLimit limit = categoryLimits[limitIndex];
                if (limit == null || limit.Category != category)
                    continue;

                int count = 0;
                for (int entryIndex = 0; entryIndex < MaximumItems; entryIndex++)
                {
                    NetworkInventoryEntry entry = Entries.Get(entryIndex);
                    if (entry.IsOccupied &&
                        TryResolveDefinition(
                            entry.ItemId.ToString(),
                            out InventoryItemDefinition definition) &&
                        definition.Category == category)
                        count++;
                }

                return count < limit.Maximum;
            }

            return true;
        }

        private int FindEntryIndex(ushort instanceId)
        {
            if (instanceId == 0)
                return -1;

            for (int index = 0; index < MaximumItems; index++)
            {
                NetworkInventoryEntry entry = Entries.Get(index);
                if (entry.IsOccupied && entry.InstanceId == instanceId)
                    return index;
            }

            return -1;
        }

        private int FindEmptyEntryIndex()
        {
            for (int index = 0; index < MaximumItems; index++)
            {
                if (!Entries.Get(index).IsOccupied)
                    return index;
            }

            return -1;
        }

        private ushort AllocateInstanceId()
        {
            ushort candidate = NextInstanceId == 0 ? (ushort)1 : NextInstanceId;
            for (int attempt = 0; attempt < ushort.MaxValue; attempt++)
            {
                if (candidate != 0 && FindEntryIndex(candidate) < 0)
                {
                    NextInstanceId = candidate == ushort.MaxValue
                        ? (ushort)1
                        : (ushort)(candidate + 1);
                    return candidate;
                }

                candidate = candidate == ushort.MaxValue
                    ? (ushort)1
                    : (ushort)(candidate + 1);
            }

            return 0;
        }

        private void RemoveEntryAt(int index, ushort instanceId)
        {
            Entries.Set(index, default);
            if (EquippedInstanceId == instanceId)
            {
                EquippedInstanceId = 0;
                FlashlightEnabled = false;
            }

            Revision++;
        }

        private bool TryFindDropPosition(
            WorldItemPhysics itemPhysics,
            Quaternion rotation,
            out Vector3 position)
        {
            position = default;
            if (itemPhysics == null || !itemPhysics.HasValidLocalBounds)
                return false;

            float yaw = _player != null ? _player.LookYaw : transform.eulerAngles.y;
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Bounds bounds = itemPhysics.LocalBounds;
            Vector3 halfExtents = bounds.extents + Vector3.one * DropBoundsPadding;
            Vector3 rotatedCenterOffset = rotation * bounds.center;
            float playerRadius = 0.32f;
            if (TryGetComponent(out CharacterController controller))
            {
                playerRadius = controller.radius * Mathf.Max(
                    transform.lossyScale.x,
                    transform.lossyScale.z);
            }

            float forwardExtent =
                Mathf.Abs(Vector3.Dot(forward, rotation * Vector3.right)) * halfExtents.x +
                Mathf.Abs(Vector3.Dot(forward, rotation * Vector3.up)) * halfExtents.y +
                Mathf.Abs(Vector3.Dot(forward, rotation * Vector3.forward)) * halfExtents.z;
            float minimumDistance = playerRadius + forwardExtent + wallClearance;
            if (minimumDistance > dropDistance)
                return false;

            Vector3 releaseBase = transform.position + Vector3.up * dropOriginHeight;
            Vector3 pathStartCenter =
                releaseBase + forward * minimumDistance + rotatedCenterOffset;
            for (float distance = dropDistance;
                 distance + 0.001f >= minimumDistance;
                 distance -= DropPlacementStep)
            {
                Vector3 candidate = releaseBase + forward * distance;
                Vector3 candidateCenter = candidate + rotatedCenterOffset;
                if (!IsDropVolumeClear(candidateCenter, halfExtents, rotation) ||
                    !IsDropPathClear(
                        pathStartCenter,
                        candidateCenter,
                        halfExtents,
                        rotation))
                    continue;

                position = candidate;
                return true;
            }

            return false;
        }

        private bool IsDropVolumeClear(
            Vector3 center,
            Vector3 halfExtents,
            Quaternion rotation)
        {
            Collider[] overlaps = Physics.OverlapBox(
                center,
                halfExtents,
                rotation,
                dropCollisionMask,
                QueryTriggerInteraction.Ignore);
            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider overlap = overlaps[index];
                if (overlap != null && !overlap.transform.IsChildOf(transform))
                    return false;
            }

            return true;
        }

        private bool IsDropPathClear(
            Vector3 startCenter,
            Vector3 endCenter,
            Vector3 halfExtents,
            Quaternion rotation)
        {
            Vector3 delta = endCenter - startCenter;
            float distance = delta.magnitude;
            if (distance <= 0.001f)
                return true;

            RaycastHit[] hits = Physics.BoxCastAll(
                startCenter,
                halfExtents,
                delta / distance,
                rotation,
                distance,
                dropCollisionMask,
                QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hits.Length; index++)
            {
                Collider collider = hits[index].collider;
                if (collider != null && !collider.transform.IsChildOf(transform))
                    return false;
            }

            return true;
        }
    }
}
