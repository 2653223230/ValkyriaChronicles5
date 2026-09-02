#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using TcgEngine.AI;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DemoAIMatchTests : Vc5LogicTestBase
    {
        private const int MatchesPerCombination = 10;
        private const int MaxDecisionSteps = 500;

        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.MobileAssaultDeckId)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, Vc5DemoBootstrap.MobileAssaultDeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, Vc5DemoBootstrap.RangedPressureDeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureC3DeckId, Vc5DemoBootstrap.MobileAssaultDeckId)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureC3DeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureC3DeckId, Vc5DemoBootstrap.RangedPressureDeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, Vc5DemoBootstrap.RangedPressureC3DeckId)]
        [TestCase(Vc5DemoBootstrap.RangedPressureC3DeckId, Vc5DemoBootstrap.RangedPressureC3DeckId)]
        public void DemoAI_CompletesTenMatchesWithoutGettingStuck(string deck0Id, string deck1Id)
        {
            int totalTurns = 0;
            int totalCardsPlayed = 0;
            int totalPasses = 0;
            Dictionary<string, int> reasons = new Dictionary<string, int>();

            for (int match = 0; match < MatchesPerCombination; match++)
            {
                GameLogic logic = CreateMatch(out Game game, deck0Id, deck1Id, match % 2);
                int cardsPlayed = 0;
                int passes = 0;
                int unchangedSteps = 0;
                List<string> recentActions = new List<string>();

                for (int step = 0; step < MaxDecisionSteps && !game.HasEnded(); step++)
                {
                    Vc5LogicTestHarness.FlushResolve(logic);
                    string before = Fingerprint(game);

                    if (game.phase == GamePhase.EndDiscard)
                    {
                        PassPendingEndDiscard(logic, game);
                    }
                    else
                    {
                        int playerId = game.selector != SelectorType.None
                            ? game.selector_player_id
                            : game.current_player;
                        AIAction action = Vc5DemoAIPlanner.ChooseNextAction(game, playerId);
                        Assert.NotNull(action, FailureMessage("Planner returned null", game, step, recentActions));

                        recentActions.Add($"P{playerId}:{action.GetText(game)}");
                        if (recentActions.Count > 12)
                            recentActions.RemoveAt(0);

                        if (action.type == GameAction.PlayCard)
                            cardsPlayed++;
                        if (action.type == GameAction.EndTurn)
                            passes++;

                        ExecuteAction(logic, game, playerId, action);
                    }

                    Vc5LogicTestHarness.FlushResolve(logic);
                    string after = Fingerprint(game);
                    unchangedSteps = before == after ? unchangedSteps + 1 : 0;
                    Assert.Less(unchangedSteps, 3,
                        FailureMessage("Three consecutive decisions did not change the game", game, step, recentActions));
                }

                Assert.IsTrue(game.HasEnded(), FailureMessage("Decision step limit exceeded", game, MaxDecisionSteps, recentActions));
                string reason = GetEndReason(game);
                Assert.AreNotEqual("unknown", reason, FailureMessage("Unexpected game-end reason", game, MaxDecisionSteps, recentActions));
                reasons[reason] = reasons.TryGetValue(reason, out int count) ? count + 1 : 1;
                totalTurns += game.turn_count;
                totalCardsPlayed += cardsPlayed;
                totalPasses += passes;
            }

            TestContext.WriteLine(
                $"{deck0Id} vs {deck1Id}: matches={MatchesPerCombination}, "
                + $"avgTurns={(float)totalTurns / MatchesPerCombination:0.0}, "
                + $"cardsPlayed={totalCardsPlayed}, passes={totalPasses}, "
                + $"reasons={FormatReasons(reasons)}");
        }

        private static GameLogic CreateMatch(out Game game, string deck0Id, string deck1Id, int firstPlayer)
        {
            Vc5LogicTestHarness.LoadGameData();
            DeckData deck0 = DeckData.Get(deck0Id);
            DeckData deck1 = DeckData.Get(deck1Id);
            Assert.NotNull(deck0, "Deck missing: " + deck0Id);
            Assert.NotNull(deck1, "Deck missing: " + deck1Id);

            game = new Game("vc5_ai_match", 2);
            GameLogic logic = new GameLogic(true);
            logic.SetData(game);
            logic.SetPlayerDeck(game.GetPlayer(0), deck0);
            logic.SetPlayerDeck(game.GetPlayer(1), deck1);
            game.GetPlayer(0).is_ai = true;
            game.GetPlayer(1).is_ai = true;
            logic.StartGame();
            game.first_player = firstPlayer;
            game.current_player = firstPlayer;
            logic.RefreshData();
            Vc5LogicTestHarness.FlushResolve(logic);
            return logic;
        }

        private static void PassPendingEndDiscard(GameLogic logic, Game game)
        {
            foreach (Player player in game.players)
            {
                if (!player.end_discard_passed)
                {
                    logic.PassEndDiscard(player);
                    Vc5LogicTestHarness.FlushResolve(logic);
                }
            }
        }

        private static void ExecuteAction(GameLogic logic, Game game, int playerId, AIAction action)
        {
            Player player = game.GetPlayer(playerId);
            if (action.type == GameAction.PlayCard)
                logic.PlayCard(game.GetCard(action.card_uid), action.slot);
            else if (action.type == GameAction.CastAbility)
                logic.CastAbility(game.GetCard(action.card_uid), AbilityData.Get(action.ability_id));
            else if (action.type == GameAction.SelectCard)
                logic.SelectCard(game.GetCard(action.target_uid));
            else if (action.type == GameAction.SelectSlot)
                logic.SelectSlot(action.slot);
            else if (action.type == GameAction.SelectChoice)
                logic.SelectChoice(action.value);
            else if (action.type == GameAction.SelectCost)
                logic.SelectCost(action.value);
            else if (action.type == GameAction.CancelSelect)
                logic.CancelSelection();
            else if (action.type == GameAction.EndTurn)
                logic.EndTurn();
            else
                Assert.Fail("Unsupported AI action type: " + action.type);
        }

        private static string Fingerprint(Game game)
        {
            List<string> parts = new List<string>
            {
                game.state.ToString(), game.phase.ToString(), game.selector.ToString(),
                game.current_player.ToString(), game.selector_player_id.ToString(),
                game.selector_ability_id ?? "", game.ability_triggerer ?? "", game.turn_count.ToString()
            };

            foreach (Player player in game.players)
            {
                parts.Add($"P{player.player_id}:{player.kill_count}:{player.mana}:{player.main_action_used}:"
                    + $"{player.EndTurn}:{player.end_discard_passed}:{player.cards_hand.Count}:{player.cards_deck.Count}");
                foreach (Card card in player.cards_board)
                    parts.Add($"{card.uid}:{card.slot.x}:{card.slot.y}:{card.GetHP()}");
            }
            return string.Join("|", parts);
        }

        private static string GetEndReason(Game game)
        {
            bool p0Score = game.GetPlayer(0).kill_count >= 9;
            bool p1Score = game.GetPlayer(1).kill_count >= 9;
            if (p0Score && p1Score)
                return "score_draw";
            if (p0Score)
                return "p0_score";
            if (p1Score)
                return "p1_score";
            if (game.GetPlayer(0).cards_deck.Count == 0)
                return "p0_deck_empty";
            if (game.GetPlayer(1).cards_deck.Count == 0)
                return "p1_deck_empty";
            return "unknown";
        }

        private static string FailureMessage(string reason, Game game, int step, List<string> recentActions = null)
        {
            string message = reason + $"; step={step}; turn={game.turn_count}; phase={game.phase}; selector={game.selector}; "
                + $"current={game.current_player}; scores={game.GetPlayer(0).kill_count}-{game.GetPlayer(1).kill_count}; "
                + $"hands={game.GetPlayer(0).cards_hand.Count}-{game.GetPlayer(1).cards_hand.Count}; "
                + $"decks={game.GetPlayer(0).cards_deck.Count}-{game.GetPlayer(1).cards_deck.Count}";
            if (recentActions != null)
                message += "; recent=" + string.Join(" -> ", recentActions);
            return message;
        }

        private static string FormatReasons(Dictionary<string, int> reasons)
        {
            List<string> values = new List<string>();
            foreach (KeyValuePair<string, int> entry in reasons)
                values.Add(entry.Key + "=" + entry.Value);
            return string.Join(",", values);
        }
    }
}
#endif
