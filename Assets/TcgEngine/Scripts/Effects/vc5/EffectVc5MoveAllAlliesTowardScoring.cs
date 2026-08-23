using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MoveAllAlliesTowardScoring", order = 10)]
    public class EffectVc5MoveAllAlliesTowardScoring : EffectData
    {
        public int steps = 1;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            if (player == null)
                return;

            Card[] allies = player.cards_board.ToArray();
            foreach (Card ally in allies)
            {
                if (ally == null || !ally.CardData.IsCharacter())
                    continue;

                Slot slot = Vc5DemoGrid.FindBestStepTowardScoring(logic.GameData, ally, steps);
                if (slot.IsValid() && slot != ally.slot)
                    logic.MoveCard(ally, slot, true);
            }
        }
    }
}
