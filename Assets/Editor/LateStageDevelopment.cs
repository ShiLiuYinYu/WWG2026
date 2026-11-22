using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WWG;

/// <summary>Editor samples for the second 24H (S5–S8). They never write the player checkpoint.</summary>
public static class LateStageDevelopment
{
    [MenuItem("WWG/Dev/Play S5 Replay")]
    public static void PlayS5()
    {
        Play("WWG_S5Bootstrap", ChapterPreviewStates.S5(StoryIds.Choice.RouteLight, StoryIds.Choice.ViewTogether),
            "Assets/Scenes/S5.unity", "WWG/Setup/Build S5 Replay Room");
    }

    [MenuItem("WWG/Dev/Play S6 Light")]
    public static void PlayS6Light()
    {
        Play("WWG_S6Bootstrap", ChapterPreviewStates.S6("S6_Corridor", StoryIds.Choice.RouteLight, StoryIds.Choice.ViewTogether),
            "Assets/Scenes/S6/S6_Corridor.unity", "WWG/Setup/Build S6 Night Walk");
    }

    [MenuItem("WWG/Dev/Play S6 Dark")]
    public static void PlayS6Dark()
    {
        Play("WWG_S6Bootstrap", ChapterPreviewStates.S6("S6_Corridor", StoryIds.Choice.RouteDark, StoryIds.Choice.ViewAlone),
            "Assets/Scenes/S6/S6_Corridor.unity", "WWG/Setup/Build S6 Night Walk");
    }

    [MenuItem("WWG/Dev/Play S7 Light")]
    public static void PlayS7Light()
    {
        Play("WWG_S7Bootstrap", ChapterPreviewStates.S7(StoryIds.Choice.RouteLight, StoryIds.Choice.ViewTogether),
            "Assets/Scenes/S7.unity", "WWG/Setup/Build S7 Reunion Room");
    }

    [MenuItem("WWG/Dev/Play S7 Dark")]
    public static void PlayS7Dark()
    {
        Play("WWG_S7Bootstrap", ChapterPreviewStates.S7(StoryIds.Choice.RouteDark, StoryIds.Choice.ViewAlone),
            "Assets/Scenes/S7.unity", "WWG/Setup/Build S7 Reunion Room");
    }

    [MenuItem("WWG/Dev/Play S8 Letter")]
    public static void PlayS8Letter()
    {
        Play("WWG_S8Bootstrap", ChapterPreviewStates.S8(StoryIds.Choice.RouteLight, StoryIds.Choice.ViewTogether),
            "Assets/Scenes/S8.unity", "WWG/Setup/Build S8 Packaging Room");
    }

    [MenuItem("WWG/Dev/Play S8 Seal")]
    public static void PlayS8Seal()
    {
        Play("WWG_S8Bootstrap", ChapterPreviewStates.S8(StoryIds.Choice.RouteDark, StoryIds.Choice.ViewAlone),
            "Assets/Scenes/S8.unity", "WWG/Setup/Build S8 Packaging Room");
    }

    private static void Play(string sessionKey, GameState sample, string scene, string setupMenu)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("请先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景。");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            throw new InvalidOperationException("请先执行 " + setupMenu + "。");
        if (!sample.Validate(out string error))
            throw new InvalidOperationException(sessionKey + " 无效：" + error);
        SessionState.SetString(sessionKey, JsonUtility.ToJson(sample));
        EditorSceneManager.OpenScene(scene);
        EditorApplication.isPlaying = true;
    }
}
