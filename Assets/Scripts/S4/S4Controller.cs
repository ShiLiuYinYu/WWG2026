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
    public GameObject dayWindow, dayRoomUpper, blackout, choicePanel;
    public Text locationText, timeText, objectiveText, recordText, inventoryText, infoTitle, infoBody;
    public GameObject infoPanel, recordPanel, inventoryPanel, pausePanel;
    public ClubPhotoView photoView;
    public Button badgeButton, restButton, computerButton;
    public VitalsBars vitals;

    private static GameState pending;
    private static bool pendingDevelopment;
    private GameState state;
    private bool development, subscribed;
    private const string ViewReviewStep = "viewReview";
    private const string ViewPlaybackStep = "viewPlayback";
    private bool ViewingTogether => state.viewChoice == StoryIds.Choice.ViewTogether;
    private bool ViewingAlone => state.viewChoice == StoryIds.Choice.ViewAlone;
    private bool ReviewingView => (ViewingTogether || ViewingAlone) &&
        state.stepId != ViewPlaybackStep && !state.HasFlag(StoryIds.Flag.S4Finished);
    private bool WitnessesReady => state.HasFlag(StoryIds.Flag.S4YuanWitness) &&
        state.HasFlag(StoryIds.Flag.S4XuWitness);
    private static bool DialogueRunning => DialogueService.Instance != null && DialogueService.Instance.IsRunning;
    internal bool IsInventoryBlocked => DialogueRunning || SaveSlotPanel.IsOpen || infoPanel.activeSelf ||
        recordPanel.activeSelf || pausePanel.activeSelf || choicePanel.activeSelf ||
        blackout.activeSelf || (photoView != null && photoView.IsOpen);
    private bool ModalOpen => IsInventoryBlocked || inventoryPanel.activeSelf;

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
        pausePanel.SetActive(false); choicePanel.SetActive(false);
        blackout.SetActive(false);
        inventoryPanel.GetComponent<S2InventoryGrid>().InstallSceneInput(this, () => state == null || ModalOpen);
        SharedInteractionHud hud = GetComponentInChildren<SharedInteractionHud>(true);
        BindButton(hud.inventoryButton, OpenInventory);
        BindButton(hud.pauseButton, TogglePause);
        BindButton(hud.recordButton, OpenRecord);
        Bind("Inspect/Close", CloseInfo);
        Bind("Task record/Close", CloseRecord);
        BindButton(inventoryPanel.transform.Find("Bag contents/Close")?.GetComponent<Button>(), CloseInventory);
        Bind("Pause/Resume", TogglePause);
        Bind("Pause/Save", SaveManual);
        Bind("Pause/Load", LoadManual);
        Bind("Pause/Title", ReturnTitle);
        BindButton(badgeButton, ArchiveBadge);
        BindButton(restButton, Rest);
        BindButton(computerButton, UseComputer);
        Bind("S4 Choice/Together", () => SelectChoice(StoryIds.Choice.ViewTogether), "一起看 · 请队友核对画面");
        Bind("S4 Choice/Alone", () => SelectChoice(StoryIds.Choice.ViewAlone), "独自查看 · 用 U 盘载入（SAN -10）");
        Bind("S4 Choice/Close", () => choicePanel.SetActive(false));

        state = pending ?? SaveSlotStore.TakeLoadedState(StoryIds.Chapter.Finale);
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
        else if (state.HasFlag(StoryIds.Flag.S4Finished)) EnterSecondHalf();
        else if (state.stepId == ViewPlaybackStep) ResumeEnding();
        else if (ReviewingView) StartViewReview();
        else if (state.HasFlag(StoryIds.Flag.S4Rested)) PlayDayReview();
    }

    private void OnDestroy()
    {
        if (subscribed && DialogueService.Instance != null)
            DialogueService.Instance.DialogueCompleted -= OnDialogueCompleted;
    }

    private void Bind(string path, Action action, string caption = null)
    {
        Transform target = transform.Find("S2 Canvas/" + path);
        if (target == null) { Debug.LogError("[S4] 缺少 UI：" + path); return; }
        Button button = target.GetComponent<Button>();
        BindButton(button, action);
        Text label = caption != null && button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (label != null) label.text = caption;
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
        switch (node)
        {
            case "S4_Rest":
                blackout.SetActive(false);
                break;
            case "S4_Yuan":
                state.SetFlag(StoryIds.Flag.S4YuanTalked);
                break;
            case "S4_Xu":
                state.SetFlag(StoryIds.Flag.S4XuTalked);
                break;
            case "S4_Tang":
                state.SetFlag(StoryIds.Flag.S4TangTalked);
                state.timeId = "saturday.1600";
                state.stepId = "decision";
                break;
            case "S4_YuanWitness":
                state.SetFlag(StoryIds.Flag.S4YuanWitness);
                break;
            case "S4_XuWitness":
                state.SetFlag(StoryIds.Flag.S4XuWitness);
                break;
            case "S4_EndTogether":
            case "S4_EndAlone":
                state.SetFlag(StoryIds.Flag.S4Finished);
                state.stepId = "demoComplete";
                state.timeId = "saturday.1605";
                break;
        }
        Persist();
        UpdateView();
        if (state.HasFlag(StoryIds.Flag.S4Finished)) EnterSecondHalf();
        else if (ReviewingView && ViewingTogether) PlayTogetherReview();
        else if (state.HasFlag(StoryIds.Flag.S4Rested) && string.IsNullOrEmpty(state.viewChoice)) PlayDayReview();
    }

    private void PlayDayReview()
    {
        if (state.HasFlag(StoryIds.Flag.S4TangTalked))
        {
            if (state.HasSeenDialogue("S4_Decision")) OpenDecision();
            else PlayNode("S4_Decision", true);
        }
        else if (!state.HasSeenDialogue("S4_DayIntro")) PlayNode("S4_DayIntro", true);
        else if (!state.HasFlag(StoryIds.Flag.S4YuanTalked)) PlayNode("S4_Yuan");
        else if (!state.HasFlag(StoryIds.Flag.S4XuTalked)) PlayNode("S4_Xu");
        else PlayNode("S4_Tang");
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

    public void OpenDecision()
    {
        if (state == null || DialogueRunning || !state.HasFlag(StoryIds.Flag.S4TangTalked) ||
            !string.IsNullOrEmpty(state.viewChoice)) return;
        choicePanel.SetActive(true);
    }
    private void UseComputer()
    {
        if (state == null || ModalOpen || !state.HasFlag(StoryIds.Flag.S4TangTalked)) return;
        if (string.IsNullOrEmpty(state.viewChoice))
        {
            OpenDecision();
            return;
        }
        if (!ReviewingView || (ViewingTogether && !WitnessesReady)) return;
        if (ViewingAlone && !state.HasFlag(StoryIds.Flag.S4UsbMounted))
        {
            ShowInfo("离线预览", "从背包选中装有旧工程的 U 盘，点击「使用」，再回到电脑播放。", false);
            return;
        }
        state.stepId = ViewPlaybackStep;
        Persist();
        ResumeEnding();
    }
    private void SelectChoice(string choice)
    {
        if (state == null || DialogueRunning || !choicePanel.activeInHierarchy ||
            !string.IsNullOrEmpty(state.viewChoice)) return;
        if (!state.ChooseView(choice)) return;
        choicePanel.SetActive(false);
        state.stepId = ViewReviewStep;
        if (choice == StoryIds.Choice.ViewAlone) ApplyShock("s4_alone", -10);
        Persist();
        StartViewReview();
    }
    private void StartViewReview()
    {
        state.stepId = ViewReviewStep;
        UpdateView();
        if (ViewingTogether) PlayTogetherReview();
        else PlayNode("S4_ReviewAlone", true);
    }
    private void PlayTogetherReview()
    {
        if (!state.HasSeenDialogue("S4_ReviewTogether")) PlayNode("S4_ReviewTogether", true);
        else if (!state.HasFlag(StoryIds.Flag.S4YuanWitness)) PlayNode("S4_YuanWitness");
        else if (!state.HasFlag(StoryIds.Flag.S4XuWitness)) PlayNode("S4_XuWitness");
    }
    private void ResumeEnding()
    {
        if (state == null || string.IsNullOrEmpty(state.viewChoice)) return;
        string node = state.viewChoice == StoryIds.Choice.ViewTogether ? "S4_EndTogether" : "S4_EndAlone";
        blackout.SetActive(true);
        DialogueService.Ensure()?.Play(node, this, showPortraits: false);
    }
    /// <summary>Ends the first 24H and hands straight over to S5; there is no intermediate screen.</summary>
    private void EnterSecondHalf()
    {
        if (state == null || !state.HasFlag(StoryIds.Flag.S4Finished)) return;
        blackout.SetActive(true);
        try { S5Controller.Begin(state, development); }
        catch (Exception e)
        {
            Debug.LogError("[S4] 无法进入后 24H：" + e.Message);
            blackout.SetActive(false);
            ShowInfo("无法进入后 24H", "后 24H 场景不可用：" + e.Message, false);
        }
    }

    public void OpenRecord()
    {
        if (state == null || ModalOpen) return;
        recordText.text = "【社团自留资料】\n旧照片背面 23:30；林未工牌编号 05；机房借用便签指向单数栋 3F 靠窗第 3 台。\n分工表第五格被撕去。" +
            (state.HasFlag(StoryIds.Flag.S4BadgeArchived) ? "\n林未的工牌已放入社团纪念盒；参赛证仍作为调查证据保留。" : "") +
            "\n\n【现实调查】\n旧工程 AfterLightsOut_Backup 已拷入 U 盘；周六_16:00.scene 在 16:00 解锁。" +
            (ViewingTogether ? "\n共同查看：袁祁核对名单 " + (state.HasFlag(StoryIds.Flag.S4YuanWitness) ? "✓" : "待完成") +
                "；许圆核对实物 " + (state.HasFlag(StoryIds.Flag.S4XuWitness) ? "✓" : "待完成") + "。" : "") +
            (ViewingAlone ? "\n独自查看：" + (state.HasFlag(StoryIds.Flag.S4UsbMounted)
                ? "U 盘已载入，可在电脑上播放。"
                : "从背包使用 U 盘，载入离线预览。") : "") +
            (state.HasFlag(StoryIds.Flag.P3MemoHeard) ? "\n\n【录音备忘】\n" + DialogueDocuments.GetBody("s3.memo") : "") +
            (state.HasFlag(StoryIds.Flag.P2GlassRead) ? "\n\n【残影】\n别只看照片，看看他们留下了什么。" : "") +
            "\n\n路线：" + state.routeChoice + "；查看选择：" + (state.viewChoice ?? "未决定");
        recordText.fontSize = 25;
        recordPanel.SetActive(true);
    }
    public void CloseRecord() => recordPanel.SetActive(false);
    public void OpenInventory() { if (state != null && !ModalOpen) inventoryPanel.SetActive(true); }
    public void CloseInventory() => inventoryPanel.SetActive(false);
    public void UseInventoryItem(string item)
    {
        if (state == null || ModalOpen || !state.HasItem(item)) return;
        if (item == StoryIds.Item.OldProjectUsb && ReviewingView && ViewingAlone)
        {
            state.SetFlag(StoryIds.Flag.S4UsbMounted);
            Persist();
            UpdateView();
            ShowInfo("离线预览 · U 盘已载入", "唐澈留下的离线工具已识别 U 盘快照。点击电脑播放，工具会把状态变化写进本地日志。", false);
            return;
        }
        ShowInfo(S2InventoryGrid.ItemName(item), "这是调查证据。请在背包中点击「查看」核对内容。", false);
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
        string body = item == StoryIds.Item.Photo ? "去年活动室的照片，背面标记 23:30。" :
            isBadge ? string.Empty :
            item == StoryIds.Item.MachineNote ? "借用记录：单数栋 3F 机房靠窗第 3 台，周五 23:30。" :
            item == StoryIds.Item.TeamSheet ? "分工表残页，第五格被撕去，余编号 05。" :
            "旧工程 AfterLightsOut_Backup；周六_16:00.scene 于今天 16:00 解锁。";
        ShowInfo(isBadge ? string.Empty : S2InventoryGrid.ItemName(item), body, false, showBadge: isBadge);
    }
    public void TogglePause()
    {
        if (state == null || DialogueRunning || blackout.activeSelf) return;
        if (photoView != null) photoView.Close();
        pausePanel.SetActive(!pausePanel.activeSelf);
    }
    public void SaveManual() { if (state == null || DialogueRunning) return; pausePanel.SetActive(false); SaveSlotPanel.OpenSave(CaptureSaveSlot); }
    public void LoadManual() { if (DialogueRunning) return; pausePanel.SetActive(false); SaveSlotPanel.OpenLoad(CaptureSaveSlot); }
    public void ReturnTitle() { DialogueService.Shutdown(); GameFlow.LoadLocation(GameFlow.TitleScene); }
    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (state == null || development || DialogueRunning) return null;
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
        computerButton.gameObject.SetActive(day && state.HasFlag(StoryIds.Flag.S4TangTalked) &&
            (string.IsNullOrEmpty(state.viewChoice) || (ReviewingView && (!ViewingTogether || WitnessesReady))));
        SetHoverCaption(computerButton, ReviewingView ? "播放旧场景" : "查看旧工程");
        vitals.SetSan(state.san, state.sanMax);
        vitals.SetHealth(state.health, 3);
    }
    private static void SetHoverCaption(Button button, string caption)
    {
        if (button.TryGetComponent(out S4HotspotHover hover)) hover.caption = caption;
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
