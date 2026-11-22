using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>
/// Authoring helpers for the 后 24H chapters (S5–S8). Builders create visuals and stable object names;
/// controllers bind buttons at runtime through those same names. Widgets come from LateUi so authored
/// and runtime-generated UI match.
/// </summary>
public static class LateStageSceneKit
{
    public static Color Ink => LateUi.Ink;
    public static Color Paper => LateUi.Paper;
    public static Color PaperDim => LateUi.PaperDim;
    public static Color Accent => LateUi.Accent;
    public static Color Warm => LateUi.Warm;

    public static void RequireIdle(string label)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("先退出 Play 模式再运行 " + label + "。");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景再运行 " + label + "。");
    }

    public static bool SceneExists(string path) => File.Exists(path);

    /// <summary>Creates an empty scene with camera, light, event system, controller root and canvas.</summary>
    public static GameObject NewScene(string rootName, Type controllerType, string canvasName, string backgroundSprite,
        string folder = null, bool mirror = false)
    {
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
        camera.transform.position = new Vector3(0, 0, -10);
        Camera view = camera.GetComponent<Camera>();
        view.orthographic = true;
        view.backgroundColor = Color.black;
        new GameObject("Global Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var root = new GameObject(rootName, controllerType);
        var canvas = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(root.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scale = canvas.GetComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scale.matchWidthOrHeight = 0.5f;

        if (!string.IsNullOrEmpty(backgroundSprite)) Background(canvas.transform, backgroundSprite, mirror);
        return root;
    }

    public static Image Background(Transform canvas, string spritePath, bool mirror = false, Color? tint = null)
    {
        GameObject go = LateUi.Panel(canvas, "Location artwork", 0, 0, 1920, 1080, Color.white, false);
        Image image = go.GetComponent<Image>();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) Debug.LogError("[WWG] 找不到背景图：" + spritePath);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = tint ?? Color.white;
        if (mirror) go.transform.localScale = new Vector3(-1, 1, 1);
        return image;
    }

    /// <summary>
    /// Placed overlay for a sprite that only covers part of the 1920×1080 frame (window panes, curtains,
    /// props that were sliced as sub-sprites). Positions the rect at the sprite's own texture rect so
    /// the artwork lands where the painter put it instead of stretching over the whole screen.
    /// </summary>
    public static Image Overlay(Transform canvas, string spritePath, string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogError("[WWG] 找不到叠层图：" + spritePath);
            return null;
        }

        GameObject go = LateUi.Panel(canvas, name, 0, 0, 1920, 1080, Color.white, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;

        Rect texRect = sprite.textureRect;
        if (texRect.width < 1f || texRect.height < 1f)
        {
            texRect = new Rect(0f, 0f, sprite.texture.width, sprite.texture.height);
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(texRect.x, -(sprite.texture.height - texRect.y - texRect.height));
        rect.sizeDelta = new Vector2(texRect.width, texRect.height);
        return image;
    }

    /// <summary>Dark translucent full-screen wash used for night views and blackout transitions.</summary>
    public static GameObject Wash(Transform canvas, string name, float alpha, Color? color = null)
    {
        return LateUi.Panel(canvas, name, 0, 0, 1920, 1080,
            color ?? new Color(0.01f, 0.02f, 0.04f, alpha), false);
    }

    public static Text Clock(Transform canvas)
    {
        Text time = LateUi.Label(canvas, "Time", "", 0, 0, 1, 1, 28, Paper, TextAnchor.MiddleRight);
        time.gameObject.SetActive(false);
        return time;
    }

    /// <summary>Hidden 88px feedback line above the navigation row.</summary>
    public static Text Feedback(Transform canvas)
    {
        Text text = LateUi.Label(canvas, "Feedback", "", 230, 745, 1460, 95, 29, Paper, TextAnchor.MiddleCenter);
        text.gameObject.AddComponent<Outline>().effectColor = Color.black;
        text.gameObject.SetActive(false);
        return text;
    }

    /// <summary>Standard modal: inspect panel with title, body and a close button.</summary>
    public static GameObject InfoPanel(Transform canvas, out Text title, out Text body)
    {
        GameObject panel = LateUi.Panel(canvas, "Inspect", 345, 160, 1230, 760, Ink);
        title = LateUi.Label(panel.transform, "Title", "", 55, 35, 1110, 85, 40, Accent, TextAnchor.MiddleLeft);
        body = LateUi.Label(panel.transform, "Body", "", 65, 150, 1090, 430, 34, Paper, TextAnchor.UpperLeft);
        LateUi.FitBody(body);
        LateUi.Button(panel.transform, "Close", "继续调查", 920, 640, 245, 68);
        panel.SetActive(false);
        return panel;
    }

    /// <summary>Task-record modal (opens with the record button).</summary>
    public static GameObject RecordPanel(Transform canvas, out Text record)
    {
        GameObject panel = LateUi.Panel(canvas, "Task record", 300, 125, 1320, 825, Ink);
        LateUi.Label(panel.transform, "Title", "任务记录", 60, 30, 880, 70, 42, Accent, TextAnchor.MiddleLeft);
        record = LateUi.Label(panel.transform, "Record", "", 75, 135, 1170, 580, 29, Paper, TextAnchor.UpperLeft);
        LateUi.Button(panel.transform, "Close", "关闭", 1080, 42, 170, 60);
        panel.SetActive(false);
        return panel;
    }

    /// <summary>Three-option puzzle modal shared by S6–S8 (choice cards, roster rows, packaging steps).</summary>
    public static GameObject PuzzlePanel(Transform canvas, out Text title, out Text body, out Text note,
        out Button a, out Button b, out Button c, out Button close)
    {
        GameObject panel = LateUi.Panel(canvas, "Puzzle", 260, 140, 1400, 800, Ink);
        title = LateUi.Label(panel.transform, "Title", "", 60, 36, 1180, 70, 40, Accent, TextAnchor.MiddleLeft);
        body = LateUi.Label(panel.transform, "Body", "", 60, 118, 1280, 460, 32, Paper, TextAnchor.UpperLeft);
        a = LateUi.Button(panel.transform, "Option A", "", 60, 596, 380, 88, 24);
        b = LateUi.Button(panel.transform, "Option B", "", 460, 596, 380, 88, 24);
        c = LateUi.Button(panel.transform, "Option C", "", 860, 596, 380, 88, 24);
        LateUi.AutoFitCaption(a, 24);
        LateUi.AutoFitCaption(b, 24);
        LateUi.AutoFitCaption(c, 24);
        note = LateUi.Label(panel.transform, "Feedback", "", 60, 690, 1120, 70, 28, PaperDim, TextAnchor.MiddleLeft);
        close = LateUi.Button(panel.transform, "Close", "关闭", 1180, 44, 180, 62);
        panel.SetActive(false);
        return panel;
    }

    /// <summary>Saves the active scene and appends it to Build Settings once.</summary>
    public static void SaveScene(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("场景路径为空");
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        Scene scene = SceneManager.GetActiveScene();
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new InvalidOperationException("保存场景失败：" + path);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[WWG] 已保存并登记场景：" + path);
    }

    public static void RunBuilders(string label, Action build)
    {
        RequireIdle(label);
        string previous = SceneManager.GetActiveScene().path;
        try
        {
            build();
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }
}
