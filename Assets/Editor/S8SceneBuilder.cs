using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>
/// Builds S8 (封包) — 5F activity room in daylight, four finished seats, two possible endings.
/// </summary>
public static class S8SceneBuilder
{
    [MenuItem("WWG/Setup/Build S8 Packaging Room")]
    public static void Build()
    {
        LateStageSceneKit.RunBuilders("S8", () =>
        {
            const string path = "Assets/Scenes/S8.unity";
            if (LateStageSceneKit.SceneExists(path)) throw new System.InvalidOperationException("场景已存在：" + path);
            GameObject root = LateStageSceneKit.NewScene("S8 Packaging ClubRoom", typeof(S8Controller), "S8 Canvas", null);
            S8Controller controller = root.GetComponent<S8Controller>();
            Transform canvas = root.transform.Find("S8 Canvas");

            controller.timeText = LateStageSceneKit.Clock(canvas);
            controller.feedbackText = LateStageSceneKit.Feedback(canvas);

            controller.infoPanel = LateStageSceneKit.InfoPanel(canvas, out controller.infoTitle, out controller.infoBody);
            controller.recordPanel = LateStageSceneKit.RecordPanel(canvas, out controller.recordText);
            controller.puzzlePanel = LateStageSceneKit.PuzzlePanel(canvas, out controller.puzzleTitle,
                out controller.puzzleBody, out controller.puzzleFeedback,
                out controller.puzzleA, out controller.puzzleB, out controller.puzzleC, out controller.puzzleClose);

            controller.materialsButton = Prop(canvas, "Materials", "袁祁的材料", 330, 490, 270, 330);
            controller.computerButton = Prop(canvas, "Computer", "电脑", 690, 620, 240, 56);
            controller.memorialButton = Prop(canvas, "Memorial box", "纪念盒", 1500, 330, 330, 58);

            BuildEnding(canvas, controller);
            LateStageScenePresentation.Apply(canvas, 8);
            LateStageSceneKit.SaveScene(path);
        });
    }

    private static void BuildEnding(Transform canvas, S8Controller controller)
    {
        GameObject panel = LateUi.Panel(canvas, "Ending", 0, 0, 1920, 1080, new Color(0.01f, 0.012f, 0.02f, 1f));
        controller.endingPanel = panel;
        controller.endingText = LateUi.Label(panel.transform, "Ending text", "", 260, 220, 1400, 560, 34,
            LateUi.Paper, TextAnchor.MiddleCenter);
        controller.endingButton = LateUi.Button(panel.transform, "Back to title", "返回标题", 810, 820, 300, 72);
        panel.SetActive(false);
    }

    private static Button Prop(Transform canvas, string name, string caption, float x, float y, float w, float h)
    {
        return LateUi.Button(canvas, name, caption, x, y, w, h, 27);
    }
}
