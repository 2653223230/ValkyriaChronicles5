using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/ApplyStatusFromCasterHP", order = 10)]
    public class EffectApplyStatusFromCasterHP : EffectData
    {
        public StatusType status = StatusType.Slime;
        public int hp_divisor = 2;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (caster == null || target == null || hp_divisor <= 0)
                return;
            int stacks = caster.GetHP() / hp_divisor;
            if (stacks > 0)
                target.AddStatus(status, stacks, 0);
        }
    }
}
