using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>Explicitly installs the shared overlay in existing location scenes.</summary>
public static class GameplayOverlayBuilder
{
    private static readonly string[] Scenes = {
        "Assets/Scenes/S0.unity", "Assets/Scenes/S0/S0_Corridor.unity",
        "Assets/Scenes/S0/S0_ClubRoom.unity", "Assets/Scenes/S0/S0_Bridge.unity",
        "Assets/Scenes/S1.unity", "Assets/Scenes/S1/S1_Corridor.unity",
        "Assets/Scenes/S1/S1_BridgeEntrance.unity"
    };
    private const string PrefabPath = "Assets/Prefabs/UI/GameplayOverlay.prefab";

    private static readonly Color Ink = new Color(0.84f, 0.88f, 0.88f);
    private static readonly Color Muted = new Color(0.59f, 0.67f, 0.69f);
    private static readonly Color Gold = new Color(0.78f, 0.65f, 0.42f);
    private static readonly Color Panel = new Color(0.055f, 0.085f, 0.11f, 0.96f);
    private static readonly Color Edge = new Color(0.25f, 0.36f, 0.39f, 0.95f);
    private static Font font;

    [MenuItem("WWG/UI/Install Gameplay Overlay")]
    public static void BuildAll()
    {
        if (Application.isPlaying) { Debug.LogWarning("Stop Play mode before installing the UI."); return; }
        string previous = SceneManager.GetActiveScene().path;
        font = WWG.WwgFonts.Body;
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null) RepairSlots();
        int installed = 0;
        foreach (string path in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Canvas canvas = null;
            S0Controller s0 = null;
            S1Controller s1 = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (canvas == null) canvas = root.GetComponentInChildren<Canvas>(true);
                if (s0 == null) s0 = root.GetComponentInChildren<S0Controller>(true);
                if (s1 == null) s1 = root.GetComponentInChildren<S1Controller>(true);
            }
            if (canvas == null || (s0 == null && s1 == null))
            {
                Debug.LogError("Gameplay overlay skipped; missing Canvas or controller: " + path);
                continue;
            }
            var overlays = new List<Transform>();
            foreach (Transform child in canvas.transform)
                if (child.GetComponent<GameplayOverlay>() != null) overlays.Add(child);
            var existing = overlays.Count > 0 ? overlays[0] : null;
            bool removedDuplicates = false;
            for (int i = overlays.Count - 1; i >= 1; i--)
            {
                Object.DestroyImmediate(overlays[i].gameObject);
                removedDuplicates = true;
            }
            if (prefab == null && existing != null)
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(existing.gameObject, PrefabPath);
                RepairSlots();
            }
            if (existing != null && PrefabUtility.IsPartOfPrefabInstance(existing))
            {
                if (removedDuplicates) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                continue;
            }
            var oldHud = canvas.transform.Find("HUD");
            if (oldHud != null) oldHud.gameObject.SetActive(false);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            if (prefab == null)
            {
                var built = Build(canvas.transform);
                prefab = PrefabUtility.SaveAsPrefabAsset(built.gameObject, PrefabPath);
                Object.DestroyImmediate(built.gameObject);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(canvas.transform, false);
            instance.transform.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            installed++;
        }
        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        Debug.Log("Installed gameplay overlay in " + installed + " WWG location scenes.");
    }

    private static void RepairSlots()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        bool changed = false;
        foreach (string name in new[] { "Photo slot", "Badge slot" })
        {
            GameObject slot = contents.transform.Find("Inventory dock/" + name).gameObject;
            changed |= GameObjectUtility.RemoveMonoBehavioursWithMissingScript(slot) > 0;
            var component = slot.GetComponent<GameplayInventorySlot>();
            if (component == null) { component = slot.AddComponent<GameplayInventorySlot>(); changed = true; }
            component.item = name == "Photo slot" ? "photo" : "badge";
        }
        if (changed) PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static RectTransform Build(Transform canvas)
    {
        var root = Rect("Gameplay overlay", canvas, 0, 0, 1920, 1080);
        root.gameObject.AddComponent<GameplayOverlay>();
        var top = Box("Top bar", root, 0, 0, 1920, 106, new Color(0.025f, 0.043f, 0.06f, 0.91f), false);
        Box("Top rule", top, 0, 103, 1920, 3, Gold, false);
        Button("Pause button", top, "Ⅱ  暂停  ESC", 24, 19, 186, 67, true);
        Text("Time", top, "周五 15:40", 228, 13, 260, 36, 24, Gold);
        Text("Objective", top, "调查目标", 228, 53, 1070, 40, 26, Ink);
        Text("Status", top, "SAN 100/100   生命 3/3", 1438, 16, 448, 37, 23, Ink, TextAnchor.MiddleRight);
        Button("Record button", top, "任务记录", 1686, 58, 200, 38, false);

        var dock = Box("Inventory dock", root, 590, 949, 740, 111, Panel, true);
        Box("Dock rule", dock, 0, 0, 740, 3, Gold, false);
        Text("Dock title", dock, "物 品 栏", 19, 10, 140, 38, 24, Gold);
        Text("Selection", dock, "暂无物品", 19, 62, 350, 31, 18, Muted);
        Slot("Photo slot", dock, "photo", "照", "旧照片", 395);
        Slot("Badge slot", dock, "badge", "证", "旧工牌", 556);

        var detail = Box("Item detail", root, 1330, 244, 548, 570, Panel, true);
        Box("Detail rule", detail, 0, 0, 548, 4, Gold, false);
        Text("Section", detail, "调查物品  /  资料", 32, 24, 470, 35, 19, Gold);
        Text("Name", detail, "物品名称", 32, 76, 478, 53, 32, Ink);
        var art = Box("Icon frame", detail, 164, 148, 220, 150, new Color(0.12f, 0.18f, 0.21f), false);
        Text("Icon", art, "✧", 0, 25, 220, 95, 65, Gold, TextAnchor.MiddleCenter);
        Text("Body", detail, "", 32, 321, 484, 130, 21, Ink);
        Button("Use", detail, "选择使用", 32, 482, 146, 56, true);
        Button("Inspect", detail, "查看详情", 190, 482, 146, 56, false);
        Button("Close", detail, "返回", 348, 482, 166, 56, false);
        detail.gameObject.SetActive(false);

        var pause = Box("Pause panel", root, 0, 0, 1920, 1080, new Color(0.01f, 0.02f, 0.03f, 0.73f), true);
        var card = Box("Pause card", pause, 86, 172, 562, 736, Panel, true);
        Box("Pause rule", card, 0, 0, 562, 5, Gold, false);
        Text("Heading", card, "暂 停 菜 单", 46, 44, 452, 60, 40, Ink);
        Text("Subheading", card, "熄灯之后  /  48H", 48, 110, 420, 35, 21, Muted);
        Button("Resume", pause, "继续游戏", 133, 367, 468, 62, true);
        Button("Save", pause, "存档", 133, 446, 468, 62, false);
        Button("Load", pause, "读档", 133, 525, 468, 62, false);
        Button("Settings", pause, "游戏设置", 133, 604, 468, 62, false);
        Button("Title", pause, "返回主菜单", 133, 683, 468, 62, false);
        Button("Close", pause, "×", 554, 192, 68, 60, false);
        Text("Hint", pause, "ESC 可继续游戏  ·  地点进度随章节保存", 133, 785, 480, 45, 18, Muted);
        var settings = Box("Settings panel", pause, 728, 288, 544, 480, Panel, true);
        Box("Settings rule", settings, 0, 0, 544, 4, Gold, false);
        Text("Settings heading", settings, "游戏设置", 42, 38, 450, 55, 34, Ink);
        Text("Volume value", settings, "主音量  100%", 42, 146, 448, 48, 25, Ink);
        Button("Volume", settings, "切换音量  100 / 50 / 0", 42, 223, 460, 62, true);
        Button("Back", settings, "返回暂停菜单", 42, 353, 460, 62, false);
        settings.gameObject.SetActive(false);
        pause.gameObject.SetActive(false);

        var toast = Box("Toast", root, 670, 850, 580, 65, new Color(0.04f, 0.07f, 0.09f, 0.92f), false);
        Text("Message", toast, "", 20, 10, 540, 45, 21, Ink, TextAnchor.MiddleCenter);
        toast.gameObject.SetActive(false);
        return root;
    }

    private static void Slot(string name, Transform parent, string item, string icon, string label, float x)
    {
        var slot = Box(name, parent, x, 14, 143, 83, new Color(0.11f, 0.16f, 0.19f), true);
        slot.gameObject.AddComponent<GameplayInventorySlot>().item = item;
        Text("Icon", slot, icon, 7, 7, 61, 68, 37, Gold, TextAnchor.MiddleCenter);
        Text("Label", slot, label, 68, 17, 70, 48, 19, Ink, TextAnchor.MiddleLeft);
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h, Color color, bool raycast)
    {
        var rt = Rect(name, parent, x, y, w, h);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return rt;
    }

    private static Text Text(string name, Transform parent, string value, float x, float y, float w, float h,
        int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rt = Rect(name, parent, x, y, w, h);
        var text = rt.gameObject.AddComponent<Text>();
        text.text = value; text.font = font; text.fontSize = size; text.color = color;
        text.alignment = alignment; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void Button(string name, Transform parent, string label, float x, float y, float w, float h, bool primary)
    {
        var rt = Box(name, parent, x, y, w, h, primary ? new Color(0.22f, 0.30f, 0.32f) : new Color(0.10f, 0.15f, 0.18f), true);
        var button = rt.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.22f, 1.16f, 1.03f);
        colors.pressedColor = new Color(0.74f, 0.79f, 0.78f);
        button.colors = colors;
        Text("Label", rt, label, 12, 3, w - 24, h - 6, primary ? 24 : 22, primary ? Gold : Ink, TextAnchor.MiddleCenter);
    }
}
