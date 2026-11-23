using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>Copies the authored 5F view once, preserving its room layers and shared UI layout.</summary>
public static class S4SceneBuilder
{
    private const string Source = "Assets/Scenes/S2/S2_ClubRoom.unity";
    private const string Destination = "Assets/Scenes/S4.unity";
    private static readonly Color Paper = new Color(0.96f, 0.91f, 0.78f);
    private static readonly Color Accent = new Color(0.95f, 0.77f, 0.37f);
    private static Font font;

    [MenuItem("WWG/Setup/Build S4 Finale Room")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先保存当前场景。");
        if (File.Exists(Destination)) throw new InvalidOperationException("S4 场景已存在；停止以保护人工布局。");
        if (!File.Exists(Source)) throw new InvalidOperationException("缺少现有 5F 活动室场景。");
        string previous = SceneManager.GetActiveScene().path;
        if (!AssetDatabase.CopyAsset(Source, Destination)) throw new InvalidOperationException("复制 S4 活动室失败。");
        try
        {
            Scene scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
            var old = UnityEngine.Object.FindFirstObjectByType<S2Controller>(FindObjectsInactive.Include);
            if (old == null) throw new InvalidOperationException("源活动室缺少 S2Controller。");
            Transform root = old.transform;
            root.name = "S4 Finale ClubRoom";
            Transform canvas = root.Find("S2 Canvas");
            if (canvas == null) throw new InvalidOperationException("源活动室缺少 Canvas。");

            // All S2 callbacks belong to the copied scene. S4 binds its own callbacks at runtime.
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEngine.Object.DestroyImmediate(old);
            foreach (string name in new[] { "Emergency lighting", "Safe room marker", "Rest hotspot",
                         "Down to bridge", "Feedback", "Four digit cabinet" })
            {
                Transform target = canvas.Find(name);
                if (target != null) UnityEngine.Object.DestroyImmediate(target.gameObject);
            }

            var c = root.gameObject.AddComponent<S4Controller>();
            Transform art = canvas.Find("Location artwork");
            if (art == null) throw new InvalidOperationException("源活动室缺少画面层。");
            c.roomImage = art.Find("Room Lower")?.GetComponent<Image>();
            c.dayRoomSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Resources/S0/ClubRoomLayers/活动室白天下层.png");
            if (c.roomImage == null || c.dayRoomSprite == null)
                throw new InvalidOperationException("缺少活动室白天窗景。");
            c.locationText = canvas.Find("Location")?.GetComponent<Text>();
            c.timeText = canvas.Find("Top bar/Time")?.GetComponent<Text>();
            c.objectiveText = canvas.Find("Objective")?.GetComponent<Text>();
            c.vitals = canvas.Find("Vitals")?.GetComponent<VitalsBars>();
            c.infoPanel = canvas.Find("Inspect")?.gameObject;
            c.infoTitle = c.infoPanel?.transform.Find("Title")?.GetComponent<Text>();
            c.infoBody = c.infoPanel?.transform.Find("Body")?.GetComponent<Text>();
            c.recordPanel = canvas.Find("Task record")?.gameObject;
            c.recordText = c.recordPanel?.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(t => t.name == "Record");
            c.inventoryPanel = canvas.GetComponentsInChildren<S2InventoryGrid>(true).FirstOrDefault()?.gameObject;
            c.inventoryText = c.inventoryPanel?.transform.Find("Items")?.GetComponent<Text>();
            c.pausePanel = canvas.Find("Pause")?.gameObject;
            if (c.locationText == null || c.timeText == null || c.objectiveText == null ||
                c.vitals == null || c.infoBody == null || c.recordText == null ||
                c.inventoryPanel == null || c.pausePanel == null)
                throw new InvalidOperationException("源活动室通用 UI 结构不完整。");
            c.locationText.gameObject.SetActive(false);
            c.timeText.gameObject.SetActive(false);
            c.objectiveText.gameObject.SetActive(false);
            font = WWG.WwgFonts.Body;

            Text hover = Label(canvas, "S4 Hover hint", "", 510, 825, 900, 62, 30, Accent, TextAnchor.MiddleCenter);
            hover.gameObject.SetActive(false);
            c.badgeButton = Hotspot(canvas, "S4 Badge box", 1611, 224, 248, 173, "社团纪念盒 · 归档林未工牌", hover);
            c.restButton = Hotspot(canvas, "S4 Rest chair", 1020, 620, 280, 395, "活动室座位 · 休息", hover);
            c.computerButton = Hotspot(canvas, "S4 Computer", 590, 555, 365, 190, "周六_16:00.scene · 打开旧工程", hover);

            c.choicePanel = Rect(canvas, "S4 Choice", 0, 0, 930, 335);
            RectTransform choices = (RectTransform)c.choicePanel.transform;
            choices.anchorMin = choices.anchorMax = choices.pivot = new Vector2(.5f, .5f);
            choices.anchoredPosition = Vector2.zero;
            Sprite choiceSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/选项按钮.png");
            Button(choices, "Together", "四个人一起看 · 有福同享有难同当", 0, 0, 930, 95, choiceSprite);
            Button(choices, "Alone", "我先点开探路 · 你们站远点（SAN -10）", 0, 120, 930, 95, choiceSprite);
            Button(choices, "Close", "稍后再决定", 0, 240, 930, 95, choiceSprite);
            c.choicePanel.SetActive(false);

            c.blackout = Panel(canvas, "S4 Blackout", 0, 0, 0, 0, Color.black);
            RectTransform blackout = (RectTransform)c.blackout.transform;
            blackout.anchorMin = Vector2.zero;
            blackout.anchorMax = Vector2.one;
            blackout.pivot = new Vector2(.5f, .5f);
            blackout.offsetMin = blackout.offsetMax = Vector2.zero;
            c.blackout.SetActive(false);

            InstallVisuals(c);

            EditorSceneManager.SaveScene(scene, Destination);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == Destination))
            {
                scenes.Add(new EditorBuildSettingsScene(Destination, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[WWG] 已创建 S4 活动室并加入 Build Settings。");
        }
        catch
        {
            // Keep the copied scene for inspection on failure; never alter the authored source.
            throw;
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }

    [MenuItem("WWG/UI/Install S4 Visuals")]
    public static void InstallExistingVisuals()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先保存当前场景。");
        if (!File.Exists(Destination)) throw new InvalidOperationException("S4 场景尚未创建。");
        string previous = SceneManager.GetActiveScene().path;
        try
        {
            Scene scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
            S4Controller c = UnityEngine.Object.FindFirstObjectByType<S4Controller>(FindObjectsInactive.Include);
            if (c == null) throw new InvalidOperationException("S4Controller 缺失。");
            InstallVisuals(c);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }

    private static void InstallVisuals(S4Controller c)
    {
        Transform art = c.roomImage.transform.parent;
        Transform existing = art.Find("S4 Day Window");
        if (existing == null)
        {
            GameObject window = UnityEngine.Object.Instantiate(c.roomImage.gameObject, art);
            window.name = "S4 Day Window";
            window.transform.SetAsLastSibling();
            Image image = window.GetComponent<Image>();
            image.sprite = c.dayRoomSprite;
            image.raycastTarget = false;
            window.SetActive(false);
            c.dayWindow = window;
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = windowRect.anchorMax = new Vector2(0, 1);
            windowRect.pivot = new Vector2(0, 1);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(1920, 1080);
            window.transform.SetSiblingIndex(c.roomImage.transform.GetSiblingIndex() + 1);
        }
        else c.dayWindow = existing.gameObject;

        Sprite boxSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/ClubRoomLayers/箱子亮.png");
        Transform box = art.Find("S4 Memorial Box");
        if (box == null && boxSprite != null)
        {
            GameObject boxObject = Rect(art, "S4 Memorial Box", 1611, 224, 248, 173);
            Image boxImage = boxObject.AddComponent<Image>();
            boxImage.sprite = boxSprite;
            boxImage.raycastTarget = false;
            box = boxObject.transform;
        }
        Transform outline = art.Find("S4 Memorial Box Outline");
        if (outline == null && box != null)
        {
            GameObject outlineObject = UnityEngine.Object.Instantiate(box.gameObject, art);
            outlineObject.name = "S4 Memorial Box Outline";
            Image outlineImage = outlineObject.GetComponent<Image>();
            outlineImage.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UI/BoxHoverOutline.mat");
            outlineImage.raycastTarget = false;
            outlineImage.enabled = false;
            outline = outlineObject.transform;
        }
        if (outline != null && c.badgeButton != null)
        {
            var hover = c.badgeButton.GetComponent<HotspotHoverOutline>();
            if (hover == null)
            {
                hover = c.badgeButton.gameObject.AddComponent<HotspotHoverOutline>();
                var serialized = new SerializedObject(hover);
                serialized.FindProperty("highlight").objectReferenceValue = outline.GetComponent<Image>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                S4HotspotHover hint = c.badgeButton.GetComponent<S4HotspotHover>();
                if (hint != null) { hint.image = null; hint.hint = null; }
            }
        }

    }

    private static GameObject Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
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
    private static Text Label(Transform parent, string name, string content, float x, float y,
        float w, float h, int size, Color color, TextAnchor anchor)
    {
        GameObject go = Rect(parent, name, x, y, w, h);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.text = content;
        text.raycastTarget = false;
        return text;
    }
    private static Button Button(Transform parent, string name, string caption, float x, float y, float w, float h,
        Sprite sprite = null)
    {
        Color background = sprite != null ? Color.white : new Color(0.18f, 0.22f, 0.24f, 0.96f);
        GameObject go = Panel(parent, name, x, y, w, h, background);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        Color textColor = sprite != null ? new Color(.95f, .88f, .72f) : Paper;
        Label(go.transform, "Caption", caption, 10, 5, w - 20, h - 10, 32, textColor, TextAnchor.MiddleCenter);
        return button;
    }
    private static Button Hotspot(Transform parent, string name, float x, float y, float w, float h,
        string caption, Text hover)
    {
        GameObject go = Panel(parent, name, x, y, w, h, new Color(1, 1, 1, 0.001f));
        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        var cue = go.AddComponent<S4HotspotHover>();
        cue.hint = hover;
        cue.caption = caption;
        cue.image = go.GetComponent<Image>();
        return button;
    }
}
