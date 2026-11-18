using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WWG;

public static class S4Development
{
    [MenuItem("WWG/Dev/Play S4")]
    public static void Play()
    {
        PlaySample(StoryIds.Choice.RouteLight);
    }

    [MenuItem("WWG/Dev/Play S4 Dark")]
    public static void PlayDark()
    {
        PlaySample(StoryIds.Choice.RouteDark);
    }

    private static void PlaySample(string route)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play 模式。");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景。");
        const string scene = "Assets/Scenes/S4.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            throw new InvalidOperationException("请先执行 WWG/Setup/Build S4 Finale Room。");
        var sample = ChapterPreviewStates.S4(route);
        if (!sample.Validate(out string error)) throw new InvalidOperationException("S4Bootstrap 无效：" + error);
        SessionState.SetString("WWG_S4Bootstrap", JsonUtility.ToJson(sample));
        EditorSceneManager.OpenScene(scene);
        EditorApplication.isPlaying = true;
    }
}
