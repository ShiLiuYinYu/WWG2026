using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WWG;

/// <summary>Single fixed view for the 3F archive recovery puzzle.</summary>
public sealed class S3Controller : MonoBehaviour, IDialogueContext
{
    public Text locationText, timeText, objectiveText, feedbackText, recordText, inventoryText;
    public Text infoTitle, infoBody, archiveFeedback;
    public Text[] fieldLabels;
    public GameObject infoPanel, archivePanel, recordPanel, inventoryPanel, pausePanel;
    public Button memoButton, copyButton;
    public VitalsBars vitals;

    private static GameState pending;
    private static bool pendingDevelopment;
    private GameState state;
    private bool development, subscribed;
    private readonly int[] selections = { -1, -1, -1 };
    private float dangerSeconds, zeroSanSeconds, idleSeconds;
    private int hintStage;

    private static readonly string[][] Options =
    {
        new[] { "袁祁", "林未", "匿名05" },
        new[] { "单数栋 5F · 活动室主机", "偶数栋 4F · 值班机", "单数栋 3F · 靠窗第 3 台" },
        new[] { "16:00", "23:30", "02:00" }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPending() { pending = null; pendingDevelopment = false; }

    public static void Begin(GameState handoff, bool editorDevelopment = false)
    {
        if (handoff == null || !handoff.HasItem(StoryIds.Item.MachineNote) ||
            !handoff.Validate(out string error))
            throw new ArgumentException("S3 需要有效的 S2 借用便签状态");
        pending = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(handoff));
        pendingDevelopment = editorDevelopment;
        pending.chapterId = StoryIds.Chapter.ServerRecovery;
        pending.stepId = pending.HasFlag(StoryIds.Flag.P3Copied) ? "copyComplete" : "p3Start";
        pending.locationId = StoryIds.Location.ServerRoom3F;
        pending.locationVariant = StoryIds.Variant.Night;
        pending.timeId = pending.HasFlag(StoryIds.Flag.P3Recovered) ? "saturday.0135" : "saturday.0100";
        if (!GameFlow.LoadLocation(GameFlow.S3Scene))
            throw new InvalidOperationException("S3 机房未加入 Build Settings");
    }

    public GameState CurrentState => state;
    private static bool DialogueRunning => DialogueService.Instance != null && DialogueService.Instance.IsRunning;
    private bool BlockingModal => DialogueRunning || infoPanel.activeSelf || recordPanel.activeSelf ||
        inventoryPanel.activeSelf || pausePanel.activeSelf;
    private bool CanAct => state != null && !BlockingModal && !archivePanel.activeSelf;

    private void Start()
    {
        Application.runInBackground = true;
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
        if (font != null)
            foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = font;
        infoPanel.SetActive(false); archivePanel.SetActive(false); recordPanel.SetActive(false);
        inventoryPanel.SetActive(false); pausePanel.SetActive(false);
        state = pending;
        development = pendingDevelopment;
        pending = null;
        pendingDevelopment = false;
#if UNITY_EDITOR
        if (state == null)
        {
            string bootstrap = UnityEditor.SessionState.GetString("WWG_S3Bootstrap", "");
            if (!string.IsNullOrEmpty(bootstrap))
            {
                UnityEditor.SessionState.EraseString("WWG_S3Bootstrap");
                state = JsonUtility.FromJson<GameState>(bootstrap);
                development = true;
            }
        }
#endif
        if (state == null && GameStateStore.LoadCheckpoint(out GameState saved, out _) &&
            saved.chapterId == StoryIds.Chapter.ServerRecovery) state = saved;
        if (state == null || !state.HasItem(StoryIds.Item.MachineNote))
        {
            Debug.LogError("[S3] 缺少 S2 交接状态或 S3 检查点。");
            return;
        }
        state.EnsureCollections();
        state.chapterId = StoryIds.Chapter.ServerRecovery;
        state.locationId = StoryIds.Location.ServerRoom3F;
        state.locationVariant = StoryIds.Variant.Night;
        DialogueService service = DialogueService.Ensure();
        if (service != null) { service.DialogueCompleted += OnDialogueCompleted; subscribed = true; }
        UpdateView();
        if (!state.HasSeenDialogue("S3_Intro")) PlayNode("S3_Intro", true);
    }

    private void OnDestroy()
    {
        if (subscribed && DialogueService.Instance != null)
            DialogueService.Instance.DialogueCompleted -= OnDialogueCompleted;
    }

    private void Update()
    {
        if (state == null || BlockingModal) return;
        if (!state.HasFlag(StoryIds.Flag.P3Recovered))
        {
            idleSeconds += Time.deltaTime;
            if (hintStage == 0 && idleSeconds >= 60f)
            {
                hintStage = 1;
                PlayNode("S3_Hint_Index", true);
            }
            else if (hintStage == 1 && idleSeconds >= 120f)
            {
                hintStage = 2;
                feedbackText.text = "证据提示：工牌核对登记人、借用便签核对机位、旧照片背面核对时间。";
                archiveFeedback.text = feedbackText.text;
            }
        }
        dangerSeconds += Time.deltaTime;
        if (dangerSeconds >= 20f)
        {
            dangerSeconds -= 20f;
            state.san = Mathf.Max(0, state.san - 1);
            UpdateView();
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
                    ReturnToS2(true);
                    return;
                }
                UpdateView();
            }
        }
    }

    public void InspectSketch()
    {
        if (!CanAct) return;
        state.SetFlag(StoryIds.Flag.P3SketchRead);
        Persist();
        PlayNode("S3_Sketch");
    }

    public void OpenComputer()
    {
        if (!CanAct) return;
        archivePanel.SetActive(true);
        archiveFeedback.text = state.HasFlag(StoryIds.Flag.P3Recovered)
            ? "AfterLightsOut_Backup 已提取。播放录音，再把工程拷入 U 盘。"
            : "核对纸面记录，选择三项索引后提取。背包可随时查看。";
        UpdateView();
        if (!state.HasSeenDialogue("S3_Computer")) PlayNode("S3_Computer", true);
    }

    public void CloseComputer() { if (!DialogueRunning) archivePanel.SetActive(false); }
    public void CycleField(int index)
    {
        if (state == null || DialogueRunning || index < 0 || index >= selections.Length ||
            state.HasFlag(StoryIds.Flag.P3Recovered)) return;
        selections[index] = (selections[index] + 1) % Options[index].Length;
        fieldLabels[index].color = new Color(0.62f, 0.9f, 0.7f);
        idleSeconds = 0;
        UpdateFields();
    }
    private void UpdateFields()
    {
        for (int i = 0; i < fieldLabels.Length; i++)
            fieldLabels[i].text = selections[i] < 0 ? "点击选择" : Options[i][selections[i]];
    }

    public void Extract()
    {
        if (state == null || DialogueRunning || state.HasFlag(StoryIds.Flag.P3Recovered)) return;
        if (!state.HasItem(StoryIds.Item.Badge) || !state.HasItem(StoryIds.Item.Photo) ||
            !state.HasItem(StoryIds.Item.MachineNote))
        {
            archiveFeedback.text = "还缺少工牌、旧照片或机房借用便签。";
            return;
        }
        if (selections[0] != 1 || selections[1] != 2 || selections[2] != 1)
        {
            int[] correct = { 1, 2, 1 };
            for (int i = 0; i < selections.Length; i++)
                fieldLabels[i].color = selections[i] == correct[i]
                    ? new Color(0.62f, 0.9f, 0.7f) : new Color(1f, 0.45f, 0.38f);
            archiveFeedback.text = "[校验失败] 找不到匹配的本地归档槽；与纸质记录不一致。已选项保留。";
            if (selections[0] == 1 && selections[1] == 2) archiveFeedback.text += " 登记人与机位已核对。";
            return;
        }
        state.SetFlag(StoryIds.Flag.P3Recovered);
        foreach (Text label in fieldLabels) label.color = new Color(0.62f, 0.9f, 0.7f);
        state.stepId = "archiveRecovered";
        state.timeId = "saturday.0135";
        archiveFeedback.text = "提取完成：AfterLightsOut_Backup／备忘_别删.wav／周六_16:00.scene";
        UpdateView();
        PlayNode("S3_Recovered");
    }

    public void PlayMemo()
    {
        if (state == null || DialogueRunning || !state.HasFlag(StoryIds.Flag.P3Recovered)) return;
        PlayNode("S3_Memo");
    }

    public void CopyProject()
    {
        if (state == null || DialogueRunning || !state.HasFlag(StoryIds.Flag.P3MemoHeard)) return;
        if (state.HasFlag(StoryIds.Flag.P3Copied))
        {
            archiveFeedback.text = "旧工程已在 U 盘中。";
            return;
        }
        state.AddItem(StoryIds.Item.OldProjectUsb);
        state.SetFlag(StoryIds.Flag.P3Copied);
        state.stepId = "copyComplete";
        state.timeId = "saturday.0145";
        UpdateView();
        PlayNode("S3_Copied");
    }

    public void GoExit()
    {
        if (!CanAct) return;
        ReturnToS2(state.HasFlag(StoryIds.Flag.P3Copied));
    }

    private void ReturnToS2(bool toSafeRoom)
    {
        if (state == null) return;
        Persist();
        S2Controller.ReturnFromS3(state, development, toSafeRoom);
    }

    public void OpenRecord()
    {
        if (state == null || DialogueRunning || infoPanel.activeSelf || inventoryPanel.activeSelf) return;
        recordText.text = "【社团自留资料】\n工牌：林未，编号 05。\n借用便签：单数栋 3F，靠窗第 3 台；周五 23:30。\n旧照片背面：23:30。" +
            (state.HasFlag(StoryIds.Flag.P3SketchRead) ? "\n\n【机房草图与便签】\n去年队友周六早上到机房时，机器还开着，林未没有回来。" : "") +
            (state.HasFlag(StoryIds.Flag.P3MemoHeard) ? "\n\n【林未的录音备忘全文】\n" + DialogueDocuments.GetBody("s3.memo") : "") +
            "\n\n【当前调查】\n" + (state.HasFlag(StoryIds.Flag.P3Copied) ? "旧工程已拷入 U 盘，可返回 5F。" : "核对三项索引并带回旧工程。") +
            "\n路线选择：" + state.routeChoice;
        recordText.fontSize = state.HasFlag(StoryIds.Flag.P3MemoHeard) ? 24 : 31;
        recordPanel.SetActive(true);
    }
    public void CloseRecord() => recordPanel.SetActive(false);
    public void OpenInventory()
    {
        if (state == null || DialogueRunning || infoPanel.activeSelf || recordPanel.activeSelf) return;
        foreach (S2InventorySlot slot in inventoryPanel.GetComponentsInChildren<S2InventorySlot>(true))
        {
            Text label = slot.GetComponentInChildren<Text>(true);
            if (label == null) continue;
            bool evidence = slot.item == StoryIds.Item.Photo || slot.item == StoryIds.Item.Badge ||
                slot.item == StoryIds.Item.MachineNote;
            label.color = hintStage >= 2 && evidence ? new Color(1f, 0.85f, 0.35f) : Color.white;
        }
        inventoryPanel.SetActive(true);
    }
    public void CloseInventory() => inventoryPanel.SetActive(false);
    public void ShowInventoryUseHint(string item)
    {
        if (feedbackText != null) feedbackText.text = S2InventoryGrid.ItemName(item) + "是调查证据。请在背包中点“查看”核对内容。";
    }
    public void InspectInventoryItem(string item)
    {
        if (state == null || !state.HasItem(item)) return;
        string body = item == StoryIds.Item.Photo ? "【社团自留资料】\n去年的活动室照片，背面标注 23:30。" :
            item == StoryIds.Item.Badge ? "【社团自留资料】\n林未的旧工牌。正面姓名林未，编号 05；背面写着“玻璃两边，各存一半”。" :
            item == StoryIds.Item.MachineNote ? "【社团自留资料】\n机房借用便签：林未，周五 23:30，单数栋 3F 机房靠窗第 3 台。" :
            item == StoryIds.Item.TeamSheet ? "【社团自留资料】\n去年的分工表残页，第五格被撕去，只剩编号 05。" :
            item == StoryIds.Item.OldProjectUsb ? "【旧工程拷贝】\nAfterLightsOut_Backup 已存入 U 盘，包含周六_16:00.scene。" : string.Empty;
        ShowInfo(S2InventoryGrid.ItemName(item), body);
    }
    public void TogglePause()
    {
        if (state == null || DialogueRunning) return;
        pausePanel.SetActive(!pausePanel.activeSelf);
    }
    public void SaveManual()
    {
        if (state == null || DialogueRunning || development) return;
        pausePanel.SetActive(false);
        SaveSlotPanel.OpenSave(CaptureSaveSlot);
    }
    public void LoadManual()
    {
        if (DialogueRunning) return;
        pausePanel.SetActive(false);
        SaveSlotPanel.OpenLoad(CaptureSaveSlot);
    }
    public void ReturnTitle() { pausePanel.SetActive(false); GameFlow.LoadLocation(GameFlow.TitleScene); }
    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (state == null || development || DialogueRunning) return null;
        return new SaveSlotSnapshot { chapter = "S3", payload = JsonUtility.ToJson(state),
            gameTime = timeText.text, locationLabel = "S3 · " + timeText.text + " · 单数栋 3F 机房" };
    }
    public void CloseInfo() => infoPanel.SetActive(false);
    private void ShowInfo(string title, string body)
    {
        infoTitle.text = title;
        infoBody.text = body;
        infoPanel.SetActive(true);
    }
    private void UpdateView()
    {
        if (state == null) return;
        locationText.text = "单数栋 · 3F 机房";
        timeText.text = state.HasFlag(StoryIds.Flag.P3Copied) ? "周六 01:45" :
            state.HasFlag(StoryIds.Flag.P3Recovered) ? "周六 01:35" : "周六 01:00";
        objectiveText.text = state.HasFlag(StoryIds.Flag.P3Copied) ? "旧工程已拷入 U 盘 · 返回 5F" :
            state.HasFlag(StoryIds.Flag.P3MemoHeard) ? "将旧工程拷入 U 盘" :
            state.HasFlag(StoryIds.Flag.P3Recovered) ? "播放林未的录音备忘" : "核对三项纸面索引，恢复旧工程";
        vitals.SetSan(state.san, state.sanMax);
        vitals.SetHealth(state.health, 3);
        memoButton.gameObject.SetActive(state.HasFlag(StoryIds.Flag.P3Recovered));
        copyButton.gameObject.SetActive(state.HasFlag(StoryIds.Flag.P3MemoHeard));
        UpdateFields();
    }
    private void PlayNode(string nodeId, bool once = false)
    {
        DialogueService service = DialogueService.Ensure();
        if (service != null) service.Play(nodeId, this, once);
    }
    private void OnDialogueCompleted(string nodeId)
    {
        if (nodeId == "S3_Memo") state.SetFlag(StoryIds.Flag.P3MemoHeard);
        if (nodeId == "S3_Recovered" || nodeId == "S3_Memo" || nodeId == "S3_Copied") Persist();
        UpdateView();
    }
    private bool Persist()
    {
        if (development || state == null) return false;
        if (!GameStateStore.SaveCheckpoint(state, out string error))
        {
            Debug.LogError("[S3] 存档失败：" + error);
            return false;
        }
        PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 3);
        PlayerPrefs.Save();
        return true;
    }

    public IEnumerator ShowDocument(string documentId)
    {
        if (!DialogueDocuments.TryGet(documentId, out DialogueDocuments.Entry document)) yield break;
        if (DialogueService.Instance != null) DialogueService.Instance.HideOverlay();
        ShowInfo(document.title, document.body);
        while (infoPanel.activeSelf) yield return null;
    }
    public IEnumerator ShowPhoto() => null;
    public bool HasFlag(string id) => state != null && state.HasFlag(id);
    public bool HasItem(string id) => state != null && state.HasItem(id);
    public bool HasSeenDialogue(string nodeId) => state != null && state.HasSeenDialogue(nodeId);
    public void MarkDialogueSeen(string nodeId) { if (state != null) state.MarkDialogueSeen(nodeId); }
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
        if (state.HasFlag(guard)) return;
        state.SetFlag(guard);
        state.san = Mathf.Clamp(state.san + sanDelta, 0, state.sanMax);
        state.health = Mathf.Clamp(state.health + healthDelta, 0, 3);
        UpdateView();
    }
    public void ChooseRoute(string choice) { if (state != null) state.ChooseRoute(choice); }
    public void ChooseView(string choice) { if (state != null) state.ChooseView(choice); }
    public void RequestCheckpoint() { if (!DialogueRunning) Persist(); }
    public void RequestGoTo(string locationId, string variant) =>
        Debug.LogWarning("[Dialogue] S3 不使用 go_to：" + locationId + "/" + variant);
    public void RefreshHud() => UpdateView();
}
