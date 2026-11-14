using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>一次性把 S0/S1 的地点画面拆为可单独编辑的场景；已有目标场景绝不覆盖。</summary>
public static class LocationSceneSplitter
{
    private const string S0Source = "Assets/Scenes/S0.unity";
    private const string S1Source = "Assets/Scenes/S1.unity";

    private static readonly string[] S0Pages = { "Exterior", "Corridor", "ClubRoom", "Bridge" };
    private static readonly string[] S1Pages = { "5F Activity Room", "5F Corridor", "4F Bridge Entrance" };
    private static readonly string[] S0Scenes = { S0Source, "Assets/Scenes/S0/S0_Corridor.unity",
        "Assets/Scenes/S0/S0_ClubRoom.unity", "Assets/Scenes/S0/S0_Bridge.unity" };
    private static readonly string[] S1Scenes = { S1Source, "Assets/Scenes/S1/S1_Corridor.unity",
        "Assets/Scenes/S1/S1_BridgeEntrance.unity" };

    [MenuItem("WWG/Setup/Split Location Scenes")]
    public static void Split()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("请先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景，再执行地点拆分。");
        foreach (string target in AllNewScenes())
            if (File.Exists(target))
                throw new InvalidOperationException("目标场景已存在，停止以保护手动编辑：" + target);

        string previous = SceneManager.GetActiveScene().path;
        Directory.CreateDirectory("Assets/Scenes/S0");
        Directory.CreateDirectory("Assets/Scenes/S1");
        SplitChapter(S0Source, "S0 Canvas", S0Pages, S0Scenes);
        SplitChapter(S1Source, "S1 Canvas", S1Pages, S1Scenes);
        RegisterBuildScenes();
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        Debug.Log("[WWG] S0 四个地点、S1 三个地点已拆分并登记 Build Settings。");
    }

    private static IEnumerable<string> AllNewScenes()
    {
        for (int i = 1; i < S0Scenes.Length; i++) yield return S0Scenes[i];
        for (int i = 1; i < S1Scenes.Length; i++) yield return S1Scenes[i];
    }

    private static void SplitChapter(string source, string canvasName, string[] pages, string[] scenes)
    {
        Scene original = EditorSceneManager.OpenScene(source, OpenSceneMode.Single);
        ValidatePages(original, canvasName, pages);
        for (int i = 1; i < scenes.Length; i++)
        {
            if (!AssetDatabase.CopyAsset(source, scenes[i]))
                throw new IOException("无法复制场景：" + scenes[i]);
            Scene copy = EditorSceneManager.OpenScene(scenes[i], OpenSceneMode.Single);
            KeepOnlyPage(copy, canvasName, pages, i);
            EditorSceneManager.SaveScene(copy);
        }
        original = EditorSceneManager.OpenScene(source, OpenSceneMode.Single);
        KeepOnlyPage(original, canvasName, pages, 0);
        EditorSceneManager.SaveScene(original);
    }

    private static void ValidatePages(Scene scene, string canvasName, string[] pages)
    {
        Transform canvas = FindRootChild(scene, canvasName);
        if (canvas == null) throw new InvalidOperationException("缺少 Canvas：" + canvasName);
        foreach (string page in pages)
            if (canvas.Find(page) == null) throw new InvalidOperationException("缺少地点画面：" + page);
    }

    private static void KeepOnlyPage(Scene scene, string canvasName, string[] pages, int keep)
    {
        bool hasLight = false;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponent<Light>() != null) hasLight = true;
        if (!hasLight)
        {
            GameObject light = new GameObject("Global Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            SceneManager.MoveGameObjectToScene(light, scene);
        }
        Transform canvas = FindRootChild(scene, canvasName);
        if (canvas == null) throw new InvalidOperationException("缺少 Canvas：" + canvasName);
        for (int i = 0; i < pages.Length; i++)
        {
            Transform page = canvas.Find(pages[i]);
            if (page == null) throw new InvalidOperationException("缺少地点画面：" + pages[i]);
            if (i != keep) UnityEngine.Object.DestroyImmediate(page.gameObject);
        }
    }

    private static Transform FindRootChild(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root.transform;
            Transform child = root.transform.Find(name);
            if (child != null) return child;
        }
        return null;
    }

    private static void RegisterBuildScenes()
    {
        var result = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            result.Add(entry);
            if (entry.path == S0Source)
                for (int i = 1; i < S0Scenes.Length; i++) result.Add(new EditorBuildSettingsScene(S0Scenes[i], true));
            if (entry.path == S1Source)
                for (int i = 1; i < S1Scenes.Length; i++) result.Add(new EditorBuildSettingsScene(S1Scenes[i], true));
        }
        EditorBuildSettings.scenes = result.ToArray();
    }
}
