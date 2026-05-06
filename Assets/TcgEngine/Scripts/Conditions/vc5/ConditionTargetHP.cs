using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/TargetHP", order = 10)]
    public class ConditionTargetHP : ConditionData
    {
        public int value = 1;
        public ConditionOperatorInt oper = ConditionOperatorInt.GreaterEqual;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return false;
            return CompareInt(target.GetHP(), oper, value);
        }
    }
}
