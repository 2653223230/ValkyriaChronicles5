using System;
using System.Collections.Generic;

namespace VC5PvE
{
    public static class EnemyPlanner
    {
        private static readonly string[] EnemyOrder = { "guard-a", "guard-b", "guard-c", "shooter", "boss" };
        private static readonly string[] PlayerOrder = { "warrior", "mage", "support" };

        public static EnemyTurnPlan PlanTurn(BattleState state)
        {
            var result = new EnemyTurnPlan();
            if (state == null || state.Phase != BattlePhase.Enemy) return result;
            var simulation = state.Clone();
            RefreshPillarShields(simulation);
            foreach (var enemyId in EnemyOrder)
            {
                var enemy = simulation.FindUnit(enemyId);
                if (enemy == null || !enemy.IsAlive || simulation.Phase != BattlePhase.Enemy) continue;
                if (enemy.Role == UnitRole.Guard) PlanGuard(simulation, enemy, result);
                else if (enemy.Role == UnitRole.Shooter) PlanShooter(simulation, enemy, result);
                else if (enemy.Role == UnitRole.Boss)
                {
                    if (simulation.Round % 2 == 0 && simulation.BossTelegraph.HasValue)
                    {
                        var aoe = new ActionPlan { IsValid = true, Cost = 0, Revision = simulation.Revision, Request = new ActionRequest { Kind = ActionKind.BossAoe, ActorId = enemy.Id }, Summary = "钟鸣震荡" };
                        AddAndApply(simulation, aoe, result);
                    }
                    else PlanShooter(simulation, enemy, result);
                }
            }
            var boss = simulation.FindUnit("boss");
            result.NextBossTelegraph = state.LevelIndex == 3 && boss != null && boss.IsAlive && (state.Round + 1) % 2 == 0 ? ChooseBossTelegraph(simulation, boss) : null;
            return result;
        }

        public static List<ActionResult> ExecuteTurn(BattleState state)
        {
            var results = new List<ActionResult>();
            if (state == null || state.Phase != BattlePhase.Enemy) return results;
            RefreshPillarShields(state);
            var plan = PlanTurn(state);
            foreach (var action in plan.Actions)
            {
                if (state.Phase != BattlePhase.Enemy) break;
                var actual = BattleResolver.Apply(state, action);
                results.Add(actual);
                if (!actual.Success) break;
            }
            if (state.Phase == BattlePhase.Enemy)
            {
                state.BossTelegraph = plan.NextBossTelegraph;
                TurnRules.BeginPlayerTurn(state);
            }
            return results;
        }

        private static void PlanGuard(BattleState state, UnitState guard, EnemyTurnPlan turn)
        {
            if (FindAdjacentTarget(state, guard) == null && !guard.EnemyMoveUsed)
            {
                var destination = BestApproach(state, guard, 1, false);
                if (destination.HasValue) AddAndApply(state, new ActionRequest { Kind = ActionKind.Move, ActorId = guard.Id, Destination = destination }, turn);
            }
            var target = FindAdjacentTarget(state, guard);
            if (target != null && !guard.EnemyAttackUsed)
                AddAndApply(state, new ActionRequest { Kind = ActionKind.Attack, ActorId = guard.Id, TargetId = target.Id }, turn);
        }

        private static void PlanShooter(BattleState state, UnitState shooter, EnemyTurnPlan turn)
        {
            var target = FindDamageableTarget(state, shooter, 3);
            if (target == null && !shooter.EnemyMoveUsed)
            {
                var destination = BestApproach(state, shooter, 1, true);
                if (destination.HasValue) AddAndApply(state, new ActionRequest { Kind = ActionKind.Move, ActorId = shooter.Id, Destination = destination }, turn);
                target = FindDamageableTarget(state, shooter, 3);
            }
            if (target != null && !shooter.EnemyAttackUsed)
                AddAndApply(state, new ActionRequest { Kind = ActionKind.Attack, ActorId = shooter.Id, TargetId = target.Id }, turn);
        }

        private static GridPos? ChooseBossTelegraph(BattleState state, UnitState boss)
        {
            GridPos? best = null; var bestCount = -1; var bestDistance = int.MaxValue;
            foreach (var id in PlayerOrder)
            {
                var centerUnit = state.FindUnit(id);
                if (centerUnit == null || !centerUnit.IsAlive) continue;
                var count = 0;
                foreach (var playerId in PlayerOrder)
                {
                    var player = state.FindUnit(playerId);
                    if (player != null && player.IsAlive && System.Math.Abs(player.Position.X - centerUnit.Position.X) <= 1 && System.Math.Abs(player.Position.Y - centerUnit.Position.Y) <= 1) count++;
                }
                var distance = boss.Position.ManhattanDistance(centerUnit.Position);
                if (count > bestCount || (count == bestCount && distance < bestDistance))
                { best = centerUnit.Position; bestCount = count; bestDistance = distance; }
            }
            return best;
        }

        public static void RefreshPillarShields(BattleState state)
        {
            if (state == null || state.LevelIndex != 2) return;
            foreach (var unit in state.Units) if (unit.Team == Team.Enemy && !unit.IsFacility) unit.Shield = 0;
            foreach (var pillar in state.Units)
            {
                if (!pillar.IsAlive || pillar.Role != UnitRole.ShieldPillar || string.IsNullOrEmpty(pillar.BoundUnitId)) continue;
                var bound = state.FindUnit(pillar.BoundUnitId);
                if (bound != null && bound.IsAlive) bound.Shield = System.Math.Max(bound.Shield, 1);
            }
        }

        private static UnitState FindAdjacentTarget(BattleState state, UnitState enemy)
        {
            UnitState best = null;
            foreach (var id in PlayerOrder)
            {
                var target = state.FindUnit(id);
                if (target == null || !target.IsAlive || enemy.Position.ManhattanDistance(target.Position) != 1) continue;
                if (BetterTarget(target, best, 1, 1)) best = target;
            }
            return best;
        }

        private static UnitState FindDamageableTarget(BattleState state, UnitState enemy, int range)
        {
            UnitState best = null;
            var bestDistance = int.MaxValue;
            foreach (var id in PlayerOrder)
            {
                var target = state.FindUnit(id);
                if (target == null || !target.IsAlive || enemy.Position.ManhattanDistance(target.Position) > range || !GridRules.HasLineOfSight(state, enemy.Position, target.Position)) continue;
                var distance = enemy.Position.ManhattanDistance(target.Position);
                if (BetterTarget(target, best, distance, bestDistance)) { best = target; bestDistance = distance; }
            }
            return best;
        }

        private static bool BetterTarget(UnitState candidate, UnitState current, int candidateDistance, int currentDistance)
        {
            if (current == null) return true;
            // Call sites iterate the frozen PlayerOrder, so equal distance/HP preserves that order.
            if (candidateDistance != currentDistance) return candidateDistance < currentDistance;
            return candidate.Hp < current.Hp;
        }

        private static GridPos? BestApproach(BattleState state, UnitState enemy, int steps, bool seekLine)
        {
            var reachable = GridRules.Reachable(state, enemy, steps);
            if (reachable.Count == 0) return null;
            GridPos? best = null; var bestDistance = int.MaxValue; var bestCanShoot = false;
            foreach (var pos in reachable)
            {
                var nearest = int.MaxValue; var canShoot = false;
                foreach (var id in PlayerOrder)
                {
                    var player = state.FindUnit(id); if (player == null || !player.IsAlive) continue;
                    nearest = Math.Min(nearest, pos.ManhattanDistance(player.Position));
                    if (pos.ManhattanDistance(player.Position) <= enemy.AttackRange && GridRules.HasLineOfSight(state, pos, player.Position)) canShoot = true;
                }
                if (seekLine && canShoot && !bestCanShoot) { best = pos; bestDistance = nearest; bestCanShoot = true; continue; }
                if (seekLine && bestCanShoot && !canShoot) continue;
                if (nearest < bestDistance) { best = pos; bestDistance = nearest; }
            }
            return best;
        }

        private static void AddAndApply(BattleState state, ActionRequest request, EnemyTurnPlan turn)
        {
            var action = ActionPlanner.Plan(state, request);
            AddAndApply(state, action, turn);
        }

        private static void AddAndApply(BattleState state, ActionPlan action, EnemyTurnPlan turn)
        {
            if (!action.IsValid) return;
            var applied = BattleResolver.Apply(state, action);
            if (!applied.Success) return;
            if (applied.DamageSummary != null)
            {
                action.DamageSummary = applied.DamageSummary;
                turn.DamageSummary.Add(applied.DamageSummary);
            }
            if (applied.AreaDamageSummaries != null) turn.DamageSummary.AddRange(applied.AreaDamageSummaries);
            turn.Actions.Add(action);
        }
    }
}
