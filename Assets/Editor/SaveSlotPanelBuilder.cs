using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>One-time authoring of the save screen shared by title and gameplay.</summary>
public static class SaveSlotPanelBuilder
{
    private const string Path = "Assets/Resources/UI/SaveSlotPanel.prefab";
    private static readonly Color Paper = new Color(0.91f, 0.94f, 0.96f, 1f);
    private static readonly Color Panel = new Color(0.79f, 0.85f, 0.88f, 1f);
    private static readonly Color Ink = new Color(0.08f, 0.15f, 0.20f, 1f);
    private static readonly Color Accent = new Color(0.15f, 0.29f, 0.35f, 1f);
    private static Font font;

    [MenuItem("WWG/UI/Build Save Slot Panel")]
    public static void Build()
    {
        if (File.Exists(Path)) throw new IOException("存档界面预制体已存在，请直接编辑：" + Path);
        Directory.CreateDirectory("Assets/Resources/UI");
        font = WWG.WwgFonts.Body;
        GameObject root = new GameObject("SaveSlotPanel", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SaveSlotPanel));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        SaveSlotPanel panel = root.GetComponent<SaveSlotPanel>();
        RectTransform shade = Box(root.transform, "Screen shade", 0, 0, 1920, 1080,
            new Color(0.015f, 0.03f, 0.045f, 0.88f));
        shade.anchorMin = Vector2.zero;
        shade.anchorMax = Vector2.one;
        shade.offsetMin = shade.offsetMax = Vector2.zero;
        Transform card = Rect(root.transform, "Save card", 370, 95, 1180, 890);
        panel.backButton = Button(card, "Back", "返回", 46, 41, 154, 68, false);
        panel.heading = Label(card, "Heading", "读取存档", 270, 40, 650, 72, 46, Paper, TextAnchor.MiddleCenter);
        panel.hint = Label(card, "Hint", "", 80, 133, 1020, 68, 26, Paper, TextAnchor.MiddleCenter);
        panel.slotButtons = new Button[SaveSlotStore.SlotCount];
        panel.overwriteButtons = new Button[SaveSlotStore.SlotCount];
        panel.deleteButtons = new Button[SaveSlotStore.SlotCount];
        panel.slotTitles = new Text[SaveSlotStore.SlotCount];
        panel.slotDetails = new Text[SaveSlotStore.SlotCount];
        panel.slotActions = new Text[SaveSlotStore.SlotCount];
        for (int i = 0; i < SaveSlotStore.SlotCount; i++)
        {
            float y = 235 + i * 190;
            Button row = Button(card, "Slot " + (i + 1), "", 70, y, 820, 160, true);
            panel.slotButtons[i] = row;
            panel.slotTitles[i] = Label(row.transform, "Title", "", 32, 18, 740, 55, 33, Ink, TextAnchor.MiddleLeft);
            panel.slotDetails[i] = Label(row.transform, "Time", "", 32, 65, 720, 78, 25, Accent, TextAnchor.MiddleLeft);
            panel.slotActions[i] = Label(row.transform, "Action", "", 630, 82, 150, 40, 23, Accent, TextAnchor.MiddleRight);
            panel.overwriteButtons[i] = Button(card, "Overwrite " + (i + 1), "覆盖", 918, y + 8, 186, 66, false);
            panel.deleteButtons[i] = Button(card, "Delete " + (i + 1), "删除", 918, y + 86, 186, 66, false);
        }
        panel.latestButton = Button(card, "Latest checkpoint", "", 330, 815, 520, 54, false);
        panel.latestLabel = Label(panel.latestButton.transform, "Label", "读取最近检查点", 10, 4, 500, 46, 25, Accent, TextAnchor.MiddleCenter);
        panel.confirmationPanel = Box(root.transform, "Confirmation", 610, 325, 700, 430, Panel).gameObject;
        Box(panel.confirmationPanel.transform, "Confirm rule", 0, 0, 700, 6, Accent, false);
        panel.confirmationText = Label(panel.confirmationPanel.transform, "Message", "", 45, 80, 610, 190, 34, Ink, TextAnchor.MiddleCenter);
        panel.yesButton = Button(panel.confirmationPanel.transform, "Yes", "是", 90, 320, 220, 72, true);
        panel.noButton = Button(panel.confirmationPanel.transform, "No", "否", 390, 320, 220, 72, false);
        panel.confirmationPanel.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, Path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[WWG] 已建立三槽位存档界面：" + Path);
    }

    private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }
    private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h,
        Color color, bool raycast = true)
    {
        RectTransform rt = Rect(parent, name, x, y, w, h);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return rt;
    }
    private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h,
        int size, Color color, TextAnchor anchor)
    {
        RectTransform rt = Rect(parent, name, x, y, w, h);
        Text label = rt.gameObject.AddComponent<Text>();
        label.text = value;
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = anchor;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }
    private static Button Button(Transform parent, string name, string title, float x, float y, float w, float h, bool main)
    {
        RectTransform rt = Box(parent, name, x, y, w, h, main ? Panel : Paper);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.78f, 0.88f, 0.89f, 1f);
        colors.pressedColor = new Color(0.66f, 0.77f, 0.79f, 1f);
        colors.disabledColor = main ? Panel : Paper;
        button.colors = colors;
        if (!string.IsNullOrEmpty(title)) Label(rt, "Label", title, 8, 4, w - 16, h - 8, 29, Ink, TextAnchor.MiddleCenter);
        return button;
    }
}
