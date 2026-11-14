using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主页面按钮层：在 Title 场景的 Title Canvas 下生成四个菜单按钮
/// （开始游戏 / 读取存档 / 设置 / 退出游戏），底图用 选项按钮.png。
/// 已有 Buttons 时只修复接线，不覆盖手工排版。
/// </summary>
public static class TitleMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/Title.unity";
    private const string ButtonSpritePath = "Assets/Resources/S0/选项按钮.png";

    private static readonly string[] Labels = { "开始游戏", "读取存档", "设置", "退出游戏" };

    private const float BtnW = 242f;
    private const float BtnH = 118f;
    private const float BtnGap = 10f;
    private const float BtnTopY = -40f;

    [MenuItem("WWG/Build Title Buttons")]
    public static void Build()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("[Menu] 场景不存在：" + ScenePath);
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        bool needOpen = active.path != ScenePath;
        Scene scene = needOpen
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
            : active;

        GameObject canvas = GameObject.Find("Title Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Menu] 找不到 Title Canvas");
            return;
        }

        if (canvas.transform.Find("Buttons") != null)
        {
            TitleMenuWiring.Wire();
            return;
        }

        ImportSprite(ButtonSpritePath, 1024);
        Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
        if (buttonSprite == null)
        {
            Debug.LogError("[Menu] 按钮底图导入失败：" + ButtonSpritePath);
            return;
        }

        // 标题上移收窄，给下方按钮腾出整片空间。
        Transform titleNode = canvas.transform.Find("Title / 熄灯之后 48H");
        if (titleNode != null)
        {
            RectTransform titleRect = titleNode.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(660f, 430f);
            titleRect.anchoredPosition = new Vector2(630f, -200f);
        }

        GameObject holder = new GameObject("Buttons", typeof(RectTransform));
        holder.transform.SetParent(canvas.transform, false);
        RectTransform holderRect = holder.GetComponent<RectTransform>();
        holderRect.anchorMin = Vector2.zero;
        holderRect.anchorMax = Vector2.one;
        holderRect.offsetMin = Vector2.zero;
        holderRect.offsetMax = Vector2.zero;

        Font font = LoadCjkFont();

        for (int i = 0; i < Labels.Length; i++)
        {
            float centerY = BtnTopY - BtnH * 0.5f - i * (BtnH + BtnGap);
            GameObject go = new GameObject("Btn / " + Labels[i], typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(holder.transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, centerY);
            rt.sizeDelta = new Vector2(BtnW, BtnH);

            Image img = go.GetComponent<Image>();
            img.sprite = buttonSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.88f, 1f);
            colors.pressedColor = new Color(0.78f, 0.70f, 0.68f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Shadow));
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Text text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = 44;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.93f, 0.88f);
            text.text = Labels[i];
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textGo.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        if (needOpen || true)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        Debug.Log("[Menu] 主页面按钮已生成：4 个按钮 + 标题区调整");
        TitleMenuWiring.Wire();
    }

    private static Font LoadCjkFont()
    {
        string[] candidates = { "Microsoft YaHei", "微软雅黑", "SimHei", "DengXian", "Arial Unicode MS" };
        foreach (string name in candidates)
        {
            try
            {
                Font f = Font.CreateDynamicFontFromOSFont(name, 44);
                if (f != null)
                {
                    Debug.Log("[Menu] 使用系统字体：" + name);
                    return f;
                }
            }
            catch (Exception) { }
        }

        Debug.LogWarning("[Menu] 没找到中文字体，回退 LegacyRuntime.ttf（中文可能显示为方块）");
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void ImportSprite(string path, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new IOException("缺少按钮底图：" + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = maxSize;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
