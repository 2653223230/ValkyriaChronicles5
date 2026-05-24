using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// GameLogic 的觉醒系统扩展
    /// 提供觉醒相关的便捷方法
    /// </summary>
    public partial class GameLogic
    {
        // 觉醒系统实例
        private AwakeningSystem awakeningSystem;

        /// <summary>
        /// 初始化觉醒系统
        /// </summary>
        public void InitializeAwakeningSystem()
        {
            awakeningSystem = new AwakeningSystem(game, this);

            // 为所有玩家初始化
            foreach (Player player in game.players)
            {
                awakeningSystem.InitializePlayer(player.player_id);
            }
        }

        /// <summary>
        /// 为英雄注册觉醒
        /// </summary>
        public void RegisterHeroAwakening(Card hero, string awakeningId)
        {
            if (awakeningSystem != null)
            {
                awakeningSystem.RegisterHeroAwakening(hero, awakeningId);
            }
        }

        /// <summary>
        /// 获取英雄的觉醒进度
        /// </summary>
        public AwakeningProgress GetAwakeningProgress(Card hero)
        {
            if (awakeningSystem != null)
                return awakeningSystem.GetProgress(hero);
            return null;
        }

        // ========== 事件转发方法 ==========

        /// <summary>
        /// 当造成伤害时（用于觉醒进度）
        /// </summary>
        public void OnDamageDealtForAwakening(Card attacker, int damage)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnDamageDealt(attacker, damage);
        }

        /// <summary>
        /// 当攻击时（用于觉醒进度）
        /// </summary>
        public void OnAttackForAwakening(Card attacker)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnAttack(attacker);
        }

        /// <summary>
        /// 当击杀时（用于觉醒进度）
        /// </summary>
        public void OnKillForAwakening(Card killer)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnKill(killer);
        }

        /// <summary>
        /// 当施加粘液时（用于觉醒进度）
        /// </summary>
        public void OnSlimeAppliedForAwakening(Card source, int amount, int targetSlimeAmount)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnSlimeApplied(source, amount, targetSlimeAmount);
        }

        /// <summary>
        /// 当移动时（用于觉醒进度）
        /// </summary>
        public void OnMoveForAwakening(Card mover)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnMove(mover);
        }

        /// <summary>
        /// 当使用卡牌时（用于觉醒进度）
        /// </summary>
        public void OnCardPlayedForAwakening(Card hero)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnCardPlayed(hero);
        }

        /// <summary>
        /// 当友方死亡时（用于觉醒进度）
        /// </summary>
        public void OnAllyDeathForAwakening(int playerId)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnAllyDeath(playerId);
        }

        /// <summary>
        /// 当回合结束时（用于觉醒进度 - 存活回合数）
        /// </summary>
        public void OnTurnEndForAwakening(int playerId)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnTurnEnd(playerId);
        }

        /// <summary>
        /// 当支付生命值时（用于觉醒进度）
        /// </summary>
        public void OnHPPaidForAwakening(Card hero, int amount)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnHPPaid(hero, amount);
        }

        /// <summary>
        /// 当使用守护时（用于觉醒进度）
        /// </summary>
        public void OnGuardUsedForAwakening(Card hero)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnGuardUsed(hero);
        }

        /// <summary>
        /// 当吸收伤害时（用于觉醒进度）
        /// </summary>
        public void OnDamageAbsorbedForAwakening(Card hero, int amount)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnDamageAbsorbed(hero, amount);
        }

        /// <summary>
        /// 当召唤时（用于觉醒进度）
        /// </summary>
        public void OnSummonForAwakening(Card hero)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnSummon(hero);
        }

        /// <summary>
        /// 为所有英雄增加地块加速进度
        /// </summary>
        public void AddTileBoostToAllHeroes(int playerId, int amount)
        {
            if (awakeningSystem != null)
                awakeningSystem.AddTileBoostToAllHeroes(playerId, amount);
        }

        /// <summary>
        /// 触发觉醒事件（用于UI/特效等）
        /// </summary>
        public void TriggerAwakeningEvent(Card hero, AwakeningStage stage)
        {
            // 触发游戏事件，可以被UI监听
            // 这里可以添加事件触发逻辑
            Debug.Log($"[GameLogic] Awakening event triggered: {hero.CardData.title} -> {stage}");
        }

        /// <summary>
        /// 触发能力（辅助方法）
        /// </summary>
        public void TriggerAbility(AbilityData ability, Card caster)
        {
            if (ability == null || caster == null) return;

            // 这里可以复用现有的能力触发逻辑
            // 简化版本：直接执行能力效果
            ResolveAbility(ability, caster);
        }

        /// <summary>
        /// 解析并执行能力效果
        /// </summary>
        private void ResolveAbility(AbilityData ability, Card caster)
        {
            // 这里应该调用现有的能力解析逻辑
            // 简化实现，实际应该复用 GameLogic 的现有方法
            if (ability.effects != null)
            {
                foreach (EffectData effect in ability.effects)
                {
                    if (effect != null)
                    {
                        effect.DoEffect(game, this, ability, caster, caster, caster.slot);
                    }
                }
            }
        }
    }
}
