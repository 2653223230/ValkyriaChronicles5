using System;

namespace VC5PvE
{
    public static class DamageRules
    {
        public static DamageSummary Preview(BattleState state, UnitState attacker, UnitState target, int baseDamage, int bonusDamage)
        {
            var summary = new DamageSummary { TargetId = target == null ? null : target.Id, BaseDamage = baseDamage, BonusDamage = bonusDamage };
            if (state == null || attacker == null || target == null || !target.IsAlive) return summary;

            var damage = Math.Max(0, baseDamage + bonusDamage);
            if (damage > 0 && attacker.InspireExpiresRound > 0)
            {
                damage++;
                summary.BonusDamage++;
            }
            if (state.Phase == BattlePhase.Enemy && target.Team == Team.Player &&
                !state.GuardReducedTargetsThisEnemyTurn.Contains(target.Id) && HasAdjacentWarrior(state, target))
            {
                damage = Math.Max(1, damage - 1);
                summary.GuardReduction = 1;
            }

            summary.ShieldAbsorbed = Math.Min(target.Shield, damage);
            summary.HpLost = Math.Min(target.Hp, damage - summary.ShieldAbsorbed);
            summary.Killed = summary.HpLost >= target.Hp;
            return summary;
        }

        public static DamageSummary Apply(BattleState state, UnitState attacker, UnitState target, int baseDamage, int bonusDamage)
        {
            var summary = Preview(state, attacker, target, baseDamage, bonusDamage);
            if (state == null || attacker == null || target == null || !target.IsAlive) return summary;

            if (Math.Max(0, baseDamage + bonusDamage) > 0 && attacker.InspireExpiresRound > 0) attacker.InspireExpiresRound = 0;
            if (summary.GuardReduction > 0) state.GuardReducedTargetsThisEnemyTurn.Add(target.Id);
            target.Shield -= summary.ShieldAbsorbed;
            target.Hp -= summary.HpLost;
            return summary;
        }

        private static bool HasAdjacentWarrior(BattleState state, UnitState target)
        {
            foreach (var unit in state.Units)
                if (unit.IsAlive && unit.Team == target.Team && unit.Role == UnitRole.Warrior && unit.Id != target.Id &&
                    unit.Position.ManhattanDistance(target.Position) == 1) return true;
            return false;
        }
    }
}
