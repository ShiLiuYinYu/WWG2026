using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WWG.EditorTools
{
    /// <summary>
    /// 生成全游戏共用的设置窗口预制体（Resources/UI/SettingsPanel），
    /// 并清掉旧暂停菜单里内嵌的那个同名小面板。
    /// 尺寸与排版按 1920×1080 参考分辨率：窗口 1040×760，标题在上，
    /// 音乐、音效两条滑条居中，关闭按钮在底部。
    /// </summary>
    public static class SettingsPanelBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/SettingsPanel.prefab";
        private const string OverlayPath = "Assets/Prefabs/UI/GameplayOverlay.prefab";
        private const string FramePath = "Assets/Resources/UI/选项框.png";
        private const string ButtonPath = "Assets/Resources/S0/选项按钮.png";

        private static readonly Vector2 Size = new Vector2(1040f, 760f);
        private static readonly Color Ink = new Color(0.09f, 0.035f, 0.03f, 1f);
        private static readonly Color Caption = new Color(0.97f, 0.92f, 0.85f, 1f);
        private static readonly Color TrackColor = new Color(0.30f, 0.11f, 0.09f, 0.42f);
        private static readonly Color FillColor = new Color(0.60f, 0.19f, 0.14f, 1f);
        private static readonly Color KnobColor = new Color(0.99f, 0.95f, 0.88f, 1f);

        private static Font font;
        private static Sprite trackSprite;
        private static Sprite knobSprite;

        [MenuItem("WWG/UI/Build Settings Window")]
        public static void Build()
        {
            Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
            Sprite button = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
            if (frame == null || button == null)
            {
                Debug.LogError("[SettingsPanel] 缺少图片：" + FramePath + " / " + ButtonPath);
                return;
            }

            font = WWG.WwgFonts.Body;
            trackSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var rootGo = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image), typeof(SettingsPanelController));
            var root = (RectTransform)rootGo.transform;
            root.sizeDelta = Size;

            var bg = rootGo.GetComponent<Image>();
            bg.sprite = frame;
            bg.type = Image.Type.Simple;
            bg.color = Color.white;
            bg.raycastTarget = true;

            // 标题
            Label(root, "Heading", "设置", 0f, 40f, Size.x, 84f, 52, Ink, TextAnchor.MiddleCenter);
            Box(root, "Heading rule", 140f, 136f, 760f, 2f, new Color(Ink.r, Ink.g, Ink.b, 0.28f));

            // 音乐
            Label(root, "Label Music", "音乐", 140f, 214f, 120f, 60f, 38, Ink, TextAnchor.MiddleLeft);
            Slider(root, "Slider Music", 272f, 226f, 496f, 40f);
            Label(root, "Value Music", "80%", 780f, 214f, 120f, 60f, 34, Ink, TextAnchor.MiddleRight);

            // 音效
            Label(root, "Label Sfx", "音效", 140f, 334f, 120f, 60f, 38, Ink, TextAnchor.MiddleLeft);
            Slider(root, "Slider Sfx", 272f, 346f, 496f, 40f);
            Label(root, "Value Sfx", "80%", 780f, 334f, 120f, 60f, 34, Ink, TextAnchor.MiddleRight);

            // 底部：分隔线与关闭
            Box(root, "Footer rule", 140f, 516f, 760f, 2f, new Color(Ink.r, Ink.g, Ink.b, 0.28f));
            RectTransform close = Rect(root, "Close", 390f, 556f, 260f, 100f);
            var closeImg = close.gameObject.AddComponent<Image>();
            closeImg.sprite = button;
            closeImg.type = Image.Type.Simple;
            closeImg.color = Color.white;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImg;
            closeButton.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(close, "Label", "关闭", 8f, 4f, 244f, 92f, 38, Caption, TextAnchor.MiddleCenter);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);
            AssetDatabase.SaveAssets();
            Debug.Log("[SettingsPanel] 已写入 " + PrefabPath, saved);

            RemoveLegacyPauseSettings();
        }

        private static void RemoveLegacyPauseSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPath) == null) return;
            GameObject contents = PrefabUtility.LoadPrefabContents(OverlayPath);
            bool changed = false;
            Transform pause = contents.transform.Find("Pause panel");
            Transform legacy = pause != null ? pause.Find("Settings panel") : null;
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy.gameObject);
                changed = true;
            }
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, OverlayPath);
                Debug.Log("[SettingsPanel] 已移除 GameplayOverlay 内嵌的 Pause panel/Settings panel");
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static RectTransform Rect(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static Image Box(RectTransform parent, string name, float x, float y, float w, float h, Color color)
        {
            RectTransform rt = Rect(parent, name, x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(RectTransform parent, string name, string text, float x, float y, float w, float h,
            int size, Color color, TextAnchor align)
        {
            RectTransform rt = Rect(parent, name, x, y, w, h);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>滑条：底槽 + 填充 + 手柄，值域 0–1，从左往右。</summary>
        private static Slider Slider(RectTransform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rt = Rect(parent, name, x, y, w, h);
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            RectTransform track = Rect(rt, "Track", 0f, 0f, w, h);
            Stretch(track, 0f, 12f);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.sprite = trackSprite;
            trackImage.type = Image.Type.Sliced;
            trackImage.color = TrackColor;
            trackImage.raycastTarget = true;

            RectTransform fillArea = Rect(rt, "Fill Area", 0f, 0f, w, h);
            Stretch(fillArea, 2f, 13f);
            RectTransform fill = Rect(fillArea, "Fill", 0f, 0f, 0f, 0f);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = trackSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = FillColor;
            fillImage.raycastTarget = false;

            RectTransform handleArea = Rect(rt, "Handle Slide Area", 0f, 0f, w, h);
            Stretch(handleArea, 13f, 3f);
            RectTransform handle = Rect(handleArea, "Handle", 0f, 0f, 0f, 0f);
            handle.anchorMin = new Vector2(0.8f, 0f);
            handle.anchorMax = new Vector2(0.8f, 1f);
            handle.sizeDelta = new Vector2(30f, 0f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = knobSprite;
            handleImage.type = Image.Type.Simple;
            handleImage.color = KnobColor;
            handleImage.raycastTarget = true;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        private static void Stretch(RectTransform rt, float horizontal, float vertical)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(horizontal, vertical);
            rt.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
