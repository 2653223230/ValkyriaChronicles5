using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/CanMoveTriggererAdjacent", order = 10)]
    public class ConditionVc5CanMoveTriggererAdjacent : ConditionData
    {
        public int max_move = -1;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            Card triggerer = data.GetCard(data.ability_triggerer);
            if (triggerer == null || target == null || !data.IsOnBoard(triggerer) || !data.IsOnBoard(target))
                return false;

            if (triggerer.player_id == target.player_id)
                return false;

            return Vc5DemoGrid.FindBestAdjacentSlot(data, triggerer, target, max_move, out _);
        }
    }
}
