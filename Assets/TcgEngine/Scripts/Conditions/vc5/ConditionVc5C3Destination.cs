namespace TcgEngine
{
    public class ConditionVc5C3Destination : ConditionData
    {
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return Vc5C3Rules.Plan(data, caster, data.GetCard(data.ability_triggerer), target).valid;
        }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target) { return false; }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target) { return false; }
    }
}
