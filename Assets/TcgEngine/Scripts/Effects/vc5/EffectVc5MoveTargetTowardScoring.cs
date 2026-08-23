using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MoveTargetTowardScoring", order = 10)]
    public class EffectVc5MoveTargetTowardScoring : EffectData
    {
        public int fixed_steps = 1;
        public bool use_move_range = false;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null || !logic.GameData.IsOnBoard(target))
                return;

            int steps = use_move_range ? target.move_Range : fixed_steps;
            Slot slot = Vc5DemoGrid.FindBestStepTowardScoring(logic.GameData, target, steps);
            if (slot.IsValid() && slot != target.slot)
                logic.MoveCard(target, slot, true);
        }
    }
}
