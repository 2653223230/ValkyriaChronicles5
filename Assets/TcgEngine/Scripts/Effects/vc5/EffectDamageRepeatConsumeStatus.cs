using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/DamageRepeatConsumeStatus", order = 10)]
    public class EffectDamageRepeatConsumeStatus : EffectData
    {
        public StatusType status;
        public int consume_per_hit = 1;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;

            int damage = Mathf.Max(ability.value, 0);
            while (target.HasStatus(status))
            {
                logic.DamageCard(caster, target, damage, true);
                target.ConsumeStatus(status, consume_per_hit);
                if (!target.HasStatus(status))
                    break;
            }
        }
    }
}
