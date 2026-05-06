using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MoveTriggererToSlot", order = 10)]
    public class EffectMoveTriggererToSlot : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            Card triggerer = logic.GameData.GetCard(logic.GameData.ability_triggerer);
            if (triggerer == null)
                return;
            logic.MoveCard(triggerer, target, true);
        }
    }
}
