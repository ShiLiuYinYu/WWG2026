using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WWG;

/// <summary>Runs the second 24H end to end with the automated smoke agent (editor only).</summary>
public static class LateStageSmokeRunner
{
    [MenuItem("WWG/Dev/Smoke Light Together")]
    public static void LightTogether() { Start("light-together", StoryIds.Choice.RouteLight, StoryIds.Choice.ViewTogether); }

    [MenuItem("WWG/Dev/Smoke Light Alone")]
    public static void LightAlone() { Start("light-alone-seal", StoryIds.Choice.RouteLight, StoryIds.Choice.ViewAlone); }

    [MenuItem("WWG/Dev/Smoke Dark Together")]
    public static void DarkTogether() { Start("dark-together", StoryIds.Choice.RouteDark, StoryIds.Choice.ViewTogether); }

    [MenuItem("WWG/Dev/Smoke Dark Alone")]
    public static void DarkAlone() { Start("dark-alone-seal", StoryIds.Choice.RouteDark, StoryIds.Choice.ViewAlone); }

    private const string MatrixKey = "WWG_LateStageSmokeMatrix";

    [MenuItem("WWG/Dev/Smoke All Late Stage Branches")]
    public static void AllBranches()
    {
        LateStageSceneKit.RequireIdle("后 24H 分支冒烟测试");
        SessionState.SetInt(MatrixKey, 0);
        RunNext();
    }

    [InitializeOnLoadMethod]
    private static void ResumeMatrixAfterPlay()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(MatrixKey, -1) >= 0)
                EditorApplication.delayCall += RunNext;
        };
    }

    private static void RunNext()
    {
        int index = SessionState.GetInt(MatrixKey, -1);
        if (index < 0 || index >= 8)
        {
            SessionState.EraseInt(MatrixKey);
            return;
        }

        bool dark = (index & 4) != 0;
        bool alone = (index & 2) != 0;
        bool seal = (index & 1) != 0;
        string name = (dark ? "dark" : "light") + "-" + (alone ? "alone" : "together") + "-" +
            (seal ? "seal" : "letter");
        SessionState.SetInt(MatrixKey, index + 1);
        Start(name, dark ? StoryIds.Choice.RouteDark : StoryIds.Choice.RouteLight,
            alone ? StoryIds.Choice.ViewAlone : StoryIds.Choice.ViewTogether);
    }

    private static void Start(string scenario, string route, string view)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("请先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景。");
        GameState sample = ChapterPreviewStates.S5(route, view);
        if (scenario == "dark-alone-seal") sample.san = 0;
        if (!sample.Validate(out string error)) throw new InvalidOperationException("S5 样本无效：" + error);
        SessionState.SetString("WWG_S5Bootstrap", JsonUtility.ToJson(sample));
        SessionState.SetString("WWG_LateStageSmoke", scenario);
        EditorSceneManager.OpenScene("Assets/Scenes/S5.unity");
        EditorApplication.isPlaying = true;
    }
}
