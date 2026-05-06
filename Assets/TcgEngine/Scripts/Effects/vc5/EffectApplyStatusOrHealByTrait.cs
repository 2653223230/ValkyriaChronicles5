using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/ApplyStatusOrHealByTrait", order = 10)]
    public class EffectApplyStatusOrHealByTrait : EffectData
    {
        public StatusType status = StatusType.Slime;
        public int status_value = 2;
        public int status_duration = 0;
        public int heal_value = 3;
        public TraitData required_trait;
        public bool require_ally_target = true;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;

            bool isAlly = caster != null && target.player_id == caster.player_id;
            bool hasTrait = required_trait != null && target.HasTrait(required_trait.id);
            bool canHeal = (!require_ally_target || isAlly) && hasTrait;
            if (canHeal)
            {
                logic.HealCard(target, heal_value);
            }
            else
            {
                target.AddStatus(status, status_value, status_duration);
            }
        }
    }
}
