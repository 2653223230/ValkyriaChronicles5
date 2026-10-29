using System.Collections.Generic;
using NUnit.Framework;

namespace VC5PvE.Tests
{
    public class GrowthRulesTests
    {
        [Test]
        public void CloneCopiesGrowthTelegraphRecoveryAndTalentStateIndependently()
        {
            var state = BattleFactory.Create(4);
            state.LevelIndex = 3;
            state.BossVulnerableRound = 2;
            state.BossTelegraph = new GridPos(4, 2);
            state.PendingRecoveryActorId = "support";
            state.PendingDrawCount = 1;
            state.FindUnit("warrior").Talents.Add(TalentKind.CounterReady);
            var card = state.Hand[0]; card.Upgraded = true;
            var copy = state.Clone();
            copy.FindUnit("warrior").Talents.Clear();
            copy.Hand[0].Upgraded = false;
            copy.PendingRecoveryActorId = null;
            copy.PendingDrawCount = 0;
            Assert.AreEqual(3, copy.LevelIndex);
            Assert.AreEqual(2, copy.BossVulnerableRound);
            Assert.AreEqual(new GridPos(4, 2), copy.BossTelegraph.Value);
            Assert.AreEqual("support", state.PendingRecoveryActorId);
            Assert.AreEqual(1, state.PendingDrawCount);
            Assert.AreEqual(1, state.FindUnit("warrior").Talents.Count);
            Assert.IsTrue(state.Hand[0].Upgraded);
        }

        [Test]
        public void UpgradedCardDefinitionsMatchTheInstanceAndCostsApplyOncePerTurn()
        {
            var state = BattleFactory.Create(8);
            state.Phase = BattlePhase.Player;
            var warrior = state.FindUnit("warrior");
            warrior.Talents.Add(TalentKind.CounterReady);
            warrior.CounterReadyPrepared = true;
            var heavy = new CardInstance { Id = "heavy-up", Kind = CardKind.HeavyAttack, Upgraded = true };
            state.Hand.Add(heavy);
            Assert.AreEqual(1, GrowthRules.CardCost(state, warrior, heavy));
            Assert.IsTrue(Definitions.Card(heavy).Description.Contains("4伤害"));
            var request = new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = heavy.Id, TargetId = "guard-a" };
            state.FindUnit("guard-a").Position = new GridPos(2, 4);
            warrior.Position = new GridPos(2, 5);
            var plan = ActionPlanner.Plan(state, request);
            Assert.IsTrue(plan.IsValid, plan.Reason);
            Assert.AreEqual(1, plan.Cost);
            Assert.AreEqual(4, plan.DamageBase);
            Assert.IsTrue(BattleResolver.Apply(state, plan).Success);
            Assert.IsFalse(warrior.CounterReadyPrepared);
        }

        [Test]
        public void NewCardsAndUpgradesUseSharedLegalityAndPreview()
        {
            var state = BattleFactory.Create(9);
            state.Phase = BattlePhase.Player;
            var warrior = state.FindUnit("warrior"); warrior.Position = new GridPos(2, 5);
            var enemy = state.FindUnit("guard-a"); enemy.Position = new GridPos(2, 4); enemy.MarkExpiresRound = 2;
            var relay = new CardInstance { Id = "relay", Kind = CardKind.RelayStrike, Upgraded = true };
            state.Hand.Add(relay);
            var request = new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = relay.Id, TargetId = enemy.Id };
            var plan = ActionPlanner.Plan(state, request);
            Assert.IsTrue(plan.IsValid, plan.Reason);
            Assert.AreEqual(4, plan.DamageBase + plan.DamageBonus);
            Assert.IsTrue(BattleResolver.Apply(state, plan).Success);
            Assert.AreEqual(0, enemy.MarkExpiresRound);
        }

        [Test]
        public void TacticalRecallSuspendsActionsUntilExplicitChoiceAndDoesNotRefreshDiscount()
        {
            var state = BattleFactory.Create(12);
            state.Phase = BattlePhase.Player;
            var support = state.FindUnit("support"); support.Position = new GridPos(2, 6);
            support.Talents.Add(TalentKind.TacticalRecall);
            support.Talents.Add(TalentKind.SupportFlow);
            var warrior = state.FindUnit("warrior"); warrior.Position = new GridPos(2, 5);
            var heavy = new CardInstance { Id = "heavy-target", Kind = CardKind.HeavyAttack, Upgraded = true };
            var cover = new CardInstance { Id = "ally-cover", Kind = CardKind.AllyCover };
            state.Hand.Clear(); state.DiscardPile.Add(heavy); state.Hand.Add(cover);
            var play = ActionPlanner.Plan(state, new ActionRequest { Kind = ActionKind.Card, ActorId = support.Id, CardId = cover.Id, TargetId = warrior.Id });
            Assert.IsTrue(play.IsValid, play.Reason);
            Assert.IsTrue(BattleResolver.Apply(state, play).Success);
            Assert.AreEqual(support.Id, state.PendingRecoveryActorId);
            Assert.AreEqual(1, state.PendingDrawCount);
            Assert.IsFalse(ActionPlanner.Plan(state, new ActionRequest { Kind = ActionKind.Move, ActorId = warrior.Id, Destination = new GridPos(2, 4) }).IsValid);
            Assert.IsTrue(GrowthRules.ResolveRecovery(state, heavy.Id));
            Assert.IsNull(state.PendingRecoveryActorId);
            Assert.AreEqual(0, state.PendingDrawCount);
            Assert.IsTrue(heavy.Upgraded);
            Assert.AreEqual(2, state.Hand.Count);
        }

        [Test]
        public void RecoverTacticsCanBePlayedFromAFullHandWhenItWillFreeItsOwnSlot()
        {
            var state = BattleFactory.Create(13);
            state.Phase = BattlePhase.Player;
            state.Hand.Clear();
            state.Hand.Add(new CardInstance { Id = "recover", Kind = CardKind.RecoverTactics });
            for (var i = 0; i < 5; i++)
                state.Hand.Add(new CardInstance { Id = "hand-" + i, Kind = CardKind.Cover });
            state.DiscardPile.Add(new CardInstance { Id = "heavy", Kind = CardKind.HeavyAttack });

            Assert.AreEqual(1, GrowthRules.RecoveryCandidates(state).Count);
            var plan = ActionPlanner.Plan(state, new ActionRequest
            {
                Kind = ActionKind.Card,
                ActorId = "warrior",
                CardId = "recover",
                RecoveryCardId = "heavy"
            });
            Assert.IsTrue(plan.IsValid, plan.Reason);
            Assert.IsTrue(BattleResolver.Apply(state, plan).Success);
            Assert.AreEqual(6, state.Hand.Count);
            Assert.IsTrue(state.Hand.Exists(card => card.Id == "heavy"));
            Assert.IsFalse(state.Hand.Exists(card => card.Id == "recover"));
        }

        [Test]
        public void SupportFlowDrawsAfterTacticalRecallAndAlsoDrawsWhenRecallIsSkipped()
        {
            var selectedState = CreateSupportFlowRecoveryState();
            for (var i = 0; i < 5; i++)
                selectedState.Hand.Add(new CardInstance { Id = "selected-hand-" + i, Kind = CardKind.Cover });
            var selectedActor = selectedState.FindUnit("support");
            var selectedAction = ActionPlanner.Plan(selectedState, new ActionRequest
            {
                Kind = ActionKind.Card, ActorId = selectedActor.Id, CardId = "ally-cover", TargetId = "warrior"
            });
            Assert.IsTrue(selectedAction.IsValid, selectedAction.Reason);
            Assert.IsTrue(BattleResolver.Apply(selectedState, selectedAction).Success);
            var selectedRecovery = selectedState.DiscardPile[0];
            Assert.IsTrue(GrowthRules.ResolveRecovery(selectedState, selectedRecovery.Id));
            Assert.IsTrue(selectedState.Hand.Exists(card => card.Id == selectedRecovery.Id));
            Assert.AreEqual(6, selectedState.Hand.Count);
            Assert.AreEqual("draw-after-recovery", selectedState.DrawPile[0].Id);
            Assert.IsNull(selectedState.PendingRecoveryActorId);
            Assert.AreEqual(0, selectedState.PendingDrawCount);
            Assert.IsTrue(selectedActor.TacticalRecallUsedThisTurn);
            Assert.IsTrue(selectedActor.SupportFlowUsedThisTurn);

            var skippedState = CreateSupportFlowRecoveryState();
            var skippedActor = skippedState.FindUnit("support");
            var skippedAction = ActionPlanner.Plan(skippedState, new ActionRequest
            {
                Kind = ActionKind.Card, ActorId = skippedActor.Id, CardId = "ally-cover", TargetId = "warrior"
            });
            Assert.IsTrue(skippedAction.IsValid, skippedAction.Reason);
            Assert.IsTrue(BattleResolver.Apply(skippedState, skippedAction).Success);
            Assert.IsTrue(GrowthRules.ResolveRecovery(skippedState, null));
            Assert.AreEqual("draw-after-recovery", skippedState.Hand[0].Id);
            Assert.AreEqual(0, skippedState.DrawPile.Count);
            Assert.IsNull(skippedState.PendingRecoveryActorId);
            Assert.AreEqual(0, skippedState.PendingDrawCount);
        }

        private static BattleState CreateSupportFlowRecoveryState()
        {
            var state = BattleFactory.Create(14);
            var support = state.FindUnit("support");
            support.Talents.Add(TalentKind.TacticalRecall);
            support.Talents.Add(TalentKind.SupportFlow);
            state.Hand.Clear();
            state.Hand.Add(new CardInstance { Id = "ally-cover", Kind = CardKind.AllyCover });
            state.DiscardPile.Clear();
            state.DiscardPile.Add(new CardInstance { Id = "heavy-for-recall", Kind = CardKind.HeavyAttack });
            state.DrawPile.Clear();
            state.DrawPile.Add(new CardInstance { Id = "draw-after-recovery", Kind = CardKind.Cover });
            state.Phase = BattlePhase.Player;
            return state;
        }

        [Test]
        public void FacilitiesDoNotPreventEnemyClearVictoryAndBossDamageUsesAnchorWindow()
        {
            var state = BattleFactory.Create(2);
            state.Phase = BattlePhase.Player;
            foreach (var unit in state.Units) if (unit.Team == Team.Enemy) unit.Hp = 0;
            state.Units.Add(new UnitState { Id = "anchor", Role = UnitRole.ResonanceAnchor, Team = Team.Enemy, Hp = 4, MaxHp = 4, IsFacility = true });
            BattleResolver.RefreshOutcome(state);
            Assert.AreEqual(BattlePhase.Victory, state.Phase);

            var bossState = BattleFactory.Create(5);
            bossState.Phase = BattlePhase.Player;
            bossState.LevelIndex = 3;
            bossState.Round = 1;
            var boss = new UnitState { Id = "boss", Role = UnitRole.Boss, Team = Team.Enemy, Hp = 24, MaxHp = 24, Position = new GridPos(4, 1) };
            var anchorUnit = new UnitState { Id = "anchor", Role = UnitRole.ResonanceAnchor, Team = Team.Enemy, Hp = 4, MaxHp = 4, IsFacility = true };
            bossState.Units.Add(boss); bossState.Units.Add(anchorUnit);
            var attacker = bossState.FindUnit("warrior"); attacker.Position = new GridPos(4, 2);
            // The living anchor reduces this 3-damage hit by 1, so the boss loses 2 HP.
            Assert.AreEqual(2, DamageRules.Preview(bossState, attacker, boss, 3, 0).HpLost);
            bossState.BossVulnerableRound = bossState.Round;
            Assert.AreEqual(3, DamageRules.Preview(bossState, attacker, boss, 3, 0).HpLost);
        }

        [Test]
        public void EnemyShieldDamageCanArmCounterReadyOnlyWhenShieldAbsorbsDamage()
        {
            var state = BattleFactory.Create(3);
            state.Phase = BattlePhase.Enemy;
            var warrior = state.FindUnit("warrior"); warrior.Talents.Add(TalentKind.CounterReady); warrior.Shield = 2;
            var guard = state.FindUnit("guard-a"); guard.Position = new GridPos(2, 5);
            warrior.Position = new GridPos(2, 6);
            var request = new ActionRequest { Kind = ActionKind.Attack, ActorId = guard.Id, TargetId = warrior.Id };
            Assert.IsTrue(BattleResolver.Apply(state, ActionPlanner.Plan(state, request)).Success);
            Assert.IsTrue(warrior.CounterReadyPrepared);
        }

        [Test]
        public void UpgradedCoverGrantsThreeShieldAndRequiresRoomForThree()
        {
            var state = BattleFactory.Create(15);
            state.Phase = BattlePhase.Player;
            var warrior = state.FindUnit("warrior");
            warrior.Shield = 2;
            var upgradedCover = new CardInstance { Id = "cover-plus", Kind = CardKind.Cover, Upgraded = true };
            state.Hand.Add(upgradedCover);

            var plan = ActionPlanner.Plan(state, new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = upgradedCover.Id });
            Assert.IsTrue(plan.IsValid, plan.Reason);
            Assert.IsTrue(BattleResolver.Apply(state, plan).Success);
            Assert.AreEqual(3, warrior.Shield);

            state.Hand.Add(new CardInstance { Id = "cover-plus-again", Kind = CardKind.Cover, Upgraded = true });
            var fullShieldPlan = ActionPlanner.Plan(state, new ActionRequest { Kind = ActionKind.Card, ActorId = warrior.Id, CardId = "cover-plus-again" });
            Assert.IsFalse(fullShieldPlan.IsValid);
        }

        [Test]
        public void UpgradedInspireStrengthensEqualExpiryAndWeakInspireCanExtendWithoutDowngrading()
        {
            var state = BattleFactory.Create(16);
            state.Phase = BattlePhase.Player;
            state.Round = 2;
            var support = state.FindUnit("support");
            var warrior = state.FindUnit("warrior");
            warrior.InspireExpiresRound = 3;
            warrior.InspireBonus = 1;
            var upgraded = new CardInstance { Id = "inspire-plus", Kind = CardKind.Inspire, Upgraded = true };
            state.Hand.Add(upgraded);

            var strengthen = ActionPlanner.Plan(state, new ActionRequest
            {
                Kind = ActionKind.Card, ActorId = support.Id, CardId = upgraded.Id, TargetId = warrior.Id
            });
            Assert.IsTrue(strengthen.IsValid, strengthen.Reason);
            Assert.IsTrue(BattleResolver.Apply(state, strengthen).Success);
            Assert.AreEqual(3, warrior.InspireExpiresRound);
            Assert.AreEqual(2, warrior.InspireBonus);

            warrior.InspireExpiresRound = 2;
            warrior.InspireBonus = 2;
            var normal = new CardInstance { Id = "inspire", Kind = CardKind.Inspire };
            state.Hand.Add(normal);
            var extend = ActionPlanner.Plan(state, new ActionRequest
            {
                Kind = ActionKind.Card, ActorId = support.Id, CardId = normal.Id, TargetId = warrior.Id
            });
            Assert.IsTrue(extend.IsValid, extend.Reason);
            Assert.IsTrue(BattleResolver.Apply(state, extend).Success);
            Assert.AreEqual(3, warrior.InspireExpiresRound);
            Assert.AreEqual(2, warrior.InspireBonus);
        }

        [Test]
        public void SupportFlowSummaryOnlyClaimsAnImmediateDrawWhenOneWasDrawn()
        {
            var state = BattleFactory.Create(17);
            state.Phase = BattlePhase.Player;
            var support = state.FindUnit("support");
            var warrior = state.FindUnit("warrior");
            support.Talents.Add(TalentKind.SupportFlow);
            support.SupportFlowUsedThisTurn = true;
            state.Hand.Add(new CardInstance { Id = "ally-cover-no-flow", Kind = CardKind.AllyCover });
            var noTrigger = ActionPlanner.Plan(state, new ActionRequest
            {
                Kind = ActionKind.Card, ActorId = support.Id, CardId = "ally-cover-no-flow", TargetId = warrior.Id
            });
            Assert.IsTrue(noTrigger.IsValid, noTrigger.Reason);
            var noTriggerResult = BattleResolver.Apply(state, noTrigger);
            Assert.IsTrue(noTriggerResult.Success);
            Assert.IsFalse(noTriggerResult.Summary.Contains("支援续牌：抽1张"));

            var drawState = BattleFactory.Create(18);
            drawState.Phase = BattlePhase.Player;
            var drawSupport = drawState.FindUnit("support");
            drawSupport.Talents.Add(TalentKind.SupportFlow);
            drawState.Hand.Clear();
            drawState.Hand.Add(new CardInstance { Id = "ally-cover-flow", Kind = CardKind.AllyCover });
            drawState.DrawPile.Add(new CardInstance { Id = "flow-draw", Kind = CardKind.Cover });
            var drawPlan = ActionPlanner.Plan(drawState, new ActionRequest
            {
                Kind = ActionKind.Card, ActorId = drawSupport.Id, CardId = "ally-cover-flow", TargetId = "warrior"
            });
            Assert.IsTrue(drawPlan.IsValid, drawPlan.Reason);
            var drawResult = BattleResolver.Apply(drawState, drawPlan);
            Assert.IsTrue(drawResult.Success);
            Assert.IsTrue(drawResult.Summary.Contains("支援续牌：抽1张"));
            Assert.IsTrue(drawState.Hand.Exists(card => card.Id == "flow-draw"));
        }

        [Test]
        public void BossAoeExecutionReplacesSimulatedDamageSummariesInsteadOfAppendingDuplicates()
        {
            var state = BattleFactory.CreateLevel(3, 19);
            state.Phase = BattlePhase.Enemy;
            state.Round = 2;
            state.BossTelegraph = new GridPos(2, 6);
            var boss = state.FindUnit("boss");
            foreach (var unit in state.Units)
                if (unit.Team == Team.Enemy && unit.Id != boss.Id) unit.Hp = 0;
            state.FindUnit("warrior").Position = new GridPos(2, 6);
            state.FindUnit("mage").Position = new GridPos(3, 6);
            state.FindUnit("support").Position = new GridPos(2, 7);

            var preview = EnemyPlanner.PlanTurn(state);
            Assert.AreEqual(1, preview.Actions.Count);
            var aoe = preview.Actions[0];
            Assert.AreEqual(3, aoe.AreaDamageSummaries.Count);
            var result = BattleResolver.Apply(state, aoe);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, aoe.AreaDamageSummaries.Count);
            Assert.AreEqual(3, result.AreaDamageSummaries.Count);
        }
    }
}
