using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UnifyShades
{
    static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.78f);
    const string Hud = "Assets/Prefabs/UI/GameplayOverlay.prefab";
    const string Save = "Assets/Resources/UI/SaveSlotPanel.prefab";
    const string Dlg = "Assets/Resources/Dialogue/WwgDialogueHost.prefab";
    const string Set = "Assets/Resources/UI/SettingsPanel.prefab";

    [MenuItem("WWG/Unify Shades")]
    public static void Run()
    {
        int n = 0;
        n += Tint(Save, "Screen shade");
        n += Tint(Dlg, "Screen shade");
        n += Tint(Set, "Screen shade");
        n += AddHud();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Shade] 完成，改动 " + n + " 处");
    }

    static int Tint(string path, string name)
    {
        if (!System.IO.File.Exists(path)) { Debug.LogWarning("[Shade] 缺文件 " + path); return 0; }
        var root = PrefabUtility.LoadPrefabContents(path);
        int c = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != name) continue;
            var img = t.GetComponent<Image>();
            if (img == null) continue;
            img.color = ShadeColor;
            img.raycastTarget = false;
            c++;
        }
        if (c > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[Shade] " + path + " 改了 " + c + " 个 " + name);
        return c;
    }

    static int AddHud()
    {
        var root = PrefabUtility.LoadPrefabContents(Hud);
        var old = root.transform.Find("Shade");
        if (old != null)
        {
            var oi = old.GetComponent<Image>();
            if (oi != null) oi.color = ShadeColor;
            PrefabUtility.SaveAsPrefabAsset(root, Hud);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[Shade] HUD Shade 已存在，更新颜色");
            return 1;
        }
        var go = new GameObject("Shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(root.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = ShadeColor;
        img.raycastTarget = false;
        go.transform.SetSiblingIndex(0);
        go.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, Hud);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[Shade] HUD 新建 Shade");
        return 1;
    }

    [MenuItem("WWG/Extract Settings Panel v2")]
    public static void ExtractSettings()
    {
        var root = PrefabUtility.LoadPrefabContents(Hud);
        var panel = root.transform.Find("Pause panel/Settings panel");
        if (panel == null)
        {
            Debug.LogError("[Shade] 找不到 Pause panel/Settings panel");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }
        var clone = Object.Instantiate(panel.gameObject);
        clone.name = "SettingsPanel";
        clone.transform.SetParent(null, false);
        var saved = PrefabUtility.SaveAsPrefabAsset(clone, Set);
        if (saved == null)
        {
            var tmp = new GameObject("SettingsPanelRoot");
            clone.transform.SetParent(tmp.transform, false);
            saved = PrefabUtility.SaveAsPrefabAsset(tmp, Set);
            Object.DestroyImmediate(tmp);
        }
        Object.DestroyImmediate(clone);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();
        Debug.Log(saved == null ? "[Shade] 导出失败" : "[Shade] 已导出 " + Set);
    }
}
