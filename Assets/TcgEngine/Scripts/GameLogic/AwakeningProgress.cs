using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 运行时觉醒进度数据
    /// Runtime awakening progress data for a hero card
    /// </summary>
    [System.Serializable]
    public class AwakeningProgress
    {
        public string card_uid;                     // 关联的卡牌UID
        public string awakening_id;                 // 觉醒数据ID
        public AwakeningStage current_stage = AwakeningStage.None;  // 当前觉醒阶段

        // 各阶段进度追踪
        public int slime_on_target_max = 0;         // 单目标最高粘液层数
        public int total_slime_applied = 0;         // 累计施加粘液总量
        public int total_damage_dealt = 0;          // 累计造成伤害
        public int total_attacks = 0;               // 累计攻击次数
        public int total_kills = 0;                 // 累计击杀数
        public int turns_survived = 0;              // 存活回合数
        public int total_moves = 0;                 // 累计位移次数
        public int cards_played = 0;                // 累计使用卡牌数
        public int ally_deaths = 0;                 // 友方死亡数
        public int total_bleed_on_enemies = 0;      // 场上敌人流血总量
        public int total_hp_paid = 0;               // 累计支付生命值
        public int total_guard_used = 0;            // 累计使用守护次数
        public int total_damage_absorbed = 0;       // 累计吸收伤害
        public int total_summons = 0;               // 累计召唤数量

        // 地块加速进度（额外加成）
        public int tile_boost_progress = 0;         // 地块加速提供的进度

        public AwakeningProgress(string uid, string awakeningId)
        {
            this.card_uid = uid;
            this.awakening_id = awakeningId;
        }

        /// <summary>
        /// 获取指定阶段的当前进度
        /// </summary>
        public int GetProgress(AwakeningStage stage, AwakeningData data)
        {
            if (data == null) return 0;

            AwakeningConditionType conditionType = data.GetConditionType(stage);
            int naturalProgress = GetNaturalProgress(conditionType);
            return naturalProgress + tile_boost_progress;
        }

        /// <summary>
        /// 获取自然积累进度（不含地块加速）
        /// </summary>
        private int GetNaturalProgress(AwakeningConditionType type)
        {
            switch (type)
            {
                case AwakeningConditionType.SlimeOnTarget: return slime_on_target_max;
                case AwakeningConditionType.TotalSlimeApplied: return total_slime_applied;
                case AwakeningConditionType.TotalDamageDealt: return total_damage_dealt;
                case AwakeningConditionType.TotalAttacks: return total_attacks;
                case AwakeningConditionType.TotalKills: return total_kills;
                case AwakeningConditionType.SurviveTurns: return turns_survived;
                case AwakeningConditionType.TotalMoves: return total_moves;
                case AwakeningConditionType.CardsPlayed: return cards_played;
                case AwakeningConditionType.AllyDeaths: return ally_deaths;
                case AwakeningConditionType.TotalBleedOnEnemies: return total_bleed_on_enemies;
                case AwakeningConditionType.TotalHPPaid: return total_hp_paid;
                case AwakeningConditionType.TotalGuardUsed: return total_guard_used;
                case AwakeningConditionType.TotalDamageAbsorbed: return total_damage_absorbed;
                case AwakeningConditionType.TotalSummons: return total_summons;
                default: return 0;
            }
        }

        /// <summary>
        /// 检查是否满足觉醒条件
        /// </summary>
        public bool CheckCondition(AwakeningStage stage, AwakeningData data)
        {
            if (data == null) return false;
            if (current_stage >= stage) return false; // 已觉醒或更高阶段

            int progress = GetProgress(stage, data);
            int required = data.GetConditionValue(stage);
            return progress >= required;
        }

        /// <summary>
        /// 执行觉醒
        /// </summary>
        public void Awaken(AwakeningStage stage)
        {
            if (stage > current_stage)
                current_stage = stage;
        }

        /// <summary>
        /// 增加地块加速进度
        /// </summary>
        public void AddTileBoost(int amount)
        {
            tile_boost_progress += amount;
        }

        // ========== 各种进度增加方法 ==========

        public void OnSlimeApplied(int amount)
        {
            total_slime_applied += amount;
        }

        public void OnSlimeOnTarget(int amount)
        {
            if (amount > slime_on_target_max)
                slime_on_target_max = amount;
        }

        public void OnDamageDealt(int amount)
        {
            total_damage_dealt += amount;
        }

        public void OnAttack()
        {
            total_attacks++;
        }

        public void OnKill()
        {
            total_kills++;
        }

        public void OnTurnSurvived()
        {
            turns_survived++;
        }

        public void OnMove()
        {
            total_moves++;
        }

        public void OnCardPlayed()
        {
            cards_played++;
        }

        public void OnAllyDeath()
        {
            ally_deaths++;
        }

        public void OnBleedOnEnemies(int amount)
        {
            total_bleed_on_enemies += amount;
        }

        public void OnHPPaid(int amount)
        {
            total_hp_paid += amount;
        }

        public void OnGuardUsed()
        {
            total_guard_used++;
        }

        public void OnDamageAbsorbed(int amount)
        {
            total_damage_absorbed += amount;
        }

        public void OnSummon()
        {
            total_summons++;
        }

        /// <summary>
        /// 重置所有进度（用于测试或新游戏）
        /// </summary>
        public void Reset()
        {
            current_stage = AwakeningStage.None;
            slime_on_target_max = 0;
            total_slime_applied = 0;
            total_damage_dealt = 0;
            total_attacks = 0;
            total_kills = 0;
            turns_survived = 0;
            total_moves = 0;
            cards_played = 0;
            ally_deaths = 0;
            total_bleed_on_enemies = 0;
            total_hp_paid = 0;
            total_guard_used = 0;
            total_damage_absorbed = 0;
            total_summons = 0;
            tile_boost_progress = 0;
        }
    }

    /// <summary>
    /// 玩家觉醒进度管理器
    /// </summary>
    [System.Serializable]
    public class PlayerAwakeningManager
    {
        public int player_id;
        public List<AwakeningProgress> hero_awakenings = new List<AwakeningProgress>();

        public PlayerAwakeningManager(int playerId)
        {
            this.player_id = playerId;
        }

        /// <summary>
        /// 为英雄注册觉醒进度
        /// </summary>
        public AwakeningProgress RegisterHero(Card hero, string awakeningId)
        {
            AwakeningProgress progress = new AwakeningProgress(hero.uid, awakeningId);
            hero_awakenings.Add(progress);
            return progress;
        }

        /// <summary>
        /// 获取指定英雄的觉醒进度
        /// </summary>
        public AwakeningProgress GetProgress(string cardUid)
        {
            foreach (AwakeningProgress progress in hero_awakenings)
            {
                if (progress.card_uid == cardUid)
                    return progress;
            }
            return null;
        }

        /// <summary>
        /// 获取指定英雄的觉醒进度（通过Card对象）
        /// </summary>
        public AwakeningProgress GetProgress(Card hero)
        {
            if (hero == null) return null;
            return GetProgress(hero.uid);
        }

        /// <summary>
        /// 为所有英雄增加地块加速进度
        /// </summary>
        public void AddTileBoostToAll(int amount)
        {
            foreach (AwakeningProgress progress in hero_awakenings)
            {
                progress.AddTileBoost(amount);
            }
        }

        /// <summary>
        /// 检查并执行所有英雄的觉醒
        /// </summary>
        public void CheckAllAwakenings(Game game)
        {
            foreach (AwakeningProgress progress in hero_awakenings)
            {
                AwakeningData data = AwakeningData.Get(progress.awakening_id);
                if (data == null) continue;

                // 检查初醒
                if (progress.CheckCondition(AwakeningStage.First, data))
                {
                    progress.Awaken(AwakeningStage.First);
                    // 触发觉醒效果（由GameLogic处理）
                }

                // 检查进阶
                if (progress.CheckCondition(AwakeningStage.Second, data))
                {
                    progress.Awaken(AwakeningStage.Second);
                }

                // 检查升华
                if (progress.CheckCondition(AwakeningStage.Third, data))
                {
                    progress.Awaken(AwakeningStage.Third);
                }
            }
        }
    }
}
