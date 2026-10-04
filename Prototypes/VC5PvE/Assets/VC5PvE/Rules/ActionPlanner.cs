using System.Collections.Generic;

namespace VC5PvE
{
    public static class ActionPlanner
    {
        public static ActionPlan Plan(BattleState state, ActionRequest request)
        {
            var plan = new ActionPlan { Request = request, Revision = state == null ? -1 : state.Revision };
            if (state == null || request == null) return Invalid(plan, "战斗或动作不存在");
            var actor = state.FindUnit(request.ActorId);
            if (actor == null || !actor.IsAlive) return Invalid(plan, "执行者不可用");
            if (state.Phase == BattlePhase.Player && actor.Team != Team.Player) return Invalid(plan, "不是玩家棋子");
            if (state.Phase == BattlePhase.Enemy && actor.Team != Team.Enemy) return Invalid(plan, "不是敌方棋子");
            if (state.Phase != BattlePhase.Player && state.Phase != BattlePhase.Enemy) return Invalid(plan, "当前阶段不能行动");

            ActionPlan actionPlan;
            switch (request.Kind)
            {
                case ActionKind.Move: actionPlan = PlanMove(state, actor, request, plan); break;
                case ActionKind.Attack: actionPlan = PlanAttack(state, actor, request, plan); break;
                case ActionKind.Heal: actionPlan = PlanHeal(state, actor, request, plan); break;
                case ActionKind.Card: actionPlan = PlanCard(state, actor, request, plan); break;
                default: actionPlan = Invalid(plan, "未知动作"); break;
            }
            if (actionPlan.IsValid && actor.Team == Team.Player && actor.Ap < actionPlan.Cost) return Invalid(actionPlan, "行动点不足");
            return actionPlan;
        }

        private static ActionPlan PlanMove(BattleState state, UnitState actor, ActionRequest request, ActionPlan plan)
        {
            if (!request.Destination.HasValue) return Invalid(plan, "请选择移动目标格");
            var limit = actor.MoveSteps;
            if (actor.Team == Team.Enemy && actor.Role == UnitRole.Guard) limit = 1;
            if (actor.Team == Team.Enemy && actor.Role == UnitRole.Shooter) limit = 1;
            var path = FindPath(state, actor, request.Destination.Value, limit);
            if (path == null || path.Count == 0) return Invalid(plan, "目标格不可达");
            plan.Path.AddRange(path); plan.Cost = actor.Team == Team.Player ? 1 : 0;
            return Valid(plan, "移动 " + path.Count + " 格");
        }

        private static ActionPlan PlanAttack(BattleState state, UnitState actor, ActionRequest request, ActionPlan plan)
        {
            if (actor.Team == Team.Player)
            {
                if (actor.AttackUsed) return Invalid(plan, "本回合已普攻");
                plan.Cost = 1;
            }
            else
            {
                if (actor.EnemyAttackUsed) return Invalid(plan, "本回合已攻击");
                plan.Cost = 0;
            }
            var target = state.FindUnit(request.TargetId);
            if (!CanDamageTarget(state, actor, target, actor.AttackRange)) return Invalid(plan, "目标超出射程、被遮挡或不合法");
            SetDamagePreview(state, actor, target, actor.Attack, 0, plan);
            return Valid(plan, actor.Id + " 攻击 " + target.Id);
        }

        private static ActionPlan PlanHeal(BattleState state, UnitState actor, ActionRequest request, ActionPlan plan)
        {
            if (actor.Team != Team.Player || actor.Role != UnitRole.Support) return Invalid(plan, "只有辅助可以急救");
            if (actor.HealUsed) return Invalid(plan, "本回合已急救");
            var target = state.FindUnit(request.TargetId);
            if (target == null || !target.IsAlive || target.Team != Team.Player || target.Hp >= target.MaxHp || actor.Position.ManhattanDistance(target.Position) > 2)
                return Invalid(plan, "急救目标无效或已满血");
            plan.Cost = 1; plan.PlannedDamage = -System.Math.Min(2, target.MaxHp - target.Hp);
            return Valid(plan, "治疗 " + target.Id);
        }

        private static ActionPlan PlanCard(BattleState state, UnitState actor, ActionRequest request, ActionPlan plan)
        {
            if (actor.Team != Team.Player) return Invalid(plan, "敌方不能使用卡牌");
            var index = state.Hand.FindIndex(card => card.Id == request.CardId);
            if (index < 0) return Invalid(plan, "手牌不存在");
            var kind = state.Hand[index].Kind;
            var definition = Definitions.Card(kind);
            if (definition.AllowedRole.HasValue && definition.AllowedRole.Value != actor.Role) return Invalid(plan, "执行者职业不符");
            plan.Cost = definition.Cost;
            switch (kind)
            {
                case CardKind.Advance:
                {
                    if (!request.Destination.HasValue) return Invalid(plan, "请选择推进目标格");
                    var path = FindPath(state, actor, request.Destination.Value, 2);
                    if (path == null || path.Count == 0) return Invalid(plan, "推进目标格不可达");
                    plan.Path.AddRange(path); return Valid(plan, "推进 " + path.Count + " 格并获得护盾");
                }
                case CardKind.HeavyAttack:
                case CardKind.SparkMark:
                case CardKind.StarBurst:
                {
                    var target = state.FindUnit(request.TargetId);
                    var range = kind == CardKind.HeavyAttack ? actor.AttackRange : 3;
                    if (!CanDamageTarget(state, actor, target, range)) return Invalid(plan, "伤害目标超出射程、被遮挡或不合法");
                    var damageBase = kind == CardKind.HeavyAttack ? 3 : kind == CardKind.SparkMark ? 1 : 2;
                    var damageBonus = kind == CardKind.StarBurst && target.MarkExpiresRound > 0 ? 2 : 0;
                    SetDamagePreview(state, actor, target, damageBase, damageBonus, plan);
                    return Valid(plan, definition.Name + " → " + target.Id);
                }
                case CardKind.Cover:
                    if (actor.Shield >= 2) return Invalid(plan, "已有足量护盾");
                    return Valid(plan, "自身获得2护盾");
                case CardKind.Charge:
                {
                    if (actor.Role != UnitRole.Warrior || !request.Destination.HasValue) return Invalid(plan, "突入需要战士和目标格");
                    var path = FindPath(state, actor, request.Destination.Value, 3);
                    if (path == null || path.Count == 0) return Invalid(plan, "突入路径不可达");
                    var finalPos = request.Destination.Value;
                    var target = state.FindUnit(request.TargetId);
                    if (target == null || !target.IsAlive || target.Team == actor.Team || finalPos.ManhattanDistance(target.Position) != 1)
                        return Invalid(plan, "突入终点必须邻接敌人");
                    plan.Path.AddRange(path);
                    var movedState = state.Clone();
                    movedState.FindUnit(actor.Id).Position = finalPos;
                    SetDamagePreview(movedState, movedState.FindUnit(actor.Id), movedState.FindUnit(target.Id), 2, 0, plan);
                    return Valid(plan, "突入并攻击 " + target.Id);
                }
                case CardKind.Inspire:
                {
                    var target = state.FindUnit(request.TargetId);
                    if (target == null || !target.IsAlive || target.Team != actor.Team || target.Id == actor.Id || actor.Position.ManhattanDistance(target.Position) > 2)
                        return Invalid(plan, "鼓舞目标无效");
                    if (target.InspireExpiresRound >= state.Round + 1) return Invalid(plan, "目标已有鼓舞");
                    return Valid(plan, "鼓舞 " + target.Id);
                }
                default: return Invalid(plan, "未支持的卡牌");
            }
        }

        private static bool CanDamageTarget(BattleState state, UnitState actor, UnitState target, int range)
        {
            if (target == null || !target.IsAlive || target.Team == actor.Team) return false;
            if (actor.Position.ManhattanDistance(target.Position) > range) return false;
            return range <= 1 || GridRules.HasLineOfSight(state, actor.Position, target.Position);
        }

        private static void SetDamagePreview(BattleState state, UnitState actor, UnitState target, int damageBase, int damageBonus, ActionPlan plan)
        {
            plan.DamageBase = damageBase;
            plan.DamageBonus = damageBonus;
            plan.DamageSummary = DamageRules.Preview(state, actor, target, damageBase, damageBonus);
            plan.PlannedDamage = plan.DamageSummary.HpLost;
        }

        internal static List<GridPos> FindPath(BattleState state, UnitState actor, GridPos destination, int maxSteps)
        {
            if (!destination.IsInside(state.Width, state.Height) || state.Obstacles.Contains(destination)) return null;
            var occupant = state.UnitAt(destination);
            if (occupant != null && occupant.Id != actor.Id) return null;
            var queue = new Queue<GridPos>();
            var previous = new Dictionary<GridPos, GridPos>();
            var distance = new Dictionary<GridPos, int>();
            queue.Enqueue(actor.Position); distance[actor.Position] = 0;
            var directions = new[] { new GridPos(0, -1), new GridPos(1, 0), new GridPos(0, 1), new GridPos(-1, 0) };
            while (queue.Count > 0)
            {
                var here = queue.Dequeue(); if (here == destination) break;
                if (distance[here] >= maxSteps) continue;
                foreach (var dir in directions)
                {
                    var next = new GridPos(here.X + dir.X, here.Y + dir.Y);
                    if (!next.IsInside(state.Width, state.Height) || state.Obstacles.Contains(next) || distance.ContainsKey(next)) continue;
                    var blocked = state.UnitAt(next); if (blocked != null && blocked.Id != actor.Id) continue;
                    distance[next] = distance[here] + 1; previous[next] = here; queue.Enqueue(next);
                }
            }
            if (!distance.ContainsKey(destination) || distance[destination] == 0 || distance[destination] > maxSteps) return null;
            var path = new List<GridPos>(); var cursor = destination;
            while (cursor != actor.Position) { path.Add(cursor); cursor = previous[cursor]; }
            path.Reverse(); return path;
        }

        private static ActionPlan Invalid(ActionPlan plan, string reason) { plan.IsValid = false; plan.Reason = reason; return plan; }
        private static ActionPlan Valid(ActionPlan plan, string summary) { plan.IsValid = true; plan.Reason = null; plan.Summary = summary; return plan; }
    }
}
