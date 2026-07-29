using System;
using System.Collections.Generic;
using TheSancturary.FusionPrototype;
using UnityEngine;

namespace TheSancturary.Inventory
{
    [CreateAssetMenu(
        menuName = "The Sancturary/Inventory/Item Catalog",
        fileName = "InventoryItemCatalog")]
    public sealed class InventoryItemCatalog : ScriptableObject
    {
        [SerializeField] private List<InventoryItemDefinition> definitions = new();

        private readonly Dictionary<string, InventoryItemDefinition> _byId =
            new(StringComparer.Ordinal);
        private bool _cacheBuilt;
        private bool _isValid;

        public IReadOnlyList<InventoryItemDefinition> Definitions => definitions;

        public bool TryGet(string itemId, out InventoryItemDefinition definition)
        {
            EnsureCache();
            return _byId.TryGetValue(NetworkLockGroup.NormalizeId(itemId), out definition);
        }

        public bool ValidateCatalog(UnityEngine.Object context)
        {
            RebuildCache(context);
            return _isValid;
        }

        private void OnEnable()
        {
            _cacheBuilt = false;
        }

        private void OnValidate()
        {
            _cacheBuilt = false;
        }

        private void EnsureCache()
        {
            if (!_cacheBuilt)
                RebuildCache(this);
        }

        private void RebuildCache(UnityEngine.Object context)
        {
            _byId.Clear();
            _isValid = true;
            _cacheBuilt = true;

            for (int index = 0; index < definitions.Count; index++)
            {
                InventoryItemDefinition definition = definitions[index];
                if (definition == null)
                {
                    Debug.LogError(
                        $"Inventory catalog '{name}' has a missing definition at index {index}.",
                        context);
                    _isValid = false;
                    continue;
                }

                string itemId = NetworkLockGroup.NormalizeId(definition.ItemId);
                if (string.IsNullOrEmpty(itemId))
                {
                    Debug.LogError(
                        $"Inventory definition '{definition.name}' has a missing item ID.",
                        definition);
                    _isValid = false;
                    continue;
                }

                if (itemId.Length > 31)
                {
                    Debug.LogError(
                        $"Inventory definition '{definition.name}' uses item ID '{itemId}', " +
                        "which exceeds the replicated 31-character limit.",
                        definition);
                    _isValid = false;
                    continue;
                }

                if (!_byId.TryAdd(itemId, definition))
                {
                    Debug.LogError(
                        $"Inventory catalog '{name}' contains duplicate item ID '{itemId}'.",
                        context);
                    _isValid = false;
                }
            }
        }
    }
}
