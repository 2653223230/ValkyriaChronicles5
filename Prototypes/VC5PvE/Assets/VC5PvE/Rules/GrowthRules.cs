using System;
using System.Collections.Generic;

namespace VC5PvE
{
    public static class GrowthRules
    {
        public static bool HasTalent(UnitState unit, TalentKind talent)
        { return unit != null && unit.Talents != null && unit.Talents.Contains(talent); }

        public static bool IsAttackCard(CardKind kind)
        {
            return kind == CardKind.HeavyAttack || kind == CardKind.Charge || kind == CardKind.SparkMark ||
                   kind == CardKind.StarBurst || kind == CardKind.RelayStrike;
        }

        public static bool IsNeutralAttackCard(CardKind kind)
        { return kind == CardKind.HeavyAttack || kind == CardKind.RelayStrike; }

        public static int FlankingBonus(BattleState state, UnitState actor, UnitState target)
        {
            if (state == null || actor == null || target == null || actor.Team != Team.Player || !HasTalent(actor, TalentKind.FlankingStrike) || actor.FlankingStrikeUsedThisTurn) return 0;
            foreach (var unit in state.Units)
                if (unit.IsAlive && unit.Team == actor.Team && unit.Id != actor.Id && unit.Position.ManhattanDistance(target.Position) == 1) return 1;
            return 0;
        }

        internal static bool FlankingBonusApplied(ActionPlan plan) { return plan != null && plan.FlankingBonus; }

        public static bool ConsumeMarkFlow(BattleState state)
        {
            if (state == null) return false;
            foreach (var unit in state.Units)
                if (unit.IsAlive && unit.Team == Team.Player && HasTalent(unit, TalentKind.MarkFlow) && !unit.MarkFlowUsedThisTurn)
                { unit.MarkFlowUsedThisTurn = true; return true; }
            return false;
        }

        public static int CardCost(BattleState state, UnitState actor, CardInstance card)
        {
            if (card == null) return 0;
            var cost = Definitions.Card(card).Cost;
            if (state == null || actor == null || actor.Team != Team.Player || state.Phase != BattlePhase.Player) return cost;
            var eligible = IsAttackCard(card.Kind) && Definitions.Card(card).Cost >= 2 && HasTalent(actor, TalentKind.CounterReady) && actor.CounterReadyPrepared && !actor.CounterReadyUsedThisTurn;
            eligible |= IsNeutralAttackCard(card.Kind) && Definitions.Card(card).Cost >= 2 && HasTalent(actor, TalentKind.NeutralMastery) && !actor.CounterReadyUsedThisTurn;
            return eligible ? Math.Max(1, cost - 1) : cost;
        }

        public static void ConsumeDiscount(BattleState state, UnitState actor, CardInstance card)
        {
            if (state == null || actor == null || card == null) return;
            var cost = Definitions.Card(card).Cost;
            var counter = IsAttackCard(card.Kind) && cost >= 2 && HasTalent(actor, TalentKind.CounterReady) && actor.CounterReadyPrepared && !actor.CounterReadyUsedThisTurn;
            var mastery = IsNeutralAttackCard(card.Kind) && cost >= 2 && HasTalent(actor, TalentKind.NeutralMastery) && !actor.CounterReadyUsedThisTurn;
            if (counter || mastery)
            {
                actor.CounterReadyUsedThisTurn = true;
                if (counter) actor.CounterReadyPrepared = false;
            }
        }

        public static bool ResolveRecovery(BattleState state, string cardId)
        {
            if (state == null || string.IsNullOrEmpty(state.PendingRecoveryActorId)) return false;
            var actor = state.FindUnit(state.PendingRecoveryActorId);
            // The pending id means this talent already triggered. Its used flag must stay set
            // to prevent another trigger this turn, but it must not block this pending choice.
            if (actor == null || !actor.IsAlive || !HasTalent(actor, TalentKind.TacticalRecall)) { state.PendingRecoveryActorId = null; return false; }
            if (!string.IsNullOrEmpty(cardId))
            {
                var index = state.DiscardPile.FindIndex(c => c.Id == cardId && IsNeutralAttackCard(c.Kind));
                if (index < 0 || state.Hand.Count >= 6) return false;
                var card = state.DiscardPile[index];
                state.DiscardPile.RemoveAt(index);
                state.Hand.Add(card);
            }
            actor.TacticalRecallUsedThisTurn = true;
            state.PendingRecoveryActorId = null;
            state.Revision++;
            if (state.PendingDrawCount > 0)
            {
                var drawCount = state.PendingDrawCount;
                state.PendingDrawCount = 0;
                DeckRules.DrawToLimit(state, Math.Min(6, state.Hand.Count + drawCount));
            }
            BattleResolver.RefreshOutcome(state);
            return true;
        }

        public static List<CardInstance> RecoveryCandidates(BattleState state)
        {
            var result = new List<CardInstance>();
            // A RecoverTactics card leaves the hand before its effect resolves, so a full
            // hand can still legally choose a discard card when playing that card.
            if (state == null) return result;
            foreach (var card in state.DiscardPile) if (IsNeutralAttackCard(card.Kind)) result.Add(card);
            return result;
        }

        internal static void TryTalentRecovery(BattleState state, UnitState actor)
        {
            if (state == null || actor == null || !actor.IsAlive || !HasTalent(actor, TalentKind.TacticalRecall) || actor.TacticalRecallUsedThisTurn) return;
            if (!HasRecoverableAttack(state) || state.Hand.Count >= 6) { actor.TacticalRecallUsedThisTurn = true; return; }
            actor.TacticalRecallUsedThisTurn = true;
            state.PendingRecoveryActorId = actor.Id;
        }

        internal static bool HasRecoverableAttack(BattleState state)
        {
            if (state == null || state.Hand.Count >= 6) return false;
            foreach (var card in state.DiscardPile) if (IsNeutralAttackCard(card.Kind)) return true;
            return false;
        }

        internal static bool ApplySupportTalents(BattleState state, UnitState actor)
        {
            if (state == null || actor == null || !actor.IsAlive) return false;
            var recall = HasTalent(actor, TalentKind.TacticalRecall) && !actor.TacticalRecallUsedThisTurn;
            var flow = HasTalent(actor, TalentKind.SupportFlow) && !actor.SupportFlowUsedThisTurn;
            if (!recall && !flow) return false;
            state.PendingDrawCount = flow ? 1 : 0;
            actor.SupportFlowUsedThisTurn = true;
            if (recall && HasRecoverableAttack(state) && state.Hand.Count < 6)
            {
                actor.TacticalRecallUsedThisTurn = true;
                state.PendingRecoveryActorId = actor.Id;
                return true;
            }
            if (recall) actor.TacticalRecallUsedThisTurn = true;
            if (state.PendingDrawCount > 0)
            {
                DeckRules.DrawToLimit(state, Math.Min(6, state.Hand.Count + state.PendingDrawCount));
                state.PendingDrawCount = 0;
            }
            return false;
        }
    }
}
