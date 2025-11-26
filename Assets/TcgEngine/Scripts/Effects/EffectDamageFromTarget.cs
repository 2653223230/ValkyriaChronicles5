using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that damages a card or a player based on LastTargeted card's attack value
    /// 基于最后选中目标卡牌的攻击力对卡牌或玩家造成伤害的效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/DamageFromTarget", order = 10)]
    public class EffectDamageFromTarget : EffectData
    {
        public TraitData bonus_damage;
        public int bonus_value = 0; // Additional damage to add (e.g., +1)

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            int damage = GetDamage(logic.GameData, caster, ability.value);
            logic.DamagePlayer(caster, target, damage);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int damage = GetDamage(logic.GameData, caster, ability.value);
            logic.DamageCard(caster, target, damage, true);
        }

        private int GetDamage(Game data, Card caster, int value)
        {
            // Get the LastTargeted card (the friendly minion selected in the first step)
            Card source_card = data.GetCard(data.last_target);
            
            int damage = value; // Base value from ability
            
            if (source_card != null)
            {
                // Use the attack value of the selected friendly minion
                damage += source_card.GetAttack();
            }
            
            // Add bonus damage (e.g., +1)
            damage += bonus_value;
            
            // Add trait bonuses
            Player player = data.GetPlayer(caster.player_id);
            damage += caster.GetTraitValue(bonus_damage) + player.GetTraitValue(bonus_damage);
            
            return damage;
        }

    }
}


