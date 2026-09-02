# VC5 AI 行动逻辑

更新时间：2026-09-02

版本快照：`demo_v2_20260830` 继续使用本文所述 `AIType.Vc5Demo`、C3 规划器与行动表现。版本封存只审计文档，没有调整 AI 评分、选择链或联网消息。

2026-08-28 已修复斥候快移空放：目标必须为一格内未占用空格。技能可用性以斥候自身检查，不读取上一行动残留角色；AI 选目标阶段不得选择棋子来结算只支持格子的效果。被包围时跳过该技能，仍保留零费、快速、每回合一次规则。

实现：`ConditionVc5SlotMoveFromTriggerer` 明确拒绝 Card、Player、CardData 目标，按实体棋盘及敌我统一占格校验 Slot；`Vc5DemoGrid.GetMovingActor` 对 Activate 使用 caster，对移动牌后续效果使用 ability_triggerer，条件、效果和 AI 格子评分共用这一判断。未改变 AI 消息类型或联网流程。新增 6 项回归覆盖双方视角、残留触发者、包围与快速移动牌第二次选择；完整门禁 182/182 通过，含九种组合共 90 局自动对战。

本文档记录当前试玩 demo 中“可与玩家对战的 AI”的行为逻辑和实现方式。后续如果希望修改 AI 行为，优先修改本文档，再按本文档同步代码。

## 当前 AI 类型

| 项目 | 当前值 |
|---|---|
| AI 类型 | `AIType.Vc5Demo` |
| 枚举值 | `20` |
| 默认配置 | `Assets/TcgEngine/Resources/GameplayData.asset` 中 `ai_type: 20` |
| AI 卡组来源 | `GameplayData.ai_decks` |
| 已注册 AI 卡组 | `deck_vc5_demo_mobile_assault`、`deck_vc5_demo_ranged_pressure`、`deck_vc5_demo_ranged_pressure_c3` |

`Vc5DemoBootstrap.Register()` 会在运行时兜底：

- 将 `GameplayData.ai_type` 设置为 `AIType.Vc5Demo`。
- 将 B/C/C3 三套 demo 卡组加入 `free_decks`。
- 将 B/C/C3 三套 demo 卡组加入 `ai_decks`。
- 对 `DeckData.deck_list`、`free_decks`、`ai_decks` 做同 ID 去重，避免 Unity 热重载后重复出现。

## 代码入口

### 2026-08-27 C3 接入

- 新增独立 `deck_vc5_demo_ranged_pressure_c3` 到玩家与 AI 可选卡组；旧 B/C 不替换。
- C3 出牌前遍历己方棋子，用 `Vc5C3Rules.Plan` 排除没有合法目标/路线的棋子，不能仅因某棋子攻击高就忽略实际可行动的棋子。
- 四种伤害牌优先考虑有效伤害与击杀，群体牌累计多个目标收益；自动选敌和落点完全沿用玩家规则，不允许 AI 改选目标。
- 精确移动继续使用选格流程，倾向推进得分区；射程增益优先给攻击较高的棋子，临时校准已存在时不重复浪费该牌。
- 伤害评分是轻量启发式，不等于多回合搜索；预览/实际结算的确定性由共享规则保障。AI 的数值强度和策略质量仍需人工试玩。
- 已新增 C3 对 B、C、C3 及反向组合共 50 局自动对战防卡死测试，连同原 B/C 40 局共 90 局通过；2026-08-27 完整门禁 156/156，报告 `TestResults/c3-gate-reviewed.xml`。这验证流程能结束，不等于 AI 强度或平衡已通过人工验收。

| 文件 | 职责 |
|---|---|
| `Assets/TcgEngine/Scripts/AI/AIPlayer.cs` | AI 工厂与 `AIType.Vc5Demo` 枚举 |
| `Assets/TcgEngine/Scripts/AI/AIPlayerVc5Demo.cs` | 实际 AI 玩家，负责在回合中调用 planner 并执行动作 |
| `Assets/TcgEngine/Scripts/AI/Vc5DemoAIPlanner.cs` | 规则式决策核心，负责选择下一步行动或选择链目标 |
| `Assets/TcgEngine/Scripts/Data/Vc5DemoBootstrap.cs` | 注册 demo 卡组，并同步默认 AI 配置 |
| `Assets/TcgEngine/Scripts/Testing/Editor/Vc5DemoAITests.cs` | AI 行为回归测试 |
| `Assets/TcgEngine/Scripts/UI/Vc5DemoBattleFeedback.cs` | 订阅权威客户端事件，展示 AI 出牌、技能、行动者、目标和结果 |
| `Assets/TcgEngine/Scripts/UI/Vc5DemoBattleFeedbackModel.cs` | 最近三条行动记录、比分摘要和胜负原因的纯显示模型 |
| `Assets/TcgEngine/Scripts/Testing/Editor/Vc5DemoBattleFeedbackTests.cs` | AI 行动文案、记录容量、计分预测和胜负原因回归测试 |

## AI 行动表现层

2026-08-25 已为 B/C Solo Demo 增加第一版 AI 行动表现层，不修改 `Vc5DemoAIPlanner` 的决策优先级，也不修改联网消息语义：

- AI 出牌时在屏幕上方显示卡名、费用和一行简短效果。
- AI 使用主动技能、移动或攻击时显示对应行动提示。
- 棋盘上的行动者使用青色框，目标单位或目标格使用黄色框短暂高亮。
- 左侧保留最近 3 条行动记录，包含目标、伤害、治疗、死亡和得分等结果。
- AI 行动权切回玩家时明确记录「AI 放弃行动」，避免玩家误以为 AI 卡住。
- 表现层只读取 `GameClient` 已接收的出牌、移动、技能、攻击、伤害、弃置、回合和全量刷新事件，不自行执行效果或修改游戏状态。

当前边界：

- 当前实现高亮行动者和最终目标/落点，不绘制完整移动路径线。
- 当前不为了动画强行阻塞服务端或联网流程；行动节奏继续沿用 `AIPlayerVc5Demo` 的执行等待，人工测试后再决定是否增加可跳过的表现队列。
- 若以后调整 AI 播放速度，必须同时验证选择链不会超时、行动权不会重复切换、Multiplayer 消息顺序不受影响。

## 总体设计目标

当前 AI 的目标不是做竞技级强度，而是服务试玩 demo：

- 能理解“卡牌驱动棋子行为”的核心玩法。
- 能完成“先选己方角色，再选敌人/格子”的链式操作。
- 会主动打攻击牌、推进得分区、保护关键角色。
- 不在 selector 状态下乱取消或乱选敌方/己方目标。
- 行为稳定、可预测，方便后续按文档调参。

## 每次决策的流程

`Vc5DemoAIPlanner.ChooseNextAction(Game data, int playerId)` 是主入口。

优先级如下：

1. 如果当前处于选择目标状态，并且 selector 属于 AI，则进入“选择链处理”。
2. 如果当前不是 AI 的可行动时机，则结束行动。
3. 在手牌中选择最合适的可打出卡牌。
4. 如果没有合适手牌，则尝试使用角色主动技能。
5. 如果仍无行动，则结束行动。

## 打牌优先级

AI 会遍历当前手牌，给每张能打出的牌评分，选择分数最高者。

### 攻击牌

包括：

- `vc5_demo_basic_attack`
- `vc5_demo_shoot`
- `vc5_demo_aimed_shot`
- `vc5_demo_focus_fire`
- `vc5_demo_ranged_suppression`

评分倾向：

- 能打到敌人才优先。
- 能击杀敌人时大幅加分。
- 优先用攻击力更高的己方角色。
- 优先攻击高攻击、低生命、位于得分区的敌人。

### 贴身/突击牌

包括：

- `vc5_demo_charge_attack`
- `vc5_demo_decisive_charge`
- `vc5_demo_close_assault`
- `vc5_demo_pursuit`

评分倾向：

- 能移动到敌人身边才优先。
- `冲锋攻击` 只按最多 2 格移动判断。
- `决胜突击` 按自身攻击力 +1 判断击杀价值。
- `追击` 只有场上存在受伤且可贴近的敌人时才允许进入候选，避免出牌后因第二步无合法目标而取消并重复出牌。
- 贴身类牌比普通移动牌更倾向于对敌人施压。

### 推进/占位牌

包括：

- `vc5_demo_raid`
- `vc5_demo_full_speed_advance`
- `vc5_demo_quick_move`
- `vc5_demo_line_advance`
- `vc5_demo_high_ground`

评分倾向：

- 优先选择离得分区更远、需要推进的己方角色。
- 选择格子时，优先选择更靠近得分区的合法空格。
- 快速行动牌有额外加分，因为不会消耗主要行动。

### 防御牌

包括：

- `vc5_demo_protect_shooter`

评分倾向：

- 优先保护已受伤角色。
- 优先保护 `vc5_demo_sniper`。

### 后撤牌

包括：

- `vc5_demo_fallback`
- `vc5_demo_retreat_2`

评分倾向：

- 只有在角色处于威胁范围内时明显加分。
- 威胁范围按“敌方攻击范围 + 敌方移动力”粗略判断。

### 齐射

卡牌：

- `vc5_demo_volley`

评分倾向：

- 只在至少 1 名己方角色能攻击同一个敌人时使用。
- 能参与齐射的己方角色越多，分数越高。
- 总伤害越高，分数越高。

## 选择链处理

当前 demo 卡牌大量采用“先选己方角色，再选目标”的交互。AI 会在 selector 状态下根据当前 ability 和已选择的 `ability_triggerer` 继续选择。

### 选择己方角色

当当前合法目标都是己方角色时：

- 攻击牌：选择能打到最佳敌人的角色。
- 贴身/突击牌：选择能贴近敌人的角色。
- 推进牌：选择距离得分区更远的角色。
- 防御牌：选择受伤较多或狙击手。
- 后撤牌：选择处于威胁范围内的角色。

### 选择敌人

当当前合法目标是敌方角色时：

- 优先选择可击杀目标。
- 其次选择攻击力高的目标。
- 再其次选择生命值低的目标。
- 如果敌人在得分区，额外加分。

### 选择格子

由于 `AbilityData.GetSlotTargets()` 只枚举 `AllSlots`，而 demo 卡牌很多是 `SelectTarget` 选格子，所以 AI 不依赖 `GetSlotTargets()`。

当前实现会遍历 `Slot.GetAll()`，再用 `ability.CanTarget(data, caster, slot)` 过滤合法格子。

格子评分：

- 越接近得分区分数越高。
- 同等接近时，移动距离稍远的格子略优先，用于更积极推进。

## 当前不会做的事

当前版本为了稳定和可读性，暂不做以下行为：

- 不做深度搜索或复杂预测。
- 不根据玩家具体卡组做针对性策略。
- 不记忆上一回合玩家行为。
- 不做随机 bluff 或故意弱化。
- 不直接改写卡牌效果；AI 只根据当前已实现卡牌行为做选择。

## 调整建议

后续如果想调整 AI，可以优先改这些内容：

| 想调整的体验 | 推荐修改点 |
|---|---|
| AI 更激进 | 提高攻击牌、突击牌、可击杀目标的评分 |
| AI 更爱抢点 | 提高推进牌和得分区敌人/己方角色的评分 |
| AI 更保守 | 提高防御牌、后撤牌、低血保护评分 |
| AI 更像射击卡组 | 提高 `瞄准射击`、`远程压制`、`占据高位` 权重 |
| AI 更像机动卡组 | 提高 `奔袭`、`冲锋攻击`、`决胜突击` 权重 |

## 验证记录

### Demo 发布完整对局门禁（2026-08-18 开始执行）

- 使用同一套 `Vc5DemoAIPlanner` 同时驱动双方，不依赖 UI、网络或协程等待。
- 覆盖 B 对 B、B 对 C、C 对 B、C 对 C，每种组合连续运行 10 局。
- 单局最多允许 500 次 AI 决策；连续 3 次决策后局面没有变化，或停留在无法处理的 selector/阶段时，测试立即失败并输出当前回合、阶段、selector、双方分数、手牌和牌库数量。
- 正常结束原因必须是：一方达到 9 分、双方同时达到 9 分判平局，或某方需要抽牌但牌库为空判负。
- 每种组合记录完成局数、平均回合数、出牌次数、放弃主要阶段次数和胜负原因；该门禁只证明不会卡死和能按规则结束，不代表 AI 强度或趣味性已经通过人工验收。
- 首轮 B 对 B 在第 1 回合复现 `追击 -> 选择己方角色 -> 无受伤敌人 -> 取消 -> 再次追击` 循环；修复方向是让 planner 在出牌评分阶段验证 `追击` 的受伤目标前置条件。
- 已修复上述循环：`追击` 在不存在受伤敌人时不再进入出牌与取消循环；攻击类和齐射类卡牌没有完整合法目标时也不会被 planner 选中。
- 修复后四种组合各连续运行 10 局，共 40 局全部正常结束：

| 组合 | 平均回合数 | 出牌次数 | 放弃主要阶段次数 | 结束原因 |
|---|---:|---:|---:|---|
| B 对 B | 5.0 | 163 | 91 | 平局 2、P1 得分胜 3、P0 得分胜 5 |
| B 对 C | 3.4 | 91 | 65 | P1 得分胜 9、P0 得分胜 1 |
| C 对 B | 3.1 | 92 | 57 | P0 得分胜 10 |
| C 对 C | 3.1 | 87 | 60 | P1 得分胜 5、P0 得分胜 5 |

- 本批模拟对局均由得分规则结束；牌库耗尽判负由独立规则测试覆盖。该结果只证明 AI 不会卡死且能完成对局，强度、节奏和出牌观感仍需人工试玩确认。

2026-07-12 已通过 Unity MCP 验证：

- Console 无编译错误。
- `Vc5DemoAITests` 4 个测试通过：
  - `AIType20_CreatesVc5DemoAI`
  - `Planner_ChoosesPlayableBasicAttackWhenItCanHit`
  - `GameplayData_DefaultsToVc5DemoAIAndDemoAIDecks`
  - `Planner_CompletesBasicAttackSelectionChain`
