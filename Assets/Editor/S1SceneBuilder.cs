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

public static class S1SceneBuilder
{
    private static readonly Color Ink = new Color(0.035f, 0.055f, 0.075f, 0.96f);
    private static readonly Color Paper = new Color(0.87f, 0.92f, 0.93f);
    private static readonly Color Accent = new Color(0.56f, 0.82f, 0.86f);
    private static Font font;
    private static S1Controller controller;

    [MenuItem("WWG/Build S1 Scene")]
    public static void Build()
    {
        ImportSprite("Assets/Resources/S1/ClubRoomNightClean.png");
        ImportSprite("Assets/Resources/S1/FifthChair.png");
        ImportSprite("Assets/Resources/S1/CorridorNight.png");
        ImportSprite("Assets/Resources/S1/OldDeskLamp.png");
        ImportSprite("Assets/Resources/S1/BridgeNight.png");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "S1";
        GameObject camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        Camera cam = camera.GetComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = Color.black;
        new GameObject("Global Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        GameObject root = new GameObject("S1 Game", typeof(S1Controller));
        controller = root.GetComponent<S1Controller>();
        GameObject canvas = new GameObject("S1 Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(root.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        controller.room = Page(canvas.transform, "5F Activity Room", "Assets/Resources/S1/ClubRoomNightClean.png");
        controller.corridor = Page(canvas.transform, "5F Corridor", "Assets/Resources/S1/CorridorNight.png");
        Label(controller.corridor.transform, "Exit glow", "安全出口  ↓ 4F", 1250, 180, 480, 80, 38,
            new Color(0.43f, 0.88f, 0.62f), TextAnchor.MiddleCenter);
        controller.bridge = Page(canvas.transform, "4F Bridge Entrance", "Assets/Resources/S1/BridgeNight.png");
        Label(controller.bridge.transform, "Emergency lamp indicator", "●  连廊应急照明", 1390, 355, 390, 90, 33,
            new Color(0.94f, 0.45f, 0.39f), TextAnchor.MiddleCenter);

        // Real scene changes stay on separate layers, so the old photo can reuse this camera view.
        controller.curtainOverlay = Panel(controller.room.transform, "Closed curtains", 1150, 145, 300, 445,
            new Color(0.13f, 0.15f, 0.20f, 0.96f), false);
        for (int i = 0; i < 6; i++)
            Panel(controller.curtainOverlay.transform, "Curtain fold " + i, i * 50, 0, 18, 445,
                new Color(0.07f, 0.09f, 0.13f, 0.35f), false);
        controller.lampGlow = Page(controller.room.transform, "Old desk lamp lit",
            "Assets/Resources/S1/OldDeskLamp.png", 1090, 525, 100, 155);

        controller.box = Rect(controller.room.transform, "Fallen old box", 1260, 650, 230, 150);
        Image boxImage = controller.box.AddComponent<Image>();
        boxImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/FallenBox.png");
        boxImage.raycastTarget = false;
        controller.roomHotspots = Rect(controller.room.transform, "Room interactions", 0, 0, 1920, 1080);
        controller.boxHotspot = Hotspot(controller.roomHotspots.transform, "Old box and photo", "box", "捡起旧照片", 1260, 650, 230, 150);
        Hotspot(controller.roomHotspots.transform, "Curtains", "curtain", "拉上 / 拉开窗帘", 1140, 135, 320, 455);
        Hotspot(controller.roomHotspots.transform, "Desk lamp", "lamp", "打开 / 关闭旧桌灯", 1065, 490, 155, 200);
        controller.chairCabinet = Rect(controller.roomHotspots.transform, "Chair cabinet position", 1500, 475, 1, 1).GetComponent<RectTransform>();
        controller.chairTape = Rect(controller.roomHotspots.transform, "Chair tape position", 1430, 600, 1, 1).GetComponent<RectTransform>();
        controller.tapeTarget = Rect(controller.roomHotspots.transform, "White tape drop target", 1410, 680, 360, 240).GetComponent<RectTransform>();
        controller.chair = Rect(controller.roomHotspots.transform, "Movable fifth chair", 1500, 475, 210, 290);
        controller.chairRect = controller.chair.GetComponent<RectTransform>();
        Image chairImage = controller.chair.AddComponent<Image>();
        chairImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S1/FifthChair.png");
        chairImage.preserveAspect = true;
        S1Hotspot chairHotspot = controller.chair.AddComponent<S1Hotspot>();
        chairHotspot.controller = controller;
        chairHotspot.action = "chair";
        chairHotspot.label = "拖动第五把椅子到白色地胶框 / 点击移动";
        chairHotspot.tapeTarget = controller.tapeTarget;
        controller.badgeHotspot = Hotspot(controller.roomHotspots.transform, "Badge beneath cushion", "badge", "取出椅垫下的工牌", 1455, 710, 205, 105);
        Hotspot(controller.roomHotspots.transform, "Glass on door", "glass", "查看活动室门玻璃", 20, 260, 245, 430);
        Nav(controller.roomHotspots.transform, "→ 5F 走廊", "toCorridor", 55, 810, 275, 65);
        Nav(controller.roomHotspots.transform, "安全屋 · 休息", "rest", 1510, 145, 330, 65);

        controller.corridorHotspots = Rect(controller.corridor.transform, "Corridor interactions", 0, 0, 1920, 1080);
        Nav(controller.corridorHotspots.transform, "← 返回活动室", "toRoom", 60, 810, 325, 65);
        Nav(controller.corridorHotspots.transform, "下楼 · 4F 连廊 →", "toBridge", 1360, 810, 480, 65);
        controller.bridgeHotspots = Rect(controller.bridge.transform, "Bridge interactions", 0, 0, 1920, 1080);
        Nav(controller.bridgeHotspots.transform, "← 返回 5F", "bridgeBack", 55, 810, 290, 65);

        GameObject hud = Panel(canvas.transform, "HUD", 0, 0, 1920, 125,
            new Color(0.025f, 0.040f, 0.055f, 0.88f));
        controller.timeText = Label(hud.transform, "Clock", "", 42, 15, 310, 48, 31, Accent, TextAnchor.MiddleLeft);
        controller.objectiveText = Label(hud.transform, "Objective", "", 375, 22, 1030, 45, 29, Paper, TextAnchor.MiddleCenter);
        controller.progressText = Label(hud.transform, "Progress", "", 1400, 18, 455, 43, 28, Accent, TextAnchor.MiddleRight);
        controller.statusText = Label(hud.transform, "Status", "", 1460, 72, 390, 38, 25, Paper, TextAnchor.MiddleRight);
        Button(hud.transform, "Inventory button", "背包", 42, 72, 170, 42, controller.ToggleInventory);
        Button(hud.transform, "Memo button", "任务记录", 230, 72, 205, 42, controller.ToggleMemo);
        Button(hud.transform, "Load checkpoint", "读取检查点", 455, 72, 220, 42, controller.LoadCheckpoint);
        controller.hoverText = Label(canvas.transform, "Hover caption", "", 390, 915, 1140, 55, 27, Color.white, TextAnchor.MiddleCenter);

        controller.dialoguePanel = Panel(canvas.transform, "Dialogue", 45, 825, 1830, 225, Ink);
        controller.speakerText = Label(controller.dialoguePanel.transform, "Speaker", "", 35, 17, 460, 48, 34, Accent, TextAnchor.MiddleLeft);
        controller.dialogueText = Label(controller.dialoguePanel.transform, "Line", "", 35, 73, 1740, 108, 31, Paper, TextAnchor.UpperLeft);
        Label(controller.dialoguePanel.transform, "Continue hint", "点击继续  ▸", 1510, 187, 265, 28, 22, Accent, TextAnchor.MiddleRight);
        Button(controller.dialoguePanel.transform, "Continue", "", 0, 0, 1830, 225, controller.ContinueDialogue, true);

        controller.infoPanel = Panel(canvas.transform, "Inspect panel", 400, 170, 1120, 730, Ink);
        controller.infoTitle = Label(controller.infoPanel.transform, "Title", "", 55, 42, 1010, 65, 38, Accent, TextAnchor.MiddleLeft);
        controller.infoBody = Label(controller.infoPanel.transform, "Body", "", 55, 140, 1010, 480, 31, Paper, TextAnchor.UpperLeft);
        Button(controller.infoPanel.transform, "Close", "关闭", 920, 645, 150, 55, controller.CloseInfo);

        controller.inventoryPanel = Panel(canvas.transform, "Inventory", 550, 150, 820, 790, Ink);
        controller.inventoryText = Label(controller.inventoryPanel.transform, "Items", "", 70, 65, 680, 215, 35, Paper, TextAnchor.UpperLeft);
        Button(controller.inventoryPanel.transform, "Inspect photo", "查看旧照片 · 正反面", 75, 345, 665, 75, controller.InspectPhoto);
        Button(controller.inventoryPanel.transform, "Inspect badge", "查看林未工牌 · 正反面", 75, 450, 665, 75, controller.InspectBadge);
        Button(controller.inventoryPanel.transform, "Close", "关闭", 575, 690, 170, 60, controller.ToggleInventory);

        controller.photoPanel = Panel(canvas.transform, "Old photograph", 330, 110, 1260, 860, Ink);
        Label(controller.photoPanel.transform, "Source", "社团自留资料 · 去年的活动室照片", 55, 28, 970, 58, 33, Accent, TextAnchor.MiddleLeft);
        controller.photoFace = Rect(controller.photoPanel.transform, "Photo front", 70, 110, 1120, 630);
        GameObject miniature = Page(controller.photoFace.transform, "Photograph image", "Assets/Resources/S1/ClubRoomNightClean.png", 0, 0, 1120, 630);
        controller.photoCurtain = Panel(miniature.transform, "Curtain in photo", 670, 90, 180, 280,
            new Color(0.13f, 0.15f, 0.20f, 0.90f), false).GetComponent<Image>();
        controller.photoLamp = Page(miniature.transform, "Lamp in photo", "Assets/Resources/S1/OldDeskLamp.png",
            630, 310, 65, 95).GetComponent<Image>();
        controller.hintLampHighlight = Panel(miniature.transform, "Lamp hint", 620, 300, 85, 112,
            new Color(1f, 0.82f, 0.28f, 0.25f), false);
        controller.hintTapeHighlight = Panel(miniature.transform, "Tape hint", 805, 480, 195, 105,
            new Color(1f, 0.82f, 0.28f, 0.22f), false);
        GameObject photoChair = Rect(miniature.transform, "Fifth chair in photo", 830, 370, 130, 195);
        Image photoChairImage = photoChair.AddComponent<Image>();
        photoChairImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S1/FifthChair.png");
        photoChairImage.preserveAspect = true;
        photoChairImage.raycastTarget = false;
        controller.photoShadow = Panel(miniature.transform, "Fifth person's shadow", 872, 380, 58, 105,
            new Color(0.04f, 0.07f, 0.10f, 0), false).GetComponent<Image>();
        controller.photoShadowHead = Label(miniature.transform, "Shadow head", "●", 872, 340, 60, 70, 62,
            new Color(0.04f, 0.07f, 0.10f, 0), TextAnchor.MiddleCenter);
        controller.photoCaption = Label(controller.photoFace.transform, "Caption", "", 30, 570, 1040, 46, 29,
            Color.white, TextAnchor.MiddleCenter);
        controller.photoBack = Panel(controller.photoPanel.transform, "Photo back", 95, 120, 1070, 615,
            new Color(0.74f, 0.70f, 0.61f, 1));
        controller.photoBackText = Label(controller.photoBack.transform, "Back inscription", "", 95, 150, 880, 310,
            49, new Color(0.16f, 0.16f, 0.15f), TextAnchor.MiddleCenter);
        Button(controller.photoPanel.transform, "Flip photo", "翻面", 680, 770, 185, 56, controller.FlipPhoto);
        Button(controller.photoPanel.transform, "Close photo", "收起照片", 930, 770, 225, 56, controller.ClosePhoto);

        controller.memoPanel = Panel(canvas.transform, "Task record", 350, 125, 1220, 840, Ink);
        Label(controller.memoPanel.transform, "Heading", "任务记录", 50, 35, 850, 55, 40, Accent, TextAnchor.MiddleLeft);
        controller.memoText = Label(controller.memoPanel.transform, "Record", "", 50, 120, 1120, 615, 29, Paper, TextAnchor.UpperLeft);
        Button(controller.memoPanel.transform, "Close", "关闭", 1000, 35, 170, 55, controller.ToggleMemo);

        controller.completionPanel = Panel(canvas.transform, "S1 completion", 0, 0, 1920, 1080,
            new Color(0.005f, 0.012f, 0.025f, 0.97f));
        controller.completionText = Label(controller.completionPanel.transform, "Completion", "", 250, 335, 1420, 300,
            54, Paper, TextAnchor.MiddleCenter);
        Button(controller.completionPanel.transform, "Return to 4F", "查看 4F 入口", 760, 730, 400, 70, controller.CloseCompletion);
        controller.dialoguePanel.transform.SetAsLastSibling();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/S1.unity");
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        bool added = false;
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (entry.path == "Assets/Scenes/S1.unity") continue;
            scenes.Add(entry);
            if (!added && entry.path == "Assets/Scenes/S0.unity")
            {
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/S1.unity", true));
                added = true;
            }
        }
        if (!added) scenes.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/S1.unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("WWG S1 scene built and added after S0.");
    }

    private static void ImportSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("Missing S1 art: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = 2048;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static GameObject Page(Transform parent, string name, string path, float x = 0, float y = 0, float w = 1920, float h = 1080)
    {
        GameObject page = Rect(parent, name, x, y, w, h);
        Image image = page.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        image.raycastTarget = false;
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

    private static GameObject Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool raycast = true)
    {
        GameObject go = Rect(parent, name, x, y, w, h);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return go;
    }

    private static Text Label(Transform parent, string name, string body, float x, float y, float w, float h,
        int size, Color color, TextAnchor anchor)
    {
        GameObject go = Rect(parent, name, x, y, w, h);
        Text value = go.AddComponent<Text>();
        value.font = font;
        value.text = body;
        value.fontSize = size;
        value.color = color;
        value.alignment = anchor;
        value.horizontalOverflow = HorizontalWrapMode.Wrap;
        value.verticalOverflow = VerticalWrapMode.Truncate;
        value.raycastTarget = false;
        return value;
    }

    private static GameObject Hotspot(Transform parent, string name, string action, string label,
        float x, float y, float w, float h)
    {
        GameObject go = Panel(parent, name, x, y, w, h, new Color(0.65f, 0.88f, 0.95f, 0.005f));
        S1Hotspot hotspot = go.AddComponent<S1Hotspot>();
        hotspot.controller = controller;
        hotspot.action = action;
        hotspot.label = label;
        return go;
    }

    private static void Nav(Transform parent, string title, string action, float x, float y, float w, float h)
    {
        GameObject panel = Panel(parent, title, x, y, w, h, new Color(0.03f, 0.06f, 0.08f, 0.88f), false);
        Label(panel.transform, "Caption", title, 10, 8, w - 20, h - 16, 27, Paper, TextAnchor.MiddleCenter);
        Hotspot(panel.transform, "Click area", action, title, 0, 0, w, h);
    }

    private static Button Button(Transform parent, string name, string title, float x, float y, float w, float h,
        UnityEngine.Events.UnityAction onClick, bool transparent = false)
    {
        GameObject go = Panel(parent, name, x, y, w, h,
            transparent ? new Color(1, 1, 1, 0.001f) : new Color(0.14f, 0.26f, 0.31f, 0.97f));
        Button button = go.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        if (!transparent) Label(go.transform, "Caption", title, 5, 3, w - 10, h - 6, 25, Paper, TextAnchor.MiddleCenter);
        return button;
    }
}
