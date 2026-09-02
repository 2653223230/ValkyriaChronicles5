namespace TcgEngine
{
    public class ConditionVc5C3Actor : ConditionData
    {
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return Vc5C3Rules.Plan(data, caster, target).valid;
        }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target) { return false; }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target) { return false; }
    }
}
