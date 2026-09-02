using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/SlotMoveFromTriggerer", order = 10)]
    public class ConditionVc5SlotMoveFromTriggerer : ConditionData
    {
        public int fixed_range = 1;
        public bool use_triggerer_move_range = false;
        public bool require_empty = true;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target) { return false; }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target) { return false; }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, CardData target) { return false; }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Card triggerer = Vc5DemoGrid.GetMovingActor(data, ability, caster);
            if (triggerer == null || !data.IsOnBoard(triggerer) || !Vc5DemoGrid.IsPlayableBoardCell(target))
                return false;

            if (target.p != triggerer.slot.p)
                return false;

            if (require_empty && Vc5DemoGrid.GetDisplayedSlotCard(data, target) != null)
                return false;

            int range = use_triggerer_move_range ? triggerer.move_Range : fixed_range;
            int distance = Vc5DemoGrid.HexDistance(triggerer.slot, target);
            return distance > 0 && distance <= range;
        }
    }
}
