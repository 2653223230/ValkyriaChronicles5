using System.Linq;
using NUnit.Framework;
using VC5PvE.SharedAP;

namespace VC5PvE.SharedAP.Tests
{
    // Rule-level fixtures are deliberately manipulated directly. These tests validate
    // the written rules, not a naturally played or balanced clear of the stage.
    public class SharedRulesAcceptanceTests
    {
        SState state;
        SUnit vanguard, archer, mage, patrol, crossbow, guard, cannon;
        SCardInstance Card(SCard kind) { var c = state.Hand.FirstOrDefault(x => x.Kind == kind); if (c != null) return c; c = state.Deck.FirstOrDefault(x => x.Kind == kind); if (c == null) Assert.Fail("Fixture has no card of kind " + kind); state.Deck.Remove(c); state.Hand.Add(c); return c; }
        SAction Act(string actor, SCardInstance card, string target = null, SPos? destination = null) =>
            new SAction { Kind = SActionKind.Card, ActorId = actor, CardId = card.Id, TargetId = target, Destination = destination };
        SPlan Apply(SAction action) => SharedRules.Apply(state, action);
        SUnit Unit(SRole role) => state.Units.First(u => u.Role == role);

        [SetUp]
        public void SetUp()
        {
            state = SharedRules.Create(17, true);
            vanguard = Unit(SRole.Vanguard); archer = Unit(SRole.Archer); mage = Unit(SRole.Mage);
            patrol = Unit(SRole.Patrol); crossbow = Unit(SRole.Crossbow); guard = Unit(SRole.Guard); cannon = Unit(SRole.Cannon);
        }

        [Test] public void DemoFixtureHasExactMapAndSharedOpeningBudget()
        {
            Assert.AreEqual(6, state.Ap); Assert.AreEqual(1, state.Round); Assert.AreEqual(SPhase.Player, state.Phase);
            Assert.AreEqual(new SPos(2, 6), archer.Position); Assert.AreEqual(new SPos(3, 6), vanguard.Position);
            Assert.AreEqual(new SPos(4, 6), mage.Position); Assert.AreEqual(new SPos(2, 3), patrol.Position);
            Assert.AreEqual(new SPos(3, 3), crossbow.Position); Assert.AreEqual(new SPos(3, 2), guard.Position);
            Assert.AreEqual(new SPos(3, 1), cannon.Position);
            Assert.AreEqual(18, state.Hand.Count + state.Deck.Count + state.Discard.Count);
            CollectionAssert.AreEqual(new[] { SCard.Attack, SCard.Move, SCard.Charge, SCard.Shield, SCard.Skill }, state.Hand.Select(c => c.Kind));
            Assert.AreEqual("card-12", state.Hand[4].Id, "The fixed tutorial must exchange the top-draw skill entity into opening slot five.");
            Assert.AreEqual("card-17", state.Deck[6].Id, "The heal entity must take the skill entity's former seventh draw position.");
            CollectionAssert.AreEqual(
                new[] { SCard.Heavy, SCard.Shield, SCard.Move, SCard.Attack, SCard.Attack, SCard.Move, SCard.Heal, SCard.Heavy, SCard.Skill, SCard.Attack, SCard.Heal, SCard.Skill, SCard.Charge },
                state.Deck.Select(c => c.Kind));
        }

        [Test] public void PlanIsPureAndDoesNotSpendOrMove()
        {
            var plan = SharedRules.Plan(state, new SAction { Kind = SActionKind.ShortStep, ActorId = vanguard.Id, Destination = new SPos(3, 5) });
            Assert.IsTrue(plan.Valid); Assert.AreEqual(6, state.Ap); Assert.AreEqual(new SPos(3, 6), vanguard.Position);
            Assert.AreEqual(5, state.Hand.Count); Assert.IsFalse(vanguard.StepUsed);
        }

        [Test] public void CancelIsRepresentedByNotApplyingPreview()
        {
            var beforeRevision = state.Revision; var beforeHand = state.Hand.Count;
            SharedRules.Plan(state, new SAction { Kind = SActionKind.ShortStep, ActorId = vanguard.Id, Destination = new SPos(3, 5) });
            Assert.AreEqual(beforeRevision, state.Revision); Assert.AreEqual(beforeHand, state.Hand.Count);
            Assert.AreEqual(6, state.Ap); Assert.AreEqual(new SPos(3, 6), vanguard.Position);
        }

        [Test] public void IllegalCardTargetDoesNotSpendApOrConsumeCard()
        {
            var card = Card(SCard.Attack); var count = state.Hand.Count;
            var plan = Apply(Act(vanguard.Id, card, cannon.Id));
            Assert.IsFalse(plan.Valid); Assert.AreEqual(6, state.Ap); Assert.AreEqual(count, state.Hand.Count);
        }

        [Test] public void SharedApDoesNotDependOnUnitCountOrDeaths()
        {
            vanguard.Hp = 0; archer.Hp = 0; Assert.AreEqual(6, state.Ap);
            Assert.IsFalse(vanguard.Alive); Assert.IsFalse(archer.Alive);
            Assert.IsTrue(SharedRules.Plan(state, new SAction { Kind = SActionKind.ShortStep, ActorId = mage.Id, Destination = new SPos(4, 5) }).Valid);
        }

        [Test] public void ShortStepCostsNoApAndCanBeUsedOncePerLivingUnit()
        {
            var first = Apply(new SAction { Kind = SActionKind.ShortStep, ActorId = archer.Id, Destination = new SPos(1, 6) });
            Assert.IsTrue(first.Valid); Assert.AreEqual(6, state.Ap); Assert.IsTrue(state.Find(archer.Id).StepUsed);
            Assert.IsFalse(SharedRules.Plan(state, new SAction { Kind = SActionKind.ShortStep, ActorId = archer.Id, Destination = new SPos(2, 5) }).Valid);
        }

        [Test] public void BasicAttackCostsTwoApAndDoesNotConsumeAttackCard()
        {
            patrol.Position = new SPos(3, 5); var attacks = state.Hand.Count(c => c.Kind == SCard.Attack);
            var p = Apply(new SAction { Kind = SActionKind.BasicAttack, ActorId = vanguard.Id, TargetId = patrol.Id });
            Assert.IsTrue(p.Valid); Assert.AreEqual(4, state.Ap); Assert.IsTrue(state.Find(vanguard.Id).AttackUsed);
            Assert.AreEqual(attacks, state.Hand.Count(c => c.Kind == SCard.Attack));
        }

        [Test] public void AttackCardDoesNotConsumeOncePerTurnBasicAttack()
        {
            patrol.Position = new SPos(3, 5); Assert.IsTrue(Apply(Act(vanguard.Id, Card(SCard.Attack), patrol.Id)).Valid);
            Assert.IsFalse(state.Find(vanguard.Id).AttackUsed); Assert.AreEqual(5, state.Ap);
        }

        [Test] public void MoveCardWorksWithoutAnyAttackTarget()
        {
            state.Units.Remove(patrol); state.Units.Remove(crossbow); state.Units.Remove(guard); state.Units.Remove(cannon);
            var p = Apply(Act(mage.Id, Card(SCard.Move), destination: new SPos(4, 5)));
            Assert.IsTrue(p.Valid); Assert.AreEqual(new SPos(4, 5), state.Find(mage.Id).Position); Assert.AreEqual(5, state.Ap);
        }

        [Test] public void ChargeRequiresLegalAttackTargetAfterLanding()
        {
            vanguard.Position = new SPos(3, 5);
            var charge = Card(SCard.Charge);
            Assert.IsFalse(SharedRules.Plan(state, Act(vanguard.Id, charge, cannon.Id, new SPos(3, 4))).Valid);
            patrol.Position = new SPos(2, 3);
            Assert.IsTrue(SharedRules.Plan(state, Act(vanguard.Id, charge, patrol.Id, new SPos(2, 4))).Valid);
        }

        [Test] public void ChargePreviewDoesNotCommitBeforeDestinationAndTargetAreValid()
        {
            vanguard.Position = new SPos(3, 5); patrol.Position = new SPos(2, 3);
            var charge = Card(SCard.Charge); var pos = vanguard.Position; var ap = state.Ap; var n = state.Hand.Count;
            SharedRules.Plan(state, Act(vanguard.Id, charge, patrol.Id, new SPos(2, 4)));
            Assert.AreEqual(pos, vanguard.Position); Assert.AreEqual(ap, state.Ap); Assert.AreEqual(n, state.Hand.Count);
        }

        [Test] public void RangedWeaponRequiresLineOfSight()
        {
            archer.Position = new SPos(1, 1); patrol.Position = new SPos(1, 4);
            Assert.IsFalse(SharedRules.HasSight(state, archer.Position, patrol.Position));
            Assert.IsFalse(SharedRules.Plan(state, Act(archer.Id, Card(SCard.Attack), patrol.Id)).Valid);
        }

        [Test] public void AttackCardBonusAppliesOnlyToPrimaryTarget()
        {
            mage.Position = new SPos(3, 5); patrol.Position = new SPos(2, 4); crossbow.Position = new SPos(2, 3);
            var patrolHp = patrol.Hp; var crossbowHp = crossbow.Hp;
            var p = Apply(Act(mage.Id, Card(SCard.Heavy), patrol.Id));
            Assert.IsTrue(p.Valid); Assert.AreEqual(patrolHp - 3, state.Find(patrol.Id).Hp); Assert.AreEqual(crossbowHp - 1, state.Find(crossbow.Id).Hp);
        }

        [Test] public void MageSkillDealsTwoToVisibleTargetAndFourNeighborsUsingNormalDamageRules()
        {
            state.Obstacles.Clear();
            mage.Position = new SPos(3, 6);
            cannon.Position = new SPos(3, 4);
            crossbow.Position = new SPos(3, 3);
            guard.Position = new SPos(2, 4);
            patrol.Position = new SPos(4, 4);
            archer.Position = new SPos(2, 6);
            crossbow.Shield = 1;
            var patrolHp = patrol.Hp;
            var crossbowHp = crossbow.Hp;
            var guardHp = guard.Hp;
            var cannonHp = cannon.Hp;
            var archerHp = archer.Hp;

            var plan = Apply(Act(mage.Id, Card(SCard.Skill), cannon.Id));

            Assert.IsTrue(plan.Valid, plan.Reason);
            Assert.AreEqual(patrolHp - 2, state.Find(patrol.Id).Hp);
            Assert.AreEqual(crossbowHp - 1, state.Find(crossbow.Id).Hp);
            Assert.AreEqual(0, state.Find(crossbow.Id).Shield);
            Assert.AreEqual(guardHp - 2, state.Find(guard.Id).Hp);
            Assert.AreEqual(cannonHp, state.Find(cannon.Id).Hp, "Adjacent guard protection must use the same damage reduction for spell damage.");
            Assert.AreEqual(archerHp, state.Find(archer.Id).Hp, "Allies are not affected.");
            CollectionAssert.AreEquivalent(new[] { cannon.Position, patrol.Position, crossbow.Position, guard.Position }, plan.Affected);
            Assert.AreEqual(5, state.Ap);
        }

        [Test] public void MageSkillRejectsOutOfRangeOrObscuredCenterAtomically()
        {
            var skill = Card(SCard.Skill);
            var ap = state.Ap;
            var handCount = state.Hand.Count;
            patrol.Position = new SPos(4, 2);
            Assert.IsFalse(SharedRules.Apply(state, Act(mage.Id, skill, patrol.Id)).Valid);
            Assert.AreEqual(ap, state.Ap);
            Assert.AreEqual(handCount, state.Hand.Count);

            patrol.Position = new SPos(4, 4);
            state.Obstacles.Add(new SPos(4, 5));
            Assert.IsFalse(SharedRules.Apply(state, Act(mage.Id, skill, patrol.Id)).Valid);
            Assert.AreEqual(ap, state.Ap);
            Assert.AreEqual(handCount, state.Hand.Count);
        }

        [Test] public void GuardProtectionReducesCannonDamageByTwoToMinimumZero()
        {
            guard.Position = new SPos(3, 2); archer.Position = new SPos(4, 2); cannon.Hp = cannon.MaxHp;
            var p = Apply(Act(archer.Id, Card(SCard.Attack), cannon.Id));
            Assert.IsTrue(p.Valid); Assert.AreEqual(cannon.MaxHp, state.Find(cannon.Id).Hp);
            state.Find(guard.Id).Position = new SPos(2, 2); state.Find(archer.Id).Braced = true;
            p = Apply(Act(archer.Id, Card(SCard.Attack), cannon.Id));
            Assert.IsTrue(p.Valid); Assert.AreEqual(cannon.MaxHp - 4, state.Find(cannon.Id).Hp);
        }

        [Test] public void BracingExpiresOnMovementAndUnusedBraceByNextOwnTurnEnd()
        {
            archer.Braced = true; archer.BraceExpiresRound = state.Round + 1;
            Assert.IsTrue(Apply(new SAction { Kind = SActionKind.ShortStep, ActorId = archer.Id, Destination = new SPos(1, 6) }).Valid);
            Assert.IsFalse(state.Find(archer.Id).Braced);
        }

        [Test] public void BraceCannotBeAppliedWhenSameEffectAndExpiryAlreadyActive()
        {
            archer.Braced = true; archer.BraceExpiresRound = state.Round + 1;
            var skill = Card(SCard.Skill); var n = state.Hand.Count; var ap = state.Ap;
            Assert.IsFalse(Apply(Act(archer.Id, skill)).Valid);
            Assert.AreEqual(n, state.Hand.Count); Assert.AreEqual(ap, state.Ap);
        }

        [Test] public void HealRequiresLivingInjuredAllyAndDoesNotReviveOrOverheal()
        {
            archer.Hp = 4; var heal = Card(SCard.Heal);
            Assert.IsTrue(Apply(Act(archer.Id, heal, archer.Id)).Valid); Assert.AreEqual(6, state.Find(archer.Id).Hp);
            var before = state.Ap; Assert.IsFalse(Apply(Act(archer.Id, Card(SCard.Heal), archer.Id)).Valid); Assert.AreEqual(before, state.Ap);
            state.Find(archer.Id).Hp = 0; Assert.IsFalse(SharedRules.Plan(state, Act(archer.Id, Card(SCard.Heal), archer.Id)).Valid);
        }

        [Test] public void ShieldUsesMaximumAndCannotBeWastedWhenValueAndExpiryUnchanged()
        {
            archer.Shield = 2; var p = Apply(Act(archer.Id, Card(SCard.Shield), archer.Id));
            Assert.IsTrue(p.Valid); Assert.AreEqual(3, state.Find(archer.Id).Shield);
            var ap = state.Ap; Assert.IsFalse(Apply(Act(archer.Id, Card(SCard.Shield), archer.Id)).Valid); Assert.AreEqual(ap, state.Ap);
        }

        [Test] public void DiscardRejectsDuplicateOrNonHandIdsAtomically()
        {
            var ids = state.Hand.Select(c => c.Id).ToArray(); var ap = state.Ap; var count = state.Hand.Count;
            Assert.IsFalse(SharedRules.EndTurn(state, new[] { "missing" }).Valid);
            Assert.AreEqual(ap, state.Ap); Assert.AreEqual(count, state.Hand.Count);
            Assert.IsFalse(SharedRules.EndTurn(state, new[] { ids[0], ids[0] }).Valid);
            Assert.AreEqual(count, state.Hand.Count);
        }

        [Test] public void EndTurnSupportsZeroAllAndPartialDiscardAndRefillsToFive()
        {
            var original = state.Hand.Select(c => c.Id).ToArray();
            var zero = state.Clone();
            Assert.IsTrue(SharedRules.EndTurn(zero, null).Valid);
            CollectionAssert.IsSubsetOf(original, zero.Hand.Select(c => c.Id).ToArray());

            var all = state.Clone();
            var allIds = all.Hand.Select(c => c.Id).ToArray();
            Assert.IsTrue(SharedRules.EndTurn(all, allIds).Valid);
            Assert.AreEqual(5, all.Hand.Count);
            Assert.IsFalse(all.Hand.Any(c => allIds.Contains(c.Id)));

            var partial = state.Clone();
            var discarded = partial.Hand[0].Id;
            var kept = partial.Hand.Skip(1).Select(c => c.Id).ToArray();
            Assert.IsTrue(SharedRules.EndTurn(partial, new[] { discarded }).Valid);
            Assert.AreEqual(5, partial.Hand.Count);
            CollectionAssert.IsSubsetOf(kept, partial.Hand.Select(c => c.Id).ToArray());
            Assert.IsFalse(partial.Hand.Any(c => c.Id == discarded));
            Assert.AreEqual(1, partial.Discard.Count(c => c.Id == discarded));
            Assert.AreEqual(partial.Hand.Count, partial.Hand.Select(c => c.Id).Distinct().Count());
            Assert.AreEqual(18, partial.Hand.Count + partial.Deck.Count + partial.Discard.Count);
        }

        [Test] public void EnemyPreviewDoesNotMutateOriginalState()
        {
            var before = state.Clone(); var preview = SharedRules.EnemyPreview(state);
            Assert.IsNotNull(preview); Assert.AreEqual(before.Ap, state.Ap); Assert.AreEqual(before.Revision, state.Revision);
            CollectionAssert.AreEqual(before.Units.Select(u => u.Position), state.Units.Select(u => u.Position));
        }

        [Test] public void FirstCannonTelegraphIsFixedAndHitsEnemiesAsWellAsPlayers()
        {
            Assert.IsTrue(state.Telegraph.Contains(new SPos(1, 5))); Assert.IsTrue(state.Telegraph.Contains(new SPos(2, 5)));
            Assert.IsTrue(state.Telegraph.Contains(new SPos(3, 5)));
            var p = SharedRules.EnemyPreview(state); Assert.IsNotNull(p);
        }

        [Test] public void CannonTieBreakIsDeterministic()
        {
            var a = SharedRules.EnemyPreview(state); var b = SharedRules.EnemyPreview(state);
            CollectionAssert.AreEqual(a.Affected, b.Affected);
        }

        [Test] public void DestroyingCannonWinsImmediately()
        {
            guard.Position = new SPos(2, 2); cannon.Hp = 1; archer.Position = new SPos(4, 2); archer.Braced = true;
            var p = Apply(Act(archer.Id, Card(SCard.Attack), cannon.Id));
            Assert.IsTrue(p.Valid); Assert.AreEqual(SPhase.Won, state.Phase);
        }

        [Test] public void DefeatingAllThreeFieldEnemiesWinsWhileCannonSurvives()
        {
            patrol.Hp = 0; crossbow.Hp = 0; guard.Hp = 1;
            vanguard.Position = new SPos(3, 3); guard.Position = new SPos(3, 2);
            var p = Apply(Act(vanguard.Id, Card(SCard.Attack), guard.Id));
            Assert.IsTrue(p.Valid); Assert.AreEqual(SPhase.Won, state.Phase); Assert.IsTrue(state.Find(cannon.Id).Alive);
        }

        [Test] public void CannonSalvoCanDefeatLastFieldEnemyAndEndsEnemyPhaseImmediately()
        {
            patrol.Hp = 1; crossbow.Hp = 0; guard.Hp = 0;
            patrol.Position = new SPos(1, 5); vanguard.Position = new SPos(1, 4);
            state.Round = 2;
            var p = SharedRules.EndTurn(state, new string[0]);
            Assert.IsTrue(p.Valid); Assert.AreEqual(SPhase.Won, state.Phase); Assert.IsFalse(state.Find(patrol.Id).Alive);
            Assert.AreEqual("cannon-salvo", p.Steps.Last().Action.CardId);
            CollectionAssert.AreEqual(new[]{new SPos(1,5)},p.Steps.Last().FiredCells,"Stop firing when the last field enemy falls.");
            Assert.AreEqual(2, state.Round);
        }

        [Test] public void GuardPursuesOnRoundOneAndMovesNoMoreThanTwoCells()
        {
            var before = guard.Position;
            var preview = SharedRules.EnemyPreview(state);
            var guardStep = preview.Steps.First(x => x.Action.ActorId == guard.Id);
            var after = guardStep.After.Find(guard.Id).Position;
            Assert.AreNotEqual(before, after, "The guard must begin pursuing during the first enemy phase.");
            Assert.LessOrEqual(before.Distance(after), 2, "The guard may move at most two cells in a round.");
        }

        [Test] public void GuardAttacksInPlaceWhenAPlayerIsAlreadyInRangeFromRoundTwo()
        {
            state.Round = 2; vanguard.Position = new SPos(4, 2);
            var before = guard.Position;
            var p = SharedRules.EnemyPreview(state);
            var guardStep = p.Steps.First(x => x.Action.ActorId == guard.Id);
            Assert.AreEqual(before, guardStep.After.Find(guard.Id).Position);
            Assert.AreEqual(guardStep.Before.Find(vanguard.Id).Hp - 3, guardStep.After.Find(vanguard.Id).Hp);
        }

        [Test] public void MeleeEnemiesDealThreeWhileCrossbowStillDealsTwo()
        {
            state.Units.Remove(cannon);
            patrol.Position = new SPos(3, 5); vanguard.Position = new SPos(3, 6);
            guard.Position = new SPos(4, 5); mage.Position = new SPos(4, 6);
            crossbow.Position = new SPos(0, 0);

            var preview = SharedRules.EnemyPreview(state);
            var patrolStep = preview.Steps.First(x => x.Action.ActorId == patrol.Id);
            var guardStep = preview.Steps.First(x => x.Action.ActorId == guard.Id);
            Assert.AreEqual(3, patrolStep.Before.Find(vanguard.Id).Hp - patrolStep.After.Find(vanguard.Id).Hp);
            Assert.AreEqual(3, guardStep.Before.Find(mage.Id).Hp - guardStep.After.Find(mage.Id).Hp);

            state.Units.Remove(patrol); state.Units.Remove(guard);
            crossbow.Position = new SPos(2, 5); archer.Position = new SPos(2, 6);
            preview = SharedRules.EnemyPreview(state);
            var crossbowStep = preview.Steps.First(x => x.Action.ActorId == crossbow.Id);
            Assert.AreEqual(2, crossbowStep.Before.Find(archer.Id).Hp - crossbowStep.After.Find(archer.Id).Hp);
        }

        [Test] public void ExactOpeningPaperLedgerFirstActionMatchesDesign()
        {
            var p = Apply(new SAction { Kind = SActionKind.ShortStep, ActorId = vanguard.Id, Destination = new SPos(3, 5) });
            Assert.IsTrue(p.Valid); Assert.AreEqual(6, state.Ap);
            p = Apply(Act(vanguard.Id, Card(SCard.Charge), patrol.Id, new SPos(2, 4)));
            Assert.IsTrue(p.Valid); Assert.AreEqual(4, state.Ap); Assert.AreEqual(new SPos(2, 4), state.Find(vanguard.Id).Position);
            Assert.AreEqual(1, state.Find(patrol.Id).Hp);
        }
    }
}






