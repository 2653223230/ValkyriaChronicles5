namespace VC5PvE
{
    public static class TurnRules
    {
        public static void StartBattle(BattleState state)
        {
            if (state == null || state.Phase != BattlePhase.Deployment) return;
            state.Round = 1;
            state.Phase = BattlePhase.Player;
            BeginPlayerTurn(state, false);
            BattleResolver.RefreshOutcome(state);
        }

        public static void BeginPlayerTurn(BattleState state)
        { BeginPlayerTurn(state, true); }

        internal static void BeginPlayerTurn(BattleState state, bool advanceRound)
        {
            if (state == null || state.Phase == BattlePhase.Victory || state.Phase == BattlePhase.Defeat) return;
            if (advanceRound && state.Phase != BattlePhase.Enemy) return;
            if (advanceRound) state.Round++;
            state.Phase = BattlePhase.Player;
            state.ExchangedThisTurn = false;
            state.FirstActionMade = false;
            foreach (var unit in state.Units)
            {
                if (unit.Team == Team.Player && unit.IsAlive)
                {
                    unit.Shield = 0;
                    unit.Ap = unit.MaxAp;
                    unit.AttackUsed = false;
                    unit.HealUsed = false;
                    unit.CounterReadyUsedThisTurn = false;
                    unit.FlankingStrikeUsedThisTurn = false;
                    unit.MarkFlowUsedThisTurn = false;
                    unit.SupportFlowUsedThisTurn = false;
                    unit.TacticalRecallUsedThisTurn = false;
                }
            }
            DeckRules.DrawToLimit(state, 6);
            state.Revision++;
            BattleResolver.RefreshOutcome(state);
        }

        public static void EndPlayerTurn(BattleState state)
        {
            if (state == null || state.Phase != BattlePhase.Player || !string.IsNullOrEmpty(state.PendingRecoveryActorId)) return;
            foreach (var unit in state.Units)
            {
                if (unit.MarkExpiresRound > 0 && unit.MarkExpiresRound <= state.Round) unit.MarkExpiresRound = 0;
                if (unit.InspireExpiresRound > 0 && unit.InspireExpiresRound <= state.Round) { unit.InspireExpiresRound = 0; unit.InspireBonus = 1; }
                if (unit.Team == Team.Enemy) { unit.EnemyMoveUsed = false; unit.EnemyAttackUsed = false; }
                if (unit.Team == Team.Player)
                {
                    unit.CounterReadyPrepared = false;
                    unit.CounterReadyTriggeredThisEnemyTurn = false;
                }
            }
            state.GuardReducedTargetsThisEnemyTurn.Clear();
            state.Phase = BattlePhase.Enemy;
            state.Revision++;
        }
    }
}
