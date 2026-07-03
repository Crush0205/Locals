using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mistforge
{
    /// Programmatic UI construction helpers. The vertical slice builds all HUD
    /// and menu chrome in code so placeholder layouts stay in sync with logic;
    /// once final UI art lands these can be replaced by authored prefabs.
    public static class UIFactory
    {
        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image.rectTransform;
        }

        public static RectTransform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        public static TextMeshProUGUI Text(Transform parent, string content, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.color = color ?? Color.white;
            text.enableWordWrapping = true;
            return text;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick,
            Color? background = null, float fontSize = 20f)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = background ?? new Color(0.22f, 0.24f, 0.3f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);

            var text = Text(go.transform, label, fontSize);
            Stretch(text.rectTransform);
            return button;
        }

        /// Anchor a RectTransform by fractional anchors + pixel offsets.
        public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void Stretch(RectTransform rect)
        {
            Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
    }
}
