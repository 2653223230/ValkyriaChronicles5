using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/CasterTrait", order = 10)]
    public class ConditionCasterTrait : ConditionData
    {
        public TraitData trait;
        public ConditionOperatorInt oper;
        public int value = 1;

        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            if (trait == null)
                return false;
            return CompareInt(caster.GetTraitValue(trait.id), oper, value);
        }
    }
}
