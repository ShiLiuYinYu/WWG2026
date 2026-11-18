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
        const string scene = "Assets/Scenes/Locations/FinaleClubRoom.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            throw new InvalidOperationException("请先执行 WWG/Setup/Build S4 Finale Room。");
        var sample = new GameState
        {
            chapterId = StoryIds.Chapter.Finale,
            stepId = "arrival",
            locationId = StoryIds.Location.ClubRoom,
            locationVariant = StoryIds.Variant.Night,
            timeId = "saturday.0200",
            san = 65,
            health = 2
        };
        sample.SetFlag(StoryIds.Flag.P1Complete);
        sample.SetFlag(StoryIds.Flag.P2DrawerTaken);
        sample.SetFlag(StoryIds.Flag.P3Recovered);
        sample.SetFlag(StoryIds.Flag.P3MemoHeard);
        sample.SetFlag(StoryIds.Flag.P3Copied);
        sample.AddItem(StoryIds.Item.Photo);
        sample.AddItem(StoryIds.Item.Badge);
        sample.AddItem(StoryIds.Item.MachineNote);
        sample.AddItem(StoryIds.Item.TeamSheet);
        sample.AddItem(StoryIds.Item.OldProjectUsb);
        sample.ChooseRoute(route);
        if (!sample.Validate(out string error)) throw new InvalidOperationException("S4Bootstrap 无效：" + error);
        SessionState.SetString("WWG_S4Bootstrap", JsonUtility.ToJson(sample));
        EditorSceneManager.OpenScene(scene);
        EditorApplication.isPlaying = true;
    }
}
