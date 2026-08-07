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
        [SerializeField] private byte loadedAmmunition;

        public InventoryItemInstance(
            ushort instanceId,
            InventoryItemDefinition definition,
            Vector2Int gridPosition,
            bool rotated,
            byte loadedAmmunition = 0)
        {
            this.instanceId = instanceId;
            this.definition = definition;
            this.gridPosition = gridPosition;
            this.rotated = rotated;
            this.loadedAmmunition = loadedAmmunition;
        }

        public ushort InstanceId => instanceId;
        public InventoryItemDefinition Definition => definition;
        public Vector2Int GridPosition => gridPosition;
        public bool Rotated => rotated;
        public byte LoadedAmmunition => loadedAmmunition;
        public int Width => rotated ? definition.Height : definition.Width;
        public int Height => rotated ? definition.Width : definition.Height;

        internal void ApplyReplicatedState(
            InventoryItemDefinition itemDefinition,
            Vector2Int position,
            bool isRotated,
            byte replicatedLoadedAmmunition = 0)
        {
            definition = itemDefinition;
            gridPosition = position;
            rotated = isRotated;
            loadedAmmunition = replicatedLoadedAmmunition;
        }
    }
}
