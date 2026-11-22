using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using WWG;

/// <summary>One-time authoring of the shared dialogue host prefab and cast asset.</summary>
public static class DialogueHostBuilder
{
    private const string Folder = "Assets/Resources/Dialogue";
    private const string PrefabPath = Folder + "/WwgDialogueHost.prefab";
    private const string CastPath = Folder + "/WwgDialogueCast.asset";
    private const string ProjectPath = "Assets/Yarn/WWG.yarnproject";

    private static readonly Color Panel = new Color(0.035f, 0.055f, 0.075f, 0.93f);
    private static readonly Color Paper = new Color(0.87f, 0.92f, 0.93f, 1f);
    private static readonly Color Accent = new Color(0.56f, 0.82f, 0.86f, 1f);
    private static readonly Color Ink = new Color(0.08f, 0.15f, 0.20f, 1f);
    private static Font font;

    [MenuItem("WWG/Dialogue/Build Dialogue Host")]
    public static void Build()
    {
        if (File.Exists(PrefabPath)) throw new IOException("对话宿主预制体已存在，请直接编辑：" + PrefabPath);
        YarnProject project = AssetDatabase.LoadAssetAtPath<YarnProject>(ProjectPath);
        if (project == null) throw new InvalidOperationException("找不到 Yarn 工程：" + ProjectPath);
        if (project.Program == null) throw new InvalidOperationException("Yarn 工程尚未编译成功，请先检查 Console。");

        Directory.CreateDirectory(Folder);
        font = WWG.WwgFonts.Body;
        DialogueCast cast = LoadOrCreateCast();

        GameObject root = new GameObject("WwgDialogueHost", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DialogueService), typeof(DialogueRunner),
            typeof(DialogueCommands));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject dialogueRoot = Stretch(root.transform, "Dialogue root").gameObject;
        RectTransform panel = Box(dialogueRoot.transform, "Dialogue panel", 45, 820, 1830, 230, Panel, false);
        Image portrait = Box(panel, "Speaker portrait", 30, -75, 245, 305, Color.white, false).GetComponent<Image>();
        RectTransform portraitRect = portrait.rectTransform;
        portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = Vector2.zero;
        portraitRect.anchoredPosition = new Vector2(30, 0);
        portrait.preserveAspect = true;
        portrait.enabled = false;
        Text speaker = Label(panel, "Speaker", "", 36, 18, 390, 48, 31, Accent, TextAnchor.MiddleLeft);
        Text line = Label(panel, "Line", "", 36, 68, 1745, 115, 32, Paper, TextAnchor.UpperLeft);
        Label(panel, "Continue hint", "点击继续  ▸", 1500, 186, 280, 30, 22, Accent, TextAnchor.MiddleRight);

        RectTransform advance = Stretch(dialogueRoot.transform, "Advance");
        Image advanceImage = advance.gameObject.AddComponent<Image>();
        advanceImage.color = new Color(1f, 1f, 1f, 0.001f);
        Button advanceButton = advance.gameObject.AddComponent<Button>();
        advanceButton.targetGraphic = advanceImage;
        advanceButton.transition = Selectable.Transition.None;
        dialogueRoot.SetActive(false);

        GameObject documentRoot = Stretch(root.transform, "Document panel").gameObject;
        RectTransform shade = Stretch(documentRoot.transform, "Screen shade");
        Image shadeImage = shade.gameObject.AddComponent<Image>();
        shadeImage.color = new Color(0.015f, 0.03f, 0.045f, 0.82f);
        RectTransform card = Box(documentRoot.transform, "Document card", 370, 130, 1180, 820, Paper, true);
        Box(card, "Rule", 0, 0, 1180, 6, Accent, false);
        Text title = Label(card, "Title", "", 60, 34, 1060, 70, 40, Ink, TextAnchor.MiddleLeft);
        Text body = Label(card, "Body", "", 60, 120, 1060, 560, 28, Ink, TextAnchor.UpperLeft);
        Button close = Button(card, "Close", "关闭", 880, 710, 240, 72, true);
        documentRoot.SetActive(false);

        WwgDialoguePresenter presenter = root.AddComponent<WwgDialoguePresenter>();
        presenter.dialogueRoot = dialogueRoot;
        presenter.speakerText = speaker;
        presenter.bodyText = line;
        presenter.portraitImage = portrait;
        presenter.cast = cast;

        WwgDocumentPanel documentPanel = root.AddComponent<WwgDocumentPanel>();
        documentPanel.panelRoot = documentRoot;
        documentPanel.titleText = title;
        documentPanel.bodyText = body;
        documentPanel.closeButton = close;

        dialogueRoot.transform.SetSiblingIndex(0);
        documentRoot.transform.SetSiblingIndex(1);

        DialogueRunner runner = root.GetComponent<DialogueRunner>();
        InMemoryVariableStorage storage = root.AddComponent<InMemoryVariableStorage>();
        SerializedObject runnerData = new SerializedObject(runner);
        runnerData.FindProperty("yarnProject").objectReferenceValue = project;
        runnerData.FindProperty("variableStorage").objectReferenceValue = storage;
        runnerData.ApplyModifiedPropertiesWithoutUndo();

        DialogueService service = root.GetComponent<DialogueService>();
        SerializedObject serviceData = new SerializedObject(service);
        serviceData.FindProperty("yarnProject").objectReferenceValue = project;
        serviceData.FindProperty("runner").objectReferenceValue = runner;
        serviceData.FindProperty("presenter").objectReferenceValue = presenter;
        serviceData.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(advanceButton.onClick, presenter.RequestAdvance);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[WWG] 已建立对话宿主：" + PrefabPath + "，立绘配置：" + CastPath);
    }

    private static DialogueCast LoadOrCreateCast()
    {
        DialogueCast cast = AssetDatabase.LoadAssetAtPath<DialogueCast>(CastPath);
        if (cast != null) return cast;
        cast = ScriptableObject.CreateInstance<DialogueCast>();
        cast.entries = new List<DialogueCast.Entry>
        {
            Entry("袁祁", LoadSprite("YuanQi")),
            Entry("唐澈", LoadSprite("TangChe")),
            Entry("许圆", LoadSprite("XuYuan"))
        };
        AssetDatabase.CreateAsset(cast, CastPath);
        AssetDatabase.SaveAssets();
        return cast;
    }

    private static DialogueCast.Entry Entry(string speaker, Sprite portrait) => new DialogueCast.Entry
    {
        speaker = speaker,
        portrait = portrait,
        speakerColor = Accent
    };

    private static Sprite LoadSprite(string name)
    {
        string path = "Assets/Resources/S0/" + name + ".png";
        Sprite first = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (!(asset is Sprite sprite)) continue;
            if (first == null) first = sprite;
            if (sprite.name == name || sprite.name == name + "_0") return sprite;
        }
        return first ?? AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform Stretch(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
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
        Label(rt, "Label", title, 8, 4, w - 16, h - 8, 29, main ? Paper : Ink, TextAnchor.MiddleCenter);
        return button;
    }
}
