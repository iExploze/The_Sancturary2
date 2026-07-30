using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheSancturary.Inventory;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// Finds the local player's current interaction target and renders a minimal local-only prompt.
    /// The first collider hit by the ray must resolve to an InteractionTarget on itself or a parent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalInteractionTargeting : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 6f;
        [SerializeField] private LayerMask interactionRaycastMask = ~0;

        [Header("Prompt")]
        [SerializeField, Min(1f)] private float dotSize = 5f;
        [SerializeField, Min(1f)] private int displayNameFontSize = 16;
        [SerializeField, Min(1f)] private int actionFontSize = 14;

        private Camera _playerCamera;
        private PlayerInput _playerInput;
        private FusionNetworkPlayer _requestingPlayer;
        private NetworkPlayerInventory _inventory;
        private InputAction _interactAction;
        private GameObject _promptRoot;
        private Text _displayNameText;
        private Text _actionText;
        private InteractionTarget _shownTarget;
        private WorldInventoryItem _shownWorldItem;
        private string _shownDisplayName;
        private string _shownActionText;
        private bool _inputCaptured;

        public float InteractionDistance => interactionDistance;
        public LayerMask InteractionRaycastMask => interactionRaycastMask;
        public InteractionTarget CurrentTarget => _shownTarget;

        public void Initialize(
            Camera playerCamera,
            PlayerInput playerInput,
            FusionNetworkPlayer requestingPlayer,
            NetworkPlayerInventory inventory)
        {
            _playerCamera = playerCamera;
            _playerInput = playerInput;
            _requestingPlayer = requestingPlayer;
            _inventory = inventory;
            _interactAction = playerInput.actions.FindActionMap("Player", true).FindAction("Interact", true);
            EnsurePrompt();
        }

        /// <summary>
        /// Lets inventory or other modal UI immediately suppress this local prompt.
        /// </summary>
        public void SetInputCaptured(bool value)
        {
            _inputCaptured = value;
            if (value)
                HidePrompt();
        }

        private void OnDisable()
        {
            HidePrompt();
        }

        private void Update()
        {
            if (!CanShowPrompt())
            {
                HidePrompt();
                return;
            }

            Ray ray = new(_playerCamera.transform.position, _playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionRaycastMask, QueryTriggerInteraction.Ignore))
            {
                HidePrompt();
                return;
            }

            InteractionTarget target = hit.collider.GetComponentInParent<InteractionTarget>();
            if (target == null || !target.TryGetPrompt(_inventory, out InteractionPrompt prompt))
            {
                HidePrompt();
                return;
            }

            ShowPrompt(target, prompt);
            if (_interactAction != null &&
                _interactAction.WasPressedThisFrame() &&
                target.RequestInteraction(_requestingPlayer))
                HidePrompt();
        }

        private bool CanShowPrompt()
        {
            if (_inputCaptured || _playerCamera == null || !_playerCamera.enabled || _playerInput == null || !_playerInput.enabled || !_playerInput.inputIsActive)
                return false;

            if (Cursor.lockState != CursorLockMode.Locked)
                return false;

            return EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null;
        }

        private void EnsurePrompt()
        {
            if (_promptRoot != null)
                return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new("Local Interaction Prompt", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            _promptRoot = new GameObject("Prompt", typeof(RectTransform));
            _promptRoot.transform.SetParent(canvasObject.transform, false);
            RectTransform promptRect = _promptRoot.GetComponent<RectTransform>();
            promptRect.anchorMin = Vector2.zero;
            promptRect.anchorMax = Vector2.one;
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;

            Image dot = CreateImage("Dot", _promptRoot.transform, Color.white);
            RectTransform dotRect = dot.rectTransform;
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = Vector2.one * dotSize;
            dot.raycastTarget = false;

            _displayNameText = CreateText("Display Name", _promptRoot.transform, font, displayNameFontSize, new Vector2(0f, -13f));
            _actionText = CreateText("Action", _promptRoot.transform, font, actionFontSize, new Vector2(0f, -37f));
            _promptRoot.SetActive(false);
        }

        private void ShowPrompt(InteractionTarget target, InteractionPrompt prompt)
        {
            EnsurePrompt();
            SetTargetedWorldItem(target.GetComponentInParent<WorldInventoryItem>());
            if (_shownTarget != target || _shownDisplayName != prompt.DisplayName)
            {
                _displayNameText.text = prompt.DisplayName;
                _shownDisplayName = prompt.DisplayName;
            }

            if (_shownTarget != target || _shownActionText != prompt.ActionText)
            {
                _actionText.text = prompt.ActionText;
                _shownActionText = prompt.ActionText;
            }

            _shownTarget = target;
            if (!_promptRoot.activeSelf)
                _promptRoot.SetActive(true);
        }

        private void HidePrompt()
        {
            SetTargetedWorldItem(null);
            if (_promptRoot != null && _promptRoot.activeSelf)
                _promptRoot.SetActive(false);

            _shownTarget = null;
            _shownDisplayName = null;
            _shownActionText = null;
        }

        private void SetTargetedWorldItem(WorldInventoryItem worldItem)
        {
            if (_shownWorldItem == worldItem)
                return;

            _shownWorldItem?.SetTargeted(false);
            _shownWorldItem = worldItem;
            _shownWorldItem?.SetTargeted(true);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, Vector2 anchoredPosition)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(440f, 24f);
            return text;
        }
    }
}
