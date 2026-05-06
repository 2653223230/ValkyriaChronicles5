using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MergeSummons", order = 10)]
    public class EffectMergeSummons : EffectData
    {
        public TraitData trait;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null || trait == null)
                return;

            if (!target.HasTrait(trait.id))
                return;

            List<Card> to_merge = new List<Card>();
            foreach (Player player in logic.GameData.players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card.uid != target.uid && card.player_id == target.player_id && card.HasTrait(trait.id))
                        to_merge.Add(card);
                }
            }

            foreach (Card card in to_merge)
            {
                target.attack += card.attack;
                target.hp += card.hp;
                target.move_Range += card.move_Range;
                target.attack_Range += card.attack_Range;
                logic.DiscardCard(card);
            }
        }
    }
}
