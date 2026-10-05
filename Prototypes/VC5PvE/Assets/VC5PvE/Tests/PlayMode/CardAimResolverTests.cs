using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VC5PvE.Tests
{
    public class CardAimResolverTests
    {
        [Test]
        public void CoverKeepsSelectedExecutorWhenHoveringAnotherFriendly()
        {
            var state = PlayerBattle(CardKind.Cover);
            var hovered = state.FindUnit("warrior");

            var plan = CardAimResolver.Resolve(state, CardId(state, CardKind.Cover), "support",
                hovered, hovered.Position, Vector2.zero, null);

            Assert.IsTrue(plan.IsValid, plan.Reason);
            Assert.AreEqual("support", plan.Request.ActorId);
        }

        [Test]
        public void ResolveDoesNotAutomaticallySwitchToRequiredRole()
        {
            var state = PlayerBattle(CardKind.SparkMark);
            var enemy = state.FindUnit("guard-a");
            enemy.Position = new GridPos(4, 4);

            var plan = CardAimResolver.Resolve(state, CardId(state, CardKind.SparkMark), "support",
                enemy, enemy.Position, Vector2.zero, null);

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("support", plan.Request.ActorId);
        }

        [Test]
        public void NeutralDamageCardKeepsSelectedActorWhenAnotherRoleWouldHaveMoreRange()
        {
            var state = PlayerBattle(CardKind.HeavyAttack);
            state.FindUnit("guard-a").Position = new GridPos(3, 4);
            var before = state.Clone();

            var plan = CardAimResolver.Resolve(state, CardId(state, CardKind.HeavyAttack), "warrior",
                state.FindUnit("guard-a"), null, Vector2.zero, null);

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("warrior", plan.Request.ActorId);
            Assert.AreEqual("guard-a", plan.Request.TargetId);
            Assert.AreEqual(before.FindUnit("warrior").Ap, state.FindUnit("warrior").Ap);
            Assert.AreEqual(before.FindUnit("warrior").Position, state.FindUnit("warrior").Position);
            Assert.AreEqual(before.Hand.Count, state.Hand.Count);
        }

        [Test]
        public void ChargeBuildsOneCompleteLegalRequestFromCursorNearestDestination()
        {
            var state = PlayerBattle(CardKind.Charge);
            state.FindUnit("warrior").Position = new GridPos(2, 5);
            state.FindUnit("guard-a").Position = new GridPos(4, 4);
            var before = state.Clone();

            var plan = CardAimResolver.Resolve(state, CardId(state, CardKind.Charge), "warrior",
                state.FindUnit("guard-a"), null, new Vector2(3, 4), p => new Vector2(p.X, p.Y));

            Assert.IsTrue(plan.IsValid, plan.Reason);
            Assert.AreEqual("warrior", plan.Request.ActorId);
            Assert.AreEqual("guard-a", plan.Request.TargetId);
            Assert.AreEqual(new GridPos(3, 4), plan.Request.Destination.Value);
            Assert.IsNotEmpty(plan.Path);
            Assert.AreEqual(before.FindUnit("warrior").Ap, state.FindUnit("warrior").Ap);
            Assert.AreEqual(before.FindUnit("warrior").Position, state.FindUnit("warrior").Position);
            Assert.AreEqual(before.FindUnit("guard-a").Hp, state.FindUnit("guard-a").Hp);
            Assert.AreEqual(before.Hand.Count, state.Hand.Count);
        }

        [Test]
        public void InvalidAdvanceCellReturnsPlannerRequestWithoutChangingBattle()
        {
            var state = PlayerBattle(CardKind.Advance);
            var before = state.Clone();

            var plan = CardAimResolver.Resolve(state, CardId(state, CardKind.Advance), "warrior",
                null, new GridPos(-1, 0), Vector2.zero, null);

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("warrior", plan.Request.ActorId);
            Assert.AreEqual(new GridPos(-1, 0), plan.Request.Destination.Value);
            Assert.AreEqual(before.Revision, state.Revision);
            Assert.AreEqual(before.FindUnit("warrior").Ap, state.FindUnit("warrior").Ap);
            Assert.AreEqual(before.FindUnit("warrior").Position, state.FindUnit("warrior").Position);
            Assert.AreEqual(before.FindUnit("warrior").Shield, state.FindUnit("warrior").Shield);
            Assert.AreEqual(before.Hand.Count, state.Hand.Count);
        }

        [Test]
        public void LegalTargetsOnlyContainsValidDamageTargetsWithinSelectedActorsReach()
        {
            var state = PlayerBattle(CardKind.HeavyAttack);
            state.FindUnit("warrior").Ap = 2;
            state.FindUnit("guard-a").Position = new GridPos(2, 5);
            state.FindUnit("guard-b").Position = new GridPos(7, 7);

            List<GridPos> targets = CardAimResolver.LegalTargets(state, CardId(state, CardKind.HeavyAttack), "warrior");

            CollectionAssert.AreEqual(new[] { new GridPos(2, 5) }, targets);
            state.FindUnit("warrior").Ap = 1;
            Assert.IsEmpty(CardAimResolver.LegalTargets(state, CardId(state, CardKind.HeavyAttack), "warrior"), "2 AP 的重攻击不可被 1 AP 执行者使用");
        }

        [Test]
        public void CanExecuteRequiresAtLeastOneCompleteLegalAction()
        {
            var state = PlayerBattle(CardKind.HeavyAttack);
            var cardId = CardId(state, CardKind.HeavyAttack);
            state.FindUnit("warrior").Ap = 2;
            state.FindUnit("guard-a").Position = new GridPos(7, 7);
            state.FindUnit("guard-b").Position = new GridPos(6, 7);

            Assert.IsFalse(CardAimResolver.CanExecute(state, cardId, "warrior"), "没有合法目标时不可用");

            state.FindUnit("guard-a").Position = new GridPos(2, 5);
            Assert.IsTrue(CardAimResolver.CanExecute(state, cardId, "warrior"));
            state.FindUnit("warrior").Ap = 1;
            Assert.IsFalse(CardAimResolver.CanExecute(state, cardId, "warrior"), "AP不足时不可用");
        }

        [Test]
        public void LegalExecutorsOnlyIncludesActorsWithACompleteLegalAction()
        {
            var state = PlayerBattle(CardKind.SparkMark);
            state.FindUnit("mage").Ap = 1;
            state.FindUnit("guard-a").Position = new GridPos(3, 5);
            state.FindUnit("guard-b").Position = new GridPos(7, 7);

            List<string> actors = CardAimResolver.LegalExecutors(state, CardId(state, CardKind.SparkMark));

            CollectionAssert.AreEqual(new[] { "mage" }, actors);
        }

        [Test]
        public void LegalChargeDestinationsEnumeratesPlannerApprovedAdjacentCellsForFixedActor()
        {
            var state = PlayerBattle(CardKind.Charge);
            state.FindUnit("warrior").Position = new GridPos(2, 5);
            state.FindUnit("guard-a").Position = new GridPos(4, 4);
            var cardId = CardId(state, CardKind.Charge);

            List<GridPos> destinations = CardAimResolver.LegalChargeDestinations(state, cardId, "warrior", "guard-a");

            CollectionAssert.Contains(destinations, new GridPos(3, 4));
            CollectionAssert.DoesNotContain(destinations, state.FindUnit("guard-a").Position);
            state.FindUnit("warrior").Ap = 1;
            Assert.IsEmpty(CardAimResolver.LegalChargeDestinations(state, cardId, "warrior", "guard-a"));
        }

        [Test]
        public void LegalTargetsForChargeListsEnemiesBeforeLandingSelection()
        {
            var state = PlayerBattle(CardKind.Charge);
            state.FindUnit("warrior").Position = new GridPos(2, 5);
            state.FindUnit("guard-a").Position = new GridPos(4, 4);
            state.FindUnit("guard-b").Position = new GridPos(7, 7);
            state.FindUnit("shooter").Position = new GridPos(7, 6);

            List<GridPos> targets = CardAimResolver.LegalTargets(state, CardId(state, CardKind.Charge), "warrior");

            CollectionAssert.Contains(targets, new GridPos(4, 4), "先选择敌人");
            CollectionAssert.DoesNotContain(targets, new GridPos(3, 4), "落点要到下一阶段再显示");
        }

        private static BattleState PlayerBattle(CardKind kind)
        {
            var state = BattleFactory.Create(17, true);
            state.Phase = BattlePhase.Player;
            state.Hand.Clear();
            state.Hand.Add(new CardInstance { Id = BattleFactory.CardId(kind, 1), Kind = kind });
            return state;
        }

        private static string CardId(BattleState state, CardKind kind)
        {
            return state.Hand.Find(card => card.Kind == kind).Id;
        }
    }
}
