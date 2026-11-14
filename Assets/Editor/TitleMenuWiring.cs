using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>
/// 一次性接线：把 TitleMenuController 挂到 Title Canvas 上，
/// 并按按钮文字自动连到「开始游戏 / 读取存档 / 设置 / 退出游戏」。
/// 不修改任何 RectTransform 尺寸和位置。
/// </summary>
[InitializeOnLoad]
public static class TitleMenuWiring
{
    private const string ScenePath = "Assets/Scenes/Title.unity";
    private static bool _done;

    static TitleMenuWiring()
    {
        Debug.Log("[Wire] 静态构造执行");
        // 幂等：每次域重载都排一次，用 update 计数代替 delayCall，
        // 避免重载阶段注册的 delayCall 被丢弃。
        _done = false;
        int ticks = 0;
        EditorApplication.UpdateCallback handler = null;
        handler = () =>
        {
            if (_done || ++ticks < 40) return;
            EditorApplication.update -= handler;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            _done = true;
            Wire();
        };
        EditorApplication.update += handler;
    }

    [MenuItem("WWG/Wire Title Menu")]
    public static void Wire()
    {
        Scene active = SceneManager.GetActiveScene();

        // 先保住用户手调的排版。
        if (active.isDirty) EditorSceneManager.SaveOpenScenes();

        if (active.path != ScenePath)
        {
            if (!File.Exists(ScenePath)) { Debug.LogError("[Wire] 场景不存在"); return; }
            active = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject canvas = GameObject.Find("Title Canvas");
        if (canvas == null) { Debug.LogError("[Wire] 找不到 Title Canvas"); return; }

        TitleMenuController controller = canvas.GetComponent<TitleMenuController>();
        if (controller == null) controller = canvas.AddComponent<TitleMenuController>();

        Button start = FindButton("开始游戏");
        Button load = FindButton("读取存档");
        Button settings = FindButton("设置");
        Button quit = FindButton("退出游戏");

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("startButton").objectReferenceValue = start;
        so.FindProperty("loadButton").objectReferenceValue = load;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active, ScenePath);

        Debug.Log($"[Wire] 接线完成 开始={start != null} 读档={load != null} 设置={settings != null} 退出={quit != null}");
    }

    private static Button FindButton(string label)
    {
        foreach (Button btn in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            string name = btn.gameObject.name;
            if (name.EndsWith(label, StringComparison.Ordinal)) return btn;

            Text txt = btn.GetComponentInChildren<Text>(true);
            if (txt != null && txt.text == label) return btn;
        }
        return null;
    }
}
