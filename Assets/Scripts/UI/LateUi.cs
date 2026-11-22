using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// Shared widget factory for the 后 24H chapters. Used both by the editor scene builders and by
    /// runtime panels, so authored and generated UI always share one look.
    /// </summary>
    public static class LateUi
    {
        public static readonly Color Ink = new Color(0.025f, 0.039f, 0.057f, 0.95f);
        public static readonly Color InkLight = new Color(0.05f, 0.09f, 0.12f, 0.92f);
        public static readonly Color Paper = new Color(0.88f, 0.92f, 0.91f);
        public static readonly Color PaperDim = new Color(0.7f, 0.75f, 0.75f);
        public static readonly Color Accent = new Color(0.57f, 0.84f, 0.78f);
        public static readonly Color Warm = new Color(0.93f, 0.79f, 0.55f);

        public static readonly Color WindowInk = new Color(0.09f, 0.035f, 0.03f);
        public static readonly Color WindowWarning = new Color(0.24f, 0.045f, 0.035f);

        private static Font font;
        private static Sprite buttonSprite;

        public static Sprite ButtonSprite => buttonSprite != null ? buttonSprite :
            (buttonSprite = Resources.Load<Sprite>("S0/选项按钮"));

        public static void ApplyButtonSprite(Image image)
        {
            image.sprite = ButtonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                Text existing = Object.FindFirstObjectByType<Text>(FindObjectsInactive.Include);
                if (existing != null && existing.font != null) return font = existing.font;
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static GameObject Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return go;
        }

        public static GameObject Panel(Transform parent, string name, float x, float y, float w, float h,
            Color color, bool raycast = true)
        {
            GameObject go = Rect(parent, name, x, y, w, h);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return go;
        }

        public static Text Label(Transform parent, string name, string value, float x, float y, float w, float h,
            int size, Color color, TextAnchor anchor)
        {
            GameObject go = Rect(parent, name, x, y, w, h);
            Text label = go.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.text = value;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(Transform parent, string name, string caption, float x, float y, float w, float h,
            int fontSize = 29)
        {
            GameObject go = Panel(parent, name, x, y, w, h, Color.white);
            Button button = go.AddComponent<Button>();
            Image image = go.GetComponent<Image>();
            ApplyButtonSprite(image);
            button.targetGraphic = image;
            Label(go.transform, "Caption", caption, 8, 4, w - 16, h - 8, fontSize, Paper, TextAnchor.MiddleCenter);
            return button;
        }

        public static Text Caption(Button button)
        {
            if (button == null) return null;
            Transform caption = button.transform.Find("Caption");
            return caption != null ? caption.GetComponent<Text>() : button.GetComponentInChildren<Text>(true);
        }

        public static void SetCaption(Button button, string text)
        {
            Text label = Caption(button);
            if (label != null) label.text = text;
        }

        public static void SetActive(Button button, bool active)
        {
            if (button != null) button.gameObject.SetActive(active);
        }

        public static Text Row(Transform parent, string name, string value, float x, float y, float w, float h,
            int size = 30, Color? color = null)
        {
            return Label(parent, name, value, x, y, w, h, size, color ?? Paper, TextAnchor.MiddleLeft);
        }
    }
}
