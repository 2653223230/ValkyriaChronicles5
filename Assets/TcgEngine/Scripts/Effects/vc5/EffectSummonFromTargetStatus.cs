using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/SummonFromTargetStatus", order = 10)]
    public class EffectSummonFromTargetStatus : EffectData
    {
        public CardData summon;
        public StatusType status;
        public int divisor = 2;
        public int min_count = 1;
        public bool consume_all = true;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null || summon == null)
                return;

            int stacks = target.GetStatusValue(status);
            int count = divisor > 0 ? stacks / divisor : stacks;
            count = Mathf.Max(count, min_count);

            if (consume_all && stacks > 0)
                target.RemoveStatus(status);

            List<Slot> slots = GetAdjacentSlots(target.slot);
            Player player = logic.GameData.GetPlayer(caster.player_id);
            foreach (Slot slot in slots)
            {
                if (count <= 0)
                    break;
                if (slot.IsValid() && logic.GameData.GetSlotCard(slot) == null)
                {
                    logic.SummonCard(player, summon, caster.VariantData, slot);
                    count--;
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
