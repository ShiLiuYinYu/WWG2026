using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// S7 归队确认（周日 00:20–10:00，5F 活动室）。走廊仍黑，第五把椅子空着。
    /// P6 四步：恢复 05 的原署名 → 核实本队四人 → 按前半程证据复查 → 验证第二遍。
    /// </summary>
    public class S7Controller : MonoBehaviour, IDialogueContext, IChapterInventory
    {
        public Text timeText, feedbackText, recordText;
        public Text infoTitle, infoBody;
        public GameObject infoPanel, recordPanel, puzzlePanel;
        public Text puzzleTitle, puzzleBody, puzzleFeedback;
        public Button puzzleA, puzzleB, puzzleC, puzzleClose;
        public Button badgeButton, rosterButton, teamButton, reviewButton, verifyButton, restButton;

        private static GameState pending;
        private static bool pendingDevelopment;

        private GameState state;
        private bool development;
        private bool subscribed;
        private const string ToMorningNode = "S7_ToMorning";
        private float feedbackTimer;
        private string puzzleStep = "name";

        private bool Together => state != null && state.viewChoice == StoryIds.Choice.ViewTogether;
        private bool DialogueRunning => DialogueService.Instance != null && DialogueService.Instance.IsRunning;
        private bool Blocked => DialogueRunning || SaveSlotPanel.IsOpen || ModalOpen;
        private bool ModalOpen =>
            (infoPanel != null && infoPanel.activeSelf) || (recordPanel != null && recordPanel.activeSelf) ||
            (puzzlePanel != null && puzzlePanel.activeSelf);

        bool IChapterInventory.IsGameplayModalOpen => ModalOpen;
        string IChapterInventory.ClockText => timeText != null ? timeText.text : string.Empty;
        void IChapterInventory.OpenRecord() => OpenRecord();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPending()
        {
            pending = null;
            pendingDevelopment = false;
        }

        public static void Begin(GameState handoff, bool editorDevelopment = false)
        {
            if (handoff == null)
                throw new ArgumentException("S7 需要有效的 S6 交接状态：状态为空。");
            if (!handoff.Validate(out string error))
                throw new ArgumentException("S7 需要有效的 S6 交接状态：" + error);
            pending = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(handoff));
            pendingDevelopment = editorDevelopment;
            pending.chapterId = StoryIds.Chapter.Reunion;
            pending.locationId = StoryIds.Location.ClubRoom;
            pending.locationVariant = StoryIds.Variant.Night;
            pending.timeId = "sunday.0020";
            pending.stepId = "s7Start";
            pending.EnsureCollections();
            GameFlow.LoadLocation(GameFlow.S7Scene);
        }

        private void Start()
        {
            Application.runInBackground = true;
            ApplyFont();
            CloseAllPanels();
            SceneHotspotInput.Install(this, () => state == null || Blocked,
                badgeButton, rosterButton, teamButton, reviewButton, verifyButton, restButton);
            Bind("Inspect/Close", () => infoPanel.SetActive(false));
            Bind("Task record/Close", () => recordPanel.SetActive(false));
            BindButton(puzzleClose, () => puzzlePanel.SetActive(false));
            BindButton(badgeButton, InspectBadgeBox);
            BindButton(rosterButton, OpenRoster);
            BindButton(teamButton, ConfirmTeam);
            BindButton(reviewButton, ReviewEvidence);
            BindButton(verifyButton, VerifyAgain);
            BindButton(restButton, Rest);

            state = pending ?? SaveSlotStore.TakeLoadedState(StoryIds.Chapter.Reunion);
            development = pendingDevelopment;
            pending = null;
            pendingDevelopment = false;

#if UNITY_EDITOR
            if (state == null)
            {
                string bootstrap = UnityEditor.SessionState.GetString("WWG_S7Bootstrap", "");
                if (!string.IsNullOrEmpty(bootstrap))
                {
                    UnityEditor.SessionState.EraseString("WWG_S7Bootstrap");
                    state = JsonUtility.FromJson<GameState>(bootstrap);
                    development = true;
                }
            }
            if (state == null)
            {
                state = ChapterPreviewStates.S7();
                development = true;
            }
#endif
            if (state == null) state = LoadSaveCheckpoint(out string error);
            if (state == null)
            {
                UnityEngine.Debug.LogError("[S7] 缺少有效的 S6 交接或 S7 检查点。");
                ShowInfo("无法进入归队确认", "请先取得归队规则并回到 5F。", false);
                return;
            }

            state.EnsureCollections();
            state.chapterId = StoryIds.Chapter.Reunion;
            state.locationVariant = StoryIds.Variant.Night;
            if (string.IsNullOrEmpty(state.viewChoice)) state.ChooseView(StoryIds.Choice.ViewTogether);

            DialogueService service = DialogueService.Ensure();
            if (!subscribed) { service.DialogueCompleted += HandleDialogueCompleted; subscribed = true; }

            Persist();
            UpdateView();
            if (!state.HasSeenDialogue("S7_Arrival")) PlayNode("S7_Arrival");
        }

        private GameState LoadSaveCheckpoint(out string error)
        {
            error = null;
            if (!GameStateStore.LoadCheckpoint(out GameState saved, out error)) return null;
            return saved.chapterId == StoryIds.Chapter.Reunion ? saved : null;
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
            if (puzzlePanel != null) puzzlePanel.SetActive(false);
        }

        private void Bind(string path, Action action)
        {
            Transform target = infoPanel.transform.parent.Find(path);
            if (target == null) { UnityEngine.Debug.LogError("[S7] 缺少 UI：" + path); return; }
            Button button = target.GetComponent<Button>();
            if (button != null) BindButton(button, action);
        }

        private static void BindButton(Button button, Action action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
            TitleButtonFx.Attach(button);
        }

        // ---------------------------------------------------------------- P6 四步

        private void InspectBadgeBox()
        {
            if (state == null || Blocked) return;
            if (state.SetFlag(StoryIds.Flag.P6BadgeChecked)) Persist();
            ShowInfo("纪念盒 · 工牌记录", DialogueDocuments.GetBody("p4.badge"), false, showBadge: true);
        }

        private void OpenRoster()
        {
            if (state == null || Blocked) return;
            if (state.HasFlag(StoryIds.Flag.P6NameRestored))
            {
                ShowInfo("名单副本 · 已恢复", DialogueDocuments.GetBody("p6.confirm"), false);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.P6BadgeChecked))
            {
                ShowInfo("名单副本 · 第五格", "这里只剩编号 05。先到纪念盒核对原工牌上的姓名和编号。", false);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.P6NameVerified))
            {
                ShowInfo("名单副本 · 第五格", DialogueDocuments.GetBody("p6.roster"), false);
                state.SetFlag(StoryIds.Flag.P6NameVerified);
                Persist();
                UpdateView();
                return;
            }
            puzzleStep = state.HasFlag(StoryIds.Flag.P6NameSelected) ? "status" : "name";
            RefreshPuzzle();
            puzzlePanel.SetActive(true);
        }

        private void RefreshPuzzle()
        {
            if (puzzleStep == "name")
            {
                puzzleTitle.text = "名单副本 · 第五格写回";
                puzzleBody.text = DialogueDocuments.GetBody("p6.roster");
                SetOption(puzzleA, "姓名：林未", PickName);
                SetOption(puzzleB, "姓名：许圆", PickWrongName);
                SetOption(puzzleC, "姓名：留空，只记编号", PickWrongName);
                puzzleFeedback.text = "姓名要按工牌记录写回。";
                puzzleFeedback.color = LateUi.WindowInk;
                return;
            }
            puzzleTitle.text = "第五格状态";
            puzzleBody.text = "姓名写回之后，还需要选一个状态。";
            SetOption(puzzleA, "状态：待确认", PickStatusPending);
            SetOption(puzzleB, "状态：已归队（计入现队）", () => Reject("05 不占现队名额。"));
            SetOption(puzzleC, "状态：已移除", () => Reject("她没有离队，只是还没被确认。"));
            puzzleFeedback.text = "林未的规则：原作者留在原格，状态待确认。";
            puzzleFeedback.color = LateUi.WindowInk;
        }

        private void PickName()
        {
            state.SetFlag(StoryIds.Flag.P6NameSelected);
            Persist();
            puzzleStep = "status";
            RefreshPuzzle();
        }

        private void PickWrongName()
        {
            puzzleFeedback.text = "工牌记录里 05 是林未。写别人会把原署名再改错一次。";
            puzzleFeedback.color = LateUi.WindowWarning;
        }

        private void PickStatusPending()
        {
            state.SetFlag(StoryIds.Flag.P6NameRestored);
            state.stepId = "nameRestored";
            puzzleStep = "name";
            Persist();
            puzzlePanel.SetActive(false);
            UpdateView();
            PlayNode("S7_NameRestored");
        }

        private void Reject(string why)
        {
            puzzleFeedback.text = why;
            puzzleFeedback.color = LateUi.WindowWarning;
        }

        private void ConfirmTeam()
        {
            if (state == null || Blocked) return;
            if (!StoryIds.HasReunionRule(state))
            {
                ShowInfo("我已归队", "还没拿到归队规则，不能先记自己。", false);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.P6NameRestored))
            {
                ShowInfo("我已归队", "先把 05 的原署名写回去，再记本队。", false);
                return;
            }
            if (state.HasFlag(StoryIds.Flag.P6TeamConfirmed))
            {
                ShowInfo("本队 4/4", "袁祁、许圆、唐澈和你都在。05 单列，仍是待确认。", false);
                return;
            }
            state.SetFlag(StoryIds.Flag.P6TeamConfirmed);
            state.stepId = "teamConfirmed";
            Persist();
            UpdateView();
            PlayNode("S7_TeamConfirmed");
        }

        private void ReviewEvidence()
        {
            if (state == null || Blocked) return;
            if (!state.HasFlag(StoryIds.Flag.P6TeamConfirmed))
            {
                ShowInfo("复查", "先记下本队，再按前半程的证据复查。", false);
                return;
            }
            if (state.HasFlag(StoryIds.Flag.P6ReviewDone))
            {
                ShowInfo("复查完成", Together
                    ? "暂停帧里 05 和主角两行已经分开。"
                    : "日志游标停在第二次状态变化，两行都稳定。", false);
                return;
            }
            puzzleStep = "review";
            RefreshReview();
            puzzlePanel.SetActive(true);
        }

        private void RefreshReview()
        {
            puzzleTitle.text = Together ? "暂停帧 · 与队友对照" : "回放日志 · 游标逐帧";
            puzzleFeedback.color = LateUi.WindowInk;
            if (!Together && !state.HasFlag(StoryIds.Flag.P6ReviewAtSecondEvent))
            {
                puzzleBody.text = "两条日志都记录了状态变化。把游标移到 P4 核对过的那一刻，再查看两个槽位。";
                SetOption(puzzleA, "第一次变化 · 标记缺席", ReviewFirstEvent);
                SetOption(puzzleB, "第二次变化 · 队友靠近", ReviewSecondEvent);
                SetOption(puzzleC, null, null);
                puzzleFeedback.text = "复盘里两行同时变化发生在哪一次？";
                return;
            }

            bool authorChecked = state.HasFlag(StoryIds.Flag.P6ReviewAuthor);
            bool playerChecked = state.HasFlag(StoryIds.Flag.P6ReviewPlayer);
            puzzleBody.text = (Together ? "袁祁对抄件，许圆看空椅，逐行核对暂停帧。" : "游标已停在第二次变化，逐行检查前后状态。") +
                "\n\n05 林未 · 原作者 · 待确认" + (authorChecked ? "（已核对）" : "") +
                "\n主角 · 本队成员 · 已归队" + (playerChecked ? "（已核对）" : "");
            SetOption(puzzleA, "核对 05 这一行", () => CheckReviewRow(StoryIds.Flag.P6ReviewAuthor));
            SetOption(puzzleB, "核对主角这一行", () => CheckReviewRow(StoryIds.Flag.P6ReviewPlayer));
            SetOption(puzzleC, authorChecked && playerChecked ? "播放到结算" : null, CompleteReview);
            puzzleFeedback.text = authorChecked && playerChecked ? "两行状态已核对，可以播放到结算。" : "分别核对两行的姓名和状态。";
        }

        private void ReviewFirstEvent()
        {
            puzzleFeedback.text = "这条只标记了主角缺席。两行同时变化发生在队友靠近以后。";
            puzzleFeedback.color = LateUi.WindowWarning;
        }

        private void ReviewSecondEvent()
        {
            state.SetFlag(StoryIds.Flag.P6ReviewAtSecondEvent);
            Persist();
            RefreshReview();
        }

        private void CheckReviewRow(string flag)
        {
            if (state.SetFlag(flag)) Persist();
            RefreshReview();
        }

        private void CompleteReview()
        {
            state.SetFlag(StoryIds.Flag.P6ReviewDone);
            state.stepId = "reviewDone";
            Persist();
            puzzlePanel.SetActive(false);
            UpdateView();
            PlayNode("S7_ReviewDone");
        }

        private void VerifyAgain()
        {
            if (state == null || Blocked) return;
            if (!state.HasFlag(StoryIds.Flag.P6ReviewDone))
            {
                ShowInfo("再验一次", "先把复查做完。", false);
                return;
            }
            if (state.HasFlag(StoryIds.Flag.P6Verified))
            {
                ShowInfo("验证完成", "两行状态稳定。第五张椅仍然是空的。", false);
                return;
            }
            state.SetFlag(StoryIds.Flag.P6Verified);
            state.SetFlag(StoryIds.Flag.P6Complete);
            state.stepId = "p6Complete";
            Persist();
            UpdateView();
            PlayNode("S7_Verified");
        }

        private void Rest()
        {
            if (state == null || Blocked) return;
            if (!state.HasFlag(StoryIds.Flag.P6Complete))
            {
                ShowInfo("休息", "归队确认还没验证第二遍。", false);
                return;
            }
            if (!PlayNode(ToMorningNode, true)) EnterMorning();
        }

        private void EnterMorning()
        {
            state.SetFlag(StoryIds.Flag.S7Finished);
            state.stepId = "morning";
            state.timeId = "sunday.1000";
            state.locationVariant = StoryIds.Variant.Day;
            Persist();
            S8Controller.Begin(state, development);
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
            if (nodeId == ToMorningNode)
            {
                EnterMorning();
                return;
            }
            Persist();
            UpdateView();
        }

        // ---------------------------------------------------------------- 记录与背包

        private void OpenRecord()
        {
            if (state == null || Blocked) return;
            recordText.text =
                "【P6 归队确认】\n" +
                "1 恢复原署名：" + (state.HasFlag(StoryIds.Flag.P6NameRestored) ? "05 = 林未，待确认" : "未完成") + "\n" +
                "2 核实本队：" + (state.HasFlag(StoryIds.Flag.P6TeamConfirmed) ? "本队 4/4，05 单列" : "未完成") + "\n" +
                "3 复查证据：" + (state.HasFlag(StoryIds.Flag.P6ReviewDone) ? "已完成" : "未完成") + "\n" +
                "4 验证第二遍：" + (state.HasFlag(StoryIds.Flag.P6Verified) ? "两行稳定，椅脚轻响" : "未完成") + "\n\n" +
                "【归队规则】" + (StoryIds.HasReunionRule(state)
                    ? "原作者留在原格。现队到齐后各记各的，空格可保留为待确认。"
                    : "缺失") + "\n\n" +
                "【下一步】" + (state.HasFlag(StoryIds.Flag.P6Complete)
                    ? "把确认页存进备忘录，休息到周日上午。"
                    : "按 1→4 的顺序做完，再点「再验一次」。");
            recordPanel.SetActive(true);
        }

        public bool HasItem(string item) => state != null && state.HasItem(item);

        public void UseInventoryItem(string item)
        {
            if (state == null || Blocked) return;
            ShowInfo(S2InventoryGrid.ItemName(item), "现在用不到这件东西。", false);
        }

        public void InspectInventoryItem(string item)
        {
            if (state == null || !state.HasItem(item)) return;
            string body = item == StoryIds.Item.Badge ? DialogueDocuments.GetBody("s1.badgeDetail") :
                item == StoryIds.Item.OldProjectUsb ? "旧工程只读副本。原始文件仍在这里，改动只写本机备份。" :
                item == StoryIds.Item.TeamSheet ? "分工表残页，第五格被撕去，只剩编号 05。" :
                "这次确认用不上。";
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

        public SaveSlotSnapshot CaptureSaveSlot()
        {
            if (state == null || development || DialogueRunning) return null;
            return new SaveSlotSnapshot
            {
                chapter = "S7",
                payload = JsonUtility.ToJson(state),
                gameTime = "周日 00:20",
                locationLabel = "S7 · 5F 活动室"
            };
        }

        private void Persist()
        {
            if (state == null || development) return;
            if (!GameStateStore.SaveCheckpoint(state, out string error))
                UnityEngine.Debug.LogError("[S7] 存档失败：" + error);
            PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 7);
            PlayerPrefs.Save();
        }

        bool IChapterInventory.CloseGameplayModal()
        {
            GameObject panel = infoPanel != null && infoPanel.activeSelf ? infoPanel :
                puzzlePanel != null && puzzlePanel.activeSelf ? puzzlePanel : recordPanel;
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

        private void UpdateView()
        {
            if (state == null) return;
            if (timeText != null) timeText.text = "周日 00:20";

            LateUi.SetActive(teamButton, StoryIds.HasReunionRule(state) && !state.HasFlag(StoryIds.Flag.P6TeamConfirmed));
            bool reviewing = state.HasFlag(StoryIds.Flag.P6TeamConfirmed) && !state.HasFlag(StoryIds.Flag.P6ReviewDone);
            bool verifying = state.HasFlag(StoryIds.Flag.P6ReviewDone) && !state.HasFlag(StoryIds.Flag.P6Verified);
            LateUi.SetActive(reviewButton, reviewing);
            LateUi.SetActive(verifyButton, verifying);
            LateUi.SetActive(rosterButton, !reviewing && !verifying);
            LateUi.SetActive(restButton, state.HasFlag(StoryIds.Flag.P6Complete));
            LateUi.SetCaption(reviewButton, Together ? "与队友对照" : "回放日志片段");
            RefreshHud();
        }

        private bool PlayNode(string node, bool once = false)
        {
            if (string.IsNullOrEmpty(node)) return false;
            return DialogueService.Ensure().Play(node, this, once);
        }

        private void FlashFeedback(string message)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            feedbackTimer = 2.4f;
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

        public void ChooseRoute(string choice) { if (state != null && state.ChooseRoute(choice)) Persist(); }
        public void ChooseView(string choice) { if (state != null && state.ChooseView(choice)) Persist(); }

        public IEnumerator ShowDocument(string documentId)
        {
            if (DialogueDocuments.TryGet(documentId, out DialogueDocuments.Entry entry)) ShowInfo(entry.title, entry.body, entry.residue);
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
            UnityEngine.Debug.LogWarning("[Dialogue] S7 不使用 go_to：" + locationId + "/" + variant);
        }

        private void SetOption(Button button, string caption, Action action)
        {
            if (button == null) return;
            if (caption == null) { LateUi.SetActive(button, false); return; }
            LateUi.SetActive(button, true);
            LateUi.SetCaption(button, caption);
            BindButton(button, action);
        }
    }
}
