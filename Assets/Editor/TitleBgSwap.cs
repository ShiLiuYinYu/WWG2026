using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把主页面 Title 场景的背景（Cover）换成 Assets/Resources/Title/白天.png，跑一次就自动失效。
/// 注意：不能用 EditorApplication.delayCall —— Unity 失去焦点时 delayCall 常常不执行，
/// 必须挂在 EditorApplication.update 上自己数帧。
/// </summary>
[InitializeOnLoad]
public static class TitleBgSwap
{
    private const string ImgPath = "Assets/Resources/Title/白天.png";
    private const string ScenePath = "Assets/Scenes/Title.unity";
    private const string Flag = "WWG.TitleBgSwap.v1";

    private static double _nextAt;
    private static int _phase; // 0=待改导入 1=待换图 2=完成

    static TitleBgSwap()
    {
        _nextAt = 0d;
        _phase = 0;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (_phase >= 2) return;
        if (EditorApplication.timeSinceStartup < _nextAt) return;
        _nextAt = EditorApplication.timeSinceStartup + 0.5;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorPrefs.GetBool(Flag, false)) { _phase = 2; return; }

        if (_phase == 0)
        {
            var ti = AssetImporter.GetAtPath(ImgPath) as TextureImporter;
            if (ti == null) { Debug.LogError("[BgSwap] 找不到 " + ImgPath); _phase = 2; return; }

            if (ti.spriteImportMode != SpriteImportMode.Single || ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
                Debug.Log("[BgSwap] 导入模式已改为 Single，等待重新导入");
                _phase = 1;
                _nextAt = EditorApplication.timeSinceStartup + 2.0;
                return;
            }
            _phase = 1;
            return;
        }

        DoSwap();
    }

    private static void DoSwap()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ImgPath);
        if (sprite == null)
        {
            Debug.Log("[BgSwap] sprite 还是空的，下轮再试");
            return;
        }

        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int n = 0;
        foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!img.gameObject.name.StartsWith("Cover")) continue;
            img.sprite = sprite;
            img.preserveAspect = false;
            EditorUtility.SetDirty(img);
            n++;
            Debug.Log($"[BgSwap] 已替换 {img.gameObject.name} -> {sprite.name} ({sprite.rect.width}x{sprite.rect.height})");
        }

        if (n == 0) Debug.LogWarning("[BgSwap] 场景里没找到 Cover");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorPrefs.SetBool(Flag, true);
        _phase = 2;
        Debug.Log($"[BgSwap] 完成，共替换 {n} 个");
    }
}
