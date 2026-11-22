using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;
using Object = UnityEngine.Object;

public static class LateStageScenePresentation
{
    private const string OverlayPath = "Assets/Prefabs/UI/GameplayOverlay.prefab";
    private const string FrameName = "Scene frame";
    private const string RoomScene = "Assets/Scenes/S4.unity";
    private const string BadgeArtworkPath = "Assets/Prefabs/UI/BadgeDetailArtwork.prefab";

    public static void Apply(Transform canvas, int chapter)
    {
        Transform previousFrame = canvas.Find(FrameName);
        if (previousFrame != null) canvas = previousFrame;
        foreach (string name in new[] { "Top band", "Location", "Objective", "Bag button", "Record button", "Pause button", "Vitals", "Pause", "Inventory" })
        {
            Transform old = canvas.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        canvas.Find("Time").gameObject.SetActive(false);

        if (chapter == 6) ApplyNightWalk(canvas);
        else WithReference(RoomScene, reference =>
        {
            var finale = reference.GetRootGameObjects().Select(root => root.GetComponent<S4Controller>())
                .First(controller => controller != null);
            ApplyRoom(canvas, finale, chapter);
        });

        InstallBadgeArtwork(canvas.Find("Inspect"));
        GameplayUiSkin.Apply(canvas.gameObject);

        Transform overlay = canvas.Find("GameplayOverlay");
        if (overlay == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPath);
            overlay = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas)).transform;
        }
        overlay.SetAsLastSibling();
        foreach (string name in new[] { "Feedback", "Task record", "Puzzle", "Case panel", "Inspect", "Ending" })
            canvas.Find(name)?.SetAsLastSibling();
        if (previousFrame == null) FitFrame(canvas);
    }

    private static void FitFrame(Transform canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var frame = new GameObject(FrameName, typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(canvas, false);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = Vector2.zero;
        frame.sizeDelta = scaler.referenceResolution;

        // Keep the 1920×1080 artwork and its hotspots in the same coordinate space.
        while (canvas.childCount > 1) canvas.GetChild(0).SetParent(frame, false);
    }

    private static void ApplyNightWalk(Transform canvas)
    {
        S6Controller controller = canvas.GetComponentInParent<S6Controller>();
        switch (controller.name)
        {
            case "S6 Corridor5F":
                WithReference("Assets/Scenes/S1/S1_Corridor.unity", scene =>
                {
                    Transform corridor = FindCanvas(scene).Find("5F Corridor/Corridor interactions");
                    AlignArrow(canvas, controller.navA, corridor.Find("← 返回活动室"));
                    AlignArrow(canvas, controller.navB, corridor.Find("下楼 · 4F 连廊 →"));
                });
                break;
            case "S6 BridgeOdd4F":
                WithReference("Assets/Scenes/S2.unity", scene =>
                {
                    Transform source = FindCanvas(scene);
                    Transform art = InstallBridgeArt(canvas, source, controller);
                    AlignArrow(canvas, controller.navA, source.Find("To safe"));
                    AlignArrow(canvas, controller.navB, source.Find("To span"));
                    AlignArrow(canvas, controller.navC, source.Find("To machine room"));
                    controller.emergencySwitchArt = art.Find("Switch art").GetComponent<Image>();
                    controller.emergencySwitchOutline = art.Find("Switch outline").GetComponent<Graphic>();
                    controller.emergencySwitchOn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Bridge4F/SwitchOn.png");
                    AlignHotspot(controller.emergencySwitch, source.Find("Switch art hotspot"), controller.emergencySwitchOutline);
                    AlignHotspot(controller.guideBoard, source.Find("Guide hotspot"),
                        art.Find("Guide board outline").GetComponent<Graphic>());
                });
                break;
            case "S6 BridgeSpan4F":
                WithReference("Assets/Scenes/S2/S2_BridgeSpan4F.unity", scene =>
                {
                    Transform source = FindCanvas(scene);
                    Transform art = InstallBridgeArt(canvas, source, controller);
                    AlignArrow(canvas, controller.navA, source.Find("Retreat"));
                    AlignArrow(canvas, controller.navB, source.Find("Forward even"));
                    AlignHotspot(controller.glass, source.Find("Glass hotspot"),
                        art.Find("Right glass outline").GetComponent<Graphic>());
                });
                break;
            case "S6 BridgeEven4F":
                WithReference("Assets/Scenes/S2/S2_BridgeEven4F.unity", scene =>
                {
                    Transform source = FindCanvas(scene);
                    Transform art = InstallBridgeArt(canvas, source, controller);
                    AlignArrow(canvas, controller.navA, source.Find("Back span"));
                    AlignReturnShortcut(canvas, controller.navB, source.Find("Back span"));
                    AlignHotspot(controller.cabinet, source.Find("Cabinet hotspot"),
                        art.Find("Cabinet outline").GetComponent<Graphic>());
                });
                break;
            case "S6 ServerRoom3F":
                WithReference("Assets/Scenes/S3.unity", scene =>
                {
                    Transform source = FindCanvas(scene);
                    AlignArrow(canvas, controller.navA, source.Find("返回楼梯口"));
                    AlignReturnShortcut(canvas, controller.navB, source.Find("返回楼梯口"));
                    AlignHotspot(controller.serverMachine, source.Find("靠窗第三台电脑热点"));
                    if (canvas.Find("夜间暗层") == null)
                        CopyArtwork(source.Find("夜间暗层"), canvas, 1);
                });
                break;
        }
    }

    private static Transform FindCanvas(Scene scene) => scene.GetRootGameObjects()
        .Select(root => root.GetComponentInChildren<Canvas>(true)).First(canvas => canvas != null).transform;

    private static void WithReference(string path, Action<Scene> apply)
    {
        Scene reference = SceneManager.GetSceneByPath(path);
        bool opened = !reference.isLoaded;
        if (opened) reference = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try { apply(reference); }
        finally { if (opened) EditorSceneManager.CloseScene(reference, true); }
    }

    private static Transform InstallBridgeArt(Transform canvas, Transform source, S6Controller controller)
    {
        Transform art = canvas.Find("Bridge object art");
        if (art != null) return art;
        Transform previous = canvas.Find("Location artwork");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        CopyArtwork(source.Find("Location artwork"), canvas, 0);
        controller.bridgeLightLayer = CopyArtwork(source.Find("Emergency lighting"), canvas, 1).gameObject;
        return CopyArtwork(source.Find("Bridge object art"), canvas, 2);
    }

    private static Transform CopyArtwork(Transform source, Transform parent, int sibling)
    {
        var clone = Object.Instantiate(source.gameObject, parent, false).transform;
        clone.name = source.name;
        clone.SetSiblingIndex(sibling);
        foreach (Graphic graphic in clone.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        return clone;
    }

    private static void AlignReturnShortcut(Transform canvas, Button button, Transform reference)
    {
        AlignArrow(canvas, button, reference);
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition += new Vector2(134f, 0f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    private static NavigationArrowHover AlignArrow(Transform canvas, Button button, Transform reference)
    {
        Image face = button.GetComponent<Image>();
        CopyRect(face.rectTransform, (RectTransform)reference);
        face.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Navigation/WhiteArrow.png");
        face.type = Image.Type.Simple;
        face.color = Color.white;
        face.preserveAspect = true;
        button.transition = Selectable.Transition.None;
        LateUi.Caption(button).gameObject.SetActive(false);

        Text hint = canvas.Find("Navigation hint")?.GetComponent<Text>();
        if (hint == null)
        {
            hint = LateUi.Label(canvas, "Navigation hint", "", 460, 990, 1000, 48, 26,
                new Color(1f, 0.91f, 0.65f), TextAnchor.MiddleCenter);
            hint.gameObject.AddComponent<Outline>().effectColor = Color.black;
        }
        Image edge = button.transform.Find("Arrow hover")?.GetComponent<Image>();
        if (edge == null)
        {
            edge = new GameObject("Arrow hover", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            edge.transform.SetParent(button.transform, false);
            edge.rectTransform.anchorMin = Vector2.zero;
            edge.rectTransform.anchorMax = Vector2.one;
            edge.rectTransform.offsetMin = edge.rectTransform.offsetMax = Vector2.zero;
        }
        edge.sprite = face.sprite;
        edge.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UI/CorridorHoverOutline.mat");
        edge.preserveAspect = true;
        edge.raycastTarget = false;
        edge.enabled = false;
        var hover = button.GetComponent<NavigationArrowHover>();
        if (hover == null) hover = button.gameObject.AddComponent<NavigationArrowHover>();
        hover.Configure(edge, hint, "");
        return hover;
    }

    private static void ApplyRoom(Transform canvas, S4Controller finale, int chapter)
    {
        Transform art = canvas.Find("Location artwork");
        if (art == null || art.Find("Room Upper") == null)
        {
            foreach (string name in new[] { "Location artwork", "Day window", "Night window", "Left curtain", "Fifth chair", "Night wash" })
            {
                Transform old = canvas.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            Transform source = finale.transform.Find("S2 Canvas/Location artwork");
            art = Object.Instantiate(source.gameObject, canvas, false).transform;
            art.name = source.name;
            art.SetAsFirstSibling();
            bool night = chapter == 7;
            art.Find("Room Lower").gameObject.SetActive(night);
            art.Find("S4 Day Window").gameObject.SetActive(!night);
            art.Find("Room Fifth Chair").gameObject.SetActive(true);
            art.Find("S4 Memorial Box Outline").GetComponent<Image>().enabled = false;
        }

        var memorial = canvas.Find("Memorial box").GetComponent<Button>();
        AlignHotspot(memorial, finale.badgeButton.transform, art.Find("S4 Memorial Box Outline").GetComponent<Graphic>());
        AlignHotspot(canvas.Find("Computer").GetComponent<Button>(), finale.computerButton.transform);
        if (chapter == 5)
        {
            S5Controller controller = canvas.GetComponentInParent<S5Controller>();
            // The new note sits on the near edge of the same computer table.
            var note = (RectTransform)controller.ruleButton.transform;
            note.anchoredPosition = new Vector2(720f, -765f);
            note.sizeDelta = new Vector2(200f, 62f);
            WithReference("Assets/Scenes/S1.unity", scene =>
            {
                Transform exit = FindCanvas(scene).Find("5F Activity Room/Room interactions/→ 5F 走廊");
                AlignArrow(canvas, controller.waitButton, exit).SetDestination("等到熄灯后离开活动室");
            });
        }
        if (chapter == 7)
        {
            S7Controller controller = canvas.GetComponentInParent<S7Controller>();
            AlignHotspot(controller.teamButton);
            AlignHotspot(controller.reviewButton, finale.computerButton.transform);
            AlignHotspot(controller.verifyButton, finale.computerButton.transform);
            AlignHotspot(controller.restButton, finale.restButton.transform);
            if (controller.restButton.transform.Find("Navigation arrow") == null)
            {
                var size = ((RectTransform)controller.restButton.transform).sizeDelta;
                GameplayUiSkin.InstallArrowFace(controller.restButton.transform,
                    new Vector2(size.x * 0.5f, -size.y * 0.5f), 108f, 0f);
            }
        }
        if (chapter == 8)
            AlignHotspot(canvas.Find("Materials").GetComponent<Button>());
    }

    private static void AlignHotspot(Button button, Transform reference = null, Graphic highlight = null)
    {
        var target = (RectTransform)button.transform;
        if (reference != null) CopyRect(target, (RectTransform)reference);

        button.transition = Selectable.Transition.None;
        button.targetGraphic.color = Color.clear;
        if (button.targetGraphic is Image hit) hit.sprite = null;
        Text caption = LateUi.Caption(button);
        if (highlight != null) caption.gameObject.SetActive(false);
        else
        {
            caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            caption.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            caption.rectTransform.anchoredPosition = Vector2.zero;
            caption.rectTransform.sizeDelta = new Vector2(Mathf.Min(300f, target.sizeDelta.x), 60f);
            caption.enabled = false;
        }

        var hover = button.GetComponent<HotspotHoverOutline>();
        if (hover == null) hover = button.gameObject.AddComponent<HotspotHoverOutline>();
        var serialized = new SerializedObject(hover);
        serialized.FindProperty("highlight").objectReferenceValue = highlight != null ? highlight : caption;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyRect(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private static void InstallBadgeArtwork(Transform panel)
    {
        Transform current = panel.Find("Badge artwork");
        if (current != null)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject) != null) return;
            Object.DestroyImmediate(current.gameObject);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BadgeArtworkPath);
        GameObject artwork = (GameObject)PrefabUtility.InstantiatePrefab(prefab, panel);
        artwork.name = "Badge artwork";
        var rect = (RectTransform)artwork.transform;
        rect.anchorMin = new Vector2(0f, 0.18f);
        rect.anchorMax = new Vector2(1f, 0.74f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-60f, 0f);
        artwork.SetActive(false);
    }
}
