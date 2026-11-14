using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using WWG;

public sealed class S1Controller : MonoBehaviour
{
    [Serializable]
    private struct Line
    {
        public string speaker;
        public string body;
        public Line(string speaker, string body) { this.speaker = speaker; this.body = body; }
    }

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
    }

    public GameObject room;
    public GameObject corridor;
    public GameObject bridge;
    public GameObject roomHotspots;
    public GameObject corridorHotspots;
    public GameObject bridgeHotspots;
    public GameObject box;
    public GameObject boxHotspot;
    public GameObject chair;
    public RectTransform chairRect;
    public RectTransform chairCabinet;
    public RectTransform chairTape;
    public RectTransform tapeTarget;
    public GameObject badgeHotspot;
    public GameObject curtainOverlay;
    public GameObject lampGlow;
    public GameObject dialoguePanel;
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
    public Text speakerText;
    public Text dialogueText;
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
    private readonly Queue<Line> lines = new Queue<Line>();
    private Action afterDialogue;
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
    private float idleSeconds;
    private float dangerSeconds;
    private float zeroSanSeconds;
    private int hintStage;
    private static Checkpoint pendingTravel;
    private static string pendingArrival;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTravel() { pendingTravel = null; pendingArrival = null; }

    private bool ModalOpen => dialoguePanel.activeSelf || infoPanel.activeSelf || inventoryPanel.activeSelf ||
                              photoPanel.activeSelf || memoPanel.activeSelf || completionPanel.activeSelf;
    public bool IsGameplayModalOpen => ModalOpen;
    public bool HasPhoto => hasPhoto;
    public bool HasBadge => hasBadge;
    public int San => san;
    public int Health => health;
    public bool SaveManualCheckpoint()
    {
        Save();
        return true;
    }

    public void UseInventoryItem(string item, string hotspot)
    {
        if (ModalOpen) return;
        if (item == "photo" && hasPhoto)
        {
            if (screen == 0 && (hotspot == "curtain" || hotspot == "lamp" || hotspot == "chair"))
            {
                ShowPhoto();
                return;
            }
            ShowInfo("去年的活动室照片", "这是社团留下的旧照片。照片记录的是活动室里窗帘、桌灯和第五把椅子的位置；可放大查看。", null);
        }
        else if (item == "badge" && hasBadge)
        {
            ShowInfo("林未的旧工牌", "背面写着：玻璃两边，各存一半。4F。\n\n这是调查线索，工牌会留在物品栏中。", null);
        }
    }
    private int Matches => (curtainClosed ? 1 : 0) + (lampOn ? 1 : 0) + (chairPlaced ? 1 : 0);

    private void Start()
    {
        Application.runInBackground = true;
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei" }, 24);
        if (font != null)
            foreach (Text item in GetComponentsInChildren<Text>(true)) item.font = font;

        dialoguePanel.SetActive(false);
        infoPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        photoPanel.SetActive(false);
        memoPanel.SetActive(false);
        completionPanel.SetActive(false);

        Checkpoint travel = pendingTravel;
        string arrival = pendingArrival;
        pendingTravel = null;
        pendingArrival = null;
        bool fresh = PlayerPrefs.GetInt("WWG_S1_EnterFresh", 0) == 1;
        PlayerPrefs.DeleteKey("WWG_S1_EnterFresh");
        if (travel != null)
        {
            Apply(travel);
            UpdateView();
            if (!string.IsNullOrEmpty(arrival))
                Say(new[] { new Line("我", arrival) }, screen == 2 && phase == 4 ? ShowBridgeCompletion : null);
        }
        else if (!fresh && PlayerPrefs.HasKey(SaveKey))
        {
            Restore();
            if (screen != GameFlow.S1LocationIndex(SceneManager.GetActiveScene().name)) SwitchScreen(screen);
        }
        else
        {
            screen = Mathf.Max(0, GameFlow.S1LocationIndex(SceneManager.GetActiveScene().name));
            UpdateView();
            if (screen == 0)
                Say(new[] {
                    new Line("唐澈", "吓我一跳……储物柜顶上那盒子怎么自己震下来了？"),
                    new Line("袁祁", "正好在你脚边，帮忙捡一下，看看有没有摔坏社团的东西。")
                });
        }
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
                Say(new[] { new Line("许圆", "照片里的窗帘是关着的。现在这边似乎还漏着窗外的光。") });
            }
            else if (hintStage == 1 && idleSeconds >= 60f)
            {
                hintStage = 2;
                ShowPhoto();
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
                    Save();
                    SwitchScreen(0, "眼前一黑。再睁眼时，我已经回到亮着灯的活动室。照片和工牌还在。");
                    return;
                }
                UpdateView();
            }
        }
    }

    public void SetHover(string label) { hoverText.text = label; }

    public void HandleHotspot(string action)
    {
        if (ModalOpen) return;
        switch (action)
        {
            case "box":
                if (phase != 0) return;
                hasPhoto = true;
                phase = 1;
                Say(new[] {
                    new Line("我", "盒子里都是过期的社团招新传单，最上面夹着一张洗出来的照片。背面写着：23:30 / 旧项目场景参考图。"),
                    new Line("许圆", "这是去年的活动室。照片里窗帘拉着，长桌边的旧桌灯亮着，第五把椅子摆在窗边白色地胶框里。"),
                    new Line("许圆", "要不照着它把灯光和位置还原一下？我拍张对比图。"),
                    new Line("袁祁", "你帮许圆把这三处对一下吧，我把分支表存个档。")
                }, () => ShowPhoto());
                break;
            case "curtain":
                if (phase != 1) return;
                curtainClosed = !curtainClosed;
                MatchChanged(curtainClosed ? "把窗帘拉严了，窗外的光被挡住。" : "窗帘重新拉开了。照片里不是这样。");
                break;
            case "lamp":
                if (phase != 1) return;
                lampOn = !lampOn;
                MatchChanged(lampOn ? "旧桌灯还能亮，暖光打在靠窗那块空地上。" : "桌灯关了，照片里的它是亮着的。");
                break;
            case "chair":
                if (phase != 1) return;
                chairPlaced = !chairPlaced;
                MatchChanged(chairPlaced ? "椅脚刚好卡进白色地胶框。" : "把第五把椅子挪回储物柜旁。" );
                break;
            case "badge":
                if (phase != 2 || hasBadge) return;
                hasBadge = true;
                phase = 3;
                Save();
                Say(new[] {
                    new Line("我", "坐垫夹缝里塞着去年的参赛工牌。正面是林未，编号 05。"),
                    new Line("我", "背面写着：玻璃两边，各存一半。4F。"),
                    new Line("许圆", "林未？她好像是去年在这间活动室参赛的人。"),
                    new Line("唐澈", "四楼带玻璃的地方，就是你下午去过的跨栋连廊。"),
                    new Line("袁祁", "先坐着缓两分钟。照片刚才在我看来还是空的。")
                });
                break;
            case "glass":
                if (phase < 3) { Say(new[] { new Line("我", "门玻璃外的公共走廊已经全黑，屋里的电脑还亮着。") }); return; }
                glassRead = true;
                Save();
                ShowInfo("残影 · 门玻璃", "水雾般的旧字一闪而过：\n\n「不要让她独自过连廊……」\n\n这行字只在玻璃反光里出现，门牌和现实墙面没有变化。", null);
                break;
            case "toCorridor":
                if (phase < 3) { Say(new[] { new Line("我", "先看清这张照片，还有椅垫下露出的东西。") }); return; }
                if (screen == 0)
                {
                    Say(new[] {
                        new Line("我", "工牌背面写了 4F，我想去四楼连廊看看。"),
                        new Line("袁祁", "走廊灯全灭了。手机保持通话，觉得不舒服就回来歇会儿。"),
                        new Line("许圆", "四楼入口有应急灯开关，看不清就先打开。"),
                        new Line("唐澈", "找到东西拍个照就回来。")
                    }, () => { dangerSeconds = 0; SwitchScreen(1); });
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
                Save();
                UpdateView();
                Say(new[] { new Line("我", "在亮着灯的活动室坐了一会儿，呼吸终于平稳了。") });
                break;
            case "toBridge":
                if (screen != 1) return;
                phase = 4;
                Save();
                SwitchScreen(2, "单数栋 4F 连廊入口。玻璃外的天井黑着，应急照明开关亮着红点。");
                break;
            case "bridgeBack":
                SwitchScreen(1);
                break;
        }
    }

    private void MatchChanged(string line)
    {
        idleSeconds = 0;
        hintStage = 0;
        UpdateView();
        if (Matches == 3) CompleteP1();
        else Say(new[] { new Line("我", line), new Line("任务记录", "环境匹配 " + Matches + "/3") });
    }

    private void CompleteP1()
    {
        phase = 2;
        if (!shockApplied) { san = Mathf.Max(0, san - 25); shockApplied = true; }
        Save();
        UpdateView();
        ShowPhoto();
        Say(new[] {
            new Line("我", "这张照片……第五把椅子上刚才明明是空的。现在怎么有个人影？"),
            new Line("许圆", "什么人影？让我看看？"),
            new Line("唐澈", "这不是照片上的污渍吗？"),
            new Line("袁祁", "先看现实里那把椅子，坐垫底下是不是夹着什么？")
        });
    }

    private void UpdateView()
    {
        if (room != null) room.SetActive(screen == 0);
        if (corridor != null) corridor.SetActive(screen == 1);
        if (bridge != null) bridge.SetActive(screen == 2);
        if (box != null) box.SetActive(screen == 0 && phase == 0);
        if (boxHotspot != null) boxHotspot.SetActive(screen == 0 && phase == 0);
        if (badgeHotspot != null) badgeHotspot.SetActive(screen == 0 && phase == 2 && !hasBadge);
        if (curtainOverlay != null) curtainOverlay.SetActive(curtainClosed);
        if (lampGlow != null) lampGlow.GetComponent<Image>().color = lampOn ? Color.white : new Color(0.40f, 0.43f, 0.50f, 0.95f);
        if (chairRect != null) chairRect.anchoredPosition = (chairPlaced ? chairTape : chairCabinet).anchoredPosition;
        timeText.text = phase >= 4 ? "周六 00:10" : phase >= 2 ? "周六 00:00" : "周五 23:30";
        objectiveText.text = phase == 0 ? "捡起掉落的旧照片" : phase == 1 ? "对照旧照片还原活动室" :
            phase == 2 ? "查看第五把椅子的坐垫" : phase == 3 ? "前往 4F 玻璃连廊" : "抵达 4F 连廊入口";
        progressText.text = phase == 1 ? "环境匹配 " + Matches + "/3" : phase >= 2 ? "P1 已完成" : "S1 · 旧照片";
        statusText.text = "SAN " + san + "/100    生命 " + health + "/3";
        inventoryText.text = "任务物品\n\n" + (hasPhoto ? "✓ 去年的活动室照片\n" : "○ 照片待取得\n") +
                             (hasBadge ? "✓ 林未的旧工牌" : "○ 工牌待取得");
        // The old photograph is the target arrangement, independent of the room's current state.
        photoCurtain.enabled = true;
        photoLamp.enabled = true;
        hintLampHighlight.SetActive(phase == 1 && hintStage >= 2);
        hintTapeHighlight.SetActive(phase == 1 && hintStage >= 2);
        photoShadow.color = new Color(0.04f, 0.07f, 0.10f, phase >= 2 ? 0.72f : Matches * 0.11f);
        photoShadowHead.color = new Color(0.04f, 0.07f, 0.10f, phase >= 2 ? 0.72f : Matches * 0.11f);
        photoCaption.text = phase >= 2 ? "第五把椅子上，多出一道模糊的人影。" :
            hintStage >= 2 ? "许圆提醒：再看一眼桌灯和白色地胶框。" :
            "去年的活动室 · 23:30    环境匹配 " + Matches + "/3";
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

    private void SwitchScreen(int index, string arrival = null)
    {
        int previous = screen;
        screen = index;
        Save();
        string destination = GameFlow.S1Location(index);
        if (SceneManager.GetActiveScene().name == destination)
        {
            UpdateView();
            if (!string.IsNullOrEmpty(arrival)) Say(new[] { new Line("我", arrival) });
            return;
        }
        pendingTravel = Capture();
        pendingArrival = arrival;
        if (!GameFlow.LoadLocation(destination))
        {
            screen = previous;
            pendingTravel = null;
            pendingArrival = null;
            UpdateView();
        }
    }

    private void ShowBridgeCompletion()
    {
        completionText.text = "S1 完成 · 00:10\n\n下一阶段：4F 连廊双栋调查";
        completionPanel.SetActive(true);
        UpdateInteractionVisibility();
    }

    private Checkpoint Capture() => new Checkpoint { phase = phase, screen = screen, san = san, health = health,
        hasPhoto = hasPhoto, hasBadge = hasBadge, curtainClosed = curtainClosed, lampOn = lampOn,
        chairPlaced = chairPlaced, shockApplied = shockApplied, glassRead = glassRead };

    private void Apply(Checkpoint data)
    {
        phase = data.phase; screen = data.screen; san = data.san; health = data.health;
        hasPhoto = data.hasPhoto; hasBadge = data.hasBadge; curtainClosed = data.curtainClosed;
        lampOn = data.lampOn; chairPlaced = data.chairPlaced; shockApplied = data.shockApplied;
        glassRead = data.glassRead;
    }

    private void Save()
    {
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
        if (ModalOpen && !memoPanel.activeSelf) return;
        if (!PlayerPrefs.HasKey(SaveKey)) { ShowInfo("检查点", "目前还没有可读取的 S1 检查点。", null); return; }
        memoPanel.SetActive(false);
        Restore();
        if (screen != GameFlow.S1LocationIndex(SceneManager.GetActiveScene().name)) SwitchScreen(screen, "检查点已读取。照片、工牌、P1 进度和 SAN/生命已恢复。");
        else ShowInfo("检查点已读取", "照片、工牌、P1 进度和 SAN/生命已恢复。", null);
    }

    public void LoadCheckpointFromPause()
    {
        dialoguePanel.SetActive(false);
        infoPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        photoPanel.SetActive(false);
        memoPanel.SetActive(false);
        completionPanel.SetActive(false);
        lines.Clear();
        afterDialogue = null;
        afterInfo = null;
        LoadCheckpoint();
    }

    public void ToggleInventory()
    {
        if (dialoguePanel.activeSelf || infoPanel.activeSelf || photoPanel.activeSelf || memoPanel.activeSelf) return;
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        UpdateInteractionVisibility();
    }

    public void InspectPhoto()
    {
        inventoryPanel.SetActive(false);
        if (!hasPhoto) { ShowInfo("背包", "照片还在地上的纸盒里。", null); return; }
        ShowPhoto();
    }

    public void InspectBadge()
    {
        inventoryPanel.SetActive(false);
        if (!hasBadge) { ShowInfo("背包", "工牌还没有取得。", null); return; }
        ShowInfo("社团旧资料 · 林未的工牌", "48H GameJam 参赛证\n姓名：林未    编号：05\n\n背面手写：玻璃两边，各存一半。4F。\n\n关键物品 · 不可丢弃", null);
    }

    private void ShowPhoto()
    {
        photoShowingBack = false;
        photoFace.SetActive(true);
        photoBack.SetActive(false);
        photoPanel.SetActive(true);
        UpdateView();
    }

    public void FlipPhoto()
    {
        photoShowingBack = !photoShowingBack;
        photoFace.SetActive(!photoShowingBack);
        photoBack.SetActive(photoShowingBack);
        photoBackText.text = "23:30\n旧项目场景参考图\n\n社团自留照片 · 去年";
    }

    public void ClosePhoto() { photoPanel.SetActive(false); UpdateInteractionVisibility(); }

    public void ToggleMemo()
    {
        if (dialoguePanel.activeSelf || infoPanel.activeSelf || photoPanel.activeSelf || inventoryPanel.activeSelf) return;
        memoPanel.SetActive(!memoPanel.activeSelf);
        UpdateInteractionVisibility();
    }

    public void CloseCompletion() { completionPanel.SetActive(false); UpdateInteractionVisibility(); }

    private Action afterInfo;
    private void ShowInfo(string title, string body, Action callback)
    {
        infoTitle.text = title;
        infoBody.text = body;
        afterInfo = callback;
        infoPanel.SetActive(true);
        UpdateInteractionVisibility();
    }

    public void CloseInfo()
    {
        infoPanel.SetActive(false);
        Action callback = afterInfo;
        afterInfo = null;
        callback?.Invoke();
        UpdateInteractionVisibility();
    }

    private void Say(IEnumerable<Line> dialogue, Action callback = null)
    {
        lines.Clear();
        foreach (Line line in dialogue) lines.Enqueue(line);
        afterDialogue = callback;
        dialoguePanel.SetActive(true);
        UpdateInteractionVisibility();
        ContinueDialogue();
    }

    public void ContinueDialogue()
    {
        if (!dialoguePanel.activeSelf) return;
        if (lines.Count > 0)
        {
            Line line = lines.Dequeue();
            speakerText.text = line.speaker;
            dialogueText.text = line.body;
        }
        else
        {
            dialoguePanel.SetActive(false);
            Action callback = afterDialogue;
            afterDialogue = null;
            callback?.Invoke();
            UpdateInteractionVisibility();
        }
    }
}
