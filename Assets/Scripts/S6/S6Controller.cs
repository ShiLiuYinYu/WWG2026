using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// S6 第二次熄灯（周六 23:30 – 周日 00:20）。一个控制器管五个地点：
    /// 5F 走廊 → 单数栋 4F 入口 → 连廊中段 → 路线专属（偶数栋资料柜 / 单数栋 3F 机房）→ 回 5F。
    /// 两条路线都只在第二次熄灯后取得「归队规则」，S7 才能核对 05。
    /// </summary>
    public class S6Controller : MonoBehaviour, IDialogueContext, IChapterInventory
    {
        public Text timeText, feedbackText, recordText;
        public Text infoTitle, infoBody;
        public GameObject infoPanel, recordPanel, puzzlePanel;
        public Text puzzleTitle, puzzleBody, puzzleFeedback;
        public Button puzzleA, puzzleB, puzzleC, puzzleClose;

        public GameObject bridgeLightLayer;
        public Image emergencySwitchArt;
        public Graphic emergencySwitchOutline;
        public Sprite emergencySwitchOn;
        public Button navA, navB, navC;
        public Button emergencySwitch, guideBoard, glass, cabinet, serverMachine;

        private static GameState pending;
        private static bool pendingDevelopment;

        private GameState state;
        private bool development;
        private bool subscribed;
        private float feedbackTimer;
        private float dangerSeconds, zeroSanSeconds;
        private Sprite emergencySwitchOff;
        private bool EmergencyLit => state.HasFlag(StoryIds.Flag.P2EmergencyLightOn);

        private string puzzleStep = "root";

        private bool RouteDark => state != null && state.routeChoice == StoryIds.Choice.RouteDark;
        private bool DialogueRunning => DialogueService.Instance != null && DialogueService.Instance.IsRunning;
        private bool ModalOpen => (infoPanel != null && infoPanel.activeSelf) || (recordPanel != null && recordPanel.activeSelf) ||
            (puzzlePanel != null && puzzlePanel.activeSelf);
        private bool Blocked => DialogueRunning || SaveSlotPanel.IsOpen || ModalOpen;

        bool IChapterInventory.IsGameplayModalOpen => ModalOpen;
        string IChapterInventory.ClockText => timeText != null ? timeText.text : string.Empty;
        void IChapterInventory.OpenRecord() => OpenRecord();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPending()
        {
            pending = null;
            pendingDevelopment = false;
        }

        /// <summary>Called by S5 when the public lights go out.</summary>
        public static void Begin(GameState handoff, bool editorDevelopment = false, string locationId = null)
        {
            if (handoff == null)
                throw new ArgumentException("S6 需要有效的 S5 交接状态：状态为空。");
            if (!handoff.Validate(out string error))
                throw new ArgumentException("S6 需要有效的 S5 交接状态：" + error);
            pending = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(handoff));
            pendingDevelopment = editorDevelopment;
            pending.chapterId = StoryIds.Chapter.SecondBlackout;
            pending.timeId = "saturday.2330";
            pending.locationVariant = StoryIds.Variant.Night;
            pending.locationId = string.IsNullOrEmpty(locationId) ? StoryIds.Location.Corridor5F : locationId;
            pending.stepId = "s6Arrival";
            pending.EnsureCollections();
            pending.ClearFlag(StoryIds.Flag.P2EmergencyLightOn);
            GameFlow.LoadLocation(GameFlow.S6Location(pending.locationId));
        }

        /// <summary>Moves inside S6 without leaving the chapter.</summary>
        private void Navigate(string locationId)
        {
            if (state == null || locationId == state.locationId) return;
            state.locationId = locationId;
            state.locationVariant = StoryIds.Variant.Night;
            Persist();
            pending = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
            pendingDevelopment = development;
            GameFlow.LoadLocation(GameFlow.S6Location(locationId));
        }

        private void Start()
        {
            Application.runInBackground = true;
            if (emergencySwitchArt != null) emergencySwitchOff = emergencySwitchArt.sprite;
            ApplyFont();
            CloseAllPanels();
            Bind("Inspect/Close", () => infoPanel.SetActive(false));
            Bind("Task record/Close", () => recordPanel.SetActive(false));
            BindButton(puzzleClose, () => puzzlePanel.SetActive(false));
            BindButton(emergencySwitch, ToggleEmergencyLight);
            BindButton(guideBoard, ReadGuideBoard);
            BindButton(glass, LookAtGlass);
            BindButton(cabinet, OpenCabinet);
            BindButton(serverMachine, () =>
            {
                if (!Blocked) ShowInfo("靠窗第 3 台", "从背包使用旧工程 U 盘，打开只读测试时间轴。", false);
            });

            state = pending ?? SaveSlotStore.TakeLoadedState(StoryIds.Chapter.SecondBlackout);
            development = pendingDevelopment;
            pending = null;
            pendingDevelopment = false;

#if UNITY_EDITOR
            if (state == null)
            {
                string bootstrap = UnityEditor.SessionState.GetString("WWG_S6Bootstrap", "");
                if (!string.IsNullOrEmpty(bootstrap))
                {
                    UnityEditor.SessionState.EraseString("WWG_S6Bootstrap");
                    state = JsonUtility.FromJson<GameState>(bootstrap);
                    development = true;
                }
            }
            if (state == null)
            {
                state = ChapterPreviewStates.S6(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                development = true;
            }
#endif
            if (state == null) state = LoadSaveCheckpoint(out string error);
            if (state == null)
            {
                UnityEngine.Debug.LogError("[S6] 缺少有效的 S5 交接或 S6 检查点。");
                ShowInfo("无法进入第二次熄灯", "请先完成 P4 复盘并等到熄灯。", false);
                return;
            }

            state.EnsureCollections();
            state.chapterId = StoryIds.Chapter.SecondBlackout;
            state.locationVariant = StoryIds.Variant.Night;
            if (string.IsNullOrEmpty(state.timeId) || state.timeId == "saturday.2329") state.timeId = "saturday.2330";
            if (string.IsNullOrEmpty(state.locationId)) state.locationId = LocationFromScene();

            DialogueService service = DialogueService.Ensure();
            if (!subscribed) { service.DialogueCompleted += HandleDialogueCompleted; subscribed = true; }

            Persist();
            UpdateView();
            string arrivalNode = ArrivalNode(state.locationId);
            if (!state.HasSeenDialogue(arrivalNode)) PlayNode(arrivalNode);
            else if (state.locationId == StoryIds.Location.Corridor5F && state.HasFlag(StoryIds.Flag.P5Complete) &&
                !state.HasSeenDialogue("S6_Returned")) PlayNode("S6_Returned");
        }

        private static string ArrivalNode(string location)
        {
            switch (location)
            {
                case StoryIds.Location.BridgeOdd4F: return "S6_Odd4FArrival";
                case StoryIds.Location.BridgeSpan4F: return "S6_SpanArrival";
                case StoryIds.Location.BridgeEven4F: return "S6_Even4FArrival";
                case StoryIds.Location.ServerRoom3F: return "S6_ServerArrival";
                default: return "S6_Arrival";
            }
        }

        private GameState LoadSaveCheckpoint(out string error)
        {
            error = null;
            if (!GameStateStore.LoadCheckpoint(out GameState saved, out error)) return null;
            return saved.chapterId == StoryIds.Chapter.SecondBlackout ? saved : null;
        }

        private string LocationFromScene()
        {
            switch (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
            {
                case "S6_BridgeOdd4F": return StoryIds.Location.BridgeOdd4F;
                case "S6_BridgeSpan4F": return StoryIds.Location.BridgeSpan4F;
                case "S6_BridgeEven4F": return StoryIds.Location.BridgeEven4F;
                case "S6_ServerRoom3F": return StoryIds.Location.ServerRoom3F;
                default: return StoryIds.Location.Corridor5F;
            }
        }

        private void ApplyFont()
        {
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
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
            if (target == null) { UnityEngine.Debug.LogError("[S6] 缺少 UI：" + path); return; }
            Button button = target.GetComponent<Button>();
            if (button != null) BindButton(button, action);
        }

        private static void BindButton(Button button, Action action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        // ---------------------------------------------------------------- 场景配置

        private void UpdateView()
        {
            if (state == null) return;
            string location = state.locationId;
            bool done = state.HasFlag(StoryIds.Flag.P5Complete);
            if (bridgeLightLayer != null) bridgeLightLayer.SetActive(EmergencyLit);
            if (emergencySwitchArt != null)
            {
                Sprite sprite = EmergencyLit ? emergencySwitchOn : emergencySwitchOff;
                emergencySwitchArt.sprite = sprite;
                if (emergencySwitchOutline is Image outline) outline.sprite = sprite;
            }

            if (timeText != null) timeText.text = state.HasFlag(StoryIds.Flag.P5Complete) ? "周日 00:20" : "周六 23:30";

            // Only the current location's props answer clicks.
            LateUi.SetActive(emergencySwitch, location == StoryIds.Location.BridgeOdd4F);
            LateUi.SetActive(guideBoard, location == StoryIds.Location.BridgeOdd4F);
            LateUi.SetActive(glass, location == StoryIds.Location.BridgeSpan4F);
            LateUi.SetActive(cabinet, location == StoryIds.Location.BridgeEven4F);
            LateUi.SetActive(serverMachine, location == StoryIds.Location.ServerRoom3F);

            ConfigureNav(location, done);
            RefreshHud();
        }

        private string LocationLabel(string location)
        {
            switch (location)
            {
                case StoryIds.Location.BridgeOdd4F: return "单数栋 · 4F 连廊入口";
                case StoryIds.Location.BridgeSpan4F: return "4F · 玻璃连廊中段";
                case StoryIds.Location.BridgeEven4F: return "偶数栋 · 4F 资料区";
                case StoryIds.Location.ServerRoom3F: return "单数栋 · 3F 机房";
                default: return "单数栋 · 5F 走廊";
            }
        }

        private void ConfigureNav(string location, bool done)
        {
            for (int i = 0; i < 3; i++)
            {
                Button button = i == 0 ? navA : i == 1 ? navB : navC;
                if (button == null) continue;
                LateUi.SetActive(button, false);
            }

            switch (location)
            {
                case StoryIds.Location.Corridor5F:
                    if (done) SetNav(navA, "进 5F 活动室", () => EnterReunion());
                    else SetNav(navB, "下到 4F 连廊入口", () => Navigate(StoryIds.Location.BridgeOdd4F));
                    break;
                case StoryIds.Location.BridgeOdd4F:
                    SetNav(navA, "上 5F 走廊", () => Navigate(StoryIds.Location.Corridor5F));
                    SetNav(navB, "走进玻璃连廊", () => Navigate(StoryIds.Location.BridgeSpan4F));
                    if (RouteDark) SetNav(navC, "下 3F 机房", () => Navigate(StoryIds.Location.ServerRoom3F));
                    break;
                case StoryIds.Location.BridgeSpan4F:
                    SetNav(navA, "回 4F 入口", () => Navigate(StoryIds.Location.BridgeOdd4F));
                    if (!RouteDark) SetNav(navB, "往偶数栋", () => Navigate(StoryIds.Location.BridgeEven4F));
                    break;
                case StoryIds.Location.BridgeEven4F:
                    SetNav(navA, "回连廊中段", () => Navigate(StoryIds.Location.BridgeSpan4F));
                    SetNav(navB, "回 5F 活动室", () => Navigate(StoryIds.Location.Corridor5F));
                    break;
                case StoryIds.Location.ServerRoom3F:
                    SetNav(navA, "上楼回 4F 入口", () => Navigate(StoryIds.Location.BridgeOdd4F));
                    SetNav(navB, "回 5F 活动室", () => Navigate(StoryIds.Location.Corridor5F));
                    break;
            }
        }

        private void SetNav(Button button, string caption, Action action)
        {
            if (button == null) return;
            var hover = button.GetComponent<NavigationArrowHover>();
            if (hover != null) hover.SetDestination(caption);
            else LateUi.SetCaption(button, caption);
            BindButton(button, () =>
            {
                if (!Blocked) action();
            });
            LateUi.SetActive(button, true);
        }

        private void EnterReunion()
        {
            if (!StoryIds.HasReunionRule(state))
            {
                FlashFeedback("还差归队规则。先去对应的取证地点核对。");
                return;
            }
            state.SetFlag(StoryIds.Flag.S6Finished);
            state.stepId = "s6Complete";
            state.timeId = "sunday.0020";
            Persist();
            S7Controller.Begin(state, development);
        }

        // ---------------------------------------------------------------- 取证

        private void ToggleEmergencyLight()
        {
            if (state == null || Blocked) return;
            if (state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
            {
                state.ClearFlag(StoryIds.Flag.P2EmergencyLightOn);
                FlashFeedback("应急灯关了。走廊重新黑下来。");
            }
            else
            {
                state.SetFlag(StoryIds.Flag.P2EmergencyLightOn);
                FlashFeedback("应急灯亮起，导览展板边缘的旧贴纸被照亮。");
            }
            Persist();
            UpdateView();
        }

        private void ReadGuideBoard()
        {
            if (state == null || Blocked) return;
            if (!state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
            {
                ShowInfo("导览展板", "走廊全黑，展板看不清。先打开应急灯。", false);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.P5LightRead))
            {
                state.SetFlag(StoryIds.Flag.P5LightRead);
                Persist();
            }
            ShowInfo("导览展板 · 旧展位号", DialogueDocuments.GetBody("p5.guide"), false);
            UpdateView();
        }

        private void LookAtGlass()
        {
            if (state == null || Blocked) return;
            if (!RouteDark)
            {
                ShowInfo("玻璃连廊中段", "玻璃映出连廊和队友。许圆记下的旧展位号指向偶数栋资料柜。", false);
                return;
            }
            if (!state.HasFlag(StoryIds.Flag.P2EmergencyLightOn))
            {
                state.SetFlag(StoryIds.Flag.P5GlassMark);
                ShowInfo("残影 · 连廊中段玻璃", DialogueDocuments.GetBody("p5.glass"), true);
                ApplyShock("s6.glass", -15);
                Persist();
                UpdateView();
                return;
            }
            ShowInfo("玻璃连廊中段", "应急灯亮着，倒影散了。要去单数栋 3F 机房核对备份。", false);
        }

        private void OpenCabinet()
        {
            if (state == null || Blocked) return;
            puzzleStep = "root";
            RefreshPuzzle();
            puzzlePanel.SetActive(true);
        }

        private void OpenTimeline()
        {
            if (state == null || Blocked) return;
            if (!state.HasItem(StoryIds.Item.OldProjectUsb))
            {
                ShowInfo("靠窗第 3 台", "机器要旧工程 U 盘才能进入只读测试时间轴。", false);
                return;
            }
            puzzleStep = "root";
            RefreshPuzzle();
            puzzlePanel.SetActive(true);
        }

        // ---------------------------------------------------------------- 解谜面板

        private void RefreshPuzzle()
        {
            bool even = state.locationId == StoryIds.Location.BridgeEven4F;
            string body;
            if (even) body = EvenCabinetText();
            else body = TimelineText();

            puzzleBody.text = body;
            puzzleFeedback.color = LateUi.WindowInk;

            if (even)
            {
                if (puzzleStep == "root")
                {
                    puzzleTitle.text = "偶数栋 4F · 旧工程资料柜";
                    SetOption(puzzleA, "查看柜门内侧（折页）", OpenPageFold);
                    SetOption(puzzleB, null, null);
                    SetOption(puzzleC, null, null);
                    if (!state.HasFlag(StoryIds.Flag.P5SheetPlaced))
                        puzzleFeedback.text = "折页被柜门的卡扣绷着。收起后，可以从背包用分工表残页对照。";
                }
                else
                {
                    puzzleTitle.text = "归队确认表 · 05 这一行";
                    SetOption(puzzleA, "05 —— 备注：原作者留在原格，待确认", PickRowPaper);
                    SetOption(puzzleB, "05 —— 备注：视为缺席，除名", PickRowWrong);
                    SetOption(puzzleC, "05 —— 备注：由现队成员顶替", PickRowWrong);
                    puzzleFeedback.text = "三栏都在同一张折页上，按林未写下的规则选。";
                }
            }
            else
            {
                if (puzzleStep == "root")
                {
                    puzzleTitle.text = "只读测试时间轴 · 三张事件卡";
                    SetOption(puzzleA, "① 门响 / 离队", () => PickCard(1));
                    SetOption(puzzleB, "② 门响 / 归队", () => PickCard(2));
                    SetOption(puzzleC, "③ 椅脚声 / 结算", () => PickCard(3));
                    puzzleFeedback.text = "只读时间轴不会改动原始文件。先选一张能对上夜里的卡。";
                }
                else
                {
                    puzzleTitle.text = "归队页 · 核对两栏";
                    SetOption(puzzleA, "归队人数：4（现队）+ 1（05，待确认）", VerifyBackup);
                    SetOption(puzzleB, "归队人数：5（含 05 计入现队）", () => RejectBackup("05 不占现队名额，她留在原格。"));
                    SetOption(puzzleC, "归队人数：4（05 视为缺席）", () => RejectBackup("05 不是缺席，是待确认。"));
                    puzzleFeedback.text = "归队页要求核对人数与 05 的状态。";
                }
            }
        }

        private string EvenCabinetText()
        {
            if (!state.HasFlag(StoryIds.Flag.P5SheetPlaced))
                return "柜门沿用前一晚的开锁状态，一拉就开。内侧压着一张折页，是当年的归队确认表。\n\n" +
                       "折页只露出上半截，下面被卡扣绷住。";
            if (state.HasFlag(StoryIds.Flag.P5PaperRule)) return DialogueDocuments.GetBody("p5.rulePaper");
            return DialogueDocuments.GetBody("p5.sheet") + "\n\n（选 05 这一行的备注栏。）";
        }

        private string TimelineText()
        {
            if (puzzleStep == "root") return DialogueDocuments.GetBody("p5.backup");
            return DialogueDocuments.GetBody("p5.ruleBackup");
        }

        private void SetOption(Button button, string caption, Action action)
        {
            if (button == null) return;
            if (caption == null) { LateUi.SetActive(button, false); return; }
            LateUi.SetActive(button, true);
            LateUi.SetCaption(button, caption);
            BindButton(button, action);
        }

        private void OpenPageFold()
        {
            if (!state.HasFlag(StoryIds.Flag.P5SheetPlaced))
            {
                puzzleFeedback.text = "折页被卡扣绷着，硬掀会把纸撕坏。找件薄的东西压住。";
                puzzleFeedback.color = LateUi.WindowWarning;
                return;
            }
            puzzleStep = "rows";
            RefreshPuzzle();
        }

        private void PlaceTeamSheet()
        {
            if (!state.HasItem(StoryIds.Item.TeamSheet))
            {
                puzzleFeedback.text = "背包里没有能压住折页的薄纸。";
                puzzleFeedback.color = LateUi.WindowWarning;
                return;
            }
            state.SetFlag(StoryIds.Flag.P5SheetPlaced);
            puzzleFeedback.text = "分工表残页卡进卡扣，折页展开，露出归队确认表。";
            puzzleFeedback.color = LateUi.WindowInk;
            Persist();
            RefreshPuzzle();
        }

        private void PickRowPaper()
        {
            state.SetFlag(StoryIds.Flag.P5PaperRule);
            state.SetFlag(StoryIds.Flag.P5Complete);
            state.timeId = "sunday.0020";
            state.stepId = "p5Paper";
            Persist();
            UpdateView();
            RefreshPuzzle();
            puzzleFeedback.text = "许圆把这一栏抄进备忘录。05 留在原格，现队各记各的。";
            puzzleFeedback.color = LateUi.WindowInk;
            state.locationId = StoryIds.Location.BridgeEven4F;
            PlayNode("S6_RuleRecorded");
        }

        private void PickRowWrong()
        {
            puzzleFeedback.text = "这一栏不是林未写的那句。她的规则是「原作者留在原格」。";
            puzzleFeedback.color = LateUi.WindowWarning;
        }

        private void PickCard(int index)
        {
            if (index != 2)
            {
                puzzleFeedback.text = index == 1
                    ? "这张是门响之后的离队记录，早于归队。"
                    : "这张是结算，已经过了归队那一段。";
                puzzleFeedback.color = LateUi.WindowWarning;
                return;
            }
            puzzleStep = "rule";
            RefreshPuzzle();
            puzzleFeedback.text = "门响之后 05 回到原格。时间轴翻到归队页。";
            puzzleFeedback.color = LateUi.WindowInk;
        }

        private void VerifyBackup()
        {
            state.SetFlag(StoryIds.Flag.P5BackupRule);
            state.SetFlag(StoryIds.Flag.P5Complete);
            state.timeId = "sunday.0020";
            state.stepId = "p5Backup";
            Persist();
            UpdateView();
            RefreshPuzzle();
            puzzleFeedback.text = "归队页对上了：现队 4 人，05 单列待确认。规则已存进备忘录。";
            puzzleFeedback.color = LateUi.WindowInk;
            PlayNode("S6_RuleRecorded");
        }

        private void RejectBackup(string why)
        {
            puzzleFeedback.text = why;
            puzzleFeedback.color = LateUi.WindowWarning;
        }

        // ---------------------------------------------------------------- 记录与背包

        private void OpenRecord()
        {
            if (state == null || Blocked) return;
            recordText.text =
                "【S6 第二次熄灯】\n" +
                "路线：" + state.routeChoice + "\n" +
                "位置：" + LocationLabel(state.locationId) + "\n\n" +
                "【已取得】\n" +
                (state.HasFlag(StoryIds.Flag.P5LightRead) ? "· 导览展板的旧展位号 4F-02\n" : "") +
                (state.HasFlag(StoryIds.Flag.P5GlassMark) ? "· 玻璃倒影：第二次门响，选归队页\n" : "") +
                (state.HasFlag(StoryIds.Flag.P5PaperRule) ? "· 纸质归队规则（许圆抄下）\n" : "") +
                (state.HasFlag(StoryIds.Flag.P5BackupRule) ? "· 备份中的归队规则（时间轴归队页）\n" : "") +
                "\n【归队规则】" + (StoryIds.HasReunionRule(state)
                    ? "原作者留在原格。现队到齐后各记各的，空格可保留为待确认。"
                    : "还没拿到。") + "\n\n" +
                "【下一步】" + (StoryIds.HasReunionRule(state)
                    ? "回 5F 活动室，做归队确认。"
                    : RouteDark ? "3F 机房靠窗第 3 台，用旧工程 U 盘核对归队页。" : "偶数栋 4F 资料柜，打开柜门内侧折页。");
            recordPanel.SetActive(true);
        }

        public bool HasItem(string item) => state != null && state.HasItem(item);

        public void UseInventoryItem(string item)
        {
            if (state == null || Blocked || !state.HasItem(item)) return;
            if (item == StoryIds.Item.TeamSheet && state.locationId == StoryIds.Location.BridgeEven4F)
            {
                PlaceTeamSheet();
                puzzlePanel.SetActive(true);
                return;
            }
            if (item == StoryIds.Item.OldProjectUsb && state.locationId == StoryIds.Location.ServerRoom3F)
            {
                OpenTimeline();
                return;
            }
            ShowInfo(S2InventoryGrid.ItemName(item), "现在用不到这件东西。", false);
        }

        public void InspectInventoryItem(string item)
        {
            if (state == null || !state.HasItem(item)) return;
            string body = item == StoryIds.Item.TeamSheet ? "分工表残页，第五格被撕去，只剩编号 05。" :
                item == StoryIds.Item.OldProjectUsb ? "旧工程只读副本，可在 3F 机房核对测试时间轴。" :
                item == StoryIds.Item.Badge ? DialogueDocuments.GetBody("s1.badgeDetail") :
                "这次取证用不上。";
            ShowInfo(S2InventoryGrid.ItemName(item), body, false, showBadge: item == StoryIds.Item.Badge);
        }

        // ---------------------------------------------------------------- 面板与存档

        private void ShowInfo(string title, string body, bool residue, bool showBadge = false)
        {
            if (infoPanel == null) return;
            infoTitle.text = (residue ? "【残影】" : "") + title;
            infoBody.text = body;
            Transform badge = infoPanel.transform.Find("Badge artwork");
            if (badge != null) badge.gameObject.SetActive(showBadge);
            infoPanel.SetActive(true);
            if (recordPanel != null) recordPanel.SetActive(false);
        }

        public SaveSlotSnapshot CaptureSaveSlot()
        {
            if (state == null || development || DialogueRunning) return null;
            return new SaveSlotSnapshot
            {
                chapter = "S6",
                payload = JsonUtility.ToJson(state),
                gameTime = state.HasFlag(StoryIds.Flag.P5Complete) ? "周日 00:20" : "周六 23:30",
                locationLabel = "S6 · " + LocationLabel(state.locationId)
            };
        }

        private void Persist()
        {
            if (state == null || development) return;
            if (!GameStateStore.SaveCheckpoint(state, out string error))
                UnityEngine.Debug.LogError("[S6] 存档失败：" + error);
            PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 6);
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
            TickNight();
            if (feedbackText != null && feedbackText.gameObject.activeSelf && !DialogueRunning)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f) feedbackText.gameObject.SetActive(false);
            }
        }

        private void TickNight()
        {
            if (state == null || Blocked || GameplayOverlay.IsInventoryOpen || Time.deltaTime == 0f) return;
            string location = state.locationId;
            bool bridge = location == StoryIds.Location.BridgeOdd4F || location == StoryIds.Location.BridgeSpan4F ||
                location == StoryIds.Location.BridgeEven4F;
            bool dark = location == StoryIds.Location.Corridor5F || bridge && !EmergencyLit;
            if (!dark) return;

            dangerSeconds += Time.deltaTime;
            if (dangerSeconds >= 20f)
            {
                dangerSeconds -= 20f;
                state.san = Mathf.Max(0, state.san - 1);
                RefreshHud();
            }
            if (state.san > 0) return;

            zeroSanSeconds += Time.deltaTime;
            if (zeroSanSeconds < 15f) return;
            zeroSanSeconds -= 15f;
            state.health = Mathf.Max(0, state.health - 1);
            if (state.health == 0)
            {
                state.health = 3;
                state.san = Mathf.Min(60, state.sanMax);
                if (location != StoryIds.Location.Corridor5F)
                    Navigate(StoryIds.Location.Corridor5F);
                else
                {
                    Persist();
                    ShowInfo("5F · 活动室门口", "队友扶你回到活动室门口。缓过气后，你可以继续核对归队规则。", false);
                }
            }
            RefreshHud();
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
            if (nodeId == "S6_RuleRecorded") state.stepId = "ruleRecorded";
            Persist();
            UpdateView();
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
                FlashFeedback(sanDelta < 0 ? "残影让呼吸乱了一拍。" : "呼吸慢了下来。");
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
            if (StoryIds.IsLocation(locationId)) Navigate(locationId);
        }
    }
}
