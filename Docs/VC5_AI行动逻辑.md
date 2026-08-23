# VC5 AI 行动逻辑

更新时间：2026-08-18

本文档记录当前试玩 demo 中“可与玩家对战的 AI”的行为逻辑和实现方式。后续如果希望修改 AI 行为，优先修改本文档，再按本文档同步代码。

## 当前 AI 类型

| 项目 | 当前值 |
|---|---|
| AI 类型 | `AIType.Vc5Demo` |
| 枚举值 | `20` |
| 默认配置 | `Assets/TcgEngine/Resources/GameplayData.asset` 中 `ai_type: 20` |
| AI 卡组来源 | `GameplayData.ai_decks` |
| 默认 AI 卡组 | `deck_vc5_demo_mobile_assault`、`deck_vc5_demo_ranged_pressure` |

`Vc5DemoBootstrap.Register()` 会在运行时兜底：

- 将 `GameplayData.ai_type` 设置为 `AIType.Vc5Demo`。
- 将 B/C 两套 demo 卡组加入 `free_decks`。
- 将 B/C 两套 demo 卡组加入 `ai_decks`。
- 对 `DeckData.deck_list`、`free_decks`、`ai_decks` 做同 ID 去重，避免 Unity 热重载后重复出现。

## 代码入口

| 文件 | 职责 |
|---|---|
| `Assets/TcgEngine/Scripts/AI/AIPlayer.cs` | AI 工厂与 `AIType.Vc5Demo` 枚举 |
| `Assets/TcgEngine/Scripts/AI/AIPlayerVc5Demo.cs` | 实际 AI 玩家，负责在回合中调用 planner 并执行动作 |
| `Assets/TcgEngine/Scripts/AI/Vc5DemoAIPlanner.cs` | 规则式决策核心，负责选择下一步行动或选择链目标 |
| `Assets/TcgEngine/Scripts/Data/Vc5DemoBootstrap.cs` | 注册 demo 卡组，并同步默认 AI 配置 |
| `Assets/TcgEngine/Scripts/Testing/Editor/Vc5DemoAITests.cs` | AI 行为回归测试 |

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
