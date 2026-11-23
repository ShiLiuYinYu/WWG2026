using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// S8 封包（周日 10:00–16:00，5F 活动室白天，四人做完作品）。
    /// 袁祁拿出「署名更正页」与「协会内部存档」两份材料；封包前从电脑二选一：
    /// 带上更正页（EndingLetter）或封存原始材料（EndingSeal）。两种结尾都保留修正后的旧署名与今夜归队记录。
    /// </summary>
    public class S8Controller : MonoBehaviour, IDialogueContext, IChapterInventory
    {
        public Text timeText, feedbackText, recordText;
        public Text infoTitle, infoBody;
        public GameObject infoPanel, recordPanel, puzzlePanel, endingPanel;
        public Text puzzleTitle, puzzleBody, puzzleFeedback;
        public Button puzzleA, puzzleB, puzzleC, puzzleClose;
        public Button materialsButton, computerButton, memorialButton;
        public Text endingText;
        public Button endingButton;

        private static GameState pending;
        private static bool pendingDevelopment;

        private GameState state;
        private bool development;
        private bool subscribed;
        private float feedbackTimer;
        private Button infoCloseButton;
        private Action nextInfoPage;

        private bool Blocked => DialogueRunning || SaveSlotPanel.IsOpen || ModalOpen;
        private bool ModalOpen =>
            (infoPanel != null && infoPanel.activeSelf) || (recordPanel != null && recordPanel.activeSelf) ||
            (puzzlePanel != null && puzzlePanel.activeSelf) ||
            (endingPanel != null && endingPanel.activeSelf);
        private bool DialogueRunning => DialogueService.Instance != null && DialogueService.Instance.IsRunning;

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
                throw new ArgumentException("S8 需要有效的 S7 交接状态：状态为空。");
            if (!handoff.Validate(out string error))
                throw new ArgumentException("S8 需要有效的 S7 交接状态：" + error);
            pending = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(handoff));
            pendingDevelopment = editorDevelopment;
            pending.chapterId = StoryIds.Chapter.Packaging;
            pending.locationId = StoryIds.Location.ClubRoom;
            pending.locationVariant = StoryIds.Variant.Day;
            pending.timeId = "sunday.1000";
            pending.stepId = "s8Start";
            pending.EnsureCollections();
            GameFlow.LoadLocation(GameFlow.S8Scene);
        }

        private void Start()
        {
            Application.runInBackground = true;
            ApplyFont();
            CloseAllPanels();
            SceneHotspotInput.Install(this, () => state == null || Blocked,
                materialsButton, computerButton, memorialButton);
            Bind("Inspect/Close", CloseInfo);
            Transform closeView = infoPanel.transform.Find("Close");
            infoCloseButton = closeView != null ? closeView.GetComponent<Button>() : null;
            Bind("Task record/Close", () => recordPanel.SetActive(false));
            BindButton(puzzleClose, () => puzzlePanel.SetActive(false));
            BindButton(materialsButton, OpenMaterials);
            BindButton(computerButton, OpenComputer);
            BindButton(memorialButton, OpenMemorial);
            BindButton(endingButton, ReturnTitle);

            state = pending ?? SaveSlotStore.TakeLoadedState(StoryIds.Chapter.Packaging);
            development = pendingDevelopment;
            pending = null;
            pendingDevelopment = false;

#if UNITY_EDITOR
            if (state == null)
            {
                string bootstrap = UnityEditor.SessionState.GetString("WWG_S8Bootstrap", "");
                if (!string.IsNullOrEmpty(bootstrap))
                {
                    UnityEditor.SessionState.EraseString("WWG_S8Bootstrap");
                    state = JsonUtility.FromJson<GameState>(bootstrap);
                    development = true;
                }
            }
            if (state == null)
            {
                state = ChapterPreviewStates.S8();
                development = true;
            }
#endif
            if (state == null) state = LoadSaveCheckpoint(out string error);
            if (state == null)
            {
                UnityEngine.Debug.LogError("[S8] 缺少有效的 S7 交接或 S8 检查点。");
                ShowInfo("无法进入封包", "请先在归队确认里完成 P6。", false);
                return;
            }

            state.EnsureCollections();
            state.chapterId = StoryIds.Chapter.Packaging;
            state.locationVariant = StoryIds.Variant.Day;

            DialogueService service = DialogueService.Ensure();
            if (!subscribed) { service.DialogueCompleted += HandleDialogueCompleted; subscribed = true; }

            Persist();
            UpdateView();
            if (state.HasFlag(StoryIds.Flag.S8Finished)) ShowEnding(state.endingChoice);
            else if (!state.HasSeenDialogue("S8_Arrival")) PlayNode("S8_Arrival");
        }

        private GameState LoadSaveCheckpoint(out string error)
        {
            error = null;
            if (!GameStateStore.LoadCheckpoint(out GameState saved, out error)) return null;
            return saved.chapterId == StoryIds.Chapter.Packaging ? saved : null;
        }

        private void ApplyFont()
        {
            Font font = WWG.WwgFonts.Body;
            if (font == null) return;
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        private void CloseAllPanels()
        {
            nextInfoPage = null;
            if (infoPanel != null) infoPanel.SetActive(false);
            if (recordPanel != null) recordPanel.SetActive(false);
            if (puzzlePanel != null) puzzlePanel.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
        }

        private void Bind(string path, Action action)
        {
            Transform target = infoPanel.transform.parent.Find(path);
            if (target == null) { UnityEngine.Debug.LogError("[S8] 缺少 UI：" + path); return; }
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

        // ---------------------------------------------------------------- 材料与源头

        private void OpenMaterials()
        {
            if (state == null || Blocked) return;
            if (!state.HasFlag(StoryIds.Flag.S8MaterialsSeen))
            {
                state.SetFlag(StoryIds.Flag.S8MaterialsSeen);
                Persist();
                UpdateView();
            }
            ShowMaterialsPage(1);
        }

        // 两份材料分开成页：一页装 16 行正文会压出资料窗边框，分页后正文保持 34 号。
        private void ShowMaterialsPage(int page)
        {
            if (page <= 1)
            {
                ShowInfo("袁祁拿出的两份材料（1/2）", DialogueDocuments.GetBody("s8.letter"), false);
                LateUi.SetCaption(infoCloseButton, "下一页");
                nextInfoPage = () => ShowMaterialsPage(2);
                return;
            }
            ShowInfo("袁祁拿出的两份材料（2/2）", DialogueDocuments.GetBody("s8.archive"), false);
        }

        private void OpenMemorial()
        {
            if (state == null || Blocked) return;
            if (state.endingChoice != StoryIds.Choice.EndingSeal)
            {
                ShowInfo("纪念盒", "盒子里是工牌记录与纸面归队表。封存路线要把旧工程副本归进同一份档案。", false,
                    showBadge: true);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.S8ArchiveCopy))
            {
                ShowInfo("纪念盒", "还差一份留存副本。先用背包里的旧工程 U 盘生成副本。", false, showBadge: true);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.S8Archived))
            {
                state.SetFlag(StoryIds.Flag.S8Archived);
                state.stepId = "sealArchived";
                Persist();
                UpdateView();
                PlayNode("S8_Archived");
                return;
            }
            ShowInfo("纪念盒 · 已归档", "副本与纸面记录在同一份档案里。档案索引：05 林未 · 原工程署名 · 待确认。", false, showBadge: true);
        }

        // ---------------------------------------------------------------- 封包选择

        private void OpenComputer()
        {
            if (state == null || Blocked) return;
            if (!state.HasFlag(StoryIds.Flag.S8MaterialsSeen))
            {
                ShowInfo("电脑", "先和袁祁把两份材料看一遍。", false);
                return;
            }
            RefreshPuzzle();
            puzzlePanel.SetActive(true);
        }

        private void RefreshPuzzle()
        {
            if (string.IsNullOrEmpty(state.endingChoice))
            {
                puzzleTitle.text = "封包 · 选择资料去向（16:00 前）";
                puzzleBody.text =
                    "带上更正页：把已核对的姓名与来源写进本队自愿展示的说明页，附上「旧工程署名更正」，16:00 展示。\n\n" +
                    "封存原始材料：用旧工程 U 盘生成留存副本，与纸面记录归入纪念盒档案，本队作品照常封包。\n\n" +
                    "两种做法都保留修正后的旧署名与今夜归队记录。";
                SetOption(puzzleA, "带上更正页", () => ChooseEnding(StoryIds.Choice.EndingLetter));
                SetOption(puzzleB, "封存原始材料", () => ChooseEnding(StoryIds.Choice.EndingSeal));
                SetOption(puzzleC, null, null);
                puzzleFeedback.text = "先选一种，再按提示逐项确认。";
                puzzleFeedback.color = LateUi.WindowInk;
                return;
            }

            if (state.endingChoice == StoryIds.Choice.EndingLetter)
            {
                puzzleTitle.text = "带上更正页";
                puzzleBody.text = DialogueDocuments.GetBody("s8.letter");
                if (!state.HasFlag(StoryIds.Flag.S8LetterAttached))
                {
                    SetOption(puzzleA, "把「旧工程署名更正」附在本队说明页", AttachLetter);
                    SetOption(puzzleB, null, null);
                    SetOption(puzzleC, null, null);
                    puzzleFeedback.text = "姓名与来源都已核对过，附在说明页后面。";
                }
                else if (!state.HasFlag(StoryIds.Flag.S8Previewed))
                {
                    SetOption(puzzleA, "预览说明", PreviewLetter);
                    SetOption(puzzleB, null, null);
                    SetOption(puzzleC, null, null);
                    puzzleFeedback.text = "预览一遍，确认更正页排在本队作品名单之后。";
                }
                else
                {
                    SetOption(puzzleA, "确认并封包", () => Finish(StoryIds.Choice.EndingLetter));
                    SetOption(puzzleB, null, null);
                    SetOption(puzzleC, null, null);
                    puzzleFeedback.text = "16:00 前会展出。";
                }
                puzzleFeedback.color = LateUi.WindowInk;
                return;
            }

            if (state.endingChoice == StoryIds.Choice.EndingSeal)
            {
                puzzleTitle.text = "封存原始材料";
                puzzleBody.text = DialogueDocuments.GetBody("s8.archive");
                if (!state.HasFlag(StoryIds.Flag.S8ArchiveCopy))
                {
                    SetOption(puzzleA, null, null);
                    SetOption(puzzleB, null, null);
                    SetOption(puzzleC, null, null);
                    puzzleFeedback.text = "收起面板，然后从背包使用旧工程 U 盘生成留存副本。";
                }
                else if (!state.HasFlag(StoryIds.Flag.S8Archived))
                {
                    SetOption(puzzleA, null, null);
                    SetOption(puzzleB, null, null);
                    SetOption(puzzleC, null, null);
                    puzzleFeedback.text = "纪念盒在资料柜上方。收起面板后到纪念盒归档。";
                }
                else
                {
                    SetOption(puzzleA, "回电脑封包", () => Finish(StoryIds.Choice.EndingSeal));
                    SetOption(puzzleB, null, null);
                    SetOption(puzzleC, null, null);
                    puzzleFeedback.text = "封包后档案留在协会内部存档。";
                }
                puzzleFeedback.color = LateUi.WindowInk;
            }
        }

        private void ChooseEnding(string choice)
        {
            if (!state.ChooseEnding(choice)) return;
            state.stepId = choice == StoryIds.Choice.EndingLetter ? "letterChosen" : "sealChosen";
            Persist();
            UpdateView();
            RefreshPuzzle();
            PlayNode(choice == StoryIds.Choice.EndingLetter ? "S8_LetterChosen" : "S8_SealChosen");
        }

        private void AttachLetter()
        {
            state.SetFlag(StoryIds.Flag.S8LetterAttached);
            state.stepId = "letterAttached";
            Persist();
            UpdateView();
            FlashFeedback("更正页已经附在说明页后面。");
            RefreshPuzzle();
        }

        private void PreviewLetter()
        {
            state.SetFlag(StoryIds.Flag.S8Previewed);
            state.stepId = "letterPreviewed";
            Persist();
            UpdateView();
            PlayNode("S8_Preview");
            RefreshPuzzle();
        }

        private void MakeArchiveCopy()
        {
            if (!state.HasItem(StoryIds.Item.OldProjectUsb))
            {
                ShowInfo("旧工程 U 盘", "背包里没有 U 盘，无法生成本机留存副本。", false);
                return;
            }
            state.SetFlag(StoryIds.Flag.S8ArchiveCopy);
            state.stepId = "archiveCopy";
            Persist();
            UpdateView();
            FlashFeedback("留存副本已写入本机备份。");
            RefreshPuzzle();
        }

        private void Finish(string choice)
        {
            state.SetFlag(StoryIds.Flag.S8Finished);
            state.stepId = "finished";
            state.timeId = "sunday.1600";
            Persist();
            puzzlePanel.SetActive(false);
            UpdateView();
            ShowEnding(choice);
        }

        private void ShowEnding(string choice)
        {
            if (endingPanel == null || endingText == null) return;
            bool letter = choice == StoryIds.Choice.EndingLetter;
            endingText.text = letter
                ? "周日 16:00\n\n" +
                  "说明页摆在本队作品前面，紧接着是四人名单。\n" +
                  "再往后是单独一页「旧工程署名更正」：\n" +
                  "05 林未 · 原署名已恢复 · 状态待确认。\n\n" +
                  "许圆退开半步，让四个人都能看见那一行全名。\n" +
                  "今夜按归队表确认的四个人都在场；第五张椅子仍空着，但名字回到了原格。"
                : "周日 16:00\n\n" +
                  "纪念盒合上。副本与纸面记录归进同一份档案。\n" +
                  "本队作品照常封包，旧工程没有拿出这间活动室。\n\n" +
                  "档案索引：05 林未 · 原工程署名 · 待确认。\n" +
                  "今夜按归队表确认的四个人，名字都记在封包记录上。";
            endingPanel.SetActive(true);
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
            Persist();
            UpdateView();
            if (puzzlePanel.activeSelf) RefreshPuzzle();
        }

        // ---------------------------------------------------------------- 记录与背包

        private void OpenRecord()
        {
            if (state == null || Blocked) return;
            recordText.text =
                "【封包】\n" +
                "材料：" + (state.HasFlag(StoryIds.Flag.S8MaterialsSeen) ? "署名更正页 + 协会内部存档" : "未查看") + "\n" +
                "资料去向：" + (string.IsNullOrEmpty(state.endingChoice)
                    ? "未选择"
                    : state.endingChoice == StoryIds.Choice.EndingLetter ? "带上更正页（随本队说明页展示）" : "封存原始材料（协会内部存档）") + "\n" +
                "操作：" + (state.HasFlag(StoryIds.Flag.S8LetterAttached) ? "更正页已附；" : "") +
                (state.HasFlag(StoryIds.Flag.S8Previewed) ? "已预览；" : "") +
                (state.HasFlag(StoryIds.Flag.S8ArchiveCopy) ? "已生成副本；" : "") +
                (state.HasFlag(StoryIds.Flag.S8Archived) ? "已归档；" : "") + "\n\n" +
                "【05】林未 · 原工程署名 · 待确认。\n" +
                "【本队】4/4：袁祁、许圆、唐澈、你。\n" +
                "【时间】周日 16:00 前完成封包。";
            recordPanel.SetActive(true);
        }

        public bool HasItem(string item) => state != null && state.HasItem(item);

        public void UseInventoryItem(string item)
        {
            if (state == null || Blocked || !state.HasItem(item)) return;
            if (item == StoryIds.Item.OldProjectUsb && state.endingChoice == StoryIds.Choice.EndingSeal &&
                !state.HasFlag(StoryIds.Flag.S8ArchiveCopy))
            {
                MakeArchiveCopy();
                return;
            }
            ShowInfo(S2InventoryGrid.ItemName(item),
                item == StoryIds.Item.OldProjectUsb ? "里面是旧工程的只读副本，原始文件仍在。" : "现在用不到这件东西。", false);
        }

        public void InspectInventoryItem(string item)
        {
            if (state == null || !state.HasItem(item)) return;
            string body = item == StoryIds.Item.Badge ? DialogueDocuments.GetBody("s1.badgeDetail") :
                item == StoryIds.Item.TeamSheet ? "分工表残页，第五格被撕去，只剩编号 05。" :
                item == StoryIds.Item.OldProjectUsb ? "旧工程只读副本。放着它的这段时间，改动都只写本机备份。" :
                "这次封包用不上。";
            ShowInfo(S2InventoryGrid.ItemName(item), body, false, showBadge: item == StoryIds.Item.Badge);
        }

        // ---------------------------------------------------------------- 面板与存档

        private void ShowInfo(string title, string body, bool residue, bool showBadge = false)
        {
            if (infoPanel == null) return;
            nextInfoPage = null;
            LateUi.SetCaption(infoCloseButton, "继续调查");
            infoTitle.text = (residue ? "【残影】" : "") + title;
            infoBody.text = body;
            LateUi.FitBody(infoBody);
            BadgeDetailLayout.Apply(infoPanel, infoBody, showBadge);
            infoPanel.SetActive(true);
            if (recordPanel != null) recordPanel.SetActive(false);
        }

        /// <summary>「继续调查」：关闭资料窗；排了下一页时接着翻，而不是直接收起。</summary>
        private void CloseInfo()
        {
            if (infoPanel != null) infoPanel.SetActive(false);
            Action next = nextInfoPage;
            nextInfoPage = null;
            if (next != null) next();
        }

        public SaveSlotSnapshot CaptureSaveSlot()
        {
            if (state == null || development || DialogueRunning) return null;
            return new SaveSlotSnapshot
            {
                chapter = "S8",
                payload = JsonUtility.ToJson(state),
                gameTime = string.IsNullOrEmpty(state.endingChoice) ? "周日 10:00" : "周日 16:00 前",
                locationLabel = "S8 · 5F 活动室"
            };
        }

        private void Persist()
        {
            if (state == null || development) return;
            if (!GameStateStore.SaveCheckpoint(state, out string error))
                UnityEngine.Debug.LogError("[S8] 存档失败：" + error);
            PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 8);
            PlayerPrefs.Save();
        }

        private void ReturnTitle()
        {
            DialogueService.Shutdown();
            GameFlow.LoadLocation(GameFlow.TitleScene);
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
            if (timeText != null) timeText.text = "周日 10:00";
            LateUi.SetCaption(materialsButton, state.HasFlag(StoryIds.Flag.S8MaterialsSeen) ? "两份材料（已看）" : "袁祁的材料");
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
                FlashFeedback(sanDelta < 0 ? "呼吸乱了一下。" : "呼吸慢了下来。");
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
            UnityEngine.Debug.LogWarning("[Dialogue] S8 不使用 go_to：" + locationId + "/" + variant);
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
