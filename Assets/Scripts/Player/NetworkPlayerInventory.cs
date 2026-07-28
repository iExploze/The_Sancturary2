using System.Text;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// Small per-player replicated key inventory. Only state authority may insert or remove items.
    /// The owner-only Tab panel is intentionally local UI.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerInventory : NetworkBehaviour
    {
        public const int MaximumItems = 8;

        [Networked] public byte ItemCount { get; private set; }
        [Networked] public ushort Revision { get; private set; }
        [Networked, Capacity(MaximumItems)] private NetworkArray<NetworkString<_32>> ItemIds => default;
        [Networked, Capacity(MaximumItems)] private NetworkArray<NetworkString<_32>> ItemDisplayNames => default;

        private readonly StringBuilder _inventoryTextBuilder = new(256);
        private PlayerInput _playerInput;
        private LocalInteractionTargeting _targeting;
        private InputAction _inventoryAction;
        private GameObject _inventoryPanel;
        private Text _inventoryText;
        private ushort _renderedRevision;
        private bool _ownerInitialized;

        public bool IsMenuOpen => _inventoryPanel != null && _inventoryPanel.activeSelf;

        public void InitializeOwner(PlayerInput playerInput, LocalInteractionTargeting targeting)
        {
            if (!HasInputAuthority)
                return;

            _playerInput = playerInput;
            _targeting = targeting;
            if (GetComponent<TheSancturary.Inventory.PlayerInventory>() != null)
                return;

            _inventoryAction = playerInput.actions.FindActionMap("Player", true).FindAction("Inventory", true);
            EnsureInventoryPanel();
            SetMenuOpen(false);
            _ownerInitialized = true;
        }

        private void Update()
        {
            if (!_ownerInitialized || !HasInputAuthority)
                return;

            if (_inventoryAction != null && _inventoryAction.WasPressedThisFrame())
                SetMenuOpen(!IsMenuOpen);

            if (IsMenuOpen && _renderedRevision != Revision)
                RefreshInventoryText();
        }

        private void OnDisable()
        {
            if (_ownerInitialized)
                SetMenuOpen(false);
        }

        public bool HasItem(string itemId)
        {
            string normalizedId = NetworkLockGroup.NormalizeId(itemId);
            if (string.IsNullOrEmpty(normalizedId))
                return false;

            int count = Mathf.Min(ItemCount, MaximumItems);
            for (int i = 0; i < count; i++)
            {
                if (ItemIds.Get(i) == normalizedId)
                    return true;
            }

            return false;
        }

        public bool CanAcceptItem(string itemId)
        {
            string normalizedId = NetworkLockGroup.NormalizeId(itemId);
            return !string.IsNullOrEmpty(normalizedId) &&
                   normalizedId.Length <= 31 &&
                   ItemCount < MaximumItems &&
                   !HasItem(normalizedId);
        }

        public bool TryAddItemAuthoritative(string itemId, string displayName)
        {
            if (!HasStateAuthority)
                return false;

            string normalizedId = NetworkLockGroup.NormalizeId(itemId);
            string normalizedDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? normalizedId
                : displayName.Trim();
            if (string.IsNullOrEmpty(normalizedId) ||
                normalizedId.Length > 31 ||
                normalizedDisplayName.Length > 31 ||
                !CanAcceptItem(normalizedId))
                return false;

            int index = ItemCount;
            ItemIds.Set(index, normalizedId);
            ItemDisplayNames.Set(index, normalizedDisplayName);
            ItemCount++;
            Revision++;
            return true;
        }

        public bool TryRemoveItemAuthoritative(string itemId)
        {
            if (!HasStateAuthority)
                return false;

            string normalizedId = NetworkLockGroup.NormalizeId(itemId);
            int count = Mathf.Min(ItemCount, MaximumItems);
            int foundIndex = -1;
            for (int i = 0; i < count; i++)
            {
                if (ItemIds.Get(i) == normalizedId)
                {
                    foundIndex = i;
                    break;
                }
            }

            if (foundIndex < 0)
                return false;

            for (int i = foundIndex; i < count - 1; i++)
            {
                ItemIds.Set(i, ItemIds.Get(i + 1));
                ItemDisplayNames.Set(i, ItemDisplayNames.Get(i + 1));
            }

            ItemIds.Set(count - 1, default);
            ItemDisplayNames.Set(count - 1, default);
            ItemCount--;
            Revision++;
            return true;
        }

        public string GetDisplayName(int index)
        {
            return index >= 0 && index < ItemCount
                ? ItemDisplayNames.Get(index).ToString()
                : string.Empty;
        }

        private void SetMenuOpen(bool open)
        {
            EnsureInventoryPanel();
            _inventoryPanel.SetActive(open);
            _targeting?.SetInputCaptured(open);

            if (open)
            {
                RefreshInventoryText();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (HasInputAuthority)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void EnsureInventoryPanel()
        {
            if (_inventoryPanel != null)
                return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new("Local Inventory", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            _inventoryPanel = new GameObject("Inventory Panel", typeof(RectTransform), typeof(Image));
            _inventoryPanel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = _inventoryPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -24f);
            panelRect.sizeDelta = new Vector2(280f, 250f);
            Image panelImage = _inventoryPanel.GetComponent<Image>();
            panelImage.color = new Color(0.035f, 0.035f, 0.04f, 0.92f);
            panelImage.raycastTarget = true;

            GameObject textObject = new("Items", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(_inventoryPanel.transform, false);
            _inventoryText = textObject.GetComponent<Text>();
            _inventoryText.font = font;
            _inventoryText.fontSize = 16;
            _inventoryText.alignment = TextAnchor.UpperLeft;
            _inventoryText.color = Color.white;
            _inventoryText.raycastTarget = false;
            RectTransform textRect = _inventoryText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 18f);
            textRect.offsetMax = new Vector2(-18f, -18f);
        }

        private void RefreshInventoryText()
        {
            _inventoryTextBuilder.Clear();
            _inventoryTextBuilder.AppendLine("KEYS");
            _inventoryTextBuilder.AppendLine();
            int count = Mathf.Min(ItemCount, MaximumItems);
            if (count == 0)
            {
                _inventoryTextBuilder.Append("Empty");
            }
            else
            {
                for (int i = 0; i < count; i++)
                    _inventoryTextBuilder.Append("\u2022 ").AppendLine(GetDisplayName(i));
            }

            _inventoryTextBuilder.AppendLine().AppendLine();
            _inventoryTextBuilder.Append("Tab \u2014 Close");
            _inventoryText.text = _inventoryTextBuilder.ToString();
            _renderedRevision = Revision;
        }
    }
}
