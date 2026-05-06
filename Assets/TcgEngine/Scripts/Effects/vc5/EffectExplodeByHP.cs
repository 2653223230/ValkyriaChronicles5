using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/ExplodeByHP", order = 10)]
    public class EffectExplodeByHP : EffectData
    {
        public int range = 1;
        public TraitData explode_trait;
        public bool explode_all_with_trait;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;

            List<Card> explode_cards = new List<Card>();
            if (explode_all_with_trait && explode_trait != null && target.HasTrait(explode_trait.id))
            {
                foreach (Player player in logic.GameData.players)
                {
                    foreach (Card card in player.cards_board)
                    {
                        if (card.HasTrait(explode_trait.id))
                            explode_cards.Add(card);
                    }
                }
            }
            else
            {
                explode_cards.Add(target);
            }

            foreach (Card boom in explode_cards)
            {
                int damage = boom.GetHP();
                logic.GameData.selected_value = damage;
                List<Card> affected = GetCardsInRange(logic.GameData, boom.slot, range);
                foreach (Card victim in affected)
                {
                    if (victim.uid == boom.uid)
                        continue;
                    logic.DamageCard(caster, victim, damage, true);
                }
                logic.KillCard(caster, boom);
            }
        }

        private List<Card> GetCardsInRange(Game data, Slot center, int distance)
        {
            List<Card> results = new List<Card>();
            foreach (Player player in data.players)
            {
                foreach (Card card in player.cards_board)
                {
                    int dx = card.slot.x - center.x;
                    int dy = card.slot.y - center.y;
                    int dz = (center.x + center.y) - (card.slot.x + card.slot.y);
                    int hexDistance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
                    if (hexDistance <= distance)
                        results.Add(card);
                }
            }
            return results;
        }
    }
}
