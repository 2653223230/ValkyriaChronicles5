using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/SlotDistFromTriggerer", order = 10)]
    public class ConditionSlotDistFromTriggerer : ConditionData
    {
        public int range_offset = 0;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return IsTargetConditionMet(data, ability, caster, target.slot);
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Card triggerer = data.GetCard(data.ability_triggerer);
            if (triggerer == null)
                return false;
            int maxRange = triggerer.attack_Range + triggerer.GetStatusValue(StatusType.Vc5AttackRangeBonus) + range_offset;

            return Vc5DemoGrid.HexDistance(triggerer.slot, target) <= maxRange;
        }
    }
}
