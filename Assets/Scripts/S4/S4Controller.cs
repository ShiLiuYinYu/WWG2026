using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>Final 5F safe-room sequence, from the return with the USB to the 24H ending.</summary>
public sealed class S4Controller : MonoBehaviour, IDialogueContext
{
    public Image roomImage;
    public Sprite dayRoomSprite;
    public GameObject dayWindow, dayRoomUpper, blackout, oldProjectPanel, endingPanel, choicePanel, confirmPanel;
    public Text locationText, timeText, objectiveText, recordText, inventoryText, infoTitle, infoBody;
    public Text choiceTitle, confirmText, projectStatus, endingText;
    public GameObject infoPanel, recordPanel, inventoryPanel, pausePanel;
    public Button badgeButton, restButton, yuanButton, xuButton, tangButton, computerButton;
    public VitalsBars vitals;

    private static GameState pending;
    private static bool pendingDevelopment;
    private GameState state;
    private bool development, subscribed;
    private string pendingChoice;

    private static bool DialogueRunning => DialogueService.Instance != null && DialogueService.Instance.IsRunning;
    private bool ModalOpen => DialogueRunning || infoPanel.activeSelf || recordPanel.activeSelf ||
        inventoryPanel.activeSelf || pausePanel.activeSelf || choicePanel.activeSelf ||
        confirmPanel.activeSelf || endingPanel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPending() { pending = null; pendingDevelopment = false; }

    public static void Begin(GameState handoff, bool editorDevelopment = false)
    {
        if (handoff == null || !handoff.Validate(out string error) ||
            !handoff.HasFlag(StoryIds.Flag.P3Copied) || !handoff.HasItem(StoryIds.Item.OldProjectUsb) ||
            string.IsNullOrEmpty(handoff.routeChoice))
            throw new ArgumentException("S4 需要完成 P3、带回旧工程并保留 4F 路线选择。");
        pending = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(handoff));
        pendingDevelopment = editorDevelopment;
        pending.chapterId = StoryIds.Chapter.Finale;
        pending.stepId = pending.HasFlag(StoryIds.Flag.S4Rested) ? pending.stepId : "arrival";
        pending.locationId = StoryIds.Location.ClubRoom;
        pending.locationVariant = pending.HasFlag(StoryIds.Flag.S4Rested) ? StoryIds.Variant.Day : StoryIds.Variant.Night;
        pending.timeId = pending.HasFlag(StoryIds.Flag.S4Rested) ? pending.timeId : "saturday.0200";
        if (!GameFlow.LoadLocation(GameFlow.S4Scene))
        {
            pending = null;
            throw new InvalidOperationException("S4 活动室未加入 Build Settings。");
        }
    }

    public GameState CurrentState => state;

    private void Start()
    {
        Application.runInBackground = true;
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
        if (font != null)
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;

        infoPanel.SetActive(false); recordPanel.SetActive(false); inventoryPanel.SetActive(false);
        pausePanel.SetActive(false); choicePanel.SetActive(false); confirmPanel.SetActive(false);
        oldProjectPanel.SetActive(false); endingPanel.SetActive(false); blackout.SetActive(false);
        Bind("Inventory", OpenInventory);
        Bind("Top bar/Pause", TogglePause);
        Bind("Top bar/Record", OpenRecord);
        Bind("Inspect/Close", CloseInfo);
        Bind("Task record/Close", CloseRecord);
        BindButton(inventoryPanel.transform.Find("Close")?.GetComponent<Button>(), CloseInventory);
        Bind("Pause/Resume", TogglePause);
        Bind("Pause/Save", SaveManual);
        Bind("Pause/Load", LoadManual);
        Bind("Pause/Title", ReturnTitle);
        BindButton(badgeButton, ArchiveBadge);
        BindButton(restButton, Rest);
        BindButton(yuanButton, TalkYuan);
        BindButton(xuButton, TalkXu);
        BindButton(tangButton, TalkTang);
        BindButton(computerButton, OpenDecision);
        Bind("S4 Choice/Together", () => SelectChoice(StoryIds.Choice.ViewTogether));
        Bind("S4 Choice/Alone", () => SelectChoice(StoryIds.Choice.ViewAlone));
        Bind("S4 Choice/Close", () => choicePanel.SetActive(false));
        Bind("S4 Confirm/Confirm", ConfirmChoice);
        Bind("S4 Confirm/Back", () => { confirmPanel.SetActive(false); choicePanel.SetActive(true); });
        Bind("S4 Ending/Title", ReturnTitle);

        state = pending;
        development = pendingDevelopment;
        pending = null;
        pendingDevelopment = false;
#if UNITY_EDITOR
        if (state == null)
        {
            string bootstrap = UnityEditor.SessionState.GetString("WWG_S4Bootstrap", "");
            if (!string.IsNullOrEmpty(bootstrap))
            {
                UnityEditor.SessionState.EraseString("WWG_S4Bootstrap");
                state = JsonUtility.FromJson<GameState>(bootstrap);
                development = true;
            }
        }
#endif
#if UNITY_EDITOR
        if (state == null)
        {
            state = ChapterPreviewStates.S4();
            development = true;
        }
#endif
        if (state == null && GameStateStore.LoadCheckpoint(out GameState saved, out _) &&
            saved.chapterId == StoryIds.Chapter.Finale) state = saved;
        if (state == null || state.chapterId != StoryIds.Chapter.Finale ||
            !state.HasFlag(StoryIds.Flag.P3Copied) || !state.HasItem(StoryIds.Item.OldProjectUsb))
        {
            Debug.LogError("[S4] 缺少有效的 P3 交接或 S4 检查点。");
            ShowInfo("无法进入最终阶段", "请先从三楼机房带回旧工程 U 盘。", false);
            return;
        }
        state.EnsureCollections();
        DialogueService service = DialogueService.Ensure();
        if (service != null) { service.DialogueCompleted += OnDialogueCompleted; subscribed = true; }
        UpdateView();
        if (!state.HasSeenDialogue("S4_Arrival") && !state.HasFlag(StoryIds.Flag.S4Rested))
            PlayNode("S4_Arrival", true);
        else if (state.HasFlag(StoryIds.Flag.S4Finished)) ShowEnding();
        else if (!string.IsNullOrEmpty(state.viewChoice)) ResumeEnding();
        else if (state.HasFlag(StoryIds.Flag.S4TangTalked)) OpenDecision();
    }

    private void OnDestroy()
    {
        if (subscribed && DialogueService.Instance != null)
            DialogueService.Instance.DialogueCompleted -= OnDialogueCompleted;
    }

    private void Bind(string path, Action action)
    {
        Transform target = transform.Find("S2 Canvas/" + path);
        if (target == null) { Debug.LogError("[S4] 缺少 UI：" + path); return; }
        BindButton(target.GetComponent<Button>(), action);
    }
    private static void BindButton(Button button, Action action)
    {
        if (button != null) button.onClick.AddListener(() => action());
    }

    private void PlayNode(string node, bool once = false)
    {
        DialogueService service = DialogueService.Ensure();
        if (service != null) service.Play(node, this, once);
    }

    private void OnDialogueCompleted(string node)
    {
        if (node == "S4_Rest")
        {
            blackout.SetActive(false);
            UpdateView();
            PlayNode("S4_DayIntro", true);
        }
        else if (node == "S4_Tang")
        {
            state.timeId = "saturday.1600";
            state.stepId = "decision";
            Persist();
            UpdateView();
            PlayNode("S4_Decision", true);
        }
        else if (node == "S4_Decision") OpenDecision();
        else if (node == "S4_EndTogether" || node == "S4_EndAlone")
        {
            state.SetFlag(StoryIds.Flag.S4Finished);
            state.stepId = "demoComplete";
            state.timeId = "saturday.1605";
            Persist();
            ShowEnding();
        }
        else Persist();
        UpdateView();
    }

    public void ArchiveBadge()
    {
        if (state == null || ModalOpen || state.HasFlag(StoryIds.Flag.S4BadgeArchived)) return;
        if (!state.HasItem(StoryIds.Item.Badge))
        {
            ShowInfo("社团纪念盒", "先找到林未的旧工牌。", false);
            return;
        }
        state.SetFlag(StoryIds.Flag.S4BadgeArchived);
        state.sanMax = 120;
        state.stepId = "badgeArchived";
        UpdateView();
        PlayNode("S4_Badge");
    }

    public void Rest()
    {
        if (state == null || ModalOpen || state.HasFlag(StoryIds.Flag.S4Rested)) return;
        if (!state.HasFlag(StoryIds.Flag.S4BadgeArchived))
        {
            ShowInfo("安全屋", "先把林未的旧工牌放进社团纪念盒。", false);
            return;
        }
        state.SetFlag(StoryIds.Flag.S4Rested);
        state.san = state.sanMax;
        state.health = 3;
        state.locationVariant = StoryIds.Variant.Day;
        state.timeId = "saturday.1000";
        state.stepId = "dayReview";
        blackout.SetActive(true);
        UpdateView();
        PlayNode("S4_Rest");
    }

    public void TalkYuan() => Talk(StoryIds.Flag.S4YuanTalked, "S4_Yuan");
    public void TalkXu() => Talk(StoryIds.Flag.S4XuTalked, "S4_Xu");
    public void TalkTang()
    {
        if (state == null || ModalOpen || !state.HasFlag(StoryIds.Flag.S4Rested) ||
            state.HasFlag(StoryIds.Flag.S4TangTalked)) return;
        if (!state.HasFlag(StoryIds.Flag.S4YuanTalked) || !state.HasFlag(StoryIds.Flag.S4XuTalked))
        {
            ShowInfo("白天复盘", "先和袁祁、许圆各聊一次，再去找唐澈。", false);
            return;
        }
        state.SetFlag(StoryIds.Flag.S4TangTalked);
        UpdateView();
        PlayNode("S4_Tang");
    }
    private void Talk(string flag, string node)
    {
        if (state == null || ModalOpen || !state.HasFlag(StoryIds.Flag.S4Rested) ||
            state.HasFlag(flag)) return;
        state.SetFlag(flag);
        UpdateView();
        PlayNode(node);
    }

    public void OpenDecision()
    {
        if (state == null || DialogueRunning || !state.HasFlag(StoryIds.Flag.S4TangTalked) ||
            !string.IsNullOrEmpty(state.viewChoice) || endingPanel.activeSelf) return;
        choicePanel.SetActive(true);
    }
    private void SelectChoice(string choice)
    {
        if (state == null || DialogueRunning || !string.IsNullOrEmpty(state.viewChoice)) return;
        pendingChoice = choice;
        choicePanel.SetActive(false);
        confirmText.text = choice == StoryIds.Choice.ViewTogether
            ? "四个人一起打开旧工程。队友也会看到文件里的异常。\n\n确认后不可反悔。"
            : "你先独自打开旧工程。队友会在之后才走近屏幕；独自直面未知将损失 10 SAN。\n\n确认后不可反悔。";
        confirmPanel.SetActive(true);
    }
    private void ConfirmChoice()
    {
        if (state == null || DialogueRunning || string.IsNullOrEmpty(pendingChoice) ||
            !string.IsNullOrEmpty(state.viewChoice)) return;
        confirmPanel.SetActive(false);
        if (!development) Persist(); // last quiet checkpoint before the irreversible choice
        state.ChooseView(pendingChoice);
        state.stepId = "ending";
        if (pendingChoice == StoryIds.Choice.ViewAlone) ApplyShock("s4_alone", -10);
        pendingChoice = null;
        Persist();
        ResumeEnding();
    }
    private void ResumeEnding()
    {
        if (state == null || string.IsNullOrEmpty(state.viewChoice)) return;
        oldProjectPanel.SetActive(true);
        projectStatus.text = state.viewChoice == StoryIds.Choice.ViewTogether
            ? "<color=#df4444>【状态：缺席】</color>" :
                "<color=#df4444>【状态：缺席】</color> → <color=#68d58a>【状态：在线】</color>\n" +
                "<color=#df4444>【05_林未：缺席】</color>";
        PlayNode(state.viewChoice == StoryIds.Choice.ViewTogether ? "S4_EndTogether" : "S4_EndAlone");
    }
    private void ShowEnding()
    {
        oldProjectPanel.SetActive(false);
        endingText.text = "距离 48H 结束还剩\n\n24:00:00\n\n前 24H Demo 完结";
        endingPanel.SetActive(true);
    }

    public void OpenRecord()
    {
        if (state == null || ModalOpen) return;
        recordText.text = "【社团自留资料】\n旧照片背面 23:30；林未工牌编号 05；机房借用便签指向单数栋 3F 靠窗第 3 台。\n分工表第五格被撕去。" +
            (state.HasFlag(StoryIds.Flag.S4BadgeArchived) ? "\n林未的工牌已放入社团纪念盒；参赛证仍作为调查证据保留。" : "") +
            "\n\n【现实调查】\n旧工程 AfterLightsOut_Backup 已拷入 U 盘；周六_16:00.scene 在 16:00 解锁。" +
            (state.HasFlag(StoryIds.Flag.P3MemoHeard) ? "\n\n【录音备忘】\n" + DialogueDocuments.GetBody("s3.memo") : "") +
            (state.HasFlag(StoryIds.Flag.P2GlassRead) ? "\n\n【残影】\n别只看照片，看看他们留下了什么。" : "") +
            "\n\n路线：" + state.routeChoice + "；查看选择：" + (state.viewChoice ?? "未决定");
        recordText.fontSize = 25;
        recordPanel.SetActive(true);
    }
    public void CloseRecord() => recordPanel.SetActive(false);
    public void OpenInventory() { if (state != null && !ModalOpen) inventoryPanel.SetActive(true); }
    public void CloseInventory() => inventoryPanel.SetActive(false);
    public void ShowInventoryUseHint(string item) => ShowInfo(S2InventoryGrid.ItemName(item), "这是调查证据。请在背包中点击“查看”核对内容。", false);
    public void InspectInventoryItem(string item)
    {
        if (state == null || !state.HasItem(item)) return;
        string body = item == StoryIds.Item.Photo ? "去年活动室的照片，背面标记 23:30。" :
            item == StoryIds.Item.Badge ? "林未的旧工牌，编号 05。" +
                (state.HasFlag(StoryIds.Flag.S4BadgeArchived) ? "已归档在社团纪念盒，记录仍可查看。" : "") :
            item == StoryIds.Item.MachineNote ? "借用记录：单数栋 3F 机房靠窗第 3 台，周五 23:30。" :
            item == StoryIds.Item.TeamSheet ? "分工表残页，第五格被撕去，余编号 05。" :
            "旧工程 AfterLightsOut_Backup；周六_16:00.scene 于今天 16:00 解锁。";
        ShowInfo(S2InventoryGrid.ItemName(item), body, false);
    }
    public void TogglePause() { if (state != null && !DialogueRunning && !endingPanel.activeSelf) pausePanel.SetActive(!pausePanel.activeSelf); }
    public void SaveManual() { if (state == null || DialogueRunning) return; pausePanel.SetActive(false); SaveSlotPanel.OpenSave(CaptureSaveSlot); }
    public void LoadManual() { if (DialogueRunning) return; pausePanel.SetActive(false); SaveSlotPanel.OpenLoad(CaptureSaveSlot); }
    public void ReturnTitle() { DialogueService.Shutdown(); GameFlow.LoadLocation(GameFlow.TitleScene); }
    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (state == null || development || DialogueRunning || confirmPanel.activeSelf) return null;
        return new SaveSlotSnapshot { chapter = "S4", payload = JsonUtility.ToJson(state),
            gameTime = timeText.text, locationLabel = "S4 · " + timeText.text + " · 5F 活动室" };
    }
    private bool Persist()
    {
        if (development || state == null || DialogueRunning) return false;
        if (!GameStateStore.SaveCheckpoint(state, out string error))
        {
            Debug.LogError("[S4] 存档失败：" + error);
            return false;
        }
        PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 4);
        PlayerPrefs.Save();
        return true;
    }
    private void UpdateView()
    {
        if (state == null) return;
        bool day = state.HasFlag(StoryIds.Flag.S4Rested);
        if (dayRoomUpper != null) dayRoomUpper.SetActive(day);
        if (dayWindow != null) dayWindow.SetActive(day);
        locationText.text = "单数栋 · 5F 活动室";
        timeText.text = state.HasFlag(StoryIds.Flag.S4TangTalked) ? "周六 16:00" : day ? "周六 10:00" : "周六 02:00";
        objectiveText.text = string.Empty;
        badgeButton.gameObject.SetActive(!state.HasFlag(StoryIds.Flag.S4BadgeArchived));
        restButton.gameObject.SetActive(state.HasFlag(StoryIds.Flag.S4BadgeArchived) && !day);
        yuanButton.gameObject.SetActive(day && !state.HasFlag(StoryIds.Flag.S4YuanTalked));
        xuButton.gameObject.SetActive(day && !state.HasFlag(StoryIds.Flag.S4XuTalked));
        tangButton.gameObject.SetActive(day && !state.HasFlag(StoryIds.Flag.S4TangTalked));
        computerButton.gameObject.SetActive(day && state.HasFlag(StoryIds.Flag.S4TangTalked) &&
            string.IsNullOrEmpty(state.viewChoice));
        vitals.SetSan(state.san, state.sanMax);
        vitals.SetHealth(state.health, 3);
    }
    private void ShowInfo(string title, string body, bool residue)
    {
        infoTitle.text = (residue ? "【残影】" : "") + title;
        infoBody.text = body;
        infoPanel.SetActive(true);
    }
    public void CloseInfo() => infoPanel.SetActive(false);

    public IEnumerator ShowDocument(string id)
    {
        if (!DialogueDocuments.TryGet(id, out DialogueDocuments.Entry doc)) yield break;
        if (DialogueService.Instance != null) DialogueService.Instance.HideOverlay();
        ShowInfo(doc.title, doc.body, doc.residue);
        while (infoPanel.activeSelf) yield return null;
    }
    public IEnumerator ShowPhoto() { yield break; }
    public bool HasFlag(string id) => state != null && state.HasFlag(id);
    public bool HasItem(string id) => state != null && state.HasItem(id);
    public bool HasSeenDialogue(string id) => state != null && state.HasSeenDialogue(id);
    public void MarkDialogueSeen(string id) { if (state != null) state.MarkDialogueSeen(id); }
    public int San => state != null ? state.san : 0;
    public int Health => state != null ? state.health : 0;
    public string Route => state != null ? state.routeChoice : string.Empty;
    public string View => state != null ? state.viewChoice : string.Empty;
    public void SetFlag(string id) { if (state != null) state.SetFlag(id); }
    public void ClearFlag(string id) { if (state != null) state.ClearFlag(id); }
    public void AddItem(string id) { if (state != null) state.AddItem(id); }
    public void ApplyShock(string id, int sanDelta, int healthDelta = 0)
    {
        if (state == null) return;
        string guard = "flag." + id + ".shock";
        if (!state.SetFlag(guard)) return;
        state.san = Mathf.Clamp(state.san + sanDelta, 0, state.sanMax);
        state.health = Mathf.Clamp(state.health + healthDelta, 0, 3);
        UpdateView();
    }
    public void ChooseRoute(string choice) { if (state != null) state.ChooseRoute(choice); }
    public void ChooseView(string choice) { if (state != null) state.ChooseView(choice); }
    public void RequestCheckpoint() { if (!DialogueRunning) Persist(); }
    public void RequestGoTo(string locationId, string variant) =>
        Debug.LogWarning("[Dialogue] S4 不使用 go_to：" + locationId + "/" + variant);
    public void RefreshHud() => UpdateView();
}
