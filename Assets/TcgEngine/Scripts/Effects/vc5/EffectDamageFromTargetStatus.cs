using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/DamageFromTargetStatus", order = 10)]
    public class EffectDamageFromTargetStatus : EffectData
    {
        public StatusType status;
        public int divisor = 2;
        public bool clear_status;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int stacks = target != null ? target.GetStatusValue(status) : 0;
            int bonus = divisor > 0 ? stacks / divisor : stacks;
            int damage = ability.value + bonus;
            logic.DamageCard(caster, target, damage, true);
            if (clear_status && target != null)
                target.RemoveStatus(status);
        }
    }
}
