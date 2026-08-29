using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// GameLogic 的觉醒系统扩展（须与 GameLogic.cs 同命名空间 partial 合并）
    /// </summary>
    public partial class GameLogic
    {
        private AwakeningSystem awakeningSystem;

        public void InitializeAwakeningSystem()
        {
            awakeningSystem = new AwakeningSystem(game_data, this);

            foreach (Player player in game_data.players)
            {
                awakeningSystem.InitializePlayer(player.player_id);
            }
        }

        public void RegisterHeroAwakening(Card hero, string awakeningId)
        {
            if (awakeningSystem != null)
                awakeningSystem.RegisterHeroAwakening(hero, awakeningId);
        }

        public AwakeningProgress GetAwakeningProgress(Card hero)
        {
            if (awakeningSystem != null)
                return awakeningSystem.GetProgress(hero);
            return null;
        }

        public void OnDamageDealtForAwakening(Card attacker, int damage)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnDamageDealt(attacker, damage);
        }

        public void OnAttackForAwakening(Card attacker)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnAttack(attacker);
        }

        public void OnKillForAwakening(Card killer)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnKill(killer);
        }

        public void OnSlimeAppliedForAwakening(Card source, int amount, int targetSlimeAmount)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnSlimeApplied(source, amount, targetSlimeAmount);
        }

        public void OnMoveForAwakening(Card mover)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnMove(mover);
        }

        public void OnCardPlayedForAwakening(Card hero)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnCardPlayed(hero);
        }

        public void OnAllyDeathForAwakening(int playerId)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnAllyDeath(playerId);
        }

        public void OnTurnEndForAwakening(int playerId)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnTurnEnd(playerId);
        }

        public void OnHPPaidForAwakening(Card hero, int amount)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnHPPaid(hero, amount);
        }

        public void OnGuardUsedForAwakening(Card hero)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnGuardUsed(hero);
        }

        public void OnDamageAbsorbedForAwakening(Card hero, int amount)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnDamageAbsorbed(hero, amount);
        }

        public void OnSummonForAwakening(Card hero)
        {
            if (awakeningSystem != null)
                awakeningSystem.OnSummon(hero);
        }

        public void AddTileBoostToAllHeroes(int playerId, int amount)
        {
            if (awakeningSystem != null)
                awakeningSystem.AddTileBoostToAllHeroes(playerId, amount);
        }

        public void TriggerAwakeningEvent(Card hero, AwakeningStage stage)
        {
            Debug.Log($"[GameLogic] Awakening event triggered: {hero.CardData.title} -> {stage}");
        }

        /// <summary>觉醒技能触发：复用引擎能力队列，避免重复实现 Resolve。</summary>
        public void TriggerAwakeningAbility(AbilityData ability, Card caster)
        {
            if (ability == null || caster == null)
                return;
            TriggerAbilityDelayed(ability, caster);
        }
    }
}
