using UnityEngine;
using TheSancturary.FusionPrototype;

namespace TheSancturary.Inventory
{
    public enum InventoryItemCategory
    {
        Test,
        Equipment,
        Medicine,
        Key,
        PuzzleTool,
        Firearm,
        Ammunition
    }

    public enum InventoryItemUseKind
    {
        None,
        FlashlightToggle,
        RevivalSyringe,
        FullHeal,
        Adrenaline,
        Crowbar,
        FireAxe,
        Firearm
    }

    public enum InventoryHoldStyle
    {
        None,
        OneHanded,
        TwoHanded
    }

    public enum InventoryHoldPose
    {
        None,
        OneHandedCarry,
        Pistol,
        LongGun,
        TwoHandedTool
    }

    [CreateAssetMenu(menuName = "The Sancturary/Inventory/Item Definition", fileName = "InventoryItem")]
    public sealed class InventoryItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName;
        [SerializeField] private string itemId;
        [SerializeField] private InventoryItemCategory category;
        [SerializeField, TextArea(1, 2)] private string temporaryIconLabel;
        [SerializeField] private Sprite thumbnail;
        public Sprite Thumbnail => thumbnail;

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

        [Header("Item Action")]
        [SerializeField] private InventoryItemUseKind useKind;
        [SerializeField] private InventoryHoldStyle holdStyle;
        [SerializeField] private InventoryHoldPose holdPose;
        [SerializeField, Min(0f)] private float useDuration;
        [SerializeField, Min(0f)] private float useCooldown;

        [Header("Ammunition")]
        [SerializeField] private string compatibleAmmoItemId;
        [SerializeField] private byte ammunitionCapacity;
        [SerializeField] private byte initialLoadedAmmunition;

        [Header("Audio")]
        [SerializeField] private AudioClip useAudioClip;
        [SerializeField] private AudioClip dryFireAudioClip;
        [SerializeField] private AudioClip impactAudioClip;
        [SerializeField] private AudioClip reloadAudioClip;

        [Header("Combat and Handling")]
        [SerializeField] private ItemCombatSettings combatSettings;
        [SerializeField, Min(0.1f)] private float range = 65f;
        [SerializeField, Range(0f, 35f)] private float hipSpreadDegrees = 12f;
        [SerializeField, Range(0f, 5f)] private float adsSpreadDegrees = 0.3f;
        [SerializeField, Min(0.1f)] private float reloadDuration = 2.5f;
        [SerializeField] private Vector3 muzzleOffset = new(0.12f, -0.12f, 0.5f);
        [SerializeField] private Vector3 adsMuzzleOffset = new(0, -0.02f, 0.5f);
        [SerializeField] private AudioClip equipAudioClip;
        [SerializeField] private AudioClip pickupAudioClip;
        [SerializeField] private AudioClip reloadInsertAudioClip;
        [SerializeField] private AudioClip reloadCloseAudioClip;
        [SerializeField] private AudioClip completionAudioClip;

        public ItemCombatSettings CombatSettings => combatSettings;
        public float Range => Mathf.Max(0.1f, range);
        public float HipSpreadDegrees => hipSpreadDegrees;
        public float AdsSpreadDegrees => adsSpreadDegrees;
        public float ReloadDuration => Mathf.Max(0.1f, reloadDuration);
        public Vector3 MuzzleOffset => muzzleOffset;
        public Vector3 AdsMuzzleOffset => adsMuzzleOffset;
        public AudioClip EquipAudioClip => equipAudioClip;
        public AudioClip PickupAudioClip => pickupAudioClip;
        public AudioClip ReloadInsertAudioClip => reloadInsertAudioClip;
        public AudioClip ReloadCloseAudioClip => reloadCloseAudioClip;
        public AudioClip CompletionAudioClip => completionAudioClip;

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
        public InventoryItemUseKind UseKind => useKind;
        public InventoryHoldStyle HoldStyle => holdStyle;
        public InventoryHoldPose HoldPose => holdPose;
        public float UseDuration => Mathf.Max(0f, useDuration);
        public float UseCooldown => Mathf.Max(0f, useCooldown);
        public string CompatibleAmmoItemId => compatibleAmmoItemId;
        public byte AmmunitionCapacity => ammunitionCapacity;
        public byte InitialLoadedAmmunition =>
            (byte)Mathf.Min(initialLoadedAmmunition, ammunitionCapacity);
        public AudioClip UseAudioClip => useAudioClip;
        public AudioClip DryFireAudioClip => dryFireAudioClip;
        public AudioClip ImpactAudioClip => impactAudioClip;
        public AudioClip ReloadAudioClip => reloadAudioClip;

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            itemId = NetworkLockGroup.NormalizeId(itemId);
            displayName = displayName?.Trim();
            compatibleAmmoItemId = NetworkLockGroup.NormalizeId(
                compatibleAmmoItemId);
            useDuration = Mathf.Max(0f, useDuration);
            useCooldown = Mathf.Max(0f, useCooldown);
            initialLoadedAmmunition = (byte)Mathf.Min(
                initialLoadedAmmunition,
                ammunitionCapacity);

            if (category != InventoryItemCategory.Firearm)
            {
                compatibleAmmoItemId = string.Empty;
                ammunitionCapacity = 0;
                initialLoadedAmmunition = 0;
            }
        }
    }
}
