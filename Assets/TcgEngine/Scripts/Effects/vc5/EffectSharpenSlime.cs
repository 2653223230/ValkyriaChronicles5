using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/SharpenSlime", order = 10)]
    public class EffectSharpenSlime : EffectData
    {
        public StatusType buff_status = StatusType.AddAttack;
        public int base_value = 2;
        public int bonus_value = 2;
        public int duration = 1;
        public StatusType consume_status = StatusType.Slime;
        public int consume_count = 2;
        public TraitData required_trait;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;

            target.AddStatus(buff_status, base_value, duration);
            if (required_trait == null || !target.HasTrait(required_trait.id))
                return;

            int consumed = ConsumeRandomStatus(logic, consume_status, consume_count);
            if (consumed > 0)
                target.AddStatus(buff_status, bonus_value, duration);
        }

        private int ConsumeRandomStatus(GameLogic logic, StatusType status, int amount)
        {
            if (logic == null || amount <= 0)
                return 0;

            List<Card> candidates = new List<Card>();
            foreach (Player player in logic.GameData.players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card.GetStatusValue(status) > 0)
                        candidates.Add(card);
                }
            }

            int consumed = 0;
            System.Random rand = logic.GetRandom();
            while (consumed < amount && candidates.Count > 0)
            {
                int idx = rand.Next(0, candidates.Count);
                Card card = candidates[idx];
                int val = card.ConsumeStatus(status, 1);
                if (val > 0)
                    consumed += val;
                if (card.GetStatusValue(status) <= 0)
                    candidates.RemoveAt(idx);
            }
            return consumed;
        }
    }
}
