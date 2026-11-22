using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using WWG;

/// <summary>
/// Editor-only smoke run through the second 24H. The menu WWG/Dev/Smoke All Late Stage Branches sets a
/// scenario in SessionState, enters Play and this agent drives the same handlers the UI buttons call.
/// It never writes player checkpoints (development states only) and reports flags as PASS/FAIL.
/// </summary>
public class LateStageSmokeAgent : MonoBehaviour
{
#if UNITY_EDITOR
    private const string Key = "WWG_LateStageSmoke";
    private readonly List<string> report = new List<string>();
    private int failures;
    private string scenario;
    private bool alone;
    private bool dark;
    private bool seal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Boot()
    {
        string value = UnityEditor.SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(value)) return;
        UnityEditor.SessionState.EraseString(Key);
        GameObject host = new GameObject("Late Stage Smoke Agent");
        DontDestroyOnLoad(host);
        LateStageSmokeAgent agent = host.AddComponent<LateStageSmokeAgent>();
        agent.scenario = value;
        agent.alone = value.Contains("alone");
        agent.dark = value.Contains("dark");
        agent.seal = value.Contains("seal");
    }

    private void Start() { StartCoroutine(Run()); }

    private IEnumerator Run()
    {
        Log("scenario " + scenario);
        yield return new WaitForSeconds(1.5f);

        S5Controller s5 = null;
        yield return WaitScene("S5", found => s5 = found.GetComponent<S5Controller>());
        yield return AdvanceAll();
        if (s5 == null) Fail("S5 控制器缺失");
        else
        {
            s5.computerButton.onClick.Invoke();
            TryCall(s5, "CloseAllPanels");
            s5.computerButton.onClick.Invoke();
            if (alone)
            {
                s5.logA.onClick.Invoke();
                s5.logB.onClick.Invoke();
                s5.replayButton.onClick.Invoke();
            }
            else s5.frameTwo.onClick.Invoke();
            yield return Settle();
            Assert(s5.HasFlag(StoryIds.Flag.P4Complete), "S5 P4 完成");
            Assert(s5.HasFlag(alone ? StoryIds.Flag.P4LogFound : StoryIds.Flag.P4FrameFound), "S5 证据与查看方式一致");
            TryCall(s5, "CloseAllPanels");
            TryCall(s5, "InspectBadgeBox");
            Assert(s5.HasFlag(StoryIds.Flag.P4BadgeRead), "S5 纪念盒工牌记录");
            TryCall(s5, "CloseAllPanels");
            TryCall(s5, "InspectOldTestNote");
            yield return Settle();
            Assert(s5.HasFlag(StoryIds.Flag.P4RuleSeen), "S5 旧测试说明");
            TryCall(s5, "CloseAllPanels");
            TryCall(s5, "WaitForLights");
            yield return Settle();
        }

        S6Controller s6 = null;
        yield return WaitScene("S6_Corridor", found => s6 = found.GetComponent<S6Controller>());
        yield return AdvanceAll();
        if (s6 == null) Fail("S6 控制器缺失");
        else
        {
            ClickActive(s6.navB, "5F 下楼箭头");
            yield return Settle();
            s6 = FindFirstObjectByType<S6Controller>();
            ClickActive(s6.emergencySwitch, "4F 应急灯开关");
            ClickActive(s6.guideBoard, "4F 导览板");
            yield return Settle();
            Assert(s6.HasFlag(StoryIds.Flag.P5LightRead), "S6 导览展板旧展位号");
            TryCall(s6, "CloseAllPanels");
            ClickActive(s6.navB, "进入连廊箭头");
            yield return Settle();
            s6 = FindFirstObjectByType<S6Controller>();
            if (dark)
            {
                ClickActive(s6.navA, "连廊退回入口箭头");
                yield return Settle();
                s6 = FindFirstObjectByType<S6Controller>();
                ClickActive(s6.emergencySwitch, "4F 关闭应急灯");
                ClickActive(s6.navB, "重新进入连廊箭头");
                yield return Settle();
                s6 = FindFirstObjectByType<S6Controller>();
                ClickActive(s6.glass, "连廊玻璃热区");
                yield return Settle();
                Assert(s6.HasFlag(StoryIds.Flag.P5GlassMark), "S6 玻璃倒影线索");
                TryCall(s6, "CloseAllPanels");
                ClickActive(s6.navA, "连廊退回入口箭头");
                yield return Settle();
                s6 = FindFirstObjectByType<S6Controller>();
                ClickActive(s6.navC, "下 3F 机房箭头");
                yield return Settle();
                s6 = FindFirstObjectByType<S6Controller>();
                UseFromBag(StoryIds.Item.OldProjectUsb);
                s6.puzzleB.onClick.Invoke();
                s6.puzzleA.onClick.Invoke();
                yield return Settle();
                Assert(s6.HasFlag(StoryIds.Flag.P5BackupRule), "S6 备份归队规则");
            }
            else
            {
                ClickActive(s6.navB, "前往偶数栋箭头");
                yield return Settle();
                s6 = FindFirstObjectByType<S6Controller>();
                ClickActive(s6.cabinet, "偶数栋资料柜热区");
                TryCall(s6, "CloseAllPanels");
                UseFromBag(StoryIds.Item.TeamSheet);
                s6.puzzleA.onClick.Invoke();
                s6.puzzleA.onClick.Invoke();
                yield return Settle();
                Assert(s6.HasFlag(StoryIds.Flag.P5PaperRule), "S6 纸质归队规则");
            }
            Assert(s6.HasFlag(StoryIds.Flag.P5Complete), "S6 取证完成");
            TryCall(s6, "CloseAllPanels");
            ClickActive(s6.navB, "回 5F 走廊箭头");
            yield return Settle();
            s6 = FindFirstObjectByType<S6Controller>();
            ClickActive(s6.navA, "进入 5F 活动室箭头");
            yield return Settle();
        }

        S7Controller s7 = null;
        yield return WaitScene("S7", found => s7 = found.GetComponent<S7Controller>());
        yield return AdvanceAll();
        if (s7 == null) Fail("S7 控制器缺失");
        else
        {
            TryCall(s7, "InspectBadgeBox");
            TryCall(s7, "CloseAllPanels");
            s7.rosterButton.onClick.Invoke();
            TryCall(s7, "CloseAllPanels");
            s7.rosterButton.onClick.Invoke();
            s7.puzzleA.onClick.Invoke();
            s7.puzzleA.onClick.Invoke();
            yield return Settle();
            TryCall(s7, "CloseAllPanels");
            Assert(s7.HasFlag(StoryIds.Flag.P6NameRestored), "S7 原署名恢复");
            TryCall(s7, "ConfirmTeam");
            yield return Settle();
            Assert(s7.HasFlag(StoryIds.Flag.P6TeamConfirmed), "S7 本队 4/4");
            s7.reviewButton.onClick.Invoke();
            if (alone) s7.puzzleB.onClick.Invoke();
            s7.puzzleA.onClick.Invoke();
            s7.puzzleB.onClick.Invoke();
            s7.puzzleC.onClick.Invoke();
            yield return Settle();
            Assert(s7.HasFlag(StoryIds.Flag.P6ReviewDone), "S7 证据复查");
            TryCall(s7, "VerifyAgain");
            yield return Settle();
            Assert(s7.HasFlag(StoryIds.Flag.P6Complete), "S7 二次验证");
            s7.restButton.onClick.Invoke();
            yield return Settle();
        }

        S8Controller s8 = null;
        yield return WaitScene("S8", found => s8 = found.GetComponent<S8Controller>());
        yield return AdvanceAll();
        if (s8 == null) Fail("S8 控制器缺失");
        else
        {
            TryCall(s8, "OpenMaterials");
            Assert(s8.HasFlag(StoryIds.Flag.S8MaterialsSeen), "S8 两份材料");
            TryCall(s8, "CloseAllPanels");
            TryCall(s8, "OpenComputer");
            if (seal)
            {
                s8.puzzleB.onClick.Invoke();
                yield return Settle();
                TryCall(s8, "CloseAllPanels");
                UseFromBag(StoryIds.Item.OldProjectUsb);
                TryCall(s8, "OpenMemorial");
                yield return Settle();
                Assert(s8.HasFlag(StoryIds.Flag.S8Archived), "S8 副本归入档案");
                TryCall(s8, "CloseAllPanels");
                s8.computerButton.onClick.Invoke();
                s8.puzzleA.onClick.Invoke();
            }
            else
            {
                s8.puzzleA.onClick.Invoke();
                yield return Settle();
                s8.puzzleA.onClick.Invoke();
                s8.puzzleA.onClick.Invoke();
                yield return Settle();
                Assert(s8.HasFlag(StoryIds.Flag.S8Previewed), "S8 说明页预览");
                s8.puzzleA.onClick.Invoke();
            }
            yield return Settle();
            Assert(s8.HasFlag(StoryIds.Flag.S8Finished), "S8 封包完成");
            Assert(s8.HasFlag(StoryIds.Flag.S7Finished), "S7 休息过渡");
        }

        FinishRun();
    }

    private void FinishRun()
    {
        report.Add(failures == 0 ? "RESULT PASS" : "RESULT FAIL " + failures);
        Write(report);
        Debug.Log("[SMOKE] " + scenario + " -> " + (failures == 0 ? "PASS" : "FAIL " + failures));
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void Write(List<string> lines)
    {
        try
        {
            string dir = Path.Combine(Application.dataPath, "..", "Temp", "WWGChecks", "smoke");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "smoke_" + scenario + ".txt"), string.Join("\n", lines));
        }
        catch (Exception e) { Debug.LogWarning("[SMOKE] 报告写入失败：" + e.Message); }
    }

    private void Log(string line) { report.Add(line); Debug.Log("[SMOKE] " + line); }

    private void Assert(bool condition, string label)
    {
        if (!condition) { failures++; Log("FAIL " + label); report.Add("  expected true"); }
        else Log("PASS " + label);
    }

    private void Fail(string label) { failures++; Log("FAIL " + label); }

    private IEnumerator WaitScene(string sceneName, Action<GameObject> found)
    {
        float deadline = Time.realtimeSinceStartup + 25f;
        while (Time.realtimeSinceStartup < deadline &&
               UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName)
            yield return new WaitForSeconds(0.25f);
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName)
        {
            Fail("未进入场景 " + sceneName);
            yield break;
        }
        yield return new WaitForSeconds(0.6f);
        GameObject root = null;
        if (sceneName.StartsWith("S6"))
        {
            S6Controller controller = FindFirstObjectByType<S6Controller>();
            if (controller != null) root = controller.gameObject;
        }
        else
        {
            Component c = sceneName == "S5" ? (Component)FindFirstObjectByType<S5Controller>()
                : sceneName == "S7" ? FindFirstObjectByType<S7Controller>()
                : (Component)FindFirstObjectByType<S8Controller>();
            if (c != null) root = c.gameObject;
        }
        if (found != null) found(root);
    }

    private IEnumerator Settle(float extra = 0.5f)
    {
        yield return AdvanceAll();
        yield return new WaitForSeconds(extra);
        yield return AdvanceAll();
    }

    private IEnumerator AdvanceAll()
    {
        for (int i = 0; i < 80; i++)
        {
            DialogueService service = DialogueService.Instance;
            if (service == null || !service.IsRunning) break;
            service.Advance();
            yield return new WaitForSeconds(0.12f);
        }
    }

    private void ClickActive(UnityEngine.UI.Button button, string label)
    {
        if (button == null || !button.isActiveAndEnabled || !button.IsInteractable())
        {
            Fail("无法点击 " + label);
            return;
        }
        button.onClick.Invoke();
    }

    private void UseFromBag(string item)
    {
        GameplayOverlay overlay = FindFirstObjectByType<GameplayOverlay>();
        if (overlay == null) { Fail("缺少共享背包"); return; }
        SharedInteractionHud hud = overlay.GetComponentInChildren<SharedInteractionHud>(true);
        hud.inventoryButton.onClick.Invoke();
        var slot = Array.Find(overlay.GetComponentsInChildren<GameplayInventorySlot>(true), entry => entry.item == item);
        if (slot == null || !slot.isActiveAndEnabled) { Fail("背包缺少可用物品 " + item); return; }
        slot.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
        var use = overlay.transform.Find("Inventory panel/Tooltip/Use").GetComponent<UnityEngine.UI.Button>();
        if (!use.isActiveAndEnabled) { Fail("物品操作菜单没有打开 " + item); return; }
        use.onClick.Invoke();
    }

    private void TryCall(object target, string method, params object[] args)
    {
        if (target == null) { Fail("调用 " + method + " 时目标为空"); return; }
        MethodInfo info = target.GetType().GetMethod(method,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (info == null) { Fail("缺少方法 " + method); return; }
        try { info.Invoke(target, args); }
        catch (Exception e) { Fail(method + " 抛异常：" + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
    }
#endif
}
