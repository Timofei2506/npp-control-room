using System;
using NPP.Room;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NPP.UI
{
    /// <summary>Минималистичный набор UGUI-элементов, создаваемых кодом.</summary>
    public static class UiKit
    {
        public static Font Font { get { return Bld.UiFont; } }

        public static Canvas MakeCanvas(string name, int order)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.55f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                         Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform FullScreen(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return rt;
        }

        public static RectTransform Panel(Transform parent, Vector2 center, Vector2 size, Color color, string name = "Panel")
        {
            var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), center, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static Text Label(Transform parent, string text, int fontSize, Color color, TextAnchor anchor,
                                 Vector2 pos, Vector2 size)
        {
            var rt = Rect(parent, "Text", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, Vector2 pos, Vector2 size, Action onClick,
                                    Color? bg = null, int fontSize = 26)
        {
            var rt = Rect(parent, "Button_" + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg ?? new Color(0.09f, 0.12f, 0.14f, 0.96f);
            var btn = rt.gameObject.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 1f);
            cb.highlightedColor = new Color(0.78f, 0.88f, 1f, 1f);
            cb.pressedColor = new Color(0.55f, 0.62f, 0.7f, 1f);
            cb.selectedColor = cb.normalColor;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var t = Label(rt, label, fontSize, new Color(0.9f, 0.93f, 0.95f), TextAnchor.MiddleCenter, Vector2.zero, size);
            t.raycastTarget = false;

            // тонкая рамка
            var border = rt.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.35f, 0.45f, 0.5f, 0.35f);
            border.effectDistance = new Vector2(1.5f, -1.5f);
            return btn;
        }

        public static InputField Input(Transform parent, string placeholder, Vector2 pos, Vector2 size,
                                       string initial = "", int fontSize = 22, int charLimit = 24)
        {
            var rt = Rect(parent, "Input", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.05f, 0.065f, 0.075f, 0.97f);

            var field = rt.gameObject.AddComponent<InputField>();
            field.characterLimit = charLimit;
            field.lineType = InputField.LineType.SingleLine;

            var textRt = Rect(rt, "Text", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(10f, 0f), Vector2.zero);
            textRt.offsetMin = new Vector2(12f, 2f);
            textRt.offsetMax = new Vector2(-12f, -2f);
            var text = textRt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = new Color(0.9f, 0.94f, 0.95f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var phRt = Rect(rt, "Placeholder", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(10f, 0f), Vector2.zero);
            phRt.offsetMin = new Vector2(12f, 2f);
            phRt.offsetMax = new Vector2(-12f, -2f);
            var ph = phRt.gameObject.AddComponent<Text>();
            ph.font = Font;
            ph.fontSize = fontSize;
            ph.color = new Color(0.45f, 0.5f, 0.52f);
            ph.alignment = TextAnchor.MiddleLeft;
            ph.text = placeholder;

            field.textComponent = text;
            field.placeholder = ph;
            field.text = initial;
            return field;
        }

        public static Slider Slider(Transform parent, Vector2 pos, Vector2 size, float min, float max,
                                    float value, Action<float> onChange)
        {
            var rt = Rect(parent, "Slider", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), pos, size);
            var slider = rt.gameObject.AddComponent<Slider>();

            var bg = Rect(rt, "Back", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bg.offsetMin = new Vector2(0f, size.y * 0.38f);
            bg.offsetMax = new Vector2(0f, -size.y * 0.38f);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.08f, 0.09f);

            var fillArea = Rect(rt, "FillArea", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fillArea.offsetMin = new Vector2(2f, size.y * 0.38f);
            fillArea.offsetMax = new Vector2(-2f, -size.y * 0.38f);
            var fill = Rect(fillArea, "Fill", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0.35f, 0.62f, 0.75f);

            var handleArea = Rect(rt, "HandleArea", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);
            var handle = Rect(handleArea, "Handle", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 0f));
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = new Color(0.8f, 0.85f, 0.88f);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }
    }
}
