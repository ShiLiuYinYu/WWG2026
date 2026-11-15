using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

public sealed class S0Controller : MonoBehaviour
{
    [Serializable]
    private struct Line
    {
        public string speaker;
        public string body;
        public Line(string speaker, string body) { this.speaker = speaker; this.body = body; }
    }

    public GameObject exterior;
    public GameObject corridor;
    public GameObject clubRoom;
    public GameObject bridge;
    public GameObject[] interactionLayers;
    public GameObject dialoguePanel;
    public GameObject infoPanel;
    public GameObject nightTint;
    public GameObject fallenBox;
    public GameObject fallenBoxHotspot;
    public Text speakerText;
    public Text dialogueText;
    public Text infoTitle;
    public Text infoBody;
    public Text hoverText;
    public Text timeText;
    public Text objectiveText;
    public Text progressText;
    public Text memoText;
    public GameObject memoPanel;
    public GameObject completionPanel;
    public Text completionText;
    public Image roomBackdrop;
    public Sprite nightRoomSprite;
    public AudioSource audioSource;
    public AudioClip deskWorkSound;
    public AudioClip breakerSound;
    public Image portraitImage;
    public Sprite yuanPortrait;
    public Sprite tangPortrait;
    public Sprite xuPortrait;

    private Sprite dayRoomSprite;

    private readonly Queue<Line> lines = new Queue<Line>();
    private Action afterDialogue;
    private int phase; // 0 arrival, 1 opening, 2 daytime investigation, 3 transition, 4 S0 complete
    private int screen; // 0 exterior, 1 corridor, 2 club room, 3 bridge
    private bool boardRead;
    private bool chairRead;
    private bool signRead;
    private bool timerRead;
    private bool guideRead;
    private bool memoOpen;
    private bool infoOpen;
    private bool transitionBusy;

    private sealed class TravelState
    {
        public int phase;
        public int screen;
        public bool boardRead, chairRead, signRead, timerRead, guideRead;
        public string arrivalLine;
    }

    [Serializable]
    private sealed class ManualCheckpoint
    {
        public int phase, screen;
        public bool boardRead, chairRead, signRead, timerRead, guideRead;
    }

    private const string ManualSaveKey = "WWG_S0_ManualCheckpoint";

    private static TravelState pendingTravel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTravel() { pendingTravel = null; }

    public bool IsGameplayModalOpen => dialoguePanel.activeSelf || infoPanel.activeSelf ||
        memoPanel.activeSelf || completionPanel.activeSelf || transitionBusy;

    public bool SaveManualCheckpoint()
    {
        if (transitionBusy || phase == 3) return false;
        var data = new ManualCheckpoint { phase = phase, screen = screen, boardRead = boardRead,
            chairRead = chairRead, signRead = signRead, timerRead = timerRead, guideRead = guideRead };
        PlayerPrefs.SetString(ManualSaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 0);
        PlayerPrefs.Save();
        return true;
    }

    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (transitionBusy || phase == 3) return null;
        var data = new ManualCheckpoint { phase = phase, screen = screen, boardRead = boardRead,
            chairRead = chairRead, signRead = signRead, timerRead = timerRead, guideRead = guideRead };
        return new SaveSlotSnapshot { chapter = "S0", payload = JsonUtility.ToJson(data),
            gameTime = timeText.text, locationLabel = "S0 · " + timeText.text + " · " + GameFlow.S0Location(screen) };
    }

    public bool LoadManualCheckpoint()
    {
        if (!PlayerPrefs.HasKey(ManualSaveKey) || transitionBusy) return false;
        var data = JsonUtility.FromJson<ManualCheckpoint>(PlayerPrefs.GetString(ManualSaveKey));
        if (data == null) return false;
        phase = data.phase; screen = data.screen; boardRead = data.boardRead;
        chairRead = data.chairRead; signRead = data.signRead; timerRead = data.timerRead;
        guideRead = data.guideRead;
        dialoguePanel.SetActive(false); infoPanel.SetActive(false); memoPanel.SetActive(false);
        completionPanel.SetActive(false); infoOpen = false; memoOpen = false;
        var destination = GameFlow.S0Location(screen);
        if (SceneManager.GetActiveScene().name != destination)
        {
            pendingTravel = new TravelState { phase = phase, screen = screen, boardRead = boardRead,
                chairRead = chairRead, signRead = signRead, timerRead = timerRead, guideRead = guideRead,
                arrivalLine = "已读取白天调查存档。" };
            return GameFlow.LoadLocation(destination);
        }
        ApplyScreen();
        return true;
    }

    private const string DirectoryText =
        "实验楼导览｜单数栋 / 偶数栋\n\n" +
        "1F  电气与电子工程\n2F  机器人工程\n3F  计算机工程\n4F  外国语学院（跨栋连廊）\n5F  数智设计学院\n\n" +
        "【物业通知】工作日 23:00；周五、周六 23:30，走廊、楼梯及连廊等公共区域照明自动断电。\n" +
        "实验室与社团活动室的内部插座独立供电，夜间不断电。";

    private void Start()
    {
        Application.runInBackground = true;
        // A system CJK font is used at runtime; the editor asset stays portable.
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
        if (font != null)
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;

        TravelState travel = pendingTravel;
        pendingTravel = null;
        phase = travel?.phase ?? 0;
        screen = travel?.screen ?? Mathf.Max(0, GameFlow.S0LocationIndex(SceneManager.GetActiveScene().name));
        if (travel != null)
        {
            boardRead = travel.boardRead; chairRead = travel.chairRead; signRead = travel.signRead;
            timerRead = travel.timerRead; guideRead = travel.guideRead;
        }
        if (roomBackdrop != null) dayRoomSprite = roomBackdrop.sprite;
        dialoguePanel.SetActive(false);
        infoPanel.SetActive(false);
        memoPanel.SetActive(false);
        completionPanel.SetActive(false);
        nightTint.SetActive(false);
        fallenBox.SetActive(false);
        ApplyScreen();
        if (PlayerPrefs.GetInt("WWG_S0_LoadManual", 0) == 1)
        {
            PlayerPrefs.DeleteKey("WWG_S0_LoadManual");
            if (LoadManualCheckpoint()) return;
        }
        if (travel != null && screen == 2 && phase == 0)
        {
            phase = 1;
            UpdateHUD();
            PlayOpening();
        }
        else if (travel != null && !string.IsNullOrEmpty(travel.arrivalLine))
            Say(new[] { new Line("我", travel.arrivalLine) });
        else if (travel == null && screen == 0)
            Say(new[] {
                new Line("我", "现在周五下午三点四十。距离四十八小时极限开发开题，还有二十分钟。得赶快了。"),
                new Line("我", "袁祁在群里发了三次定位，让我到了直接上单数栋五楼的活动室集合。")
            });
    }

    private void ShowScreen(int index, string arrivalLine = null)
    {
        string destination = GameFlow.S0Location(index);
        if (SceneManager.GetActiveScene().name != destination)
        {
            pendingTravel = new TravelState { phase = phase, screen = index, boardRead = boardRead,
                chairRead = chairRead, signRead = signRead, timerRead = timerRead,
                guideRead = guideRead, arrivalLine = arrivalLine };
            if (!GameFlow.LoadLocation(destination)) pendingTravel = null;
            return;
        }
        screen = index;
        ApplyScreen();
    }

    private void ApplyScreen()
    {
        if (exterior != null) exterior.SetActive(screen == 0);
        if (corridor != null) corridor.SetActive(screen == 1);
        if (clubRoom != null) clubRoom.SetActive(screen == 2);
        if (bridge != null) bridge.SetActive(screen == 3);
        if (roomBackdrop != null) roomBackdrop.sprite = phase == 4 ? nightRoomSprite : dayRoomSprite;
        nightTint.SetActive(phase == 4 && screen == 2);
        fallenBox.SetActive(phase == 4 && screen == 2);
        SetHover("");
        UpdateHUD();
        UpdateInteractionVisibility();
    }

    private void UpdateInteractionVisibility()
    {
        bool visible = !dialoguePanel.activeSelf && !infoPanel.activeSelf && !memoPanel.activeSelf &&
                       !completionPanel.activeSelf && !transitionBusy;
        if (interactionLayers != null)
            foreach (GameObject layer in interactionLayers)
                if (layer != null) layer.SetActive(visible);
        if (fallenBoxHotspot != null) fallenBoxHotspot.SetActive(visible && phase == 4 && screen == 2);
        if (!visible) SetHover("");
    }

    public void SetHover(string label) { hoverText.text = label; }

    public void HandleHotspot(string action)
    {
        if (dialoguePanel.activeSelf || infoOpen || memoOpen || transitionBusy || completionPanel.activeSelf) return;
        switch (action)
        {
            case "directory":
                boardRead = true;
                ShowInfo("门口楼层展板", DirectoryText, () => Say(new[] {
                    new Line("我", "十一点半只断走廊的灯，屋里插座不断电……这么好的环境，怪不得袁祁非要申请在社团活动室通宵。") }));
                break;
            case "exteriorBridge":
                Say(new[] { new Line("我", "四楼那条玻璃连廊串联着两栋楼。太阳还没落山，阳光刺得看不清里面。") });
                break;
            case "enter":
                if (!boardRead) Say(new[] { new Line("我", "先看看门口的楼层展板，确认单数栋五楼怎么走。") });
                else ShowScreen(1, "单数栋五楼。活动室就在走廊这边。");
                break;
            case "toRoom":
                ShowScreen(2);
                break;
            case "toCorridor":
                if (phase == 4) Say(new[] { new Line("我", "那张照片还在脚边。先看看掉出来的是什么。") });
                else ShowScreen(1);
                break;
            case "toBridge":
                if (phase < 2) Say(new[] { new Line("我", "先去活动室和大家集合，开题快开始了。") });
                else ShowScreen(3, "单数栋四楼。连廊通往对面的偶数栋。");
                break;
            case "bridgeBack": ShowScreen(1); break;
            case "chair":
                if (phase < 2) break;
                chairRead = true;
                Say(new[] {
                    new Line("我", "第五把椅子被袁祁推到了铁皮储物柜旁边。柜子顶上塞着个旧纸盒，感觉随时会掉下来。"),
                    new Line("许圆", "椅背上还贴着去年的旧编号贴纸，字都磨没了。拍参考图的话，要不要把它移走？"),
                    new Line("我", "先别动了，晚上腾不出地方再说吧。")
                });
                UpdateHUD();
                break;
            case "sign":
                signRead = true;
                Say(new[] { new Line("我", "单数栋 5F，数智设计学院。安全出口指示灯常亮，往下是去 4F 和 3F 的楼梯。") });
                UpdateHUD();
                break;
            case "timer":
                timerRead = true;
                ShowInfo("照明定时箱", "周五、周六 23:30 自动切断走廊与楼梯照明。请勿私自扳动开关。\n\n从活动室门口到楼梯口只有十几步，但走廊没有外窗。", null);
                UpdateHUD();
                break;
            case "floorPlaque":
                ShowInfo("单数栋 5F · 数智设计学院", "← 游戏开发协会活动室\n↓ 4F 连廊 / 3F 机房\n\n其余实验室门禁关闭。", null);
                break;
            case "guide":
                guideRead = true;
                ShowInfo("双栋导览练习稿｜社团旧资料", "去年社团留下的平面练习稿。\n\n单数栋 —— 4F 玻璃连廊 —— 偶数栋\n\n红笔批注：巡场顺序，先去单数栋，再去偶数栋；从连廊口向外走。\n\n两端墙上各有近、远两张海报。远端海报被新招募贴纸盖住一层。", null);
                UpdateHUD();
                break;
            case "glass":
                Say(new[] { new Line("我", "透过连廊玻璃能看见对面的偶数栋。两边的教室门都锁着。") });
                break;
            case "switch":
                ShowInfo("连廊应急照明", "墙上有个独立的『连廊应急照明』开关。现在是白天，还不用打开。", null);
                break;
            case "leader":
                if (phase != 2) break;
                if (!AllMemories) Say(new[] { new Line("袁祁", "先把活动室、五楼走廊和四楼连廊入口都看一遍。拍完参考图回来找我。") });
                else BeginTransition();
                break;
            case "photo":
                if (phase == 4)
                {
                    PlayerPrefs.SetInt("WWG_S1_EnterFresh", 1);
                    PlayerPrefs.Save();
                    GameFlow.LoadLocation(GameFlow.S1Scene);
                }
                break;
        }
    }

    private bool AllMemories => chairRead && signRead && timerRead && guideRead;

    private void PlayOpening()
    {
        Say(new[] {
            new Line("袁祁", "可算来了！快把包放下，马上还有五分钟直播开题了。"),
            new Line("唐澈", "插线板给你留了靠边那个口，你工位就在那儿。"),
            new Line("许圆", "我把数位板接好了。你直接插电脑上就能用了。"),
            new Line("袁祁", "靠窗那把空椅子我顺手推到储物柜边上了，省得挡路。"),
            new Line("许圆", "我们当初报名的时候是四个人吧？怎么多出来一把椅子？"),
            new Line("唐澈", "可能是之前参加活动的人搬过来的，就放那儿吧。"),
            new Line("直播主持 · 16:00", "各位参赛队伍下午好！本届 48H GameJam 的创作主题是——『选择』。周日下午 16:00 前做出可玩的作品，之后大家一起展示。"),
            new Line("唐澈", "……一个词的题怎么办啊？策划救一下，咱怎么做？"),
            new Line("袁祁", "得让玩家的选择真正影响游戏流程。你们觉得呢？"),
            new Line("许圆", "我们只有四十八个小时。要不拿这栋实验楼当原型？白天做两层，熄灯后做两层。"),
            new Line("我", "那就先去楼里转一圈，拍点走廊和连廊当参考图。"),
            new Line("袁祁", "行，你去踩点。我们仨先把工程仓库和基础框架搭起来。拍完回来找我。")
        }, () => { phase = 2; UpdateHUD(); });
    }

    private void BeginTransition()
    {
        phase = 3;
        UpdateHUD();
        Say(new[] { new Line("袁祁", "参考图拍够了吗？来，趁现在脑子还清醒，我们把前三个场景的交互逻辑顺一遍。") },
            TransitionSequence);
    }

    private void TransitionSequence()
    {
        transitionBusy = true;
        UpdateInteractionVisibility();
        if (deskWorkSound != null) audioSource.PlayOneShot(deskWorkSound, 0.65f);
        Say(new[] {
            new Line("时间 · 18:30", "外卖袋堆上了桌角。白板上画满第一版流程图。"),
            new Line("时间 · 21:15", "唐澈喝掉第二罐咖啡。许圆把活动室布局画进背景草图。"),
            new Line("时间 · 23:29", "公共照明熄灭倒计时。"),
            new Line("唐澈", "二十九分了。我先把工程保存一下，万一跳闸，没保存的文件就完了。"),
            new Line("袁祁", "今晚大家轮流眯一会儿。明天下午四点，我们要凑出第一版能跑的 Demo。")
        }, LightsOut);
    }

    private void LightsOut()
    {
        if (breakerSound != null) audioSource.PlayOneShot(breakerSound, 0.75f);
        phase = 4;
        PlayerPrefs.SetInt("WWG_S0_Complete", 1);
        PlayerPrefs.Save();
        ShowScreen(2);
        transitionBusy = false;
        Say(new[] {
            new Line("时间 · 23:30", "咔哒——嗡。走廊的灯由远及近，逐盏熄灭。"),
            new Line("我", "门玻璃外的走廊暗了。屋里的顶灯、电脑屏幕还亮着。"),
            new Line("我", "一声闷响。储物柜顶的旧纸盒掉到了第五把椅子旁，滑出一张背面朝上的照片。")
        });
    }

    public void ContinueDialogue()
    {
        if (!dialoguePanel.activeSelf) return;
        if (lines.Count > 0)
        {
            Line line = lines.Dequeue();
            speakerText.text = line.speaker;
            dialogueText.text = line.body;
            portraitImage.sprite = line.speaker == "袁祁" ? yuanPortrait : line.speaker == "唐澈" ? tangPortrait : line.speaker == "许圆" ? xuPortrait : null;
            bool hasPortrait = portraitImage.sprite != null;
            portraitImage.enabled = hasPortrait;
            speakerText.rectTransform.anchoredPosition = new Vector2(hasPortrait ? 290 : 36, -18);
            dialogueText.rectTransform.anchoredPosition = new Vector2(hasPortrait ? 290 : 36, -68);
            dialogueText.rectTransform.sizeDelta = new Vector2(hasPortrait ? 1480 : 1745, 115);
        }
        else
        {
            dialoguePanel.SetActive(false);
            Action callback = afterDialogue;
            afterDialogue = null;
            callback?.Invoke();
            UpdateInteractionVisibility();
        }
    }

    private void Say(IEnumerable<Line> dialogue, Action callback = null)
    {
        lines.Clear();
        foreach (Line line in dialogue) lines.Enqueue(line);
        afterDialogue = callback;
        dialoguePanel.SetActive(true);
        UpdateInteractionVisibility();
        ContinueDialogue();
    }

    private Action afterInfo;
    private void ShowInfo(string title, string body, Action callback)
    {
        infoTitle.text = title;
        infoBody.text = body;
        afterInfo = callback;
        infoOpen = true;
        infoPanel.SetActive(true);
        UpdateInteractionVisibility();
    }

    public void CloseInfo()
    {
        infoPanel.SetActive(false);
        infoOpen = false;
        Action callback = afterInfo;
        afterInfo = null;
        callback?.Invoke();
        UpdateInteractionVisibility();
    }

    public void ToggleMemo()
    {
        if (dialoguePanel.activeSelf || infoOpen || transitionBusy) return;
        memoOpen = !memoOpen;
        memoPanel.SetActive(memoOpen);
        UpdateInteractionVisibility();
        if (!memoOpen) return;
        memoText.text = "【建筑导览】\n" + (boardRead ? DirectoryText : "尚未查阅门口展板。") +
            "\n\n【白天取景】\n" +
            (chairRead ? "✓ 活动室：第五把椅子、白色地胶框和柜顶旧纸盒。\n" : "○ 活动室：靠窗区域待查看。\n") +
            (signRead && timerRead ? "✓ 5F 走廊：安全出口与 23:30 公共照明定时箱。\n" : "○ 5F 走廊：安全指示牌和定时箱待查看。\n") +
            (guideRead ? "✓ 4F 连廊：先单数栋、后偶数栋；从连廊口向外。" : "○ 4F 连廊：导览练习稿待查看。");
    }

    private void UpdateHUD()
    {
        if (timeText == null) return;
        timeText.text = phase == 4 ? "周五 23:30" : phase == 3 ? "周五 16:30 → 23:30" : phase == 2 ? "周五 16:10" : phase == 1 ? "周五 15:55" : "周五 15:40";
        if (phase == 4) objectiveText.text = "储物柜的旧纸盒掉了下来……";
        else if (phase == 3) objectiveText.text = "与队友整理关卡草图";
        else if (phase == 2) objectiveText.text = AllMemories ? "回 5F 活动室，向袁祁汇报" : "查看活动室、5F 走廊和 4F 连廊入口";
        else objectiveText.text = "前往单数栋 5F 游戏开发协会活动室";
        progressText.text = phase == 4 ? "S0 完成 · 旧照片待调查" : phase == 3 ? "时间推进中" : phase < 2 ? "开题前" : "白天记忆点 " + ((chairRead ? 1 : 0) + (signRead && timerRead ? 1 : 0) + (guideRead ? 1 : 0)) + "/3";
    }
}
