using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 觉醒阶段枚举
    /// Awakening Stage Enum
    /// </summary>
    public enum AwakeningStage
    {
        None = 0,       // 未觉醒
        First = 1,      // 初醒
        Second = 2,     // 进阶
        Third = 3       // 升华
    }

    /// <summary>
    /// 觉醒条件类型
    /// Awakening Condition Type
    /// </summary>
    public enum AwakeningConditionType
    {
        None = 0,
        SlimeOnTarget = 1,          // 单目标粘液层数
        TotalSlimeApplied = 2,      // 累计施加粘液总量
        TotalDamageDealt = 3,       // 累计造成伤害
        TotalAttacks = 4,           // 累计攻击次数
        TotalKills = 5,             // 累计击杀数
        HPLessThan = 6,             // 生命值条件
        SurviveTurns = 7,           // 存活回合数
        TotalMoves = 8,             // 累计位移次数
        CardsPlayed = 9,            // 累计使用卡牌数
        AllyDeaths = 10,            // 友方死亡数
        TotalBleedOnEnemies = 11,   // 场上敌人流血总量
        TotalHPPaid = 12,           // 累计支付生命值
        TotalGuardUsed = 13,        // 累计使用守护次数
        TotalDamageAbsorbed = 14,   // 累计吸收伤害
        TotalSummons = 15           // 累计召唤数量
    }

    /// <summary>
    /// 觉醒加成类型 - 当回合加成
    /// Awakening Turn Bonus Type
    /// </summary>
    public enum TurnBonusType
    {
        None = 0,
        DamageAllWithSlime = 1,     // 对所有带粘液敌人造成伤害
        ApplySlimeToAll = 2,        // 对所有敌人施加粘液
        MoveAndDamage = 3,          // 移动并造成伤害
        SummonSpawns = 4,           // 召唤幼崽
        HealAllAllies = 5,          // 治疗所有友军
        BuffAllAllies = 6,          // 强化所有友军
        DamageAOE = 7,              // AOE伤害
        SpecialAbility = 8          // 特殊能力（自定义）
    }

    /// <summary>
    /// 觉醒加成类型 - 永续加成
    /// Awakening Permanent Bonus Type
    /// </summary>
    public enum PermanentBonusType
    {
        None = 0,
        AttackPlus = 1,             // 攻击力+
        AttackRangePlus = 2,        // 攻击距离+
        HPPlus = 3,                 // 生命值+
        MovePlus = 4,               // 移动力+
        AddSlimeOnAttack = 5,       // 攻击附带粘液
        DamageBonusVsSlime = 6,     // 对带粘液敌人伤害+
        SlownessDebuff = 7,         // 带粘液敌人减速
        WeakenDebuff = 8,           // 带粘液敌人弱化
        DamageOnMove = 9,           // 位移造成伤害
        MoveWithoutSlime = 10,      // 无需粘液即可位移
        AutoSlimePerTurn = 11,      // 每回合自动施加粘液
        SpawnResurrection = 12,     // 幼崽复活
        DamageOnMoveEnd = 13,       // 移动终点造成伤害
        BleedOnAttack = 14,         // 攻击附带流血
        RemoveAttackRestriction = 15,// 移除攻击限制
        AutoGuardPerTurn = 16,      // 每回合自动守护
        SharpOnAttack = 17          // 攻击后获得尖锐
    }

    /// <summary>
    /// 定义觉醒数据
    /// Defines awakening data for a hero
    /// </summary>
    [CreateAssetMenu(fileName = "awakening", menuName = "TcgEngine/AwakeningData", order = 20)]
    public class AwakeningData : ScriptableObject
    {
        public string id;
        public string hero_id;              // 关联的英雄ID

        [Header("初醒 First Awakening")]
        public int first_condition_value;           // 条件数值
        public AwakeningConditionType first_condition_type;  // 条件类型
        public TurnBonusType first_turn_bonus;      // 当回合加成类型
        public int first_turn_bonus_value;          // 当回合加成数值
        public PermanentBonusType first_permanent_bonus;     // 永续加成类型
        public int first_permanent_bonus_value;     // 永续加成数值
        public AbilityData first_ability;           // 初醒触发的技能（可选）

        [Header("进阶 Second Awakening")]
        public int second_condition_value;
        public AwakeningConditionType second_condition_type;
        public TurnBonusType second_turn_bonus;
        public int second_turn_bonus_value;
        public PermanentBonusType second_permanent_bonus;
        public int second_permanent_bonus_value;
        public AbilityData second_ability;

        [Header("升华 Third Awakening")]
        public int third_condition_value;
        public AwakeningConditionType third_condition_type;
        public TurnBonusType third_turn_bonus;
        public int third_turn_bonus_value;
        public PermanentBonusType third_permanent_bonus;
        public int third_permanent_bonus_value;
        public AbilityData third_ability;

        [Header("Text")]
        public string title;
        [TextArea(3, 5)]
        public string desc;

        // 静态列表用于快速访问
        public static List<AwakeningData> awakening_list = new List<AwakeningData>();
        public static Dictionary<string, AwakeningData> awakening_dict = new Dictionary<string, AwakeningData>();

        public static void Load(string folder = "")
        {
            if (awakening_list.Count == 0)
            {
                awakening_list.AddRange(Resources.LoadAll<AwakeningData>(folder));
                foreach (AwakeningData awakening in awakening_list)
                    awakening_dict[awakening.id] = awakening;
            }
        }

        public static AwakeningData Get(string id)
        {
            if (awakening_dict.ContainsKey(id))
                return awakening_dict[id];
            return null;
        }

        /// <summary>
        /// 获取指定阶段的条件数值
        /// </summary>
        public int GetConditionValue(AwakeningStage stage)
        {
            switch (stage)
            {
                case AwakeningStage.First: return first_condition_value;
                case AwakeningStage.Second: return second_condition_value;
                case AwakeningStage.Third: return third_condition_value;
                default: return 0;
            }
        }

        /// <summary>
        /// 获取指定阶段的条件类型
        /// </summary>
        public AwakeningConditionType GetConditionType(AwakeningStage stage)
        {
            switch (stage)
            {
                case AwakeningStage.First: return first_condition_type;
                case AwakeningStage.Second: return second_condition_type;
                case AwakeningStage.Third: return third_condition_type;
                default: return AwakeningConditionType.None;
            }
        }

        /// <summary>
        /// 获取指定阶段的当回合加成
        /// </summary>
        public TurnBonusType GetTurnBonusType(AwakeningStage stage)
        {
            switch (stage)
            {
                case AwakeningStage.First: return first_turn_bonus;
                case AwakeningStage.Second: return second_turn_bonus;
                case AwakeningStage.Third: return third_turn_bonus;
                default: return TurnBonusType.None;
            }
        }

        /// <summary>
        /// 获取指定阶段的永续加成
        /// </summary>
        public PermanentBonusType GetPermanentBonusType(AwakeningStage stage)
        {
            switch (stage)
            {
                case AwakeningStage.First: return first_permanent_bonus;
                case AwakeningStage.Second: return second_permanent_bonus;
                case AwakeningStage.Third: return third_permanent_bonus;
                default: return PermanentBonusType.None;
            }
        }

        /// <summary>
        /// 获取指定阶段触发的技能
        /// </summary>
        public AbilityData GetAbility(AwakeningStage stage)
        {
            switch (stage)
            {
                case AwakeningStage.First: return first_ability;
                case AwakeningStage.Second: return second_ability;
                case AwakeningStage.Third: return third_ability;
                default: return null;
            }
        }
    }
}
