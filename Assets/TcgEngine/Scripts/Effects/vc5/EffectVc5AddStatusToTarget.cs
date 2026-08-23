using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/AddStatusToTarget", order = 10)]
    public class EffectVc5AddStatusToTarget : EffectData
    {
        public StatusType status_type = StatusType.None;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target != null && status_type != StatusType.None)
                target.AddStatus(status_type, ability.value, ability.duration);
        }
    }
}
