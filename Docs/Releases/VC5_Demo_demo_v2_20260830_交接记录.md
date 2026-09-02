# VC5 Demo `demo_v2_20260830` 交接记录

封存日期：2026-09-02
代码分支：`main`
版本标识：Git Tag `demo_v2_20260830`
Unity：`2021.3.33f1c1 (682b9db7927c)`

## 交接目的

本文档供下一次 Codex 会话快速恢复项目上下文。Tag 创建后，用 `git rev-list -n 1 demo_v2_20260830` 获取该版本的准确提交；不要依赖旧聊天记录或构建目录判断源码版本。

## 当前可玩范围

- 启动场景只有 `Assets/TcgEngine/Scenes/Menu/Menu.unity` 和 `Assets/TcgEngine/Scenes/Game/Game.unity`，`Menu.unity` 为索引 0。
- 玩家从 `VC5 Demo 对战`面板选择自己和 AI 的卡组，进入本地 Solo VS AI；不需要双开 Unity。
- 可选卡组：机动突击 B、旧射击压制 C、射击压制 C3。三套卡组均可由玩家或 AI 使用。
- Demo Solo 固定玩家先攻。普通 Solo、Adventure、Multiplayer、HostP2P 的原先后攻与联网消息语义不应被改动。
- 棋子不能点击自由移动，只能由卡牌或棋子技能移动；任一实际棋盘格最多存在一个单位。
- Demo 攻击牌默认单向伤害，不继承模板的自动反击。
- 率先达到 9 分获胜；需要抽牌但牌库为空时立即判负。

## 本版主要成果

1. 独立实现射击压制 C3：3 名棋子、8 种卡牌、20 张固定构筑，旧 B/C 不被覆盖。
2. C3 多数卡牌拖到己方棋子即可自动选敌并结算；战术移动、强行军额外选择落点。
3. 预览与结算共用规则计算，显示路线、落点、范围、目标、预计伤害和移动被动。
4. V2.1 六状态实操教学引导玩家完成`移动射击`：固定首抽、拖向游骑射手、查看预览、松开结算，再介绍法力、得分和回合流程。
5. 教学结束前暂停本地 AI；最终关闭或教学对象销毁时必须释放门禁，不能让 AI 永久停住。
6. 已强化 AI 卡牌/技能横幅、目标高亮、最近行动，以及比分、得分区人数、结算预测和胜负原因反馈。
7. 已接入 C3 八张功能卡图、C3/B 六张角色图和 Windows/Android 共用应用图标。

## 关键代码入口

- Demo 数据与旧 B/C：`Assets/TcgEngine/Scripts/Data/Vc5DemoBootstrap.cs`
- C3 注册与牌组：`Assets/TcgEngine/Scripts/Data/Vc5C3Bootstrap.cs`
- C3 规则、效果与预览：`Assets/TcgEngine/Scripts/Effects/vc5/Vc5C3Rules.cs`、`EffectVc5C3Action.cs`、`Vc5C3Preview.cs`
- 实际棋盘与占格：`Assets/TcgEngine/Scripts/Effects/vc5/Vc5DemoGrid.cs`
- AI：`Assets/TcgEngine/Scripts/AI/AIPlayerVc5Demo.cs`、`Vc5DemoAIPlanner.cs`
- 教学：`Assets/TcgEngine/Scripts/UI/Vc5DemoTutorialOverlay.cs`
- C3 预览 UI：`Assets/TcgEngine/Scripts/UI/Vc5C3PreviewOverlay.cs`、`Vc5C3PreviewGraphic.cs`
- 对局反馈：`Assets/TcgEngine/Scripts/UI/Vc5DemoBattleFeedback.cs`、`Vc5DemoBattleFeedbackModel.cs`
- Demo 菜单启动：`Assets/TcgEngine/Scripts/Menu/Vc5DemoMatchSetup.cs`
- 构建入口：`Assets/TcgEngine/Scripts/Editor/Vc5DemoBuildPipeline.cs`

## 下一会话必读顺序

1. `.codex/rules/default.rules`
2. 本交接文档
3. `Docs/VC5_Demo发布计划与进度.md`
4. `Docs/VC5_已实现可玩卡牌与卡组.md`
5. `Docs/VC5_初版Demo简易卡组方案_20260701.md` 的 C3 章节
6. `Docs/VC5_AI行动逻辑.md`
7. `Docs/VC5_Demo启动与AI对战流程.md`
8. `战场女武神5最新规则与卡牌-20260621.md`
9. 新一轮玩家反馈原文

## 验证基线

- 封存提交前通过当前 Unity Editor 与 Unity MCP 新鲜运行 `VC5DemoGate 186/186`，失败 0、跳过 0，耗时约 `25.90` 秒；测试后 Console 0 error。
- 教学专项 `4/4`；真实 `Menu -> C3 Solo -> Game` 运行测试 `1/1`，Unity 原生结果 `Passed`，当时 Console 0 error。
- 九种 B/C/C3 AI 组合的自动对局已纳入门禁；自动结果证明流程可结束，不证明 AI 强度、平衡或观感。
- 2026-08-30 的真实 `Menu -> C3 Solo -> Game` 运行测试仍是最近一次完整启动链路证据；本次封存没有重跑 PlayMode、Android 真机或双开 P2P。

## Windows 构建

- 构建入口：`Builds/Direct/Windows/ValkyriaChronicles5/ValkyriaChronicles5.exe`
- 构建日志：`Builds/Direct/WindowsBuild-20260830.log`，记录 `Build Finished, Result: Success`。
- 完整目录约 `219.79 MiB`；排除 `ValkyriaChronicles5_BurstDebugInformation_DoNotShip` 后约 `219.57 MiB`。
- EXE SHA-256：`6C3966BBAA6D0E7F47D0EBE969EFA91E3195484FEE76F3401E5D870ED3A12C75`。
- 该目录已经用于玩家试玩。`Builds` 被 Git 忽略，因此二进制不属于 Tag；需要复现时必须从 Tag 重新构建。

## 未完成与风险

- 下一轮玩家反馈尚未整理；先保存反馈原文，再区分已确认 Bug、体验意见和准备采用的方案。
- Android v2 未发布。旧 `VC5Demo_v1_20260823.apk` 不是本版本，不能用于 v2 验收。
- Android 手机/平板的安装、横屏旋转、触控拖牌、刘海和系统导航栏遮挡未完成 v2 真机验收。
- 现有联网底层要求不被破坏，但本版本没有重新完成双开 P2P 人工回归。
- 外部角色图片在更大规模或商业发布前需要确认授权；替换运行时图片时保留对应 `.meta`。
- 自动测试不能代替教学是否易懂、AI 表现节奏、卡牌平衡和拖拽手感的玩家验收。

## 下一会话开场模板

```text
这是 VC5 在 demo_v2_20260830 之后的下一轮开发。

项目：E:\unity\git_newest\ValkyriaChronicles5
基线 Tag：demo_v2_20260830

开始前请：
1.读取 .codex/rules/default.rules。
2.检查 git status、当前分支和 demo_v2_20260830 指向的提交。
3.读取 Docs/Releases/VC5_Demo_demo_v2_20260830_交接记录.md。
4.按交接文档顺序读取关键设计文档和新玩家反馈。
5.先列出本轮目标、非目标、风险和验收标准，得到确认后再修改；涉及 UI 时先使用 $local-figma-unity-ui-review 在本地 Figma 对齐。
```
