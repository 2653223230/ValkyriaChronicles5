using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/AnyEnemyHasStatus", order = 10)]
    public class ConditionAnyEnemyHasStatus : ConditionData
    {
        public StatusType status;
        public int value = 1;

        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            foreach (Player player in data.players)
            {
                if (player.player_id == caster.player_id)
                    continue;
                foreach (Card card in player.cards_board)
                {
                    if (card.HasStatus(status) && card.GetStatusValue(status) >= value)
                        return true;
                }
            }
            return false;
        }
    }
}
