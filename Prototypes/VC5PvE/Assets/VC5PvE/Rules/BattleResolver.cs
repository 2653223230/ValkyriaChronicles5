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
                case ActionKind.BossAoe:
                    ApplyBossAoe(state, actor, plan, result);
                    break;
            }
            state.Revision++;
            RefreshOutcome(state);
            if (state.Phase == BattlePhase.Victory || state.Phase == BattlePhase.Defeat) { state.PendingRecoveryActorId = null; state.PendingDrawCount = 0; }
            return result;
        }

        private static void ApplyCard(BattleState state, UnitState actor, ActionPlan plan, ActionResult result)
        {
            var request = plan.Request;
            var index = state.Hand.FindIndex(card => card.Id == request.CardId);
            if (index < 0) { result.Success = false; result.Reason = "手牌不存在"; return; }
            var card = state.Hand[index]; state.Hand.RemoveAt(index); state.DiscardPile.Add(card);
            var originalSummary = result.Summary;
            var wasMarked = false;
            if (!string.IsNullOrEmpty(request.TargetId))
            {
                var markTarget = state.FindUnit(request.TargetId);
                wasMarked = markTarget != null && !markTarget.IsFacility && markTarget.MarkExpiresRound > 0 &&
                    (card.Kind == CardKind.StarBurst || card.Kind == CardKind.RelayStrike);
            }
            GrowthRules.ConsumeDiscount(state, actor, card);
            var wasAttackCard = GrowthRules.IsAttackCard(card.Kind);
            switch (card.Kind)
            {
                case CardKind.Advance:
                    MoveAlong(actor, plan.Path); actor.Shield = System.Math.Max(actor.Shield, card.Upgraded ? 2 : 1); break;
                case CardKind.HeavyAttack:
                    result.DamageSummary = DamageRules.Apply(state, actor, state.FindUnit(request.TargetId), plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost; break;
                case CardKind.Cover:
                    actor.Shield = System.Math.Max(actor.Shield, card.Upgraded ? 3 : 2); break;
                case CardKind.Charge:
                {
                    MoveAlong(actor, plan.Path);
                    var target = state.FindUnit(request.TargetId);
                    result.DamageSummary = DamageRules.Apply(state, actor, target, plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost;
                    if (target != null && target.IsAlive && !target.IsFacility) TryPush(state, actor, target);
                    break;
                }
                case CardKind.SparkMark:
                {
                    var target = state.FindUnit(request.TargetId);
                    result.DamageSummary = DamageRules.Apply(state, actor, target, plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost;
                    if (target != null && target.IsAlive && !target.IsFacility)
                    {
                        target.MarkExpiresRound = state.Round + 1;
                        if (card.Upgraded && state.Hand.Count < 6) DeckRules.DrawToLimit(state, state.Hand.Count + 1);
                    }
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
                {
                    var target = state.FindUnit(request.TargetId);
                    target.InspireExpiresRound = System.Math.Max(target.InspireExpiresRound, state.Round + 1);
                    target.InspireBonus = System.Math.Max(System.Math.Max(1, target.InspireBonus), card.Upgraded ? 2 : 1);
                    break;
                }
                case CardKind.RelayStrike:
                {
                    var target = state.FindUnit(request.TargetId);
                    if (target != null && target.MarkExpiresRound > 0 && !target.IsFacility) target.MarkExpiresRound = 0;
                    result.DamageSummary = DamageRules.Apply(state, actor, target, plan.DamageBase, plan.DamageBonus); result.Damage = result.DamageSummary.HpLost;
                    break;
                }
                case CardKind.AllyCover:
                    state.FindUnit(request.TargetId).Shield = System.Math.Max(state.FindUnit(request.TargetId).Shield, card.Upgraded ? 3 : 2);
                    break;
                case CardKind.RecoverTactics:
                {
                    var targetIndex = state.DiscardPile.FindIndex(c => c.Id == request.RecoveryCardId && GrowthRules.IsNeutralAttackCard(c.Kind));
                    if (targetIndex >= 0)
                    {
                        var recovered = state.DiscardPile[targetIndex]; state.DiscardPile.RemoveAt(targetIndex); state.Hand.Add(recovered);
                    }
                    if (card.Upgraded) actor.Shield = System.Math.Max(actor.Shield, 2);
                    break;
                }
            }
            if (wasMarked && GrowthRules.ConsumeMarkFlow(state))
            {
                int beforeDraw = state.Hand.Count;
                if (state.Hand.Count < 6 && HasCombatEnemy(state)) DeckRules.DrawToLimit(state, state.Hand.Count + 1);
                if (state.Hand.Count > beforeDraw) result.Summary += "；印记回流：抽1张";
            }
            if (wasAttackCard && !actor.FlankingStrikeUsedThisTurn && GrowthRules.HasTalent(actor, TalentKind.FlankingStrike))
            {
                actor.FlankingStrikeUsedThisTurn = true;
                if (plan.DamageBonus > 0 && GrowthRules.FlankingBonusApplied(plan)) result.Summary += "；并肩强袭：伤害+1";
            }
            if (card.Kind == CardKind.Inspire || card.Kind == CardKind.AllyCover)
            {
                var canTriggerFlow = GrowthRules.HasTalent(actor, TalentKind.SupportFlow) && !actor.SupportFlowUsedThisTurn;
                var handBeforeSupport = state.Hand.Count;
                if (GrowthRules.ApplySupportTalents(state, actor))
                {
                    result.Summary += "；战术回收：等待选择";
                    if (canTriggerFlow && state.PendingDrawCount > 0) result.Summary += "；支援续牌：回收后尝试抽1张";
                }
                else if (canTriggerFlow && state.Hand.Count > handBeforeSupport) result.Summary += "；支援续牌：抽1张";
            }
            if (plan.Cost < Definitions.Card(card).Cost)
                result.Summary += GrowthRules.HasTalent(actor, TalentKind.CounterReady) ? "；反攻准备：费用-1" : "；中立专精：费用-1";
        }

        private static void ApplyBossAoe(BattleState state, UnitState boss, ActionPlan plan, ActionResult result)
        {
            if (state.BossTelegraph == null || boss == null || !boss.IsAlive) return;
            plan.AreaDamageSummaries.Clear();
            var center = state.BossTelegraph.Value;
            foreach (var target in state.Units)
            {
                if (!target.IsAlive || target.Team != Team.Player || System.Math.Abs(target.Position.X - center.X) > 1 || System.Math.Abs(target.Position.Y - center.Y) > 1) continue;
                var damage = DamageRules.Apply(state, boss, target, 2, 0);
                result.AreaDamageSummaries.Add(damage);
                plan.AreaDamageSummaries.Add(damage);
                result.Damage += damage.HpLost;
            }
            state.BossTelegraph = null;
            result.Summary = "钟鸣震荡：执行已预告区域";
        }

        private static bool HasCombatEnemy(BattleState state)
        {
            foreach (var unit in state.Units) if (unit.Team == Team.Enemy && !unit.IsFacility && unit.IsAlive) return true;
            return false;
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
            var bossAlive = false;
            foreach (var unit in state.Units) if (unit.IsAlive && !unit.IsFacility) { if (unit.Team == Team.Player) playerAlive = true; else enemyAlive = true; if (unit.Role == UnitRole.Boss) bossAlive = true; }
            if (!bossAlive) state.BossTelegraph = null;
            if (!enemyAlive) state.Phase = BattlePhase.Victory;
            else if (!playerAlive) state.Phase = BattlePhase.Defeat;
        }

        private static ActionResult Fail(string reason) { return new ActionResult { Success = false, Reason = reason }; }
    }
}
