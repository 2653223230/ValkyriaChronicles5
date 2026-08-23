using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MoveTargetAwayFromEnemies", order = 10)]
    public class EffectVc5MoveTargetAwayFromEnemies : EffectData
    {
        public int steps = 1;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null || !logic.GameData.IsOnBoard(target))
                return;

            Slot slot = Vc5DemoGrid.FindBestStepAwayFromEnemies(logic.GameData, target, steps);
            if (slot.IsValid() && slot != target.slot)
                logic.MoveCard(target, slot, true);
        }
    }
}
