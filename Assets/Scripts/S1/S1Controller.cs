using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using WWG;

public sealed class S1Controller : MonoBehaviour, IDialogueContext
{
    [Serializable]
    private sealed class Checkpoint
    {
        public int phase;
        public int screen;
        public int san;
        public int health;
        public bool hasPhoto;
        public bool hasBadge;
        public bool curtainClosed;
        public bool lampOn;
        public bool chairPlaced;
        public bool shockApplied;
        public bool glassRead;
        public List<string> seenDialogue;
    }

    public GameObject room;
    public GameObject corridor;
    public GameObject bridge;
    public GameObject roomHotspots;
    public GameObject corridorHotspots;
    public GameObject bridgeHotspots;
    public GameObject box;
    public GameObject boxHotspot;
    public Vector2 boxCabinetPosition = new Vector2(1611f, -224f);
    public Vector2 boxCabinetSize = new Vector2(248f, 173f);
    public GameObject chair;
    public RectTransform chairRect;
    public RectTransform chairHotspotRect;
    public RectTransform chairCabinet;
    public RectTransform chairTape;
    public RectTransform tapeTarget;
    public GameObject badgeHotspot;
    public GameObject curtainOverlay;
    public GameObject lampGlow;
    public Sprite curtainOpenSprite;
    public Sprite curtainClosedSprite;
    public Image curtainHoverImage;
    public Material curtainOpenHoverMaterial;
    public Material curtainClosedHoverMaterial;
    public RectTransform curtainHotspotRect;
    public Vector2 closedCurtainPosition = new Vector2(875, -247);
    public Vector2 closedCurtainSize = new Vector2(470, 374);
    public Vector2 openCurtainHotspotPosition = new Vector2(875, -247);
    public Vector2 openCurtainHotspotSize = new Vector2(170, 374);
    public Sprite lampOffSprite;
    public Sprite lampOnSprite;
    public GameObject infoPanel;
    public GameObject inventoryPanel;
    public GameObject photoPanel;
    public GameObject memoPanel;
    public Image photoShadow;
    public Text photoShadowHead;
    public Image photoCurtain;
    public Image photoLamp;
    public GameObject hintLampHighlight;
    public GameObject hintTapeHighlight;
    public Text infoTitle;
    public Text infoBody;
    public Text hoverText;
    public Text timeText;
    public Text objectiveText;
    public Text progressText;
    public Text statusText;
    public Text inventoryText;
    public Text photoCaption;
    public Text photoBackText;
    public Text memoText;
    public GameObject photoFace;
    public GameObject photoBack;
    public Text completionText;
    public GameObject completionPanel;

    private const string SaveKey = "WWG_S1_Checkpoint";
    private int phase; // 0 box, 1 P1, 2 badge, 3 corridor unlocked, 4 S1 complete
    private int screen; // 0 room, 1 corridor, 2 4F entrance
    private int san = 100;
    private int health = 3;
    private bool hasPhoto;
    private bool hasBadge;
    private bool curtainClosed;
    private bool lampOn;
    private bool chairPlaced;
    private bool shockApplied;
    private bool glassRead;
    private bool photoShowingBack;
    private Vector2 boxFloorPosition;
    private Vector2 boxFloorSize;
    private Vector3 boxFloorScale;
    private Vector2 defaultHoverPosition;
    private bool subscribed;
    private float idleSeconds;
    private float dangerSeconds;
    private float zeroSanSeconds;
    private int hintStage;
    private readonly HashSet<string> extraFlags = new HashSet<string>();
    private readonly HashSet<string> extraItems = new HashSet<string>();
    private readonly HashSet<string> dialogueSeen = new HashSet<string>();
    private static Checkpoint pendingTravel;
    private static string pendingArrivalNode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTravel() { pendingTravel = null; pendingArrivalNode = null; }

    private static bool DialogueRunning =>
        DialogueService.Instance != null && DialogueService.Instance.IsRunning;

    private bool ModalOpen => DialogueRunning || infoPanel.activeSelf || inventoryPanel.activeSelf ||
                              photoPanel.activeSelf || memoPanel.activeSelf || completionPanel.activeSelf;
    public bool IsGameplayModalOpen => ModalOpen;
    public bool HasPhoto => hasPhoto;
    public bool HasBadge => hasBadge;
    public bool CanMoveChair => screen == 0 && phase == 1 && !ModalOpen;
    public int San => san;
    public int Health => health;

    public bool SaveManualCheckpoint()
    {
        if (DialogueRunning) return false;
        Save();
        return true;
    }

    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (DialogueRunning) return null;
        return new SaveSlotSnapshot
        {
            chapter = "S1",
            payload = JsonUtility.ToJson(Capture()),
            gameTime = timeText.text,
            locationLabel = "S1 · " + timeText.text + " · " + GameFlow.S1Location(screen)
        };
    }

    public void UseInventoryItem(string item, string hotspot)
    {
        if (ModalOpen) return;
        if (item == "photo" && hasPhoto)
        {
            if (screen == 0 && (hotspot == "curtain" || hotspot == "lamp" || hotspot == "chair"))
            {
                OpenPhoto();
                return;
            }
            ShowInfoPanel("去年的活动室照片", DialogueDocuments.GetBody("s1.photoInfo"));
        }
        else if (item == "badge" && hasBadge)
        {
            ShowInfoPanel("林未的旧工牌", DialogueDocuments.GetBody("s1.badgeInfo"));
        }
    }

    private int Matches => (curtainClosed ? 1 : 0) + (lampOn ? 1 : 0) + (chairPlaced ? 1 : 0);

    private void Start()
    {
        defaultHoverPosition = hoverText.rectTransform.anchoredPosition;
        Application.runInBackground = true;
        if (box != null)
        {
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxFloorPosition = boxRect.anchoredPosition;
            boxFloorSize = boxRect.sizeDelta;
            boxFloorScale = boxRect.localScale;
        }
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
        if (font != null)
            foreach (Text item in GetComponentsInChildren<Text>(true)) item.font = font;

        DialogueService service = DialogueService.Ensure();
        if (service != null)
        {
            service.DialogueCompleted += HandleDialogueCompleted;
            subscribed = true;
        }

        infoPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        photoPanel.SetActive(false);
        memoPanel.SetActive(false);
        completionPanel.SetActive(false);

        Checkpoint travel = pendingTravel;
        string arrivalNode = pendingArrivalNode;
        pendingTravel = null;
        pendingArrivalNode = null;
        bool fresh = PlayerPrefs.GetInt("WWG_S1_EnterFresh", 0) == 1;
        PlayerPrefs.DeleteKey("WWG_S1_EnterFresh");
        if (travel != null)
        {
            Apply(travel);
            UpdateView();
            if (!string.IsNullOrEmpty(arrivalNode))
                PlayNode(arrivalNode);
            else if (screen == 2 && phase == 4) EnterS2();
        }
        else if (!fresh && PlayerPrefs.HasKey(SaveKey))
        {
            Restore();
            if (screen != GameFlow.S1LocationIndex(SceneManager.GetActiveScene().name)) SwitchScreen(screen);
            else if (screen == 2 && phase == 4) EnterS2();
        }
        else
        {
            screen = Mathf.Max(0, GameFlow.S1LocationIndex(SceneManager.GetActiveScene().name));
            UpdateView();
            if (screen == 0) PlayNode("S1_ClubRoom_Intro");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && DialogueService.Instance != null)
            DialogueService.Instance.DialogueCompleted -= HandleDialogueCompleted;
        subscribed = false;
    }

    private void Update()
    {
        if (ModalOpen) return;
        if (phase == 1 && screen == 0)
        {
            idleSeconds += Time.deltaTime;
            if (hintStage == 0 && idleSeconds >= 30f)
            {
                hintStage = 1;
                PlayNode("S1_ClubRoom_Hint_30", true);
            }
            else if (hintStage == 1 && idleSeconds >= 60f)
            {
                hintStage = 2;
                UpdateView();
                PlayNode("S1_ClubRoom_Hint_60", true);
            }
        }
        if (screen == 1)
        {
            dangerSeconds += Time.deltaTime;
            if (dangerSeconds >= 20f)
            {
                dangerSeconds -= 20f;
                san = Mathf.Max(0, san - 1);
                UpdateView();
            }
        }
        if (san == 0 && screen == 1)
        {
            zeroSanSeconds += Time.deltaTime;
            if (zeroSanSeconds >= 15f)
            {
                zeroSanSeconds -= 15f;
                health--;
                if (health <= 0)
                {
                    health = 3;
                    san = 60;
                    UpdateView();
                    Save();
                    SwitchScreen(0, "S1_ClubRoom_Revive");
                    return;
                }
                UpdateView();
            }
        }
    }

    public void SetHover(string label, Image arrow = null)
    {
        hoverText.text = label;
        if (arrow != null) NavigationHintLayout.Place(hoverText, arrow);
        else hoverText.rectTransform.anchoredPosition = defaultHoverPosition;
    }

    public void HandleHotspot(string action)
    {
        if (ModalOpen) return;
        switch (action)
        {
            case "box":
                if (phase != 0) return;
                PlayNode("S1_ClubRoom_Box");
                break;
            case "curtain":
                curtainClosed = !curtainClosed;
                MatchToggled();
                PlayNode("S1_ClubRoom_Curtain");
                break;
            case "lamp":
                lampOn = !lampOn;
                MatchToggled();
                PlayNode("S1_ClubRoom_Lamp");
                break;
            case "chair":
                if (phase == 2 && !hasBadge)
                {
                    PlayNode("S1_ClubRoom_Badge");
                    return;
                }
                if (!CanMoveChair) return;
                chairPlaced = !chairPlaced;
                MatchToggled();
                PlayNode("S1_ClubRoom_Chair");
                break;
            case "badge":
                if (phase != 2 || hasBadge) return;
                PlayNode("S1_ClubRoom_Badge");
                break;
            case "glass":
                if (phase < 3) PlayNode("S1_ClubRoom_GlassBeforeBadge");
                else PlayNode("S1_ClubRoom_GlassResidue");
                break;
            case "toCorridor":
                if (phase < 3) PlayNode("S1_ClubRoom_NeedBadge");
                else if (screen == 0)
                {
                    if (HasSeenDialogue("S1_ClubRoom_ToCorridor")) SwitchScreen(1);
                    else PlayNode("S1_ClubRoom_ToCorridor", true);
                }
                break;
            case "toRoom":
                dangerSeconds = 0;
                SwitchScreen(0);
                break;
            case "rest":
                if (screen != 0 || phase < 3) return;
                san = 100;
                health = 3;
                UpdateView();
                Save();
                PlayNode("S1_ClubRoom_Rest");
                break;
            case "toBridge":
                if (screen != 1) return;
                phase = 4;
                UpdateView();
                Save();
                SwitchScreen(2, "S1_Bridge_Arrive");
                break;
            case "bridgeBack":
                SwitchScreen(1);
                break;
        }
    }

    private void MatchToggled()
    {
        idleSeconds = 0;
        hintStage = 0;
        UpdateView();
    }

    private void CompleteP1()
    {
        if (phase != 1) return;
        phase = 2;
        UpdateView();
        PlayNode("S1_ClubRoom_P1Complete");
    }

    private void HandleDialogueCompleted(string nodeId)
    {
        switch (nodeId)
        {
            case "S1_ClubRoom_Box":
                phase = 1;
                Save();
                UpdateView();
                break;
            case "S1_ClubRoom_Curtain":
            case "S1_ClubRoom_Lamp":
            case "S1_ClubRoom_Chair":
                if (phase == 1 && Matches == 3) CompleteP1();
                break;
            case "S1_ClubRoom_P1Complete":
                Save();
                UpdateView();
                break;
            case "S1_ClubRoom_Badge":
                phase = 3;
                Save();
                UpdateView();
                break;
            case "S1_ClubRoom_GlassResidue":
                Save();
                UpdateView();
                break;
            case "S1_ClubRoom_ToCorridor":
                dangerSeconds = 0;
                SwitchScreen(1);
                break;
            case "S1_Bridge_Arrive":
                EnterS2();
                break;
            case "S1_System_Checkpoint":
                if (screen == 2 && phase == 4) EnterS2();
                break;
        }
    }

    private void PlayNode(string nodeId, bool once = false)
    {
        DialogueService service = DialogueService.Ensure();
        if (service != null) service.Play(nodeId, this, once);
    }

    private void UpdateView()
    {
        if (room != null) room.SetActive(screen == 0);
        if (corridor != null) corridor.SetActive(screen == 1);
        if (bridge != null) bridge.SetActive(screen == 2);
        if (box != null)
        {
            box.SetActive(screen == 0);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchoredPosition = phase == 0 ? boxFloorPosition : boxCabinetPosition;
            boxRect.sizeDelta = phase == 0 ? boxFloorSize : boxCabinetSize;
            boxRect.localScale = phase == 0 ? boxFloorScale : Vector3.one;
        }
        if (boxHotspot != null) boxHotspot.SetActive(screen == 0 && phase == 0);
        if (badgeHotspot != null) badgeHotspot.SetActive(screen == 0 && phase == 2 && !hasBadge);
        if (curtainOverlay != null)
        {
            curtainOverlay.SetActive(true);
            Sprite curtainSprite = curtainClosed ? curtainClosedSprite : curtainOpenSprite;
            curtainOverlay.GetComponent<Image>().sprite = curtainSprite;
            RectTransform curtainRect = curtainOverlay.GetComponent<RectTransform>();
            Vector2 curtainPosition = curtainClosed ? closedCurtainPosition : new Vector2(curtainSprite.rect.x,
                -(curtainSprite.texture.height - curtainSprite.rect.yMax));
            Vector2 curtainSize = curtainClosed ? closedCurtainSize : curtainSprite.rect.size;
            curtainRect.anchoredPosition = curtainPosition;
            curtainRect.sizeDelta = curtainSize;
            if (curtainHoverImage != null)
            {
                curtainHoverImage.sprite = curtainSprite;
                curtainHoverImage.rectTransform.sizeDelta = curtainSize;
                Material hoverMaterial = curtainClosed ? curtainClosedHoverMaterial : curtainOpenHoverMaterial;
                if (hoverMaterial != null) curtainHoverImage.material = hoverMaterial;
            }
            if (curtainHotspotRect != null)
            {
                curtainHotspotRect.anchoredPosition = curtainClosed ? curtainPosition : openCurtainHotspotPosition;
                curtainHotspotRect.sizeDelta = curtainClosed ? curtainSize : openCurtainHotspotSize;
            }
        }
        if (lampGlow != null)
        {
            Sprite lampSprite = lampOn ? lampOnSprite : lampOffSprite;
            lampGlow.GetComponent<Image>().sprite = lampSprite;
            RectTransform lampRect = lampGlow.GetComponent<RectTransform>();
            lampRect.anchoredPosition = new Vector2(lampSprite.rect.x,
                -(lampSprite.texture.height - lampSprite.rect.yMax));
            lampRect.sizeDelta = lampSprite.rect.size;
        }
        if (chairRect != null) chairRect.anchoredPosition = (chairPlaced ? chairTape : chairCabinet).anchoredPosition;
        if (chairHotspotRect != null)
        {
            chairHotspotRect.anchoredPosition = chairRect.anchoredPosition;
            chairHotspotRect.gameObject.SetActive(screen == 0 && (phase == 1 || (phase == 2 && !hasBadge)));
        }
        timeText.text = phase >= 4 ? "周六 00:10" : phase >= 2 ? "周六 00:00" : "周五 23:30";
        objectiveText.text = phase == 0 ? "捡起掉落的旧照片" : phase == 1 ? "对照旧照片还原活动室" :
            phase == 2 ? "查看第五把椅子的坐垫" : phase == 3 ? "前往 4F 玻璃连廊" : "抵达 4F 连廊入口";
        progressText.text = phase == 1 ? "环境匹配 " + Matches + "/3" : phase >= 2 ? "P1 已完成" : "S1 · 旧照片";
        statusText.text = "SAN " + san + "/100    生命 " + health + "/3";
        inventoryText.text = "任务物品\n\n" + (hasPhoto ? "✓ 去年的活动室照片\n" : "○ 照片待取得\n") +
                             (hasBadge ? "✓ 林未的旧工牌" : "○ 工牌待取得");
        // The old photograph is the target arrangement, independent of the room's current state.
        photoCurtain.enabled = false;
        photoLamp.enabled = false;
        hintLampHighlight.SetActive(phase == 1 && hintStage >= 2);
        hintTapeHighlight.SetActive(phase == 1 && hintStage >= 2);
        photoShadow.color = new Color(0.04f, 0.07f, 0.10f, phase >= 2 ? 0.72f : Matches * 0.11f);
        photoShadowHead.color = new Color(0.04f, 0.07f, 0.10f, phase >= 2 ? 0.72f : Matches * 0.11f);
        photoCaption.text = "";
        memoText.text = "【社团自留资料】\n" + (hasPhoto ? "去年的活动室照片：背面记有 23:30。\n" : "旧照片尚未取得。\n") +
                        (hasBadge ? "林未的旧工牌：编号 05，背面指向 4F 玻璃两端。\n" : "旧工牌尚未取得。\n") +
                        "\n【现实调查】\n窗帘 " + (curtainClosed ? "已拉合" : "拉开") + "；桌灯 " + (lampOn ? "亮" : "灭") +
                        "；第五把椅子 " + (chairPlaced ? "在白色地胶框内" : "在储物柜旁") + "。" +
                        (glassRead ? "\n\n【残影】\n门玻璃上短暂出现「不要让她独自过连廊……」。" : "");
        UpdateInteractionVisibility();
    }

    private void UpdateInteractionVisibility()
    {
        bool visible = !ModalOpen;
        if (roomHotspots != null) roomHotspots.SetActive(visible && screen == 0);
        if (corridorHotspots != null) corridorHotspots.SetActive(visible && screen == 1);
        if (bridgeHotspots != null) bridgeHotspots.SetActive(visible && screen == 2);
        if (!visible) SetHover("");
    }

    private void SwitchScreen(int index, string arrivalNode = null)
    {
        int previous = screen;
        screen = index;
        Save();
        string destination = GameFlow.S1Location(index);
        if (SceneManager.GetActiveScene().name == destination)
        {
            UpdateView();
            if (!string.IsNullOrEmpty(arrivalNode)) PlayNode(arrivalNode);
            return;
        }
        pendingTravel = Capture();
        pendingArrivalNode = arrivalNode;
        if (!GameFlow.LoadLocation(destination))
        {
            screen = previous;
            pendingTravel = null;
            pendingArrivalNode = null;
            UpdateView();
        }
    }

    private void EnterS2()
    {
        if (phase < 4 || !hasPhoto || !hasBadge) return;
        var state = new GameState
        {
            chapterId = StoryIds.Chapter.BridgeRoute,
            stepId = "bridgeReached",
            locationId = StoryIds.Location.BridgeOdd4F,
            locationVariant = StoryIds.Variant.Night,
            timeId = "saturday.0010",
            san = san,
            health = health
        };
        state.SetFlag(StoryIds.Flag.P1Complete);
        if (shockApplied) state.SetFlag(StoryIds.Flag.P1Shock);
        if (curtainClosed) state.SetFlag("flag.p1.curtain");
        if (lampOn) state.SetFlag("flag.p1.lamp");
        if (chairPlaced) state.SetFlag("flag.p1.chair");
        if (glassRead) state.SetFlag("flag.s1.glassRead");
        foreach (string node in dialogueSeen) state.MarkDialogueSeen(node);
        state.AddItem(StoryIds.Item.Photo);
        state.AddItem(StoryIds.Item.Badge);
        S2Controller.Begin(state);
    }

    private Checkpoint Capture() => new Checkpoint { phase = phase, screen = screen, san = san, health = health,
        hasPhoto = hasPhoto, hasBadge = hasBadge, curtainClosed = curtainClosed, lampOn = lampOn,
        chairPlaced = chairPlaced, shockApplied = shockApplied, glassRead = glassRead,
        seenDialogue = new List<string>(dialogueSeen) };

    private void Apply(Checkpoint data)
    {
        phase = data.phase; screen = data.screen; san = data.san; health = data.health;
        hasPhoto = data.hasPhoto; hasBadge = data.hasBadge; curtainClosed = data.curtainClosed;
        lampOn = data.lampOn; chairPlaced = data.chairPlaced; shockApplied = data.shockApplied;
        glassRead = data.glassRead;
        dialogueSeen.Clear();
        if (data.seenDialogue != null) dialogueSeen.UnionWith(data.seenDialogue);
    }

    private void Save()
    {
        if (DialogueRunning) return;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Capture()));
        PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 1);
        PlayerPrefs.Save();
    }

    private void Restore()
    {
        Checkpoint data = JsonUtility.FromJson<Checkpoint>(PlayerPrefs.GetString(SaveKey));
        if (data == null) { UpdateView(); return; }
        Apply(data);
        UpdateView();
    }

    public void LoadCheckpoint()
    {
        if (DialogueRunning) return;
        if (!PlayerPrefs.HasKey(SaveKey)) { ShowInfoPanel("检查点", "目前还没有可读取的 S1 检查点。"); return; }
        memoPanel.SetActive(false);
        Restore();
        if (screen != GameFlow.S1LocationIndex(SceneManager.GetActiveScene().name))
            SwitchScreen(screen, "S1_System_Checkpoint");
        else ShowInfoPanel("检查点已读取", "照片、工牌、P1 进度和 SAN/生命已恢复。");
    }

    public void LoadCheckpointFromPause()
    {
        infoPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        photoPanel.SetActive(false);
        memoPanel.SetActive(false);
        completionPanel.SetActive(false);
        LoadCheckpoint();
    }

    public void ToggleInventory()
    {
        if (DialogueRunning || infoPanel.activeSelf || photoPanel.activeSelf || memoPanel.activeSelf) return;
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        UpdateInteractionVisibility();
    }

    public void InspectPhoto()
    {
        if (DialogueRunning) return;
        inventoryPanel.SetActive(false);
        if (!hasPhoto) { ShowInfoPanel("背包", "照片还在地上的纸盒里。"); return; }
        OpenPhoto();
    }

    public void InspectBadge()
    {
        if (DialogueRunning) return;
        inventoryPanel.SetActive(false);
        if (!hasBadge) { ShowInfoPanel("背包", "工牌还没有取得。"); return; }
        ShowInfoPanel("社团旧资料 · 林未的工牌", DialogueDocuments.GetBody("s1.badgeDetail"));
    }

    private void OpenPhoto()
    {
        if (DialogueService.Instance != null) DialogueService.Instance.HideOverlay();
        photoShowingBack = false;
        photoFace.SetActive(true);
        photoBack.SetActive(false);
        photoPanel.SetActive(true);
        UpdateView();
    }

    public IEnumerator ShowPhoto()
    {
        if (!hasPhoto) yield break;
        OpenPhoto();
        while (photoPanel.activeSelf) yield return null;
    }

    public void FlipPhoto()
    {
        photoShowingBack = !photoShowingBack;
        photoFace.SetActive(!photoShowingBack);
        photoBack.SetActive(photoShowingBack);
        photoBackText.text = "23:30\n旧项目场景参考图\n\n社团自留照片 · 去年";
        if (S0Audio.Instance != null) S0Audio.Instance.PlaySfx(S0Audio.SfxPage, 0.85f);
    }

    public void ClosePhoto()
    {
        if (S0Audio.Instance != null) S0Audio.Instance.PlaySfx(S0Audio.SfxPage, 0.55f);
        photoPanel.SetActive(false);
        UpdateInteractionVisibility();
    }

    public void ToggleMemo()
    {
        if (DialogueRunning || infoPanel.activeSelf || photoPanel.activeSelf || inventoryPanel.activeSelf) return;
        memoPanel.SetActive(!memoPanel.activeSelf);
        UpdateInteractionVisibility();
    }

    public void CloseCompletion() { completionPanel.SetActive(false); UpdateInteractionVisibility(); }

    private void ShowInfoPanel(string title, string body)
    {
        infoTitle.text = title;
        infoBody.text = body;
        infoPanel.SetActive(true);
        UpdateInteractionVisibility();
    }

    public void CloseInfo()
    {
        infoPanel.SetActive(false);
        UpdateInteractionVisibility();
    }

    public IEnumerator ShowDocument(string documentId)
    {
        if (!DialogueDocuments.TryGet(documentId, out DialogueDocuments.Entry document))
        {
            Debug.LogWarning("[Dialogue] 缺少文档 ID：" + documentId);
            yield break;
        }
        if (DialogueService.Instance != null) DialogueService.Instance.HideOverlay();
        infoTitle.text = document.residue ? "【残影】" + document.title : document.title;
        infoBody.text = document.body;
        infoPanel.SetActive(true);
        UpdateInteractionVisibility();
        while (infoPanel.activeSelf) yield return null;
    }

    public bool HasFlag(string id)
    {
        switch (id)
        {
            case "flag.p1.curtain": return curtainClosed;
            case "flag.p1.lamp": return lampOn;
            case "flag.p1.chair": return chairPlaced;
            case "flag.p1.complete": return phase >= 2;
            case "flag.p1.shock": return shockApplied;
            case "flag.s1.box": return phase >= 1;
            case "flag.s1.badge": return hasBadge;
            case "flag.s1.corridorUnlocked": return phase >= 3;
            case "flag.s1.glassRead": return glassRead;
            default: return extraFlags.Contains(id);
        }
    }

    public bool HasItem(string id)
    {
        if (id == "photo") return hasPhoto;
        if (id == "badge") return hasBadge;
        return extraItems.Contains(id);
    }

    public bool HasSeenDialogue(string nodeId) => dialogueSeen.Contains(nodeId);
    public void MarkDialogueSeen(string nodeId)
    {
        if (!string.IsNullOrWhiteSpace(nodeId)) dialogueSeen.Add(nodeId);
    }

    public string Route => string.Empty;
    public string View => string.Empty;

    public void SetFlag(string id)
    {
        switch (id)
        {
            case "flag.p1.curtain": curtainClosed = true; break;
            case "flag.p1.lamp": lampOn = true; break;
            case "flag.p1.chair": chairPlaced = true; break;
            case "flag.p1.complete": phase = Mathf.Max(phase, 2); break;
            case "flag.p1.shock": shockApplied = true; break;
            case "flag.s1.badge": hasBadge = true; break;
            case "flag.s1.glassRead": glassRead = true; break;
            default: extraFlags.Add(id); break;
        }
    }

    public void ClearFlag(string id)
    {
        switch (id)
        {
            case "flag.p1.curtain": curtainClosed = false; break;
            case "flag.p1.lamp": lampOn = false; break;
            case "flag.p1.chair": chairPlaced = false; break;
            case "flag.p1.shock": shockApplied = false; break;
            case "flag.s1.badge": hasBadge = false; break;
            case "flag.s1.glassRead": glassRead = false; break;
            default: extraFlags.Remove(id); break;
        }
    }

    public void AddItem(string id)
    {
        if (id == "photo") hasPhoto = true;
        else if (id == "badge") hasBadge = true;
        else if (!string.IsNullOrWhiteSpace(id)) extraItems.Add(id);
    }

    public void ApplyShock(string id, int sanDelta, int healthDelta = 0)
    {
        string guard = "flag." + id + ".shock";
        if (HasFlag(guard)) return;
        if (id == "p1") shockApplied = true;
        else extraFlags.Add(guard);
        san = Mathf.Max(0, san + sanDelta);
        health = Mathf.Clamp(health + healthDelta, 0, 3);
        UpdateView();
    }

    public void ChooseRoute(string choice) { }
    public void ChooseView(string choice) { }

    public void RequestCheckpoint()
    {
        if (DialogueRunning)
        {
            Debug.LogWarning("[Dialogue] 对话进行中不允许写入 S1 检查点。");
            return;
        }
        Save();
    }

    public void RequestGoTo(string locationId, string variant)
    {
        Debug.LogWarning("[Dialogue] S1 不使用 go_to：" + locationId + "/" + variant);
    }

    public void RefreshHud()
    {
        UpdateView();
    }
}
