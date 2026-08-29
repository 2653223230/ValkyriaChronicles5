using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 觉醒系统管理器
    /// 负责觉醒进度的追踪、判定和效果触发
    /// </summary>
    public class AwakeningSystem
    {
        private Game game;
        private GameLogic logic;

        // 玩家觉醒管理器字典
        private Dictionary<int, PlayerAwakeningManager> playerAwakeningManagers = new Dictionary<int, PlayerAwakeningManager>();

        public AwakeningSystem(Game game, GameLogic logic)
        {
            this.game = game;
            this.logic = logic;
        }

        /// <summary>
        /// 初始化玩家的觉醒管理器
        /// </summary>
        public void InitializePlayer(int playerId)
        {
            if (!playerAwakeningManagers.ContainsKey(playerId))
            {
                playerAwakeningManagers[playerId] = new PlayerAwakeningManager(playerId);
            }
        }

        /// <summary>
        /// 为英雄注册觉醒
        /// </summary>
        public void RegisterHeroAwakening(Card hero, string awakeningId)
        {
            if (hero == null || string.IsNullOrEmpty(awakeningId)) return;

            hero.awakening_id = awakeningId;
            hero.awakening_stage = AwakeningStage.None;
            hero.awakening_progress = 0;

            PlayerAwakeningManager manager = GetManager(hero.player_id);
            if (manager != null)
            {
                manager.RegisterHero(hero, awakeningId);
            }
        }

        /// <summary>
        /// 获取玩家的觉醒管理器
        /// </summary>
        private PlayerAwakeningManager GetManager(int playerId)
        {
            if (playerAwakeningManagers.ContainsKey(playerId))
                return playerAwakeningManagers[playerId];
            return null;
        }

        /// <summary>
        /// 获取指定英雄的觉醒进度
        /// </summary>
        public AwakeningProgress GetProgress(Card hero)
        {
            if (hero == null) return null;
            PlayerAwakeningManager manager = GetManager(hero.player_id);
            if (manager != null)
                return manager.GetProgress(hero);
            return null;
        }

        // ========== 各种事件监听，更新觉醒进度 ==========

        /// <summary>
        /// 当造成伤害时调用
        /// </summary>
        public void OnDamageDealt(Card attacker, int damage)
        {
            if (attacker == null || damage <= 0) return;

            AwakeningProgress progress = GetProgress(attacker);
            if (progress != null)
            {
                progress.OnDamageDealt(damage);
                CheckAndTriggerAwakening(attacker, progress);
            }
        }

        /// <summary>
        /// 当攻击时调用
        /// </summary>
        public void OnAttack(Card attacker)
        {
            if (attacker == null) return;

            AwakeningProgress progress = GetProgress(attacker);
            if (progress != null)
            {
                progress.OnAttack();
                CheckAndTriggerAwakening(attacker, progress);
            }
        }

        /// <summary>
        /// 当击杀敌人时调用
        /// </summary>
        public void OnKill(Card killer)
        {
            if (killer == null) return;

            AwakeningProgress progress = GetProgress(killer);
            if (progress != null)
            {
                progress.OnKill();
                CheckAndTriggerAwakening(killer, progress);
            }
        }

        /// <summary>
        /// 当施加粘液时调用
        /// </summary>
        public void OnSlimeApplied(Card source, int amount, int targetSlimeAmount)
        {
            if (source == null || amount <= 0) return;

            AwakeningProgress progress = GetProgress(source);
            if (progress != null)
            {
                progress.OnSlimeApplied(amount);
                progress.OnSlimeOnTarget(targetSlimeAmount);
                CheckAndTriggerAwakening(source, progress);
            }
        }

        /// <summary>
        /// 当移动时调用
        /// </summary>
        public void OnMove(Card mover)
        {
            if (mover == null) return;

            AwakeningProgress progress = GetProgress(mover);
            if (progress != null)
            {
                progress.OnMove();
                CheckAndTriggerAwakening(mover, progress);
            }
        }

        /// <summary>
        /// 当使用卡牌时调用
        /// </summary>
        public void OnCardPlayed(Card hero)
        {
            if (hero == null) return;

            AwakeningProgress progress = GetProgress(hero);
            if (progress != null)
            {
                progress.OnCardPlayed();
                CheckAndTriggerAwakening(hero, progress);
            }
        }

        /// <summary>
        /// 当友方死亡时调用（需要遍历所有英雄）
        /// </summary>
        public void OnAllyDeath(int playerId)
        {
            Player player = game.GetPlayer(playerId);
            if (player == null) return;

            foreach (Card hero in player.cards_board)
            {
                if (hero.CardData.IsHero())
                {
                    AwakeningProgress progress = GetProgress(hero);
                    if (progress != null)
                    {
                        progress.OnAllyDeath();
                        CheckAndTriggerAwakening(hero, progress);
                    }
                }
            }
        }

        /// <summary>
        /// 当回合结束时调用（存活回合数）
        /// </summary>
        public void OnTurnEnd(int playerId)
        {
            Player player = game.GetPlayer(playerId);
            if (player == null) return;

            foreach (Card hero in player.cards_board)
            {
                if (hero.CardData.IsHero())
                {
                    AwakeningProgress progress = GetProgress(hero);
                    if (progress != null)
                    {
                        progress.OnTurnSurvived();
                        CheckAndTriggerAwakening(hero, progress);
                    }
                }
            }
        }

        /// <summary>
        /// 当支付生命值时调用
        /// </summary>
        public void OnHPPaid(Card hero, int amount)
        {
            if (hero == null || amount <= 0) return;

            AwakeningProgress progress = GetProgress(hero);
            if (progress != null)
            {
                progress.OnHPPaid(amount);
                CheckAndTriggerAwakening(hero, progress);
            }
        }

        /// <summary>
        /// 当使用守护时调用
        /// </summary>
        public void OnGuardUsed(Card hero)
        {
            if (hero == null) return;

            AwakeningProgress progress = GetProgress(hero);
            if (progress != null)
            {
                progress.OnGuardUsed();
                CheckAndTriggerAwakening(hero, progress);
            }
        }

        /// <summary>
        /// 当吸收伤害时调用（守护战士）
        /// </summary>
        public void OnDamageAbsorbed(Card hero, int amount)
        {
            if (hero == null || amount <= 0) return;

            AwakeningProgress progress = GetProgress(hero);
            if (progress != null)
            {
                progress.OnDamageAbsorbed(amount);
                CheckAndTriggerAwakening(hero, progress);
            }
        }

        /// <summary>
        /// 当召唤单位时调用
        /// </summary>
        public void OnSummon(Card hero)
        {
            if (hero == null) return;

            AwakeningProgress progress = GetProgress(hero);
            if (progress != null)
            {
                progress.OnSummon();
                CheckAndTriggerAwakening(hero, progress);
            }
        }

        /// <summary>
        /// 为所有英雄增加地块加速进度
        /// </summary>
        public void AddTileBoostToAllHeroes(int playerId, int amount)
        {
            Player player = game.GetPlayer(playerId);
            if (player == null) return;

            foreach (Card hero in player.cards_board)
            {
                if (hero.CardData.IsHero())
                {
                    AwakeningProgress progress = GetProgress(hero);
                    if (progress != null)
                    {
                        progress.AddTileBoost(amount);
                        CheckAndTriggerAwakening(hero, progress);
                    }
                }
            }
        }

        // ========== 觉醒判定和触发 ==========

        /// <summary>
        /// 检查并触发觉醒
        /// </summary>
        private void CheckAndTriggerAwakening(Card hero, AwakeningProgress progress)
        {
            if (hero == null || progress == null) return;

            AwakeningData data = AwakeningData.Get(progress.awakening_id);
            if (data == null) return;

            AwakeningStage oldStage = hero.awakening_stage;

            // 检查初醒
            if (progress.CheckCondition(AwakeningStage.First, data) && oldStage < AwakeningStage.First)
            {
                TriggerAwakening(hero, progress, AwakeningStage.First, data);
            }

            // 检查进阶
            if (progress.CheckCondition(AwakeningStage.Second, data) && hero.awakening_stage < AwakeningStage.Second)
            {
                TriggerAwakening(hero, progress, AwakeningStage.Second, data);
            }

            // 检查升华
            if (progress.CheckCondition(AwakeningStage.Third, data) && hero.awakening_stage < AwakeningStage.Third)
            {
                TriggerAwakening(hero, progress, AwakeningStage.Third, data);
            }
        }

        /// <summary>
        /// 触发觉醒效果
        /// </summary>
        private void TriggerAwakening(Card hero, AwakeningProgress progress, AwakeningStage stage, AwakeningData data)
        {
            // 更新进度数据
            progress.Awaken(stage);

            // 更新卡牌数据
            hero.awakening_stage = stage;
            hero.awakening_progress = progress.GetProgress(stage, data);

            // 应用当回合加成
            ApplyTurnBonus(hero, stage, data);

            // 应用永续加成
            ApplyPermanentBonus(hero, stage, data);

            // 触发技能（如果有）
            AbilityData ability = data.GetAbility(stage);
            if (ability != null)
            {
                TriggerAwakeningAbility(hero, ability);
            }

            // 触发事件
            logic.TriggerAwakeningEvent(hero, stage);

            Debug.Log($"[Awakening] Hero {hero.CardData.title} has awakened to stage {stage}!");
        }

        /// <summary>
        /// 应用当回合加成
        /// </summary>
        private void ApplyTurnBonus(Card hero, AwakeningStage stage, AwakeningData data)
        {
            TurnBonusType bonusType = data.GetTurnBonusType(stage);
            int bonusValue = 0;

            switch (stage)
            {
                case AwakeningStage.First:
                    bonusValue = data.first_turn_bonus_value;
                    break;
                case AwakeningStage.Second:
                    bonusValue = data.second_turn_bonus_value;
                    break;
                case AwakeningStage.Third:
                    bonusValue = data.third_turn_bonus_value;
                    break;
            }

            switch (bonusType)
            {
                case TurnBonusType.DamageAllWithSlime:
                    // 对所有带粘液敌人造成伤害
                    ApplyDamageToSlimeTargets(hero, bonusValue);
                    break;

                case TurnBonusType.ApplySlimeToAll:
                    // 对所有敌人施加粘液
                    ApplySlimeToAllEnemies(hero, bonusValue);
                    break;

                case TurnBonusType.MoveAndDamage:
                    // 移动并造成伤害
                    // 由具体英雄逻辑处理
                    break;

                case TurnBonusType.SummonSpawns:
                    // 召唤幼崽
                    SummonSpawns(hero, bonusValue);
                    break;

                case TurnBonusType.HealAllAllies:
                    // 治疗所有友军
                    HealAllAllies(hero.player_id, bonusValue);
                    break;

                case TurnBonusType.BuffAllAllies:
                    // 强化所有友军
                    BuffAllAllies(hero.player_id, bonusValue);
                    break;

                case TurnBonusType.DamageAOE:
                    // AOE伤害
                    ApplyAOEDamage(hero, bonusValue);
                    break;

                case TurnBonusType.SpecialAbility:
                    // 特殊能力，由具体英雄处理
                    break;
            }
        }

        /// <summary>
        /// 应用永续加成
        /// </summary>
        private void ApplyPermanentBonus(Card hero, AwakeningStage stage, AwakeningData data)
        {
            PermanentBonusType bonusType = data.GetPermanentBonusType(stage);
            int bonusValue = 0;

            switch (stage)
            {
                case AwakeningStage.First:
                    bonusValue = data.first_permanent_bonus_value;
                    break;
                case AwakeningStage.Second:
                    bonusValue = data.second_permanent_bonus_value;
                    break;
                case AwakeningStage.Third:
                    bonusValue = data.third_permanent_bonus_value;
                    break;
            }

            switch (bonusType)
            {
                case PermanentBonusType.AttackPlus:
                    hero.attack += bonusValue;
                    break;

                case PermanentBonusType.AttackRangePlus:
                    hero.attack_Range += bonusValue;
                    break;

                case PermanentBonusType.HPPlus:
                    hero.hp += bonusValue;
                    break;

                case PermanentBonusType.MovePlus:
                    hero.move_Range += bonusValue;
                    break;

                case PermanentBonusType.AddSlimeOnAttack:
                case PermanentBonusType.DamageBonusVsSlime:
                case PermanentBonusType.SlownessDebuff:
                case PermanentBonusType.WeakenDebuff:
                case PermanentBonusType.DamageOnMove:
                case PermanentBonusType.MoveWithoutSlime:
                case PermanentBonusType.AutoSlimePerTurn:
                case PermanentBonusType.SpawnResurrection:
                case PermanentBonusType.DamageOnMoveEnd:
                case PermanentBonusType.BleedOnAttack:
                case PermanentBonusType.RemoveAttackRestriction:
                case PermanentBonusType.AutoGuardPerTurn:
                case PermanentBonusType.SharpOnAttack:
                    // 这些效果通过添加 Trait 或 Status 实现
                    AddAwakeningTrait(hero, bonusType, bonusValue);
                    break;
            }
        }

        /// <summary>
        /// 触发觉醒技能
        /// </summary>
        private void TriggerAwakeningAbility(Card hero, AbilityData ability)
        {
            if (ability == null || logic == null) return;

            // 使用 GameLogic 的能力触发机制
            logic.TriggerAwakeningAbility(ability, hero);
        }

        // ========== 辅助方法 ==========

        private void ApplyDamageToSlimeTargets(Card hero, int damage)
        {
            // 实现对所有带粘液敌人造成伤害
            // 需要遍历场上所有敌方单位，检查是否有粘液状态
        }

        private void ApplySlimeToAllEnemies(Card hero, int layers)
        {
            // 对所有敌方单位施加粘液
        }

        private void SummonSpawns(Card hero, int count)
        {
            // 召唤幼崽
        }

        private void HealAllAllies(int playerId, int amount)
        {
            Player player = game.GetPlayer(playerId);
            if (player == null) return;

            foreach (Card card in player.cards_board)
            {
                // 治疗逻辑
            }
        }

        private void BuffAllAllies(int playerId, int amount)
        {
            // 强化所有友军
        }

        private void ApplyAOEDamage(Card hero, int damage)
        {
            // AOE伤害
        }

        private void AddAwakeningTrait(Card hero, PermanentBonusType type, int value)
        {
            // 添加觉醒特质，用于标记永续效果
            string traitId = "awakening_" + type.ToString().ToLower();
            hero.SetTrait(traitId, value);
        }
    }
}
