using System;
using System.Collections.Generic;
using System.Linq;
using VC5PvE;

namespace VC5PvE.SharedAP
{
    /// <summary>Projects shared-rule state and results into the legacy visual-only data model.</summary>
    public static class SharedVisualAdapter
    {
        public static BattleState ToBattleState(SState state)
        {
            if (state == null) return null;
            var visual = new BattleState
            {
                Width = 7,
                Height = 7,
                Round = state.Round,
                Revision = state.Revision,
                RandomSeed = state.Seed,
                Phase = ToBattlePhase(state.Phase),
                LevelIndex = 1,
                BossTelegraph = null
            };

            foreach (var unit in state.Units)
            {
                if (unit == null) continue;
                UnitRole role = ToVisualRole(unit.Role);
                visual.Units.Add(new UnitState
                {
                    Id = unit.Id,
                    Role = role,
                    Team = unit.Player ? Team.Player : Team.Enemy,
                    Position = new GridPos(unit.Position.X, unit.Position.Y),
                    Hp = unit.Hp,
                    MaxHp = unit.MaxHp,
                    Ap = 0,
                    MaxAp = 0,
                    Attack = SharedRules.WeaponDamage(unit) + (unit.Role == SRole.Archer && unit.Braced ? 2 : 0),
                    AttackRange = SharedRules.WeaponRange(unit),
                    MoveSteps = 2,
                    AttackUsed = unit.AttackUsed,
                    Shield = unit.Shield,
                    IsFacility = unit.Role == SRole.Cannon,
                    Talents = new List<TalentKind>()
                });
            }
            foreach (SPos obstacle in state.Obstacles)
                visual.Obstacles.Add(new GridPos(obstacle.X, obstacle.Y));
            return visual;
        }

        public static ActionPlan ToPlan(SState before, SPlan plan, SAction request)
        {
            if (plan == null) return null;
            var actorBefore = before == null || request == null ? null : before.Find(request.ActorId);
            var actorAfter = plan.After == null || request == null ? null : plan.After.Find(request.ActorId);
            bool actorMoved = actorBefore != null && actorAfter != null && actorBefore.Position != actorAfter.Position;
            var adapted = new ActionPlan
            {
                IsValid = plan.Valid,
                Reason = plan.Reason,
                Revision = plan.Revision,
                Summary = plan.Summary,
                Request = ToActionRequest(request, actorMoved ? actorAfter.Position : (SPos?)null),
                Path = new List<GridPos>()
            };
            if (request != null && request.Kind == SActionKind.Card && before != null)
            {
                var card = before.Hand.FirstOrDefault(c => c.Id == request.CardId);
                if (card != null) adapted.Cost = SharedRules.Cost(card.Kind);
            }
            else if (request != null && request.Kind == SActionKind.BasicAttack) adapted.Cost = 2;

            if (actorMoved)
            {
                bool skippedOrigin = false;
                foreach (SPos step in plan.Path)
                {
                    if (!skippedOrigin && step == actorBefore.Position)
                    {
                        skippedOrigin = true;
                        continue;
                    }
                    adapted.Path.Add(new GridPos(step.X, step.Y));
                }
                if (adapted.Path.Count == 0)
                    adapted.Path.Add(new GridPos(actorAfter.Position.X, actorAfter.Position.Y));
            }

            var summaries = DamageChanges(before, plan.After);
            if (request != null)
                adapted.DamageSummary = summaries.FirstOrDefault(s => s.TargetId == request.TargetId);
            if (adapted.DamageSummary == null && summaries.Count > 0 && request != null && request.ActorId != "cannon")
                adapted.DamageSummary = summaries[0];
            foreach (var summary in summaries)
                if (adapted.DamageSummary == null || summary.TargetId != adapted.DamageSummary.TargetId)
                    adapted.AreaDamageSummaries.Add(summary);
            return adapted;
        }

        public static ActionResult ToResult(SState before, SPlan plan, SAction request)
        {
            if (plan == null) return null;
            var result = new ActionResult
            {
                Success = plan.Valid,
                Reason = plan.Reason,
                Summary = plan.Summary
            };
            if (!plan.Valid || before == null || plan.After == null) return result;

            var summaries = DamageChanges(before, plan.After);
            if (request != null)
                result.DamageSummary = summaries.FirstOrDefault(s => s.TargetId == request.TargetId);
            foreach (var summary in summaries)
            {
                if (result.DamageSummary != null && summary.TargetId == result.DamageSummary.TargetId) continue;
                result.AreaDamageSummaries.Add(summary);
            }

            if (result.DamageSummary != null) result.Damage = result.DamageSummary.HpLost;
            else result.Damage = summaries.Sum(s => s.HpLost);
            if (request != null)
            {
                string recipientId = string.IsNullOrEmpty(request.TargetId) ? request.ActorId : request.TargetId;
                var oldTarget = before.Find(recipientId);
                var newTarget = plan.After.Find(recipientId);
                if (oldTarget != null && newTarget != null)
                    result.Healing = Math.Max(0, newTarget.Hp - oldTarget.Hp);
            }
            return result;
        }

        private static ActionRequest ToActionRequest(SAction request, SPos? actorDestination)
        {
            if (request == null) return null;
            ActionKind kind = request.Kind == SActionKind.ShortStep ? ActionKind.Move
                : request.Kind == SActionKind.BasicAttack ? ActionKind.Attack : ActionKind.Card;
            return new ActionRequest
            {
                Kind = request.ActorId == "cannon" ? ActionKind.BossAoe : kind,
                ActorId = request.ActorId,
                CardId = request.CardId,
                TargetId = request.TargetId,
                Destination = actorDestination.HasValue
                    ? new GridPos(actorDestination.Value.X, actorDestination.Value.Y)
                    : (GridPos?)null
            };
        }

        private static List<DamageSummary> DamageChanges(SState before, SState after)
        {
            var summaries = new List<DamageSummary>();
            if (before == null || after == null) return summaries;
            foreach (var oldUnit in before.Units)
            {
                if (oldUnit == null) continue;
                var newUnit = after.Find(oldUnit.Id);
                if (newUnit == null) continue;
                int hpLost = Math.Max(0, oldUnit.Hp - newUnit.Hp);
                int shieldAbsorbed = Math.Max(0, oldUnit.Shield - newUnit.Shield);
                if (hpLost == 0 && shieldAbsorbed == 0) continue;
                summaries.Add(new DamageSummary
                {
                    TargetId = oldUnit.Id,
                    HpLost = hpLost,
                    ShieldAbsorbed = shieldAbsorbed,
                    Killed = oldUnit.Hp > 0 && newUnit.Hp <= 0
                });
            }
            return summaries;
        }

        private static BattlePhase ToBattlePhase(SPhase phase)
        {
            switch (phase)
            {
                case SPhase.Enemy: return BattlePhase.Enemy;
                case SPhase.Won: return BattlePhase.Victory;
                case SPhase.Lost: return BattlePhase.Defeat;
                default: return BattlePhase.Player;
            }
        }

        private static UnitRole ToVisualRole(SRole role)
        {
            switch (role)
            {
                case SRole.Vanguard: return UnitRole.Warrior;
                case SRole.Archer: return UnitRole.Support;
                case SRole.Mage: return UnitRole.Mage;
                case SRole.Patrol: return UnitRole.Guard;
                case SRole.Crossbow: return UnitRole.Shooter;
                case SRole.Guard: return UnitRole.Guard;
                case SRole.Cannon: return UnitRole.ShieldPillar;
                default: return UnitRole.Guard;
            }
        }

    }
}
