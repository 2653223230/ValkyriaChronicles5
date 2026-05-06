using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/TriggererTrait", order = 10)]
    public class ConditionTriggererTrait : ConditionData
    {
        public TraitData trait;
        public ConditionOperatorInt oper;
        public int value = 1;

        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            Card triggerer = data.GetCard(data.ability_triggerer);
            if (triggerer == null || trait == null)
                return false;
            return CompareInt(triggerer.GetTraitValue(trait.id), oper, value);
        }
    }
}
