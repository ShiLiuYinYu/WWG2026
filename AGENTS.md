# AGENTS.md — WWG

本项目是 Unity 6 的 2D URP 点击式微恐怖解谜游戏《熄灯之后：48H》。当前已制作 Title、S0、S1、S2、S3 与 S4；S0 有 4 个地点场景，S1 有 3 个，S2 有 4F 三处地点和 5F 安全屋视图，S3 有 3F 机房，S4 有独立的 5F 终章活动室，均已加入 Build Settings。旧 `SampleScene` 已不存在，也不在 Build Settings 中。S1 包含 P1、工牌、SAN 与阶段检查点；S2 包含 P2、`RouteLight / RouteDark`、借用便签与分工表残页；S3 包含 P3 与 U 盘；S4 包含工牌归档、休息、`ViewTogether / ViewAlone` 和两种 24H 结尾。各章仍保留分章控制器与检查点，完整 Demo 统一存档及目标 `GameRuntime` 架构尚未实现。不要把设计文档中的目标功能描述成已完成实现。

## 项目依据

- `Document/《熄灯之后：48H》——午夜GameJam怪谈.md`：原始故事设定。
- `Document/《熄灯之后：48H》前24H Demo剧本与关卡设计.md`：当前前 24H Demo 的可制作方案、场景、谜题、分支与完成判定。制作相关功能前先读对应章节。
- `Document/Image/`：现有机制、系统和 UI 草图。
- `Document/Program/`：WWG 程序规范、目录、架构和运行流程；场景命名与维护以此为准。
- `G:\学校文件\TEMP\实验楼取景\`：本项目实验楼的实地取景照片，共 20 张，包含正门、游戏开发活动室、楼内走廊、各层楼梯口与连廊。制作对应场景前查看照片，参考建筑形态、材质、空间透视和日间光照。该目录在项目仓库外，不要假定其他设备上也存在。照片中的连廊是开放式的；游戏所需的 4F 玻璃连廊及其他剧情布置以 Demo 文档为准，不直接照搬照片中的空间细节。

## 技术现状

- 当前项目文件记录 Unity `6000.3.28f1`、URP `17.3.0`（2D）、Input System `1.20.1`、uGUI `2.0.0`。制作前以实际连接的 WWG 编辑器和 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json` 为准。
- `dev.yarnspinner.unity` 3.2.8 已接入：`Assets/Yarn/WWG.yarnproject`、`Assets/Scripts/Dialogue/` 共享服务与自定义 uGUI 呈现器、`Assets/Resources/Dialogue/WwgDialogueHost.prefab`、`WWG/Yarn/Validate Dialogue` 校验菜单已存在；S0–S4 的对白位于 `Assets/Yarn/Chapters/`，五个控制器都实现 `IDialogueContext`。S2–S4 仍使用各自场景 UI 和 `GameState`，`GameplayOverlay` 与 `GameRuntime` 的目标架构合并（重构阶段 4–5）尚未实施。接入方案和迁移顺序见 `Document/Program/YarnSpinner接入与对话系统重构.md`。
- 项目已安装 `com.coplaydev.unity-mcp`。需要操作或检查 Unity 编辑器时，使用 Unity MCP，并先确认连接到 **WWG** 实例。代理只完成 Unity MCP 能完成的任务；需要 Computer Use 的步骤不在本机代做，整理成明确的待办交给用户，详见下一条约束。
- 当前没有 `WjSceneBuilder`、`WJ/Setup` 菜单、`WJ.*` 命名空间或 WJ 的预制体。不要照搬 WJ 专属调用。
- S0/S1 的七个地点场景已引用 `Assets/Prefabs/UI/GameplayOverlay.prefab`，包含暂停菜单、右下角背包按钮、5×5 背包格网和物品详情。标题页及各章暂停菜单共用 `Assets/Resources/UI/SaveSlotPanel.prefab` 的三槽位存读档界面，槽位仍承载各章节检查点，不是完整 Demo 存档。设置窗口同样是共用预制体 `Assets/Resources/UI/SettingsPanel.prefab`（音乐/音效两条音量滑条，音量键 `WWG_MusicVolume` / `WWG_SfxVolume` 由 `Assets/Scripts/Audio/WwgAudioSettings.cs` 读写），重建菜单为 `WWG/UI/Build Settings Window`。调整共享游戏内 UI 时改预制体，显式安装命令为 `WWG/UI/Install Gameplay Overlay`。
- S2–S4 现有独立场景分别为 `Assets/Scenes/S2.unity`、`S2/S2_BridgeSpan4F.unity`、`S2/S2_BridgeEven4F.unity`、`S2/S2_ClubRoom.unity`、`S3.unity`、`S4.unity`，均保留迁移前 GUID；章节控制器仍各自运行。`GameStateStore` 覆盖 S2–S4，但 S0/S1 尚未统一为完整 Demo 存档。`WWG/Dev/Play S2`、`Play S3`、`Play S4` 使用不写玩家存档的编辑器状态样本。场景生成菜单只在全部目标场景不存在时使用，已有人工维护场景不得重建。
- 重构目标改为**阶段场景差分**：S0–S4 的 `.unity` 已按阶段独立可编辑，公共静态空间将逐地点提取为基础 Prefab，阶段专属物件、热点、照明及位置 override 留在各自场景。`StoryIds.LocationScene(chapterId, locationId)` 已映射当前文件；基础 Prefab 差分和 `GameRuntime` 迁移尚未实施。不要按旧方案把各阶段物件聚合进一个地点场景，也不要删除现有阶段场景或改其 GUID。详见 `Document/Program/架构重构设计.md`。
- 编辑器中直接从 S2 任一地点或 S3/S4 场景按 Play，会使用 `ChapterPreviewStates` 的内存默认配置；开发菜单沿用相同样本。该预览状态不写玩家检查点或槽位，正式交接仍优先。构建版仍需有效交接或存档。

## 制作约束

- Demo 是固定视角的 2D 场景点击解谜：点击热点调查或切屏，背包道具用于场景热点；不做自由行走、战斗或追逐。
- 可探索范围按 Demo 文档：楼外、单数栋 5F 活动室与走廊、4F 连廊两端、单数栋 3F 机房。5F 活动室是夜间安全屋；23:30 熄的是公共区域照明，室内设备不断电。
- 保留 P1 旧照片对位、P2 双栋四位数、P3 三项证据恢复旧工程，以及 `RouteLight / RouteDark` 和 `ViewTogether / ViewAlone` 两次选择。两条调查路线和两种结尾都应可通关。
- 低 SAN 残影可提供真实线索，不能给出随机错误答案。SAN 或生命耗尽后可继续，关键物品与谜题进度不能丢失。
- 当前真实场景、社团留下的资料、低 SAN 残影须能被玩家区分。不要添加赛事监控、强制汇报或统一上传设定。
- 每次制作决策都记录到 `Document/Requirement/决策记录.md`，即使当次没有单独需求文件；备忘录文案清单见 `Document/Requirement/备忘录内容.md`，美术方向见 `Document/Requirement/美术方向.md`。
- 程序创建的资源、游戏对象和预制体只创建一次；已有对象保留可在编辑器手动调整的位置。`WWG/UI/Apply temp1 Layout` 仅用于给现有资源补缺失结构与指定样式，不得重建已人工调整的布局。
- 任务边界：只完成 Unity MCP 能完成的步骤，包括脚本、对白和文档编辑，以及 MCP 提供的编辑器检查、场景/预制体修改、编译刷新、Console 读取、Play 模式切换与截图。需要 Computer Use 的步骤（在 Unity 窗口内拖拽或点击、操作 Game 视图热点、控制系统或第三方窗口、需要人工确认的画面判断等）不自行执行：把目标、具体操作、预期结果和验证标准写成清晰的待办交给用户手动完成，并说明该部分为何无法由 MCP 完成。Unity MCP 连不上 WWG 时不要改走 Computer Use 兜底，先报告连接问题。

## 场景与 UI 工作

修改或新增 Unity 场景、UI、预制体及相关交互时，遵循 `.opencode/skills/unity-scene-workflow/SKILL.md` 的流程。配置文件为 `opencode.json`；Unity MCP 服务需在目标编辑器中运行后才能连接。

已有独立透明抠图的可交互物，悬停提示使用黄色边缘高亮；图片留在画面层，透明热点留在交互层。未抠图物件暂保留原提示，逐个迁移。接入方式见 `Document/Requirement/交互高亮规范.md`。
