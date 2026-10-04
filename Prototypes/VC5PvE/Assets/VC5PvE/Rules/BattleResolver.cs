namespace VC5PvE
{
    public static class BattleResolver
    {
        public static ActionResult Apply(BattleState state, ActionPlan plan)
        {
            if (state == null || plan == null) return Fail("战斗或计划不存在");
            if (!plan.IsValid) return Fail(plan.Reason ?? "动作无效");
            if (plan.Revision != state.Revision) return Fail("动作预览已过期");
            var actor = state.FindUnit(plan.Request.ActorId);
            if (actor == null || !actor.IsAlive) return Fail("执行者不可用");
            if (actor.Ap < plan.Cost) return Fail("行动点不足");

            actor.Ap -= plan.Cost;
            if (actor.Team == Team.Player) state.FirstActionMade = true;
            var result = new ActionResult { Success = true, Summary = plan.Summary };
            switch (plan.Request.Kind)
            {
                case ActionKind.Move:
                    MoveAlong(actor, plan.Path);
                    if (actor.Team == Team.Enemy) actor.EnemyMoveUsed = true;
                    break;
                case ActionKind.Attack:
                {
                    if (actor.Team == Team.Player) actor.AttackUsed = true; else actor.EnemyAttackUsed = true;
                    result.DamageSummary = DamageRules.Apply(state, actor, state.FindUnit(plan.Request.TargetId), plan.DamageBase, plan.DamageBonus);
                    result.Damage = result.DamageSummary.HpLost;
                    break;
                }
                case ActionKind.Heal:
                {
                    actor.HealUsed = true;
                    var target = state.FindUnit(plan.Request.TargetId);
                    result.Healing = System.Math.Min(2, target.MaxHp - target.Hp);
                    target.Hp += result.Healing;
                    break;
                }
                case ActionKind.Card:
                    ApplyCard(state, actor, plan, result);
                    break;
            }
            state.Revision++;
            RefreshOutcome(state);
            return result;
        }

        private static void ApplyCard(BattleState state, UnitState actor, ActionPlan plan, ActionResult result)
        {
            var request = plan.Request;
            var index = state.Hand.FindIndex(card => card.Id == request.CardId);
            if (index < 0) { result.Success = false; result.Reason = "手牌不存在"; return; }
            var card = state.Hand[index]; state.Hand.RemoveAt(index); state.DiscardPile.Add(card);
            switch (card.Kind)
            {
                case CardKind.Advance:
                    MoveAlong(actor, plan.Path); actor.Shield = System.Math.Max(actor.Shield, 1); break;
                case CardKind.HeavyAttack:
                    result.DamageSummary = DamageRules.Apply(state, actor, state.FindUnit(request.TargetId), plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost; break;
                case CardKind.Cover:
                    actor.Shield = System.Math.Max(actor.Shield, 2); break;
                case CardKind.Charge:
                {
                    MoveAlong(actor, plan.Path);
                    var target = state.FindUnit(request.TargetId);
                    result.DamageSummary = DamageRules.Apply(state, actor, target, plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost;
                    if (target != null && target.IsAlive) TryPush(state, actor, target);
                    break;
                }
                case CardKind.SparkMark:
                {
                    var target = state.FindUnit(request.TargetId);
                    result.DamageSummary = DamageRules.Apply(state, actor, target, plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost;
                    if (target != null && target.IsAlive) target.MarkExpiresRound = state.Round + 1;
                    break;
                }
                case CardKind.StarBurst:
                {
                    var target = state.FindUnit(request.TargetId);
                    var hasMark = target != null && target.MarkExpiresRound > 0;
                    result.DamageSummary = DamageRules.Apply(state, actor, target, plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost;
                    if (target != null && hasMark) target.MarkExpiresRound = 0;
                    break;
                }
                case CardKind.Inspire:
                    state.FindUnit(request.TargetId).InspireExpiresRound = state.Round + 1;
                    break;
            }
        }

        private static void MoveAlong(UnitState actor, System.Collections.Generic.List<GridPos> path)
        { if (path != null && path.Count > 0) actor.Position = path[path.Count - 1]; }

        private static bool TryPush(BattleState state, UnitState actor, UnitState target)
        {
            var dx = target.Position.X - actor.Position.X; var dy = target.Position.Y - actor.Position.Y;
            if (System.Math.Abs(dx) + System.Math.Abs(dy) != 1) return false;
            var destination = new GridPos(target.Position.X + dx, target.Position.Y + dy);
            if (!destination.IsInside(state.Width, state.Height) || state.Obstacles.Contains(destination) || state.UnitAt(destination) != null) return false;
            target.Position = destination; return true;
        }

        public static void RefreshOutcome(BattleState state)
        {
            if (state == null || state.Phase == BattlePhase.Deployment || state.Phase == BattlePhase.Victory || state.Phase == BattlePhase.Defeat) return;
            var playerAlive = false; var enemyAlive = false;
            foreach (var unit in state.Units) if (unit.IsAlive) { if (unit.Team == Team.Player) playerAlive = true; else enemyAlive = true; }
            if (!enemyAlive) state.Phase = BattlePhase.Victory;
            else if (!playerAlive) state.Phase = BattlePhase.Defeat;
        }

        private static ActionResult Fail(string reason) { return new ActionResult { Success = false, Reason = reason }; }
    }
}
