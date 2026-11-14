using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主页面（封面页）生成器：白天实验楼封面 + 标题字。
/// 当前版本不放按钮，只做静态封面；后续在 Title Canvas 下追加按钮即可。
/// </summary>
[InitializeOnLoad]
public static class TitleSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Title.unity";
    private const string ArtDir = "Assets/Resources/Title/";
    private const string TitleArtPath = ArtDir + "TitleArt.png";
    private const string CoverPath = "Assets/Resources/S0/Exterior.png";
    private const string AutoBuildKey = "WWG.TitleScene.AutoBuilt";

    static TitleSceneBuilder()
    {
        if (File.Exists(ScenePath) || SessionState.GetBool(AutoBuildKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(ScenePath) || SessionState.GetBool(AutoBuildKey, false)) return;
            SessionState.SetBool(AutoBuildKey, true);
            Build();
        };
    }

    [MenuItem("WWG/Build Title Scene")]
    public static void Build()
    {
        ImportSprite(CoverPath, 2048);
        ImportSprite(TitleArtPath, 4096);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Title";

        GameObject camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camera.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        GameObject canvas = new GameObject("Title Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 封面：白天实验楼正门，铺满 16:9 画布。
        GameObject cover = Rect(canvas.transform, "Cover / 白天实验楼", 0f, 0f, 1920f, 1080f);
        Image coverImage = cover.AddComponent<Image>();
        coverImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoverPath);
        coverImage.raycastTarget = false;
        coverImage.preserveAspect = false;

        // 标题字：血手写体「熄灯之后：48H」，压在上部连廊与天空之间。
        GameObject titleArt = Rect(canvas.transform, "Title / 熄灯之后 48H", 530f, 110f, 860f, 560f);
        Image titleImage = titleArt.AddComponent<Image>();
        titleImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleArtPath);
        titleImage.raycastTarget = false;
        titleImage.preserveAspect = true;

        // 底部留白层，给后续按钮区一个稳定的压暗底，避免按钮直接压在树影上。
        GameObject footer = Rect(canvas.transform, "Footer shade", 0f, 780f, 1920f, 300f);
        Image footerImage = footer.AddComponent<Image>();
        footerImage.color = new Color(0.02f, 0.035f, 0.05f, 0.22f);
        footerImage.raycastTarget = false;

        EditorSceneManager.SaveScene(scene, ScenePath);

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            if (entry.path != ScenePath) scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("WWG 主页面已生成：" + ScenePath + "（已设为 Build Settings 第一场景）");
    }

    private static void ImportSprite(string path, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new IOException("缺少封面素材：" + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = maxSize;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static GameObject Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }
}
