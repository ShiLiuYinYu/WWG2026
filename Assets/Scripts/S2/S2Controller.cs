using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

/// <summary>Four fixed views for the S2 bridge investigation. The same GameState travels between views.</summary>
public sealed class S2Controller : MonoBehaviour, IDialogueContext
{
    public Text locationText, timeText, objectiveText, feedbackText, recordText, inventoryText;
    public VitalsBars vitals;
    public Text farRevealText, codeFeedbackText;
    public Text[] digitTexts;
    public GameObject infoPanel, codePanel, recordPanel, inventoryPanel, pausePanel, glow;
    public ClubPhotoView photoView;
    public Text infoTitle, infoBody;
    public Button drawerButton, switchButton, glassButton;
    public Image bridgeSwitchImage;
    public Sprite bridgeSwitchOffSprite, bridgeSwitchOnSprite;
    public Image locationArtwork;
    public Sprite bridgeNightOffSprite, bridgeNightOnSprite;

    private static GameState pending;
    private static bool pendingDevelopment;
    private GameState state;
    private bool development;
    private bool subscribed;
    private string codeEntry = string.Empty;
    private const string CabinetCode = "2741";
    private const int GlassFocusCost = 40;
    private const string GlassMessage = "别只看照片，看看他们留下了什么。";
    private float dangerSeconds, zeroSanSeconds, idleSeconds;
    private int hintStage;

    private bool UsingGlass => state.routeChoice == StoryIds.Choice.RouteDark;
    private bool FarNumbersFound => state.HasFlag(StoryIds.Flag.P2OddFarRead) &&
        state.HasFlag(StoryIds.Flag.P2EvenFarRead);
    private bool AllNumbersFound => FarNumbersFound && state.HasFlag(StoryIds.Flag.P2OddNearRead) &&
        state.HasFlag(StoryIds.Flag.P2EvenNearRead);

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

    public static void ReturnFromS3(GameState handoff, bool editorDevelopment, bool toSafeRoom)
    {
        if (handoff == null) throw new ArgumentNullException(nameof(handoff));
        if (!handoff.Validate(out string error))
            throw new ArgumentException("S3 返回状态无效：" + error);
        pending = Clone(handoff);
        pending.locationId = toSafeRoom ? StoryIds.Location.ClubRoom : StoryIds.Location.BridgeOdd4F;
        pending.locationVariant = StoryIds.Variant.Night;
        pending.chapterId = StoryIds.Chapter.BridgeRoute;
        pending.stepId = pending.HasFlag(StoryIds.Flag.P3Copied) ? "copyComplete" : "p2Complete";
        pendingDevelopment = editorDevelopment;
        if (!editorDevelopment && GameStateStore.SaveCheckpoint(pending, out _))
        {
            PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 2);
            PlayerPrefs.Save();
        }
        if (!GameFlow.LoadLocation(GameFlow.S2Location(pending.locationId)))
            throw new InvalidOperationException("S3 返回地点未加入 Build Settings");
    }

    private static GameState Clone(GameState source) => JsonUtility.FromJson<GameState>(JsonUtility.ToJson(source));
    public GameState CurrentState => state;
    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (state == null || development || DialogueRunning) return null;
        return new SaveSlotSnapshot { chapter = "S2", payload = JsonUtility.ToJson(state), gameTime = timeText.text,
            locationLabel = "S2 · " + timeText.text + " · " + locationText.text };
    }

    private static bool DialogueRunning =>
        DialogueService.Instance != null && DialogueService.Instance.IsRunning;

    private void Start()
    {
        Application.runInBackground = true;
        Font font = WWG.WwgFonts.Body;
        if (font != null)
            foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = font;

        DialogueService service = DialogueService.Ensure();
        if (service != null)
        {
            service.DialogueCompleted += HandleDialogueCompleted;
            subscribed = true;
        }

        infoPanel.SetActive(false); CloseCode();
        recordPanel.SetActive(false); inventoryPanel.SetActive(false); pausePanel.SetActive(false);
        inventoryPanel.GetComponent<S2InventoryGrid>().InstallSceneInput(this, () => !CanAct());
        state = pending ?? SaveSlotStore.TakeLoadedState(StoryIds.Chapter.BridgeRoute);
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
        // Opening any S2 scene and pressing Play is an isolated editor preview.
#if UNITY_EDITOR
        if (state == null)
        {
            state = ChapterPreviewStates.S2(SceneManager.GetActiveScene().name);
            development = true;
        }
#endif
        if (state == null && GameStateStore.LoadCheckpoint(out GameState saved, out _) &&
            saved.chapterId == StoryIds.Chapter.BridgeRoute)
            state = saved;
        if (state == null)
        {
            Debug.LogError("[S2] 缺少 S1 交接或 S2 检查点。请从 S1 进入，或用 WWG/Dev/Play S2。");
            ShowInfo("无法进入本章", "请从上一章到达连廊，或使用编辑器的章节预览菜单。", false);
            return;
        }
        if (state.locationId == StoryIds.Location.ClubRoom && state.HasFlag(StoryIds.Flag.P3Copied))
        {
            S4Controller.Begin(state, development);
            return;
        }
        state.EnsureCollections();
        state.chapterId = StoryIds.Chapter.BridgeRoute;
        state.stepId = state.HasFlag(StoryIds.Flag.P3Copied) ? "copyComplete" :
            state.HasFlag(StoryIds.Flag.P2DrawerTaken) ? "p2Complete" : "bridgeReached";
        if (!string.IsNullOrEmpty(state.routeChoice)) state.locationVariant =
            state.locationId != StoryIds.Location.ClubRoom && state.HasFlag(StoryIds.Flag.P2EmergencyLightOn)
                ? StoryIds.Variant.Emergency : StoryIds.Variant.Night;
        UpdateView();
        if (string.IsNullOrEmpty(state.routeChoice) && state.locationId == StoryIds.Location.BridgeOdd4F)
        {
            if (!state.HasSeenDialogue("S2_Odd4F_Intro")) PlayNode("S2_Odd4F_Intro", true);
        }
    }

    private void OnDestroy()
    {
        if (subscribed && DialogueService.Instance != null)
            DialogueService.Instance.DialogueCompleted -= HandleDialogueCompleted;
        subscribed = false;
    }

    private bool IsCodeOpen => codePanel != null && codePanel.activeSelf;
    internal bool IsInventoryBlocked => DialogueRunning || SaveSlotPanel.IsOpen || infoPanel.activeSelf ||
        IsCodeOpen || recordPanel.activeSelf || pausePanel.activeSelf ||
        (photoView != null && photoView.IsOpen);
    private bool ModalOpen => IsInventoryBlocked || inventoryPanel.activeSelf;

    private void Update()
    {
        if (state == null || ModalOpen) return;
        if (!state.HasFlag(StoryIds.Flag.P2DrawerTaken) && !string.IsNullOrEmpty(state.routeChoice))
        {
            idleSeconds += Time.deltaTime;
            if (hintStage == 0 && idleSeconds >= 45f &&
                !FarNumbersFound)
            {
                hintStage = 1;
                PlayNode("S2_Odd4F_Hint_Far", true);
            }
            else if (hintStage < 2 && idleSeconds >= 90f)
            {
                hintStage = 2;
                PlayNode("S2_Odd4F_Hint_Record", true);
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
    }

    public void SwitchLight()
    {
        if (state == null || !CanAct()) return;
        bool light = state.HasFlag(StoryIds.Flag.P2EmergencyLightOn);
        if (light) state.ClearFlag(StoryIds.Flag.P2EmergencyLightOn);
        else state.SetFlag(StoryIds.Flag.P2EmergencyLightOn);
        state.locationVariant = light ? StoryIds.Variant.Night : StoryIds.Variant.Emergency;
        feedbackText.text = light ? "应急照明已关闭。" : "应急照明已打开。";
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
            ShowInfo("远端海报", "贴纸盖得严严实实。先去看看连廊中段右侧玻璃上的痕迹。", false);
            return;
        }
        bool odd = state.locationId == StoryIds.Location.BridgeOdd4F;
        string flag = odd ? StoryIds.Flag.P2OddFarRead : StoryIds.Flag.P2EvenFarRead;
        int number = odd ? 7 : 1;
        if (state.HasFlag(flag))
        {
            ShowInfo("调查记录 · 远端海报", "这张海报的旧展位号是 " + number + "，已记入任务记录。", false);
            return;
        }
        if (UsingGlass)
        {
            ShowInfo("远端海报", "这张贴纸盖得很严。沿着刚才发现的痕迹，回连廊中段凝视玻璃。", false);
            return;
        }
        if (!state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
        {
            ShowInfo("远端海报", "暗处摸不准贴纸的边。回单数栋入口开灯，再揭开贴纸。", false);
            return;
        }
        state.SetFlag(flag);
        ShowInfo("现实海报 · 揭开贴纸", "顺着翘边揭开贴纸。旧展位号是 " + number + "。", false);
        idleSeconds = 0;
        UpdateView();
        Persist();
    }

    public void InspectGlass()
    {
        if (!CanAct() || state.locationId != StoryIds.Location.BridgeSpan4F) return;
        if (string.IsNullOrEmpty(state.routeChoice))
        {
            bool lightOn = state.HasFlag(StoryIds.Flag.P2EmergencyLightOn);
            ChooseRoute(lightOn ? StoryIds.Choice.RouteLight : StoryIds.Choice.RouteDark);
            if (PlayNode(lightOn ? "S2_Odd4F_RouteLight" : "S2_Odd4F_RouteDark")) return;
            if (UsingGlass)
            {
                ShowInfo("连廊玻璃", "玻璃里浮出两张远端海报的倒影。再点击一次凝视，SAN 最多扣 " + GlassFocusCost + "。", true);
                return;
            }
        }
        if (!UsingGlass)
        {
            ShowInfo("连廊玻璃", "刚才的反光里只有划痕。沿两端墙面找海报，远端的贴纸得开灯才好揭。", false);
            return;
        }
        if (state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
        {
            ShowInfo("连廊玻璃", "反光盖住了残影。回单数栋入口关灯，再来凝视玻璃。", false);
            return;
        }

        int spent = FarNumbersFound && state.HasFlag(StoryIds.Flag.P2GlassRead)
            ? 0 : Mathf.Min(state.san, GlassFocusCost);
        state.san -= spent;
        state.SetFlag(StoryIds.Flag.P2GlassRead);
        state.SetFlag(StoryIds.Flag.P2OddFarRead);
        state.SetFlag(StoryIds.Flag.P2EvenFarRead);
        idleSeconds = 0;
        Persist();
        UpdateView();
        ShowInfo("残影 · 玻璃旧字",
            "玻璃里映出两张被贴纸遮住的海报。单数栋远端是 7，偶数栋远端是 1。\n\n「" +
            GlassMessage + "」\n\n两处远端编号已记录。两张近端海报还可以在墙上核对。" +
            (spent > 0 ? "\nSAN -" + spent + "。" : ""), true);
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
        DialogueService service = DialogueService.Ensure();
        if (service == null || !service.Play("S2_Even4F_Cabinet", this, true)) OpenCabinetCode();
    }

    private void OpenCabinetCode()
    {
        if (state == null || codePanel == null) return;
        if (!AllNumbersFound)
        {
            ShowInfo("资料柜 · 核对展位号", "先把四个展位号记齐。两张近端海报都在各自的连廊口。" +
                (UsingGlass ? "\n远端编号从中段玻璃的残影读取。" : "\n远端编号要开灯揭开两张海报上的贴纸。"), false);
            return;
        }
        codePanel.SetActive(true);
        RefreshCodeEntry();
    }

    public void EnterCodeDigit(int digit)
    {
        if (!IsCodeOpen || digit < 1 || digit > 9 || codeEntry.Length >= CabinetCode.Length) return;
        codeEntry += (char)('0' + digit);
        RefreshCodeEntry();
    }

    public void DeleteCodeDigit()
    {
        if (!IsCodeOpen || codeEntry.Length == 0) return;
        codeEntry = codeEntry.Substring(0, codeEntry.Length - 1);
        RefreshCodeEntry();
    }

    private void RefreshCodeEntry()
    {
        codeFeedbackText.text = string.Empty;
        for (int i = 0; i < digitTexts.Length; i++)
            digitTexts[i].text = i < codeEntry.Length ? codeEntry[i].ToString() : string.Empty;
    }

    public void SubmitCode()
    {
        if (state == null || !IsCodeOpen || !AllNumbersFound) return;
        if (codeEntry.Length < CabinetCode.Length)
        {
            codeFeedbackText.text = "请输入完整的四位密码。";
            return;
        }
        if (codeEntry != CabinetCode)
        {
            codeFeedbackText.text = "密码错误，可删除后修改。";
            return;
        }

        CloseCode();
        state.SetFlag(StoryIds.Flag.P2CabinetOpen);
        Persist();
        ShowInfo("资料柜", "咔哒一声，锁开了。抽屉里压着去年的纸质记录。", false);
        UpdateView();
    }
    public void TakeDrawer()
    {
        if (state == null || !CanAct() || !state.HasFlag(StoryIds.Flag.P2CabinetOpen)) return;
        state.AddItem(StoryIds.Item.MachineNote);
        state.AddItem(StoryIds.Item.TeamSheet);
        state.SetFlag(StoryIds.Flag.P2DrawerTaken);
        state.stepId = "p2Complete";
        state.timeId = "saturday.0050";
        SaveCheckpoint(true);
        PlayNode("S2_Even4F_Drawer");
        UpdateView();
    }

    public void GoOdd() => Navigate(StoryIds.Location.BridgeOdd4F);
    public void GoSpan() => Navigate(StoryIds.Location.BridgeSpan4F);
    public void GoEven() => Navigate(StoryIds.Location.BridgeEven4F);
    public void GoSafe() => Navigate(StoryIds.Location.ClubRoom);
    public void GoMachineRoom()
    {
        if (!CanAct()) return;
        if (!state.HasItem(StoryIds.Item.MachineNote))
        {
            ShowInfo("单数栋 3F 机房", "需要先在偶数栋资料柜里找到机房借用记录。", false);
            return;
        }
        S3Controller.Begin(state, development);
    }
    private void Navigate(string location)
    {
        if (!CanAct()) return;
        if (string.IsNullOrEmpty(state.routeChoice) && location == StoryIds.Location.BridgeEven4F)
        {
            ShowInfo("连廊玻璃", "右侧玻璃上好像有一道痕迹。走近看看。", false);
            return;
        }
        state.locationId = location;
        state.locationVariant = location == StoryIds.Location.ClubRoom ? StoryIds.Variant.Night :
            state.HasFlag(StoryIds.Flag.P2EmergencyLightOn) ? StoryIds.Variant.Emergency : StoryIds.Variant.Night;
        dangerSeconds = 0; zeroSanSeconds = 0;
        Persist();
        pending = Clone(state);
        pendingDevelopment = development;
        string scene = GameFlow.S2Location(location);
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

    public void OpenRecord()
    {
        if (!CanAct()) return;
        recordText.fontSize = state.HasFlag(StoryIds.Flag.P3MemoHeard) ? 23 : 30;
        recordText.text = BuildRecord();
        recordPanel.SetActive(true);
    }
    public void CloseRecord() => recordPanel.SetActive(false);
    private string BuildRecord()
    {
        string order = "单数栋近端 " + (state.HasFlag(StoryIds.Flag.P2OddNearRead) ? "2" : "?") +
            " → 远端 " + (state.HasFlag(StoryIds.Flag.P2OddFarRead) ? "7" : "?") +
            " → 偶数栋近端 " + (state.HasFlag(StoryIds.Flag.P2EvenNearRead) ? "4" : "?") +
            " → 远端 " + (state.HasFlag(StoryIds.Flag.P2EvenFarRead) ? "1" : "?");
        return "【社团自留资料】\n旧照片：背面 23:30。工牌：林未，编号 05；玻璃两边各存一半。\n双栋导览：先单数栋，后偶数栋；每栋从连廊口向外。\n" +
            (state.HasItem(StoryIds.Item.MachineNote) ? "机房借用便签：单数栋 3F，靠窗第 3 台。\n" : "") +
            (state.HasItem(StoryIds.Item.TeamSheet) ? "分工表残页：第五格被撕去，余编号 05。\n" : "") +
            (state.HasItem(StoryIds.Item.OldProjectUsb) ? "旧工程 U 盘：已拷贝 AfterLightsOut_Backup。\n" : "") +
            "\n【展位号】\n" + order + "\n调查方式：" +
            (UsingGlass ? "玻璃倒影" : state.routeChoice == StoryIds.Choice.RouteLight ? "纸面海报" : "待决定") +
            "\n应急灯：" + (state.HasFlag(StoryIds.Flag.P2EmergencyLightOn) ? "亮" : "灭") +
            (state.HasFlag(StoryIds.Flag.P2GlassRead) ? "\n\n【残影】\n玻璃旧字：" + GlassMessage : "") +
            (state.HasFlag(StoryIds.Flag.P3MemoHeard) ? "\n\n【林未的录音备忘全文】\n" + DialogueDocuments.GetBody("s3.memo") : "");
    }
    public void OpenInventory()
    {
        if (!CanAct()) return;
        inventoryText.text = "任务物品 · 不可丢弃\n\n" +
            (state.HasItem(StoryIds.Item.Photo) ? "✓ 去年的活动室照片（背面 23:30）\n" : "") +
            (state.HasItem(StoryIds.Item.Badge) ? "✓ 林未的旧工牌（编号 05）\n" : "") +
            (state.HasItem(StoryIds.Item.MachineNote) ? "✓ 机房借用便签（3F 靠窗第 3 台）\n" : "") +
            (state.HasItem(StoryIds.Item.TeamSheet) ? "✓ 去年的分工表残页（第五格 05）\n" : "") +
            (state.HasItem(StoryIds.Item.OldProjectUsb) ? "✓ 装有旧工程的 U 盘" : "");
        inventoryPanel.SetActive(true);
    }
    public void CloseInventory() => inventoryPanel.SetActive(false);
    public void ShowInventoryUseHint(string item)
    {
        feedbackText.text = S2InventoryGrid.ItemName(item) + "是调查线索。请在背包中点“查看”核对内容。";
    }

    public void InspectInventoryItem(string item)
    {
        if (state == null || !state.HasItem(item)) return;
        if (item == StoryIds.Item.Photo && photoView != null)
        {
            photoView.Open(state.HasFlag(StoryIds.Flag.P1Complete) ? ClubPhotoView.RevealedShadowOpacity : 0f);
            return;
        }
        bool isBadge = item == StoryIds.Item.Badge;
        string body = item == StoryIds.Item.Photo ? "【社团自留资料】\n去年活动室的照片。背面标注 23:30。" :
            isBadge ? string.Empty :
            item == StoryIds.Item.MachineNote ? "【社团自留资料】\n机房借用便签：林未，周五 23:30，单数栋 3F 机房靠窗第 3 台。" :
            item == StoryIds.Item.TeamSheet ? "【社团自留资料】\n去年的分工表残页。第五格被撕去，残存编号 05。" : string.Empty;
        if (item == StoryIds.Item.OldProjectUsb) body = "【旧工程拷贝】\nAfterLightsOut_Backup 已存入 U 盘；其中有打不开的 周六_16:00.scene。";
        ShowInfo(isBadge ? string.Empty : S2InventoryGrid.ItemName(item), body, false, showBadge: isBadge);
    }
    public void TogglePause()
    {
        if (DialogueRunning || state == null) return;
        if (photoView != null) photoView.Close();
        pausePanel.SetActive(!pausePanel.activeSelf);
    }
    public void SaveManual()
    {
        if (state == null || DialogueRunning) return;
        pausePanel.SetActive(false);
        SaveSlotPanel.OpenSave(CaptureSaveSlot);
    }
    private void SaveCheckpoint(bool automatic)
    {
        if (development) return;
        if (!Persist()) return;
        feedbackText.text = automatic ? "P2 完成，已自动保存检查点。" : "检查点已保存。";
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
        if (DialogueRunning) return;
        pausePanel.SetActive(false);
        SaveSlotPanel.OpenLoad(CaptureSaveSlot);
    }
    public void ReturnTitle() { pausePanel.SetActive(false); GameFlow.LoadLocation(GameFlow.TitleScene); }
    public void CloseCode()
    {
        if (IsCodeOpen) codePanel.SetActive(false);
    }
    public void CloseInfo()
    {
        infoPanel.SetActive(false);
        infoPanel.transform.Find("Badge artwork")?.gameObject.SetActive(false);
        UpdateView();
    }
    private void ShowInfo(string title, string body, bool residue, bool showBadge = false)
    {
        if (photoView != null) photoView.Close();
        infoTitle.text = (residue ? "【残影】" : "") + title;
        infoBody.text = body;
        LateUi.FitBody(infoBody);
        BadgeDetailLayout.Apply(infoPanel, infoBody, showBadge);
        infoPanel.SetActive(true);
    }

    private bool PlayNode(string nodeId, bool once = false)
    {
        DialogueService service = DialogueService.Ensure();
        return service != null && service.Play(nodeId, this, once);
    }

    private void HandleDialogueCompleted(string nodeId)
    {
        if (nodeId == "S2_Even4F_Cabinet") OpenCabinetCode();
        Persist();
        UpdateView();
    }

    public IEnumerator ShowDocument(string documentId)
    {
        if (!DialogueDocuments.TryGet(documentId, out DialogueDocuments.Entry document))
        {
            Debug.LogWarning("[Dialogue] 缺少文档 ID：" + documentId);
            yield break;
        }
        if (DialogueService.Instance != null) DialogueService.Instance.HideOverlay();
        ShowInfo(document.title, document.body, document.residue);
        while (infoPanel.activeSelf) yield return null;
    }

    public IEnumerator ShowPhoto() => null;

    public bool HasFlag(string id) => state != null && state.HasFlag(id);
    public bool HasItem(string id) => state != null && state.HasItem(id);
    public bool HasSeenDialogue(string nodeId) => state != null && state.HasSeenDialogue(nodeId);
    public void MarkDialogueSeen(string nodeId)
    {
        if (state != null && !string.IsNullOrWhiteSpace(nodeId)) state.MarkDialogueSeen(nodeId);
    }

    public int San => state != null ? state.san : 0;
    public int Health => state != null ? state.health : 0;
    public string Route => state != null ? state.routeChoice : string.Empty;
    public string View => state != null ? state.viewChoice : string.Empty;

    public void SetFlag(string id)
    {
        if (state != null) state.SetFlag(id);
    }

    public void ClearFlag(string id)
    {
        if (state != null) state.ClearFlag(id);
    }

    public void AddItem(string id)
    {
        if (state != null) state.AddItem(id);
    }

    public void ApplyShock(string id, int sanDelta, int healthDelta = 0)
    {
        if (state == null) return;
        string guard = "flag." + id + ".shock";
        if (state.HasFlag(guard)) return;
        state.SetFlag(guard);
        state.san = Mathf.Max(0, state.san + sanDelta);
        state.health = Mathf.Clamp(state.health + healthDelta, 0, 3);
        Persist();
        UpdateView();
    }

    public void ChooseRoute(string choice)
    {
        if (!CanAct() || state.locationId != StoryIds.Location.BridgeSpan4F ||
            !string.IsNullOrEmpty(state.routeChoice) || !state.ChooseRoute(choice)) return;
        UpdateView();
        Persist();
    }

    public void ChooseView(string choice)
    {
        if (state == null) return;
        state.ChooseView(choice);
        Persist();
        UpdateView();
    }

    public void RequestCheckpoint()
    {
        if (DialogueRunning)
        {
            Debug.LogWarning("[Dialogue] 对话进行中不允许写入 S2 检查点。");
            return;
        }
        SaveCheckpoint(false);
    }

    public void RequestGoTo(string locationId, string variant)
    {
        Debug.LogWarning("[Dialogue] S2 不使用 go_to：" + locationId + "/" + variant);
    }

    public void RefreshHud()
    {
        UpdateView();
    }

    private void UpdateView()
    {
        if (state == null) return;
        bool light = state.HasFlag(StoryIds.Flag.P2EmergencyLightOn);
        string location = state.locationId;
        locationText.text = location == StoryIds.Location.BridgeOdd4F ? "单数栋 · 4F 连廊入口" :
            location == StoryIds.Location.BridgeSpan4F ? "4F · 玻璃连廊中段" :
            location == StoryIds.Location.BridgeEven4F ? "偶数栋 · 4F 连廊尽头" : "单数栋 · 5F 活动室";
        timeText.text = state.HasFlag(StoryIds.Flag.P3Copied) ? "周六 01:45" :
            state.HasFlag(StoryIds.Flag.P2DrawerTaken) ? "周六 00:50" : "周六 00:10";
        objectiveText.text = state.HasFlag(StoryIds.Flag.P2DrawerTaken) ? "借用便签指向单数栋 3F 机房" :
            string.IsNullOrEmpty(state.routeChoice) ? "查看连廊中段右侧的玻璃" :
            AllNumbersFound ? "打开偶数栋资料柜" :
            UsingGlass ? "核对两张近端海报和连廊玻璃" : "开灯检查两端的四张海报";
        if (state.HasFlag(StoryIds.Flag.P3Copied)) objectiveText.text = "旧工程已带回 5F；等待整理证据";
        vitals.SetSan(state.san, state.sanMax);
        vitals.SetHealth(state.health, 3);
        if (glow != null) glow.SetActive(light && location != StoryIds.Location.ClubRoom);
        // 连廊远景随应急灯切换：灯亮换成开灯版，灯灭用夜晚熄灯版
        if (locationArtwork != null && bridgeNightOffSprite != null && bridgeNightOnSprite != null)
            locationArtwork.sprite = light ? bridgeNightOnSprite : bridgeNightOffSprite;
        if (bridgeSwitchImage != null)
            bridgeSwitchImage.sprite = light ? bridgeSwitchOnSprite : bridgeSwitchOffSprite;
        if (farRevealText != null)
        {
            bool odd = location == StoryIds.Location.BridgeOdd4F;
            bool read = state.HasFlag(odd ? StoryIds.Flag.P2OddFarRead : StoryIds.Flag.P2EvenFarRead);
            farRevealText.text = read && !UsingGlass && light ? "旧展位号  " + (odd ? "7" : "1") : "贴纸遮住了旧展位号";
        }
        if (drawerButton != null) drawerButton.gameObject.SetActive(state.HasFlag(StoryIds.Flag.P2CabinetOpen) && !state.HasFlag(StoryIds.Flag.P2DrawerTaken));
        if (switchButton != null)
        {
            switchButton.gameObject.SetActive(location == StoryIds.Location.BridgeOdd4F);
            Text caption = switchButton.GetComponentInChildren<Text>();
            if (caption != null) caption.text = light ? "关闭应急灯" : "打开应急灯";
        }
        if (glassButton != null) glassButton.gameObject.SetActive(location == StoryIds.Location.BridgeSpan4F);
    }
}
