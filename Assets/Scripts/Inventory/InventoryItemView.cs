using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheSancturary.Inventory
{
    [DisallowMultipleComponent]
    public sealed class InventoryItemView : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private Image _background;
        private Outline _outline;
        private Text _label;
        private Text _equippedIndicator;
        private CanvasGroup _canvasGroup;
        private InventoryItemInstance _instance;
        private Action<InventoryItemView, PointerEventData> _beginDrag;
        private Action<InventoryItemView, PointerEventData> _drag;
        private Action<InventoryItemView, PointerEventData> _endDrag;
        private Action<InventoryItemInstance> _clicked;
        private bool _selected;
        private bool _focused;
        private bool _hovered;
        private bool _dragging;

        public InventoryItemInstance Instance => _instance;
        public RectTransform RectTransform => (RectTransform)transform;

        public void Configure(
            InventoryItemInstance instance,
            Font font,
            bool selected,
            bool focused,
            bool equipped,
            Action<InventoryItemView, PointerEventData> beginDrag,
            Action<InventoryItemView, PointerEventData> drag,
            Action<InventoryItemView, PointerEventData> endDrag,
            Action<InventoryItemInstance> clicked)
        {
            _instance = instance;
            _selected = selected;
            _focused = focused;
            _beginDrag = beginDrag;
            _drag = drag;
            _endDrag = endDrag;
            _clicked = clicked;
            _background = GetComponent<Image>();
            _outline = GetComponent<Outline>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _background.color = GetCategoryColor(instance.Definition.Category);
            ApplyOutline();

            _label = CreateText("Icon Label", transform, font, 18, TextAnchor.MiddleCenter);
            _label.text = string.IsNullOrWhiteSpace(instance.Definition.TemporaryIconLabel)
                ? instance.Definition.DisplayName
                : instance.Definition.TemporaryIconLabel;
            Stretch(_label.rectTransform, 6f);
            if (instance.Definition.Thumbnail != null)
            {
                var iconObject = new GameObject("Item thumbnail", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(transform, false);
                var icon = iconObject.GetComponent<Image>();
                icon.sprite = instance.Definition.Thumbnail;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Stretch(icon.rectTransform, 5f);
                _label.text = string.Empty;
            }

            _equippedIndicator = CreateText("Equipped", transform, font, 11, TextAnchor.LowerRight);
            _equippedIndicator.text = equipped ? "EQUIPPED" : string.Empty;
            Stretch(_equippedIndicator.rectTransform, 6f);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _dragging = true;
            SetDraggingVisual(true);
            _beginDrag?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging)
                _drag?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _dragging = false;
            _endDrag?.Invoke(this, eventData);
            SetDraggingVisual(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_dragging && eventData.button == PointerEventData.InputButton.Left)
                _clicked?.Invoke(_instance);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyOutline();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyOutline();
        }

        private void SetDraggingVisual(bool dragging)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = dragging ? 0.82f : 1f;
            _canvasGroup.blocksRaycasts = !dragging;
        }

        private void ApplyOutline()
        {
            if (_outline == null)
                return;

            if (_selected)
            {
                _outline.effectColor = new Color(1f, 0.78f, 0.2f, 1f);
                _outline.effectDistance = new Vector2(3f, -3f);
            }
            else if (_focused)
            {
                _outline.effectColor = new Color(0.25f, 0.72f, 0.82f, 1f);
                _outline.effectDistance = new Vector2(2f, -2f);
            }
            else if (_hovered)
            {
                _outline.effectColor = new Color(0.72f, 0.75f, 0.8f, 1f);
                _outline.effectDistance = new Vector2(2f, -2f);
            }
            else
            {
                _outline.effectColor = new Color(0.12f, 0.12f, 0.14f, 1f);
                _outline.effectDistance = new Vector2(1f, -1f);
            }
        }

        private static Color GetCategoryColor(InventoryItemCategory category)
        {
            return category switch
            {
                InventoryItemCategory.Equipment => new Color(0.18f, 0.30f, 0.38f, 0.98f),
                InventoryItemCategory.Firearm => new Color(0.38f, 0.20f, 0.18f, 0.98f),
                InventoryItemCategory.Medicine => new Color(0.18f, 0.36f, 0.24f, 0.98f),
                InventoryItemCategory.Key => new Color(0.38f, 0.32f, 0.14f, 0.98f),
                _ => new Color(0.25f, 0.25f, 0.29f, 0.98f)
            };
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
