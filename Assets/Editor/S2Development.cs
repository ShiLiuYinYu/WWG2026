using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WWG;

public static class S2Development
{
    [MenuItem("WWG/Dev/Play S2")]
    public static void Play()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play 模式。");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景。");
        const string scene = "Assets/Scenes/S2.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            throw new InvalidOperationException("请先执行 WWG/Setup/Build S2 Location Scenes。");
        var sample = ChapterPreviewStates.S2("S2");
        if (!sample.Validate(out string error)) throw new InvalidOperationException("S2Bootstrap 无效：" + error);
        SessionState.SetString("WWG_S2Bootstrap", JsonUtility.ToJson(sample));
        EditorSceneManager.OpenScene(scene);
        EditorApplication.isPlaying = true;
    }
}
