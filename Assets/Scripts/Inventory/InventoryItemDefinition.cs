using UnityEngine;

namespace TheSancturary.Inventory
{
    public enum InventoryItemCategory
    {
        Test,
        Equipment,
        Medicine,
        Key,
        PuzzleTool,
        Firearm
    }

    [CreateAssetMenu(menuName = "The Sancturary/Inventory/Item Definition", fileName = "InventoryItem")]
    public sealed class InventoryItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName;
        [SerializeField] private string itemId;
        [SerializeField] private InventoryItemCategory category;
        [SerializeField, TextArea(1, 2)] private string temporaryIconLabel;

        [Header("Grid")]
        [SerializeField, Min(1)] private int width = 1;
        [SerializeField, Min(1)] private int height = 1;
        [SerializeField] private bool canRotate;
        [SerializeField] private bool stackable;

        [Header("Use")]
        [SerializeField] private bool canEquip;
        [SerializeField] private bool canDrop = true;
        [SerializeField] private GameObject worldPrefab;
        [SerializeField] private GameObject equippedPrefab;
        [SerializeField] private GameObject thirdPersonEquippedPrefab;

        public string DisplayName => displayName;
        public string ItemId => itemId;
        public InventoryItemCategory Category => category;
        public string TemporaryIconLabel => temporaryIconLabel;
        public int Width => Mathf.Max(1, width);
        public int Height => Mathf.Max(1, height);
        public bool CanRotate => canRotate;
        public bool Stackable => stackable;
        public bool CanEquip => canEquip;
        public bool CanDrop => canDrop;
        public GameObject WorldPrefab => worldPrefab;
        public GameObject EquippedPrefab => equippedPrefab;
        public GameObject ThirdPersonEquippedPrefab =>
            thirdPersonEquippedPrefab != null ? thirdPersonEquippedPrefab : equippedPrefab;

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            itemId = itemId?.Trim();
            displayName = displayName?.Trim();
        }
    }
}
