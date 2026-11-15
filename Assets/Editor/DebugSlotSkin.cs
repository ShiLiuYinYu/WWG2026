using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DebugSlotSkin
{
    [MenuItem("WWG/Debug Slot Skin")]
    public static void Run()
    {
        const string path = "Assets/Resources/UI/SaveSlotPanel.prefab";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) { Debug.Log("[Skin] prefab 加载失败: " + path); return; }

        var imgs = go.GetComponentsInChildren<Image>(true);
        Debug.Log("[Skin] Image 数量 = " + imgs.Length);
        foreach (var img in imgs)
        {
            string sp = img.sprite == null ? "NULL" : img.sprite.name;
            string bd = img.sprite == null ? "-" : img.sprite.border.x + "," + img.sprite.border.y + "," + img.sprite.border.z + "," + img.sprite.border.w;
            Debug.Log("[Skin] " + img.gameObject.name + " | sprite=" + sp + " | border=" + bd + " | color=" + img.color + " | type=" + img.type);
        }

        var boxSprite = Resources.Load<Sprite>("UI/选择框");
        var btnSprite = Resources.Load<Sprite>("S0/选项按钮");
        Debug.Log("[Skin] Resources.Load UI/选择框 = " + (boxSprite == null ? "NULL" : boxSprite.name + " border=" + boxSprite.border));
        Debug.Log("[Skin] Resources.Load S0/选项按钮 = " + (btnSprite == null ? "NULL" : btnSprite.name + " border=" + btnSprite.border));
    }
}
