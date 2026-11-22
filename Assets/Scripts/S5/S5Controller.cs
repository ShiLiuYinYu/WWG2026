using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// S5 复盘（周六 16:05–23:29，5F 活动室）：P4 证据核对、林未旧测试说明，然后等到熄灯进入 S6。
    /// 与 S2–S4 相同：章节控制器 + 检查点 + IDialogueContext。
    /// </summary>
    public class S5Controller : MonoBehaviour, IDialogueContext, IChapterInventory
    {
        public Text timeText, feedbackText, recordText;
        public Text infoTitle, infoBody;
        public GameObject infoPanel, recordPanel, casePanel;
        public Text caseTitle, caseBody, caseFeedback, caseRows;
        public Button badgeButton, ruleButton, computerButton, waitButton;
        public Button frameOne, frameTwo, frameThree, logA, logB, replayButton, caseClose;

        private static GameState pending;
        private static bool pendingDevelopment;

        private GameState state;
        private bool development;
        private bool subscribed;
        private bool pendingLightsOut;
        private string logFirst, logSecond;

        private const string CaseTogetherText =
            "三张帧卡是我们各自看到的那一刻，不是监控。先把它们和对面的房间草图对上，再选一张能证明「写在这里之前」的。\n\n" +
            "房间草图：插线板在桌子左侧，许圆当时画的纸团就落在插线板旁边；名单抄件压在桌角。\n\n" +
            "名单抄件只剩五个格子，第五格写着「05」，名字被擦掉了。";

        private const string CaseAloneText =
            "离线预览日志只有一张快照和两条状态日志。\n\n" +
            "快照：椅子 4 把，状态 4。\n" +
            "日志 A：椅子 5 把，状态 4。\n" +
            "日志 B：椅子 5 把，状态 5。\n\n" +
            "先把两条日志按真实先后点出来，再回放这一段。";

        private const string CaseDoneText =
            "【写入前的暂停帧】23:30:00 椅子 5 把，插线板左侧有纸团，名单第五格是 05。\n" +
            "【状态变化的时间段】23:30:00 — 23:30:12。\n\n" +
            "当前玩家临时标记：本队 4 人，已归队登记中的新记录。\n" +
            "05 原待确认项：原作者林未，写入后从座位表消失。";

        private bool Together => state != null && state.viewChoice == StoryIds.Choice.ViewTogether;
        private bool Done => state != null && state.HasFlag(StoryIds.Flag.P4Complete);
        private bool DialogueRunning => DialogueService.Instance != null && DialogueService.Instance.IsRunning;

        private bool IsInventoryBlocked => DialogueRunning || SaveSlotPanel.IsOpen || ModalOpen;
        private bool ModalOpen =>
            (infoPanel != null && infoPanel.activeSelf) || (recordPanel != null && recordPanel.activeSelf) ||
            (casePanel != null && casePanel.activeSelf);

        bool IChapterInventory.IsGameplayModalOpen => ModalOpen;
        string IChapterInventory.ClockText => timeText != null ? timeText.text : string.Empty;
        void IChapterInventory.OpenRecord() => OpenRecord();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPending()
        {
            pending = null;
            pendingDevelopment = false;
        }

        /// <summary>Enters S5 from either 24H ending of S4.</summary>
        public static void Begin(GameState handoff, bool editorDevelopment = false)
        {
            if (handoff == null)
                throw new ArgumentException("S5 需要有效的 S4 交接状态：状态为空。");
            if (!handoff.Validate(out string error))
                throw new ArgumentException("S5 需要有效的 S4 交接状态：" + error);
            pending = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(handoff));
            pendingDevelopment = editorDevelopment;
            pending.chapterId = StoryIds.Chapter.Replay;
            pending.stepId = "s5Arrival";
            pending.locationId = StoryIds.Location.ClubRoom;
            pending.locationVariant = StoryIds.Variant.Day;
            pending.timeId = pending.HasFlag(StoryIds.Flag.P5Complete) ? "saturday.2329" : "saturday.1605";
            pending.EnsureCollections();
            GameFlow.LoadLocation(GameFlow.S5Scene);
        }

        private void Start()
        {
            Application.runInBackground = true;
            ApplyFont();
            CloseAllPanels();
            SceneHotspotInput.Install(this, () => state == null || IsInventoryBlocked,
                badgeButton, ruleButton, computerButton, waitButton);

            Bind("Inspect/Close", CloseInfo);
            Bind("Task record/Close", CloseRecord);

            BindButton(badgeButton, InspectBadgeBox);
            BindButton(ruleButton, InspectOldTestNote);
            BindButton(computerButton, OpenCase);
            BindButton(waitButton, WaitForLights);
            BindButton(frameOne, () => PickFrame(1));
            BindButton(frameTwo, () => PickFrame(2));
            BindButton(frameThree, () => PickFrame(3));
            BindButton(logA, () => PickLog("A"));
            BindButton(logB, () => PickLog("B"));
            BindButton(replayButton, ReplayLog);
            BindButton(caseClose, () => casePanel.SetActive(false));

            state = pending ?? SaveSlotStore.TakeLoadedState(StoryIds.Chapter.Replay);
            development = pendingDevelopment;
            pending = null;
            pendingDevelopment = false;

#if UNITY_EDITOR
            if (state == null)
            {
                string bootstrap = UnityEditor.SessionState.GetString("WWG_S5Bootstrap", "");
                if (!string.IsNullOrEmpty(bootstrap))
                {
                    UnityEditor.SessionState.EraseString("WWG_S5Bootstrap");
                    state = JsonUtility.FromJson<GameState>(bootstrap);
                    development = true;
                }
            }
            if (state == null)
            {
                state = ChapterPreviewStates.S5();
                development = true;
                UnityEngine.Debug.Log("[S5] 使用编辑器预览状态（不写玩家存档）。");
            }
#endif
            if (state == null) state = LoadSaveCheckpoint(out string loadError);
            if (state == null)
            {
                UnityEngine.Debug.LogError("[S5] 缺少有效的 S4 交接或 S5 检查点。");
                ShowInfo("无法进入复盘", "请先完成前 24H 的结尾，再进入周六下午。", false);
                return;
            }

            state.EnsureCollections();
            if (string.IsNullOrEmpty(state.routeChoice)) state.ChooseRoute(StoryIds.Choice.RouteLight);
            if (string.IsNullOrEmpty(state.viewChoice)) state.ChooseView(StoryIds.Choice.ViewTogether);
            if (string.IsNullOrEmpty(state.timeId)) state.timeId = "saturday.1605";
            state.chapterId = StoryIds.Chapter.Replay;

            DialogueService service = DialogueService.Ensure();
            if (!subscribed) { service.DialogueCompleted += HandleDialogueCompleted; subscribed = true; }

            Persist();
            UpdateView();
            if (!state.HasSeenDialogue("S5_Arrival"))
                PlayNode("S5_Arrival");
            else if (state.HasFlag(StoryIds.Flag.P4RuleSeen))
                PlayNode("S5_ReadyForLights");
        }

        private GameState LoadSaveCheckpoint(out string error)
        {
            error = null;
            if (!GameStateStore.LoadCheckpoint(out GameState saved, out error)) return null;
            if (saved.chapterId != StoryIds.Chapter.Replay) return null;
            return saved;
        }

        private void ApplyFont()
        {
            Font font = WWG.WwgFonts.Body;
            if (font == null) return;
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        private void CloseAllPanels()
        {
            if (infoPanel != null) infoPanel.SetActive(false);
            if (recordPanel != null) recordPanel.SetActive(false);
            if (casePanel != null) casePanel.SetActive(false);
        }

        private void Bind(string path, Action action)
        {
            Transform target = infoPanel.transform.parent.Find(path);
            if (target == null) { UnityEngine.Debug.LogError("[S5] 缺少 UI：" + path); return; }
            Button button = target.GetComponent<Button>();
            BindButton(button, action);
        }

        private static void BindButton(Button button, Action action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
            TitleButtonFx.Attach(button);
        }

        // ---------------------------------------------------------------- 证据与调查

        private void OpenCase()
        {
            if (state == null || IsInventoryBlocked) return;
            if (!state.HasFlag(StoryIds.Flag.P4FrameRead) && Together)
            {
                state.SetFlag(StoryIds.Flag.P4FrameRead);
                ShowInfo("四人的目击记录", DialogueDocuments.GetBody("p4.frames"), false);
                Persist();
                UpdateView();
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.P4LogRead) && !Together)
            {
                state.SetFlag(StoryIds.Flag.P4LogRead);
                ShowInfo("离线预览日志", DialogueDocuments.GetBody("p4.log"), false);
                Persist();
                UpdateView();
                return;
            }
            logFirst = null;
            logSecond = null;
            casePanel.SetActive(true);
            RefreshCase();
        }

        private void RefreshCase()
        {
            bool done = Done;
            caseTitle.text = Together ? "四人的目击记录 · 对照房间草图" : "离线预览日志 · 状态先后";
            caseBody.gameObject.SetActive(!done);
            caseRows.gameObject.SetActive(done);
            if (done)
            {
                caseBody.text = string.Empty;
                caseRows.text = CaseDoneText;
            }
            else caseBody.text = Together ? CaseTogetherText : CaseAloneText;
            LateUi.SetActive(frameOne, !done && Together);
            LateUi.SetActive(frameTwo, !done && Together);
            LateUi.SetActive(frameThree, !done && Together);
            LateUi.SetActive(logA, !done && !Together);
            LateUi.SetActive(logB, !done && !Together);
            LateUi.SetActive(replayButton, !done && !Together && logFirst != null);
            caseFeedback.text = done
                ? "两行状态已经分开：你只是这一场的新记录，05 仍是原文件名下的待确认项。"
                : "选出的证据会写进备忘录，归队时用得上。";
            caseFeedback.color = LateUi.WindowInk;
        }

        private void PickFrame(int index)
        {
            if (state == null || !Together || Done) return;
            if (index == 2)
            {
                state.SetFlag(StoryIds.Flag.P4FrameFound);
                CompleteCase();
                return;
            }
            caseFeedback.text = index == 1
                ? "帧卡 1 是灯刚暗下：椅子还在，纸团还没被压住 —— 早于写入。"
                : "帧卡 3 已经把第五把椅子抹掉了，状态变化早就结束。";
            caseFeedback.color = LateUi.WindowWarning;
        }

        private void PickLog(string id)
        {
            if (state == null || Together || Done) return;
            if (logFirst == null)
            {
                logFirst = id;
                caseFeedback.text = "已把日志 " + id + " 放在前面。再点另一条，然后回放。";
                caseFeedback.color = LateUi.WindowInk;
                LateUi.SetActive(replayButton, true);
                return;
            }
            if (logFirst == id)
            {
                caseFeedback.text = "同一条日志不能自己排在自己前面。";
                caseFeedback.color = LateUi.WindowWarning;
                return;
            }
            logSecond = id;
            caseFeedback.text = "顺序：日志 " + logFirst + " → 日志 " + logSecond + "。点「回放这一段」核对。";
            caseFeedback.color = LateUi.WindowInk;
            LateUi.SetActive(replayButton, true);
        }

        private void ReplayLog()
        {
            if (state == null || Together || Done) return;
            if (logFirst == "A" && logSecond == "B")
            {
                state.SetFlag(StoryIds.Flag.P4LogFound);
                CompleteCase();
                return;
            }
            logFirst = null;
            logSecond = null;
            RefreshCase();
            caseFeedback.text = "回放后椅子数量先少后多 —— 顺序反了。状态是「先 5 后 4」。";
            caseFeedback.color = LateUi.WindowWarning;
        }

        private void CompleteCase()
        {
            state.SetFlag(StoryIds.Flag.P4Complete);
            state.stepId = "p4Complete";
            Persist();
            UpdateView();
            RefreshCase();
            casePanel.SetActive(true);
            PlayNode("S5_P4Complete");
        }

        private void InspectBadgeBox()
        {
            if (state == null || IsInventoryBlocked) return;
            state.SetFlag(StoryIds.Flag.P4BadgeRead);
            ShowInfo("纪念盒", DialogueDocuments.GetBody("p4.badge"), false, showBadge: true);
            Persist();
            UpdateView();
        }

        private void InspectOldTestNote()
        {
            if (state == null || IsInventoryBlocked) return;
            if (!Done)
            {
                ShowInfo("未核实的归队规则", "先把桌上的四人的记录和名单核对完，再读林未留下的规则。", false);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.P4RuleSeen))
            {
                state.SetFlag(StoryIds.Flag.P4RuleSeen);
                state.stepId = "ruleKnown";
                PlayNode("S5_RuleSeen");
            }
            else ShowInfo("旧测试说明", DialogueDocuments.GetBody("p4.rule"), false);
            Persist();
            UpdateView();
        }

        private void WaitForLights()
        {
            if (state == null || IsInventoryBlocked) return;
            if (!state.HasFlag(StoryIds.Flag.P4RuleSeen))
            {
                ShowInfo("还不到熄灯", "P4 的证据还没核对完，归队规则也还没读。", false);
                return;
            }
            pendingLightsOut = true;
            if (!PlayNode("S5_ToBlackout"))
            {
                pendingLightsOut = false;
                LeaveForBlackout();
            }
        }

        private void OnDestroy()
        {
            if (!subscribed || DialogueService.Instance == null) return;
            DialogueService.Instance.DialogueCompleted -= HandleDialogueCompleted;
            subscribed = false;
        }

        private void HandleDialogueCompleted(string nodeId)
        {
            if (state == null) return;
            state.MarkDialogueSeen(nodeId);
            if (pendingLightsOut && nodeId == "S5_ToBlackout")
            {
                pendingLightsOut = false;
                LeaveForBlackout();
            }
            Persist();
            UpdateView();
        }

        private void LeaveForBlackout()
        {
            state.SetFlag(StoryIds.Flag.S5Finished);
            state.stepId = "lightsOut";
            state.timeId = "saturday.2329";
            Persist();
            S6Controller.Begin(state, development, StoryIds.Location.Corridor5F);
        }

        // ---------------------------------------------------------------- 记录与背包

        private void OpenRecord()
        {
            if (state == null || IsInventoryBlocked) return;
            string evidence = state.HasFlag(StoryIds.Flag.P4FrameFound) ? "写入前的暂停帧" :
                state.HasFlag(StoryIds.Flag.P4LogFound) ? "状态变化的时间段" : "尚未完成核对";
            recordText.text =
                "【P4 复盘】\n" +
                (Together
                    ? "四人的目击记录：三张帧卡 + 许圆的房间草图 + 名单抄件。\n"
                    : "离线预览日志：一张快照 + 两条状态日志。\n") +
                "核对结果：" + evidence + "\n" +
                "名单两行：当前玩家——本队新记录；05——原作者林未，待确认。\n\n" +
                "【纪念盒】" + (state.HasFlag(StoryIds.Flag.P4BadgeRead) ? "已核对 05 归档记录。" : "还没打开。") + "\n" +
                "【旧测试说明】" + (state.HasFlag(StoryIds.Flag.P4RuleSeen) ? "「公共灯熄灭后，按归队表验一次。」" : "还没读。") + "\n\n" +
                "【下一步】" + (state.HasFlag(StoryIds.Flag.P4RuleSeen)
                    ? "周六 23:29，公共照明即将断电。点电脑旁的「等到熄灯」。"
                    : "核对 P4 证据，再读未核实的归队规则。") + "\n\n" +
                "路线：" + state.routeChoice + "；查看方式：" + state.viewChoice;
            recordPanel.SetActive(true);
        }


        public bool HasItem(string item) => state != null && state.HasItem(item);

        public void UseInventoryItem(string item)
        {
            if (state == null || IsInventoryBlocked || !state.HasItem(item)) return;
            if (item == StoryIds.Item.TeamSheet)
                ShowInfo(S2InventoryGrid.ItemName(item), "分工表残页停在第五格被撕去的位置。按纸面线索，去偶数栋资料柜时会用到它。", false);
            else if (item == StoryIds.Item.OldProjectUsb)
                ShowInfo(S2InventoryGrid.ItemName(item), "旧工程的只读副本。若要核对备份里的归队页，需要去单数栋 3F 机房。", false);
            else
                ShowInfo(S2InventoryGrid.ItemName(item), "这是调查证据，暂时不在这次复盘里使用。", false);
        }

        public void InspectInventoryItem(string item)
        {
            if (state == null || !state.HasItem(item)) return;
            string body = item == StoryIds.Item.Photo ? "去年活动室的照片，背面标着 23:30。" :
                item == StoryIds.Item.Badge ? DialogueDocuments.GetBody("s1.badgeDetail") :
                item == StoryIds.Item.TeamSheet ? "分工表残页，第五格被撕去，只剩编号 05。" :
                item == StoryIds.Item.MachineNote ? "借用记录：单数栋 3F 机房靠窗第 3 台，周五 23:30。" :
                "旧工牌 AfterLightsOut_Backup；周六 16:00 的场景已解锁。";
            ShowInfo(S2InventoryGrid.ItemName(item), body, false, showBadge: item == StoryIds.Item.Badge);
        }

        // ---------------------------------------------------------------- 面板与存档

        private void ShowInfo(string title, string body, bool residue, bool showBadge = false)
        {
            if (infoPanel == null) return;
            infoTitle.text = (residue ? "【残影】" : "") + title;
            infoBody.text = body;
            LateUi.FitBody(infoBody);
            BadgeDetailLayout.Apply(infoPanel, infoBody, showBadge);
            infoPanel.SetActive(true);
            if (recordPanel != null) recordPanel.SetActive(false);
        }

        private void CloseInfo() { if (infoPanel != null) infoPanel.SetActive(false); }
        private void CloseRecord() { if (recordPanel != null) recordPanel.SetActive(false); }

        public SaveSlotSnapshot CaptureSaveSlot()
        {
            if (state == null || development || DialogueRunning) return null;
            return new SaveSlotSnapshot
            {
                chapter = "S5",
                payload = JsonUtility.ToJson(state),
                gameTime = timeText != null ? timeText.text : "周六 16:05",
                locationLabel = "S5 · 5F 活动室"
            };
        }

        private void Persist()
        {
            if (state == null || development) return;
            if (!GameStateStore.SaveCheckpoint(state, out string error))
                UnityEngine.Debug.LogError("[S5] 存档失败：" + error);
            PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 5);
            PlayerPrefs.Save();
        }

        bool IChapterInventory.CloseGameplayModal()
        {
            GameObject panel = infoPanel != null && infoPanel.activeSelf ? infoPanel :
                casePanel != null && casePanel.activeSelf ? casePanel : recordPanel;
            if (panel == null || !panel.activeSelf) return false;
            panel.SetActive(false);
            return true;
        }

        private void Update()
        {
            if (feedbackText != null && feedbackText.gameObject.activeSelf && !DialogueRunning)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f) feedbackText.gameObject.SetActive(false);
            }
        }

        private float feedbackTimer;

        private void UpdateView()
        {
            if (state == null) return;
            if (timeText != null) timeText.text = state.timeId == "saturday.2329" ? "周六 23:29" : "周六 16:05";
            LateUi.SetCaption(computerButton, Done ? "再核对一次" : Together ? "查看四人的记录" : "查看离线日志");
            LateUi.SetActive(waitButton, state.HasFlag(StoryIds.Flag.P4RuleSeen));
            LateUi.SetCaption(ruleButton, state.HasFlag(StoryIds.Flag.P4RuleSeen) ? "旧测试说明" : "未核实的归队规则");
            LateUi.SetActive(badgeButton, !state.HasFlag(StoryIds.Flag.P4BadgeRead));
            RefreshHud();
        }

        private bool PlayNode(string node, bool once = false)
        {
            if (string.IsNullOrEmpty(node)) return false;
            return DialogueService.Ensure().Play(node, this, once);
        }

        public void RefreshHud()
        {
            VitalsBars bars = GetComponentInChildren<VitalsBars>(true);
            if (bars == null || state == null) return;
            bars.SetSan(state.san, state.sanMax);
            bars.SetHealth(state.health, 3);
        }

        // ---------------------------------------------------------------- IDialogueContext

        public bool HasFlag(string id) => state != null && state.HasFlag(id);
        public bool HasSeenDialogue(string nodeId) => state != null && state.HasSeenDialogue(nodeId);
        public void MarkDialogueSeen(string nodeId) { if (state != null) state.MarkDialogueSeen(nodeId); }
        public int San => state != null ? state.san : 0;
        public int Health => state != null ? state.health : 0;
        public string Route => state != null ? state.routeChoice : string.Empty;
        public string View => state != null ? state.viewChoice : string.Empty;

        public void SetFlag(string id) { if (state != null && state.SetFlag(id)) Persist(); }
        public void ClearFlag(string id) { if (state != null) state.ClearFlag(id); }
        public void AddItem(string id) { if (state != null && state.AddItem(id)) Persist(); }

        public void ApplyShock(string id, int sanDelta, int healthDelta = 0)
        {
            if (state == null) return;
            if (!string.IsNullOrEmpty(id))
            {
                if (state.HasFlag("flag." + id + ".shock")) return;
                state.SetFlag("flag." + id + ".shock");
            }
            if (sanDelta != 0)
            {
                state.san = Mathf.Clamp(state.san + sanDelta, 0, state.sanMax);
                FlashFeedback(sanDelta < 0 ? "心里一紧。" : "呼吸慢了下来。");
            }
            if (healthDelta != 0) state.health = Mathf.Clamp(state.health + healthDelta, 0, 3);
            RefreshHud();
            Persist();
        }

        public void ChooseRoute(string choice) { if (state == null) return; if (state.ChooseRoute(choice)) Persist(); }
        public void ChooseView(string choice) { if (state == null) return; if (state.ChooseView(choice)) Persist(); }

        public IEnumerator ShowDocument(string documentId)
        {
            if (DialogueDocuments.TryGet(documentId, out DialogueDocuments.Entry entry))
                ShowInfo(entry.title, entry.body, entry.residue);
            else ShowInfo("资料", "找不到文档：" + documentId, false);
            while (infoPanel != null && infoPanel.activeSelf) yield return null;
        }

        public IEnumerator ShowPhoto()
        {
            ShowInfo("去年的活动室照片", DialogueDocuments.GetBody("s1.photoInfo"), false);
            while (infoPanel != null && infoPanel.activeSelf) yield return null;
        }

        public void RequestCheckpoint() { if (state != null) { Persist(); FlashFeedback("进度已记录。"); } }

        public void RequestGoTo(string locationId, string variant)
        {
            UnityEngine.Debug.LogWarning("[Dialogue] S5 不使用 go_to：" + locationId + "/" + variant);
        }

        private void FlashFeedback(string message)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            feedbackTimer = 2.2f;
        }
    }
}
