using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/TargetDamaged", order = 10)]
    public class ConditionVc5TargetDamaged : ConditionData
    {
        public bool below_half = false;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return false;

            if (below_half)
                return target.GetHP() * 2 < target.GetHPMax();

            return target.damage > 0 || target.GetHP() < target.GetHPMax();
        }
    }
}
