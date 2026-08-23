using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/SlotMoveFromTriggerer", order = 10)]
    public class ConditionVc5SlotMoveFromTriggerer : ConditionData
    {
        public int fixed_range = 1;
        public bool use_triggerer_move_range = false;
        public bool require_empty = true;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Card triggerer = data.GetCard(data.ability_triggerer);
            if (triggerer == null || !data.IsOnBoard(triggerer) || !target.IsValid())
                return false;

            if (target.p != triggerer.slot.p)
                return false;

            if (require_empty && data.GetSlotCard(target) != null)
                return false;

            int range = use_triggerer_move_range ? triggerer.move_Range : fixed_range;
            return Vc5DemoGrid.HexDistance(triggerer.slot, target) <= range;
        }
    }
}
