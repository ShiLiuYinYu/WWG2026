using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>Four fixed views for the S2 bridge investigation. The same GameState travels between views.</summary>
public sealed class S2Controller : MonoBehaviour
{
    public Text locationText, timeText, objectiveText, statusText, feedbackText, recordText, inventoryText;
    public Text farRevealText, codeFeedbackText;
    public Text[] digitTexts;
    public GameObject infoPanel, codePanel, recordPanel, inventoryPanel, pausePanel, glow;
    public Text infoTitle, infoBody;
    public Button drawerButton, switchButton, darkChoiceButton, glassButton;

    private const string SafeScene = "ClubRoom";
    private static GameState pending;
    private static bool pendingDevelopment;
    private GameState state;
    private bool development;
    private int[] digits = { 0, 0, 0, 0 };
    private float dangerSeconds, zeroSanSeconds, idleSeconds, revealedUntil;
    private string revealLocation;
    private int hintStage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPending() { pending = null; pendingDevelopment = false; }

    public static void Begin(GameState handoff, bool editorDevelopment = false)
    {
        if (handoff == null) throw new ArgumentNullException(nameof(handoff));
        if (!handoff.Validate(out string error))
            throw new ArgumentException("S2 交接状态无效：" + error);
        pending = Clone(handoff);
        pendingDevelopment = editorDevelopment;
        if (!GameFlow.LoadLocation(GameFlow.S2StartScene))
            throw new InvalidOperationException("S2 起点未加入 Build Settings");
    }

    private static GameState Clone(GameState source) => JsonUtility.FromJson<GameState>(JsonUtility.ToJson(source));
    public GameState CurrentState => state;
    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (state == null || development) return null;
        return new SaveSlotSnapshot { chapter = "S2", payload = JsonUtility.ToJson(state), gameTime = timeText.text,
            locationLabel = "S2 · " + timeText.text + " · " + locationText.text };
    }

    private void Start()
    {
        Application.runInBackground = true;
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
        if (font != null)
            foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = font;
        infoPanel.SetActive(false); codePanel.SetActive(false);
        recordPanel.SetActive(false); inventoryPanel.SetActive(false); pausePanel.SetActive(false);
        state = pending;
        development = pendingDevelopment;
        pending = null;
        pendingDevelopment = false;
#if UNITY_EDITOR
        if (state == null)
        {
            string bootstrap = UnityEditor.SessionState.GetString("WWG_S2Bootstrap", "");
            if (!string.IsNullOrEmpty(bootstrap))
            {
                UnityEditor.SessionState.EraseString("WWG_S2Bootstrap");
                state = JsonUtility.FromJson<GameState>(bootstrap);
                development = true;
            }
        }
#endif
        if (state == null && GameStateStore.LoadCheckpoint(out GameState saved, out _) &&
            saved.chapterId == StoryIds.Chapter.BridgeRoute)
            state = saved;
        if (state == null)
        {
            Debug.LogError("[S2] 缺少 S1 交接或 S2 检查点。请从 S1 进入，或用 WWG/Dev/Play S2。");
            ShowInfo("无法进入 S2", "请从 S1 到达连廊，或使用编辑器的 WWG/Dev/Play S2。", false);
            return;
        }
        state.chapterId = StoryIds.Chapter.BridgeRoute;
        state.stepId = state.HasFlag(StoryIds.Flag.P2DrawerTaken) ? "p2Complete" : "bridgeReached";
        if (!string.IsNullOrEmpty(state.routeChoice)) state.locationVariant =
            state.locationId != StoryIds.Location.ClubRoom && state.HasFlag(StoryIds.Flag.P2EmergencyLightOn)
                ? StoryIds.Variant.Emergency : StoryIds.Variant.Night;
        UpdateView();
        if (string.IsNullOrEmpty(state.routeChoice))
            feedbackText.text = "入口照明关着。点击开关打开，或暂不开灯观察连廊。";
    }

    private bool ModalOpen => infoPanel.activeSelf || codePanel.activeSelf ||
        recordPanel.activeSelf || inventoryPanel.activeSelf || pausePanel.activeSelf;

    private void Update()
    {
        if (state == null || ModalOpen) return;
        if (!state.HasFlag(StoryIds.Flag.P2DrawerTaken))
        {
            idleSeconds += Time.deltaTime;
            if (hintStage == 0 && idleSeconds >= 45f &&
                (!state.HasFlag(StoryIds.Flag.P2OddFarRead) || !state.HasFlag(StoryIds.Flag.P2EvenFarRead)))
            {
                hintStage = 1;
                feedbackText.text = state.HasFlag(StoryIds.Flag.P2EmergencyLightOn)
                    ? "许圆：远端海报的新贴纸，或许能揭开。"
                    : "我：新贴纸下面像还有一层旧字。试着凝视。";
            }
            if (hintStage < 2 && idleSeconds >= 90f)
            {
                hintStage = 2;
                feedbackText.text = "任务记录已整理目前发现的数字与位置。";
            }
        }
        if (state.locationId != StoryIds.Location.ClubRoom)
        {
            if (!state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
            {
                dangerSeconds += Time.deltaTime;
                if (dangerSeconds >= 20f)
                {
                    dangerSeconds -= 20f;
                    state.san = Mathf.Max(0, state.san - 1);
                    UpdateView();
                }
            }
            if (state.san == 0)
            {
                zeroSanSeconds += Time.deltaTime;
                if (zeroSanSeconds >= 15f)
                {
                    zeroSanSeconds -= 15f;
                    state.health--;
                    if (state.health <= 0)
                    {
                        state.health = 3;
                        state.san = 60;
                        Navigate(StoryIds.Location.BridgeOdd4F);
                        return;
                    }
                    UpdateView();
                }
            }
        }
        if (revealedUntil > 0 && Time.time >= revealedUntil)
        {
            revealedUntil = 0;
            UpdateView();
        }
    }

    public void ChooseDark() => ChooseRoute(StoryIds.Choice.RouteDark);
    private void ChooseRoute(string choice)
    {
        if (state == null || !state.ChooseRoute(choice)) return;
        if (choice == StoryIds.Choice.RouteLight) state.SetFlag(StoryIds.Flag.P2EmergencyLightOn);
        state.locationVariant = choice == StoryIds.Choice.RouteLight
            ? StoryIds.Variant.Emergency : StoryIds.Variant.Night;
        feedbackText.text = choice == StoryIds.Choice.RouteLight
            ? "应急灯亮了。旧残影被玻璃反光冲淡，海报贴纸可以揭开。"
            : "先留在黑暗中。可以凝视远端海报，之后仍可返回开灯。";
        UpdateView();
        SaveCheckpoint(true);
    }

    public void SwitchLight()
    {
        if (state == null) return;
        if (string.IsNullOrEmpty(state.routeChoice))
        {
            ChooseRoute(StoryIds.Choice.RouteLight);
            return;
        }
        bool light = state.HasFlag(StoryIds.Flag.P2EmergencyLightOn);
        if (light) state.ClearFlag(StoryIds.Flag.P2EmergencyLightOn);
        else state.SetFlag(StoryIds.Flag.P2EmergencyLightOn);
        state.locationVariant = light ? StoryIds.Variant.Night : StoryIds.Variant.Emergency;
        feedbackText.text = light ? "应急照明已关闭。最初的路线选择保留。" :
            "应急照明已打开。最初的路线选择保留。";
        UpdateView();
        Persist();
    }

    public void InspectGuide() => ShowInfo("社团自留资料 · 双栋导览练习稿",
        "红笔批注：先去单数栋，再去偶数栋；每栋都从连廊口往外走。\n\n去年四楼两侧共四个展位，号码印在海报右下角。", false);

    public void InspectNear()
    {
        if (!CanAct()) return;
        bool odd = state.locationId == StoryIds.Location.BridgeOdd4F;
        state.SetFlag(odd ? StoryIds.Flag.P2OddNearRead : StoryIds.Flag.P2EvenNearRead);
        Persist();
        ShowInfo("现实海报 · " + (odd ? "单数栋近端" : "偶数栋近端"),
            "右下角的展位号是 " + (odd ? "2" : "4") + "。", false);
    }

    public void InspectFar()
    {
        if (!CanAct()) return;
        if (string.IsNullOrEmpty(state.routeChoice))
        {
            feedbackText.text = "先在入口决定是否打开应急照明。";
            return;
        }
        bool odd = state.locationId == StoryIds.Location.BridgeOdd4F;
        string flag = odd ? StoryIds.Flag.P2OddFarRead : StoryIds.Flag.P2EvenFarRead;
        int number = odd ? 7 : 1;
        if (state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
        {
            state.SetFlag(flag);
            ShowInfo("现实海报 · 揭开贴纸", "新贴纸的胶还没干。揭开后，旧展位号是 " + number + "。", false);
        }
        else
        {
            state.san = Mathf.Max(0, state.san - 20);
            state.SetFlag(flag);
            revealLocation = state.locationId;
            revealedUntil = Time.time + 8f;
            ShowInfo("凝视 · 旧字显影", "贴纸在视野中短暂褪色。旧展位号是 " + number + "。\n\nSAN -20；这个真实数字已记入任务记录。", true);
        }
        idleSeconds = 0;
        UpdateView();
        Persist();
    }

    public void InspectGlass()
    {
        if (!CanAct()) return;
        if (state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
            ShowInfo("连廊玻璃", "应急灯的反光太亮，只能看到浅浅的划痕。", false);
        else if (state.routeChoice == StoryIds.Choice.RouteDark && state.san <= 60)
        {
            state.SetFlag(StoryIds.Flag.P2GlassRead);
            Persist();
            ShowInfo("残影 · 玻璃旧字", "别只看照片，看看他们留下了什么。\n\n这句话只出现在玻璃水雾里，已记入任务记录。", true);
        }
        else ShowInfo("连廊玻璃", "玻璃上似乎有旧字。等眼睛适应黑暗，也许能看得更清楚。", true);
    }

    public void InspectCabinet()
    {
        if (!CanAct()) return;
        if (state.HasFlag(StoryIds.Flag.P2CabinetOpen))
        {
            ShowInfo("社团旧资料柜", state.HasFlag(StoryIds.Flag.P2DrawerTaken)
                ? "抽屉已经打开，借用便签和分工表残页都在背包。"
                : "锁已打开。点击抽屉取得里面的资料。", false);
            return;
        }
        codePanel.SetActive(true);
        codeFeedbackText.text = "";
        UpdateDigits();
    }

    public void CycleDigit(int index)
    {
        if (index < 0 || index >= digits.Length) return;
        digits[index] = (digits[index] + 1) % 10;
        UpdateDigits();
    }
    private void UpdateDigits()
    {
        for (int i = 0; i < digits.Length; i++) digitTexts[i].text = digits[i].ToString();
    }
    public void SubmitCode()
    {
        if (state == null) return;
        if (digits[0] != 2 || digits[1] != 7 || digits[2] != 4 || digits[3] != 1)
        {
            codeFeedbackText.text = "顺序不符。四位输入已保留，可以继续修改。";
            return;
        }
        codePanel.SetActive(false);
        state.SetFlag(StoryIds.Flag.P2CabinetOpen);
        Persist();
        ShowInfo("资料柜", "咔哒一声，锁开了。抽屉里压着去年的纸质记录。", false);
        UpdateView();
    }
    public void TakeDrawer()
    {
        if (state == null || !state.HasFlag(StoryIds.Flag.P2CabinetOpen)) return;
        state.AddItem(StoryIds.Item.MachineNote);
        state.AddItem(StoryIds.Item.TeamSheet);
        state.SetFlag(StoryIds.Flag.P2DrawerTaken);
        state.stepId = "p2Complete";
        state.timeId = "saturday.0050";
        SaveCheckpoint(true);
        ShowInfo("社团自留资料 · 柜内纸张", "机房借用便签：林未，周五 23:30，单数栋 3F 机房靠窗第 3 台。\n\n去年的分工表残页：第五格被撕去，只剩编号 05。\n\n两件关键物品已入背包。", false);
        UpdateView();
    }

    public void GoOdd() => Navigate(StoryIds.Location.BridgeOdd4F);
    public void GoSpan() => Navigate(StoryIds.Location.BridgeSpan4F);
    public void GoEven() => Navigate(StoryIds.Location.BridgeEven4F);
    public void GoSafe() => Navigate(StoryIds.Location.ClubRoom);
    public void GoMachineRoom()
    {
        if (!CanAct()) return;
        ShowInfo("单数栋 3F 机房", state.HasItem(StoryIds.Item.MachineNote)
            ? "目标已确认：靠窗第 3 台。S3 机房阶段尚未制作；可以继续调查四楼或返回 5F 安全屋。"
            : "需要先在偶数栋资料柜里找到机房借用记录。", false);
    }
    private void Navigate(string location)
    {
        if (!CanAct() || string.IsNullOrEmpty(state.routeChoice)) return;
        state.locationId = location;
        state.locationVariant = location == StoryIds.Location.ClubRoom ? StoryIds.Variant.Night :
            state.HasFlag(StoryIds.Flag.P2EmergencyLightOn) ? StoryIds.Variant.Emergency : StoryIds.Variant.Night;
        dangerSeconds = 0; zeroSanSeconds = 0;
        Persist();
        pending = Clone(state);
        pendingDevelopment = development;
        string scene = location == StoryIds.Location.ClubRoom ? SafeScene : location;
        if (!GameFlow.LoadLocation(scene)) { pending = null; Debug.LogError("[S2] 地点未加入 Build Settings：" + scene); }
    }
    private bool CanAct() => state != null && !ModalOpen;

    public void Rest()
    {
        if (!CanAct() || state.locationId != StoryIds.Location.ClubRoom) return;
        state.san = state.sanMax; state.health = 3;
        Persist();
        ShowInfo("5F 活动室 · 安全屋", "室内电脑仍有电。坐下休息后，SAN 与生命已恢复；已读数字和路线标记保留。", false);
        UpdateView();
    }
    public void Retreat()
    {
        if (state == null) return;
        infoPanel.SetActive(false); pausePanel.SetActive(false);
        Navigate(StoryIds.Location.BridgeOdd4F);
    }

    public void OpenRecord() { if (CanAct()) { recordText.text = BuildRecord(); recordPanel.SetActive(true); } }
    public void CloseRecord() => recordPanel.SetActive(false);
    private string BuildRecord()
    {
        string order = "单数栋近端 " + (state.HasFlag(StoryIds.Flag.P2OddNearRead) || hintStage >= 2 ? "2" : "?") +
            " → 远端 " + (state.HasFlag(StoryIds.Flag.P2OddFarRead) ? "7" : "?") +
            " → 偶数栋近端 " + (state.HasFlag(StoryIds.Flag.P2EvenNearRead) || hintStage >= 2 ? "4" : "?") +
            " → 远端 " + (state.HasFlag(StoryIds.Flag.P2EvenFarRead) ? "1" : "?");
        return "【社团自留资料】\n旧照片：背面 23:30。工牌：林未，编号 05；玻璃两边各存一半。\n双栋导览：先单数栋，后偶数栋；每栋从连廊口向外。\n" +
            (state.HasItem(StoryIds.Item.MachineNote) ? "机房借用便签：单数栋 3F，靠窗第 3 台。\n" : "") +
            (state.HasItem(StoryIds.Item.TeamSheet) ? "分工表残页：第五格被撕去，余编号 05。\n" : "") +
            "\n【现实调查】\n" + order + "\n路线：" + state.routeChoice +
            "；应急灯：" + (state.HasFlag(StoryIds.Flag.P2EmergencyLightOn) ? "亮" : "灭") +
            (state.HasFlag(StoryIds.Flag.P2GlassRead) ? "\n\n【残影】\n玻璃旧字：别只看照片，看看他们留下了什么。" : "");
    }
    public void OpenInventory()
    {
        if (!CanAct()) return;
        inventoryText.text = "任务物品 · 不可丢弃\n\n" +
            (state.HasItem(StoryIds.Item.Photo) ? "✓ 去年的活动室照片（背面 23:30）\n" : "") +
            (state.HasItem(StoryIds.Item.Badge) ? "✓ 林未的旧工牌（编号 05）\n" : "") +
            (state.HasItem(StoryIds.Item.MachineNote) ? "✓ 机房借用便签（3F 靠窗第 3 台）\n" : "") +
            (state.HasItem(StoryIds.Item.TeamSheet) ? "✓ 去年的分工表残页（第五格 05）" : "");
        inventoryPanel.SetActive(true);
    }
    public void CloseInventory() => inventoryPanel.SetActive(false);
    public void TogglePause() { if (state != null) pausePanel.SetActive(!pausePanel.activeSelf); }
    public void SaveManual()
    {
        if (state == null) return;
        pausePanel.SetActive(false);
        SaveSlotPanel.OpenSave(CaptureSaveSlot);
    }
    private void SaveCheckpoint(bool automatic)
    {
        if (development) return;
        if (!Persist()) return;
        feedbackText.text = !automatic ? "S2 检查点已保存。" :
            state.HasFlag(StoryIds.Flag.P2DrawerTaken) ? "P2 完成，已自动保存检查点。" :
            "路线已记录，检查点已保存。";
    }
    private bool Persist()
    {
        if (development || state == null) return false;
        if (!GameStateStore.SaveCheckpoint(state, out string error))
        {
            Debug.LogError("[S2] 存档失败：" + error);
            return false;
        }
        PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 2);
        PlayerPrefs.Save();
        return true;
    }
    public void LoadManual()
    {
        pausePanel.SetActive(false);
        SaveSlotPanel.OpenLoad(CaptureSaveSlot);
    }
    public void ReturnTitle() { pausePanel.SetActive(false); GameFlow.LoadLocation(GameFlow.TitleScene); }
    public void CloseCode() => codePanel.SetActive(false);
    public void CloseInfo() => infoPanel.SetActive(false);
    private void ShowInfo(string title, string body, bool residue)
    {
        infoTitle.text = (residue ? "【残影】" : "") + title;
        infoBody.text = body;
        infoPanel.SetActive(true);
    }

    private void UpdateView()
    {
        if (state == null) return;
        bool light = state.HasFlag(StoryIds.Flag.P2EmergencyLightOn);
        string location = state.locationId;
        locationText.text = location == StoryIds.Location.BridgeOdd4F ? "单数栋 · 4F 连廊入口" :
            location == StoryIds.Location.BridgeSpan4F ? "4F · 玻璃连廊中段" :
            location == StoryIds.Location.BridgeEven4F ? "偶数栋 · 4F 连廊尽头" : "单数栋 · 5F 活动室";
        timeText.text = state.HasFlag(StoryIds.Flag.P2DrawerTaken) ? "周六 00:50" : "周六 00:10";
        objectiveText.text = state.HasFlag(StoryIds.Flag.P2DrawerTaken) ? "借用便签指向单数栋 3F 机房" :
            string.IsNullOrEmpty(state.routeChoice) ? "在应急照明开关旁作出选择" : "查找四张海报，打开偶数栋资料柜";
        statusText.text = "SAN " + state.san + "/" + state.sanMax + "   生命 " + state.health + "/3";
        if (glow != null) glow.SetActive(light);
        if (farRevealText != null)
        {
            bool odd = location == StoryIds.Location.BridgeOdd4F;
            bool read = state.HasFlag(odd ? StoryIds.Flag.P2OddFarRead : StoryIds.Flag.P2EvenFarRead);
            bool visible = read && (light || state.san <= 60 ||
                (revealLocation == location && Time.time < revealedUntil));
            farRevealText.text = visible ? "旧展位号  " + (odd ? "7" : "1") : "贴纸遮住了旧展位号";
        }
        if (drawerButton != null) drawerButton.gameObject.SetActive(state.HasFlag(StoryIds.Flag.P2CabinetOpen) && !state.HasFlag(StoryIds.Flag.P2DrawerTaken));
        if (switchButton != null)
        {
            switchButton.gameObject.SetActive(location == StoryIds.Location.BridgeOdd4F);
            Text caption = switchButton.GetComponentInChildren<Text>();
            if (caption != null) caption.text = light ? "关闭应急灯" : "打开应急灯";
        }
        if (darkChoiceButton != null)
            darkChoiceButton.gameObject.SetActive(string.IsNullOrEmpty(state.routeChoice));
        if (glassButton != null) glassButton.gameObject.SetActive(location == StoryIds.Location.BridgeSpan4F);
    }
}
