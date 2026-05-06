using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/SlotRangeFromAlly", order = 10)]
    public class ConditionSlotRangeFromAlly : ConditionData
    {
        public int range = 2;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Player player = data.GetPlayer(caster.player_id);
            if (player == null)
                return false;

            foreach (Card card in player.cards_board)
            {
                int dx = target.x - card.slot.x;
                int dy = target.y - card.slot.y;
                int dz = (card.slot.x + card.slot.y) - (target.x + target.y);
                int hexDistance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
                if (hexDistance <= range)
                    return true;
            }
            return false;
        }
    }
}
