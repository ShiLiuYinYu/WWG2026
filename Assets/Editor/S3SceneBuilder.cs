using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>Creates the S3 view once, then leaves all layout to scene editing.</summary>
public static class S3SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/S3.unity";
    private const string ArtPath = "Assets/Resources/S3/ServerRoom3F.png";
    private const string MonitorArtPath = "Assets/Resources/S3/ComputerScreen.png";
    internal const string ArchiveWindowContent = "归档窗口内容";
    private static readonly Color Ink = new Color(0.035f, 0.055f, 0.055f, 0.97f);
    private static readonly Color Terminal = new Color(0.014f, 0.037f, 0.037f, 0.98f);
    private static readonly Color Green = new Color(0.62f, 0.9f, 0.7f);
    private static readonly Color Paper = new Color(0.19f, 0.15f, 0.12f);
    private static Font font;

    [MenuItem("WWG/Setup/Build S3 Server Room")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先保存当前场景。");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("机房场景已存在，停止以保护手动编辑。");
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath) == null)
            throw new InvalidOperationException("缺少机房美术素材：" + ArtPath);
        string previous = SceneManager.GetActiveScene().path;
        var importer = AssetImporter.GetAtPath(ArtPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        camera.GetComponent<Camera>().orthographic = true;
        camera.GetComponent<Camera>().backgroundColor = Color.black;
        new GameObject("Global Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var root = new GameObject("S3 Server Room", typeof(S3Controller));
        var c = root.GetComponent<S3Controller>();
        var canvas = new GameObject("S3 Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(root.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject art = Panel(canvas.transform, "机房画面", 0, 0, 1920, 1080, Color.white, false);
        art.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        art.GetComponent<Image>().type = Image.Type.Simple;
        Panel(canvas.transform, "夜间暗层", 0, 0, 1920, 1080, new Color(0.015f, 0.022f, 0.038f, 0.16f), false);
        Panel(canvas.transform, "顶栏", 0, 0, 1920, 118, new Color(0.025f, 0.037f, 0.052f, 0.88f), false);
        c.locationText = Label(canvas.transform, "地点", "", 32, 15, 455, 55, 32, Green, TextAnchor.MiddleLeft);
        c.objectiveText = Label(canvas.transform, "目标", "", 485, 15, 1000, 55, 30, Color.white, TextAnchor.MiddleCenter);
        c.timeText = Label(canvas.transform, "时间", "", 1130, 75, 330, 40, 28, Green, TextAnchor.MiddleRight);
        var vitalsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/VitalsBars.prefab");
        if (vitalsPrefab == null) throw new InvalidOperationException("缺少 VitalsBars 预制体。");
        var vitals = (GameObject)PrefabUtility.InstantiatePrefab(vitalsPrefab, canvas.transform);
        vitals.name = "Vitals";
        c.vitals = vitals.GetComponent<VitalsBars>();
        Button(canvas.transform, "背包入口", "背包", 230, 78, 130, 42, c.OpenInventory);
        Button(canvas.transform, "任务记录入口", "记录", 380, 78, 130, 42, c.OpenRecord);
        Button(canvas.transform, "暂停入口", "暂停", 530, 78, 130, 42, c.TogglePause);

        Hotspot(canvas.transform, "电脑桌草图热点", "查看草图与便签", 345, 735, 565, 165, c.InspectSketch);
        Hotspot(canvas.transform, "靠窗第三台电脑热点", "唤醒靠窗第三台", 770, 480, 375, 260, c.OpenComputer);
        Button(canvas.transform, "返回楼梯口", "← 单数栋楼梯口 / 5F 安全屋", 52, 932, 555, 78, c.GoExit);
        c.feedbackText = Label(canvas.transform, "提示", "", 640, 914, 1180, 96, 28, Color.white, TextAnchor.MiddleCenter);
        c.feedbackText.gameObject.AddComponent<Outline>().effectColor = Color.black;

        c.archivePanel = Panel(canvas.transform, "归档提取工具", 350, 142, 1220, 775, Terminal);
        Label(c.archivePanel.transform, "标题", "游戏开发社 · 机房本地归档提取工具 v1.2", 55, 30, 1080, 65, 39, Green, TextAnchor.MiddleCenter);
        Label(c.archivePanel.transform, "说明", "核对登记人 / 机位编号 / 封存时间。点击字段切换候选。", 70, 100, 1080, 54, 27, Green, TextAnchor.MiddleCenter);
        c.fieldLabels = new Text[3];
        string[] names = { "登记人", "机位编号", "封存时间" };
        for (int i = 0; i < 3; i++)
        {
            Label(c.archivePanel.transform, "字段名 " + i, names[i], 65, 180 + i * 112, 245, 77, 31, Green, TextAnchor.MiddleRight);
            var b = Button(c.archivePanel.transform, "字段选择 " + i, "点击选择", 340, 180 + i * 112, 790, 77, null);
            UnityEventTools.AddIntPersistentListener(b.onClick, c.CycleField, i);
            c.fieldLabels[i] = b.GetComponentInChildren<Text>();
        }
        c.archiveFeedback = Label(c.archivePanel.transform, "校验反馈", "", 75, 532, 1060, 90, 27, Green, TextAnchor.MiddleCenter);
        // 五个按钮统一 194×70、间距 25，左右各留 75，避免“查看背包”窄一截。
        Button(c.archivePanel.transform, "提取", "提取归档", 75, 646, 194, 70, c.Extract);
        c.memoButton = Button(c.archivePanel.transform, "录音", "播放备忘", 294, 646, 194, 70, c.PlayMemo);
        c.copyButton = Button(c.archivePanel.transform, "拷贝", "拷入 U 盘", 513, 646, 194, 70, c.CopyProject);
        Button(c.archivePanel.transform, "背包", "查看背包", 732, 646, 194, 70, c.OpenInventory);
        Button(c.archivePanel.transform, "关闭", "返回机房", 951, 646, 194, 70, c.CloseComputer);

        c.infoPanel = PaperPanel(canvas.transform, "纸面线索", 330, 185, 1260, 700);
        c.infoTitle = Label(c.infoPanel.transform, "标题", "", 80, 67, 1090, 85, 42, Paper, TextAnchor.MiddleLeft);
        c.infoBody = Label(c.infoPanel.transform, "内容", "", 88, 172, 1075, 400, 34, Paper, TextAnchor.UpperLeft);
        Button(c.infoPanel.transform, "关闭", "继续调查", 940, 589, 215, 66, c.CloseInfo);

        c.recordPanel = PaperPanel(canvas.transform, "任务记录", 315, 145, 1290, 790);
        Label(c.recordPanel.transform, "标题", "任务记录", 90, 58, 900, 65, 42, Paper, TextAnchor.MiddleLeft);
        c.recordText = Label(c.recordPanel.transform, "内容", "", 100, 145, 1090, 560, 31, Paper, TextAnchor.UpperLeft);
        Button(c.recordPanel.transform, "关闭", "关闭", 995, 64, 195, 62, c.CloseRecord);

        c.inventoryPanel = PaperPanel(canvas.transform, "背包", 460, 170, 1000, 725);
        Label(c.inventoryPanel.transform, "标题", "背包 · 调查证据", 75, 55, 700, 72, 42, Paper, TextAnchor.MiddleLeft);
        c.inventoryText = Label(c.inventoryPanel.transform, "物品", "", 85, 150, 815, 455, 34, Paper, TextAnchor.UpperLeft);
        Button(c.inventoryPanel.transform, "关闭", "关闭", 710, 610, 190, 65, c.CloseInventory);

        c.pausePanel = Panel(canvas.transform, "暂停菜单", 570, 170, 780, 730, Ink);
        Label(c.pausePanel.transform, "标题", "暂停", 80, 45, 620, 78, 48, Green, TextAnchor.MiddleCenter);
        Button(c.pausePanel.transform, "继续", "继续", 125, 160, 530, 75, c.TogglePause);
        Button(c.pausePanel.transform, "存档", "存档", 125, 270, 530, 75, c.SaveManual);
        Button(c.pausePanel.transform, "读档", "读档", 125, 380, 530, 75, c.LoadManual);
        Button(c.pausePanel.transform, "标题", "返回标题", 125, 490, 530, 75, c.ReturnTitle);
        c.archivePanel.SetActive(false); c.infoPanel.SetActive(false); c.recordPanel.SetActive(false);
        c.inventoryPanel.SetActive(false); c.pausePanel.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        InstallUsbSlots();
        S3UiAlignment.Align();
        InstallMonitorOverlay();
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        Debug.Log("[WWG] 已建立 S3 机房场景并加入 Build Settings。");
    }

    [MenuItem("WWG/UI/Install S3 Monitor Overlay")]
    public static void InstallMonitorOverlay()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先保存当前场景。");
        string previous = SceneManager.GetActiveScene().path;
        Texture2D monitorArt = AssetDatabase.LoadAssetAtPath<Texture2D>(MonitorArtPath);
        if (monitorArt == null) throw new InvalidOperationException("缺少电脑屏幕素材：" + MonitorArtPath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        try
        {
            S3Controller c = UnityEngine.Object.FindFirstObjectByType<S3Controller>(FindObjectsInactive.Include);
            if (c == null || c.archivePanel == null) throw new InvalidOperationException("S3 归档工具缺失。");
            Transform panel = c.archivePanel.transform;
            Transform existingScreen = panel.Find("屏幕画面");
            if (existingScreen != null)
            {
                bool changed = false;
                // Image uses the sprite's trimmed alpha bounds. RawImage keeps the full 1920×1080 artwork.
                Image trimmedImage = existingScreen.GetComponent<Image>();
                if (trimmedImage != null)
                {
                    UnityEngine.Object.DestroyImmediate(trimmedImage);
                    RawImage fullImage = existingScreen.gameObject.AddComponent<RawImage>();
                    fullImage.texture = monitorArt;
                    fullImage.raycastTarget = false;
                    changed = true;
                }
                Image existingMask = panel.Find("归档窗口底色")?.GetComponent<Image>();
                if (existingMask != null && existingMask.color.a < 1f)
                {
                    existingMask.color = new Color(Terminal.r, Terminal.g, Terminal.b, 1f);
                    changed = true;
                }
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[WWG] S3 电脑屏幕显示已校正。");
                }
                else Debug.Log("[WWG] S3 电脑屏幕已安装；保留现有人工布局。");
                return;
            }

            var existingControls = new List<Transform>();
            foreach (Transform child in panel) existingControls.Add(child);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1920, 1080);
            Image backdrop = panel.GetComponent<Image>();
            backdrop.sprite = null;
            backdrop.color = Color.black;
            backdrop.raycastTarget = true;

            GameObject monitor = Rect(panel, "屏幕画面", 0, 0, 1920, 1080);
            RawImage monitorImage = monitor.AddComponent<RawImage>();
            monitorImage.texture = monitorArt;
            monitorImage.color = Color.white;
            monitorImage.raycastTarget = false;

            GameObject mask = Rect(panel, "归档窗口底色", 382, 182, 850, 655);
            Image maskImage = mask.AddComponent<Image>();
            maskImage.color = new Color(Terminal.r, Terminal.g, Terminal.b, 1f);
            maskImage.raycastTarget = false;

            GameObject content = Rect(panel, ArchiveWindowContent, 365, 218, 1220, 775);
            content.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            foreach (Transform child in existingControls) child.SetParent(content.transform, false);
            ApplyArchiveWindowStyle(content);

            GameObject close = Rect(panel, "屏幕关闭热点", 1243, 164, 34, 36);
            Image closeImage = close.AddComponent<Image>();
            closeImage.color = Color.clear;
            var closeButton = close.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(closeButton.onClick, c.CloseComputer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WWG] S3 归档工具已嵌入电脑屏幕画面。");
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }

    internal static void ApplyArchiveWindowStyle(GameObject content)
    {
        var surface = content.GetComponent<Image>();
        if (surface == null) surface = content.AddComponent<Image>();

        surface.color = new Color32(9, 15, 13, 255);
        surface.raycastTarget = false;
        ApplyArchiveFrame(surface, new Color32(98, 173, 130, 255), 3f);

        var sectionColor = new Color32(19, 39, 29, 255);
        var ruleColor = new Color32(55, 104, 78, 255);
        var header = ArchiveRule(content.transform, "程序标题栏", 0, 0, 104, sectionColor);
        ArchiveRule(header, "分隔线", 0, 102, 2, ruleColor);
        ArchiveRule(content.transform, "反馈区分隔线", 75, 509, 2, ruleColor);

        var accent = new Color32(209, 250, 224, 255);
        var text = new Color32(141, 189, 161, 255);
        foreach (Text label in content.GetComponentsInChildren<Text>(true))
            label.color = label.name == "标题" || label.GetComponentInParent<Button>(true) != null ? accent : text;

        foreach (Button button in content.GetComponentsInChildren<Button>(true))
        {
            if (button.targetGraphic is not Image image) continue;

            bool primary = button.name == "提取";
            image.color = primary ? new Color32(47, 119, 80, 255) : new Color32(35, 66, 52, 255);
            ApplyArchiveFrame(image, primary ? new Color32(144, 233, 175, 255) : new Color32(117, 187, 144, 255), 3f);

            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(0.78f, 0.78f, 0.78f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.55f, 0.55f, 0.55f);
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;
        }
    }

    private static void ApplyArchiveFrame(Image image, Color border, float width)
    {
        image.sprite = null;
        image.overrideSprite = null;
        image.type = Image.Type.Simple;
        var outline = image.GetComponent<Outline>();
        if (outline == null) outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(width, width);
    }

    private static RectTransform ArchiveRule(Transform parent, string name, float inset, float y, float height, Color color)
    {
        var existing = parent.Find(name);
        if (existing != null) return (RectTransform)existing;

        var image = Panel(parent, name, inset, y, 0, height, color, false).GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = new Vector2(-2 * inset, height);
        rect.SetAsFirstSibling();
        return rect;
    }

    private static void InstallUsbSlots()
    {
        string[] paths = { "Assets/Scenes/S2.unity", "Assets/Scenes/S2/S2_BridgeSpan4F.unity",
            "Assets/Scenes/S2/S2_BridgeEven4F.unity", "Assets/Scenes/S2/S2_ClubRoom.unity" };
        foreach (string path in paths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            S2InventoryGrid grid = UnityEngine.Object.FindFirstObjectByType<S2InventoryGrid>(FindObjectsInactive.Include);
            if (grid == null) throw new InvalidOperationException("S2 背包缺失：" + path);
            Transform cell = null;
            foreach (Transform child in grid.GetComponentsInChildren<Transform>(true))
                if (child.name == "Cell 04") { cell = child; break; }
            if (cell == null) throw new InvalidOperationException("S2 背包 Cell 04 缺失：" + path);
            S2InventorySlot slot = cell.GetComponent<S2InventorySlot>();
            if (slot == null) slot = cell.gameObject.AddComponent<S2InventorySlot>();
            slot.item = StoryIds.Item.OldProjectUsb;
            if (cell.Find("USB label") == null)
                Label(cell, "USB label", "U", 10, 7, 72, 78, 42, Green, TextAnchor.MiddleCenter).alignByGeometry = true;
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static GameObject Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }
    private static GameObject Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool raycast = true)
    {
        var go = Rect(parent, name, x, y, w, h);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return go;
    }
    private static GameObject PaperPanel(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = Panel(parent, name, x, y, w, h, Color.white);
        go.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/黄皮纸4_横.png");
        return go;
    }
    private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h,
        int size, Color color, TextAnchor anchor)
    {
        var go = Rect(parent, name, x, y, w, h);
        var label = go.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = anchor;
        label.text = value;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }
    private static Button Button(Transform parent, string name, string caption, float x, float y, float w, float h,
        UnityEngine.Events.UnityAction action)
    {
        var go = Panel(parent, name, x, y, w, h, new Color(0.09f, 0.14f, 0.14f, 0.94f));
        var b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
        Label(go.transform, "文字", caption, 8, 4, w - 16, h - 8, 28, Green, TextAnchor.MiddleCenter);
        return b;
    }
    private static void Hotspot(Transform parent, string name, string caption, float x, float y, float w, float h,
        UnityEngine.Events.UnityAction action)
    {
        var go = Panel(parent, name, x, y, w, h, Color.clear);
        var b = go.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
        var prompt = Panel(go.transform, "悬停提示", 8, h - 60, w - 16, 52,
            new Color(0.035f, 0.046f, 0.046f, 0.94f), false);
        Label(prompt.transform, "文字", caption, 4, 1, w - 24, 50, 26, new Color(1f, 0.84f, 0.4f), TextAnchor.MiddleCenter);
        var hover = go.AddComponent<S3HotspotHover>();
        hover.prompt = prompt;
        prompt.SetActive(false);
    }
}
