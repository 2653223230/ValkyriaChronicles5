# VC5 Demo 启动与 AI 对战流程

更新时间：2026-08-23

本文档记录当前已经实现的「打开游戏后选择卡组并直接单人 VS AI」流程。后续如果需要调整 Demo 入口、默认卡组、AI 卡组选取或测试方式，可以直接修改本文档，再让 Codex 按文档同步实现。

## 目标

这一版 Demo 的启动目标是：

- 玩家打开构建包后，优先进入 Demo 对战准备界面。
- 玩家只需要选择自己的卡组和 AI 卡组，然后点击开始对战。
- 战斗使用本地 Solo VS AI 流程，不需要双开 Unity，也不需要 `TestP2P.unity`。
- 不改动底层联机、局域网 P2P、Matchmaking 的网络流程。

## 当前入口

构建场景顺序已调整为：

1. `Assets/TcgEngine/Scenes/Menu/Menu.unity`
2. `Assets/TcgEngine/Scenes/Menu/LoginMenu.unity`
3. `Assets/TcgEngine/Scenes/Menu/OpenPack.unity`
4. `Assets/TcgEngine/Scenes/Game/Game.unity`

因此玩家启动构建包时会先进入 `Menu.unity`。`MainMenu` 启动时会运行时创建 `VC5 Demo AI Battle Panel`，显示 Demo 对战准备界面。

如果本地没有登录记录，菜单不会立刻跳转到 `LoginMenu`，而是保持在 Demo 对战入口。点击「开始对战」时会为本地 Demo 流程创建临时测试用户 `VC5_Demo_Player`。

## 可选择卡组

当前 Demo 对战入口只展示已经实现并可实际游玩的试玩卡组：

| 用途 | 默认卡组 ID | 显示名 |
| --- | --- | --- |
| 玩家默认 | `deck_vc5_demo_mobile_assault` | VC5 Demo 机动突击 |
| AI 默认 | `deck_vc5_demo_ranged_pressure` | VC5 Demo 射击压制 |

玩家和 AI 都可以在这两套卡组之间切换。入口使用左右按钮切换卡组，而不是复杂的牌库编辑流程。

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

从 `VC5 Demo 对战` 面板点击「开始对战」，进入 B/C 试玩卡组的本地 Solo VS AI 对局并完成游戏初始化后，顶层 UI 显示两页式玩家引导。视觉与文案以 Figma 文件 `VC5 Demo 玩家引导 UI` 的审核版本为准：

- 核心提示：「你只需要打牌」「移动、攻击、战术行动，都从手牌开始」。
- 手牌区：「移动和攻击都要先打牌。拖出卡牌并松开，开始行动。」
- 棋盘区：「打出卡牌后，按提示选择己方棋子、敌人或目标格。」
- 中央得分区：「让棋子进入得分区来获得分数，先达到 9 分获胜。」
- 右下阶段按钮：「点击“放弃行动”，结束当前主要阶段。」
- 第一页按钮：「下一页：资源与技能」。
- 第二页说明法力值、`当前分数/9`、击杀与得分区得分方式、悬停查看技能和点击己方棋子后选择主动技能。
- 第二页按钮：返回箭头与「知道了，开始对战」。
第二页引导实现状态（2026-08-23）：

- Figma 第二页 `TutorialOverlay_Page2_ResourcesScoringSkills` 已由策划确认，并已同步实现到 Unity。
- 引导初始显示第一页；点击「下一页：资源与技能」切换到第二页，遮罩持续阻断底层操作。
- 第二页返回箭头切回第一页；只有点击第二页「知道了，开始对战」才销毁整个引导并恢复游戏操作。
- 分页、返回、最终关闭和真实菜单运行验证均已通过；当前状态为待策划人工确认实际画面与引导易理解程度。

交互规则：

1. 引导只在 `GameType.Solo` 且玩家与 AI 都使用两套 VC5 Demo B/C 试玩卡组之一时显示；联网、局域网 P2P、旧 Solo 卡组不显示。
2. 每次从 Demo 对战菜单开始一局符合条件的对局都显示，不使用永久跳过记录，便于首版 Demo 试玩和重复测试。
3. 引导使用全屏半透明遮罩并置于 `TopCanvas` 最上层；显示期间拦截鼠标和触摸，不能误操作底层手牌、棋盘或阶段按钮。
4. 第一页的「下一页：资源与技能」只切换页面；第二页返回箭头可回到第一页；只有第二页「知道了，开始对战」会销毁整个引导层并恢复正常游戏操作。
5. 引导只解释已有规则，不暂停、推进或修改回合、AI、卡牌结算和联网消息。

核心代码：

- `Assets/TcgEngine/Scripts/UI/Vc5DemoTutorialOverlay.cs`
- `Assets/TcgEngine/Scripts/UI/GameUI.cs` 的 `OnGameStart()`

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

- 将 `ProjectSettings/EditorBuildSettings.asset` 中 `LoginMenu.unity` 调回第一项。
- 或关闭 `MainMenu.show_vc5_demo_ai_panel`。

2026-08-23 玩家固定先攻验证结果：

- 新增 `Vc5DemoFirstPlayerTests`：B/C 四种 Solo 卡组组合全部要求玩家 `player_id = 0` 先攻；Multiplayer 与混入非 Demo 卡组的 Solo 对局继续使用原随机规则。
- TDD RED 阶段四种 Demo 组合稳定得到 AI 先攻并失败；实现 Demo 匹配判断后，完整 `VC5DemoGate` 为 `101/101` 通过，耗时约 `5.448` 秒。
- Unity MCP 从 `Menu.unity` 调用真实 Demo 开始入口进入 `Game.unity`，运行态结果为 `type=Solo`、`first_player=0`、`current_player=0`、`local_player=0`，默认玩家 B、AI C 卡组保持正确。
- 本轮运行时 Unity Console 为 0 error；联网和非 Demo Solo 的先后攻边界未被改为固定玩家先攻。