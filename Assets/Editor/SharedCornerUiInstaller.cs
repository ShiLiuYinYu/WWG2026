using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>Installs one shared corner HUD in every gameplay chapter without rebuilding scenes.</summary>
public static class SharedCornerUiInstaller
{
    private const string HudPath = "Assets/Prefabs/UI/SharedInteractionHud.prefab";
    private const string OverlayPath = "Assets/Prefabs/UI/GameplayOverlay.prefab";
    private const string TopPath = "Assets/Prefabs/UI/CornerTopBar.prefab";
    private const string BagPath = "Assets/Prefabs/UI/CornerInventoryButton.prefab";
    private const string VitalsPath = "Assets/Prefabs/UI/VitalsBars.prefab";
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/S2.unity",
        "Assets/Scenes/S2/S2_BridgeSpan4F.unity",
        "Assets/Scenes/S2/S2_BridgeEven4F.unity",
        "Assets/Scenes/S2/S2_ClubRoom.unity",
        "Assets/Scenes/S3.unity",
        "Assets/Scenes/S4.unity"
    };

    [MenuItem("WWG/UI/Install Shared Interaction HUD")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("请先退出 Play 模式。");
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请先保存当前场景。");
        string previous = SceneManager.GetActiveScene().path;
        try
        {
            CreateHudPrefabIfMissing();
            InstallInOverlay();
            foreach (string path in Scenes) InstallInScene(path);
            Debug.Log("[WWG] S0–S4 的玩法场景现共用 SharedInteractionHud。");
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }

    private static void CreateHudPrefabIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) != null) return;
        Scene scene = EditorSceneManager.OpenScene(Scenes[0], OpenSceneMode.Single);
        Canvas canvas = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
            .First(item => item.name == "S2 Canvas");
        VitalsBars oldVitals = canvas.GetComponentInChildren<VitalsBars>(true);
        GameObject root = new GameObject("SharedInteractionHud", typeof(RectTransform), typeof(SharedInteractionHud));
        root.transform.SetParent(canvas.transform, false);
        try
        {
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GameObject top = AddNested(TopPath, root.transform);
            GameObject vitals = AddNested(VitalsPath, root.transform);
            GameObject bag = AddNested(BagPath, root.transform);
            RectTransform from = (RectTransform)oldVitals.transform;
            RectTransform to = (RectTransform)vitals.transform;
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;

            SharedInteractionHud hud = root.GetComponent<SharedInteractionHud>();
            hud.topBar = top;
            hud.timeText = top.transform.Find("Time").GetComponent<Text>();
            hud.vitals = vitals.GetComponent<VitalsBars>();
            hud.pauseButton = top.transform.Find("Pause").GetComponent<Button>();
            hud.recordButton = top.transform.Find("Record").GetComponent<Button>();
            hud.inventoryButton = bag.GetComponent<Button>();
            if (!PrefabUtility.SaveAsPrefabAsset(root, HudPath))
                throw new InvalidOperationException("无法保存共享交互 HUD。");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject AddNested(string path, Transform parent)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new InvalidOperationException("缺少预制体：" + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        if (instance == null) throw new InvalidOperationException("无法实例化：" + path);
        return instance;
    }

    private static void InstallInOverlay()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(OverlayPath);
        try
        {
            if (root.transform.Find("SharedInteractionHud") != null) return;
            Transform oldTop = root.transform.Find("Top bar");
            Transform oldBag = root.transform.Find("Inventory button");
            if (oldTop == null || oldBag == null)
                throw new InvalidOperationException("GameplayOverlay 缺少原角落 UI。");
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            GameObject hud = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            hud.transform.SetSiblingIndex(Mathf.Min(oldTop.GetSiblingIndex(), oldBag.GetSiblingIndex()));
            UnityEngine.Object.DestroyImmediate(oldTop.gameObject);
            UnityEngine.Object.DestroyImmediate(oldBag.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, OverlayPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void InstallInScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Component controller = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .FirstOrDefault(item => item is S2Controller || item is S3Controller || item is S4Controller);
        if (controller == null) throw new InvalidOperationException(path + " 缺少章节控制器。");
        Canvas canvas = controller.GetComponentInChildren<Canvas>(true);
        Transform holder = canvas.transform.Find("S3 S2-aligned UI") ?? canvas.transform;
        if (holder.Find("SharedInteractionHud") != null) return;
        Transform oldTop = holder.Find("Top bar");
        Transform oldBag = holder.Cast<Transform>()
            .FirstOrDefault(child => child.name == "Inventory" && child.GetComponent<Button>() != null);
        VitalsBars oldVitals = canvas.GetComponentInChildren<VitalsBars>(true);
        if (oldTop == null || oldBag == null || oldVitals == null)
            throw new InvalidOperationException(path + " 缺少原角落 UI。");

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
        instance.transform.SetParent(holder, false);
        instance.transform.SetSiblingIndex(oldTop.GetSiblingIndex());
        SharedInteractionHud hud = instance.GetComponent<SharedInteractionHud>();
        if (controller is S2Controller s2)
        {
            s2.timeText = hud.timeText;
            s2.vitals = hud.vitals;
            Bind(hud.pauseButton, s2.TogglePause);
            Bind(hud.recordButton, s2.OpenRecord);
            Bind(hud.inventoryButton, s2.OpenInventory);
        }
        else if (controller is S3Controller s3)
        {
            s3.timeText = hud.timeText;
            s3.vitals = hud.vitals;
            Bind(hud.pauseButton, s3.TogglePause);
            Bind(hud.recordButton, s3.OpenRecord);
            Bind(hud.inventoryButton, s3.OpenInventory);
        }
        else
        {
            S4Controller s4 = (S4Controller)controller;
            s4.timeText = hud.timeText;
            s4.vitals = hud.vitals;
        }
        UnityEngine.Object.DestroyImmediate(oldTop.gameObject);
        UnityEngine.Object.DestroyImmediate(oldBag.gameObject);
        UnityEngine.Object.DestroyImmediate(oldVitals.gameObject);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Bind(Button button, UnityAction action)
    {
        for (int index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
            UnityEventTools.RemovePersistentListener(button.onClick, index);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
