namespace TcgEngine
{
    public class ConditionVc5R4SkillReady : ConditionData
    {
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            if (data.phase != GamePhase.Main || data.GetPlayer(caster.player_id).EndTurn
                || !data.IsPlayerActionTurn(data.GetPlayer(caster.player_id)) || !data.IsOnBoard(caster)) return false;
            if (ability.id == Vc5R4Rules.MoveSkill) return Vc5C3Rules.Reachable(data, caster, 1).Count > 1;
            return data.GetPlayer(caster.player_id).cards_hand.Exists(c => Vc5R4Rules.CanDiscard(data, caster, c));
        }
    }

    public class ConditionVc5R4Skill : ConditionData
    {
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return ability.id == Vc5R4Rules.PrepareSkill && Vc5R4Rules.CanDiscard(data, caster, target);
        }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target) { return false; }
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return ability.id == Vc5R4Rules.MoveSkill && target != caster.slot
                && Vc5C3Rules.Reachable(data, caster, 1).ContainsKey(target);
        }
    }
}
