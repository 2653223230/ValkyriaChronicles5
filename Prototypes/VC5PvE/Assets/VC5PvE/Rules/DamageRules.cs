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
                var inspire = Math.Max(1, attacker.InspireBonus);
                damage += inspire;
                summary.BonusDamage += inspire;
            }
            if (state.LevelIndex == 3 && target.Role == UnitRole.Boss && state.BossVulnerableRound != state.Round && HasLivingAnchor(state))
            {
                damage = Math.Max(1, damage - 1);
                summary.BossReduction = 1;
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

            if (Math.Max(0, baseDamage + bonusDamage) > 0 && attacker.InspireExpiresRound > 0) { attacker.InspireExpiresRound = 0; attacker.InspireBonus = 1; }
            if (summary.GuardReduction > 0) state.GuardReducedTargetsThisEnemyTurn.Add(target.Id);
            target.Shield -= summary.ShieldAbsorbed;
            target.Hp -= summary.HpLost;
            if (target.IsFacility && target.Role == UnitRole.ShieldPillar && !target.IsAlive && !string.IsNullOrEmpty(target.BoundUnitId))
            {
                var bound = state.FindUnit(target.BoundUnitId);
                if (bound != null) bound.Shield = 0;
            }
            if (target.IsFacility && target.Role == UnitRole.ResonanceAnchor && !target.IsAlive && state.LevelIndex == 3)
                state.BossVulnerableRound = state.Round + 1;
            if (attacker.Team == Team.Enemy && target.Team == Team.Player && target.IsAlive && summary.ShieldAbsorbed > 0 &&
                GrowthRules.HasTalent(target, TalentKind.CounterReady) && !target.CounterReadyTriggeredThisEnemyTurn)
            {
                target.CounterReadyPrepared = true;
                target.CounterReadyTriggeredThisEnemyTurn = true;
            }
            return summary;
        }

        private static bool HasLivingAnchor(BattleState state)
        {
            foreach (var unit in state.Units) if (unit.IsAlive && unit.Role == UnitRole.ResonanceAnchor) return true;
            return false;
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
