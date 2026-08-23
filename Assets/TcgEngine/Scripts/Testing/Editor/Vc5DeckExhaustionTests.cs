#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DeckExhaustionTests : Vc5LogicTestBase
    {
        [Test]
        public void DrawCard_WhenDeckIsEmptyAndHandHasSpace_LosesImmediately()
        {
            GameLogic logic = CreateStandardLogic(out Game game);
            Player player = game.GetPlayer(0);
            player.cards_deck.Clear();
            player.cards_hand.Clear();

            logic.DrawCard(player);

            Assert.AreEqual(GameState.GameEnded, game.state);
            Assert.AreEqual(1, game.current_player, "The opponent must be recorded as the winner.");
        }

        [Test]
        public void DrawCard_WhenHandIsFullAndDeckIsEmpty_DoesNotLose()
        {
            GameLogic logic = CreateStandardLogic(out Game game);
            Player player = game.GetPlayer(0);
            player.cards_deck.Clear();
            while (player.cards_hand.Count < GameplayData.Get().cards_max)
                Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_quick_move");

            logic.DrawCard(player);

            Assert.AreEqual(GameState.Play, game.state,
                "A full hand means no draw can occur, so an empty deck must not cause a loss.");
        }

        [Test]
        public void DrawMultiple_WhenDeckRunsOutMidDraw_DrawsAvailableCardThenLoses()
        {
            GameLogic logic = CreateStandardLogic(out Game game);
            Player player = game.GetPlayer(0);
            Card onlyCard = player.cards_deck[0];
            player.cards_deck.Clear();
            player.cards_deck.Add(onlyCard);
            player.cards_hand.Clear();

            logic.DrawCard(player, 3);

            Assert.AreEqual(1, player.cards_hand.Count);
            Assert.AreSame(onlyCard, player.cards_hand[0]);
            Assert.AreEqual(GameState.GameEnded, game.state);
            Assert.AreEqual(1, game.current_player);
        }

        [Test]
        public void StartTurn_RefillsEachHandToConfiguredFiveCards()
        {
            GameLogic logic = CreateStandardLogic(out Game game);
            foreach (Player player in game.players)
            {
                while (player.cards_hand.Count > 2)
                {
                    Card card = player.cards_hand[player.cards_hand.Count - 1];
                    player.cards_hand.RemoveAt(player.cards_hand.Count - 1);
                    player.cards_deck.Add(card);
                }
            }

            logic.StartTurn();
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(GameplayData.Get().cards_per_turn, game.GetPlayer(0).cards_hand.Count);
            Assert.AreEqual(GameplayData.Get().cards_per_turn, game.GetPlayer(1).cards_hand.Count);
        }

        private static GameLogic CreateStandardLogic(out Game game)
        {
            return Vc5LogicTestHarness.CreateLogic(
                out game,
                Vc5DemoBootstrap.MobileAssaultDeckId,
                vc5TestMode: false);
        }
    }
}
#endif
