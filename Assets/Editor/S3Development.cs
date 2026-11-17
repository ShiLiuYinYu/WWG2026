using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WWG;

public static class S3Development
{
    [MenuItem("WWG/Dev/Play S3")]
    public static void Play()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play 模式。");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景。");
        const string scene = "Assets/Scenes/Locations/ServerRoom3F.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            throw new InvalidOperationException("请先执行 WWG/Setup/Build S3 Server Room。");
        var sample = new GameState
        {
            chapterId = StoryIds.Chapter.ServerRecovery,
            stepId = "p3Start",
            locationId = StoryIds.Location.ServerRoom3F,
            locationVariant = StoryIds.Variant.Night,
            timeId = "saturday.0100",
            san = 75,
            health = 3
        };
        sample.SetFlag(StoryIds.Flag.P1Complete);
        sample.SetFlag(StoryIds.Flag.P2DrawerTaken);
        sample.AddItem(StoryIds.Item.Photo);
        sample.AddItem(StoryIds.Item.Badge);
        sample.AddItem(StoryIds.Item.MachineNote);
        sample.AddItem(StoryIds.Item.TeamSheet);
        sample.ChooseRoute(StoryIds.Choice.RouteLight);
        if (!sample.Validate(out string error)) throw new InvalidOperationException("S3Bootstrap 无效：" + error);
        SessionState.SetString("WWG_S3Bootstrap", JsonUtility.ToJson(sample));
        EditorSceneManager.OpenScene(scene);
        EditorApplication.isPlaying = true;
    }
}
