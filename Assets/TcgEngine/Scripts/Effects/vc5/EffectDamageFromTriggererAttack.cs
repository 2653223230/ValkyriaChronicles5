using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/DamageFromTriggererAttack", order = 10)]
    public class EffectDamageFromTriggererAttack : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Card triggerer = logic.GameData.GetCard(logic.GameData.ability_triggerer);
            int bonus = triggerer != null ? triggerer.GetAttack() : 0;
            int damage = ability.value + bonus;
            logic.DamageCard(caster, target, damage, true);
        }
    }
}
