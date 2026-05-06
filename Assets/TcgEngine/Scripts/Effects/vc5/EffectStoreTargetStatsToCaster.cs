using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/StoreTargetStatsToCaster", order = 10)]
    public class EffectStoreTargetStatsToCaster : EffectData
    {
        public TraitData attack_trait;
        public TraitData slime_flag_trait;
        public TraitData required_trait;
        public int hp_cost_from_target;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;

            logic.GameData.ability_triggerer = target.uid;
            logic.GameData.last_target = target.uid;

            if (hp_cost_from_target > 0)
                target.damage += hp_cost_from_target;

            if (attack_trait != null)
                caster.SetTrait(attack_trait.id, target.GetAttack());

            if (slime_flag_trait != null)
            {
                int val = 0;
                if (required_trait == null || target.HasTrait(required_trait.id))
                    val = 1;
                caster.SetTrait(slime_flag_trait.id, val);
            }
        }
    }
}
