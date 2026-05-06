using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/DamageFromCasterAttack", order = 10)]
    public class EffectDamageFromCasterAttack : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int damage = ability.value + caster.GetAttack();
            logic.DamageCard(caster, target, damage, true);
        }
    }
}
