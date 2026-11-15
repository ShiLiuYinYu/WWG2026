using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PolishUi
{
    const string OverlayPath = "Assets/Prefabs/UI/GameplayOverlay.prefab";
    const string DialoguePath = "Assets/Resources/Dialogue/WwgDialogueHost.prefab";

    [MenuItem("WWG/Polish UI")]
    public static void Polish()
    {
        var fade = Resources.Load<Sprite>("UI/对话框渐变");
        if (fade == null) { Debug.LogError("[Polish] 对话框渐变 加载失败"); return; }

        // 1. 对话框：换成上透明下黑的渐变
        var dlg = PrefabUtility.LoadPrefabContents(DialoguePath);
        Transform panel = dlg.transform.Find("Dialogue root/Dialogue panel");
        if (panel == null) Debug.LogError("[Polish] 找不到 Dialogue root/Dialogue panel");
        else
        {
            var img = panel.GetComponent<Image>();
            if (img == null) img = panel.gameObject.AddComponent<Image>();
            img.sprite = fade;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = false;
            Debug.Log("[Polish] 对话框已换成渐变");
        }
        PrefabUtility.SaveAsPrefabAsset(dlg, DialoguePath);
        PrefabUtility.UnloadPrefabContents(dlg);

        // 2. 顶部任务表：隐藏 Objective
        var hud = PrefabUtility.LoadPrefabContents(OverlayPath);
        Transform obj = hud.transform.Find("Top bar/Objective");
        if (obj == null) Debug.LogError("[Polish] 找不到 Top bar/Objective");
        else { obj.gameObject.SetActive(false); Debug.Log("[Polish] 已隐藏 Top bar/Objective"); }
        PrefabUtility.SaveAsPrefabAsset(hud, OverlayPath);
        PrefabUtility.UnloadPrefabContents(hud);
    }

    [MenuItem("WWG/Build Volume Slider")]
    public static void BuildVolumeSlider()
    {
        var hud = PrefabUtility.LoadPrefabContents(OverlayPath);
        Transform vol = hud.transform.Find("Pause panel/Settings panel/Volume");
        if (vol == null) { Debug.LogError("[Polish] 找不到 Volume"); PrefabUtility.UnloadPrefabContents(hud); return; }

        var rt = vol.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, 20f);
        rt.anchoredPosition = new Vector2(42f, -228f);

        var bg = vol.GetComponent<Image>();
        bg.color = new Color(0.16f, 0.2f, 0.22f, 1f);

        Transform oldLabel = vol.Find("Label");
        if (oldLabel != null) oldLabel.gameObject.SetActive(false);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var frt = fillGo.GetComponent<RectTransform>();
        frt.SetParent(vol, false);
        frt.anchorMin = new Vector2(0f, 0f);
        frt.anchorMax = new Vector2(1f, 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        var fi = fillGo.GetComponent<Image>();
        fi.color = new Color(0.74f, 0.62f, 0.42f, 1f);
        fi.raycastTarget = false;

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var hrt = handleGo.GetComponent<RectTransform>();
        hrt.SetParent(vol, false);
        hrt.sizeDelta = new Vector2(14f, 30f);
        var hi = handleGo.GetComponent<Image>();
        hi.color = new Color(0.94f, 0.9f, 0.82f, 1f);

        var slider = vol.gameObject.GetComponent<Slider>();
        if (slider == null)
        {
            var oldBtn = vol.gameObject.GetComponent<Button>();
            if (oldBtn != null) Object.DestroyImmediate(oldBtn);
            slider = vol.gameObject.AddComponent<Slider>();
        }
        if (slider.fillRect == null) slider.fillRect = frt;
        if (slider.handleRect == null) slider.handleRect = hrt;
        slider.targetGraphic = hi;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 1f;

        PrefabUtility.SaveAsPrefabAsset(hud, OverlayPath);
        PrefabUtility.UnloadPrefabContents(hud);
        Debug.Log("[Polish] 音量滑条已建好");
    }

    [MenuItem("WWG/Polish Dialogue")]
    public static void PolishDialogue()
    {
        var dlg = PrefabUtility.LoadPrefabContents(DialoguePath);
        var root = dlg.transform.Find("Dialogue root");
        if (root == null) { Debug.LogError("[Polish] 找不到 Dialogue root"); PrefabUtility.UnloadPrefabContents(dlg); return; }

        var fade = Resources.Load<Sprite>("UI/对话框渐变");
        var panel = root.Find("Dialogue panel");
        if (panel != null)
        {
            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920f, 260f);
            rt.anchoredPosition = new Vector2(0f, -820f);
            var img = panel.GetComponent<Image>();
            if (img != null && fade != null)
            {
                img.sprite = fade;
                img.type = Image.Type.Simple;
                img.color = Color.white;
                img.raycastTarget = false;
            }
        }

        Transform shade = root.Find("Screen shade");
        if (shade == null)
        {
            var go = new GameObject("Screen shade", typeof(RectTransform), typeof(Image));
            var srt = go.GetComponent<RectTransform>();
            srt.SetParent(root, false);
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            srt.SetSiblingIndex(0);
            var si = go.GetComponent<Image>();
            si.color = new Color(0.01f, 0.02f, 0.03f, 0.42f);
            si.raycastTarget = false;
            shade = go.transform;
        }
        else shade.SetSiblingIndex(0);

        PrefabUtility.SaveAsPrefabAsset(dlg, DialoguePath);
        PrefabUtility.UnloadPrefabContents(dlg);
        Debug.Log("[Polish] 对话框已铺满 + 加暗罩 + 渐变更强");
    }

    [MenuItem("WWG/Move Inventory Dock")]
    public static void MoveInventoryDock()
    {
        var p = PrefabUtility.LoadPrefabContents(OverlayPath);
        var t = p.transform.Find("Inventory dock");
        if (t == null) { Debug.LogError("[Polish] 找不到 Inventory dock"); PrefabUtility.UnloadPrefabContents(p); return; }
        var rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(590f, -715f);
        PrefabUtility.SaveAsPrefabAsset(p, OverlayPath);
        PrefabUtility.UnloadPrefabContents(p);
        Debug.Log("[Polish] 物品栏已上移到对话框之上 y=-715");
    }

    [MenuItem("WWG/Add Text Speed Setting")]
    public static void AddTextSpeedSetting()
    {
        const string path = "Assets/Prefabs/UI/GameplayOverlay.prefab";
        var p = PrefabUtility.LoadPrefabContents(path);
        var settings = p.transform.Find("Pause panel/Settings panel");
        if (settings == null) { Debug.LogError("[Polish] 找不到 Settings panel"); PrefabUtility.UnloadPrefabContents(p); return; }
        if (settings.Find("Text speed") != null)
        {
            Debug.Log("[Polish] 文字速度设置已存在，跳过");
            PrefabUtility.UnloadPrefabContents(p);
            return;
        }

        var vol = settings.Find("Volume");
        var volVal = settings.Find("Volume value");
        if (vol == null || volVal == null) { Debug.LogError("[Polish] 找不到 Volume / Volume value"); PrefabUtility.UnloadPrefabContents(p); return; }

        var spd = UnityEngine.Object.Instantiate(vol.gameObject, settings).transform;
        spd.name = "Text speed";
        var spdVal = UnityEngine.Object.Instantiate(volVal.gameObject, settings).transform;
        spdVal.name = "Text speed value";
        ((RectTransform)spd).anchoredPosition += new Vector2(0f, -96f);
        ((RectTransform)spdVal).anchoredPosition += new Vector2(0f, -96f);

        var slider = spd.GetComponent<Slider>();
        if (slider != null)
        {
            slider.minValue = 5f;
            slider.maxValue = 80f;
            slider.wholeNumbers = true;
            slider.value = 30f;
        }
        var txt = spdVal.GetComponent<Text>();
        if (txt != null) txt.text = "文字速度 30 字/秒";

        PrefabUtility.SaveAsPrefabAsset(p, path);
        PrefabUtility.UnloadPrefabContents(p);
        Debug.Log("[Polish] 已添加文字速度设置");
    }

    [MenuItem("WWG/Extract Settings Panel")]
    public static void ExtractSettingsPanel()
    {
        const string src = "Assets/Prefabs/UI/GameplayOverlay.prefab";
        const string dst = "Assets/Resources/UI/SettingsPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(src);
        var panel = root.transform.Find("Pause panel/Settings panel");
        if (panel == null) { Debug.LogError("[Polish] 找不到 Settings panel"); PrefabUtility.UnloadPrefabContents(root); return; }
        var go = UnityEngine.Object.Instantiate(panel.gameObject);
        go.name = "SettingsPanel";
        go.transform.SetParent(null, false);
        PrefabUtility.SaveAsPrefabAsset(go, dst);
        UnityEngine.Object.DestroyImmediate(go);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();
        Debug.Log("[Polish] 已导出 " + dst);
    }

    [MenuItem("WWG/Dump Dialogue Host")]
    public static void DumpDialogueHost()
    {
        const string path = "Assets/Resources/Dialogue/WwgDialogueHost.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var sb = new System.Text.StringBuilder();
        Walk(root.transform, 0, sb);
        Debug.Log("[Dump]\n" + sb);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void Walk(Transform t, int d, System.Text.StringBuilder sb)
    {
        string extra = "";
        var img = t.GetComponent<UnityEngine.UI.Image>();
        if (img != null) extra += "  <Img " + (img.sprite != null ? img.sprite.name : "null") + " " + img.color + ">";
        sb.AppendLine(new string(' ', d * 2) + t.name + (t.gameObject.activeSelf ? "" : " [off]") + extra);
        foreach (Transform c in t) Walk(c, d + 1, sb);
    }

    [MenuItem("WWG/Show Objective Again")]
    public static void ShowObjective()
    {
        var hud = PrefabUtility.LoadPrefabContents(OverlayPath);
        var obj = hud.transform.Find("Top bar/Objective");
        if (obj != null) obj.gameObject.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(hud, OverlayPath);
        PrefabUtility.UnloadPrefabContents(hud);
        Debug.Log("[Polish] Objective 已恢复显示");
    }
}
