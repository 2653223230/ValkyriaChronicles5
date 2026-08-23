using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/DamageFromTriggererAttack", order = 10)]
    public class EffectVc5DamageFromTriggererAttack : EffectData
    {
        public bool require_range = true;
        public bool add_support_bonus = false;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Card attacker = logic.GameData.GetCard(logic.GameData.ability_triggerer);
            if (attacker == null || target == null)
                return;

            if (require_range && !Vc5DemoGrid.InAttackRange(attacker, target))
                return;

            int damage = attacker.GetAttack() + ability.value;
            if (add_support_bonus && HasSupportingShooter(logic.GameData, attacker, target))
                damage += 1;

            logic.DamageCard(attacker, target, damage, false);
        }

        private static bool HasSupportingShooter(Game data, Card attacker, Card target)
        {
            Player player = data.GetPlayer(attacker.player_id);
            if (player == null)
                return false;

            foreach (Card ally in player.cards_board)
            {
                if (ally != attacker && ally.CardData.IsCharacter() && Vc5DemoGrid.InAttackRange(ally, target))
                    return true;
            }
            return false;
        }
    }
}
