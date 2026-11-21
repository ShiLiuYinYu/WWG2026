using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-time, non-destructive S3 HUD alignment to the hand-authored S2 UI.</summary>
public static class S3UiAlignment
{
    private const string S2Path = "Assets/Scenes/S2.unity";
    private const string S3Path = "Assets/Scenes/S3.unity";
    private const string Marker = "S3 S2-aligned UI";

    [MenuItem("WWG/UI/Align S3 With S2")]
    public static void Align()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先保存当前场景。");
        string previous = SceneManager.GetActiveScene().path;
        Scene target = EditorSceneManager.OpenScene(S3Path, OpenSceneMode.Single);
        var controller = UnityEngine.Object.FindFirstObjectByType<S3Controller>(FindObjectsInactive.Include);
        if (controller == null) throw new InvalidOperationException("S3 控制器缺失。");
        Canvas canvas = controller.GetComponentInChildren<Canvas>(true);
        if (canvas == null) throw new InvalidOperationException("S3 Canvas 缺失。");
        if (canvas.transform.Find(Marker) != null)
        {
            Debug.Log("[WWG] S3 已采用 S2 UI；保留现有人工布局。");
            if (!string.IsNullOrEmpty(previous) && previous != S3Path) EditorSceneManager.OpenScene(previous);
            return;
        }

        Scene reference = EditorSceneManager.OpenScene(S2Path, OpenSceneMode.Additive);
        try
        {
            Canvas source = null;
            foreach (GameObject root in reference.GetRootGameObjects())
                foreach (Canvas candidate in root.GetComponentsInChildren<Canvas>(true))
                    if (candidate.name == "S2 Canvas") source = candidate;
            if (source == null) throw new InvalidOperationException("S2 参考 Canvas 缺失。");

            var holder = new GameObject(Marker, typeof(RectTransform));
            holder.transform.SetParent(canvas.transform, false);
            var holderRect = (RectTransform)holder.transform;
            holderRect.anchorMin = Vector2.zero;
            holderRect.anchorMax = Vector2.one;
            holderRect.offsetMin = Vector2.zero;
            holderRect.offsetMax = Vector2.zero;

            var top = Clone(Root(source, "Top bar"), holderRect);
            var bagButton = Clone(Root(source, "Inventory", true), holderRect);
            var inspect = Clone(Root(source, "Inspect"), holderRect);
            var memo = Clone(Root(source, "Task record"), holderRect);
            var bag = Clone(Root(source, "Inventory", false), holderRect);
            var pause = Clone(Root(source, "Pause", false), holderRect);

            Rewire(top.transform.Find("Pause"), controller.TogglePause);
            Rewire(top.transform.Find("Record"), controller.OpenRecord);
            Rewire(bagButton.transform, controller.OpenInventory);
            Rewire(inspect.transform.Find("Close"), controller.CloseInfo);
            Rewire(memo.transform.Find("Close"), controller.CloseRecord);
            Rewire(bag.transform.Find("Bag contents/Close") ?? bag.transform.Find("Close"), controller.CloseInventory);
            Rewire(pause.transform.Find("Resume"), controller.TogglePause);
            Rewire(pause.transform.Find("Save"), controller.SaveManual);
            Rewire(pause.transform.Find("Load"), controller.LoadManual);
            Rewire(FindButton(pause.transform, "Title"), controller.ReturnTitle);

            // Retain the authored S3 computer and hotspots; only retire our initial generic HUD.
            foreach (string oldName in new[] { "顶栏", "地点", "目标", "时间", "背包入口", "任务记录入口",
                "暂停入口", "纸面线索", "任务记录", "背包", "暂停菜单" })
            {
                Transform old = canvas.transform.Find(oldName);
                if (old != null) old.gameObject.SetActive(false);
            }

            controller.timeText = top.transform.Find("Time").GetComponent<Text>();
            controller.infoPanel = inspect;
            controller.infoTitle = inspect.transform.Find("Title").GetComponent<Text>();
            controller.infoBody = inspect.transform.Find("Body").GetComponent<Text>();
            controller.recordPanel = memo;
            controller.recordText = memo.transform.Find("Viewport/Record").GetComponent<Text>();
            controller.inventoryPanel = bag;
            controller.inventoryText = null;
            controller.pausePanel = pause;
            controller.locationText = canvas.transform.Find("地点").GetComponent<Text>();
            controller.objectiveText = canvas.transform.Find("目标").GetComponent<Text>();

            Sprite buttonArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/S0/选项按钮.png");
            if (buttonArt == null) throw new InvalidOperationException("选项按钮素材缺失。");
            foreach (string group in new[] { "归档提取工具", "返回楼梯口" })
            {
                Transform root = canvas.transform.Find(group);
                if (root == null) continue;
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.GetComponent<S3HotspotHover>() != null) continue;
                    Image image = button.GetComponent<Image>();
                    if (image == null) continue;
                    image.sprite = buttonArt;
                    image.type = Image.Type.Simple;
                    image.color = Color.white;
                    Text caption = button.GetComponentInChildren<Text>(true);
                    if (caption != null) caption.color = new Color(0.94f, 0.87f, 0.73f);
                }
            }
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(target);
            EditorSceneManager.SaveScene(target);
            Debug.Log("[WWG] S3 顶栏、背包、备忘录、调查页和暂停菜单已对齐 S2。");
        }
        finally
        {
            EditorSceneManager.CloseScene(reference, true);
            if (!string.IsNullOrEmpty(previous) && previous != S3Path) EditorSceneManager.OpenScene(previous);
        }
    }

    private static Transform Root(Canvas source, string name, bool button = false)
    {
        foreach (Transform child in source.transform)
            if (child.name == name && (child.GetComponent<Button>() != null) == button) return child;
        throw new InvalidOperationException("S2 UI 参考对象缺失：" + name + (button ? " button" : " panel"));
    }
    private static GameObject Clone(Transform source, Transform target)
    {
        GameObject result = UnityEngine.Object.Instantiate(source.gameObject, target, false);
        result.name = source.name;
        return result;
    }

    private static Transform FindButton(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name && child.GetComponent<Button>() != null) return child;
        throw new InvalidOperationException("S3 UI 按钮缺失：" + name);
    }
    private static void Rewire(Transform target, UnityEngine.Events.UnityAction action)
    {
        if (target == null) throw new InvalidOperationException("S3 UI 按钮缺失。");
        Button button = target.GetComponent<Button>();
        if (button == null) throw new InvalidOperationException("S3 UI Button 缺失：" + target.name);
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
