# VC5 觉醒与地图设计稿

> **项目**: 战场女武神5 (Valkyria Chronicles 5) -- 六边形战棋卡牌游戏
> **框架**: TcgEngine (Unity)
> **版本**: v1.0 Draft
> **日期**: 2026-05-24
> **状态**: 设计阶段

---

## 目录

1. [MDA 框架分析](#1-mda-框架分析)
2. [三段式觉醒系统设计](#2-三段式觉醒系统设计)
3. [地块系统设计](#3-地块系统设计)
4. [策略深度分析](#4-策略深度分析)
5. [示例设计](#5-示例设计)
6. [实现路线图](#6-实现路线图)

---

## 1. MDA 框架分析

### 1.1 Mechanics（机制）

本设计的核心机制由两大系统构成：**三段式觉醒系统** 与 **地块系统**。

#### 1.1.1 三段式觉醒机制

| 机制要素 | 说明 |
|---------|------|
| 觉醒对象 | 仅英雄（`CardType.Hero`）可觉醒，每个英雄独立走完三段觉醒 |
| 觉醒阶段 | 初醒（Tier 1）-> 进阶（Tier 2）-> 升华（Tier 3） |
| 觉醒条件 | 每阶段需满足特定条件，逐级苛刻（详见第2章） |
| 觉醒加成 | 分为"当回合加成"（Turn Bonus）和"永续加成"（Permanent Bonus） |
| 觉醒时机 | 条件满足时自动触发（无需手动激活），在条件检查点进行判定 |
| 觉醒不可逆 | 已完成的觉醒阶段不会因状态变化而回退 |

**核心规则**：

- 每个英雄拥有独立的 `AwakeningLevel`（0/1/2/3），初始为 0
- 觉醒条件检查时机：`EndOfTurn`（回合结束时）、`OnKill`（击杀时）、`OnAfterAttack`（攻击后）等关键节点
- 觉醒条件由 `AwakeningConditionData` 定义，支持多种条件类型
- 当回合加成在觉醒当回合结束后自动消失，永续加成持续到英雄死亡

#### 1.1.2 地块机制

| 机制要素 | 说明 |
|---------|------|
| 地块数量 | 每局 3 个特殊地块 |
| 可见性 | 开局全部可见（区别于漫威终极逆转的逐步揭示） |
| 地块类型 | 得分地块、觉醒加速地块、行动干扰地块 |
| 占领判定 | 英雄或角色移动到地块所在 `Slot` 并停留至回合结束 |
| 地块效果 | 回合结束时结算占领效果 |
| 地块可争夺 | 任何一方均可占领，占领权随单位离开而丧失 |

**核心规则**：

- 地块在 `Game.StartGame()` 时随机生成，位置遵循分布规则（详见第3章）
- 地块效果在 `EndOfTurn` 阶段结算，检查每个地块上是否有单位
- 地块效果作用于占领方玩家，不作用于特定卡牌（除非地块类型特别指定）

### 1.2 Dynamics（动态）

#### 1.2.1 觉醒驱动的行为模式

```
玩家行为链条：

条件积累阶段 -> 觉醒触发 -> 加成利用 -> 下一阶段条件积累
     |                |            |
     v                v            v
  策略性布局       短期爆发      长期规划
  （移动/攻击）   （利用当回合） （永续加成收益）
```

**典型动态行为**：

1. **条件 farming**：玩家有意识地执行特定行为以满足觉醒条件（如黏黏流玩家刻意给敌人叠粘液层数）
2. **觉醒节奏控制**：选择在合适的时机触发觉醒（如等待对方关键单位出现后再触发升华）
3. **多英雄觉醒优先级**：3 个英雄各自独立觉醒，玩家需决策先推进哪个英雄的觉醒
4. **觉醒后战术切换**：永续加成改变英雄定位，玩家需调整整体战术

#### 1.2.2 地块驱动的行为模式

```
地块争夺行为链条：

开局观察地块 -> 制定路线 -> 移动争夺 -> 占领维持 -> 效果结算
     |              |            |            |            |
     v              v            v            v            v
  战略规划       路径选择     行动消耗     防守决策     收益评估
```

**典型动态行为**：

1. **开局博弈**：根据地块位置决定英雄初始移动方向和出牌策略
2. **路线冲突**：双方争夺同一地块时产生的直接对抗
3. **地块-觉醒协同**：占领觉醒加速地块以更快完成觉醒，形成正向循环
4. **机会成本权衡**：争夺地块消耗行动力，可能延误其他战术目标

#### 1.2.3 觉醒与地块的交叉动态

| 交互场景 | 产生的动态行为 |
|---------|--------------|
| 觉醒加速地块 + 觉醒条件 | 玩家优先争夺加速地块，以更快完成觉醒 |
| 得分地块 + 觉醒后强力英雄 | 将觉醒后的强力英雄部署在得分地块上，最大化收益 |
| 行动干扰地块 + 对方即将觉醒 | 争夺干扰地块以阻止对方完成觉醒 |
| 多地块分布 + 多英雄觉醒 | 不同英雄负责不同地块，形成分工策略 |

### 1.3 Aesthetics（美学）

#### 1.3.1 核心美学体验目标

| 美学类型 | 描述 | 驱动系统 |
|---------|------|---------|
| **挑战 (Challenge)** | 觉醒条件的设计让玩家感到"差一点就够"的紧张感，推动反复尝试和优化 | 觉醒系统 |
| **涌现叙事 (Emergent Narrative)** | 每局游戏因地块随机分布和觉醒路径选择产生不同故事线 | 地块 + 觉醒 |
| **发现 (Discovery)** | 三段觉醒的逐步揭示让玩家探索每个英雄的完整潜力 | 觉醒系统 |
| **表达 (Expression)** | 不同的觉醒路线和地块争夺策略体现玩家的个人风格 | 双系统 |
| ** Submission (沉浸)** | 觉醒触发的视觉反馈和地块效果营造战场氛围 | 双系统 |

#### 1.3.2 情感曲线设计

```
情绪强度
  ^
  |          * 觉醒触发
  |         / \      * 升华觉醒
  |   *    /   \    /
  |  / \  /     \  /
  | /   \/       \/
  |/    *初醒     \
  +---------------------------> 时间
  开局  中期  后期  终局
  |<--->|<--->|<--->|
  地块   觉醒  高潮
  争夺   积累  对决
```

- **开局**：地块分布揭示，玩家制定策略（期待感）
- **中期**：条件积累与地块争夺（紧张感、策略思考）
- **觉醒触发**：加成生效的爽快感（成就感）
- **终局**：升华觉醒 + 地块收益的最终对决（高潮体验）

---

## 2. 三段式觉醒系统设计

### 2.1 设计理念

#### 2.1.1 为什么是三段式

| 设计考量 | 说明 |
|---------|------|
| **成长感** | 三段式提供清晰的成长曲线，玩家能感受到英雄从普通到超凡的蜕变 |
| **策略深度** | 每段觉醒的条件和加成都不同，玩家需要在不同阶段做出不同决策 |
| **节奏控制** | 三段式将觉醒分散到游戏的前、中、后期，避免一次性觉醒的节奏断裂 |
| **流派一致性** | 每段觉醒都与英雄流派核心机制挂钩，强化流派特色 |
| **信息可读性** | 三段比四段或更多段更容易记忆和理解，降低认知负担 |

#### 2.1.2 设计目标

1. **移除旧觉醒设计**：不再使用 `AbilityTrigger.Activate` + `ConditionData` 的觉醒模式
2. **独立觉醒系统**：觉醒作为英雄的固有属性，与能力系统解耦
3. **视觉反馈**：每段觉醒有独特的视觉表现（模型变化、特效升级）
4. **平衡性**：三段觉醒的总收益应与"一个强力主动技能"相当，但分散在三个阶段
5. **不可逆性**：觉醒一旦触发不可回退，增加决策权重

### 2.2 觉醒阶段定义

#### 2.2.1 第一阶段「初醒」

| 属性 | 说明 |
|------|------|
| **条件难度** | 低 -- 通常在游戏前 3-4 回合内可完成 |
| **加成幅度** | 小 -- 相当于一个被动技能的弱化版 |
| **设计定位** | "英雄开始展现潜力"，给玩家一个早期小目标 |
| **当回合加成** | 一次性效果，如：立即恢复 2 点生命、本回合攻击力 +2 |
| **永续加成** | 持续被动，如：攻击力永久 +1、获得某个弱化版被动 |

#### 2.2.2 第二阶段「进阶」

| 属性 | 说明 |
|------|------|
| **条件难度** | 中 -- 通常在游戏第 5-8 回合完成 |
| **加成幅度** | 中 -- 相当于一个完整的被动技能 |
| **设计定位** | "英雄实力显著提升"，改变英雄在战场中的角色 |
| **当回合加成** | 强力一次性效果，如：立即对周围敌人造成伤害、抽 2 张牌 |
| **永续加成** | 显著被动，如：攻击距离 +1、获得新能力、数值大幅提升 |

#### 2.2.3 第三阶段「升华」

| 属性 | 说明 |
|------|------|
| **条件难度** | 高 -- 通常在游戏第 9+ 回合才能完成，甚至可能无法完成 |
| **加成幅度** | 大 -- 足以改变战局走向 |
| **设计定位** | "英雄达到巅峰状态"，作为游戏后期的决胜手段 |
| **当回合加成** | 终极效果，如：全场效果、复活、大规模伤害 |
| **永续加成** | 游戏改变者，如：每次攻击附带额外效果、获得全新主动技能 |

### 2.3 觉醒条件设计原则

#### 2.3.1 条件类型分类

觉醒条件支持以下类型，通过 `AwakeningConditionType` 枚举定义：

```csharp
public enum AwakeningConditionType
{
    None = 0,

    // --- 累积型条件（需要多次行为积累） ---
    SlimeStackOnEnemy = 10,      // 敌人粘液总层数达到 X
    DamageDealt = 11,            // 累计造成 X 点伤害
    KillCount = 12,              // 累计击杀 X 个敌方单位
    CardPlayed = 13,             // 累计打出 X 张牌
    TurnCount = 14,              // 游戏进行到第 X 回合
    MoveCount = 15,              // 累计移动 X 次
    AttackCount = 16,            // 累计攻击 X 次

    // --- 状态型条件（需要在特定状态下触发） ---
    SelfHpBelow = 20,            // 自身生命值低于 X
    SelfHpAbove = 21,            // 自身生命值高于 X
    SelfStatusHas = 22,          // 自身拥有特定状态
    EnemyCountOnBoard = 23,      // 场上敌方单位数量 >= X
    AllyCountOnBoard = 24,       // 场上友方单位数量 >= X

    // --- 事件型条件（需要完成特定事件） ---
    OnSpecialTileHeld = 30,      // 占领特殊地块时
    ComboKill = 31,              // 一回合内击杀 X 个单位
    SurviveNTurns = 32,          // 连续存活 X 回合
}
```

#### 2.3.2 逐级苛刻原则

| 原则 | 说明 | 示例（血腥黏黏） |
|------|------|----------------|
| **数量递增** | 同类条件，数值逐级增加 | 初醒：敌人粘液 2 层 / 进阶：4 层 / 升华：8 层 |
| **类型升级** | 从简单条件升级为复合条件 | 初醒：造成伤害 / 进阶：击杀单位 / 升华：一回合击杀 2 个 |
| **风险递增** | 从安全条件升级为危险条件 | 初醒：回合数达标 / 进阶：生命值低于 3 / 升华：生命值低于 1 |
| **时间压力** | 后期觉醒需要更长时间积累 | 初醒：3 回合 / 进阶：6 回合 / 升华：10 回合 |

#### 2.3.3 条件检查与进度追踪

每个英雄在运行时维护一个 `AwakeningProgress` 数据结构：

```csharp
[System.Serializable]
public class AwakeningProgress
{
    public int level;                    // 当前觉醒等级 (0-3)
    public int progress_tier1;           // 第一阶段进度值
    public int progress_tier2;           // 第二阶段进度值
    public int progress_tier3;           // 第三阶段进度值
    public bool tier1_completed;         // 第一阶段是否已完成
    public bool tier2_completed;         // 第二阶段是否已完成
    public bool tier3_completed;         // 第三阶段是否已完成
    public int turn_bonus_remaining;     // 当回合加成剩余回合数
}
```

### 2.4 觉醒加成设计

#### 2.4.1 当回合加成（Turn Bonus）

当回合加成在觉醒触发的当回合立即生效，回合结束时消失。

| 设计要点 | 说明 |
|---------|------|
| **生效时机** | 觉醒触发后立即生效 |
| **持续时长** | 仅当前回合（`turn_bonus_remaining = 1`） |
| **效果强度** | 比同等级永续加成更强（约 1.5-2 倍） |
| **设计目的** | 提供即时的"觉醒爽感"，奖励玩家完成条件 |
| **实现方式** | 通过临时添加 `ongoing_status` 或 `ongoing_traits` 实现 |

**当回合加成类型**：

```csharp
public enum TurnBonusType
{
    None = 0,
    AddAttackTemp = 10,     // 临时攻击力加成
    AddHpTemp = 11,         // 临时生命值加成
    HealSelf = 12,          // 立即治疗自身
    DealDamageToEnemies = 13, // 立即对范围内敌人造成伤害
    DrawCards = 14,         // 立即抽牌
    GainMana = 15,          // 立即获得法力值
    ExtraAction = 16,       // 本回合额外行动
    ApplyStatusToEnemies = 17, // 立即对范围内敌人施加状态
}
```

#### 2.4.2 永续加成（Permanent Bonus）

永续加成在觉醒后永久持续，直到英雄死亡。

| 设计要点 | 说明 |
|---------|------|
| **生效时机** | 觉醒触发后立即生效 |
| **持续时长** | 永久（直到英雄死亡或对局结束） |
| **效果强度** | 稳定且持续，但单次不如当回合加成 |
| **设计目的** | 长期改变英雄定位，提供持续收益 |
| **实现方式** | 通过修改英雄的 `traits`、`status`（permanent）、或添加新 `abilities` 实现 |

**永续加成类型**：

```csharp
public enum PermanentBonusType
{
    None = 0,
    AddAttackPerm = 10,        // 永久攻击力加成
    AddHpPerm = 11,            // 永久生命值加成
    AddMoveRange = 12,         // 移动力增加
    AddAttackRange = 13,       // 攻击距离增加
    AddNewAbility = 14,        // 获得新能力
    AddNewTrait = 15,          // 获得新特质
    ModifyExistingAbility = 16, // 强化现有能力
    StatusImmunity = 17,       // 获得状态免疫
    OnAttackEffect = 18,       // 攻击时附带效果
    OnKillEffect = 19,         // 击杀时附带效果
    StartOfTurnEffect = 20,    // 回合开始时触发效果
}
```

### 2.5 觉醒与英雄流派的结合

#### 2.5.1 黏黏流（Slime）觉醒设计

黏黏流的核心机制是**粘液（Slime）状态**，所有觉醒条件与加成都围绕粘液展开。

**设计原则**：

- 条件与粘液层数、粘液施加次数、粘液消耗相关
- 加成强化粘液的施加效率或粘液相关的伤害/治疗
- 每段觉醒让英雄在"粘液操控者"的角色上更进一步

#### 2.5.2 战士流（Warrior）觉醒设计

战士流的核心机制是**尖锐（Sharp）状态**和**流血/直接伤害**。

**设计原则**：

- 条件与攻击次数、击杀数、自身受伤程度相关
- 加成强化攻击力、攻击距离、或攻击附带效果
- 每段觉醒让英雄在"近战输出者"的角色上更进一步

### 2.6 觉醒在代码层面的实现建议

#### 2.6.1 新增数据结构

**文件**: `Assets/TcgEngine/Scripts/Data/AwakeningData.cs`（新建）

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义英雄的三段式觉醒数据
    /// </summary>
    [System.Serializable]
    public class AwakeningTierData
    {
        [Header("Condition")]
        public AwakeningConditionType condition_type;
        public int condition_value;           // 条件数值（如"粘液层数>=4"中的4）

        [Header("Turn Bonus (当回合加成)")]
        public TurnBonusType turn_bonus_type;
        public int turn_bonus_value;
        public StatusType turn_bonus_status;  // 附加的状态类型
        public int turn_bonus_status_value;   // 附加的状态值

        [Header("Permanent Bonus (永续加成)")]
        public PermanentBonusType perm_bonus_type;
        public int perm_bonus_value;
        public string perm_bonus_trait_id;    // 附加的特质 ID
        public string perm_bonus_ability_id;  // 附加的能力 ID
        public StatusType perm_bonus_status;
        public int perm_bonus_status_value;

        [Header("Visual")]
        public GameObject awaken_fx;
        public AudioClip awaken_audio;
    }

    [CreateAssetMenu(fileName = "awakening", menuName = "TcgEngine/AwakeningData", order = 8)]
    public class AwakeningData : ScriptableObject
    {
        public string id;
        public string hero_card_id;           // 关联的英雄 CardData ID

        [Header("Awakening Tiers")]
        public AwakeningTierData tier1;       // 初醒
        public AwakeningTierData tier2;       // 进阶
        public AwakeningTierData tier3;       // 升华

        [Header("Display")]
        public string tier1_title = "初醒";
        public string tier2_title = "进阶";
        public string tier3_title = "升华";

        [TextArea(3, 5)]
        public string tier1_desc;
        [TextArea(3, 5)]
        public string tier2_desc;
        [TextArea(3, 5)]
        public string tier3_desc;

        // --- 静态管理 ---
        public static List<AwakeningData> awakening_list = new List<AwakeningData>();
        public static Dictionary<string, AwakeningData> awakening_dict = new Dictionary<string, AwakeningData>();

        public static void Load(string folder = "")
        {
            if (awakening_list.Count == 0)
            {
                awakening_list.AddRange(Resources.LoadAll<AwakeningData>(folder));
                foreach (AwakeningData aw in awakening_list)
                    if (!string.IsNullOrEmpty(aw.id))
                        awakening_dict[aw.id] = aw;
            }
        }

        public static AwakeningData Get(string id)
        {
            if (id == null) return null;
            awakening_dict.TryGetValue(id, out AwakeningData data);
            return data;
        }

        public static AwakeningData GetByHero(string hero_card_id)
        {
            foreach (AwakeningData aw in awakening_list)
            {
                if (aw.hero_card_id == hero_card_id)
                    return aw;
            }
            return null;
        }
    }
}
```

#### 2.6.2 扩展 Card 类

**文件**: `Assets/TcgEngine/Scripts/GameLogic/Card.cs`（修改）

在 `Card` 类中添加觉醒进度字段：

```csharp
// --- 觉醒系统（新增） ---
public int awakening_level = 0;           // 当前觉醒等级 0-3
public int awakening_progress_t1 = 0;     // 第一阶段进度
public int awakening_progress_t2 = 0;     // 第二阶段进度
public int awakening_progress_t3 = 0;     // 第三阶段进度
public int turn_bonus_remaining = 0;      // 当回合加成剩余回合
```

在 `Card.Clone()` 方法中添加对应字段的克隆逻辑。

#### 2.6.3 扩展 GameLogic 类

**文件**: `Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs`（修改）

添加觉醒检查和触发逻辑：

```csharp
// 在 GameLogic 中新增方法

/// <summary>
/// 检查并触发所有英雄的觉醒条件
/// </summary>
public void CheckAwakenings()
{
    foreach (Player player in game_data.players)
    {
        // 检查英雄觉醒
        if (player.hero != null && player.hero.awakening_level < 3)
            CheckHeroAwakening(player.hero);

        // 检查场上英雄卡觉醒（如果英雄作为场上单位）
        foreach (Card card in player.cards_board)
        {
            if (card.CardData.type == CardType.Hero && card.awakening_level < 3)
                CheckHeroAwakening(card);
        }
    }
}

/// <summary>
/// 检查单个英雄的觉醒条件
/// </summary>
private void CheckHeroAwakening(Card hero)
{
    AwakeningData awData = AwakeningData.GetByHero(hero.card_id);
    if (awData == null) return;

    int nextTier = hero.awakening_level + 1;
    AwakeningTierData tier = GetTierData(awData, nextTier);
    if (tier == null) return;

    // 更新进度
    UpdateAwakeningProgress(hero, tier);

    // 检查条件是否满足
    if (IsAwakeningConditionMet(hero, tier))
    {
        TriggerAwakening(hero, awData, nextTier, tier);
    }
}

/// <summary>
/// 触发觉醒
/// </summary>
private void TriggerAwakening(Card hero, AwakeningData awData, int tier, AwakeningTierData tierData)
{
    hero.awakening_level = tier;

    // 应用当回合加成
    ApplyTurnBonus(hero, tierData);

    // 应用永续加成
    ApplyPermanentBonus(hero, tierData);

    // 发送觉醒事件
    onHeroAwakened?.Invoke(hero, tier);

    // 刷新 UI
    RefreshData();
}
```

#### 2.6.4 新增事件定义

**文件**: `Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs`（修改）

```csharp
// 新增事件
public UnityAction<Card, int> onHeroAwakened;  // Hero, Tier Level
```

#### 2.6.5 新增 GameAction 常量

**文件**: `Assets/TcgEngine/Scripts/GameLogic/GameAction.cs`（修改）

```csharp
// 新增觉醒相关 Action
public const ushort HeroAwakened = 2080;        // 英雄觉醒
public const ushort AwakeningProgress = 2081;   // 觉醒进度更新
```

#### 2.6.6 觉醒条件检查时机

在以下节点插入 `CheckAwakenings()` 调用：

| 检查时机 | 对应方法 | 说明 |
|---------|---------|------|
| 回合结束 | `EndTurn()` | 检查回合数、存活回合等条件 |
| 击杀后 | `KillCard()` | 检查击杀数条件 |
| 攻击后 | `AttackCard()` | 检查伤害累计条件 |
| 出牌后 | `PlayCard()` | 检查出牌数条件 |
| 移动后 | `MoveCard()` | 检查移动次数条件 |
| 状态变化后 | `AddStatus()` | 检查粘液层数等状态条件 |

#### 2.6.7 与现有系统的兼容性

| 现有系统 | 兼容方案 |
|---------|---------|
| `AbilityData` | 觉醒永续加成可通过 `AddOngoingAbility()` 添加新能力 |
| `StatusData` | 觉醒加成可通过 `AddStatus(type, value, 0)` 添加永久状态 |
| `ConditionData` | 觉醒条件使用独立的 `AwakeningConditionType`，不与现有条件冲突 |
| `EffectData` | 当回合加成复用现有 Effect 系统 |
| `CardData.abilities` | 觉醒添加的新能力通过 `abilities_ongoing` 列表管理 |
| `CardData.traits` | 觉醒添加的新特质通过 `ongoing_traits` 列表管理 |

---

## 3. 地块系统设计

### 3.1 设计理念

#### 3.1.1 参考漫威终极逆转

| 漫威终极逆转特性 | VC5 适配方案 |
|----------------|-------------|
| 每局 3 个地块 | 保持不变：每局 3 个特殊地块 |
| 逐步揭示（Turn 1/2/3） | 改为开局全部可见，适配六边形战棋的规划型玩法 |
| 地块效果多样 | 定义 3 种核心地块类型，后续可扩展 |
| "第三位玩家"理念 | 地块作为中立要素影响双方决策 |
| 可读的随机性 | 地块位置随机但遵循分布规则，保证公平性 |

#### 3.1.2 适配六边形战棋的考量

- 六边形网格的移动距离计算与矩形不同，地块位置需要考虑六边形距离
- 双方各有自己的领地（通过 `Slot.p` 值区分），地块应倾向于分布在中间区域
- 地块效果应考虑六边形相邻关系（如"相邻地块"的概念）

### 3.2 地块分布规则

#### 3.2.1 棋盘坐标系

当前棋盘为 10x5 六边形网格（`Slot.x: 1-10`, `Slot.y: 1-5`）。

```
y=1  [1,1] [2,1] [3,1] [4,1] [5,1] [6,1] [7,1] [8,1] [9,1] [10,1]
y=2  [1,2] [2,2] [3,2] [4,2] [5,2] [6,2] [7,2] [8,2] [9,2] [10,2]
y=3  [1,3] [2,3] [3,3] [4,3] [5,3] [6,3] [7,3] [8,3] [9,3] [10,3]
y=4  [1,4] [2,4] [3,4] [4,4] [5,4] [6,4] [7,4] [8,4] [9,4] [10,4]
y=5  [1,5] [2,5] [3,5] [4,5] [5,5] [6,5] [7,5] [8,5] [9,5] [10,5]
```

英雄初始部署位置（三角形）：
- 左上: (3, 2)
- 右上: (2, 4)
- 下方中心: (2, 3)

#### 3.2.2 地块分布区域

将棋盘分为三个区域，每个地块从不同区域随机选取：

| 区域 | 坐标范围 | 说明 |
|------|---------|------|
| **A 区（左侧）** | x: 1-4, y: 1-5 | 靠近部署区域 |
| **B 区（中央）** | x: 5-7, y: 1-5 | 棋盘中央 |
| **C 区（右侧）** | x: 8-10, y: 1-5 | 远端区域 |

**分布规则**：

1. 3 个地块分别从 A、B、C 三个区域各选 1 个
2. 地块之间最小六边形距离 >= 3（避免过于集中）
3. 地块不与英雄初始部署位置重叠
4. 每局游戏开始时随机生成

#### 3.2.3 地块位置生成算法

```
GenerateTilePositions():
  1. 从 A 区随机选取候选位置集合
  2. 从 B 区随机选取候选位置集合
  3. 从 C 区随机选取候选位置集合
  4. 排除英雄部署位置
  5. 在候选集合中选择满足最小距离约束的组合
  6. 如果无满足约束的组合，放宽距离约束到 >= 2
  7. 为每个位置分配地块类型（确保 3 种类型各出现 1 次）
```

### 3.3 三种地块类型详细设计

#### 3.3.1 得分地块（Score Tile）

| 属性 | 说明 |
|------|------|
| **视觉标识** | 金色/黄色高亮 |
| **效果** | 回合结束时，占领方获得 1 分（`kill_count += 1`） |
| **叠加规则** | 不叠加，每回合最多获得 1 分 |
| **策略价值** | 高 -- 直接推进胜利条件（`kill_count >= 9`） |

**详细规则**：

- 占领判定：回合结束时，地块 `Slot` 上有己方单位（英雄或角色）
- 得分时机：`EndOfTurn` 阶段，在所有其他结算之后
- 得分效果：`player.kill_count += 1`
- 如果双方单位在同一地块上（理论上不会发生，因为同一 Slot 只能有一个单位），不得分
- 地块得分与击杀得分独立计算

**与现有胜利条件的关联**：

当前胜利条件为 `kill_count >= 9`，击杀得 3 分。得分地块每回合提供 1 分，意味着：
- 纯击杀路线：需击杀 3 个敌方单位（3 x 3 = 9 分）
- 纯地块路线：需占领得分地块 9 个回合（9 x 1 = 9 分）
- 混合路线：击杀 + 地块的组合

#### 3.3.2 觉醒加速地块（Awakening Boost Tile）

| 属性 | 说明 |
|------|------|
| **视觉标识** | 蓝色/紫色高亮 |
| **效果** | 回合结束时，占领方所有英雄的觉醒进度 +1 |
| **叠加规则** | 不叠加，每回合每个英雄最多 +1 |
| **策略价值** | 中高 -- 加速觉醒进程，间接提升战斗力 |

**详细规则**：

- 占领判定：同得分地块
- 加速时机：`EndOfTurn` 阶段
- 加速效果：占领方每个英雄的 `awakening_progress_t(N)` 各 +1
- 加速对所有英雄生效，不限于地块上的单位
- 加速效果对已完成的觉醒阶段无效

**觉醒进度计算修正**：

```
实际进度 = 自然积累进度 + 地块加速进度
```

例如：血腥黏黏的初醒条件为"敌人粘液总层数 >= 2"
- 自然积累：通过战斗给敌人施加粘液
- 地块加速：每回合占领觉醒加速地块，进度 +1
- 两者独立计算，取较大值判定是否满足条件

#### 3.3.3 行动干扰地块（Disruption Tile）

| 属性 | 说明 |
|------|------|
| **视觉标识** | 红色/暗红色高亮 |
| **效果** | 回合结束时，占领方选择对方一个单位，使其下回合进入「麻痹」状态 |
| **叠加规则** | 不叠加，每回合最多干扰 1 个单位 |
| **策略价值** | 中 -- 限制对方行动，但需要选择目标 |

**详细规则**：

- 占领判定：同得分地块
- 干扰时机：`EndOfTurn` 阶段
- 干扰效果：占领方玩家选择对方场上 1 个单位，对其施加 `StatusType.Paralysed`，持续 1 回合
- 如果对方没有场上单位，效果无效
- 选择目标通过 `SelectorType.SelectTarget` 实现
- 被干扰的单位下回合无法攻击、移动、使用技能

### 3.4 地块与觉醒的联动

#### 3.4.1 联动矩阵

| 场景 | 联动效果 |
|------|---------|
| 占领觉醒加速地块 + 黏黏流 | 加速粘液相关觉醒条件，更快触发觉醒 |
| 占领得分地块 + 觉醒后英雄 | 觉醒后的强力英雄在得分地块附近防守，稳定得分 |
| 占领干扰地块 + 对方即将升华 | 关键时机干扰对方即将完成升华的英雄 |
| 觉醒加速地块 + 战士流 | 加速击杀/攻击次数相关觉醒条件 |

#### 3.4.2 策略权衡

```
地块选择决策树：

                    开局观察地块分布
                          |
              +-----------+-----------+
              |           |           |
         得分地块优先  加速地块优先  干扰地块优先
              |           |           |
         快速得分路线   觉醒爆发路线   控制压制路线
              |           |           |
         风险：被反击   风险：前期弱   风险：收益慢
```

### 3.5 地块在代码层面的实现建议

#### 3.5.1 新增数据结构

**文件**: `Assets/TcgEngine/Scripts/Data/TileData.cs`（新建）

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// 地块类型枚举
    /// </summary>
    public enum TileType
    {
        None = 0,
        Score = 1,              // 得分地块
        AwakeningBoost = 2,     // 觉醒加速地块
        Disruption = 3,         // 行动干扰地块
    }

    /// <summary>
    /// 地块实例数据（运行时）
    /// </summary>
    [System.Serializable]
    public struct TileInstance
    {
        public TileType type;
        public Slot slot;
        public int owner_player_id;     // 当前占领方玩家 ID（-1 = 未被占领）
        public int consecutive_holds;   // 连续占领回合数

        public TileInstance(TileType type, Slot slot)
        {
            this.type = type;
            this.slot = slot;
            this.owner_player_id = -1;
            this.consecutive_holds = 0;
        }
    }

    /// <summary>
    /// 地块配置数据（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "tile_config", menuName = "TcgEngine/TileConfigData", order = 9)]
    public class TileConfigData : ScriptableObject
    {
        public TileType type;

        [Header("Score Tile Config")]
        public int score_per_turn = 1;

        [Header("Awakening Boost Config")]
        public int awakening_progress_per_turn = 1;

        [Header("Disruption Config")]
        public StatusType disruption_status = StatusType.Paralysed;
        public int disruption_duration = 1;

        [Header("Visual")]
        public Sprite tile_icon;
        public GameObject tile_fx;
        public AudioClip tile_audio;
    }
}
```

#### 3.5.2 扩展 Slot 结构

**文件**: `Assets/TcgEngine/Scripts/GameLogic/Slot.cs`（修改）

在 `Slot` 结构体中添加地块标识：

```csharp
// 注意：Slot 是 INetworkSerializable 的 struct，
// 地块信息不直接存在 Slot 上，而是存在 Game 中
// 因为地块是每局动态生成的
```

#### 3.5.3 扩展 Game 类

**文件**: `Assets/TcgEngine/Scripts/GameLogic/Game.cs`（修改）

```csharp
// 在 Game 类中新增字段
public TileInstance[] tiles;  // 本局的 3 个地块实例

/// <summary>
/// 获取指定 Slot 上的地块（如果有）
/// </summary>
public TileInstance GetTile(Slot slot)
{
    if (tiles == null) return default(TileInstance);
    foreach (TileInstance tile in tiles)
    {
        if (tile.slot == slot)
            return tile;
    }
    return default(TileInstance);
}

/// <summary>
/// 检查 Slot 是否有地块
/// </summary>
public bool HasTile(Slot slot)
{
    return GetTile(slot).type != TileType.None;
}
```

#### 3.5.4 地块生成逻辑

**文件**: `Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs`（修改）

在 `StartGame()` 方法中，`DeployInitialHeroes()` 之前添加：

```csharp
/// <summary>
/// 生成本局的地块配置
/// </summary>
private void GenerateTiles()
{
    game_data.tiles = new TileInstance[3];

    // 定义区域
    List<Slot> zone_a = new List<Slot>(); // x: 1-4
    List<Slot> zone_b = new List<Slot>(); // x: 5-7
    List<Slot> zone_c = new List<Slot>(); // x: 8-10

    int p = Slot.GetP(0); // 使用 p=0 的坐标（双方共享同一逻辑棋盘）

    for (int y = Slot.y_min; y <= Slot.y_max; y++)
    {
        for (int x = Slot.x_min; x <= Slot.x_max; x++)
        {
            Slot s = new Slot(x, y, p);
            if (x <= 4) zone_a.Add(s);
            else if (x <= 7) zone_b.Add(s);
            else zone_c.Add(s);
        }
    }

    // 排除部署位置
    SlotXY[] deploy_slots = new SlotXY[]
    {
        new SlotXY { x = 3, y = 2 },
        new SlotXY { x = 2, y = 4 },
        new SlotXY { x = 2, y = 3 },
    };
    foreach (var ds in deploy_slots)
    {
        Slot ds_slot = new Slot(ds.x, ds.y, p);
        zone_a.Remove(ds_slot);
        zone_b.Remove(ds_slot);
        zone_c.Remove(ds_slot);
    }

    // 随机选取
    Slot slot_a = zone_a[random.Next(zone_a.Count)];
    Slot slot_b = zone_b[random.Next(zone_b.Count)];
    Slot slot_c = zone_c[random.Next(zone_c.Count)];

    // 分配地块类型（随机排列）
    TileType[] types = new TileType[] { TileType.Score, TileType.AwakeningBoost, TileType.Disruption };
    ShuffleArray(types);

    game_data.tiles[0] = new TileInstance(types[0], slot_a);
    game_data.tiles[1] = new TileInstance(types[1], slot_b);
    game_data.tiles[2] = new TileInstance(types[2], slot_c);
}
```

#### 3.5.5 地块效果结算

**文件**: `Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs`（修改）

在 `EndTurn()` 方法中，`ReduceStatusDurations()` 之前添加：

```csharp
/// <summary>
/// 结算所有地块效果
/// </summary>
private void ResolveTiles()
{
    if (game_data.tiles == null) return;

    foreach (TileInstance tile in game_data.tiles)
    {
        Card occupant = game_data.GetSlotCard(tile.slot);
        if (occupant == null)
        {
            tile.owner_player_id = -1;
            tile.consecutive_holds = 0;
            continue;
        }

        int owner_pid = occupant.player_id;
        Player owner = game_data.GetPlayer(owner_pid);

        switch (tile.type)
        {
            case TileType.Score:
                owner.kill_count += 1;
                // 发送得分事件
                break;

            case TileType.AwakeningBoost:
                // 加速所有英雄的觉醒进度
                if (owner.hero != null && owner.hero.awakening_level < 3)
                    owner.hero.awakening_progress_t1 += 1; // 简化：对所有阶段进度 +1
                foreach (Card card in owner.cards_board)
                {
                    if (card.CardData.type == CardType.Hero && card.awakening_level < 3)
                        card.awakening_progress_t1 += 1;
                }
                break;

            case TileType.Disruption:
                // 触发选择目标流程
                TriggerDisruptionTile(owner, tile);
                break;
        }

        // 更新占领信息
        if (tile.owner_player_id == owner_pid)
            tile.consecutive_holds++;
        else
            tile.consecutive_holds = 1;
        tile.owner_player_id = owner_pid;
    }
}
```

#### 3.5.6 UI 层面

**文件**: `Assets/TcgEngine/Scripts/GameClient/BoardSlot.cs`（修改）

在 `BoardSlot` 中添加地块视觉显示：

```csharp
// 新增字段
public GameObject tile_indicator;      // 地块指示器
public SpriteRenderer tile_icon;       // 地块图标

/// <summary>
/// 更新地块显示
/// </summary>
public void UpdateTileDisplay(TileInstance tile)
{
    if (tile.type == TileType.None)
    {
        tile_indicator.SetActive(false);
        return;
    }

    tile_indicator.SetActive(true);
    // 根据 tile.type 设置不同的颜色和图标
    // 根据 tile.owner_player_id 设置占领标识
}
```

---

## 4. 策略深度分析

### 4.1 觉醒路线选择策略

#### 4.1.1 多英雄觉醒优先级

每局游戏有 3 个英雄，各自独立觉醒。玩家需要决策觉醒顺序：

| 策略 | 说明 | 适用场景 |
|------|------|---------|
| **集中突破** | 优先推进 1 个英雄到升华 | 该英雄的升华效果是战局关键 |
| **均衡发展** | 3 个英雄同步推进到进阶 | 需要整体实力提升 |
| **快速初醒** | 先让 3 个英雄都完成初醒 | 初醒条件简单，快速获得 3 个永续加成 |
| **针对性觉醒** | 根据对方阵容选择觉醒优先级 | 对方有强力英雄需要压制 |

#### 4.1.2 觉醒条件 farming 的机会成本

| 行为 | 觉醒收益 | 机会成本 |
|------|---------|---------|
| 刻意给敌人叠粘液 | 加速黏黏流觉醒 | 可能延误击杀节奏 |
| 刻意攻击而非移动 | 加速攻击次数觉醒 | 可能错过地块争夺 |
| 保留低血量英雄 | 触发"生命值低于 X"条件 | 英雄可能被击杀 |

### 4.2 地块争夺策略

#### 4.2.1 开局地块评估

```
地块评估维度：
1. 距离：地块离初始部署位置有多远？
2. 类型：地块类型与己方流派的契合度
3. 位置：地块是否在移动路线上？
4. 对称性：对方到达该地块的难度如何？
```

#### 4.2.2 地块争夺的节奏

| 阶段 | 策略重点 |
|------|---------|
| 前期（1-3 回合） | 抢占离自己最近的地块，建立优势 |
| 中期（4-7 回合） | 根据觉醒进度调整地块策略 |
| 后期（8+ 回合） | 地块得分可能成为胜负手 |

### 4.3 觉醒与地块的协同策略

#### 4.3.1 黏黏流协同策略

```
策略链条：
1. 优先争夺觉醒加速地块
2. 在加速地块附近给敌人叠粘液
3. 快速完成初醒和进阶
4. 利用觉醒加成争夺得分地块
5. 完成升华，形成碾压优势
```

#### 4.3.2 战士流协同策略

```
策略链条：
1. 优先争夺得分地块或干扰地块
2. 通过攻击积累觉醒条件
3. 初醒后获得攻击力加成，加强地块控制力
4. 进阶后获得攻击距离加成，可以远程控制地块
5. 升华后获得击杀特效，终结比赛
```

### 4.4 不同英雄流派的差异化策略

| 流派 | 觉醒倾向 | 地块倾向 | 整体策略 |
|------|---------|---------|---------|
| **黏黏流** | 觉醒加速地块 > 得分地块 | 中期觉醒爆发 | 控制型，通过粘液和觉醒压制 |
| **战士流** | 得分地块 > 干扰地块 | 前期进攻得分 | 进攻型，通过击杀和得分快速取胜 |
| **防御流** | 干扰地块 > 觉醒加速地块 | 控制对方行动 | 防守反击型，拖延对方觉醒 |
| **混合流** | 根据局势灵活选择 | 均衡发展 | 适应型，根据对方策略调整 |

---

## 5. 示例设计

### 5.1 英雄觉醒设计示例

#### 5.1.1 血腥黏黏（hero_bloody_slime）

**英雄基础数值**：攻击 1 / 生命 12 / 移动 3 / 攻击距离 1

**核心机制**：粘液（Slime）

**特殊技能**：被动 -- 己方黏黏对带粘液的敌人造成伤害时自愈 1

| 阶段 | 觉醒条件 | 当回合加成 | 永续加成 |
|------|---------|-----------|---------|
| **初醒** | 任一敌人粘液层数 >= 2 | 立即恢复 3 点生命 | 攻击力永久 +1 |
| **进阶** | 任一敌人粘液层数 >= 5 | 对所有带粘液敌人造成 2 点伤害 | 攻击距离永久 +1 |
| **升华** | 任一敌人粘液层数 >= 8 | 对所有带粘液敌人造成(2+层数/2)伤害并清空粘液 | 每次攻击附带 1 层粘液 |

**设计说明**：

- 初醒条件宽松（2 层粘液），让玩家在前期就能获得第一个觉醒
- 进阶条件中等（5 层粘液），需要刻意积累粘液
- 升华条件苛刻（8 层粘液），需要大量粘液积累，但效果足以清场
- 当回合加成与永续加成都围绕粘液机制，强化流派特色

#### 5.1.2 腐蚀黏黏（hero_corrosion_slime）

**英雄基础数值**：攻击 1 / 生命 14 / 移动 3 / 攻击距离 1

**核心机制**：粘液 + 减速

**特殊技能**：被动 -- 带粘液敌人移动力 -1（不叠加）

| 阶段 | 觉醒条件 | 当回合加成 | 永续加成 |
|------|---------|-----------|---------|
| **初醒** | 累计施加粘液 6 层 | 对攻击距离内所有敌人施加 1 层粘液 | 生命值永久 +2 |
| **进阶** | 累计施加粘液 15 层 | 对攻击距离内所有敌人施加 2 层粘液 | 带粘液敌人攻击力 -1 |
| **升华** | 累计施加粘液 30 层 | 对全场敌人施加 3 层粘液 | 带粘液敌人攻击距离 -1（与减速叠加） |

**设计说明**：

- 腐蚀黏黏的觉醒围绕"粘液施加总量"而非"单目标层数"
- 永续加成逐步强化对带粘液敌人的削弱效果
- 升华后的全场粘液施加是改变战局的关键手段

#### 5.1.3 狂战士（hero_warrior_berserker）

**英雄基础数值**：攻击 1 / 生命 12 / 移动 3 / 攻击距离 2

**核心机制**：尖锐（Sharp）+ 爆发伤害

**特殊技能**：被动 -- 自身获得的尖锐不会因时间减少

| 阶段 | 觉醒条件 | 当回合加成 | 永续加成 |
|------|---------|-----------|---------|
| **初醒** | 累计攻击 3 次 | 本回合攻击力 +3 | 攻击力永久 +1 |
| **进阶** | 累计造成 15 点伤害 | 立即对攻击距离内所有敌人造成攻击力伤害 | 攻击距离永久 +1 |
| **升华** | 生命值 <= 3 | 对攻击距离内所有敌人造成(攻击力x2)伤害，然后自尽 | 每次攻击后获得 1 层尖锐 |

**设计说明**：

- 狂战士的觉醒条件围绕攻击和伤害，鼓励进攻型玩法
- 升华条件"生命值 <= 3"是高风险条件，需要玩家刻意控制血量
- 升华的当回合加成保留了旧设计中"自尽"的特色，但伤害翻倍
- 升华的永续加成让狂战士在攻击中不断变强

### 5.2 地块效果示例

#### 5.2.1 典型对局地块分布

```
y=1  [  ] [  ] [  ] [  ] [  ] [  ] [  ] [  ] [  ] [  ]
y=2  [  ] [  ] [H1] [  ] [  ] [  ] [  ] [  ] [  ] [  ]
y=3  [  ] [H3] [  ] [  ] [B2] [  ] [  ] [  ] [  ] [  ]
y=4  [  ] [H2] [  ] [  ] [  ] [  ] [  ] [  ] [  ] [  ]
y=5  [  ] [  ] [  ] [  ] [  ] [  ] [  ] [  ] [  ] [  ]

H1, H2, H3 = 英雄初始部署位置
B2 (5, 3) = 觉醒加速地块（B 区中央）

A 区地块: (3, 5) = 得分地块
B 区地块: (5, 3) = 觉醒加速地块
C 区地块: (9, 2) = 行动干扰地块
```

#### 5.2.2 地块效果结算示例

**场景**：回合结束时，玩家 0 的血腥黏黏站在觉醒加速地块 (5, 3) 上

```
结算流程：
1. 检查地块 (5, 3) 上的单位 -> 血腥黏黏（玩家 0）
2. 地块类型 = AwakeningBoost
3. 效果：玩家 0 所有英雄觉醒进度 +1
4. 更新占领信息：owner_player_id = 0, consecutive_holds++
```

**场景**：回合结束时，玩家 1 的狂战士站在得分地块 (3, 5) 上

```
结算流程：
1. 检查地块 (3, 5) 上的单位 -> 狂战士（玩家 1）
2. 地块类型 = Score
3. 效果：玩家 1 kill_count += 1
4. 更新占领信息：owner_player_id = 1, consecutive_holds++
```

---

## 6. 实现路线图

### 6.1 分阶段开发建议

#### Phase 1：基础框架（预计 2 周）

| 任务 | 优先级 | 涉及文件 | 说明 |
|------|--------|---------|------|
| 新建 `AwakeningData.cs` | P0 | `Scripts/Data/AwakeningData.cs` | 觉醒数据结构定义 |
| 新建 `TileData.cs` | P0 | `Scripts/Data/TileData.cs` | 地块数据结构定义 |
| 扩展 `Card.cs` | P0 | `Scripts/GameLogic/Card.cs` | 添加觉醒进度字段 |
| 扩展 `Game.cs` | P0 | `Scripts/GameLogic/Game.cs` | 添加地块实例字段 |
| 扩展 `GameAction.cs` | P0 | `Scripts/GameLogic/GameAction.cs` | 添加觉醒/地块事件常量 |
| 扩展 `GameLogic.cs` | P0 | `Scripts/GameLogic/GameLogic.cs` | 添加觉醒检查和地块结算框架 |

#### Phase 2：觉醒系统实现（预计 2 周）

| 任务 | 优先级 | 涉及文件 | 说明 |
|------|--------|---------|------|
| 实现觉醒条件检查 | P0 | `GameLogic.cs` | `CheckAwakenings()` 方法 |
| 实现觉醒触发 | P0 | `GameLogic.cs` | `TriggerAwakening()` 方法 |
| 实现当回合加成 | P0 | `GameLogic.cs` | `ApplyTurnBonus()` 方法 |
| 实现永续加成 | P0 | `GameLogic.cs` | `ApplyPermanentBonus()` 方法 |
| 创建 ScriptableObject 资源 | P1 | `Resources/Abilities/vc5/` | 为现有英雄创建觉醒配置 |
| 觉醒进度 UI 显示 | P1 | `Scripts/GameClient/` | 在英雄卡牌上显示觉醒进度 |
| 觉醒触发特效 | P2 | `Animations/` | 觉醒触发时的视觉反馈 |

#### Phase 3：地块系统实现（预计 2 周）

| 任务 | 优先级 | 涉及文件 | 说明 |
|------|--------|---------|------|
| 实现地块生成 | P0 | `GameLogic.cs` | `GenerateTiles()` 方法 |
| 实现地块效果结算 | P0 | `GameLogic.cs` | `ResolveTiles()` 方法 |
| 得分地块逻辑 | P0 | `GameLogic.cs` | `kill_count` 加分 |
| 觉醒加速地块逻辑 | P0 | `GameLogic.cs` | 觉醒进度加速 |
| 行动干扰地块逻辑 | P1 | `GameLogic.cs` | 麻痹效果施加 |
| 地块 UI 显示 | P1 | `Scripts/GameClient/BoardSlot.cs` | 地块视觉标识 |
| 地块占领指示器 | P2 | `Scripts/GameClient/` | 显示当前占领方 |

#### Phase 4：联调与平衡（预计 2 周）

| 任务 | 优先级 | 说明 |
|------|--------|------|
| 觉醒条件平衡调整 | P0 | 根据测试数据调整各英雄觉醒条件数值 |
| 地块分布平衡 | P0 | 确保地块分布公平性 |
| 胜利条件调整 | P1 | 考虑地块得分对 `kill_count >= 9` 的影响 |
| AI 适配 | P1 | 让 AI 理解觉醒和地块的价值 |
| 网络同步 | P0 | 确保觉醒和地块状态在多人模式下正确同步 |

### 6.2 优先级排序

```
P0（必须完成）：
  - 觉醒数据结构
  - 地块数据结构
  - Card/Game 扩展
  - 觉醒条件检查与触发
  - 地块生成与结算
  - 得分地块逻辑
  - 觉醒加速地块逻辑
  - 网络同步

P1（重要）：
  - 行动干扰地块逻辑
  - 觉醒/地块 UI 显示
  - ScriptableObject 资源创建
  - AI 适配
  - 平衡调整

P2（锦上添花）：
  - 觉醒触发特效
  - 地块占领指示器
  - 音效
  - 高级地块类型扩展
```

### 6.3 风险与注意事项

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 觉醒条件过于苛刻导致无法触发 | 玩家体验差 | 提供觉醒加速地块作为兜底机制 |
| 地块分布不公平 | 一方始终占优 | 严格的分布规则 + 大量测试 |
| 觉醒加成过强破坏平衡 | 游戏失去策略性 | 分阶段测试，逐步调整数值 |
| 网络同步问题 | 多人模式异常 | 觉醒和地块状态变更需通过标准 Action 通道 |
| AI 无法理解新系统 | AI 表现差 | 为觉醒和地块添加 AI 启发式评估 |
| 与现有代码的兼容性 | 引入 Bug | 觉醒系统作为独立模块，最小化对现有代码的修改 |

---

## 附录

### A. 术语表

| 术语 | 英文 | 说明 |
|------|------|------|
| 觉醒 | Awakening | 英雄的阶段性强化机制 |
| 初醒 | First Awakening (Tier 1) | 觉醒第一阶段 |
| 进阶 | Advanced Awakening (Tier 2) | 觉醒第二阶段 |
| 升华 | Ultimate Awakening (Tier 3) | 觉醒第三阶段 |
| 当回合加成 | Turn Bonus | 觉醒当回合的临时效果 |
| 永续加成 | Permanent Bonus | 觉醒后的永久效果 |
| 地块 | Tile | 棋盘上的特殊区域 |
| 得分地块 | Score Tile | 占领后得分的地块 |
| 觉醒加速地块 | Awakening Boost Tile | 占领后加速觉醒的地块 |
| 行动干扰地块 | Disruption Tile | 占领后干扰对方的地块 |
| 占领 | Occupy | 单位站在地块上 |
| 粘液 | Slime | 黏黏流的核心状态 |
| 尖锐 | Sharp | 战士流的核心状态 |

### B. 相关文件索引

| 文件 | 路径 | 说明 |
|------|------|------|
| AbilityData | `Assets/TcgEngine/Scripts/Data/AbilityData.cs` | 能力数据定义 |
| ConditionData | `Assets/TcgEngine/Scripts/Data/ConditionData.cs` | 条件数据基类 |
| StatusData | `Assets/TcgEngine/Scripts/Data/StatusData.cs` | 状态效果定义 |
| EffectData | `Assets/TcgEngine/Scripts/Data/EffectData.cs` | 效果数据基类 |
| CardData | `Assets/TcgEngine/Scripts/Data/CardData.cs` | 卡牌数据定义 |
| Card | `Assets/TcgEngine/Scripts/GameLogic/Card.cs` | 卡牌运行时实例 |
| Slot | `Assets/TcgEngine/Scripts/GameLogic/Slot.cs` | 棋盘格定义 |
| Game | `Assets/TcgEngine/Scripts/GameLogic/Game.cs` | 游戏状态数据 |
| GameLogic | `Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs` | 游戏逻辑处理 |
| Player | `Assets/TcgEngine/Scripts/GameLogic/Player.cs` | 玩家数据 |
| GameAction | `Assets/TcgEngine/Scripts/GameLogic/GameAction.cs` | 网络事件常量 |
| GameplayData | `Assets/TcgEngine/Scripts/Data/GameplayData.cs` | 游戏配置 |
| Heroes.json | `Assets/Resources/Data/Heroes.json` | 英雄配置数据 |
| Cards.json | `Assets/Resources/Data/Cards.json` | 卡牌配置数据 |
| Skills.json | `Assets/Resources/Data/Skills.json` | 技能配置数据 |
| BoardSlot | `Assets/TcgEngine/Scripts/GameClient/BoardSlot.cs` | 棋盘格 UI |

### C. 漫威终极逆转地块参考

| 地块效果类别 | VC5 对应/参考 |
|-------------|-------------|
| 战力增减 | 觉醒永续加成（攻击力/生命值） |
| 规则改变 | 地块效果（行动干扰） |
| 卡牌摧毁 | 升华当回合加成（大规模伤害） |
| 费用操控 | （暂未实现，可作为未来扩展） |
| 特殊胜利条件 | 得分地块（加速 kill_count） |
| 能力修改 | 觉醒永续加成（新能力/特质） |
| 回合操控 | 行动干扰地块（麻痹） |
