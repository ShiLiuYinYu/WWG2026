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

/// <summary>Explicit one-time authoring of the four S2 fixed views. Never overwrites hand-edited scenes.</summary>
public static class S2SceneBuilder
{
    private const string Folder = "Assets/Scenes/Locations";
    private static readonly string[] Names = { "BridgeOdd4F", "BridgeSpan4F", "BridgeEven4F", "ClubRoom" };
    private static readonly Color Ink = new Color(0.025f, 0.039f, 0.057f, 0.95f);
    private static readonly Color Paper = new Color(0.88f, 0.92f, 0.91f);
    private static readonly Color Accent = new Color(0.57f, 0.84f, 0.78f);
    private static Font font;

    [MenuItem("WWG/Setup/Build S2 Location Scenes")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先保存当前场景。");
        foreach (string name in Names)
            if (File.Exists(Folder + "/" + name + ".unity"))
                throw new InvalidOperationException("S2 地点已存在，停止以保护手动编辑：" + name);
        string previous = SceneManager.GetActiveScene().path;
        Directory.CreateDirectory(Folder);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < Names.Length; i++) BuildLocation(i);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string name in Names) scenes.Add(new EditorBuildSettingsScene(Folder + "/" + name + ".unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        Debug.Log("[WWG] 已建立 S2 三处 4F 地点与 5F 安全屋视图，并加入 Build Settings。");
    }

    private static void BuildLocation(int index)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string name = Names[index];
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        camera.GetComponent<Camera>().orthographic = true;
        camera.GetComponent<Camera>().backgroundColor = Color.black;
        new GameObject("Global Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var root = new GameObject("S2 " + name, typeof(S2Controller));
        S2Controller c = root.GetComponent<S2Controller>();
        var canvas = new GameObject("S2 Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(root.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scale = canvas.GetComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scale.matchWidthOrHeight = 0.5f;

        string art = index == 3 ? "Assets/Resources/S1/ClubRoomNightClean.png" : "Assets/Resources/S1/BridgeNight.png";
        var background = Panel(canvas.transform, "Location artwork", 0, 0, 1920, 1080, Color.white, false);
        var image = background.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        if (index == 2) background.transform.localScale = new Vector3(-1, 1, 1);
        c.glow = Panel(canvas.transform, "Emergency lighting", 0, 0, 1920, 1080,
            new Color(0.64f, 0.86f, 0.85f, 0.28f), false);

        c.locationText = Label(canvas.transform, "Location", "", 34, 17, 520, 55, 32, Accent, TextAnchor.MiddleLeft);
        c.objectiveText = Label(canvas.transform, "Objective", "", 570, 18, 980, 50, 30, Paper, TextAnchor.MiddleCenter);
        c.objectiveText.gameObject.SetActive(false);
        c.timeText = Label(canvas.transform, "Time", "", 1590, 15, 290, 48, 29, Accent, TextAnchor.MiddleRight);
        c.timeText.gameObject.SetActive(false);
        c.vitals = Vitals(canvas.transform);
        Button(canvas.transform, "Inventory", "背包", 235, 78, 130, 42, c.OpenInventory);
        Button(canvas.transform, "Pause", "暂停", 385, 78, 130, 42, c.TogglePause);

        // Visible 2D click targets occupy distinct parts of each fixed view.
        if (index == 0)
        {
            Label(canvas.transform, "Odd marker", "单数栋 4F  ·  连廊口", 640, 174, 640, 56, 40, Paper, TextAnchor.MiddleCenter).gameObject.SetActive(false);
            Button(canvas.transform, "Guide hotspot", "双栋导览练习稿", 105, 330, 360, 88, c.InspectGuide);
            Button(canvas.transform, "Near poster hotspot", "近端海报", 450, 510, 270, 105, c.InspectNear);
            Button(canvas.transform, "Far poster hotspot", "远端海报 · 贴纸", 1050, 440, 360, 110, c.InspectFar);
            c.farRevealText = Label(canvas.transform, "Far number", "贴纸遮住了旧展位号", 1055, 555, 350, 58, 29, Accent, TextAnchor.MiddleCenter);
            c.farRevealText.gameObject.SetActive(false);
            c.switchButton = Button(canvas.transform, "Emergency switch hotspot", "打开应急灯", 1450, 305, 360, 90, c.SwitchLight);
            Button(canvas.transform, "To span", "走入玻璃连廊  →", 1320, 862, 500, 95, c.GoSpan);
            Button(canvas.transform, "To safe", "↑ 5F 活动室 · 安全屋", 70, 862, 475, 95, c.GoSafe);
            Button(canvas.transform, "To machine room", "↓ 单数栋 3F 机房", 645, 862, 430, 95, c.GoMachineRoom);
        }
        else if (index == 1)
        {
            Label(canvas.transform, "Span marker", "玻璃连廊 · 中段", 690, 174, 540, 56, 40, Paper, TextAnchor.MiddleCenter).gameObject.SetActive(false);
            c.glassButton = Button(canvas.transform, "Glass hotspot", "查看玻璃上的痕迹", 650, 360, 620, 280, c.InspectGlass);
            Button(canvas.transform, "Back odd", "← 单数栋 4F", 70, 862, 430, 95, c.GoOdd);
            Button(canvas.transform, "Forward even", "偶数栋 4F →", 1420, 862, 430, 95, c.GoEven);
            Button(canvas.transform, "Retreat", "退回单数栋楼梯口", 730, 862, 460, 95, c.Retreat);
        }
        else if (index == 2)
        {
            Label(canvas.transform, "Even marker", "偶数栋 4F  ·  连廊尽头", 625, 174, 670, 56, 40, Paper, TextAnchor.MiddleCenter).gameObject.SetActive(false);
            Button(canvas.transform, "Near poster hotspot", "近端海报", 1150, 470, 280, 108, c.InspectNear);
            Button(canvas.transform, "Far poster hotspot", "远端海报 · 贴纸", 520, 450, 355, 108, c.InspectFar);
            c.farRevealText = Label(canvas.transform, "Far number", "贴纸遮住了旧展位号", 522, 560, 355, 58, 29, Accent, TextAnchor.MiddleCenter);
            c.farRevealText.gameObject.SetActive(false);
            Button(canvas.transform, "Cabinet hotspot", "社团旧资料柜 · 四位锁", 140, 630, 475, 110, c.InspectCabinet);
            c.drawerButton = Button(canvas.transform, "Drawer hotspot", "打开抽屉 · 取出资料", 140, 750, 475, 85, c.TakeDrawer);
            Button(canvas.transform, "Back span", "← 返回玻璃连廊", 65, 862, 475, 95, c.GoSpan);
        }
        else
        {
            Label(canvas.transform, "Safe room marker", "5F 活动室 · 设备仍在运行", 570, 175, 780, 62, 42, Paper, TextAnchor.MiddleCenter).gameObject.SetActive(false);
            Button(canvas.transform, "Rest hotspot", "坐下休息 · 恢复 SAN / 生命", 625, 610, 670, 110, c.Rest);
            Button(canvas.transform, "Down to bridge", "↓ 返回单数栋 4F 连廊口", 590, 862, 740, 95, c.GoOdd);
        }

        c.feedbackText = Label(canvas.transform, "Feedback", "", 230, 735, 1460, 105, 29, Paper, TextAnchor.MiddleCenter);
        c.feedbackText.gameObject.AddComponent<Outline>().effectColor = Color.black;
        c.feedbackText.gameObject.SetActive(false);

        c.infoPanel = Panel(canvas.transform, "Inspect", 365, 180, 1190, 720, Ink);
        c.infoTitle = Label(c.infoPanel.transform, "Title", "", 55, 45, 1080, 85, 40, Accent, TextAnchor.MiddleLeft);
        c.infoBody = Label(c.infoPanel.transform, "Body", "", 65, 165, 1060, 395, 36, Paper, TextAnchor.UpperLeft);
        Button(c.infoPanel.transform, "Close", "继续调查", 890, 615, 245, 68, c.CloseInfo);
        if (index == 1)
        {
            c.routePanel = c.infoPanel;
            c.routeDescriptionText = c.infoBody;
            c.routeConfirmButton = Button(c.infoPanel.transform, "Confirm glass route", "确认调查",
                590, 615, 275, 68, c.ConfirmGlassRoute);
            c.routeConfirmText = c.routeConfirmButton.GetComponentInChildren<Text>();
            c.routeConfirmButton.gameObject.SetActive(false);
        }

        c.codePanel = Panel(canvas.transform, "Four digit cabinet", 500, 225, 920, 600, Ink);
        Label(c.codePanel.transform, "Title", "社团资料柜 · 四位密码", 60, 35, 800, 64, 40, Accent, TextAnchor.MiddleCenter);
        Label(c.codePanel.transform, "Note", "密码是四张海报的展位号，按导览巡场顺序排。点击数字逐位调整。", 65, 125, 790, 110, 29, Paper, TextAnchor.MiddleCenter);
        c.codeFeedbackText = Label(c.codePanel.transform, "Feedback", "", 70, 244, 780, 52, 27, Accent, TextAnchor.MiddleCenter);
        c.digitTexts = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            Button b = Button(c.codePanel.transform, "Digit " + i, "0", 100 + i * 185, 300, 125, 100, null);
            UnityEventTools.AddIntPersistentListener(b.onClick, c.CycleDigit, i);
            c.digitTexts[i] = b.GetComponentInChildren<Text>();
        }
        Button(c.codePanel.transform, "Submit", "开锁", 190, 480, 245, 65, c.SubmitCode);
        Button(c.codePanel.transform, "Close", "返回", 500, 480, 245, 65, c.CloseCode);

        c.recordPanel = Panel(canvas.transform, "Task record", 300, 125, 1320, 825, Ink);
        Label(c.recordPanel.transform, "Title", "任务记录", 60, 30, 880, 70, 42, Accent, TextAnchor.MiddleLeft);
        c.recordText = Label(c.recordPanel.transform, "Record", "", 75, 135, 1170, 580, 30, Paper, TextAnchor.UpperLeft);
        Button(c.recordPanel.transform, "Close", "关闭", 1080, 42, 170, 60, c.CloseRecord);

        c.inventoryPanel = Panel(canvas.transform, "Inventory", 460, 160, 1000, 740, Ink);
        Label(c.inventoryPanel.transform, "Title", "背包", 65, 42, 600, 72, 42, Accent, TextAnchor.MiddleLeft);
        c.inventoryText = Label(c.inventoryPanel.transform, "Items", "", 75, 145, 850, 420, 34, Paper, TextAnchor.UpperLeft);
        Button(c.inventoryPanel.transform, "Close", "关闭", 735, 620, 190, 62, c.CloseInventory);

        c.pausePanel = Panel(canvas.transform, "Pause", 575, 170, 770, 740, Ink);
        Label(c.pausePanel.transform, "Title", "暂停", 80, 38, 610, 80, 48, Accent, TextAnchor.MiddleCenter);
        Button(c.pausePanel.transform, "Resume", "继续", 125, 160, 520, 72, c.TogglePause);
        Button(c.pausePanel.transform, "Save", "存档", 125, 267, 520, 72, c.SaveManual);
        Button(c.pausePanel.transform, "Load", "读档", 125, 374, 520, 72, c.LoadManual);
        Button(c.pausePanel.transform, "Title", "返回标题", 125, 481, 520, 72, c.ReturnTitle);

        c.infoPanel.SetActive(false);
        c.codePanel.SetActive(false);
        c.recordPanel.SetActive(false);
        c.inventoryPanel.SetActive(false);
        c.pausePanel.SetActive(false);

        EditorSceneManager.SaveScene(scene, Folder + "/" + name + ".unity");
    }

    private static VitalsBars Vitals(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/VitalsBars.prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = "Vitals";
        return go.GetComponent<VitalsBars>();
    }
    private static GameObject Rect(Transform parent, string name, float x, float y, float w, float h)
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
    private static GameObject Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool raycast = true)
    {
        GameObject go = Rect(parent, name, x, y, w, h);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return go;
    }
    private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h,
        int size, Color color, TextAnchor anchor)
    {
        GameObject go = Rect(parent, name, x, y, w, h);
        Text label = go.AddComponent<Text>();
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
    private static Button Button(Transform parent, string name, string caption, float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
    {
        GameObject go = Panel(parent, name, x, y, w, h, new Color(0.075f, 0.14f, 0.18f, 0.91f));
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
        Label(go.transform, "Caption", caption, 8, 4, w - 16, h - 8, 29, Paper, TextAnchor.MiddleCenter);
        return button;
    }
}
