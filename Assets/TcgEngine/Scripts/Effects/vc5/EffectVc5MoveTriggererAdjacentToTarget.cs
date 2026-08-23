using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MoveTriggererAdjacentToTarget", order = 10)]
    public class EffectVc5MoveTriggererAdjacentToTarget : EffectData
    {
        public int max_move = -1;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Card triggerer = logic.GameData.GetCard(logic.GameData.ability_triggerer);
            if (triggerer == null || target == null)
                return;

            if (!Vc5DemoGrid.FindBestAdjacentSlot(logic.GameData, triggerer, target, max_move, out Slot slot))
                return;

            logic.MoveCard(triggerer, slot, true, max_move < 0);
        }
    }
}
