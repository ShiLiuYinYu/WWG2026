using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FixSlotSkin
{
    const string PrefabPath = "Assets/Resources/UI/SaveSlotPanel.prefab";

    [MenuItem("WWG/Fix Slot Skin")]
    public static void Fix()
    {
        var btn = Resources.Load<Sprite>("S0/选项按钮");
        if (btn == null)
        {
            Debug.LogError("[FixSkin] 贴图加载失败");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int n = 0;
        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            string name = img.gameObject.name;
            if (name == "Screen shade" || name == "Confirm rule") continue;

            img.sprite = btn;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            n++;
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[FixSkin] 全部改用选项按钮，共写入 " + n + " 个 Image");
    }
}
