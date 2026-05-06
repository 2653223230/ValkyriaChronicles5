using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MoveTriggererToTarget", order = 10)]
    public class EffectMoveTriggererToTarget : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            MoveTo(logic, target);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;
            MoveTo(logic, target.slot);
        }

        private void MoveTo(GameLogic logic, Slot target)
        {
            Card triggerer = logic.GameData.GetCard(logic.GameData.ability_triggerer);
            if (triggerer == null)
                return;
            logic.MoveCard(triggerer, target, true);
        }
    }
}
