using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>
/// Builds the S5 复盘 scene (5F club room, Saturday afternoon). The room is the same 5F club room art the
/// player saw in S1/S2/S4, so returning there is recognisable; only the late-chapter UI is new.
/// </summary>
public static class S5SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/S5.unity";

    [MenuItem("WWG/Setup/Build S5 Replay Room")]
    public static void BuildFromMenu()
    {
        LateStageSceneKit.RunBuilders("S5 复盘场景", Build);
    }

    public static void Build()
    {
        LateStageSceneKit.RequireIdle("S5 复盘场景");
        if (LateStageSceneKit.SceneExists(ScenePath)) throw new System.InvalidOperationException("场景已存在：" + ScenePath);

        GameObject root = LateStageSceneKit.NewScene("S5 Replay ClubRoom", typeof(S5Controller), "S5 Canvas", null);
        Transform canvas = root.transform.Find("S5 Canvas");
        S5Controller controller = root.GetComponent<S5Controller>();

        Text time = LateStageSceneKit.Clock(canvas);
        Text feedback = LateStageSceneKit.Feedback(canvas);

        // Props and people stay where the room art puts them.
        Button badge = Prop(canvas, "Memorial box", "纪念盒", 1480, 300, 330, 58);
        Button note = Prop(canvas, "Old test note", "未核实的归队规则", 40, 300, 360, 58);
        Button computer = Prop(canvas, "Computer", "电脑", 700, 610, 260, 56);
        Button wait = Prop(canvas, "Wait for lights", "等到熄灯", 40, 430, 260, 56);
        Button yuan = LateUi.Button(canvas, "Yuan", "袁祁", 330, 490, 270, 330, 26);
        Button xu = LateUi.Button(canvas, "Xu", "许圆", 700, 450, 265, 300, 26);
        Button tang = Person(canvas, "Tang", "唐澈", 40, 770);

        LateUi.SetActive(wait, false);

        GameObject infoPanel = LateStageSceneKit.InfoPanel(canvas, out Text infoTitle, out Text infoBody);

        GameObject recordPanel = LateStageSceneKit.RecordPanel(canvas, out Text recordText);

        // P4 evidence board: frame cards for the 与队友对照 branch, status logs for the 独自查看 branch.
        GameObject casePanel = LateUi.Panel(canvas, "Case panel", 210, 105, 1500, 870, LateUi.Ink);
        Text caseTitle = LateUi.Label(casePanel.transform, "Title", "", 60, 28, 1380, 70, 40, LateUi.Accent, TextAnchor.MiddleLeft);
        Text caseBody = LateUi.Label(casePanel.transform, "Body", "", 70, 110, 1360, 500, 31, LateUi.Paper, TextAnchor.UpperLeft);
        Text caseRows = LateUi.Label(casePanel.transform, "Rows", "", 70, 120, 1360, 480, 32, LateUi.Paper, TextAnchor.UpperLeft);
        Button frame1 = LateUi.Button(casePanel.transform, "Frame one", "第一张 · 椅子四把", 70, 630, 430, 70);
        Button frame2 = LateUi.Button(casePanel.transform, "Frame two", "第二张 · 纸团在插线板左侧", 520, 630, 500, 70);
        Button frame3 = LateUi.Button(casePanel.transform, "Frame three", "第三张 · 名单抄件", 1040, 630, 390, 70);
        Button logA = LateUi.Button(casePanel.transform, "Log A", "日志 A · 椅子 4 / 状态 4", 70, 630, 480, 70);
        Button logB = LateUi.Button(casePanel.transform, "Log B", "日志 B · 椅子 5 / 状态 5", 570, 630, 480, 70);
        Button replay = LateUi.Button(casePanel.transform, "Replay", "回放这一段", 70, 720, 400, 66);
        Text caseFeedback = LateUi.Label(casePanel.transform, "Case feedback", "", 70, 800, 1100, 50, 28, LateUi.PaperDim, TextAnchor.MiddleLeft);
        Button caseClose = LateUi.Button(casePanel.transform, "Close", "收起", 1240, 786, 190, 60);
        casePanel.SetActive(false);

        controller.timeText = time;
        controller.feedbackText = feedback;
        controller.recordText = recordText;
        controller.infoTitle = infoTitle;
        controller.infoBody = infoBody;
        controller.infoPanel = infoPanel;
        controller.recordPanel = recordPanel;
        controller.casePanel = casePanel;
        controller.caseTitle = caseTitle;
        controller.caseBody = caseBody;
        controller.caseRows = caseRows;
        controller.caseFeedback = caseFeedback;
        controller.badgeButton = badge;
        controller.ruleButton = note;
        controller.computerButton = computer;
        controller.waitButton = wait;
        controller.yuanButton = yuan;
        controller.xuButton = xu;
        controller.tangButton = tang;
        controller.frameOne = frame1;
        controller.frameTwo = frame2;
        controller.frameThree = frame3;
        controller.logA = logA;
        controller.logB = logB;
        controller.replayButton = replay;
        controller.caseClose = caseClose;

        LateStageScenePresentation.Apply(canvas, 5);
        LateStageSceneKit.SaveScene(ScenePath);
        Selection.activeGameObject = root;
        Debug.Log("[WWG] S5 场景已生成：" + ScenePath);
    }

    private static Button Prop(Transform canvas, string name, string caption, float x, float y, float w, float h)
    {
        Button button = LateUi.Button(canvas, name, caption, x, y, w, h, 27);
        return button;
    }

    private static Button Person(Transform canvas, string name, string caption, float x, float y)
    {
        return LateUi.Button(canvas, name, caption, x, y, 210, 52, 26);
    }
}
