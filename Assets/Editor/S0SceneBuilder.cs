using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class S0SceneBuilder
{
    private static readonly Color Ink = new Color(0.035f, 0.055f, 0.075f, 0.93f);
    private static readonly Color Paper = new Color(0.87f, 0.92f, 0.93f);
    private static readonly Color Accent = new Color(0.56f, 0.82f, 0.86f);
    private static Font font;
    private static S0Controller controller;

    [MenuItem("WWG/Build S0 Scene")]
    public static void Build()
    {
        string[] art = { "Exterior", "Corridor", "ClubRoom", "ClubRoomNight", "Bridge", "FallenBox", "YuanQi", "TangChe", "XuYuan" };
        foreach (string name in art)
        {
            string path = "Assets/Resources/S0/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new Exception("Missing S0 art: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "S0";
        GameObject camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        camera.GetComponent<Camera>().orthographic = true;
        camera.GetComponent<Camera>().backgroundColor = Color.black;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        GameObject root = new GameObject("S0 Game", typeof(S0Controller), typeof(AudioSource));
        controller = root.GetComponent<S0Controller>();
        controller.audioSource = root.GetComponent<AudioSource>();
        controller.audioSource.playOnAwake = false;
        controller.deskWorkSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/S0/DeskWork.wav");
        controller.breakerSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/S0/Breaker.wav");
        GameObject canvas = new GameObject("S0 Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(root.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        controller.exterior = Page(canvas.transform, "Exterior", "Exterior");
        controller.corridor = Page(canvas.transform, "Corridor", "Corridor");
        controller.clubRoom = Page(canvas.transform, "ClubRoom", "ClubRoom");
        controller.roomBackdrop = controller.clubRoom.GetComponent<Image>();
        controller.nightRoomSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/ClubRoomNight.png");
        controller.bridge = Page(canvas.transform, "Bridge", "Bridge");
        GameObject exteriorInteractions = Rect(controller.exterior.transform, "Scene interactions", 0, 0, 1920, 1080);
        GameObject corridorInteractions = Rect(controller.corridor.transform, "Scene interactions", 0, 0, 1920, 1080);
        GameObject roomInteractions = Rect(controller.clubRoom.transform, "Scene interactions", 0, 0, 1920, 1080);
        GameObject bridgeInteractions = Rect(controller.bridge.transform, "Scene interactions", 0, 0, 1920, 1080);
        controller.interactionLayers = new[] { exteriorInteractions, corridorInteractions, roomInteractions, bridgeInteractions };

        // Exterior: one physical board plus architecture hotspots.
        Panel(controller.exterior.transform, "Directory board", 40, 390, 305, 400, new Color(0.15f, 0.22f, 0.26f, 0.94f));
        Label(controller.exterior.transform, "Directory board caption", "实验楼导览\n\n单数栋 / 偶数栋\n\n1F  电气与电子\n2F  机器人\n3F  计算机工程\n4F  外国语\n5F  数智设计\n\n点击查看夜间通知", 58, 410, 275, 360, 28, Paper, TextAnchor.UpperLeft);
        Hotspot(exteriorInteractions.transform, "楼层展板", "directory", "查看楼层展板", 40, 390, 305, 400);
        Hotspot(exteriorInteractions.transform, "4F 玻璃连廊", "exteriorBridge", "抬头看 4F 玻璃连廊", 410, 250, 910, 210);
        Hotspot(exteriorInteractions.transform, "实验楼大门", "enter", "进入实验楼 · 单数栋 5F", 580, 515, 750, 350);
        exteriorInteractions.transform.SetAsLastSibling();

        // Fifth-floor corridor and its clickable signage.
        Hotspot(corridorInteractions.transform, "安全指示牌", "sign", "查看安全出口指示牌", 365, 570, 115, 125);
        Hotspot(corridorInteractions.transform, "照明定时箱", "timer", "查看照明定时箱", 275, 302, 135, 180);
        Hotspot(corridorInteractions.transform, "楼层牌", "floorPlaque", "查看楼层牌", 1515, 75, 280, 360);
        Nav(corridorInteractions.transform, "← 活动室", "toRoom", 70, 805, 245, 64);
        Nav(corridorInteractions.transform, "楼梯 ↓ 4F 连廊", "toBridge", 1360, 805, 405, 64);

        // Club room: the old box, chair and taped floor remain in the shared scene art.
        Hotspot(roomInteractions.transform, "第五把椅子", "chair", "查看第五把椅子 / 储物柜", 1280, 395, 390, 380);
        Nav(roomInteractions.transform, "← 5F 走廊", "toCorridor", 64, 800, 280, 64);
        Nav(roomInteractions.transform, "与袁祁交谈", "leader", 760, 800, 330, 64);

        // Four-floor entrance: leave source material on its own visual layer.
        Hotspot(bridgeInteractions.transform, "双栋导览练习稿", "guide", "查看双栋导览练习稿", 40, 135, 460, 460);
        Hotspot(bridgeInteractions.transform, "玻璃连廊", "glass", "查看玻璃连廊", 590, 285, 625, 425);
        Hotspot(bridgeInteractions.transform, "应急照明开关", "switch", "查看应急照明开关", 1480, 350, 130, 160);
        Nav(bridgeInteractions.transform, "← 返回 5F", "bridgeBack", 64, 805, 310, 64);

        // A small HUD follows the existing UI sketch: objective at top and dialogue at bottom.
        GameObject hud = Panel(canvas.transform, "HUD", 0, 0, 1920, 120, new Color(0.025f, 0.040f, 0.055f, 0.82f));
        controller.timeText = Label(hud.transform, "Clock", "", 45, 20, 330, 48, 33, Accent, TextAnchor.MiddleLeft);
        controller.objectiveText = Label(hud.transform, "Objective", "", 370, 24, 1050, 44, 30, Paper, TextAnchor.MiddleCenter);
        controller.progressText = Label(hud.transform, "Progress", "", 1420, 20, 450, 48, 27, Accent, TextAnchor.MiddleRight);
        Label(hud.transform, "Status", "SAN 100/100    生命 3/3", 1505, 70, 365, 34, 23, Paper, TextAnchor.MiddleRight);
        Button(hud.transform, "Memo button", "备忘录", 45, 72, 210, 43, controller.ToggleMemo);
        controller.hoverText = Label(canvas.transform, "Hover caption", "", 400, 895, 1120, 60, 30, Color.white, TextAnchor.MiddleCenter);

        controller.nightTint = Panel(canvas.transform, "Night room grade", 0, 120, 1920, 815, new Color(0.02f, 0.045f, 0.10f, 0.08f));
        controller.nightTint.GetComponent<Image>().raycastTarget = false;
        controller.fallenBox = Rect(canvas.transform, "Fallen box and photo", 1280, 615, 390, 235);
        Image fallenImage = controller.fallenBox.AddComponent<Image>();
        fallenImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/FallenBox.png");
        fallenImage.raycastTarget = false;
        controller.fallenBoxHotspot = Hotspot(controller.fallenBox.transform, "照片", "photo", "查看掉落的照片", 0, 0, 390, 235);

        controller.dialoguePanel = Panel(canvas.transform, "Dialogue", 45, 820, 1830, 230, Ink);
        GameObject portrait = Rect(controller.dialoguePanel.transform, "Speaker portrait", 30, -75, 245, 305);
        controller.portraitImage = portrait.AddComponent<Image>();
        controller.portraitImage.preserveAspect = true;
        controller.portraitImage.raycastTarget = false;
        controller.yuanPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/YuanQi.png");
        controller.tangPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/TangChe.png");
        controller.xuPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/XuYuan.png");
        controller.speakerText = Label(controller.dialoguePanel.transform, "Speaker", "", 36, 18, 390, 48, 31, Accent, TextAnchor.MiddleLeft);
        controller.dialogueText = Label(controller.dialoguePanel.transform, "Line", "", 36, 68, 1745, 115, 32, Paper, TextAnchor.UpperLeft);
        Label(controller.dialoguePanel.transform, "Continue hint", "点击继续  ▸", 1500, 186, 280, 30, 22, Accent, TextAnchor.MiddleRight);
        Button(controller.dialoguePanel.transform, "Continue", "", 0, 0, 1830, 230, controller.ContinueDialogue, transparent: true);

        controller.infoPanel = Panel(canvas.transform, "Inspect panel", 390, 140, 1140, 800, Ink);
        controller.infoTitle = Label(controller.infoPanel.transform, "Inspect title", "", 55, 42, 1025, 60, 39, Accent, TextAnchor.MiddleLeft);
        controller.infoBody = Label(controller.infoPanel.transform, "Inspect body", "", 55, 130, 1025, 570, 31, Paper, TextAnchor.UpperLeft);
        Button(controller.infoPanel.transform, "Close inspect", "关闭", 930, 715, 150, 56, controller.CloseInfo);

        controller.memoPanel = Panel(canvas.transform, "Memo panel", 310, 95, 1300, 885, Ink);
        Label(controller.memoPanel.transform, "Memo title", "备忘录", 50, 35, 1050, 60, 40, Accent, TextAnchor.MiddleLeft);
        controller.memoText = Label(controller.memoPanel.transform, "Memo body", "", 50, 125, 1190, 665, 28, Paper, TextAnchor.UpperLeft);
        Button(controller.memoPanel.transform, "Close memo", "关闭", 1100, 35, 150, 55, controller.ToggleMemo);

        controller.completionPanel = Panel(canvas.transform, "Time transition", 0, 0, 1920, 1080, new Color(0.005f, 0.010f, 0.020f, 1));
        controller.completionText = Label(controller.completionPanel.transform, "Transition caption", "", 190, 300, 1540, 460, 47, Paper, TextAnchor.MiddleCenter);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/S0.unity");
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene("Assets/Scenes/S0.unity", true) };
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            if (entry.path != "Assets/Scenes/S0.unity") scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("WWG S0 scene built and added as first Build Settings scene.");
    }

    private static GameObject Page(Transform parent, string name, string resource)
    {
        GameObject page = Rect(parent, name, 0, 0, 1920, 1080);
        Image image = page.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/" + resource + ".png");
        image.raycastTarget = false;
        image.preserveAspect = false;
        return page;
    }

    private static GameObject Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    private static GameObject Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        GameObject go = Rect(parent, name, x, y, w, h);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    private static Text Label(Transform parent, string name, string body, float x, float y, float w, float h, int size, Color color, TextAnchor anchor)
    {
        GameObject go = Rect(parent, name, x, y, w, h);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = body;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject Hotspot(Transform parent, string name, string action, string label, float x, float y, float w, float h)
    {
        GameObject go = Panel(parent, name, x, y, w, h, new Color(0.65f, 0.88f, 0.95f, 0.005f));
        S0Hotspot hotspot = go.AddComponent<S0Hotspot>();
        hotspot.controller = controller;
        hotspot.action = action;
        hotspot.label = label;
        return go;
    }

    private static void Nav(Transform parent, string title, string action, float x, float y, float w, float h)
    {
        GameObject panel = Panel(parent, title, x, y, w, h, new Color(0.03f, 0.06f, 0.08f, 0.82f));
        Label(panel.transform, "Caption", title, 15, 9, w - 30, h - 18, 27, Paper, TextAnchor.MiddleCenter);
        Hotspot(panel.transform, "Click area", action, title, 0, 0, w, h);
    }

    private static Button Button(Transform parent, string name, string title, float x, float y, float w, float h, UnityEngine.Events.UnityAction onClick, bool transparent = false)
    {
        GameObject go = Panel(parent, name, x, y, w, h, transparent ? new Color(1, 1, 1, 0.001f) : new Color(0.14f, 0.26f, 0.31f, 0.95f));
        Button button = go.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        if (!transparent) Label(go.transform, "Caption", title, 6, 3, w - 12, h - 6, 25, Paper, TextAnchor.MiddleCenter);
        return button;
    }
}
