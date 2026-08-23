#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5VictoryAndScoringTests : Vc5LogicTestBase
    {
        [Test]
        public void KillEnemyCharacter_AwardsThreePointsAndEndsAtNine()
        {
            GameLogic logic = CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);
            p0.kill_count = 6;
            p0.cards_board.Clear();
            p1.cards_board.Clear();
            Card attacker = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_cavalry", new Slot(2, 2, Slot.GetP(0)));
            Card target = Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_guard", new Slot(3, 2, Slot.GetP(1)));

            logic.KillCard(attacker, target);

            Assert.AreEqual(9, p0.kill_count);
            Assert.AreEqual(GameState.GameEnded, game.state);
            Assert.AreEqual(0, game.current_player);
            Assert.Contains(target, p1.cards_discard);
        }

        [Test]
        public void SelfInflictedDeath_AwardsThreePointsToOpponent()
        {
            GameLogic logic = CreateLogic(out Game game);
            Player p1 = game.GetPlayer(1);
            Card target = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_scout", new Slot(2, 2, Slot.GetP(0)));

            logic.KillCard(null, target);

            Assert.AreEqual(3, p1.kill_count);
        }

        [Test]
        public void ScoringPhase_WhenLeaderReachesNine_EndsBeforeDiscardPhase()
        {
            GameLogic logic = CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);
            p0.kill_count = 7;
            p0.cards_board.Clear();
            p1.cards_board.Clear();
            Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_scout", new Slot(6, 3, Slot.GetP(0)));

            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(9, p0.kill_count);
            Assert.AreEqual(GameState.GameEnded, game.state);
            Assert.AreEqual(GamePhase.None, game.phase);
            Assert.AreEqual(0, game.current_player);
        }

        [Test]
        public void ScoringPhase_WhenBothReachNine_EndsAsDraw()
        {
            GameLogic logic = CreateLogic(out Game game);
            game.GetPlayer(0).kill_count = 8;
            game.GetPlayer(1).kill_count = 8;
            game.GetPlayer(0).cards_board.Clear();
            game.GetPlayer(1).cards_board.Clear();

            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(GameState.GameEnded, game.state);
            Assert.AreEqual(-1, game.current_player, "-1 represents a draw with no winning player.");
        }

        private static GameLogic CreateLogic(out Game game)
        {
            return Vc5LogicTestHarness.CreateLogic(
                out game,
                Vc5DemoBootstrap.MobileAssaultDeckId,
                vc5TestMode: false);
        }
    }
}
#endif
