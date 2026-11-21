using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WWG;

public sealed class S0Controller : MonoBehaviour, IDialogueContext
{
    public GameObject exterior;
    public GameObject corridor;
    public GameObject clubRoom;
    public GameObject bridge;
    public GameObject[] interactionLayers;
    public GameObject infoPanel;
    public GameObject nightTint;
    public GameObject fallenBox;
    public GameObject fallenBoxHotspot;
    public Text infoTitle;
    public Text infoBody;
    public Text hoverText;
    public Text timeText;
    public Text objectiveText;
    public Text progressText;
    public Text memoText;
    public GameObject memoPanel;
    public GameObject completionPanel;
    public Text completionText;
    public Image roomBackdrop;
    public Sprite nightRoomSprite;
    public Image roomLower;
    public Image roomCurtain;
    public Image roomLamp;
    public GameObject cabinetBox;
    public GameObject cabinetBoxHotspot;
    public Sprite nightLowerSprite;
    public Sprite nightCurtainSprite;
    public Sprite nightLampSprite;
    public AudioSource audioSource;
    public AudioClip deskWorkSound;
    public AudioClip breakerSound;

    private Sprite dayRoomSprite;
    private Sprite dayLowerSprite;
    private Sprite dayCurtainSprite;
    private Sprite dayLampSprite;

    private int phase; // 0 arrival, 1 opening, 2 daytime investigation, 3 transition, 4 S0 complete
    private int screen; // 0 exterior, 1 corridor, 2 club room, 3 bridge
    private bool boardRead;
    private bool chairRead;
    private bool signRead;
    private bool timerRead;
    private bool guideRead;
    private bool memoOpen;
    private bool infoOpen;
    private bool transitionBusy;
    private bool boxFallen;
    private bool subscribed;
    private readonly HashSet<string> extraFlags = new HashSet<string>();
    private readonly HashSet<string> extraItems = new HashSet<string>();
    private readonly HashSet<string> dialogueSeen = new HashSet<string>();

    private sealed class TravelState
    {
        public int phase;
        public int screen;
        public bool boardRead, chairRead, signRead, timerRead, guideRead;
        public string arrivalNode;
        public List<string> seenDialogue;
    }

    [Serializable]
    private sealed class ManualCheckpoint
    {
        public int phase, screen;
        public bool boardRead, chairRead, signRead, timerRead, guideRead;
        public List<string> seenDialogue;
    }

    private const string ManualSaveKey = "WWG_S0_ManualCheckpoint";

    private static TravelState pendingTravel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTravel() { pendingTravel = null; }

    private static bool DialogueRunning =>
        DialogueService.Instance != null && DialogueService.Instance.IsRunning;

    public bool IsGameplayModalOpen => DialogueRunning || infoPanel.activeSelf ||
        memoPanel.activeSelf || completionPanel.activeSelf || transitionBusy;

    public bool SaveManualCheckpoint()
    {
        if (transitionBusy || phase == 3 || DialogueRunning) return false;
        var data = new ManualCheckpoint { phase = phase, screen = screen, boardRead = boardRead,
            chairRead = chairRead, signRead = signRead, timerRead = timerRead, guideRead = guideRead,
            seenDialogue = new List<string>(dialogueSeen) };
        PlayerPrefs.SetString(ManualSaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.SetInt("WWG_LastCheckpointChapter", 0);
        PlayerPrefs.Save();
        return true;
    }

    public SaveSlotSnapshot CaptureSaveSlot()
    {
        if (transitionBusy || phase == 3 || DialogueRunning) return null;
        var data = new ManualCheckpoint { phase = phase, screen = screen, boardRead = boardRead,
            chairRead = chairRead, signRead = signRead, timerRead = timerRead, guideRead = guideRead,
            seenDialogue = new List<string>(dialogueSeen) };
        return new SaveSlotSnapshot { chapter = "S0", payload = JsonUtility.ToJson(data),
            gameTime = timeText.text, locationLabel = "S0 · " + timeText.text + " · " + GameFlow.S0Location(screen) };
    }

    public bool LoadManualCheckpoint()
    {
        if (!PlayerPrefs.HasKey(ManualSaveKey) || transitionBusy) return false;
        var data = JsonUtility.FromJson<ManualCheckpoint>(PlayerPrefs.GetString(ManualSaveKey));
        if (data == null) return false;
        phase = data.phase; screen = data.screen; boardRead = data.boardRead;
        boxFallen = phase == 4;
        chairRead = data.chairRead; signRead = data.signRead; timerRead = data.timerRead;
        guideRead = data.guideRead;
        dialogueSeen.Clear();
        if (data.seenDialogue != null) dialogueSeen.UnionWith(data.seenDialogue);
        infoPanel.SetActive(false); memoPanel.SetActive(false); completionPanel.SetActive(false);
        infoOpen = false; memoOpen = false;
        var destination = GameFlow.S0Location(screen);
        if (SceneManager.GetActiveScene().name != destination)
        {
            pendingTravel = new TravelState { phase = phase, screen = screen, boardRead = boardRead,
                chairRead = chairRead, signRead = signRead, timerRead = timerRead, guideRead = guideRead,
                arrivalNode = "S0_System_Resume", seenDialogue = new List<string>(dialogueSeen) };
            return GameFlow.LoadLocation(destination);
        }
        ApplyScreen();
        PlayNode("S0_System_Resume");
        return true;
    }

    private void Start()
    {
        Application.runInBackground = true;
        S0Audio.Ensure(transform);
        // A system CJK font is used at runtime; the editor asset stays portable.
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
        if (font != null)
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;

        DialogueService service = DialogueService.Ensure();
        if (service != null)
        {
            service.DialogueCompleted += HandleDialogueCompleted;
            subscribed = true;
        }

        TravelState travel = pendingTravel;
        pendingTravel = null;
        phase = travel?.phase ?? 0;
        boxFallen = phase == 4;
        screen = travel?.screen ?? Mathf.Max(0, GameFlow.S0LocationIndex(SceneManager.GetActiveScene().name));
        if (travel != null)
        {
            boardRead = travel.boardRead; chairRead = travel.chairRead; signRead = travel.signRead;
            timerRead = travel.timerRead; guideRead = travel.guideRead;
            dialogueSeen.Clear();
            if (travel.seenDialogue != null) dialogueSeen.UnionWith(travel.seenDialogue);
        }
        if (roomBackdrop != null) dayRoomSprite = roomBackdrop.sprite;
        if (roomLower != null) dayLowerSprite = roomLower.sprite;
        if (roomCurtain != null) dayCurtainSprite = roomCurtain.sprite;
        if (roomLamp != null) dayLampSprite = roomLamp.sprite;
        infoPanel.SetActive(false);
        memoPanel.SetActive(false);
        completionPanel.SetActive(false);
        nightTint.SetActive(false);
        fallenBox.SetActive(false);
        ApplyScreen();
        if (PlayerPrefs.GetInt("WWG_S0_LoadManual", 0) == 1)
        {
            PlayerPrefs.DeleteKey("WWG_S0_LoadManual");
            if (LoadManualCheckpoint()) return;
        }
        if (travel != null && !string.IsNullOrEmpty(travel.arrivalNode))
            PlayNode(travel.arrivalNode);
        else if (travel != null && screen == 2 && phase == 0)
        {
            phase = 1;
            UpdateHUD();
            PlayNode("S0_ClubRoom_Intro");
        }
        else if (travel == null && screen == 0)
            PlayNode("S0_Exterior_Intro");
        else
            TryBeginTransition();
    }

    private void OnDestroy()
    {
        if (subscribed && DialogueService.Instance != null)
            DialogueService.Instance.DialogueCompleted -= HandleDialogueCompleted;
        subscribed = false;
    }

    private void ShowScreen(int index, string arrivalNode = null)
    {
        string destination = GameFlow.S0Location(index);
        if (SceneManager.GetActiveScene().name != destination)
        {
            pendingTravel = new TravelState { phase = phase, screen = index, boardRead = boardRead,
                chairRead = chairRead, signRead = signRead, timerRead = timerRead,
                guideRead = guideRead, arrivalNode = arrivalNode,
                seenDialogue = new List<string>(dialogueSeen) };
            if (!GameFlow.LoadLocation(destination)) pendingTravel = null;
            return;
        }
        screen = index;
        ApplyScreen();
    }

    private void ApplyScreen()
    {
        if (exterior != null) exterior.SetActive(screen == 0);
        if (corridor != null) corridor.SetActive(screen == 1);
        if (clubRoom != null) clubRoom.SetActive(screen == 2);
        if (bridge != null) bridge.SetActive(screen == 3);
        bool lightsOut = phase == 4;
        if (roomBackdrop != null) roomBackdrop.sprite = lightsOut ? nightRoomSprite : dayRoomSprite;
        if (roomLower != null)
        {
            Sprite lowerSprite = lightsOut ? nightLowerSprite : dayLowerSprite;
            roomLower.sprite = lowerSprite;
            roomLower.rectTransform.anchoredPosition = new Vector2(lowerSprite.rect.x,
                -(lowerSprite.texture.height - lowerSprite.rect.yMax));
            roomLower.rectTransform.sizeDelta = lowerSprite.rect.size;
        }
        if (roomCurtain != null)
        {
            Sprite curtainSprite = lightsOut ? nightCurtainSprite : dayCurtainSprite;
            roomCurtain.sprite = curtainSprite;
            roomCurtain.rectTransform.anchoredPosition = new Vector2(curtainSprite.rect.x,
                -(curtainSprite.texture.height - curtainSprite.rect.yMax));
            roomCurtain.rectTransform.sizeDelta = curtainSprite.rect.size;
        }
        if (roomLamp != null) roomLamp.sprite = lightsOut ? nightLampSprite : dayLampSprite;
        if (cabinetBox != null) cabinetBox.SetActive(!boxFallen);
        nightTint.SetActive(false);
        fallenBox.SetActive(boxFallen && screen == 2);
        SetHover("");
        UpdateHUD();
        UpdateInteractionVisibility();
        if (S0Audio.Instance != null)
        {
            var audio = S0Audio.Instance;
            if (screen == 0)
            {
                audio.Play("voice", S0Audio.BgmVoice, 0.5f, 2f);
                audio.StopNow("room");
                audio.StopNow("cricket");
            }
            else if (screen == 1)
            {
                audio.Play("voice", S0Audio.BgmVoice, 0.1f, 1.5f);
                audio.StopNow("room");
                audio.StopNow("cricket");
            }
            else if (screen == 2)
            {
                audio.Stop("voice", 1.5f);
                audio.Play("room", S0Audio.BgmClubRoom, 0.25f, 1.2f);
            }
            else
            {
                audio.Stop("voice", 1.5f);
                audio.StopNow("room");
                audio.StopNow("cricket");
            }
            Debug.Log("[S0Audio] 应用场景音量 screen=" + screen);
        }
    }

    private void UpdateInteractionVisibility()
    {
        bool visible = !DialogueRunning && !infoPanel.activeSelf && !memoPanel.activeSelf &&
                       !completionPanel.activeSelf && !transitionBusy;
        if (interactionLayers != null)
            foreach (GameObject layer in interactionLayers)
                if (layer != null) layer.SetActive(visible);
        if (fallenBoxHotspot != null) fallenBoxHotspot.SetActive(visible && boxFallen && screen == 2);
        if (cabinetBoxHotspot != null) cabinetBoxHotspot.SetActive(visible && !boxFallen && screen == 2);
        if (!visible) SetHover("");
    }

    public void SetHover(string label) { hoverText.text = label; }

    public void HandleHotspot(string action)
    {
        if (DialogueRunning || infoOpen || memoOpen || transitionBusy || completionPanel.activeSelf) return;
        switch (action)
        {
            case "directory":
                PlayNode("S0_Exterior_Directory");
                break;
            case "exteriorBridge":
                PlayNode("S0_Exterior_Bridge");
                break;
            case "enter":
                if (!boardRead) PlayNode("S0_Exterior_NeedBoard");
                else ShowScreen(1, "S0_Corridor_Arrive");
                break;
            case "toRoom":
                ShowScreen(2);
                break;
            case "toCorridor":
                if (phase == 4) PlayNode("S0_ClubRoom_ReturnPhoto");
                else ShowScreen(1);
                break;
            case "toBridge":
                if (phase < 2) PlayNode("S0_Day_NeedClubRoom");
                else ShowScreen(3, "S0_Bridge_Arrive");
                break;
            case "bridgeBack": ShowScreen(1); break;
            case "chair":
                if (phase < 2) break;
                PlayNode("S0_ClubRoom_Chair");
                break;
            case "box":
                if (phase < 2 || phase == 4) break;
                PlayNode("S0_ClubRoom_Box");
                break;
            case "lamp":
                if (phase < 2) break;
                PlayNode("S0_ClubRoom_Lamp");
                break;
            case "sign":
                PlayNode("S0_ClubRoom_Sign");
                break;
            case "timer":
                PlayNode("S0_Corridor_Timer");
                break;
            case "floorPlaque":
                PlayNode("S0_Corridor_FloorPlaque");
                break;
            case "guide":
                PlayNode("S0_Bridge_Guide");
                break;
            case "glass":
                PlayNode("S0_Bridge_Glass");
                break;
            case "switch":
                PlayNode("S0_Bridge_Switch");
                break;
            case "photo":
                if (phase == 4)
                {
                    PlayerPrefs.SetInt("WWG_S1_EnterFresh", 1);
                    PlayerPrefs.Save();
                    GameFlow.LoadLocation(GameFlow.S1Scene);
                }
                break;
        }
    }

    private bool AllMemories => chairRead && signRead && timerRead && guideRead;

    private bool PlayNode(string nodeId, bool once = false)
    {
        DialogueService service = DialogueService.Ensure();
        return service != null && service.Play(nodeId, this, once);
    }

    private void HandleDialogueCompleted(string nodeId)
    {
        switch (nodeId)
        {
            case "S0_ClubRoom_Intro":
                phase = 2;
                UpdateHUD();
                UpdateInteractionVisibility();
                break;
            case "S0_ClubRoom_Transition":
                TransitionSequence();
                break;
            case "S0_ClubRoom_WorkMontage":
                LightsOut();
                break;
            case "S0_ClubRoom_LightsOut":
                DropBox();
                break;
            case "S0_ClubRoom_Chair":
            case "S0_ClubRoom_Box":
            case "S0_System_Resume":
                TryBeginTransition();
                break;
        }
    }

    private void TryBeginTransition()
    {
        if (phase != 2 || screen != 2 || !AllMemories || DialogueRunning) return;
        if (!PlayNode("S0_ClubRoom_Transition")) return;
        phase = 3;
        UpdateHUD();
    }

    private void TransitionSequence()
    {
        transitionBusy = true;
        UpdateInteractionVisibility();
        if (deskWorkSound != null) audioSource.PlayOneShot(deskWorkSound, 0.65f);
        PlayNode("S0_ClubRoom_WorkMontage");
    }

    private void LightsOut()
    {
        if (breakerSound != null) audioSource.PlayOneShot(breakerSound, 0.75f);
        phase = 4;
        boxFallen = false;
        ShowScreen(2);
        PlayNode("S0_ClubRoom_LightsOut");
        transitionBusy = false;
        UpdateInteractionVisibility();
    }

    private void DropBox()
    {
        boxFallen = true;
        PlayerPrefs.SetInt("WWG_S0_Complete", 1);
        PlayerPrefs.Save();
        ApplyScreen();
        PlayNode("S0_ClubRoom_BoxFalls");
    }

    public bool HasFlag(string id)
    {
        switch (id)
        {
            case "flag.s0.board": return boardRead;
            case "flag.s0.chair": return chairRead;
            case "flag.s0.sign": return signRead;
            case "flag.s0.timer": return timerRead;
            case "flag.s0.guide": return guideRead;
            case "flag.s0.complete": return phase == 4;
            default: return extraFlags.Contains(id);
        }
    }

    public bool HasItem(string id) => extraItems.Contains(id);
    public bool HasSeenDialogue(string nodeId) => dialogueSeen.Contains(nodeId);
    public void MarkDialogueSeen(string nodeId)
    {
        if (!string.IsNullOrWhiteSpace(nodeId)) dialogueSeen.Add(nodeId);
    }

    public int San => 100;
    public int Health => 3;
    public string Route => string.Empty;
    public string View => string.Empty;

    public void SetFlag(string id)
    {
        switch (id)
        {
            case "flag.s0.board": boardRead = true; break;
            case "flag.s0.chair": chairRead = true; break;
            case "flag.s0.sign": signRead = true; break;
            case "flag.s0.timer": timerRead = true; break;
            case "flag.s0.guide": guideRead = true; break;
            default: extraFlags.Add(id); break;
        }
    }

    public void ClearFlag(string id)
    {
        switch (id)
        {
            case "flag.s0.board": boardRead = false; break;
            case "flag.s0.chair": chairRead = false; break;
            case "flag.s0.sign": signRead = false; break;
            case "flag.s0.timer": timerRead = false; break;
            case "flag.s0.guide": guideRead = false; break;
            default: extraFlags.Remove(id); break;
        }
    }

    public void AddItem(string id)
    {
        if (!string.IsNullOrWhiteSpace(id)) extraItems.Add(id);
    }

    public void ApplyShock(string id, int sanDelta, int healthDelta = 0)
    {
        string guard = "flag." + id + ".shock";
        if (HasFlag(guard)) return;
        extraFlags.Add(guard);
        Debug.Log("[Dialogue] S0 无 SAN 冲击定义，已忽略：" + id);
    }

    public void ChooseRoute(string choice) { }
    public void ChooseView(string choice) { }

    public IEnumerator ShowDocument(string documentId)
    {
        if (DialogueService.Instance != null) DialogueService.Instance.HideOverlay();
        var shared = WwgDocumentPanel.Instance;
        if (shared != null)
        {
            infoOpen = true;
            UpdateInteractionVisibility();
            yield return shared.Show(documentId);
            infoOpen = false;
            UpdateInteractionVisibility();
            yield break;
        }
        if (!DialogueDocuments.TryGet(documentId, out DialogueDocuments.Entry document))
        {
            Debug.LogWarning("[Dialogue] 缺少文档 ID：" + documentId);
            yield break;
        }
        infoTitle.text = document.residue ? "【残影】" + document.title : document.title;
        infoBody.text = document.body;
        infoOpen = true;
        infoPanel.SetActive(true);
        UpdateInteractionVisibility();
        while (infoOpen) yield return null;
    }

    public IEnumerator ShowPhoto() => null;

    public void RequestCheckpoint()
    {
        if (DialogueRunning)
        {
            Debug.LogWarning("[Dialogue] 对话进行中不允许写入 S0 检查点。");
            return;
        }
        SaveManualCheckpoint();
    }

    public void RequestGoTo(string locationId, string variant)
    {
        Debug.LogWarning("[Dialogue] S0 不使用 go_to：" + locationId + "/" + variant);
    }

    public void RefreshHud()
    {
        UpdateHUD();
        UpdateInteractionVisibility();
    }

    public void CloseInfo()
    {
        var shared = WwgDocumentPanel.Instance;
        if (shared != null) shared.Close();
        if (infoPanel != null) infoPanel.SetActive(false);
        infoOpen = false;
        UpdateInteractionVisibility();
    }

    public void ToggleMemo()
    {
        if (DialogueRunning || infoOpen || transitionBusy) return;
        memoOpen = !memoOpen;
        memoPanel.SetActive(memoOpen);
        UpdateInteractionVisibility();
        if (!memoOpen) return;
        memoText.text = "【建筑导览】\n" +
            (boardRead ? DialogueDocuments.GetBody("s0.directory") : "尚未查阅门口展板。");
        if (phase < 2) return;
        memoText.text += "\n\n【白天取景】\n" +
            (chairRead ? "✓ 活动室：第五把椅子、白色地胶框和柜顶旧纸盒。\n" : "○ 活动室：靠窗区域待查看。\n") +
            (signRead && timerRead ? "✓ 5F 走廊：安全出口与 23:30 公共照明定时箱。\n" : "○ 5F 走廊：安全指示牌和定时箱待查看。\n") +
            (guideRead ? "✓ 4F 连廊：先单数栋、后偶数栋；从连廊口向外。" : "○ 4F 连廊：导览练习稿待查看。");
    }

    private void UpdateHUD()
    {
        if (timeText == null) return;
        timeText.text = phase == 4 ? "周五 23:30" : phase == 3 ? "周五 16:30 → 23:30" : phase == 2 ? "周五 16:10" : phase == 1 ? "周五 15:55" : "周五 15:40";
        if (phase == 4) objectiveText.text = boxFallen ? "储物柜的旧纸盒掉了下来……" : "走廊灯熄了，活动室仍亮着";
        else if (phase == 3) objectiveText.text = "与队友整理关卡草图";
        else if (phase == 2) objectiveText.text = AllMemories ? "回 5F 活动室" : "查看活动室、5F 走廊和 4F 连廊入口";
        else objectiveText.text = "前往单数栋 5F 游戏开发协会活动室";
        progressText.text = phase == 4 ? (boxFallen ? "S0 完成 · 旧照片待调查" : "23:30 · 公共照明熄灭") : phase == 3 ? "时间推进中" : phase < 2 ? "开题前" : "白天记忆点 " + ((chairRead ? 1 : 0) + (signRead && timerRead ? 1 : 0) + (guideRead ? 1 : 0)) + "/3";
    }
}
