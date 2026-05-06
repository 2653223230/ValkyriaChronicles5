using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/SummonAdjacentToTarget", order = 10)]
    public class EffectSummonAdjacentToTarget : EffectData
    {
        public CardData summon;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null || summon == null)
                return;

            Player player = logic.GameData.GetPlayer(caster.player_id);
            List<Slot> slots = GetAdjacentSlots(target.slot);
            foreach (Slot slot in slots)
            {
                if (slot.IsValid() && logic.GameData.GetSlotCard(slot) == null)
                {
                    logic.SummonCard(player, summon, caster.VariantData, slot);
                    break;
                }
            }
        }

        private List<Slot> GetAdjacentSlots(Slot center)
        {
            List<Slot> slots = new List<Slot>();
            int p = center.p;
            int x = center.x;
            int y = center.y;
            slots.Add(new Slot(x + 1, y, p));
            slots.Add(new Slot(x - 1, y, p));
            slots.Add(new Slot(x, y + 1, p));
            slots.Add(new Slot(x, y - 1, p));
            slots.Add(new Slot(x + 1, y - 1, p));
            slots.Add(new Slot(x - 1, y + 1, p));
            return slots;
        }
    }
}
