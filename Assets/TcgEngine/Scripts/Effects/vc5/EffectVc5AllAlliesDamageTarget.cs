using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/AllAlliesDamageTarget", order = 10)]
    public class EffectVc5AllAlliesDamageTarget : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            if (player == null || target == null)
                return;

            Card[] allies = player.cards_board.ToArray();
            foreach (Card ally in allies)
            {
                if (ally == null || !ally.CardData.IsCharacter() || !Vc5DemoGrid.InAttackRange(ally, target))
                    continue;

                logic.DamageCard(ally, target, ally.GetAttack(), false);
                if (!logic.GameData.IsOnBoard(target))
                    break;
            }
        }
    }
}
