using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/SlotCardTrait", order = 10)]
    public class ConditionSlotCardTrait : ConditionData
    {
        public TraitData trait;
        public ConditionOperatorBool oper;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Card card = data.GetSlotCard(target);
            bool has_trait = card != null && trait != null && card.HasTrait(trait.id);
            return CompareBool(has_trait, oper);
        }
    }
}
