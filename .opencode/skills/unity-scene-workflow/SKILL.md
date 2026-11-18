---
name: unity-scene-workflow
description: 在 WWG 项目中创建或修改 Unity 2D 场景、点击热点、UI、预制体及相关交互，并用 Unity MCP 检查编译和 Play 模式效果时使用。
---

# WWG 场景与 UI 工作流

当前 Title、S0、S1 和 S2 已按地点拆分；场景清单和运行流程见 `Document/Program/`。旧 `SampleScene` 已不存在。先查看实际资产，再依 `Document/《熄灯之后：48H》前24H Demo剧本与关卡设计.md` 的对应章节制作。不要调用 WJ 项目的场景生成菜单或代码。

## 开始前

1. 确认 Unity MCP 连接的是 **WWG** 编辑器实例；若同时连接多个实例，明确指定 WWG。检查当前场景、Build Settings 和相关资产。
2. 查看任务涉及的 Demo 文档章节；场景流程见 §1、§3、§6，谜题见 §4，SAN/存档见 §5。原始设定见 `Document/《熄灯之后：48H》——午夜GameJam怪谈.md`。
3. 确认目标场景或预制体是否已经存在。已有资产先检查引用和绑定，再改动；新增资产应加入正确的场景流程与 Build Settings。
4. S0/S1 每个固定地点是独立 `.unity`，切屏状态由章节控制器传递。旧 `Build S0/S1 Scene` 脚手架不会覆盖现有场景；`Split Location Scenes` 只供未拆分原始场景一次性迁移，不要在已拆分项目上运行。
5. S0/S1 七个地点共用 `Assets/Prefabs/UI/GameplayOverlay.prefab` 的场景实例。布局改动优先修改预制体；`WWG/UI/Install Gameplay Overlay` 只在显式执行时补装缺失实例，不在域重载时改写场景。
6. S2 的 `S2.unity`、`S2/S2_BridgeSpan4F.unity`、`S2/S2_BridgeEven4F.unity` 和 `S2/S2_ClubRoom.unity` 已存在；S3、S4 入口分别是 `S3.unity`、`S4.unity`。只编辑目标地点；`WWG/Setup/Build S2 Location Scenes` 在任何目标场景已存在时拒绝执行。编辑器样本入口为 `WWG/Dev/Play S2`，不写玩家检查点。
7. S2 入口墙上的开关只切换照明。首次在连廊中段点击右侧玻璃并确认当前照明下的调查方式时，才写入 `RouteLight` 或 `RouteDark`；之后灯可再次开关，路线标记不改写。
8. 标题页与 S0/S1/S2 暂停菜单共用 `Assets/Resources/UI/SaveSlotPanel.prefab` 的三槽位存读档界面；修改布局时编辑预制体。S0 时间推进和 S1→S2 交接不使用全屏黑色阶段提示。
9. 对白文本在 `Assets/Yarn/Chapters/`，由常驻 `WwgDialogueHost` 和 `DialogueService` 播放；不要在场景里新建或恢复旧对话面板，也不要往章节控制器里加私有 `Line` 队列。S2–S4 仍保留各自的暂停、背包和任务记录 UI。S4 终章场景由已有 5F 活动室视图复制一次，`WWG/Setup/Build S4 Finale Room` 不得用于重建已维护场景。
10. 架构重构已确定保留每阶段独立的地点 `.unity`。同一地点跨阶段的稳定静态结构可逐场景提取为基础 Prefab；阶段专属物件、热点与人工调整位置留在各自场景。现有场景与 GUID 不重建，修改基础 Prefab 前核对所有引用场景，不能把某阶段的 override Apply 到基础资产。差分目标尚未在场景资产中实施。

## 修改方式

- 布局、图片、文字和热点范围：在 Unity 编辑器中修改目标场景或预制体并保存。可交互线索尽量使用独立图层或 UI，避免烘焙进背景。
- 交互或状态逻辑：修改对应 C# 脚本，等待 Unity 编译完成，再检查 Console。新增脚本时按本项目实际目录和程序集组织，不预设 WJ 的命名空间或脚手架。
- 对白与分支：编辑对应章节的 `.yarn`，节点名使用 `S0_`/`S1_`/`S2_` 前缀，关键行保留唯一 `#line:`；长文档加到 `DialogueDocuments`。一次性数值冲击用 `apply_shock`，不要把 P1/P2 判定搬进 Yarn。
- 新场景：按固定视角点击切屏设计，确保热点、切屏箭头和背包交互可用。夜间同一地点应与白天空间关系一致；公共照明熄灭时室内电脑仍有电。
- 场景提示：未悬停或选中物件、未主动打开调查界面时，不显示常驻的地点标签、线索、操作说明或保存反馈；具体规范见 `Document/Requirement/交互高亮规范.md`。
- 谜题/分支：P2 两栋海报的读取顺序是“先单数栋、后偶数栋；每栋从连廊口向外”，结果为 `2741`。开灯和留暗都能取得数字；存档和读取应保留已做选择与关键进度。

## 验证

1. 脚本或资产改动后调用 `refresh_unity(compile="request", scope="all", wait_for_ready=true)`，再用 `read_console(types=["error","warning"], format="detailed")` 检查新错误。先解决编译错误，再验证交互。改对白后执行 `WWG/Yarn/Validate Dialogue`。
2. 场景/UI 改动需进 Play 模式检查目标界面，点击实际热点、按钮和切屏入口；用 `manage_camera(action="screenshot", include_image=true)` 查看画面，结束后退出 Play 模式。
3. UI 使用 Screen Space Overlay 时，截图不要指定某个 Camera，以免漏掉 UI。若编辑器未连接，记录未验证的部分和连接前提。
4. 涉及 P1/P2/P3、分支或存档时，验证两条 4F 路线均可继续、两种旧工程查看选择均可到达结尾；SAN/生命耗尽和读档不能丢失关键物与选择标记。只验证本次改动涉及的路径。

## 文件速查

| 内容 | 路径 |
| --- | --- |
| Demo 剧本和关卡规格 | `Document/《熄灯之后：48H》前24H Demo剧本与关卡设计.md` |
| 原始世界观 | `Document/《熄灯之后：48H》——午夜GameJam怪谈.md` |
| 机制、系统、UI 草图 | `Document/Image/` |
| 当前 Unity 场景 | `Assets/Scenes/Title.unity`、`S0.unity`、`S0/S0_*.unity`、`S1.unity`、`S1/S1_*.unity`、`S2.unity`、`S2/S2_*.unity`、`S3.unity`、`S4.unity`；旧 `SampleScene` 已移除 |
| 共享游戏内 UI | `Assets/Prefabs/UI/GameplayOverlay.prefab`、`Assets/Scripts/UI/GameplayOverlay.cs` |
| 当前 Build Settings | `ProjectSettings/EditorBuildSettings.asset` |
| 程序规范与场景流程 | `Document/Program/` |
