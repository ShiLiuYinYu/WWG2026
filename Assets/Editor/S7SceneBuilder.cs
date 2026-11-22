using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>
/// Builds S7 (归队确认) — 5F activity room at night, corridor still dark, the fifth chair empty.
/// Four P6 steps plus the rainy-morning handoff to S8.
/// </summary>
public static class S7SceneBuilder
{
    [MenuItem("WWG/Setup/Build S7 Reunion Room")]
    public static void Build()
    {
        LateStageSceneKit.RunBuilders("S7", () =>
        {
            const string path = "Assets/Scenes/S7.unity";
            if (LateStageSceneKit.SceneExists(path)) throw new System.InvalidOperationException("场景已存在：" + path);
            GameObject root = LateStageSceneKit.NewScene("S7 Reunion ClubRoom", typeof(S7Controller), "S7 Canvas", null);
            S7Controller controller = root.GetComponent<S7Controller>();
            Transform canvas = root.transform.Find("S7 Canvas");

            controller.timeText = LateStageSceneKit.Clock(canvas);
            controller.feedbackText = LateStageSceneKit.Feedback(canvas);

            controller.infoPanel = LateStageSceneKit.InfoPanel(canvas, out controller.infoTitle, out controller.infoBody);
            controller.recordPanel = LateStageSceneKit.RecordPanel(canvas, out controller.recordText);
            controller.puzzlePanel = LateStageSceneKit.PuzzlePanel(canvas, out controller.puzzleTitle,
                out controller.puzzleBody, out controller.puzzleFeedback,
                out controller.puzzleA, out controller.puzzleB, out controller.puzzleC, out controller.puzzleClose);

            controller.badgeButton = Prop(canvas, "Memorial box", "纪念盒", 1500, 330, 330, 58);
            controller.rosterButton = Prop(canvas, "Computer", "名单副本", 690, 620, 280, 56);
            controller.teamButton = Prop(canvas, "Team confirm", "我已归队", 40, 700, 260, 56);
            controller.reviewButton = Prop(canvas, "Evidence review", "与队友对照", 40, 780, 300, 56);
            controller.verifyButton = Prop(canvas, "Verify again", "再验一次", 330, 700, 260, 56);
            controller.restButton = Prop(canvas, "Rest", "休息到周日上午", 330, 780, 300, 56);

            LateStageScenePresentation.Apply(canvas, 7);
            LateStageSceneKit.SaveScene(path);
        });
    }

    private static Button Prop(Transform canvas, string name, string caption, float x, float y, float w, float h)
    {
        return LateUi.Button(canvas, name, caption, x, y, w, h, 27);
    }
}
