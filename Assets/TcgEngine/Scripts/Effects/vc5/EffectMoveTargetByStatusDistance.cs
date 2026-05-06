using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/MoveTargetByStatusDistance", order = 10)]
    public class EffectMoveTargetByStatusDistance : EffectData
    {
        public StatusType status;
        public int divisor = 3;
        public int base_distance = 1;
        public bool damage_if_awakened;
        public StatusType awakened_status = StatusType.GiantArmAwakened;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return;

            int stacks = target.GetStatusValue(status);
            int bonus = divisor > 0 ? stacks / divisor : stacks;
            int distance = Mathf.Max(0, base_distance + bonus);

            List<Slot> candidates = GetSlotsInRange(target.slot, distance);
            List<Slot> free = new List<Slot>();
            foreach (Slot slot in candidates)
            {
                if (slot.IsValid() && logic.GameData.GetSlotCard(slot) == null)
                    free.Add(slot);
            }

            if (free.Count > 0)
            {
                System.Random rand = logic.GetRandom();
                Slot destination = free[rand.Next(0, free.Count)];
                int dx = destination.x - target.slot.x;
                int dy = destination.y - target.slot.y;
                int dz = (target.slot.x + target.slot.y) - (destination.x + destination.y);
                int moved = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
                logic.MoveCard(target, destination, true);
                if (damage_if_awakened && caster.HasStatus(awakened_status) && moved > 0)
                    logic.DamageCard(caster, target, moved, true);
            }
        }

        private List<Slot> GetSlotsInRange(Slot center, int distance)
        {
            List<Slot> slots = new List<Slot>();
            int p = center.p;
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    int dz = (center.x + center.y) - (center.x + dx + center.y + dy);
                    int hexDistance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
                    if (hexDistance <= distance)
                        slots.Add(new Slot(center.x + dx, center.y + dy, p));
                }
            }
            return slots;
        }
    }
}
