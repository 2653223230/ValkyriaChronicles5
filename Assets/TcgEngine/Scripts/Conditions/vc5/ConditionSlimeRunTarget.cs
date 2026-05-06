using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/SlimeRunTarget", order = 10)]
    public class ConditionSlimeRunTarget : ConditionData
    {
        public int base_range = 2;
        public TraitData triggerer_required_trait;
        public TraitData spawn_trait;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            if (data.GetSlotCard(target) != null)
                return false;
            return IsInBaseRange(data, target);
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            Card triggerer = data.GetCard(data.ability_triggerer);
            if (triggerer == null || target == null)
                return false;
            if (target.player_id != triggerer.player_id)
                return false;
            if (triggerer_required_trait != null && !triggerer.HasTrait(triggerer_required_trait.id))
                return false;
            if (spawn_trait != null && !target.HasTrait(spawn_trait.id))
                return false;
            return true;
        }

        private bool IsInBaseRange(Game data, Slot target)
        {
            Card triggerer = data.GetCard(data.ability_triggerer);
            if (triggerer == null)
                return false;

            int dx = target.x - triggerer.slot.x;
            int dy = target.y - triggerer.slot.y;
            int dz = (triggerer.slot.x + triggerer.slot.y) - (target.x + target.y);
            int hexDistance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
            return hexDistance <= base_range;
        }
    }
}
