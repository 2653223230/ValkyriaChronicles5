using System;
using System.Collections.Generic;
using NUnit.Framework;
using VC5PvE;

namespace VC5PvE.Tests
{
    public class RulesAcceptanceTests
    {
        [Test]
        public void CreateBuildsFixedRosterBoardAndSixCardOpeningHand()
        {
            var state = BattleFactory.Create(17);
            Assert.AreEqual(8, state.Width);
            Assert.AreEqual(8, state.Height);
            Assert.AreEqual(6, state.Hand.Count);
            Assert.AreEqual(11, state.DrawPile.Count);
            Assert.AreEqual(17, state.Hand.Count + state.DrawPile.Count + state.DiscardPile.Count);
            Assert.AreEqual(new GridPos(2, 6), state.FindUnit("warrior").Position);
            Assert.AreEqual(new GridPos(3, 7), state.FindUnit("mage").Position);
            Assert.AreEqual(new GridPos(1, 7), state.FindUnit("support").Position);
            Assert.AreEqual(6, state.FindUnit("guard-a").Hp);
            Assert.AreEqual(10, state.FindUnit("shooter").Hp);
            Assert.IsTrue(state.Obstacles.Contains(new GridPos(2, 2)));
        }

        [Test]
        public void SeededRandomOpeningIsReproducibleAndBothDeckModesConserveAllSeventeenCards()
        {
            var fixedState = BattleFactory.Create(24, true);
            var randomState = BattleFactory.Create(24, false);
            var randomAgain = BattleFactory.Create(24, false);
            Assert.AreEqual(6, fixedState.Hand.Count);
            Assert.AreEqual(11, fixedState.DrawPile.Count);
            Assert.AreEqual(17, UniqueIds(fixedState).Count);
            Assert.AreEqual(6, randomState.Hand.Count);
            Assert.AreEqual(11, randomState.DrawPile.Count);
            Assert.AreEqual(17, UniqueIds(randomState).Count);
            for (var i = 0; i < randomState.Hand.Count; i++) Assert.AreEqual(randomState.Hand[i].Id, randomAgain.Hand[i].Id);
            var unseeded = BattleFactory.Create(false);
            Assert.AreEqual(17, UniqueIds(unseeded).Count);
        }

        [Test]
        public void DeployCanSwapOnlyWithinTheSouthDeploymentArea()
        {
            var state = BattleFactory.Create(1);
            Assert.IsTrue(BattleFactory.TryDeploy(state, "warrior", new GridPos(3, 7)));
            Assert.AreEqual(new GridPos(3, 7), state.FindUnit("warrior").Position);
            Assert.AreEqual(new GridPos(2, 6), state.FindUnit("mage").Position);
            Assert.IsFalse(BattleFactory.TryDeploy(state, "warrior", new GridPos(4, 7)));
            Assert.IsFalse(BattleFactory.TryDeploy(state, "warrior", new GridPos(2, 2)));
        }

        [Test]
        public void BaseMovementCostsOneApAndUsesRoleStepLimit()
        {
            var state = BattleFactory.Create(2);
            state.Phase = BattlePhase.Player;
            var warrior = state.FindUnit("warrior");
            var one = new ActionRequest { Kind = ActionKind.Move, ActorId = warrior.Id, Destination = new GridPos(2, 5) };
            Assert.IsTrue(BattleResolver.Apply(state, ActionPlanner.Plan(state, one)).Success);
            Assert.AreEqual(2, warrior.Ap);
            var two = new ActionRequest { Kind = ActionKind.Move, ActorId = warrior.Id, Destination = new GridPos(2, 3) };
            Assert.IsFalse(ActionPlanner.Plan(state, two).IsValid);
            var support = state.FindUnit("support");
            var supportMove = new ActionRequest { Kind = ActionKind.Move, ActorId = support.Id, Destination = new GridPos(1, 5) };
            Assert.IsTrue(BattleResolver.Apply(state, ActionPlanner.Plan(state, supportMove)).Success);
            Assert.AreEqual(1, support.Ap);
        }

        [Test]
        public void SightLineIsBlockedByObstacleInteriorButNotByCornerContact()
        {
            var state = BattleFactory.Create(21);
            state.Obstacles.Clear();
            state.Obstacles.Add(new GridPos(2, 2));
            Assert.IsFalse(GridRules.HasLineOfSight(state, new GridPos(1, 2), new GridPos(3, 2)));
            state.Obstacles.Clear();
            state.Obstacles.Add(new GridPos(2, 1));
            Assert.IsTrue(GridRules.HasLineOfSight(state, new GridPos(1, 1), new GridPos(3, 3)));
        }

        [Test]
        public void InvalidAndStaleActionsDoNotSpendApOrConsumeCards()
        {
            var state = BattleFactory.Create(3);
            state.Phase = BattlePhase.Player;
            var card = FindCard(state, CardKind.SparkMark);
            var warrior = state.FindUnit("warrior");
            var invalid = new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = card.Id, TargetId = "guard-a" };
            var plan = ActionPlanner.Plan(state, invalid);
            Assert.IsFalse(plan.IsValid);
            Assert.IsFalse(BattleResolver.Apply(state, plan).Success);
            Assert.AreEqual(3, warrior.Ap);
            Assert.IsTrue(ContainsCard(state.Hand, card.Id));
            var stale = ActionPlanner.Plan(state, new ActionRequest { Kind = ActionKind.Move, ActorId = warrior.Id, Destination = new GridPos(2, 5) });
            state.Revision++;
            Assert.IsFalse(BattleResolver.Apply(state, stale).Success);
            Assert.AreEqual(3, warrior.Ap);
        }

        [Test]
        public void CoverWithFullShieldIsNotAnActionAndKeepsTheCard()
        {
            var state = BattleFactory.Create(22);
            state.Phase = BattlePhase.Player;
            var warrior = state.FindUnit("warrior"); warrior.Shield = 2;
            var card = FindCard(state, CardKind.Cover);
            var request = new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = card.Id };
            var plan = ActionPlanner.Plan(state, request);
            Assert.IsFalse(plan.IsValid);
            Assert.IsFalse(BattleResolver.Apply(state, plan).Success);
            Assert.AreEqual(3, warrior.Ap);
            Assert.IsTrue(ContainsCard(state.Hand, card.Id));
        }

        [Test]
        public void BasicAttackIsRoleSpecificAndOnlyOncePerPlayerTurn()
        {
            var state = BattleFactory.Create(4);
            state.Phase = BattlePhase.Player;
            var mage = state.FindUnit("mage");
            var target = state.FindUnit("guard-a");
            mage.Position = new GridPos(3, 4);
            target.Position = new GridPos(3, 3);
            var request = new ActionRequest { Kind = ActionKind.Attack, ActorId = mage.Id, TargetId = target.Id };
            var plan = ActionPlanner.Plan(state, request);
            Assert.AreEqual(2, plan.PlannedDamage);
            Assert.AreEqual(2, plan.DamageSummary.HpLost);
            Assert.IsTrue(BattleResolver.Apply(state, plan).Success);
            Assert.AreEqual(4, target.Hp);
            Assert.IsFalse(ActionPlanner.Plan(state, request).IsValid);
        }

        [Test]
        public void SupportHealIsLimitedToTwoAndOncePerTurn()
        {
            var state = BattleFactory.Create(15);
            state.Phase = BattlePhase.Player;
            var support = state.FindUnit("support");
            var warrior = state.FindUnit("warrior");
            warrior.Hp = 8; warrior.Position = new GridPos(2, 7);
            support.Position = new GridPos(1, 7);
            var heal = new ActionRequest { Kind = ActionKind.Heal, ActorId = support.Id, TargetId = warrior.Id };
            var result = BattleResolver.Apply(state, ActionPlanner.Plan(state, heal));
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Healing);
            Assert.AreEqual(9, warrior.Hp);
            Assert.IsFalse(ActionPlanner.Plan(state, heal).IsValid);
        }

        [Test]
        public void GuardReductionHappensBeforeShieldAndOnlyOncePerTarget()
        {
            var state = BattleFactory.Create(16);
            state.Phase = BattlePhase.Enemy;
            var warrior = state.FindUnit("warrior"); warrior.Position = new GridPos(2, 4);
            var support = state.FindUnit("support"); support.Position = new GridPos(3, 4); support.Shield = 1;
            var first = state.FindUnit("guard-a"); first.Position = new GridPos(3, 3);
            var second = state.FindUnit("guard-b"); second.Position = new GridPos(4, 4);
            var firstAttack = new ActionRequest { Kind = ActionKind.Attack, ActorId = first.Id, TargetId = support.Id };
            var firstResult = BattleResolver.Apply(state, ActionPlanner.Plan(state, firstAttack));
            Assert.AreEqual(1, firstResult.DamageSummary.GuardReduction);
            Assert.AreEqual(1, firstResult.DamageSummary.ShieldAbsorbed);
            Assert.AreEqual(0, firstResult.DamageSummary.HpLost);
            var secondAttack = new ActionRequest { Kind = ActionKind.Attack, ActorId = second.Id, TargetId = support.Id };
            var secondResult = BattleResolver.Apply(state, ActionPlanner.Plan(state, secondAttack));
            Assert.AreEqual(0, secondResult.DamageSummary.GuardReduction);
            Assert.AreEqual(2, secondResult.DamageSummary.HpLost);
        }

        [Test]
        public void ChargeMovesDealsDamageThenPushesButDoesNotPushKilledTarget()
        {
            var state = BattleFactory.Create(17);
            state.Phase = BattlePhase.Player;
            var warrior = state.FindUnit("warrior"); warrior.Position = new GridPos(2, 5);
            var target = state.FindUnit("guard-a"); target.Position = new GridPos(3, 4); target.Shield = 1;
            var request = new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = MoveCardToHand(state, CardKind.Charge).Id, TargetId = target.Id, Destination = new GridPos(2, 4) };
            var plan = ActionPlanner.Plan(state, request);
            Assert.AreEqual(1, plan.PlannedDamage);
            Assert.AreEqual(1, plan.DamageSummary.ShieldAbsorbed);
            var result = BattleResolver.Apply(state, plan);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(5, target.Hp);
            Assert.AreEqual(new GridPos(4, 4), target.Position);

            var second = BattleFactory.Create(18);
            second.Phase = BattlePhase.Player;
            warrior = second.FindUnit("warrior"); warrior.Position = new GridPos(2, 5);
            target = second.FindUnit("guard-a"); target.Position = new GridPos(3, 4); target.Hp = 2;
            request = new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = MoveCardToHand(second, CardKind.Charge).Id, TargetId = target.Id, Destination = new GridPos(2, 4) };
            result = BattleResolver.Apply(second, ActionPlanner.Plan(second, request));
            Assert.AreEqual(0, target.Hp);
            Assert.AreEqual(new GridPos(3, 4), target.Position);
        }

        [Test]
        public void MarkAndInspireResolveBeforeGuardAndShield()
        {
            var state = BattleFactory.Create(5);
            state.Phase = BattlePhase.Player;
            var mage = state.FindUnit("mage");
            var guard = state.FindUnit("guard-a");
            var support = state.FindUnit("support");
            var warrior = state.FindUnit("warrior");
            mage.Position = new GridPos(3, 5); guard.Position = new GridPos(3, 4);
            support.Position = new GridPos(4, 5); warrior.Position = new GridPos(2, 4);
            var spark = FindCard(state, CardKind.SparkMark);
            var sparkRequest = new ActionRequest { Kind = ActionKind.Card, ActorId = mage.Id, CardId = spark.Id, TargetId = guard.Id };
            var sparkPlan = ActionPlanner.Plan(state, sparkRequest);
            Assert.AreEqual(1, sparkPlan.PlannedDamage);
            Assert.AreEqual(1, sparkPlan.DamageSummary.HpLost);
            var sparkResult = BattleResolver.Apply(state, sparkPlan);
            Assert.AreEqual(sparkPlan.PlannedDamage, sparkResult.DamageSummary.HpLost);
            Assert.AreEqual(5, guard.Hp); Assert.AreEqual(state.Round + 1, guard.MarkExpiresRound);
            ApplyCard(state, support.Id, CardKind.Inspire, mage.Id);
            guard.Shield = 1;
            var burst = FindCard(state, CardKind.StarBurst);
            var burstRequest = new ActionRequest { Kind = ActionKind.Card, ActorId = mage.Id, CardId = burst.Id, TargetId = guard.Id };
            var burstPlan = ActionPlanner.Plan(state, burstRequest);
            Assert.AreEqual(4, burstPlan.PlannedDamage);
            Assert.AreEqual(3, burstPlan.DamageSummary.BonusDamage);
            Assert.AreEqual(1, burstPlan.DamageSummary.ShieldAbsorbed);
            Assert.AreEqual(4, burstPlan.DamageSummary.HpLost);
            Assert.AreEqual(state.Round + 1, guard.MarkExpiresRound);
            Assert.AreEqual(state.Round + 1, mage.InspireExpiresRound);
            var burstResult = BattleResolver.Apply(state, burstPlan);
            Assert.AreEqual(burstPlan.PlannedDamage, burstResult.DamageSummary.HpLost);
            Assert.AreEqual(burstPlan.DamageSummary.BonusDamage, burstResult.DamageSummary.BonusDamage);
            Assert.AreEqual(1, guard.Hp); // marked attack 4 + inspire 1, shield absorbs 1
            Assert.AreEqual(0, guard.Shield);
            Assert.AreEqual(0, guard.MarkExpiresRound);
            Assert.AreEqual(0, mage.InspireExpiresRound);

            var shieldState = BattleFactory.Create(26);
            shieldState.Phase = BattlePhase.Player;
            var shieldMage = shieldState.FindUnit("mage"); shieldMage.Position = new GridPos(3, 5); shieldMage.InspireExpiresRound = 2;
            var shieldTarget = shieldState.FindUnit("guard-a"); shieldTarget.Position = new GridPos(3, 4); shieldTarget.MarkExpiresRound = 2; shieldTarget.Shield = 5;
            var fullAbsorb = new ActionRequest { Kind = ActionKind.Card, ActorId = shieldMage.Id, CardId = FindCard(shieldState, CardKind.StarBurst).Id, TargetId = shieldTarget.Id };
            var fullAbsorbPlan = ActionPlanner.Plan(shieldState, fullAbsorb);
            Assert.AreEqual(0, fullAbsorbPlan.PlannedDamage);
            Assert.AreEqual(5, fullAbsorbPlan.DamageSummary.ShieldAbsorbed);
            var fullAbsorbResult = BattleResolver.Apply(shieldState, fullAbsorbPlan);
            Assert.AreEqual(0, fullAbsorbResult.DamageSummary.HpLost);
            Assert.AreEqual(0, shieldTarget.Shield);
            Assert.AreEqual(0, shieldTarget.MarkExpiresRound);
            Assert.AreEqual(0, shieldMage.InspireExpiresRound);
            Assert.AreEqual(6, shieldTarget.Hp);
        }

        [Test]
        public void InspiredHeavyAttackPreviewIncludesShieldAndMatchesExecutionWithoutConsumingPreviewState()
        {
            var state = BattleFactory.Create(25);
            state.Phase = BattlePhase.Player;
            var mage = state.FindUnit("mage"); mage.Position = new GridPos(3, 5); mage.InspireExpiresRound = 2;
            var target = state.FindUnit("guard-a"); target.Position = new GridPos(3, 4); target.Shield = 3;
            var request = new ActionRequest { Kind = ActionKind.Card, ActorId = mage.Id, CardId = FindCard(state, CardKind.HeavyAttack).Id, TargetId = target.Id };

            var plan = ActionPlanner.Plan(state, request);
            Assert.IsTrue(plan.IsValid);
            Assert.AreEqual(1, plan.PlannedDamage);
            Assert.AreEqual(1, plan.DamageSummary.BonusDamage);
            Assert.AreEqual(3, plan.DamageSummary.ShieldAbsorbed);
            Assert.AreEqual(1, plan.DamageSummary.HpLost);
            Assert.AreEqual(2, mage.InspireExpiresRound);
            Assert.AreEqual(3, target.Shield);
            var result = BattleResolver.Apply(state, plan);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(plan.PlannedDamage, result.DamageSummary.HpLost);
            Assert.AreEqual(plan.DamageSummary.BonusDamage, result.DamageSummary.BonusDamage);
            Assert.AreEqual(plan.DamageSummary.ShieldAbsorbed, result.DamageSummary.ShieldAbsorbed);
            Assert.AreEqual(5, target.Hp);
            Assert.AreEqual(0, target.Shield);
            Assert.AreEqual(0, mage.InspireExpiresRound);

            var lowHpState = BattleFactory.Create(27);
            lowHpState.Phase = BattlePhase.Player;
            var lowHpMage = lowHpState.FindUnit("mage"); lowHpMage.Position = new GridPos(3, 5); lowHpMage.InspireExpiresRound = 2;
            var lowHpTarget = lowHpState.FindUnit("guard-a"); lowHpTarget.Position = new GridPos(3, 4); lowHpTarget.Hp = 1;
            var lowHpAttack = new ActionRequest { Kind = ActionKind.Card, ActorId = lowHpMage.Id, CardId = FindCard(lowHpState, CardKind.HeavyAttack).Id, TargetId = lowHpTarget.Id };
            var lowHpPlan = ActionPlanner.Plan(lowHpState, lowHpAttack);
            Assert.AreEqual(1, lowHpPlan.PlannedDamage);
            Assert.AreEqual(1, lowHpPlan.DamageSummary.HpLost);
            var lowHpResult = BattleResolver.Apply(lowHpState, lowHpPlan);
            Assert.AreEqual(1, lowHpResult.DamageSummary.HpLost);
            Assert.AreEqual(0, lowHpTarget.Hp);
        }

        [Test]
        public void DeckDrawExchangeAndDiscardRecycleWithoutDuplicatingCards()
        {
            var state = BattleFactory.Create(123);
            state.Phase = BattlePhase.Player;
            var outgoing = state.Hand[0].Id;
            Assert.IsTrue(DeckRules.TryExchange(state, outgoing));
            Assert.AreEqual(6, state.Hand.Count);
            Assert.AreEqual(17, state.Hand.Count + state.DrawPile.Count + state.DiscardPile.Count);
            Assert.IsFalse(DeckRules.TryExchange(state, outgoing));
            state.DiscardPile.AddRange(state.Hand);
            state.DiscardPile.AddRange(state.DrawPile);
            state.Hand.Clear(); state.DrawPile.Clear();
            Assert.AreEqual(6, DeckRules.DrawToLimit(state, 6));
            Assert.AreEqual(6, state.Hand.Count);
            Assert.AreEqual(17, UniqueIds(state).Count);
        }

        [Test]
        public void PlayerTurnCleansShieldAndExpiresMarkAndInspireAtNextPlayerEnd()
        {
            var state = BattleFactory.Create(6);
            state.Phase = BattlePhase.Player;
            var target = state.FindUnit("guard-a");
            var mage = state.FindUnit("mage");
            var shielded = state.FindUnit("warrior");
            shielded.Shield = 2; target.MarkExpiresRound = 2; mage.InspireExpiresRound = 2;
            state.Round = 1;
            TurnRules.EndPlayerTurn(state);
            Assert.AreEqual(BattlePhase.Enemy, state.Phase);
            TurnRules.BeginPlayerTurn(state);
            Assert.AreEqual(2, state.Round);
            Assert.AreEqual(0, shielded.Shield);
            TurnRules.EndPlayerTurn(state);
            Assert.AreEqual(0, target.MarkExpiresRound);
            Assert.AreEqual(0, mage.InspireExpiresRound);
        }

        [Test]
        public void BeginPlayerTurnCannotSkipDeploymentOrEnemyActions()
        {
            var state = BattleFactory.Create(23);
            TurnRules.BeginPlayerTurn(state);
            Assert.AreEqual(BattlePhase.Deployment, state.Phase);
            Assert.AreEqual(0, state.Round);
            TurnRules.StartBattle(state);
            Assert.AreEqual(BattlePhase.Player, state.Phase);
            Assert.AreEqual(1, state.Round);
        }

        [Test]
        public void EnemyPreviewAndExecutionUseTheSameSequentialPlan()
        {
            var preview = BattleFactory.Create(9);
            preview.Phase = BattlePhase.Player;
            preview.FindUnit("warrior").Position = new GridPos(3, 4);
            preview.FindUnit("support").Position = new GridPos(4, 4);
            var executable = preview.Clone();
            TurnRules.EndPlayerTurn(preview);
            TurnRules.EndPlayerTurn(executable);
            var plan = EnemyPlanner.PlanTurn(preview);
            Assert.IsTrue(plan.Actions.Count > 0);
            foreach (var action in plan.Actions)
            {
                Assert.IsTrue(action.IsValid);
                Assert.AreEqual(preview.Revision, action.Revision);
                var result = BattleResolver.Apply(preview, action);
                Assert.IsTrue(result.Success);
            }
            var authoritative = EnemyPlanner.ExecuteTurn(executable);
            Assert.AreEqual(plan.Actions.Count, authoritative.Count);
            Assert.AreEqual(StateDigest(preview), StateDigest(executable));
        }

        [Test]
        public void EliminatingEitherTeamEndsBattleImmediately()
        {
            var state = BattleFactory.Create(10);
            state.Phase = BattlePhase.Player;
            foreach (var enemy in state.Units) if (enemy.Team == Team.Enemy) enemy.Hp = 0;
            BattleResolver.RefreshOutcome(state);
            Assert.AreEqual(BattlePhase.Victory, state.Phase);
            var second = BattleFactory.Create(11);
            second.Phase = BattlePhase.Player;
            foreach (var ally in second.Units) if (ally.Team == Team.Player) ally.Hp = 0;
            BattleResolver.RefreshOutcome(second);
            Assert.AreEqual(BattlePhase.Defeat, second.Phase);
        }

        private static CardInstance FindCard(BattleState state, CardKind kind)
        { foreach (var card in state.Hand) if (card.Kind == kind) return card; throw new Exception("missing card " + kind); }
        private static CardInstance MoveCardToHand(BattleState state, CardKind kind)
        { var i = state.DrawPile.FindIndex(c => c.Kind == kind); var card = state.DrawPile[i]; state.DrawPile.RemoveAt(i); state.Hand.Add(card); return card; }
        private static bool ContainsCard(List<CardInstance> cards, string id)
        { foreach (var card in cards) if (card.Id == id) return true; return false; }
        private static void ApplyCard(BattleState state, string actor, CardKind kind, string target)
        { var card = FindCard(state, kind); var r = new ActionRequest { Kind = ActionKind.Card, ActorId = actor, CardId = card.Id, TargetId = target }; var p = ActionPlanner.Plan(state, r); Assert.IsTrue(p.IsValid, p.Reason); Assert.IsTrue(BattleResolver.Apply(state, p).Success); }
        private static HashSet<string> UniqueIds(BattleState state)
        { var ids = new HashSet<string>(); foreach (var c in state.Hand) ids.Add(c.Id); foreach (var c in state.DrawPile) ids.Add(c.Id); foreach (var c in state.DiscardPile) ids.Add(c.Id); return ids; }
        private static string StateDigest(BattleState state)
        { var parts = new List<string>(); foreach (var u in state.Units) parts.Add(u.Id + ":" + u.Hp + ":" + u.Position + ":" + u.Shield); return string.Join("|", parts.ToArray()); }
    }
}
