using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>
/// Builds the five S6 (第二次熄灯) scenes: 5F 走廊 → 单数栋 4F 入口 → 连廊中段 → 偶数栋资料区 / 3F 机房.
/// All five share S6Controller and the same canvas layout; only the background and the location label differ.
/// Each scene carries every prop button and shows only the ones its location uses.
/// </summary>
public static class S6SceneBuilder
{
    private struct Spot
    {
        public string Scene, Root, Background, Location;

        public Spot(string scene, string root, string background, string location)
        {
            Scene = scene; Root = root; Background = background; Location = location;
        }
    }

    [MenuItem("WWG/Setup/Build S6 Night Walk")]
    public static void Build()
    {
        LateStageSceneKit.RunBuilders("S6", () =>
        {
            Spot[] spots =
            {
                new Spot("Assets/Scenes/S6/S6_Corridor.unity", "S6 Corridor5F",
                    "Assets/Resources/S1/CorridorNight.png", "单数栋 · 5F 走廊"),
                new Spot("Assets/Scenes/S6/S6_BridgeOdd4F.unity", "S6 BridgeOdd4F",
                    "Assets/Art/Bridge4F/BridgeNightOff.png", "单数栋 · 4F 连廊入口"),
                new Spot("Assets/Scenes/S6/S6_BridgeSpan4F.unity", "S6 BridgeSpan4F",
                    "Assets/Art/Bridge4F/BridgeNightOff.png", "4F · 玻璃连廊中段"),
                new Spot("Assets/Scenes/S6/S6_BridgeEven4F.unity", "S6 BridgeEven4F",
                    "Assets/Art/Bridge4F/CabinetEvenNight.png", "偶数栋 · 4F 资料区"),
                new Spot("Assets/Scenes/S6/S6_ServerRoom3F.unity", "S6 ServerRoom3F",
                    "Assets/Resources/S3/ServerRoom3F.png", "单数栋 · 3F 机房")
            };

            foreach (Spot spot in spots)
                if (LateStageSceneKit.SceneExists(spot.Scene))
                    throw new System.InvalidOperationException("场景已存在：" + spot.Scene);
            foreach (Spot spot in spots) BuildScene(spot);
        });
    }

    private static void BuildScene(Spot spot)
    {
        Debug.Log("[WWG] 构建 " + spot.Scene);
        GameObject root = LateStageSceneKit.NewScene(spot.Root, typeof(S6Controller), "S6 Canvas", spot.Background);
        S6Controller controller = root.GetComponent<S6Controller>();
        Transform canvas = root.transform.Find("S6 Canvas");

        controller.timeText = LateStageSceneKit.Clock(canvas);
        controller.feedbackText = LateStageSceneKit.Feedback(canvas);

        controller.infoPanel = LateStageSceneKit.InfoPanel(canvas, out controller.infoTitle, out controller.infoBody);
        controller.recordPanel = LateStageSceneKit.RecordPanel(canvas, out controller.recordText);
        controller.puzzlePanel = LateStageSceneKit.PuzzlePanel(canvas, out controller.puzzleTitle,
            out controller.puzzleBody, out controller.puzzleFeedback,
            out controller.puzzleA, out controller.puzzleB, out controller.puzzleC, out controller.puzzleClose);

        controller.navA = Nav(canvas, "Nav forward", 40);
        controller.navB = Nav(canvas, "Nav back", 400);
        controller.navC = Nav(canvas, "Nav down", 760);

        controller.emergencySwitch = Prop(canvas, "Emergency switch", "应急灯开关", 60, 300, 300, 60);
        controller.guideBoard = Prop(canvas, "Guide board", "导览展板", 60, 380, 300, 60);
        controller.glass = Prop(canvas, "Bridge glass", "看中段玻璃", 60, 380, 300, 60);
        controller.cabinet = Prop(canvas, "Data cabinet", "打开资料柜", 60, 460, 300, 60);
        controller.serverMachine = Prop(canvas, "Server machine", "靠窗第 3 台", 60, 460, 320, 60);

        LateStageScenePresentation.Apply(canvas, 6);
        LateStageSceneKit.SaveScene(spot.Scene);
    }

    private static Button Nav(Transform canvas, string name, float x)
    {
        return LateUi.Button(canvas, name, "", x, 860, 340, 66);
    }

    private static Button Prop(Transform canvas, string name, string caption, float x, float y, float w, float h)
    {
        return LateUi.Button(canvas, name, caption, x, y, w, h, 27);
    }
}
