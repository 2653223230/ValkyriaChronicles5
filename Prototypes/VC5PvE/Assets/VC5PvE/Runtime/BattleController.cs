using System;
using System.Collections;
using UnityEngine;

namespace VC5PvE
{
    public sealed class BattleController : MonoBehaviour
    {
        public BattleState State { get; private set; }
        public bool IsBusy { get; private set; }
        public ActionPlan CurrentPreview { get; private set; }
        public BoardView Board;
        public Camera ViewCamera;
        public int BattleSeed = 20260930;
        public event Action Changed;
        public event Action<string> Message;
        private int generation;

        public void CreateBattle()
        {
            generation++;
            StopAllCoroutines();
            State = BattleFactory.Create(BattleSeed, true);
            IsBusy = false;
            CurrentPreview = null;
            if (Board != null) Board.Initialize(State, ViewCamera);
            Notify();
        }

        public void BeginBattle()
        {
            if (State == null || IsBusy || State.Phase != BattlePhase.Deployment) return;
            TurnRules.StartBattle(State);
            Notify();
        }

        public bool Deploy(string unitId, GridPos position)
        {
            if (IsBusy || !BattleFactory.TryDeploy(State, unitId, position)) return false;
            Notify(); return true;
        }

        public ActionPlan Preview(ActionRequest request)
        {
            if (IsBusy || State == null || State.Phase != BattlePhase.Player)
                return new ActionPlan { IsValid = false, Reason = "当前不能执行玩家行动" };
            CurrentPreview = ActionPlanner.Plan(State, request);
            return CurrentPreview;
        }

        public void CancelPreview() { CurrentPreview = null; }

        public bool Exchange(string cardId)
        {
            if (IsBusy) return false;
            string reason;
            if (!DeckRules.TryExchange(State, cardId, out reason)) { Tell(reason); return false; }
            CurrentPreview = null;
            Notify(); Tell("已换牌；本轮换牌机会已使用"); return true;
        }

        public EnemyTurnPlan Intent()
        {
            if (State == null || State.Phase != BattlePhase.Player) return new EnemyTurnPlan();
            var clone = State.Clone();
            TurnRules.EndPlayerTurn(clone);
            return EnemyPlanner.PlanTurn(clone);
        }

        public IEnumerator Confirm(ActionPlan plan)
        {
            if (IsBusy || plan == null || !plan.IsValid) yield break;
            if (State.Phase != BattlePhase.Player) yield break;
            if (plan.Revision != State.Revision)
            {
                CurrentPreview = ActionPlanner.Plan(State, plan.Request);
                Notify(); Tell("战场已变化，请重新预览后确认"); yield break;
            }
            int session = generation;
            IsBusy = true;
            var result = BattleResolver.Apply(State, plan);
            CurrentPreview = null;
            Notify(false);
            if (result.Success && Board != null) yield return Board.Animate(plan, result);
            if (session != generation) yield break;
            IsBusy = false;
            Notify();
            Tell(result.Success ? result.Summary : result.Reason);
            if (result.Success && State.Phase == BattlePhase.Player && AllPlayerApSpent())
                yield return EndTurn();
        }

        public IEnumerator EndTurn()
        {
            if (State == null || IsBusy || State.Phase != BattlePhase.Player) yield break;
            int session = generation;
            IsBusy = true; CurrentPreview = null;
            TurnRules.EndPlayerTurn(State);
            var intent = EnemyPlanner.PlanTurn(State);
            Notify();
            yield return new WaitForSeconds(0.35f);
            foreach (var plan in intent.Actions)
            {
                if (session != generation || State.Phase != BattlePhase.Enemy) yield break;
                var result = BattleResolver.Apply(State, plan);
                if (!result.Success) { Tell(result.Reason); break; }
                if (Board != null) yield return Board.Animate(plan, result);
                else yield return new WaitForSeconds(0.1f);
                if (session != generation) yield break;
                Notify();
                if (State.Phase == BattlePhase.Victory || State.Phase == BattlePhase.Defeat) break;
            }
            if (session != generation) yield break;
            if (State.Phase == BattlePhase.Enemy) TurnRules.BeginPlayerTurn(State);
            IsBusy = false; Notify();
        }

        private void Tell(string message) { if (!string.IsNullOrEmpty(message)) Message?.Invoke(message); }
        private bool AllPlayerApSpent()
        {
            foreach (var unit in State.Units)
                if (unit.Team == Team.Player && unit.IsAlive && unit.Ap > 0) return false;
            return true;
        }
        private void Notify(bool refreshBoard = true)
        {
            if (refreshBoard && Board != null && State != null) Board.Refresh(State);
            Changed?.Invoke();
        }
    }
}
