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
public static class TitleMenuWiring
{
    private const string ScenePath = "Assets/Scenes/Title.unity";

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

        Button start = FindButton(canvas.transform, "开始游戏");
        Button load = FindButton(canvas.transform, "读取存档");
        Button settings = FindButton(canvas.transform, "设置");
        Button quit = FindButton(canvas.transform, "退出游戏");
        if (start == null || load == null || settings == null || quit == null)
        {
            Debug.LogError("[Wire] 菜单按钮不完整，接线取消");
            return;
        }

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

    private static Button FindButton(Transform canvas, string label)
    {
        foreach (Button btn in canvas.GetComponentsInChildren<Button>(true))
        {
            string name = btn.gameObject.name;
            if (name.EndsWith(label, StringComparison.Ordinal)) return btn;

            Text txt = btn.GetComponentInChildren<Text>(true);
            if (txt != null && txt.text == label) return btn;
        }
        return null;
    }
}
