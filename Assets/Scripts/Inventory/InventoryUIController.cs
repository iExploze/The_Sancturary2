using TheSancturary.FusionPrototype;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TheSancturary.Inventory
{
    [DisallowMultipleComponent]
    public sealed class InventoryUIController : MonoBehaviour
    {
        private const float CellSize = 92f;
        private const float CellGap = 4f;

        [Header("Input Actions")]
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string inventoryActionName = "Inventory";
        [SerializeField] private string dropActionName = "Drop";

        [Header("Messages")]
        [SerializeField, Min(0.25f)] private float messageDuration = 2f;
        [SerializeField, Range(4, 20)] private int dragThresholdPixels = 8;

        private PlayerInventory _inventory;
        private LocalInteractionTargeting _targeting;
        private InputAction _inventoryAction;
        private InputAction _dropAction;
        private GameObject _panel;
        private RectTransform _itemsRoot;
        private Image _placementPreview;
        private Text _selectedName;
        private Text _messageText;
        private GameObject _messageRoot;
        private Font _font;
        private float _messageHideTime;
        private bool _initialized;
        private InventoryItemView _draggedView;
        private InventoryItemInstance _draggedItem;
        private Vector2 _dragPointerOffset;
        private Vector2Int _dragCandidate;
        private bool _dragCandidateValid;
        private bool _lockerInputLocked;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Initialize(
            PlayerInventory inventory,
            PlayerInput playerInput,
            LocalInteractionTargeting targeting)
        {
            if (_initialized)
                return;

            _inventory = inventory;
            _targeting = targeting;
            InputActionMap map = playerInput != null ? playerInput.actions.FindActionMap(actionMapName, false) : null;
            _inventoryAction = map?.FindAction(inventoryActionName, false);
            _dropAction = map?.FindAction(dropActionName, false);
            EnsureUI();
            _inventory.Changed += Refresh;
            SetOpen(false);
            Refresh();
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            if (!_lockerInputLocked && _inventoryAction != null && _inventoryAction.WasPressedThisFrame())
                SetOpen(!IsOpen);

            bool fallbackDropPressed = _dropAction == null &&
                                       Keyboard.current != null &&
                                       Keyboard.current.gKey.wasPressedThisFrame;
            if (!_lockerInputLocked && IsOpen && ((_dropAction != null && _dropAction.WasPressedThisFrame()) || fallbackDropPressed))
            {
                CancelActiveDrag();
                _inventory.DropSelected();
            }

            if (_messageRoot != null && _messageRoot.activeSelf && Time.unscaledTime >= _messageHideTime)
                _messageRoot.SetActive(false);
        }

        public void ShowMessage(string message)
        {
            EnsureUI();
            _messageText.text = message;
            _messageRoot.SetActive(true);
            _messageHideTime = Time.unscaledTime + messageDuration;
        }

        public void SetLockerInputLocked(bool locked)
        {
            _lockerInputLocked = locked;
            if (locked && IsOpen)
                SetOpen(false);
        }

        public void SetOpen(bool open)
        {
            EnsureUI();
            if (!open)
                CancelActiveDrag();

            _panel.SetActive(open);
            _targeting?.SetInputCaptured(open);

            if (open)
            {
                Refresh();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                EventSystem.current?.SetSelectedGameObject(null);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void EnsureUI()
        {
            if (_panel != null)
                return;

            EnsureEventSystem();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new("Local Grid Inventory", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            _panel = CreateImage("Inventory Panel", canvasObject.transform, new Color(0.025f, 0.025f, 0.03f, 0.96f)).gameObject;
            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 350f);

            Text title = CreateText("Title", _panel.transform, 28, TextAnchor.UpperLeft);
            title.text = "INVENTORY";
            SetRect(title.rectTransform, new Vector2(28f, -22f), new Vector2(640f, 40f));

            RectTransform cellsRoot = CreateRect("Grid Cells", _panel.transform);
            SetRect(cellsRoot, new Vector2(28f, -78f), GetGridSize());
            for (int row = 0; row < PlayerInventory.Rows; row++)
            {
                for (int column = 0; column < PlayerInventory.Columns; column++)
                {
                    Image cell = CreateImage($"Cell {column},{row}", cellsRoot, new Color(0.09f, 0.09f, 0.105f, 1f));
                    RectTransform rect = cell.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(column * (CellSize + CellGap), -row * (CellSize + CellGap));
                    rect.sizeDelta = Vector2.one * CellSize;
                    Outline outline = cell.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.28f, 0.28f, 0.32f, 1f);
                    outline.effectDistance = new Vector2(1f, -1f);
                    cell.raycastTarget = false;
                }
            }

            RectTransform previewRoot = CreateRect("Placement Preview", _panel.transform);
            SetRect(previewRoot, new Vector2(28f, -78f), GetGridSize());
            _placementPreview = CreateImage("Preview", previewRoot, new Color(0.2f, 0.8f, 0.35f, 0.42f));
            _placementPreview.raycastTarget = false;
            _placementPreview.gameObject.SetActive(false);

            _itemsRoot = CreateRect("Items", _panel.transform);
            SetRect(_itemsRoot, new Vector2(28f, -78f), GetGridSize());

            _selectedName = CreateText("Selected Item", _panel.transform, 18, TextAnchor.UpperLeft);
            SetRect(_selectedName.rectTransform, new Vector2(526f, -82f), new Vector2(165f, 88f));

            Button dropButton = CreateButton("Drop Button", _panel.transform, "DROP", new Color(0.35f, 0.13f, 0.13f, 1f));
            RectTransform dropRect = dropButton.GetComponent<RectTransform>();
            SetRect(dropRect, new Vector2(526f, -184f), new Vector2(165f, 44f));
            dropButton.onClick.AddListener(() => _inventory.DropSelected());

            Text hint = CreateText("Hint", _panel.transform, 14, TextAnchor.UpperLeft);
            hint.text = "Click flashlight \u2014 Equip\nDrag item \u2014 Move\nG \u2014 Drop focused\nTab \u2014 Close";
            SetRect(hint.rectTransform, new Vector2(526f, -244f), new Vector2(175f, 86f));

            _messageRoot = CreateImage("Inventory Message", canvasObject.transform, new Color(0.12f, 0.025f, 0.025f, 0.94f)).gameObject;
            RectTransform messageRect = _messageRoot.GetComponent<RectTransform>();
            messageRect.anchorMin = messageRect.anchorMax = new Vector2(0.5f, 0.82f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.sizeDelta = new Vector2(300f, 48f);
            _messageText = CreateText("Message Text", _messageRoot.transform, 20, TextAnchor.MiddleCenter);
            Stretch(_messageText.rectTransform, 8f);
            _messageRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (_inventory == null || _itemsRoot == null)
                return;

            for (int index = _itemsRoot.childCount - 1; index >= 0; index--)
            {
                _itemsRoot.GetChild(index).gameObject.SetActive(false);
                Destroy(_itemsRoot.GetChild(index).gameObject);
            }

            foreach (InventoryItemInstance item in _inventory.Items)
            {
                GameObject viewObject = new(
                    item.Definition.DisplayName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Outline),
                    typeof(CanvasGroup),
                    typeof(InventoryItemView));
                viewObject.transform.SetParent(_itemsRoot, false);
                RectTransform rect = viewObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(
                    item.GridPosition.x * (CellSize + CellGap),
                    -item.GridPosition.y * (CellSize + CellGap));
                rect.sizeDelta = new Vector2(
                    item.Width * CellSize + (item.Width - 1) * CellGap,
                    item.Height * CellSize + (item.Height - 1) * CellGap);

                InventoryItemView view = viewObject.GetComponent<InventoryItemView>();
                view.Configure(
                    item,
                    _font,
                    item.Definition.CanEquip && item == _inventory.SelectedItem,
                    item == _inventory.FocusedItem && item != _inventory.SelectedItem,
                    item == _inventory.EquippedItem,
                    BeginDrag,
                    Drag,
                    EndDrag,
                    _inventory.ActivateItem);
            }

            InventoryItemInstance focused = _inventory.FocusedItem;
            _selectedName.text = focused == null
                ? "No item focused"
                : $"{focused.Definition.DisplayName}\n{focused.Width}\u00d7{focused.Height}  {focused.Definition.Category}";
        }

        private void BeginDrag(InventoryItemView view, PointerEventData eventData)
        {
            if (_draggedItem != null || !_inventory.BeginMove(view.Instance))
                return;

            _draggedView = view;
            _draggedItem = view.Instance;
            view.transform.SetAsLastSibling();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _itemsRoot,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointer))
                _dragPointerOffset = view.RectTransform.anchoredPosition - localPointer;
            else
                _dragPointerOffset = Vector2.zero;

            UpdateDrag(eventData);
        }

        private void Drag(InventoryItemView view, PointerEventData eventData)
        {
            if (view != _draggedView)
                return;

            UpdateDrag(eventData);
        }

        private void EndDrag(InventoryItemView view, PointerEventData eventData)
        {
            if (view != _draggedView || _draggedItem == null)
                return;

            InventoryItemInstance movedItem = _draggedItem;
            bool committed = _dragCandidateValid && _inventory.TryMove(movedItem, _dragCandidate);
            if (!committed)
                _inventory.CancelMove(movedItem);
            ClearDragState();
        }

        private void UpdateDrag(PointerEventData eventData)
        {
            if (_draggedView == null || _draggedItem == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _itemsRoot,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointer))
                return;

            Vector2 draggedPosition = localPointer + _dragPointerOffset;
            _draggedView.RectTransform.anchoredPosition = draggedPosition;
            float stride = CellSize + CellGap;
            _dragCandidate = new Vector2Int(
                Mathf.RoundToInt(draggedPosition.x / stride),
                Mathf.RoundToInt(-draggedPosition.y / stride));
            _dragCandidateValid = _inventory.CanPlace(
                _draggedItem.Definition,
                _dragCandidate,
                _draggedItem.Rotated);

            RectTransform previewRect = _placementPreview.rectTransform;
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0f, 1f);
            previewRect.pivot = new Vector2(0f, 1f);
            previewRect.anchoredPosition = new Vector2(
                _dragCandidate.x * stride,
                -_dragCandidate.y * stride);
            previewRect.sizeDelta = new Vector2(
                _draggedItem.Width * CellSize + (_draggedItem.Width - 1) * CellGap,
                _draggedItem.Height * CellSize + (_draggedItem.Height - 1) * CellGap);
            _placementPreview.color = _dragCandidateValid
                ? new Color(0.18f, 0.82f, 0.35f, 0.42f)
                : new Color(0.9f, 0.2f, 0.2f, 0.42f);
            _placementPreview.gameObject.SetActive(true);
        }

        private void CancelActiveDrag()
        {
            if (_draggedItem != null)
                _inventory?.CancelMove(_draggedItem);
            ClearDragState();
        }

        private void ClearDragState()
        {
            if (_placementPreview != null)
                _placementPreview.gameObject.SetActive(false);
            _draggedView = null;
            _draggedItem = null;
            _dragCandidateValid = false;
        }

        private Button CreateButton(string name, Transform parent, string label, Color color)
        {
            Image image = CreateImage(name, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Label", image.transform, 17, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform, 4f);
            return button;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.pixelDragThreshold = dragThresholdPixels;
                return;
            }

            GameObject eventSystemObject = new("Local UI EventSystem", typeof(EventSystem));
            eventSystemObject.GetComponent<EventSystem>().pixelDragThreshold = dragThresholdPixels;
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Vector2 GetGridSize()
        {
            return new Vector2(
                PlayerInventory.Columns * CellSize + (PlayerInventory.Columns - 1) * CellGap,
                PlayerInventory.Rows * CellSize + (PlayerInventory.Rows - 1) * CellGap);
        }

        private void OnDisable()
        {
            if (_inventory != null)
                _inventory.Changed -= Refresh;
            if (_panel != null)
                SetOpen(false);
        }
    }
}
