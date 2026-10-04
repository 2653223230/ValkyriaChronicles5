using System;
using System.Collections.Generic;

namespace VC5PvE
{
    public static class EnemyPlanner
    {
        private static readonly string[] EnemyOrder = { "guard-a", "guard-b", "shooter" };
        private static readonly string[] PlayerOrder = { "warrior", "mage", "support" };

        public static EnemyTurnPlan PlanTurn(BattleState state)
        {
            var result = new EnemyTurnPlan();
            if (state == null || state.Phase != BattlePhase.Enemy) return result;
            var simulation = state.Clone();
            foreach (var enemyId in EnemyOrder)
            {
                var enemy = simulation.FindUnit(enemyId);
                if (enemy == null || !enemy.IsAlive || simulation.Phase != BattlePhase.Enemy) continue;
                if (enemy.Role == UnitRole.Guard) PlanGuard(simulation, enemy, result);
                else if (enemy.Role == UnitRole.Shooter) PlanShooter(simulation, enemy, result);
            }
            return result;
        }

        public static List<ActionResult> ExecuteTurn(BattleState state)
        {
            var results = new List<ActionResult>();
            if (state == null || state.Phase != BattlePhase.Enemy) return results;
            var plan = PlanTurn(state);
            foreach (var action in plan.Actions)
            {
                if (state.Phase != BattlePhase.Enemy) break;
                var actual = BattleResolver.Apply(state, action);
                results.Add(actual);
                if (!actual.Success) break;
            }
            if (state.Phase == BattlePhase.Enemy) TurnRules.BeginPlayerTurn(state);
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
            if (!action.IsValid) return;
            var applied = BattleResolver.Apply(state, action);
            if (!applied.Success) return;
            if (applied.DamageSummary != null)
            {
                action.DamageSummary = applied.DamageSummary;
                turn.DamageSummary.Add(applied.DamageSummary);
            }
            turn.Actions.Add(action);
        }
    }
}
