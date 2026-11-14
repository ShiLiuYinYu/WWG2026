# AGENTS.md — WWG

本项目是 Unity 6 的 2D URP 点击式微恐怖解谜游戏《熄灯之后：48H》。当前仍是项目骨架阶段：`Assets/Scenes/SampleScene.unity` 是唯一加入 Build Settings 的场景，尚未实现场景、UI、谜题和存档系统。不要把设计文档中的目标功能描述成已完成实现。

## 项目依据

- `Document/《熄灯之后：48H》——午夜GameJam怪谈.md`：原始故事设定。
- `Document/《熄灯之后：48H》前24H Demo剧本与关卡设计.md`：当前前 24H Demo 的可制作方案、场景、谜题、分支与完成判定。制作相关功能前先读对应章节。
- `Document/Image/`：现有机制、系统和 UI 草图。
- `G:\学校文件\TEMP\实验楼取景\`：本项目实验楼的实地取景照片，共 20 张，包含正门、游戏开发活动室、楼内走廊、各层楼梯口与连廊。制作对应场景前查看照片，参考建筑形态、材质、空间透视和日间光照。该目录在项目仓库外，不要假定其他设备上也存在。照片中的连廊是开放式的；游戏所需的 4F 玻璃连廊及其他剧情布置以 Demo 文档为准，不直接照搬照片中的空间细节。

## 技术现状

- 当前项目文件记录 Unity `6000.3.28f1`、URP `17.3.0`（2D）、Input System `1.20.1`、uGUI `2.0.0`。制作前以实际连接的 WWG 编辑器和 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json` 为准。
- 项目已安装 `com.coplaydev.unity-mcp`。使用 Unity MCP 时先确认连接的是 **WWG** 的编辑器实例；多项目同时打开时不要误操作 WJ。
- 当前没有 `WjSceneBuilder`、`WJ/Setup` 菜单、`WJ.*` 命名空间或 WJ 的预制体。不要照搬 WJ 专属调用。

## 制作约束

- Demo 是固定视角的 2D 场景点击解谜：点击热点调查或切屏，背包道具用于场景热点；不做自由行走、战斗或追逐。
- 可探索范围按 Demo 文档：楼外、单数栋 5F 活动室与走廊、4F 连廊两端、单数栋 3F 机房。5F 活动室是夜间安全屋；23:30 熄的是公共区域照明，室内设备不断电。
- 保留 P1 旧照片对位、P2 双栋四位数、P3 三项证据恢复旧工程，以及 `RouteLight / RouteDark` 和 `ViewTogether / ViewAlone` 两次选择。两条调查路线和两种结尾都应可通关。
- 低 SAN 残影可提供真实线索，不能给出随机错误答案。SAN 或生命耗尽后可继续，关键物品与谜题进度不能丢失。
- 当前真实场景、社团留下的资料、低 SAN 残影须能被玩家区分。不要添加赛事监控、强制汇报或统一上传设定。

## 场景与 UI 工作

修改或新增 Unity 场景、UI、预制体及相关交互时，遵循 `.opencode/skills/unity-scene-workflow/SKILL.md` 的流程。配置文件为 `opencode.json`；Unity MCP 服务需在目标编辑器中运行后才能连接。
