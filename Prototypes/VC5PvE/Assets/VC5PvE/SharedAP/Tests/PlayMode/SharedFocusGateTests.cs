using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace VC5PvE.SharedAP.Tests
{
    public class SharedFocusGateTests
    {
        GameObject host;
        SharedBattleController flow;
        [SetUp] public void SetUp()
        {
            host = new GameObject("Focus gate fixture");
            flow = host.AddComponent<SharedBattleController>();
            flow.InitializeForTests(17);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); }

        [Test] public void OpeningRejectsEarlyEndWithoutChangingBattle()
        {
            flow.BeginEndTurn();
            Assert.AreEqual(SharedInputStage.Idle, flow.Stage, "Must complete the focused action before ending the turn.");
            Assert.AreEqual(1, flow.State.Round);
            Assert.AreEqual(6, flow.State.Ap);
            Assert.AreEqual(5, flow.State.Hand.Count);
        }

        [Test] public void OpeningRejectsWrongUnitAndMovement()
        {
            var before = flow.State.Find("vanguard").Position;
            flow.ChooseUnit("vanguard");
            flow.ClickCell(new SPos(3,5));
            Assert.AreEqual(before, flow.State.Find("vanguard").Position, "Wrong but legal actions must not change the board.");
            Assert.IsFalse(flow.State.Find("vanguard").StepUsed);
            Assert.AreEqual(6, flow.State.Ap);
        }

        [Test] public void SkipConfirmationCanBeCancelledAndConfirmedWithoutResettingTheBattle()
        {
            flow.ChooseUnit("mage");
            var hp=flow.State.Find("mage").Hp;
            var hand=flow.State.Hand.Select(c=>c.Id).ToArray();
            flow.RequestSkipTutorial();
            Assert.IsTrue(flow.FocusExitOpen);
            flow.ResumeFocusTutorial();
            Assert.IsFalse(flow.FocusExitOpen);
            Assert.AreEqual(FocusTutorialStep.ReadInfo,flow.FocusStep);
            Assert.AreEqual(hp,flow.State.Find("mage").Hp);
            CollectionAssert.AreEqual(hand,flow.State.Hand.Select(c=>c.Id));

            flow.ContinueFocusTutorial();
            flow.ChooseBasic(SActionKind.ShortStep);
            flow.ClickCell(new SPos(4,5));
            var position=flow.State.Find("mage").Position;
            var ap=flow.State.Ap;
            flow.RequestSkipTutorial();
            Assert.IsTrue(flow.FocusExitOpen);
            flow.ConfirmSkipTutorial();
            Assert.AreEqual(FocusTutorialStep.Off,flow.FocusStep);
            Assert.IsFalse(flow.FocusTutorialActive);
            Assert.AreEqual(position,flow.State.Find("mage").Position);
            Assert.AreEqual(ap,flow.State.Ap);
        }

        [Test] public void ReadyStartsFreshFixedBattleWithoutTeachingAndDoesNotRestartAgain()
        {
            var initial=SharedRules.Create(17,true);
            flow.State.Round=2;flow.State.Ap=1;
            flow.State.Find("mage").Hp=2;flow.State.Find("mage").Position=new SPos(4,3);
            flow.State.Find("patrol").Hp=0;
            flow.State.Hand.RemoveAt(0);
            typeof(SharedBattleController).GetProperty("FocusStep").SetValue(flow,FocusTutorialStep.Ready,null);
            flow.ContinueFocusTutorial();
            Assert.AreEqual(1,flow.State.Round);Assert.AreEqual(6,flow.State.Ap);
            Assert.IsFalse(flow.Teaching);Assert.IsFalse(flow.FocusTutorialActive);
            Assert.AreEqual(SharedInputStage.Idle,flow.Stage);
            CollectionAssert.AreEqual(initial.Hand.Select(c=>c.Id),flow.State.Hand.Select(c=>c.Id));
            foreach(var expected in initial.Units)
            {
                var actual=flow.State.Find(expected.Id);
                Assert.AreEqual(expected.Hp,actual.Hp);Assert.AreEqual(expected.Position,actual.Position);
                Assert.IsFalse(actual.StepUsed);Assert.IsFalse(actual.AttackUsed);
            }
            flow.ChooseUnit("archer");flow.ClickCell(new SPos(1,6));
            Assert.AreEqual(new SPos(1,6),flow.State.Find("archer").Position,"No tutorial gate remains.");
            flow.ContinueFocusTutorial();
            Assert.AreEqual(new SPos(1,6),flow.State.Find("archer").Position,"A repeated callback must not restart free play.");
        }

        [Test] public void RandomRestartDoesNotEnableTheFixedFocusRoute()
        {
            flow.Restart(false);
            Assert.AreEqual(FocusTutorialStep.Off,flow.FocusStep);
            Assert.IsFalse(flow.FocusTutorialActive);
        }

        [Test] public void MissingAnchorEntersRecoveryInsteadOfAllowingProgress()
        {
            var before=flow.FocusStep;
            flow.ReportFocusAnchorMissing("mage");
            Assert.AreEqual(before,flow.FocusStep);
            Assert.IsNotEmpty(flow.FocusRecoveryReason);
            flow.ContinueFocusTutorial();
            Assert.AreEqual(before,flow.FocusStep,"A missing target must not silently advance the tutorial.");
        }

        [Test] public void MissingTaughtCardOpensRecoveryAndCanRestartTheTutorial()
        {
            flow.ChooseUnit("mage");
            flow.ContinueFocusTutorial();
            flow.ClickCell(new SPos(4,5));
            Assert.AreEqual(FocusTutorialStep.DragMove,flow.FocusStep);
            var move=flow.State.Hand.First(c=>c.Kind==SCard.Move);
            flow.State.Hand.Remove(move);
            flow.CheckFocusTutorialState();
            Assert.IsNotEmpty(flow.FocusRecoveryReason);
            Assert.IsFalse(flow.FocusAllowsCard(move.Id),"Recovery must block the now-missing card action.");
            flow.RequestRestartFocusTutorial();
            Assert.IsTrue(flow.FocusRestartConfirm);
            flow.ConfirmRestartFocusTutorial();
            Assert.AreEqual(FocusTutorialStep.SelectMage,flow.FocusStep);
            Assert.IsTrue(string.IsNullOrEmpty(flow.FocusRecoveryReason));
            Assert.AreEqual(5,flow.State.Hand.Count);
        }
    }
}
