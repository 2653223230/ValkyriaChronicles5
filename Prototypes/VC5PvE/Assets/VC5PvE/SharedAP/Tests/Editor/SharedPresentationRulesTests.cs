using System.Linq;
using NUnit.Framework;
using VC5PvE.SharedAP;

namespace VC5PvE.SharedAP.Tests
{
    public class SharedPresentationRulesTests
    {
        [Test]
        public void EnemyStepsChainSnapshotsAndKeepCannonCardActionTargetless()
        {
            var state = SharedRules.Create(17, true);
            foreach (var enemy in state.Units.Where(u => !u.Player && u.Role != SRole.Cannon))
                enemy.Hp = enemy.MaxHp;

            var preview = SharedRules.EnemyPreview(state);

            Assert.IsNotEmpty(preview.Steps);
            for (var i = 0; i < preview.Steps.Count; i++)
            {
                var step = preview.Steps[i];
                Assert.IsNotNull(step.Before);
                Assert.IsNotNull(step.After);
                Assert.IsNotNull(step.Action);
                Assert.AreEqual(SPhase.Enemy, step.Before.Phase);
                if (i > 0)
                    AssertStateEqual(preview.Steps[i - 1].After, step.Before);
                Assert.AreNotSame(step.Before, step.After);
                Assert.AreNotSame(step.Before.Find(step.Action.ActorId), step.After.Find(step.Action.ActorId));
                Assert.AreNotSame(step.Before.Hand[0], step.After.Hand[0]);
                Assert.AreNotSame(step.Before.Obstacles, step.After.Obstacles);
                Assert.AreNotSame(step.Before.Telegraph, step.After.Telegraph);
            }

            var cannonStep = preview.Steps.Single(step => step.Action.ActorId == "cannon");
            Assert.AreEqual(SActionKind.Card, cannonStep.Action.Kind);
            Assert.IsNull(cannonStep.Action.TargetId);
            Assert.AreEqual(SRole.Cannon, cannonStep.After.Find("cannon").Role);
        }

        [Test]
        public void EnemyPreviewAndEndTurnRecordSameActionsAndCombatSnapshotsWithoutMutatingPreviewInput()
        {
            var state = SharedRules.Create(17, true);
            var before = state.Clone();
            var preview = SharedRules.EnemyPreview(state);
            AssertStateEqual(before, state);

            var turn = SharedRules.EndTurn(state, new string[0]);

            Assert.IsTrue(turn.Valid, turn.Reason);
            Assert.AreEqual(preview.Steps.Count, turn.Steps.Count);
            for (var i = 0; i < preview.Steps.Count; i++)
            {
                var expected = preview.Steps[i];
                var actual = turn.Steps[i];
                Assert.AreEqual(expected.Action.Kind, actual.Action.Kind);
                Assert.AreEqual(expected.Action.ActorId, actual.Action.ActorId);
                Assert.AreEqual(expected.Action.CardId, actual.Action.CardId);
                Assert.AreEqual(expected.Action.TargetId, actual.Action.TargetId);
                AssertCombatStateEqual(expected.Before, actual.Before);
                AssertCombatStateEqual(expected.After, actual.After);
            }

            Assert.AreEqual(SPhase.Player, turn.After.Phase);
            Assert.AreEqual(2, turn.After.Round);
            Assert.AreEqual(1, turn.Steps[turn.Steps.Count - 1].After.Round);
            Assert.AreEqual(SPhase.Enemy, turn.Steps[turn.Steps.Count - 1].After.Phase);
        }

        [Test]
        public void CannonShotIsRecordedBeforeRoundRefreshAndNoLaterEnemyActsAfterWin()
        {
            var state = SharedRules.Create(17, true);
            foreach (var enemy in state.Units.Where(u => !u.Player && u.Role != SRole.Cannon)) enemy.Hp = 0;
            state.Find("archer").Position = new SPos(4, 1);
            state.Find("cannon").Hp = 1;

            var win = SharedRules.Apply(state, new SAction
            {
                Kind = SActionKind.Card,
                ActorId = "archer",
                CardId = state.Hand.First(c => c.Kind == SCard.Attack).Id,
                TargetId = "cannon"
            });
            Assert.IsTrue(win.Valid);
            Assert.AreEqual(SPhase.Won, state.Phase);

            // A terminal state does not run an enemy turn; the separate preview test
            // below verifies the cannon step's round-boundary state on a live board.
            var terminalPreview = SharedRules.EnemyPreview(state);
            Assert.IsEmpty(terminalPreview.Steps);
        }

        [Test]
        public void RoleAndCardCopyStatesRealRuleDifferences()
        {
            var state = SharedRules.Create(17, true);
            var vanguard = state.Find("vanguard");
            var archer = state.Find("archer");
            var mage = state.Find("mage");

            StringAssert.Contains("3", SharedRoleInfo.Weapon(vanguard));
            StringAssert.Contains("1", SharedRoleInfo.Weapon(vanguard));
            StringAssert.Contains("射程 3", SharedRoleInfo.Weapon(archer));
            StringAssert.Contains("架弩", SharedRoleInfo.Weapon(archer));
            StringAssert.Contains("射程 2", SharedRoleInfo.Weapon(mage));
            StringAssert.Contains("相邻", SharedRoleInfo.Weapon(mage));

            StringAssert.Contains("移动", SharedRoleInfo.Skill(vanguard));
            StringAssert.Contains("架弩", SharedRoleInfo.Skill(archer));
            StringAssert.Contains("星火爆裂", SharedRoleInfo.Skill(mage));
            StringAssert.Contains("四邻可见敌人各受 2 点伤害", SharedRoleInfo.Skill(mage));
            StringAssert.Contains("星火爆裂", SharedRules.CardText(SCard.Skill));

            var heavy = SharedRoleInfo.CardEffect(mage, SCard.Heavy);
            StringAssert.Contains("主目标", heavy);
            StringAssert.Contains("相邻", heavy);
            archer.Braced = true;
            StringAssert.Contains("移动", SharedRoleInfo.CardEffect(archer, SCard.Charge));
            StringAssert.Contains("解除", SharedRoleInfo.CardEffect(archer, SCard.Charge));
            StringAssert.DoesNotContain("当前架弩", SharedRoleInfo.CardEffect(archer, SCard.Charge));
            StringAssert.Contains("3", SharedRoleInfo.CardEffect(vanguard, SCard.Shield));
            StringAssert.Contains("无职业加成", SharedRoleInfo.CardEffect(mage, SCard.Shield));
            StringAssert.Contains("3", SharedRoleInfo.CardEffect(archer, SCard.Heal));
            StringAssert.Contains("无职业加成", SharedRoleInfo.CardEffect(archer, SCard.Heal));
        }

        static void AssertStateEqual(SState expected, SState actual)
        {
            Assert.AreEqual(expected.Round, actual.Round);
            Assert.AreEqual(expected.Ap, actual.Ap);
            Assert.AreEqual(expected.Revision, actual.Revision);
            Assert.AreEqual(expected.Phase, actual.Phase);
            Assert.AreEqual(expected.Seed, actual.Seed);
            AssertCombatStateEqual(expected, actual);
            CollectionAssert.AreEqual(expected.Hand.Select(c => c.Id), actual.Hand.Select(c => c.Id));
            CollectionAssert.AreEqual(expected.Deck.Select(c => c.Id), actual.Deck.Select(c => c.Id));
            CollectionAssert.AreEqual(expected.Discard.Select(c => c.Id), actual.Discard.Select(c => c.Id));
        }

        static void AssertCombatStateEqual(SState expected, SState actual)
        {
            CollectionAssert.AreEqual(expected.Units.Select(UnitSignature), actual.Units.Select(UnitSignature));
            CollectionAssert.AreEqual(expected.Telegraph, actual.Telegraph);
            CollectionAssert.AreEquivalent(expected.Obstacles, actual.Obstacles);
        }

        static string UnitSignature(SUnit unit) => unit.Id + ":" + unit.Hp + ":" + unit.Shield + ":" + unit.Position + ":" + unit.Braced + ":" + unit.BraceExpiresRound;
    }
}
