using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/ApplyStatusPairOrSharpByTrait", order = 10)]
    public class EffectApplyStatusPairOrSharpByTrait : EffectData
    {
        public StatusType status_a = StatusType.Slime;
        public int value_a = 1;
        public int duration_a = 0;
        public StatusType status_b = StatusType.Rooted;
        public int value_b = 1;
        public int duration_b = 1;
        public StatusType ally_trait_status = StatusType.Sharp;
        public int ally_trait_value = 1;
        public int ally_trait_duration = 2;
        public TraitData required_trait;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;

            bool isAlly = caster != null && target.player_id == caster.player_id;
            bool hasTrait = required_trait != null && target.HasTrait(required_trait.id);
            if (isAlly && hasTrait)
            {
                target.AddStatus(ally_trait_status, ally_trait_value, ally_trait_duration);
                return;
            }

            target.AddStatus(status_a, value_a, duration_a);
            target.AddStatus(status_b, value_b, duration_b);
        }
    }
}
