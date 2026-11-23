using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>One-time, idempotent installation for the existing S2/S3 navigation buttons.</summary>
public static class NavigationArrowHintInstaller
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/S2.unity",
        "Assets/Scenes/S2/S2_BridgeSpan4F.unity",
        "Assets/Scenes/S2/S2_BridgeEven4F.unity",
        "Assets/Scenes/S2/S2_ClubRoom.unity",
        "Assets/Scenes/S3.unity"
    };

    private static readonly Dictionary<string, string> Hints = new Dictionary<string, string>
    {
        { "To machine room", "前往单数栋 3F 机房" },
        { "To safe", "返回 5F 活动室" },
        { "To span", "前往 4F 连廊中段" },
        { "Back span", "返回 4F 连廊中段" },
        { "Retreat", "返回单数栋 4F 连廊口" },
        { "Back odd", "返回单数栋 4F 连廊口" },
        { "Forward even", "前往偶数栋 4F 连廊口" },
        { "Down to bridge", "下楼到单数栋 4F 连廊口" },
        { "返回楼梯口", "离开 3F 机房" }
    };

    [MenuItem("WWG/UI/Install Navigation Arrow Hints")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) return;
        Scene original = SceneManager.GetActiveScene();
        string originalPath = original.path;
        if (original.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Material edgeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UI/CorridorHoverOutline.mat");
        Sprite arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Navigation/WhiteArrow.png");
        if (edgeMaterial == null || arrowSprite == null)
        {
            Debug.LogError("[WWG] 行动箭头材质或图片缺失。");
            return;
        }

        int installed = 0;
        foreach (string path in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.gameObject.scene != scene) continue;
                List<Button> arrows = new List<Button>();
                foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
                {
                    Image face = button.GetComponent<Image>();
                    if (face != null && face.sprite == arrowSprite && Hints.ContainsKey(button.name)) arrows.Add(button);
                }
                if (arrows.Count == 0) continue;

                Text caption = canvas.transform.Find("Navigation hint")?.GetComponent<Text>();
                if (caption == null)
                {
                    GameObject captionObject = new GameObject("Navigation hint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    captionObject.transform.SetParent(canvas.transform, false);
                    RectTransform rect = (RectTransform)captionObject.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, 55f);
                    rect.sizeDelta = new Vector2(1000f, 48f);
                    caption = captionObject.GetComponent<Text>();
                    caption.font = WWG.WwgFonts.Body;
                    caption.fontSize = 26;
                    caption.alignment = TextAnchor.MiddleCenter;
                    caption.color = new Color(1f, 0.91f, 0.65f);
                    caption.raycastTarget = false;
                    caption.text = "";
                    changed = true;
                }

                foreach (Button button in arrows)
                {
                    Image face = button.GetComponent<Image>();
                    Transform child = button.transform.Find("Arrow hover");
                    Image edge;
                    if (child == null)
                    {
                        GameObject edgeObject = new GameObject("Arrow hover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        edgeObject.transform.SetParent(button.transform, false);
                        RectTransform rect = (RectTransform)edgeObject.transform;
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                        edge = edgeObject.GetComponent<Image>();
                        edge.sprite = face.sprite;
                        edge.preserveAspect = face.preserveAspect;
                        edge.material = edgeMaterial;
                        edge.raycastTarget = false;
                        edge.enabled = false;
                        changed = true;
                    }
                    else edge = child.GetComponent<Image>();

                    NavigationArrowHover hover = button.GetComponent<NavigationArrowHover>();
                    if (hover == null)
                    {
                        hover = button.gameObject.AddComponent<NavigationArrowHover>();
                        changed = true;
                    }
                    hover.Configure(edge, caption, Hints[button.name]);
                    EditorUtility.SetDirty(hover);
                    installed++;
                }
            }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        Debug.Log("[WWG] 已检查 " + installed + " 个 S2/S3 行动箭头的悬停提示。");
    }
}
