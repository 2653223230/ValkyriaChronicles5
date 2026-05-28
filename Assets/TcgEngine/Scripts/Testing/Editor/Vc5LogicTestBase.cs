#if UNITY_EDITOR
using System;
using System.Diagnostics;
using NUnit.Framework;
using TcgEngine.Gameplay;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
    public abstract class Vc5LogicTestBase
    {
        protected Stopwatch testWatch;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Vc5TestReportWriter.BeginSession();
            Vc5LogicTestHarness.LoadGameData();
        }

        [SetUp]
        public void SetUp()
        {
            testWatch = Stopwatch.StartNew();
        }

        [TearDown]
        public void TearDown()
        {
            testWatch.Stop();
            var context = TestContext.CurrentContext;
            bool passed = context.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Passed;
            Vc5TestReportWriter.Record(
                context.Test.Name,
                passed,
                context.Result.Message,
                string.Empty,
                string.Empty,
                passed ? null : Vc5LogicTestHarness.LastSnapshot,
                testWatch.ElapsedMilliseconds
            );
            Vc5LogicTestHarness.LastSnapshot = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Vc5TestReportWriter.FlushToDisk();
        }

        protected static void AssertStatus(Game game, Card card, StatusType type, int expected, string hint = "")
        {
            int actual = card.GetStatusValue(type);
            if (actual == expected)
                return;

            Vc5GameSnapshot snapshot = Vc5LogicTestHarness.CaptureSnapshot(game);
            Vc5LogicTestHarness.LastSnapshot = snapshot;
            string message = string.IsNullOrEmpty(hint)
                ? $"Expected {type}={expected}, got {actual} on {card.card_id}"
                : hint;

            Assert.AreEqual(expected, actual, message + "\n" + BuildSnapshotHint(snapshot));
        }

        protected static void AssertHp(Card card, int expected, Game game, string hint = "")
        {
            int actual = card.GetHP();
            if (actual == expected)
                return;

            Vc5GameSnapshot snapshot = Vc5LogicTestHarness.CaptureSnapshot(game);
            Vc5LogicTestHarness.LastSnapshot = snapshot;
            string message = string.IsNullOrEmpty(hint)
                ? $"Expected HP={expected}, got {actual} on {card.card_id}"
                : hint;

            Assert.AreEqual(expected, actual, message + "\n" + BuildSnapshotHint(snapshot));
        }

        protected static void AssertBoardCount(Player player, int expected, Game game, string hint = "")
        {
            int actual = player.cards_board.Count;
            if (actual == expected)
                return;

            Vc5GameSnapshot snapshot = Vc5LogicTestHarness.CaptureSnapshot(game);
            Vc5LogicTestHarness.LastSnapshot = snapshot;
            string message = string.IsNullOrEmpty(hint)
                ? $"Expected board count={expected}, got {actual} for player {player.player_id}"
                : hint;

            Assert.AreEqual(expected, actual, message + "\n" + BuildSnapshotHint(snapshot));
        }

        private static string BuildSnapshotHint(Vc5GameSnapshot snapshot)
        {
            return "Snapshot saved to TestResults/vc5_logic_latest.json — turn="
                + snapshot.turn_count
                + " selector="
                + snapshot.selector;
        }
    }
}
#endif
