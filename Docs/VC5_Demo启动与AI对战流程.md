# VC5 Demo 启动与 AI 对战流程

更新时间：2026-09-02

版本快照：`demo_v2_20260830` Windows 构建已从 `Menu.unity` 进入本页所述 VC5 Demo 对战入口并用于玩家试玩；Android v2 未发布。

本文档记录当前已经实现的「打开游戏后选择卡组并直接单人 VS AI」流程。后续如果需要调整 Demo 入口、默认卡组、AI 卡组选取或测试方式，可以直接修改本文档，再让 Codex 按文档同步实现。

## 目标

这一版 Demo 的启动目标是：

- 玩家打开构建包后，优先进入 Demo 对战准备界面。
- 玩家只需要选择自己的卡组和 AI 卡组，然后点击开始对战。
- 战斗使用本地 Solo VS AI 流程，不需要双开 Unity，也不需要 `TestP2P.unity`。
- 不改动底层联机、局域网 P2P、Matchmaking 的网络流程。

## 当前入口

Demo 直接构建的场景顺序固定为：

1. `Assets/TcgEngine/Scenes/Menu/Menu.unity`
2. `Assets/TcgEngine/Scenes/Game/Game.unity`

`LoginMenu.unity`、`OpenPack.unity` 和其他旧场景不加入本次 Demo 的 Build Settings。因此玩家启动 Windows 或 Android 构建包时会直接进入 `Menu.unity`。`MainMenu` 启动时会运行时创建 `VC5 Demo AI Battle Panel`，显示 Demo 对战准备界面。

如果本地没有登录记录，菜单不会立刻跳转到 `LoginMenu`，而是保持在 Demo 对战入口。点击「开始对战」时会为本地 Demo 流程创建临时测试用户 `VC5_Demo_Player`。

## 可选择卡组

当前 Demo 对战入口只展示已经实现并可实际游玩的试玩卡组：

| 用途 | 默认卡组 ID | 显示名 |
| --- | --- | --- |
| 玩家默认 | `deck_vc5_demo_mobile_assault` | VC5 Demo 机动突击 |
| AI 默认 | `deck_vc5_demo_ranged_pressure` | VC5 Demo 射击压制 |
| 新增可选，不替换默认 | `deck_vc5_demo_ranged_pressure_c3` | VC5 Demo 射击压制 C3 |

玩家和 AI 都可以在这三套卡组之间切换。入口使用左右按钮切换卡组，而不是复杂的牌库编辑流程。旧射击压制仍是原有九种牌；名称带 C3 的才是新的三棋子、八种牌、20 张固定构筑。三套 Demo 卡组的 Solo 组合均固定玩家先攻，联网先攻规则不变。

### C3 人工复验入口（2026-08-27）

1. 打开 `Assets/TcgEngine/Scenes/Menu/Menu.unity`，点击 Unity Play。
2. 玩家卡组切换为“VC5 Demo 射击压制 C3”；AI 可选旧机动突击、旧射击压制或 C3。
3. 点击“开始对战”，关闭已有引导后开始测试。不要从 Game3D 场景直接启动。
4. 将 C3 牌拖到己方棋子：确认范围、伤害、移动路线与落点预览；松开即结算。战术移动/强行军松开后再选择高亮空格。
5. 重点复验下方出生位置是否可直接选中、手机拖拽是否舒适、护卫警戒后重选是否清楚、绕行不穿人，以及预览是否与实际伤害一致。
6. 本轮属于反馈优化第 4 项，不重新打包 APK；自动测试和画面证据见发布计划。自动通过不替代你的手感验收。

自动验收：2026-08-27 完整门禁 `156/156`，原 B/C 与新 C3 共 90 局 AI 自动对战通过；真实菜单启动和预览双分辨率测试 `1/1`。未重做双开 P2P 联机/Android 真机验收，也未改联网消息协议；发布前仍需按既有流程复验这些入口。

## 启动逻辑

核心代码：

- `Assets/TcgEngine/Scripts/Menu/Vc5DemoMatchSetup.cs`
- `Assets/TcgEngine/Scripts/Menu/Vc5DemoAIBattlePanel.cs`
- `Assets/TcgEngine/Scripts/Menu/MainMenu.cs`

`Vc5DemoMatchSetup.ApplySoloAIMatch(playerDeckId, aiDeckId)` 会设置：

- `GameClient.game_settings.game_type = GameType.Solo`
- `GameClient.game_settings.game_mode = GameMode.Casual`
- `GameClient.game_settings.server_url = ""`
- `GameClient.game_settings.scene = GameplayData.Get().GetRandomArena()`
- `GameClient.player_settings.deck = new UserDeckData(playerDeck)`
- `GameClient.ai_settings.deck = new UserDeckData(aiDeck)`
- `GameClient.ai_settings.ai_level = GameplayData.Get().ai_level`

随后通过 `MainMenu.StartGame(GameType.Solo, GameMode.Casual)` 进入现有游戏场景。
## Demo 先后攻规则

- 从 `VC5 Demo 对战` 菜单进入的 B/C 卡组本地 Solo VS AI 对局，玩家固定先攻：玩家为 `player_id = 0`，AI 为 `player_id = 1`。
- 该规则仅在 `GameType.Solo` 且双方都使用机动突击或射击压制 Demo 卡组时生效。
- 普通 Solo、Adventure、Multiplayer 和 HostP2P 保持原有先后攻规则，不因 Demo 修改而改变。
- 自动化验收必须覆盖 B/C 四种组合均由玩家先攻，并验证非 Demo 对局不会被误判为 Demo 固定先攻。
## 进入战斗后的玩家引导

从 `VC5 Demo 对战` 面板点击「开始对战」，玩家选择“VC5 Demo 射击压制 C3”并进入本地 Solo VS AI 对局后，顶层 UI 按 Figma 文件 `VC5 Demo 玩家引导 UI` 的 `V2.1 Editable` 定稿运行六状态实操教学：

1. `INTRO`：说明“你只需要打牌”和率先获得 9 分的目标。
2. `WAIT_CARD_DRAG`：框出最左侧固定起手的`移动射击`与初始位置的`游骑射手`，等待玩家按住并拖动指定卡牌。
3. `PREVIEW`：卡牌仍被按住且指向游骑射手时显示，结合实际 C3 预览说明黄色路线、青色落点、红色攻击目标和预计伤害；移开目标返回上一步。
4. `RESULT`：仅在玩家松开且出牌成功、移动和伤害结算结束后显示，解释落点、敌人生命降低与`机动火力`。
5. `HUD_INFO`：框出左下当前得分、法力值和卡牌费用。
6. `SCORE_FLOW`：框出中央七格得分区，说明得分与“玩家出牌 -> AI 行动 -> 双方放弃后结算/弃牌”的回合流程；最终按钮关闭教学并开始自由对战。

旧的 2026-08-23 两页式说明引导保留为历史记录，不再作为当前 C3 教学验收基线。当前实现与验证进度以本文后续 2026-08-29 记录和《VC5_Demo发布计划与进度》为准。

交互规则：

1. 六状态实操引导只在 `GameType.Solo`、玩家使用 C3 且 AI 使用任一已实现 Demo 卡组时显示；联网、局域网 P2P、旧 Solo 卡组和玩家使用旧 B/C 时不显示。
2. 每次从 Demo 对战菜单开始一局符合条件的对局都显示，不使用永久跳过记录，便于首版 Demo 试玩和重复测试。
3. 除`WAIT_CARD_DRAG/PREVIEW`允许拖动指定`移动射击`外，引导阻断底层手牌、棋盘和阶段按钮；其他卡牌或错误目标不能让教学前进，也不能扣费。
4. 教学进度由实际交互驱动：只有成功出牌才进入结果页，之后两个说明按钮依次进入 HUD 和得分/回合页，最终按钮才销毁引导。
5. 教学期间暂停本地 `AIPlayerVc5Demo` 的行动执行。示范普通牌交出底层行动权后 AI 仍等待，教学最终关闭后再正常行动；不修改当前玩家、回合规则和联网消息。
6. 玩家在本地 `GameType.Solo` 使用 C3 时，牌库仍先正常洗牌，只把一张`移动射击`移动到第一抽位置；其余牌保持洗牌后的随机相对顺序。该规则不作用于 AI、旧 B/C、Multiplayer 或 P2P；六状态教学仍只在双方使用已实现 Demo 卡组且玩家为 C3 时显示。

### 2026-08-30 V2.1 实现验证

- 教学专项 `4/4`、完整 `VC5DemoGate` `184/184` 通过；真实 `Menu -> VC5 Demo 对战 -> C3 玩家 -> Game` 运行测试 `1/1` 通过，原生 XML 结果为 `Passed`，耗时约 19.64 秒。
- 六页真实场景截图位于 `TestResults/tutorial-v21-01-intro-1920x1080.png` 至 `tutorial-v21-06-score-flow-1920x1080.png`；空心框不会遮挡目标，最左侧`移动射击`、游骑射手、当前得分、法力和中央七格位置已核对。
- 自动测试已覆盖首抽随机边界、教学状态转换、错误卡牌/目标阻断、AI 门禁建立与释放，以及 Solo/Multiplayer 触发边界。仍需按本文自测方式人工完成一次真实拖牌，观察 AI 在最后一页关闭前等待、关闭后继续行动。

核心代码：

- `Assets/TcgEngine/Scripts/UI/Vc5DemoTutorialOverlay.cs`
- `Assets/TcgEngine/Scripts/UI/GameUI.cs` 的 `OnGameStart()`
- `Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs` 的 C3 教学首抽调整
- `Assets/TcgEngine/Scripts/GameClient/HandCard.cs` 的教学输入门禁
- `Assets/TcgEngine/Scripts/AI/AIPlayerVc5Demo.cs` 的教学期间暂停门禁

## 与联网功能的边界

本功能没有修改以下底层流程：

- `GameType.Multiplayer`
- `GameType.HostP2P`
- `GameClientMatchmaker`
- `ServerManager`
- `ServerManagerLocal`
- `TestP2P.unity`
- `Vc5LanBattlePanel`

旧的按钮和方法仍保留：

- `MainMenu.OnClickSolo()`：仍使用原来的玩家卡组选择器，并从 `GameplayData.ai_decks` 随机选择 AI 卡组。
- `MainMenu.TryStartLanBattleHost()`：仍走 HostP2P。
- `MainMenu.TryStartLanBattleJoin()`：仍走 Multiplayer + IP。
- `TestP2P`：仍可用于双开本地联机测试。

Demo 面板只新增一个更快的本地 Solo VS AI 测试入口。

## 当前回合与阶段逻辑

当前实现严格区分“完成一次主要行动”和“放弃本回合后续主要阶段行动权”：

1. 新回合开始时，双方补牌至 5 张并刷新法力；先手玩家获得第一个主要行动机会。
2. 快速行动不消耗主要行动，也不会切换当前行动玩家；玩家仍可继续操作。
3. 普通卡牌或非快速角色技能完整结算后，系统自动把行动权交给尚未放弃的对手，不需要再点击按钮。
4. 如果对手已经放弃主要阶段，则未放弃玩家完成普通行动后仍继续获得下一个行动机会。
5. 点击「放弃行动」表示该玩家本回合不再获得主要阶段行动权。只有一方放弃时，行动权交给另一方；双方都放弃后，系统自动结算得分区并进入结束弃牌阶段。
6. 结束弃牌阶段中，玩家拖动任意手牌并松开即可弃置该牌，不要求拖到指定区域；单纯点击不会弃牌。点击「完成弃牌」表示自己不再弃牌，双方都完成后系统开始下一回合。

代码中的 `EndStage`、`EndTurn`、`NextStep`、`NextPhase` 是历史联网消息和方法名。主要阶段内它们都表示“当前玩家放弃后续主要行动”，不是“完成一次普通行动”；Demo UI 统一只显示和调用语义明确的阶段按钮，底层联网消息仍保留兼容。

## 自测方式

### 2026-08-26 贴身移动选择不存在棋盘格（已修复，待策划画面复测）

- `BUG-BOARD-CELL-01`：实际场景只有 34 个 `BoardSlot`，但底层 `Slot.IsValid()` 使用更大的矩形坐标范围。运行态复现斥候向敌方护卫发动 `贴身攻击` 时，相邻格算法会选择场景中不存在的 `(9,3)`；逻辑位置改变后，客户端找不到对应 `BoardSlot`，画面对象停留在旧位置，因而可能与另一枚棋子重叠。
- 修复范围：新增统一的 `Vc5DemoGrid.IsPlayableBoardCell`，自动贴身/追击/冲锋/撤退/向得分区移动、通用移动、打出棋子和效果召唤都必须同时满足“逻辑坐标有效”和“属于 `Game.unity` 的 34 个实际棋盘格”。
- 自动验证：新增锯齿边缘回归测试，修复前 `2/2` 稳定失败，修复后 `2/2` 通过；机动突击卡牌测试 `18/18`、完整 `VC5DemoGate` `120/120` 通过，Unity Console 为 0 error。仍需策划按实际画面复测贴身落点不重叠。

### 2026-08-20 人工测试问题（已修复，待策划复测）

- `BUG-OCCUPY-01`：新增统一的画面物理格查询。移动、召唤、贴身攻击、追击、冲锋攻击和决胜突击均按画面实际位置排除敌我任意单位；目标周围没有空格时，该目标不可选。
- `BUG-DISCARD-01`：弃牌改为按下手牌后实际拖动至少 20 像素，再于任意位置松开即弃置；单纯点击或轻微抖动不弃牌。界面分别显示「弃牌阶段」「拖动手牌后松开即可弃置」「完成弃牌」。
- `BUG-PHASE-UI-01`：删除 `Game.unity` 中重复的场景按钮，只保留 `GameUI.prefab` 的响应式阶段按钮；主要阶段显示「放弃行动」，弃牌阶段显示「完成弃牌」。
- 自动验证：针对物理格占用、通用移动/召唤占用、无空邻格、拖动阈值、阶段文案和单按钮场景新增回归测试；2026-08-20 完整 `VC5DemoGate` 连续两轮 `93/93` 通过。仍需策划按实际画面与拖动手感人工复测。

### 2026-08-19 人工测试问题（已修复，待策划复测）

- `BUG-MOVE-01`：根因是双方逻辑棋盘都保存玩家相对坐标，而敌方棋子只在画面层旋转 180 度显示；贴身寻路此前直接使用敌方相对坐标。现已统一将敌方目标转换到行动角色视角，再计算距离、攻击范围和相邻落点。`贴身攻击` 只移动，`决胜突击` 正确移动后造成自身攻击力 +1 伤害。
- `BUG-TURN-01`：除旧按钮文字有歧义外，代码还缺少“普通行动完整结算后自动交替行动权”。现已补齐自动交替；点击按钮只负责主动放弃本回合剩余的主要阶段行动权。
- UI 后续已进一步简化：主要阶段按钮显示「放弃行动」，结束弃牌阶段显示「完成弃牌」；状态区明确区分「我方行动」「对手行动」「弃牌阶段」和「等待对手」。
- 自动验证：移动方向、伤害、快速行动、普通行动自动交替、对手已放弃时继续行动、取消选目标退款均有回归测试；完整 `VC5DemoGate` 为 `88/88` 通过。仍需策划按画面与实际手感人工复测。

推荐测试流程：

1. 在 Unity 打开 `Assets/TcgEngine/Scenes/Menu/Menu.unity`。
2. 点击 Play。
3. 看到 `VC5 Demo 对战`面板。
4. 玩家卡组默认应为 `VC5 Demo 机动突击`。
5. AI 卡组默认应为 `VC5 Demo 射击压制`。
6. 点击左右按钮，确认玩家和 AI 卡组都可以切换。
7. 点击「开始对战」。
8. 应进入 `Game.unity` 对战场景，并开始本地 Solo VS AI 对战。
9. 游戏初始化完成后应显示两页式玩家引导。第一页确认四个高亮区域和核心打牌流程；点击「下一页：资源与技能」后，引导不应关闭，底层操作仍应被阻断。
10. 第二页确认法力值、胜利分数、两种得分方式和棋子技能说明；点击返回箭头应回到第一页，再次进入第二页并点击「知道了，开始对战」后，引导才应完整消失并恢复手牌、棋盘和阶段按钮操作。
11. 打出一张普通行动牌并完整选完目标后，系统应自动把行动权交给 AI；打出快速行动牌后仍应由玩家继续行动。
12. 在我方主要阶段点击「放弃行动」后，玩家本回合不再获得主要行动；AI 可继续行动，直到 AI 也放弃后才进入得分和结束弃牌阶段。
13. 进入弃牌阶段时，右下角应显示「弃牌阶段」和「拖动手牌后松开即可弃置」；轻点手牌不应弃置，拖动手牌后在任意位置松开应弃置；完成后点击「完成弃牌」。
14. 直接点击己方棋子和空格不应移动棋子；移动只能通过移动卡牌或角色技能产生。
15. 打出 `普通攻击` 后，先点击己方角色作为攻击者，再点击该角色攻击范围内的敌人；点击超出范围或非法目标时会显示无效目标提示。
16. 让双方棋子处于可攻击距离，分别测试 `普通攻击`、`射击` 和 `冲锋攻击`：目标只受到卡面写明的伤害，攻击者生命值必须保持不变，不得触发模板自动反击。
17. 分别测试 `贴身攻击` 和 `决胜突击`：角色应向画面中所选敌人方向移动并停在未被任何单位占用的相邻空格；前者不造成伤害，后者造成自身攻击力 +1 伤害。再测试敌人周围无空格时不能选择该敌人。
18. 结束测试后停止 Play。

构建包测试流程：

1. 确认 Build Settings 第一场景是 `Assets/TcgEngine/Scenes/Menu/Menu.unity`。
2. 打包运行。
3. 启动后应直接看到 Demo 对战准备界面。
4. 选择玩家与 AI 卡组后点击「开始对战」。
5. 应直接进入本地 VS AI 对战。

自动化验证：

- `TcgEngine.Testing.Editor.Vc5DemoMatchSetupTests`
  - 验证默认玩家/AI 卡组。
  - 验证指定玩家/AI 卡组会写入 `GameClient` Solo 设置。
  - 验证缺失卡组不会覆盖当前有效选择。
- `TcgEngine.Testing.Editor.Vc5DemoStartupTests`
  - 验证 Build Settings 第一场景为 `Menu.unity`，且 `Game.unity` 已启用。
  - 验证 `Menu.unity` 可以加载并生成 `VC5 Demo AI Battle Panel`，面板只提供 B/C 两套卡组。
  - 验证应用 B/C 选择后可以加载 `Game.unity`，且 Solo 设置中的玩家/AI 卡组 ID 保持正确。
- Unity MCP 运行时冒烟：在上述静态/Editor 测试通过后，进入 Play Mode 检查 Menu 面板与 Game 场景运行时对象；该步骤不替代玩家实际点击和画面验收。
2026-08-23 两页式玩家引导实现与验证结果：

- `Vc5DemoTutorialTests` 验证引导只在 B/C 卡组的 Solo 对局显示、全屏阻断层、两页高亮与边框、必要文案、下一页、返回和最终关闭行为。
- Unity MCP 完整运行 `VC5DemoGate`，结果 `101/101` 通过，耗时约 `5.610` 秒。
- Unity MCP 从 `Menu.unity` 调用真实 Demo 开始入口进入 `Game.unity`；确认引导位于 `TopCanvas` 最上层，第一页默认显示且第二页隐藏，阻断层可拦截底层点击。
- 运行态确认「下一页：资源与技能」切换到第二页且不关闭引导，返回箭头可回到第一页；第二页点击「知道了，开始对战」后引导在下一帧完整销毁，本轮 Console 为 0 error。
- 上述自动结果不替代策划对实际分辨率下的文字可读性、布局遮挡和引导易理解程度的人工验收。

2026-08-18 自动化与运行时验证结果：

- `Vc5DemoStartupTests` 共 3 项，`3/3` 通过。
- Unity MCP 从 `Menu.unity` 进入 Play Mode，确认 `Vc5DemoAIBattlePanel` 已激活，且只显示 B/C 两套试玩卡组。
- 运行时调用实际 Solo 启动入口，使用玩家 B、AI C 成功切换到 `Game.unity`。
- 对局进入 `Play/Main`，玩家和 AI 棋盘各生成 3 名角色，双方卡组 ID 保持为所选 B/C 卡组。
- 本次 Play Mode 冒烟后 Unity Console 为 0 error，并已正常退出 Play Mode。

2026-08-19 人工问题修复验证结果：

- 新增跨玩家视角移动和回合行动权交替回归测试，修复前稳定失败，修复后通过。
- Unity MCP 完整运行 `VC5DemoGate`，结果 `88/88` 通过，耗时约 `5.505` 秒。
- 脚本刷新后 Unity Console 为 0 error。

## 对局中的行动与得分反馈

B/C Solo Demo 从 `Menu.unity` 的「VC5 Demo 对战」进入后，会在教学层下方创建运行时反馈 HUD：

- 左上常驻显示双方当前分数、得分区人数和按当前占格计算的本次结算预测。
- 上方横幅显示 AI 打出的卡牌、费用、简短效果或主动技能。
- 青色框表示 AI 行动者，黄色框表示目标单位或目标格；高亮自动消失，不拦截点击。
- 左侧「最近行动」只保留最新 3 条，便于玩家回看伤害、移动、死亡和得分来源。
- 进入得分阶段时显示双方得分区人数和本次加分；双方同时加分时必须同时显示两边变化。
- 结束面板显示胜负原因和最终比分；当前可识别「达到 9 分」与「需要抽牌时牌库为空」。

反馈 HUD 复用 `Vc5ScoringZone` 和服务端同步后的 `Player.kill_count`，不会在客户端维护第二套胜负规则。当前未保存整局“击杀分/得分区分”的权威历史拆分，因此结束面板只显示最终总分；需要该统计时应先扩展服务端数据结构与网络序列化。

## 修改规则

后续如果要改默认卡组：

- 修改本文档「可选择卡组」表格。
- 同步修改 `Vc5DemoMatchSetup.GetDefaultPlayerDeckId()` 或 `GetDefaultAIDeckId()`。
- 更新对应测试。

后续如果要新增可选试玩卡组：

- 先确保卡组已经在 `Vc5DemoBootstrap` 或正式数据中注册。
- 修改 `Vc5DemoMatchSetup.GetPlayableDemoDecks()`。
- 更新本文档「可选择卡组」表格。
- 补充或更新自动化测试。

后续如果要恢复传统登录首屏：

- 将 `LoginMenu.unity` 重新加入 `ProjectSettings/EditorBuildSettings.asset` 并调回第一项。
- 或关闭 `MainMenu.show_vc5_demo_ai_panel`。

## 直接构建基线

2026-08-23 首轮采用方案 A 直接构建，用于先测量真实包体，不进行资源裁剪或 AssetBundle 重构：

- Windows：非 Development、x64，输出到 `Builds/Direct/Windows/VC5Demo.exe`。
- Android：非 Development APK，输出到 `Builds/Direct/Android/VC5Demo.apk`。
- 两个平台均只包含 `Menu.unity` 和 `Game.unity`，且 `Menu.unity` 必须为索引 0。
- 构建完成后记录可分发文件总大小；Windows 以整个目录压缩前大小为准，Android 以 APK 文件大小为准。
- Windows 首轮直接构建已成功：原始目录 `210.25 MiB`，排除 `VC5Demo_BurstDebugInformation_DoNotShip` 后生成的分发 ZIP 为 `81.69 MiB`。
- Windows Player 独立启动 12 秒保持运行，Player 日志无崩溃或脚本错误；启动画面与完整对局仍需策划手工验收。
- Unity `2021.3.33f1c1` 的 Android Build Support、Android SDK & NDK Tools 和 OpenJDK 已安装。安装后的首轮构建已进入 Gradle `:launcher:packageRelease`，但在 APK 压缩阶段以 `Execution of compression failed` 结束，尚未生成 APK。
- Android 模块安装后，可通过 Editor-only 入口 `TcgEngine.Editor.Vc5DemoBuildPipeline.BuildAndroid` 执行非交互构建。该入口固定读取 Build Settings 中的两个启用场景，输出 `Builds/Direct/Android/VC5Demo.apk`，并强制生成 APK 而不是 AAB。
- 为避免 Android 构建占用 C 盘，批处理启动环境与构建入口都会将 `TEMP`、`TMP` 指向 `Builds/Temp/Android`，将 `GRADLE_USER_HOME` 指向 `Builds/GradleHome`。构建完成后清理 `Builds/Temp/Android`；Gradle 缓存保留在 E 盘以加快后续构建。
- `BuildAndroid` 用于日常增量构建；`BuildAndroidClean` 仅用于缓存异常、Unity/Android 平台升级或最终候选包。不要为名称、图标等小改动每次清空 Android 构建缓存。
- APK 应用显示名为 `ValkyriaChronicles5`，图标使用 `Assets/TcgEngine/Images/VC5/AppIcon.png`。构建入口会在开始打包前再次应用并校验品牌配置，避免模板名或默认图标回退。
- 首次构建可能因平台首次导入、着色器/资源重建和 Gradle 初始化持续数分钟。Gradle 阶段日志短暂无更新不代表卡死，应同时观察 Unity/Java 进程和 CPU；不要在前一个构建未退出时再次启动 Unity。
- 必须在启动批处理 Unity 前就设置 E 盘 `TEMP`、`TMP`、`GRADLE_USER_HOME`。首次 `Execution of compression failed` 已通过该方式解决，本轮构建结束后只删除 `Builds/Temp/Android`，保留 E 盘 Gradle 缓存。
- 清理前先确认 Unity/Java 已退出并关闭 ADB；若 Java 的空 `hsperfdata_Vet` 目录拒绝普通删除，只允许在校验完整绝对路径后对项目内 `Builds/Temp/Android` 精确提升权限清理。
- 第一次构建遗留的 `C:/Users/Vet/.gradle`（约 `177.55 MiB`）已在确认 E 盘缓存可用后删除。若该目录再次出现，先检查批处理是否在 Unity 启动前正确设置了 `GRADLE_USER_HOME`，不要让两份 Gradle 缓存长期并存。
- Android 品牌版直接构建已成功：`Builds/Direct/Android/VC5Demo.apk` 为 `77,275,392` 字节（`73.70 MiB`），SHA-256 为 `CA46D486401A22CFB21910C72E323419D0AEB5F7D2E9914E1BFE23AB7A460694`。
- APK 清单静态校验结果：包名 `com.IndieMarc.TcgEngine`，显示名 `ValkyriaChronicles5`，最低/目标 API 33，横屏，原生架构 `armeabi-v7a`，启动 Activity 为 `com.unity3d.player.UnityPlayerActivity`。
- APK 内六档 mipmap 图标均已生成；提取 `xxxhdpi` 图标后确认内容与 `Assets/TcgEngine/Images/VC5/AppIcon.png` 一致。不同 Android 启动器仍可能按圆形或圆角方形蒙版裁切，最终桌面效果以真机为准。
- APK v2 签名验证通过，但本轮仍使用 Android Debug 证书，仅适合直接安装试玩。正式对外长期发布前需要创建并妥善保存正式 keystore；使用不同证书的包不能直接覆盖升级。
- 当前未检测到已连接的 Android 设备，因此 APK 的安装启动、触控适配、Demo 菜单首屏和完整对局需要策划在真机上继续验收。

2026-08-23 玩家固定先攻验证结果：

- 新增 `Vc5DemoFirstPlayerTests`：B/C 四种 Solo 卡组组合全部要求玩家 `player_id = 0` 先攻；Multiplayer 与混入非 Demo 卡组的 Solo 对局继续使用原随机规则。
- TDD RED 阶段四种 Demo 组合稳定得到 AI 先攻并失败；实现 Demo 匹配判断后，完整 `VC5DemoGate` 为 `101/101` 通过，耗时约 `5.448` 秒。
- Unity MCP 从 `Menu.unity` 调用真实 Demo 开始入口进入 `Game.unity`，运行态结果为 `type=Solo`、`first_player=0`、`current_player=0`、`local_player=0`，默认玩家 B、AI C 卡组保持正确。
- 本轮运行时 Unity Console 为 0 error；联网和非 Demo Solo 的先后攻边界未被改为固定玩家先攻。

### 2026-08-28 新卡图查看方法

#### 后续试玩问题修复（已实现，待人工复测）

- 斥候快移：只允许选择一格内未占用的空格，发动前以斥候自身判断合法目的地，不使用上一张牌残留的行动角色。AI 必须选择格子并实际移动，被包围时不空放；零费用、快速行动、每回合一次不变。
- 选中棋子查看技能时不显示模板攻击虚线；拖动需目标的手牌、技能/卡牌选目标时保留原有指示，取消或结束选目标后清除。
- 手牌和左侧详情放大完整方形卡图，压缩留白，标题/效果分区，长说明自适应；棋盘方形图片与碰撞体不变。
- 查看详情时得分汇总和行动日志暂时让位，关闭后恢复；相同效果不在卡面下重复显示，角色属性及额外技能说明仍保留。
- 本轮修复遵循已有规则，不修改主规则。完整门禁 `182/182`，额外卡面渲染 `1/1`，真实 Menu 启动与双分辨率运行时 `1/1`；本轮控制台 0 error。

1. 打开 `Assets/TcgEngine/Scenes/Menu/Menu.unity` 并进入 Play Mode。
2. 在 VC5 Demo 对战菜单中选择自己使用“射击压制 C3”、AI 使用“机动突击”，点击开始对战。
3. C3 的狙击手、火力护卫、游骑射手以及 B 的骑兵、刺客、斥候应显示对应角色图；详情卡面与棋盘共用原图。旧 C 狙击手不在本次换图范围内。
4. 查看 C3 手牌并悬停放大，应显示已审核的几何示意图，卡名、费用及效果文字与图片分开。正常手牌仍沿用扇形排列；未抽到的卡可在后续回合查看。
5. 八张卡面完整总览在 `TestResults/c3-art-cards.png`，六名角色详情在 `TestResults/demo-art-heroes.png`，无需为了逐张审核反复开局。

最初卡图接入记录：图片专项 `18/18`、完整门禁 `173/173`；以上后续修复已扩展测试，不再使用旧门禁数量作为当前结果。主规则与卡牌数值未变，现有 APK/Windows 发布包未更新。

#### 本轮人工复测

1. 从上述菜单选择自己 C3、AI 机动突击。让 AI 行动，斥候使用快移后应移动一格，不与其他单位重叠。
2. 自己使用机动突击时，点击斥候查看技能不应产生跟随鼠标的长虚线；发动快移后选择合法空格，移动后虚线清除。被其他单位占用的格子不可作为目的地。
3. 悬停 C3 手牌及两套卡组的棋子，检查图片、卡名、效果与属性不互相遮挡。详情出现时左侧分数摘要/日志隐藏，移开鼠标并淡出后恢复。
4. 可直接查看 `TestResults/card-detail-1920x1080.png`、`TestResults/card-detail-1280x720.png`、`TestResults/hero-detail-1280x720.png`；这些是实际 Game 场景中的确定性测试局截图，不是新 UI 的示意图。手机实机触控和观感待重新打包后验收。

### 2026-08-30 V2.1 第二步拖牌引导细化

- 等待拖牌页正文保持“从手牌按住这张牌，拖到青色框内的「游骑射手」”，其中“拖到”使用橙色强调。
- 最左侧`移动射击`的黄色框按 Figma 收紧为 `1920x1080` 坐标 `(585, 913, 126, 167)`；左侧提示改为“按住并拖动到棋子上！”。
- 视觉框收紧不缩小实际拖动入口：最左侧卡牌从手牌上缘到屏幕底边均可开始拖动，合法卡牌仍由`移动射击`ID 校验，其他牌不能绕过教学门禁。
- 人工复测时从最左侧卡面的上、中、下三个位置分别按住拖动，均应进入预览；橙色强调、黄色框和左侧提示不应遮挡卡名、费用或相邻手牌。

### 2026-08-30 本轮试玩包运行范围

- Windows 与 Android 包启动后都直接进入 `Menu.unity`，玩家在 `VC5 Demo 对战`面板选择双方卡组并开始 Solo VS AI；打包场景仍只有 `Menu.unity` 与 `Game.unity`。
- Android APK 最低支持 Android 7.0（API 24），包含 ARMv7 与 ARM64，允许平板横屏两个方向旋转。现有 UI 按横屏全屏设计，不开放自由缩放窗口。
- 应用显示名和 Windows 可执行文件名使用 `ValkyriaChronicles5`，两个平台使用同一张 VC5 图标。
- Android 手机和平板仍需真机检查：安装、启动、横屏旋转、菜单完整显示、拖牌触控、教学文字、刘海/系统导航栏遮挡以及完成一局 AI 对战。

### 2026-09-02 `demo_v2_20260830` 封存结论

- Windows v2 构建路径：`Builds/Direct/Windows/ValkyriaChronicles5/ValkyriaChronicles5.exe`。构建目录约 `219.79 MiB`，其中 `ValkyriaChronicles5_BurstDebugInformation_DoNotShip` 不应发送给玩家。
- 该 Windows 版本已用于本轮玩家试玩；后续反馈在下一开发会话中整理，不在本次封存时预先推断结论。
- Android v2 构建已停止；旧 `VC5Demo_v1_20260823.apk` 不包含本页全部 v2 教学与 C3 最新改动。
- Git Tag `demo_v2_20260830` 固定源码、资源、项目设置和文档，不包含 `Builds` 下受忽略的二进制构建产物。
