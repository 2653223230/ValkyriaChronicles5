using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/ApplyStatusPair", order = 10)]
    public class EffectApplyStatusPair : EffectData
    {
        public StatusType status_a;
        public int value_a = 1;
        public int duration_a = 0;
        public StatusType status_b;
        public int value_b = 1;
        public int duration_b = 0;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;
            target.AddStatus(status_a, value_a, duration_a);
            target.AddStatus(status_b, value_b, duration_b);
        }
    }
}
