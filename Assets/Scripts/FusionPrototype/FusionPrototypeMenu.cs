using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TheSancturary.FusionPrototype
{
    public sealed class FusionPrototypeMenu : MonoBehaviour
    {
        [SerializeField] private NetworkObject canonicalPlayerPrefab;

        private InputField _sessionInput;
        private Text _statusText;
        private Button _hostButton;
        private Button _joinButton;
        private FusionSessionManager _sessionManager;

        private void Awake()
        {
            EnsureEventSystem();
            BuildInterface();
            _sessionManager = FusionSessionManager.GetOrCreate(canonicalPlayerPrefab);
            _sessionManager.StatusChanged += OnStatusChanged;

            if (!FusionSessionManager.HasFusionAppId)
                OnStatusChanged("Photon Fusion App ID is missing. Configure it in Tools > Fusion > Fusion Hub.", true);
            else
                OnStatusChanged("Enter a session name, then Host or Join.", false);
        }

        private async void Host()
        {
            SetButtonsInteractable(false);
            if (!await _sessionManager.StartHostAsync(_sessionInput.text))
                SetButtonsInteractable(true);
        }

        private async void Join()
        {
            SetButtonsInteractable(false);
            if (!await _sessionManager.StartClientAsync(_sessionInput.text))
                SetButtonsInteractable(true);
        }

        private void OnStatusChanged(string message, bool isError)
        {
            if (_statusText == null)
                return;
            _statusText.text = message;
            _statusText.color = isError ? new Color(1f, 0.35f, 0.32f) : new Color(0.8f, 0.86f, 0.9f);
        }

        private void SetButtonsInteractable(bool value)
        {
            _hostButton.interactable = value;
            _joinButton.interactable = value;
        }

        private void BuildInterface()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new("Fusion Prototype Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            Image backdrop = CreateImage("Backdrop", canvasObject.transform, new Color(0.025f, 0.03f, 0.035f, 1f));
            Stretch(backdrop.rectTransform);

            Image panel = CreateImage("Panel", canvasObject.transform, new Color(0.075f, 0.085f, 0.095f, 0.98f));
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 420f), Vector2.zero);

            Text title = CreateText("Title", panel.transform, font, "THE SANCTURARY\nFUSION PROTOTYPE", 30, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(500f, 90f), new Vector2(0f, -62f));
            title.color = new Color(0.9f, 0.92f, 0.9f);

            Text label = CreateText("Session Label", panel.transform, font, "Session / room name", 18, TextAnchor.MiddleLeft);
            SetRect(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(440f, 32f), new Vector2(0f, -142f));

            _sessionInput = CreateInputField(panel.transform, font);
            SetRect(_sessionInput.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(440f, 48f), new Vector2(0f, -186f));
            _sessionInput.text = "sancturary-prototype";

            _hostButton = CreateButton(panel.transform, font, "Host / Create", new Color(0.18f, 0.34f, 0.28f));
            SetRect(_hostButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(210f, 54f), new Vector2(-115f, -258f));
            _hostButton.onClick.AddListener(Host);

            _joinButton = CreateButton(panel.transform, font, "Join", new Color(0.22f, 0.28f, 0.38f));
            SetRect(_joinButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(210f, 54f), new Vector2(115f, -258f));
            _joinButton.onClick.AddListener(Join);

            _statusText = CreateText("Status", panel.transform, font, string.Empty, 16, TextAnchor.UpperCenter);
            SetRect(_statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(480f, 78f), new Vector2(0f, 58f));

            Text hint = CreateText("Hint", panel.transform, font, "1-4 players | Host can play alone | no lobby browser", 14, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(480f, 30f), new Vector2(0f, 24f));
            hint.color = new Color(0.5f, 0.55f, 0.58f);
        }

        private static InputField CreateInputField(Transform parent, Font font)
        {
            Image image = CreateImage("Session Input", parent, new Color(0.12f, 0.135f, 0.15f));
            InputField field = image.gameObject.AddComponent<InputField>();

            Text text = CreateText("Text", image.transform, font, string.Empty, 18, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, new Vector2(14f, 4f), new Vector2(-14f, -4f));
            Text placeholder = CreateText("Placeholder", image.transform, font, "sancturary-prototype", 18, TextAnchor.MiddleLeft);
            Stretch(placeholder.rectTransform, new Vector2(14f, 4f), new Vector2(-14f, -4f));
            placeholder.color = new Color(0.45f, 0.48f, 0.5f);

            field.textComponent = text;
            field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            field.characterLimit = 32;
            return field;
        }

        private static Button CreateButton(Transform parent, Font font, string label, Color color)
        {
            Image image = CreateImage(label, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = color * 1.18f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;
            Text text = CreateText("Label", image.transform, font, label, 19, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Font font, string value, int size, TextAnchor alignment)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void OnDestroy()
        {
            if (_sessionManager != null)
                _sessionManager.StatusChanged -= OnStatusChanged;
        }
    }
}
