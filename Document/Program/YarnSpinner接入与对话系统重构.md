# YarnSpinner 接入与对话系统重构

> 状态：**阶段 0 底座已实施，章节对白尚未迁移**。`dev.yarnspinner.unity` 3.2.8 已写入 `Packages/manifest.json` 与 `Packages/packages-lock.json`；`Assets/Yarn/WWG.yarnproject`、`Assets/Yarn/Shared/Common.yarn` 测试节点、`Assets/Scripts/Dialogue/` 共享服务与自定义 uGUI 呈现器、`Assets/Resources/Dialogue/WwgDialogueHost.prefab`、立绘配置和 `WWG/Yarn/Validate Dialogue` 校验菜单已建立，并已用 `WWG/Dev/Play Dialogue Test` 验证中文行、点击推进、`show_info`、`set_flag`、`give_item`、`apply_shock` 和 `go_to` 排队。S0、S1、S2 仍继续使用各自控制器里的私有 `Line` 队列和 `Say/ShowInfo`，阶段 1–5 未实施。
>
> 关联文档：[架构重构设计](./架构重构设计.md)、[架构规范](./架构规范.md)、[运行流程](./运行流程.md)、[目录结构组织](./目录结构组织.md)、[程序规范文档](./程序规范文档.md)。

## 1. 当前架构分析

### 1.1 已实现的调用链

```text
TitleMenuController ──▶ GameFlow ──▶ SceneManager
S0Hotspot ──▶ S0Controller ──▶ GameFlow（场景切换）
                         └─▶ PlayerPrefs（S0 手动检查点）
S1Hotspot ──▶ S1Controller ──▶ GameFlow（场景切换）
                         └─▶ PlayerPrefs（WWG_S1_Checkpoint）
S2Controller ──▶ GameState / GameStateStore ──▶ 4F 与 5F 地点场景
GameplayOverlay ──▶ S0Controller / S1Controller（仅这两个章节）
```

- `GameFlow` 只负责场景名和加载入口；`S0Controller`、`S1Controller`、`S2Controller` 各自持有本章的进度、对白、HUD 与转场。
- S2 已经使用 `GameState` 和 `GameStateStore`；S0/S1 仍是控制器私有布尔字段加章节 `PlayerPrefs`，没有统一状态源。
- `GameplayOverlay` 只认识 `S0Controller` 和 `S1Controller`，物品槽仍然硬编码照片与工牌；S2 使用自己的暂停、背包和对话面板。

### 1.2 对白实现分布

| 位置 | 现有实现 | 主要问题 |
| --- | --- | --- |
| `S0Controller` | 私有 `Line` 结构、`Queue<Line>`、`Say`、`ContinueDialogue`、`ShowInfo`、`afterDialogue`、立绘查表、字体注入；开场、记忆点、熄灯转场全部内联在代码里 | 文本与章节流程耦合；一句话的修改也要改 C#；立绘/排版规则不可复用 |
| `S1Controller` | 又一份 `Line`、队列、`Say`、`ContinueDialogue`、`ShowInfo`、字体注入；盒中照片、P1、工牌、走廊、4F 台词全部内联 | 与 S0 重复实现；提示梯度与对白混在 `Update` 中 |
| `S2Controller` | 第三份 `DialogueLine`、队列、`Say`、`ContinueDialogue`；路线对白、资料柜便签、抽屉后电话对白内联 | 第三次复制；`seenDialogue` 只用于少数节点；信息面板与对白面板两套逻辑 |
| `GameplayOverlay` | 顶部 HUD、暂停、存档槽、物品详情；通过 `s0/s1` 字段判断章节 | 不能覆盖 S2；新章节必须改共享 UI；不知道新对话服务是否正在运行 |

当前对白数据、UI 面板、模态判断和章节推进是同一层代码。YarnSpinner 如果直接塞进三个控制器，只会把三份队列换成三份 `DialogueRunner` 配置，不能解决根因。

### 1.3 YarnSpinner 包状态

- 包名 `dev.yarnspinner.unity`，版本 `3.2.8`，来自 `https://github.com/YarnSpinnerTool/YarnSpinner-Unity.git#v3.2.8`；`packages-lock.json` 中已锁定提交 `bfc5b6a2e8bc...`。
- 运行时程序集为 `YarnSpinner.Unity`，命名空间主要是 `Yarn.Unity`；编辑器程序集为 `YarnSpinner.Unity.Editor`。
- 关键类型：`DialogueRunner`、`YarnProject`、`DialoguePresenterBase`、`LinePresenter`、`OptionsPresenter`、`LineAdvancer`、`VariableStorageBehaviour`、`InMemoryVariableStorage`、`BuiltinLocalisedLineProvider`、`LocalizedLine`、`DialogueOption`、`YarnTask`。
- 包内自带 `Dialogue System.prefab` 和 `Option Item.prefab`，但内置 `LinePresenter`/`OptionsPresenter` 使用 `TMP_Text`。本项目现有 UI 是 uGUI `Text`，运行时用系统 CJK 字体，不能直接套用内置预制体。
- `YarnSpinner.Unity.asmdef` 通过 `versionDefines` 对 Addressables、Localization、UniTask、TextMeshPro 做条件编译；当前项目没有安装 Addressables、Unity Localization 和 UniTask。现阶段应使用内置的 `BuiltinLocalisedLineProvider`，不要启用 Unity Localization 路径。
- 当前 `Assets/Scripts` 没有自定义 `.asmdef`，运行时代码在 `Assembly-CSharp` 中；`YarnSpinner.Unity` 是 auto-referenced，游戏脚本可以直接引用。第一批接入不需要引入新程序集。

### 1.4 结论

YarnSpinner 应作为**对白与分支文本的唯一来源**接入，但不能成为第二个存档系统，也不能接管章节谜题和场景加载。接入的关键不是“把字符串搬进 `.yarn`”，而是先建立一层共享的对话服务、章节上下文和状态边界，然后把三个控制器的重复对白实现逐个替换掉。

## 2. 目标与边界

### 2.1 本方案覆盖

- 一个项目级 Yarn 工程和按章节拆分的 `.yarn` 文件。
- 共享 `DialogueService`、自定义 uGUI 对话呈现器、章节上下文接口。
- Yarn 命令/函数与 `GameState`、HUD、信息面板、检查点的边界。
- S0、S1、S2 的逐章迁移顺序和验收条件。
- 与新架构 `GameRuntime / StoryDirector / ChapterModule` 的衔接方式。
- 存档版本、旧检查点迁移、`seenDialogue` 去重规则。

### 2.2 本方案不覆盖

- 完整的 `GameRuntime`、`StoryDirector`、`ChapterModule` 迁移；那仍是[架构重构设计](./架构重构设计.md)的工作。
- 多语言本地化、配音、TMP 字体资产、逐字打字机效果。
- 自由行走、战斗、追逐或 3D 对话镜头。
- 把 P1/P2/P3 的谜题判定搬进 Yarn。
- 重新制作现有场景画面、热点布局或存档槽 UI。

## 3. 关键设计决策

| 编号 | 决策 | 理由 |
| --- | --- | --- |
| D1 | **一个** `WWG.yarnproject`，源文件按章节分目录，节点名带章节前缀 | 小体量 Demo 的编译成本很低；一个工程可共享函数、文档 ID 和 `seen` 逻辑；`.yarn` 是文本文件，按章节拆文件即可并行编辑，只有 `.yarnproject` 的源文件模式由集成人维护 |
| D2 | `GameState` 是唯一持久状态；**不把 SAN、flag、物品、路线镜像成 Yarn 变量** | 避免第二套变量存储与存档；Yarn 只通过只读函数查询状态，通过命令修改状态。Yarn 局部变量只用于单次对话内的临时分支 |
| D3 | 使用**自定义 uGUI 呈现器**，不使用内置 `LinePresenter` | 现有 UI 是 `Text` + 系统 CJK 字体；内置呈现器依赖 `TMP_Text`。自定义呈现器还能复用 S0 的立绘和排版规则 |
| D4 | Yarn 只写**叙事对白、独白、电话对白和选项**；展板、便签、工牌、任务记录等长文档留在 `StoryText`/文档资产，由 `show_info` 按 ID 打开 | 长文档有标题/正文/残影样式和多行排版，放进 Yarn 会充斥转义和引号；分离后也便于后续本地化 |
| D5 | 章节推进、场景加载、谜题判定留在章节控制器/章节模块；Yarn 命令只做状态修改、信息展示和“请求转场” | Yarn 不应知道 Build Settings、场景叠加加载和谜题计数；否则对话脚本会变成不可测试的场景脚本 |
| D6 | `seenDialogue` 以**节点完成**为单位记录，关键行使用显式 `#line:` ID | 节点级去重能避免读档重播一次性剧情冲击；显式行 ID 在改文案、做本地化、接配音时保持稳定 |
| D7 | 现在用**常驻 `DialogueHost`**，目标架构落地后改由 `GameRuntime` 持有同一服务 | 当前七个地点场景没有常驻 `Gameplay.unity`；常驻 Host 先避免每个场景复制 `DialogueRunner`，之后 API 不变 |
| D8 | 对话进行中**不允许存档**；检查点只落在对白结束后的安静点 | YarnSpinner 不提供可靠的“行中间恢复”；强行保存行号会在改文案后失效。暂停保存需拒绝或延后 |
| D9 | 第一版不使用 `LineAdvancer` 的内置输入映射 | 项目使用 Input System + uGUI 点击；自定义呈现器直接处理全屏点击推进，避免额外 Input Actions 与暂停键冲突 |

## 4. 目标结构

### 4.1 资产目录

```text
Assets/
├─ Yarn/
│  ├─ WWG.yarnproject                 # 唯一 Yarn 工程，源文件模式指向下面两个目录
│  ├─ Shared/                         # 集成人维护；跨章节函数说明、公共短节点
│  │  └─ Common.yarn
│  └─ Chapters/
│     ├─ Prologue/                    # 原 S0
│     │  ├─ S0_Exterior.yarn
│     │  ├─ S0_ClubRoom.yarn
│     │  └─ S0_Transition.yarn
│     ├─ NightPhoto/                  # 原 S1
│     │  ├─ S1_ClubRoom.yarn
│     │  ├─ S1_Corridor.yarn
│     │  └─ S1_Bridge.yarn
│     ├─ BridgeRoute/                 # S2
│     │  ├─ S2_Odd4F.yarn
│     │  ├─ S2_Span4F.yarn
│     │  ├─ S2_Even4F.yarn
│     │  └─ S2_ClubRoom.yarn
│     └─ Later/                      # S3/S4 预留
├─ Scripts/
│  └─ Dialogue/
│     ├─ DialogueService.cs           # 启动、停止、节点校验、seen 去重
│     ├─ WwgDialoguePresenter.cs      # uGUI 行呈现器
│     ├─ WwgOptionsPresenter.cs       # 可选的 uGUI 选项呈现器
│     ├─ DialogueCommands.cs          # Yarn 命令注册与转发
│     ├─ DialogueFunctions.cs         # Yarn 只读函数注册
│     ├─ IDialogueContext.cs          # 章节/运行时向对话层暴露的操作
│     └─ DialogueCast.cs              # 说话人 -> 立绘/排版配置（ScriptableObject）
├─ Resources/
│  ├─ Dialogue/
│  │  └─ WwgDialogueHost.prefab       # Canvas + DialogueRunner + 呈现器 + 命令
│  └─ Documents/                      # 可选：长文档 ScriptableObject；或继续用 StoryText
└─ Editor/
   └─ YarnDialogueValidation.cs       # WWG/Yarn/Validate Dialogue
```

- `.yarnproject` 由集成人维护；章节作者只新增/修改自己目录下的 `.yarn`，通过源文件模式自动纳入编译，不需要每次改 `.yarnproject`。
- `Assets/Scripts/Dialogue` 与 `Assets/Yarn` 都是新增目录；第一批不改现有七个场景，也不改 Build Settings。

### 4.2 运行时组成

```text
WwgDialogueHost (DontDestroyOnLoad，首次 Ensure 时从 Resources 实例化)
├─ Canvas (Screen Space Overlay, sortingOrder 高)
│  ├─ Dialogue panel
│  │  ├─ Speaker
│  │  ├─ Body
│  │  ├─ Portrait
│  │  └─ Advance (透明全屏按钮)
│  └─ Options panel（可选）
├─ DialogueRunner
│  ├─ YarnProject -> Assets/Yarn/WWG.yarnproject
│  ├─ BuiltinLocalisedLineProvider（LocaleCode = zh-CN）
│  ├─ InMemoryVariableStorage（只存对话局部变量）
│  ├─ WwgDialoguePresenter
│  └─ WwgOptionsPresenter（可选）
├─ DialogueCommands
└─ DialogueService
```

章节侧：

```text
Hotspot / 场景按钮
  └─▶ S0Controller / S1Controller / S2Controller
        └─▶ DialogueService.Play(nodeId, context)
              ├─ WwgDialoguePresenter：显示行、立绘、等待点击
              ├─ DialogueCommands：修改 GameState / HUD / 信息面板
              └─ onDialogueComplete：标记 seen、处理排队的转场
```

目标架构落地后，`GameRuntime` 持有 `DialogueService`，`StoryDirector` 加载章节预制体；`WwgDialogueHost` 不再需要 `DontDestroyOnLoad`，而是成为 `Gameplay.unity` 的常驻子对象。服务 API 不变。

### 4.3 生命周期

- 首次进入任一游戏地点时，`DialogueService.Ensure()` 检查场景中是否已有 Host；没有则从 `Resources/Dialogue/WwgDialogueHost` 实例化并 `DontDestroyOnLoad`。
- Host 只创建一次；后续地点切屏沿用同一个 `DialogueRunner`，对话局部变量在每次 `Play` 开始时清空。
- 返回标题页前调用 `DialogueService.Shutdown()`：停止当前对话、隐藏 Host、清空上下文；不销毁 `WWG_Save` 或其他存档。
- 暂停、物品详情、信息面板、存读档面板打开时，不允许开始新对话；对话进行时，暂停按钮应被禁用，或先关闭对话再暂停。
- 对话 UI 的 Canvas `sortingOrder` 要高于现有地点 HUD，但低于系统级确认框；具体值在安装 Host 时统一设置，不在各场景单独调整。

## 5. 核心接口

### 5.1 DialogueService

```csharp
namespace WWG
{
    public sealed class DialogueService : MonoBehaviour
    {
        public static DialogueService Instance { get; }
        public bool IsRunning { get; }
        public IDialogueContext Context { get; }
        public event System.Action<string> NodeCompleted;
        public event System.Action<string> DialogueCompleted;

        public static DialogueService Ensure();
        public bool Play(string nodeId, IDialogueContext context, bool once = false);
        public void Advance();
        public void Stop();
        public static void Shutdown();
    }
}
```

规则：

- `Play` 在 `IsRunning`、`context == null`、节点不存在时返回 `false` 并输出可诊断日志；不静默回退到其他节点。
- `once == true` 时先检查 `Context.HasSeenDialogue(nodeId)`；只有整个对话正常完成才通过 `Context.MarkDialogueSeen(nodeId)` 写入，中途停止或场景异常不写。
- `StartDialogue` 返回 `YarnTask`，服务内部使用 `YarnTask.Forget()` 启动；不要用 `async void` 暴露给章节。
- `onNodeComplete` 用于记录跳转过的子节点；`onDialogueComplete` 用于收尾、标记起始节点、处理排队的转场和刷新 HUD。
- 同一时间只允许一个对话。热点在 `IsRunning` 时应被 `UpdateInteractionVisibility` 隐藏或禁用。

### 5.2 IDialogueContext

章节控制器或目标架构中的 `GameRuntime` 实现该接口；Yarn 命令只通过它操作游戏，不直接找 `S0Controller`/`S1Controller`。

```csharp
public interface IDialogueContext
{
    bool HasFlag(string id);
    bool HasItem(string id);
    bool HasSeenDialogue(string nodeId);
    void MarkDialogueSeen(string nodeId);

    int San { get; }
    int Health { get; }
    string Route { get; }
    string View { get; }

    void SetFlag(string id);
    void ClearFlag(string id);
    void AddItem(string id);
    void ApplyShock(string id, int sanDelta, int healthDelta = 0);
    void ChooseRoute(string choice);
    void ChooseView(string choice);

    IEnumerator ShowDocument(string documentId);
    IEnumerator ShowPhoto();

    void RequestCheckpoint();
    void RequestGoTo(string locationId, string variant);
    void RefreshHud();
}
```

- 不向对话层暴露 `GameState` 引用，只暴露读写行为和只读快照；S0/S1 适配器和目标架构 `GameRuntime` 都能实现。
- `ApplyShock` 必须在内部检查 `flag.<id>.shock`，一次生效；Yarn 不直接写 `san`，避免重复播放或读档重扣。
- `ChooseRoute` / `ChooseView` 调用 `GameState` 的锁定方法；已选择时只做校验，不改写。
- `ShowDocument` 返回 `IEnumerator`，Yarn 命令 `yield return` 它，直到信息面板关闭；对话期间模态不结束。
- `RequestGoTo` 只记录待处理目标，在 `onDialogueComplete` 后由章节模块执行；不要在 Yarn 执行到一半时卸载地点场景。
- `RequestCheckpoint` 由章节模块判断当前是否允许存档；对话进行中应拒绝。

S0/S1 在状态迁移完成前可以使用临时适配器实现该接口，把 `SetFlag("flag.s0.board")` 映射到现有 `boardRead` 字段，并把 `seenDialogue` 暂存在适配器自己的 `GameState`/列表里；适配器只存在于迁移期，状态统一后删除。S2 直接使用 `GameState` 实现同一接口。

### 5.3 WwgDialoguePresenter

自定义 `DialoguePresenterBase`，负责：

- `OnDialogueStartedAsync`：显示对话面板，记录模态开始，隐藏热点/HUD 交互。
- `RunLineAsync`：读取 `LocalizedLine.CharacterName` 和 `LocalizedLine.TextWithoutCharacterName.Text`，设置说话人与正文；按 `DialogueCast` 查立绘和排版；等待全屏点击或 `Advance()`。
- `OnDialogueCompleteAsync`：隐藏面板，清除立绘，通知服务结束。
- 处理 `LineCancellationToken`：收到“快速推进/取消”时结束当前等待，不吞掉异常。
- 第一版不做逐字打字机，保持现有“整句立即显示”的手感；后续若加打字机，使用 `IAsyncTypewriter` 或自带协程，不影响 Yarn 节点。

`DialogueCast` 至少包含：

| 字段 | 说明 |
| --- | --- |
| `speaker` | 与 Yarn 行首说话人完全一致，例如 `袁祁`、`唐澈（电话）`、`我`、`系统` |
| `portrait` | 可选；为空时不显示立绘 |
| `speakerColor` | 可选；说话人文字颜色 |
| `textIndent` | 有立绘/无立绘时的正文左边距，复用 S0 的 290/36 规则 |

现有 S0 的立绘 Sprite 在 `Assets/Resources/S0/`，迁移时把映射从 `S0Controller` 移到 `DialogueCast` 资产；S1/S2 没有立绘，保持 `portrait=null`。

### 5.4 信息与长文档

- `DialogueService` 不接管任务记录、背包详情和照片放大；这些仍由章节 HUD 或目标架构的 `InfoService` 负责。
- Yarn 只调用 `<<show_info "documentId">>`；`ShowDocument` 从 `StoryText` 静态表或 `Resources/Documents` 的 ScriptableObject 读取标题、正文和 `residue` 标记。
- 现有 `S0Controller.DirectoryText`、`S1Controller` 的照片/工牌说明、`S2Controller.BuildRecord` 中的长文本，迁移到文档表；Yarn 中不出现整段展板或便签正文。
- `residue=true` 的文档在标题前加 `【残影】`，与 S2 现有样式一致。

### 5.5 Yarn 命令与函数

命令只做“请求”，不直接加载场景或重新计算谜题。

| 命令 | 参数 | 行为 | 阻塞 |
| --- | --- | --- | --- |
| `set_flag` | flag ID | `Context.SetFlag`，随后刷新 HUD | 否 |
| `clear_flag` | flag ID | `Context.ClearFlag` | 否 |
| `give_item` | item ID | `Context.AddItem`，随后刷新背包/HUD | 否 |
| `apply_shock` | `p1` / `p3` / `s4_alone` | 按 ID 一次性扣 SAN/生命并置 flag | 否 |
| `choose_route` | `RouteLight` / `RouteDark` | 锁定路线；已锁定则校验 | 否 |
| `choose_view` | `ViewTogether` / `ViewAlone` | 锁定结尾选择 | 否 |
| `show_info` | document ID | 打开信息面板，等待关闭 | 是 |
| `show_photo` | 无 | 打开旧照片，等待关闭 | 是 |
| `checkpoint` | 无 | 请求章节检查点；不允许时输出日志 | 否 |
| `go_to` | location ID、variant | 记录待转场，对话结束后执行 | 否 |
| `refresh_hud` | 无 | 立即刷新目标/状态/背包 | 否 |

只读函数：

| 函数 | 返回 | 用途 |
| --- | --- | --- |
| `has_flag("id")` | bool | 条件分支 |
| `has_item("id")` | bool | 条件分支/选项可用性 |
| `seen("nodeId")` | bool | 避免重复一次性剧情 |
| `san()` / `health()` | number | 低 SAN 特殊台词、危险提示 |
| `route()` / `view()` | string | 路线/结尾分支；未选择返回空字符串 |
| `chapter()` / `step()` | string | 诊断和特殊分支，尽量少用 |

实现建议：

- 在 `DialogueCommands`/`DialogueFunctions` 中用显式 `AddCommandHandler` / `AddFunction` 注册，不用场景反射查找 `[YarnCommand]`，避免 Host 预制体复制后重复注册或找不到对象。
- 命令名使用小写字母和下划线；Yarn 节点名也使用 `[A-Za-z0-9_]`，不要依赖点号。
- 命令失败时输出 `[Dialogue]` 前缀的警告，包含节点名、命令名和参数，便于在 Console 定位。

### 5.6 节点与行 ID 约定

- 节点名：`<章节前缀>_<地点>_<事件>`，例如 `S0_Exterior_Intro`、`S1_ClubRoom_Box`、`S2_Odd4F_RouteLight`。
- 文件：`Assets/Yarn/Chapters/<章节目录>/<同类节点>.yarn`；一个文件可以包含同场景的多个节点。
- 行 ID：需要跨版本稳定、用于去重或未来本地化的行使用显式标签，例如：

```yarn
袁祁: 可算来了！快把包放下，马上还有五分钟直播开题了。 #line:s0_clubroom_opening_01
```

- 行 ID 使用小写字母、数字和下划线，以 `s0_`、`s1_`、`s2_` 开头；全工程唯一。
- `seenDialogue` 的键与节点名一致；节点完成后由 `DialogueService` 写入，不在 Yarn 中手写 `mark_seen`，避免漏写和重复。
- 需要“只播放一次”的热点对白，调用 `Play(nodeId, context, once: true)`；一次性数值冲击用 `apply_shock` + flag 双重保护。
- 跨章节切换不要在 Yarn 中直接 `<<jump>>` 到另一章节点；由章节模块在对话完成后调用 `StartChapter`/`GameFlow`。同一章内的分支可以使用 `<<jump>>`。

## 6. Yarn 剧本规范

### 6.1 文件头

```yarn
title: S1_ClubRoom_Box
tags: chapter_night_photo location_clubroom
---
<<declare $hint_stage = 0>>

我: 盒子里都是过期的社团招新传单，最上面夹着一张洗出来的照片。 #line:s1_box_01
许圆: 这是去年的活动室。照片里窗帘拉着，桌灯亮着，第五把椅子在白色地胶框里。 #line:s1_box_02
<<show_photo>>
<<set_flag "flag.s1.photoTaken">>
===
```

- 说话人分隔符使用**半角冒号加空格** `: `；正文中的中文冒号、引号和省略号不受影响。不要用全角 `：` 作为说话人分隔符。
- `#line:` 标签放在行末；同一行只能有一个。
- 文件内 `<<declare>>` 的变量只用于本次对话的局部流程，不写入存档；跨对话状态一律用 `GameState` flag。
- 注释使用 `//`；不要把设计备注写成玩家可见行。

### 6.2 条件分支

```yarn
<<if has_flag("flag.p1.complete")>>
许圆: 照片里那个人影，你刚才也看见了吧？ #line:s1_p1_seen_01
<<else>>
许圆: 先把三处对好吧。 #line:s1_p1_todo_01
<<endif>>
```

- 条件只读函数和局部变量；不要在 Yarn 中直接写 C# 谜题计数。
- 低 SAN 台词用 `san() <= 60`，不要用随机数；残影只能给真实线索。
- 路线/结尾分支用 `route() == "RouteLight"` / `view() == "ViewTogether"`；未选择时不要默认成某一路线。

### 6.3 选项

```yarn
-> 四个人一起打开，有福同享有难同当
    <<choose_view "ViewTogether">>
    <<jump S4_ViewTogether>>
-> 我先点开探路，你们站远点
    <<choose_view "ViewAlone">>
    <<jump S4_ViewAlone>>
```

- Yarn 选项适合 S4 的结尾选择、确认框和纯文本分支。
- 4F 入口的“开灯/留暗”仍由场景按钮触发：按钮先调用章节控制器的 `ChooseRoute`，再播放 `S2_Odd4F_RouteLight` / `S2_Odd4F_RouteDark` 节点，保持“开关旁边两个明确交互”的空间感。
- 选项文案必须明确记录选择，不能让“保持黑暗”变成玩家什么都不做的默认结果。

### 6.4 禁止事项

- 不在 Yarn 中加载场景、调用 `SceneManager`、引用 `S0Controller`/`S1Controller`/`S2Controller` 类型。
- 不在 Yarn 中做 P1 三处匹配、P2 四位数校验、P3 三项索引校验；这些留在章节模块，Yarn 只读取 flag 和结果。
- 不把 `GameState` 的 `san`、`inventory`、`flags` 镜像成 Yarn 变量；避免第二套存档。
- 不用 `<<wait>>` 代替转场系统；需要等待的演出由呈现器或章节模块处理。
- 不把长展板、长便签、任务记录正文直接写进 Yarn。

## 7. 存档与状态

### 7.1 GameState 版本

建议把 `GameState.CurrentVersion` 从 1 升到 2，增加最小字段：

```csharp
public string dialogueNode;      // 最近完成的对话节点，可选，用于诊断
public string dialogueLineId;    // 预留；第一版不要求在行中间恢复
```

- `seenDialogue` 继续使用现有 `List<string>`；值为完成过的节点名。
- `dialogueNode` 只记录最近完成的节点，不用于逐行恢复；读档从章节检查点重新进入该节点的**下一个**安全点。
- `GameStateStore.LoadCheckpoint` 对 v1 存档补齐新字段并 `Normalize`；`SaveSlotStore.IsValid` 接受 v1/v2，写回时统一为 v2。
- S0/S1 的旧 `PlayerPrefs` 转换（`ConvertS0/ConvertS1`）继续工作，转换结果补齐 v2 字段；旧键保留到迁移验证通过。
- 如果 S0/S1 的三槽位 payload 也迁成 `GameState`，`SaveSlotStore` 的 `S0`/`S1` 分支必须同时识别旧的 `phase/screen` 格式和新的 `GameState` 格式；只做读取兼容，新写入统一用新格式，避免旧槽位失效。

### 7.2 对话状态

- `DialogueService` 不保存 Yarn 变量。`InMemoryVariableStorage` 只在本次 Host 生命周期内保存局部变量；每次 `Play` 前清空，`Shutdown` 时清空。
- `seenDialogue` 只在 `onDialogueComplete` 正常触发时写入；玩家中途退出、场景加载失败、异常停止都不写。
- 一次性 SAN/生命冲击由 `apply_shock` 的 flag 保护；读档恢复到冲击之后时，不再扣一次。
- 对话进行中禁止存档：`SaveManualCheckpoint` / `CaptureSaveSlot` / 暂停菜单的保存按钮检查 `DialogueService.IsRunning`，返回明确提示或在对话结束后再允许。
- 读档后如果需要播放“到达台词”，由章节模块调用 `PlayOnce`；已经 `seen` 的节点直接跳过，不重播冲击。

### 7.3 模态与计时

- S0/S1/S2 的 `IsGameplayModalOpen`/`ModalOpen` 都要把 `DialogueService.IsRunning` 计入；对话期间暂停 SAN 被动消耗、走廊危险计时和零 SAN 计时。
- `GameplayOverlay` 的暂停、任务记录、物品详情在对话期间禁用或关闭；ESC 不应同时触发暂停和推进对白。
- 对话 UI 使用 `Time.unscaledTime` 处理点击提示和淡入淡出；不要依赖 `Time.timeScale`。

## 8. 迁移顺序

### 阶段 0：接入底座（不改章节内容）

1. 新增 `Assets/Yarn/WWG.yarnproject`，源文件模式指向 `Chapters/**/*.yarn` 和 `Shared/**/*.yarn`；Base Language 设为 `zh-CN`。
2. 新增 `Assets/Scripts/Dialogue/` 下的 `DialogueService`、`IDialogueContext`、`WwgDialoguePresenter`、`DialogueCommands`、`DialogueFunctions`、`DialogueCast`。
3. 制作 `Assets/Resources/Dialogue/WwgDialogueHost.prefab`：Canvas、全屏点击层、说话人/正文/立绘、`DialogueRunner`、`BuiltinLocalisedLineProvider`（LocaleCode=`zh-CN`）、`InMemoryVariableStorage`、命令与函数注册。
4. 写一个只包含测试节点的 `Shared/Common.yarn`，例如 `Test_Hello`，用编辑器菜单或临时调试入口验证：中文显示、点击推进、`show_info`、`set_flag`、`give_item`、`apply_shock`、`go_to` 排队。
5. 新增 `Assets/Editor/YarnDialogueValidation.cs`：校验节点常量存在、`#line` 唯一、命令已注册、文档 ID 存在。菜单名为 `WWG/Yarn/Validate Dialogue`，只在显式执行时运行。
6. 用 Unity MCP 执行 `refresh_unity(compile="request", scope="all", wait_for_ready=true)`，检查 Console；不改现有七个场景和 Build Settings。

验收：编译无新错误；测试节点能在空场景或当前章节中显示并完成；Host 只创建一次；返回标题页不残留对话 UI。

### 阶段 1：S0 对白迁移

1. 把 `S0Controller` 的开场、记忆点、熄灯转场文本拆到 `Chapters/Prologue/*.yarn`，节点名用 `S0_...`。
2. `S0Controller` 实现 `IDialogueContext`（或临时 `S0DialogueContext` 适配器），保留现有 `boardRead/chairRead/signRead/timerRead/guideRead`；`SetFlag`/`HasFlag` 映射到这些字段，迁移完成后再换成 `GameState`。
3. 删除 `S0Controller` 内的私有 `Line`、`Queue`、`Say`、`ContinueDialogue`、`ShowInfo`、字体注入和立绘查表；热点改为调用 `DialogueService.Play(...)`。
4. 把展板、定时箱、导览稿、楼层牌正文迁到 `StoryText`/文档表，Yarn 用 `show_info` 引用。
5. 保留 S0 的 `PlayerPrefs` 检查点逻辑；对话进行中禁止手动存档。

验收：`Title → S0 楼外 → 5F 走廊 → 活动室 → 4F 连廊 → 活动室 → 熄灯 → 照片` 全流程可用；立绘、说话人、排版与旧版一致；Console 无 Yarn 缺行/缺命令警告；S0 检查点可存读。

### 阶段 2：S1 对白迁移

1. 把盒中照片、P1 完成、工牌、玻璃残影、走廊准备、休息、4F 到达等文本拆到 `Chapters/NightPhoto/*.yarn`。
2. `S1Controller` 实现 `IDialogueContext`（或临时适配器）；P1 匹配、SAN/生命、提示梯度仍留在控制器。
3. 提示梯度改为控制器在 30/60 秒时调用 `Play("S1_ClubRoom_Hint_...", once: true)`；提示节点内部根据 `san()`、匹配计数 flag 分支。
4. 删除 S1 的私有 `Line` 队列、`Say/ContinueDialogue/ShowInfo`、字体注入；`InspectPhoto/InspectBadge` 改为调用 `show_photo`/`show_info` 或保留 UI 方法。
5. 一次性冲击用 `apply_shock("p1", -25)`，由 `flag.p1.shock` 保护；不允许在读档后重扣。

验收：S1 盒中照片、P1 三项、工牌、夜间走廊、4F 入口均可用；照片/工牌/ SAN/生命检查点恢复；一次性冲击不重播；旧 S1 检查点迁移仍可读。

### 阶段 3：S2 对白迁移

1. 把 S2 入口通话、路线 A/B 对白、资料柜便签、抽屉后电话、提示文本拆到 `Chapters/BridgeRoute/*.yarn`。
2. `S2Controller` 实现 `IDialogueContext`，直接读写 `GameState`；`ChooseRoute`/`ChooseView` 使用现有锁定方法。
3. 4F 入口的两个场景按钮保持现状：按钮先决定路线并写 `GameState`，再播放对应 `S2_Odd4F_RouteLight` / `S2_Odd4F_RouteDark`。
4. `show_info` 的文档表包含资料柜便签、使用说明、残影文本；`residue` 标记控制标题前缀。
5. 删除 S2 的 `DialogueLine`、队列、`Say/ContinueDialogue`；`Update` 的模态判断加入 `DialogueService.IsRunning`。
6. `s2.intro`、`s2.cabinet` 等一次性节点改用 `PlayOnce`/`seen`；不再手写 `MarkDialogueSeen` 条件。

验收：`BridgeOdd4F` 开灯/留暗都能取得 2741；资料柜抽屉、借用便签、分工表残页、返回 5F 和检查点正常；读档不重播一次性对白；路线标记不被后续开灯改写。

### 阶段 4：接入目标运行时

- `GameRuntime` 持有 `DialogueService`，`ChapterModule` 实现 `IDialogueContext`；`StoryDirector` 在章节切换时先 `DialogueService.Stop`，进入新章再 `Play`；只有返回标题页才调用 `Shutdown`。
- `WwgDialogueHost` 从 `DontDestroyOnLoad` 移入 `Gameplay.unity`，`DialogueService.Ensure()` 优先查找场景实例。
- `GameplayOverlay` 去掉 `s0/s1` 分支，改为依赖 `GameRuntime` 和 `GameState`；背包槽按 `GameState.inventory` 构建，不再硬编码照片/工牌。
- S0/S1 的临时 `IDialogueContext` 适配器删除，章节状态统一进入 `GameState`。

验收：新入口与旧入口均可 Play；对话服务不再依赖具体章节类型；`GameplayOverlay` 不再引用 `S0Controller`/`S1Controller`。

### 阶段 5：清理

- 删除三个控制器中的对白残留、重复的 `Line`/`DialogueLine` 结构、私有队列和 `ShowInfo` 复制；用全仓搜索确认没有遗漏。
- 删除不再使用的旧对话面板对象和场景字段；保留旧场景直到新流程验收通过，删除单独提交。
- 更新 `架构规范.md`、`目录结构组织.md`、`运行流程.md`、`程序规范文档.md`、`AGENTS.md` 和场景工作流 skill。
- 若以后启用 TMP/Unity Localization，再单独评估；不在本次迁移中引入。

## 9. 验收标准

### 9.1 底座验收

- `WWG.yarnproject` 能编译，Console 无 Yarn 编译错误。
- `WwgDialogueHost` 只存在一个；返回标题页后对话 UI 不残留、不响应点击。
- 中文显示正常，说话人/正文/立绘布局与现有 S0 一致。
- `show_info` 阻塞直到关闭；`go_to` 在对话结束后才执行；`apply_shock` 只生效一次。
- 节点不存在、命令未注册、文档 ID 缺失时给出明确日志，不静默回退。

### 9.2 章节验收

- S0：完整白天流程、三个记忆点、熄灯转场、进入 S1。
- S1：照片、P1、工牌、夜间走廊、4F 入口、检查点读取。
- S2：两条路线都取得 2741；资料柜、借用便签、分工表残页、5F 安全屋、检查点读取。
- 所有一次性剧情冲击在读档后不重复；SAN/生命耗尽后关键物品与谜题进度不丢失。
- 对话期间不扣被动 SAN，不触发暂停，不允许存档。
- 旧 S0/S1 检查点与三槽位存档仍可读取；v1→v2 迁移失败时提示原因，不进入错误章节。

### 9.3 代码边界验收

- `Assets/Scripts/Dialogue` 不引用 `S0Controller`/`S1Controller`/`S2Controller`。
- `S0Controller`/`S1Controller`/`S2Controller` 不再包含 `Line`/`DialogueLine` 队列和私有 `Say`。
- Yarn 文件中不出现场景加载、谜题校验和长文档正文。
- `GameState` 仍是唯一持久状态；`InMemoryVariableStorage` 不被序列化进存档。

## 10. 风险与对策

| 风险 | 影响 | 对策 |
| --- | --- | --- |
| 内置呈现器依赖 TMP | 直接使用包内预制体会缺中文字体或与现有 uGUI 不一致 | 自定义 `WwgDialoguePresenter`，复用 uGUI `Text` 和系统 CJK 字体；不启用 TMP 路径 |
| 本地化 Locale 不匹配 | 运行时找不到行，显示缺行警告 | `.yarnproject` Base Language 与 `BuiltinLocalisedLineProvider.LocaleCode` 都显式设为 `zh-CN`；在非中文系统上测试 |
| Yarn 变量成为第二套状态 | 读档后状态分叉、SAN/flag 被重置 | 只用只读函数和命令；`InMemoryVariableStorage` 只存局部变量；每次 `Play` 清空 |
| 对话中途切场景 | 呈现器对象被卸载、对白丢失 | Host 常驻；`go_to` 排队到对话结束；章节模块不在对话中加载场景 |
| 行 ID 随文案变化 | `seenDialogue`、本地化、配音引用失效 | 关键行显式 `#line:`；节点级 seen；行 ID 全工程唯一校验 |
| 暂停/存档与对话竞争 | 对话框下仍可暂停、保存到半句状态 | 对话期间禁用暂停/存档；`IsGameplayModalOpen` 计入 `DialogueService.IsRunning` |
| 一次性冲击重复扣 SAN | 读档或重播导致数值错误 | `apply_shock` 使用 flag 幂等；`PlayOnce` 节点级去重；读档只恢复不重放 |
| 七个地点场景重复安装 Host | 多个 `DialogueRunner`、重复注册函数 | Host 单例 `Ensure()`；场景中已有实例则复用；不把 Host 复制进每个场景 |
| 章节并行编辑 `.yarnproject` | 合并冲突 | `.yarnproject` 由集成人维护；使用源文件模式；章节作者只改自己目录的 `.yarn` |
| 包升级破坏 API | 迁移中途编译失败 | 锁定 `v3.2.8` 与 `packages-lock.json`；迁移完成前不升级；升级单独提交并跑全流程 |

## 11. 第一批改动清单

建议按以下顺序提交，第一批不碰现有场景和章节内容：

- [x] `Assets/Yarn/WWG.yarnproject` + `Assets/Yarn/Shared/Common.yarn`（测试节点）。
- [x] `Assets/Scripts/Dialogue/DialogueService.cs`
- [x] `Assets/Scripts/Dialogue/IDialogueContext.cs`
- [x] `Assets/Scripts/Dialogue/WwgDialoguePresenter.cs`
- [x] `Assets/Scripts/Dialogue/DialogueCommands.cs`
- [x] `Assets/Scripts/Dialogue/DialogueFunctions.cs`
- [x] `Assets/Scripts/Dialogue/DialogueCast.cs` + 立绘配置资产
- [x] `Assets/Resources/Dialogue/WwgDialogueHost.prefab`
- [x] `Assets/Editor/YarnDialogueValidation.cs`
- [x] 用 Unity MCP 编译、检查 Console、在一个临时场景或当前章节验证测试节点。

第一批验收后再开始 S0 内容迁移；S0、S1、S2 分别提交，避免一次改完整仓库。

## 12. 待定问题

1. **长文档载体**：继续使用 `StoryText.cs` 静态表，还是新增 `Resources/Documents/*.asset` ScriptableObject？建议文档数量超过 10 个时用 ScriptableObject；第一批可先用静态表降低门槛。
2. **选项呈现器**：第一版是否需要 Yarn 选项 UI？S4 的结尾选择需要；若暂不做，可先用场景按钮并在后续补 `WwgOptionsPresenter`。4F 路线选择保持场景按钮。
3. **打字机效果**：现有对白是整句显示；是否在后续加入逐字显示，需要单独评估中文断句和点击加速。
4. **单工程拆分**：当前单 `WWG.yarnproject` 适合 Demo；若章节数量和编译时间显著增长，再评估按章节拆工程，但需要同时迁移节点名和 `seenDialogue` 键。
5. **目标架构时间点**：`GameRuntime`/`StoryDirector` 落地前，`DialogueService` 的常驻 Host 是过渡方案；落地后 Host 是否保留为 `Gameplay.unity` 子对象，需要在集成时确认。
