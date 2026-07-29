using System;
using UnityEngine;

namespace TheSancturary.Inventory
{
    [Serializable]
    public sealed class InventoryItemInstance
    {
        [SerializeField] private InventoryItemDefinition definition;
        [SerializeField] private ushort instanceId;
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private bool rotated;

        public InventoryItemInstance(
            ushort instanceId,
            InventoryItemDefinition definition,
            Vector2Int gridPosition,
            bool rotated)
        {
            this.instanceId = instanceId;
            this.definition = definition;
            this.gridPosition = gridPosition;
            this.rotated = rotated;
        }

        public ushort InstanceId => instanceId;
        public InventoryItemDefinition Definition => definition;
        public Vector2Int GridPosition => gridPosition;
        public bool Rotated => rotated;
        public int Width => rotated ? definition.Height : definition.Width;
        public int Height => rotated ? definition.Width : definition.Height;

        internal void ApplyReplicatedState(
            InventoryItemDefinition itemDefinition,
            Vector2Int position,
            bool isRotated)
        {
            definition = itemDefinition;
            gridPosition = position;
            rotated = isRotated;
        }
    }
}
