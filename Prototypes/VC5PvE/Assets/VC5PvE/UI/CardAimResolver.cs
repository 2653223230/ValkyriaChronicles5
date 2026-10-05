using System;
using System.Collections.Generic;
using UnityEngine;

namespace VC5PvE
{
    public static class CardAimResolver
    {
        public static UnitState ResolveActor(BattleState state, string cardId, string selectedId, UnitState hovered = null)
        {
            if (state == null) return null;
            var actor = state.FindUnit(selectedId);
            return actor != null && actor.IsAlive && actor.Team == Team.Player ? actor : null;
        }

        public static bool CanExecute(BattleState state, string cardId, string actorId)
        {
            return EnumerateLegalPlans(state, cardId, actorId).Count > 0;
        }

        public static List<string> LegalExecutors(BattleState state, string cardId)
        {
            var result = new List<string>();
            if (state == null) return result;
            foreach (var unit in state.Units)
            {
                if (unit.Team == Team.Player && unit.IsAlive && CanExecute(state, cardId, unit.Id))
                    result.Add(unit.Id);
            }
            return result;
        }

        public static ActionPlan Resolve(BattleState state, string cardId, string selectedId, UnitState hovered,
            GridPos? cell, Vector2 pointer, Func<GridPos, Vector2> project)
        {
            var card = FindCard(state, cardId);
            var actor = state == null ? null : state.FindUnit(selectedId);
            var request = new ActionRequest
            {
                Kind = ActionKind.Card,
                CardId = cardId,
                ActorId = selectedId,
                TargetId = card != null && card.Kind == CardKind.Cover ? null : hovered == null ? null : hovered.Id,
                Destination = null
            };

            if (card == null || actor == null || !actor.IsAlive || actor.Team != Team.Player)
                return Invalid(state, request, "请先选择可用的己方执行者");

            if (card.Kind != CardKind.Charge)
                return ActionPlanner.Plan(state, requestWithCell(request, cell, card.Kind));

            if (hovered == null || hovered.Team != Team.Enemy || !hovered.IsAlive)
                return Invalid(state, request, "先选择敌人，再选择突入落点");

            var destinations = LegalChargeDestinations(state, cardId, selectedId, hovered.Id);
            ActionPlan best = null;
            var bestScore = float.MaxValue;
            foreach (var destination in destinations)
            {
                var candidate = new ActionRequest
                {
                    Kind = ActionKind.Card,
                    CardId = cardId,
                    ActorId = selectedId,
                    TargetId = hovered.Id,
                    Destination = destination
                };
                var plan = ActionPlanner.Plan(state, candidate);
                if (!plan.IsValid) continue;
                var distance = project == null ? plan.Path.Count : (project(destination) - pointer).sqrMagnitude;
                if (best == null || distance < bestScore - .001f)
                {
                    best = plan;
                    bestScore = distance;
                }
            }
            return best ?? Invalid(state, request, "没有可达的邻敌落点（最多移动3格）");
        }

        public static List<GridPos> LegalTargets(BattleState state, string cardId, string actorId)
        {
            var result = new List<GridPos>();
            var seen = new HashSet<GridPos>();
            foreach (var plan in EnumerateLegalPlans(state, cardId, actorId))
            {
                GridPos targetPosition;
                if (plan.Request.TargetId != null)
                {
                    var target = state.FindUnit(plan.Request.TargetId);
                    if (target == null) continue;
                    targetPosition = target.Position;
                }
                else if (plan.Request.Destination.HasValue)
                    targetPosition = plan.Request.Destination.Value;
                else
                {
                    var actor = state.FindUnit(actorId);
                    if (actor == null) continue;
                    targetPosition = actor.Position;
                }

                if (seen.Add(targetPosition)) result.Add(targetPosition);
            }
            return result;
        }

        public static List<GridPos> LegalChargeDestinations(BattleState state, string cardId, string actorId, string targetId)
        {
            var result = new List<GridPos>();
            var card = FindCard(state, cardId);
            if (card == null || card.Kind != CardKind.Charge || string.IsNullOrEmpty(targetId)) return result;

            var target = state.FindUnit(targetId);
            var actor = state.FindUnit(actorId);
            if (target == null || actor == null) return result;

            foreach (var destination in AdjacentCells(target.Position))
            {
                var request = new ActionRequest
                {
                    Kind = ActionKind.Card,
                    CardId = cardId,
                    ActorId = actorId,
                    TargetId = targetId,
                    Destination = destination
                };
                if (ActionPlanner.Plan(state, request).IsValid) result.Add(destination);
            }
            return result;
        }

        private static ActionRequest requestWithCell(ActionRequest request, GridPos? cell, CardKind kind)
        {
            if (kind == CardKind.Advance) request.Destination = cell;
            return request;
        }

        private static List<ActionPlan> EnumerateLegalPlans(BattleState state, string cardId, string actorId)
        {
            var result = new List<ActionPlan>();
            var card = FindCard(state, cardId);
            var actor = state == null ? null : state.FindUnit(actorId);
            if (card == null || actor == null || actor.Team != Team.Player || !actor.IsAlive) return result;

            if (card.Kind == CardKind.Cover)
            {
                AddLegalPlan(state, result, new ActionRequest
                {
                    Kind = ActionKind.Card, CardId = cardId, ActorId = actorId
                });
                return result;
            }

            if (card.Kind == CardKind.Advance)
            {
                for (var y = 0; y < state.Height; y++)
                    for (var x = 0; x < state.Width; x++)
                        AddLegalPlan(state, result, new ActionRequest
                        {
                            Kind = ActionKind.Card, CardId = cardId, ActorId = actorId,
                            Destination = new GridPos(x, y)
                        });
                return result;
            }

            if (card.Kind == CardKind.Charge)
            {
                foreach (var target in state.Units)
                {
                    if (!target.IsAlive || target.Team != Team.Enemy) continue;
                    foreach (var destination in AdjacentCells(target.Position))
                        AddLegalPlan(state, result, new ActionRequest
                        {
                            Kind = ActionKind.Card, CardId = cardId, ActorId = actorId,
                            TargetId = target.Id, Destination = destination
                        });
                }
                return result;
            }

            if (card.Kind == CardKind.HeavyAttack || card.Kind == CardKind.SparkMark || card.Kind == CardKind.StarBurst)
            {
                foreach (var target in state.Units)
                    if (target.IsAlive && target.Team == Team.Enemy)
                        AddLegalPlan(state, result, new ActionRequest
                        {
                            Kind = ActionKind.Card, CardId = cardId, ActorId = actorId, TargetId = target.Id
                        });
                return result;
            }

            if (card.Kind == CardKind.Inspire)
            {
                foreach (var target in state.Units)
                    if (target.IsAlive && target.Team == actor.Team && target.Id != actor.Id)
                        AddLegalPlan(state, result, new ActionRequest
                        {
                            Kind = ActionKind.Card, CardId = cardId, ActorId = actorId, TargetId = target.Id
                        });
            }
            return result;
        }

        private static void AddLegalPlan(BattleState state, List<ActionPlan> result, ActionRequest request)
        {
            var plan = ActionPlanner.Plan(state, request);
            if (plan.IsValid) result.Add(plan);
        }

        private static IEnumerable<GridPos> AdjacentCells(GridPos position)
        {
            yield return new GridPos(position.X, position.Y - 1);
            yield return new GridPos(position.X + 1, position.Y);
            yield return new GridPos(position.X, position.Y + 1);
            yield return new GridPos(position.X - 1, position.Y);
        }

        private static CardInstance FindCard(BattleState state, string cardId)
        {
            return state == null || string.IsNullOrEmpty(cardId) ? null : state.Hand.Find(c => c.Id == cardId);
        }

        private static ActionPlan Invalid(BattleState state, ActionRequest request, string reason)
        {
            return new ActionPlan
            {
                Request = request,
                Revision = state == null ? -1 : state.Revision,
                IsValid = false,
                Reason = reason
            };
        }
    }
}
