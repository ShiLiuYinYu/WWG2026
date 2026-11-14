using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 一次性辅助：看到项目根目录的 _quit.flag 就保存所有打开的场景与资源，然后退出编辑器，
/// 让界面语言这类必须重启才生效的设置真正落地。
/// </summary>
[InitializeOnLoad]
public static class SaveAndQuit
{
    private static readonly string FlagPath =
        Path.Combine(Directory.GetCurrentDirectory(), "_quit.flag");

    static SaveAndQuit()
    {
        if (!File.Exists(FlagPath)) return;
        EditorApplication.delayCall += () =>
        {
            Debug.Log("[Quit] 收到退出标记，保存场景并退出");
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            try { File.Delete(FlagPath); } catch { }
            EditorApplication.Exit(0);
        };
    }
}
