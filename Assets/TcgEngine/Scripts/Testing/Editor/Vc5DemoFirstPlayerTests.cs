#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5DemoFirstPlayerTests : Vc5LogicTestBase
    {
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.MobileAssaultDeckId)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, Vc5DemoBootstrap.MobileAssaultDeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, Vc5DemoBootstrap.RangedPressureDeckId)]
        public void DemoSoloMatch_PlayerAlwaysStarts(string playerDeckId, string aiDeckId)
        {
            Game game = StartMatch(GameType.Solo, playerDeckId, aiDeckId);

            Assert.AreEqual(0, game.first_player);
            Assert.AreEqual(0, game.current_player);
        }

        [Test]
        public void MultiplayerMatch_KeepsExistingRandomFirstPlayerRule()
        {
            Game game = StartMatch(GameType.Multiplayer,
                Vc5DemoBootstrap.MobileAssaultDeckId,
                Vc5DemoBootstrap.RangedPressureDeckId);

            Assert.AreEqual(1, game.first_player,
                "Seed 0 chooses player 1, proving Multiplayer was not forced to player 0.");
        }

        [Test]
        public void SoloMatchWithNonDemoDeck_KeepsExistingRandomFirstPlayerRule()
        {
            Game game = CreateConfiguredGame(GameType.Solo,
                Vc5DemoBootstrap.MobileAssaultDeckId,
                Vc5DemoBootstrap.RangedPressureDeckId);
            game.players[1].deck = "non_demo_deck";

            GameLogic logic = CreateLogic(game);
            logic.StartGame();

            Assert.AreEqual(1, game.first_player,
                "Seed 0 chooses player 1, proving non-Demo Solo was not forced to player 0.");
        }

        private static Game StartMatch(GameType gameType, string playerDeckId, string aiDeckId)
        {
            Game game = CreateConfiguredGame(gameType, playerDeckId, aiDeckId);
            GameLogic logic = CreateLogic(game);
            logic.StartGame();
            return game;
        }

        private static Game CreateConfiguredGame(GameType gameType, string playerDeckId, string aiDeckId)
        {
            Vc5LogicTestHarness.LoadGameData();

            Game game = new Game("vc5_demo_first_player_test", 2);
            game.settings.game_type = gameType;

            GameLogic logic = new GameLogic(true);
            logic.SetData(game);
            logic.SetPlayerDeck(game.players[0], DeckData.Get(playerDeckId));
            logic.SetPlayerDeck(game.players[1], DeckData.Get(aiDeckId));
            return game;
        }

        private static GameLogic CreateLogic(Game game)
        {
            GameLogic logic = new GameLogic(true);
            logic.SetData(game);

            FieldInfo randomField = typeof(GameLogic).GetField("random",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(randomField);
            randomField.SetValue(logic, new Random(0));
            return logic;
        }
    }
}
#endif
