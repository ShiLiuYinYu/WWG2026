using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WWG;

public static class GameplayUiSkin
{
    private const string WindowPath = "Assets/Resources/UI/选项框.png";

    private static readonly HashSet<string> Windows = new HashSet<string>
    {
        "Inspect", "Inspect panel", "Memo panel", "Task record", "Inventory", "Inventory panel",
        "Tooltip", "Item detail", "Pause", "Pause card", "Settings panel", "SettingsPanel", "Confirmation",
        "Four digit cabinet", "ClubPhotoView", "Old photograph", "纸面线索", "任务记录", "背包", "暂停菜单",
        "归档窗口内容", "S4 Choice", "Case panel", "Puzzle", "Dialogue panel", "Document card"
    };

    [MenuItem("WWG/UI/Apply Shared Window and Button Sprites")]
    public static void ApplyProject()
    {
        LateStageSceneKit.RequireIdle("统一弹窗与按钮贴图");

        foreach (string path in new[]
        {
            "Assets/Prefabs/UI/CornerTopBar.prefab",
            "Assets/Prefabs/UI/SharedInteractionHud.prefab",
            "Assets/Prefabs/UI/GameplayOverlay.prefab",
            "Assets/Prefabs/UI/ClubPhotoView.prefab",
            "Assets/Resources/UI/SettingsPanel.prefab",
            "Assets/Resources/UI/SaveSlotPanel.prefab",
            "Assets/Resources/Dialogue/WwgDialogueHost.prefab"
        })
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (Apply(prefab)) PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var entry in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            {
                var scene = EditorSceneManager.OpenScene(entry.path);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    changed |= Apply(root);
                    changed |= InstallEarlyArrows(root, scene.name);
                }
                if (changed) EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    public static bool Apply(GameObject root)
    {
        Sprite window = AssetDatabase.LoadAssetAtPath<Sprite>(WindowPath);
        bool changed = false;
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (!Windows.Contains(rect.name) || rect.GetComponent<Button>() != null) continue;
            Image image = rect.GetComponent<Image>();
            if (image == null && (rect.name == "归档窗口内容" || rect.name == "S4 Choice"))
                image = rect.gameObject.AddComponent<Image>();
            if (image == null || image.sprite == window && image.color == Color.white) continue;
            image.sprite = window;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            if (rect.name == "S4 Choice" || rect.name == "归档窗口内容") image.raycastTarget = false;
            changed = true;

            foreach (Text label in rect.GetComponentsInChildren<Text>(true))
            {
                if (label.GetComponentInParent<Button>(true) != null) continue;
                label.color = LateUi.WindowInk;
            }
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name is "CornerInventoryButton" or "Inventory button" or "背包入口") continue;
            if (!(button.targetGraphic is Image image) || image.color.a < 0.05f ||
                image.sprite == LateUi.ButtonSprite || image.sprite != null && image.sprite.name == "WhiteArrow") continue;

            LateUi.ApplyButtonSprite(image);
            if (button.transition == Selectable.Transition.SpriteSwap)
                button.transition = Selectable.Transition.ColorTint;
            changed = true;
        }
        return changed;
    }

    private static bool InstallEarlyArrows(GameObject root, string scene)
    {
        if (scene == "S4")
        {
            var controller = root.GetComponent<S4Controller>();
            if (controller == null || controller.endingPanel.transform.Find("Continue") != null) return false;
            Button button = LateUi.Button(controller.endingPanel.transform, "Continue", "继续 · 后 24H（周六 16:05）",
                660, 750, 600, 78, fontSize: 32);
            button.transition = Selectable.Transition.None;
            button.targetGraphic.color = Color.clear;
            InstallArrowFace(button.transform, new Vector2(60f, -39f), 88f, 0f);
            button.gameObject.SetActive(false);
            return true;
        }

        if (scene != "S0_Corridor") return false;
        S0Hotspot hotspot = root.GetComponentsInChildren<S0Hotspot>(true)
            .FirstOrDefault(candidate => candidate.action == "toRoom");
        if (hotspot == null || hotspot.transform.Find("Navigation arrow") != null) return false;

        Image arrow = InstallArrowFace(hotspot.transform, new Vector2(133.13f, -589.62f), 108f, 180f);
        var highlight = hotspot.GetComponent<HotspotHoverOutline>();
        if (highlight != null) return true;

        var edge = new GameObject("Arrow hover", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        edge.transform.SetParent(arrow.transform, false);
        var rect = edge.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        edge.sprite = arrow.sprite;
        edge.preserveAspect = true;
        edge.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UI/CorridorHoverOutline.mat");
        edge.raycastTarget = false;
        edge.enabled = false;
        highlight = hotspot.gameObject.AddComponent<HotspotHoverOutline>();
        var serialized = new SerializedObject(highlight);
        serialized.FindProperty("highlight").objectReferenceValue = edge;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    internal static Image InstallArrowFace(Transform parent, Vector2 position, float size, float angle)
    {
        var image = new GameObject("Navigation arrow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Navigation/WhiteArrow.png");
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
}
