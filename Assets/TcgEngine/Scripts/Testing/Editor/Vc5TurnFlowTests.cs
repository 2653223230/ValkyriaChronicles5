#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using TcgEngine.Client;
using TcgEngine.Gameplay;
using TcgEngine.UI;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5TurnFlowTests : Vc5LogicTestBase
    {
        [Test]
        public void StartGame_BothPlayersStartAtConfiguredMana()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, vc5TestMode: false);
            Vc5LogicTestHarness.FlushResolve(logic);

            int expected = GameplayData.Get().mana_start;
            Assert.AreEqual(expected, game.GetPlayer(0).mana_max);
            Assert.AreEqual(expected, game.GetPlayer(0).mana);
            Assert.AreEqual(expected, game.GetPlayer(1).mana_max);
            Assert.AreEqual(expected, game.GetPlayer(1).mana);
        }

        [Test]
        public void StartNextTurn_WhenNextRoundBegins_BothPlayersGainOneMana()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, firstPlayer: 0, vc5TestMode: false);
            Vc5LogicTestHarness.FlushResolve(logic);

            int startMana = GameplayData.Get().mana_start;
            int expectedAfterGrowth = startMana + GameplayData.Get().mana_per_turn;

            game.current_player = 1;
            logic.StartNextTurn();
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(0, game.current_player);
            Assert.AreEqual(2, game.turn_count);
            Assert.AreEqual(expectedAfterGrowth, game.GetPlayer(0).mana_max);
            Assert.AreEqual(expectedAfterGrowth, game.GetPlayer(0).mana);
            Assert.AreEqual(expectedAfterGrowth, game.GetPlayer(1).mana_max);
            Assert.AreEqual(expectedAfterGrowth, game.GetPlayer(1).mana);
        }

        [Test]
        public void EndStage_InVc5RulesPassesCurrentPlayerToOpponent()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, firstPlayer: 0, vc5TestMode: false);
            Vc5LogicTestHarness.FlushResolve(logic);

            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.IsTrue(game.GetPlayer(0).EndTurn);
            Assert.IsFalse(game.GetPlayer(1).EndTurn);
            Assert.AreEqual(1, game.current_player);
            Assert.AreEqual(GamePhase.Main, game.phase);
        }

        [Test]
        public void TurnUi_UsesUnambiguousMainPassAndEndDiscardWording()
        {
            MethodInfo buttonText = typeof(GameUI).GetMethod(
                "GetVc5EndButtonText", BindingFlags.Public | BindingFlags.Static);
            MethodInfo statusText = typeof(GameUI).GetMethod(
                "GetVc5TurnStatusText", BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(buttonText, "GameUI must expose a deterministic phase-specific button label.");
            Assert.IsNotNull(statusText, "GameUI must expose deterministic phase status wording.");
            MethodInfo detailText = typeof(GameUI).GetMethod(
                "GetVc5TurnDetailText", BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(detailText, "GameUI must expose a separate short instruction line.");
            Assert.AreEqual("放弃行动", buttonText.Invoke(null, new object[] { GamePhase.Main }));
            Assert.AreEqual("完成弃牌", buttonText.Invoke(null, new object[] { GamePhase.EndDiscard }));
            Assert.AreEqual("我方行动", statusText.Invoke(null, new object[] { GamePhase.Main, true, false }));
            Assert.AreEqual("对手行动", statusText.Invoke(null, new object[] { GamePhase.Main, false, false }));
            Assert.AreEqual("弃牌阶段",
                statusText.Invoke(null, new object[] { GamePhase.EndDiscard, false, false }));
            Assert.AreEqual("等待对手",
                statusText.Invoke(null, new object[] { GamePhase.EndDiscard, false, true }));
            Assert.AreEqual("拖动手牌后松开即可弃置",
                detailText.Invoke(null, new object[] { GamePhase.EndDiscard, false, 3 }));
            Assert.AreEqual("弃牌已完成",
                detailText.Invoke(null, new object[] { GamePhase.EndDiscard, true, 3 }));
        }

        [Test]
        public void EndDiscard_RequiresDragDistanceAndDoesNotDiscardOnSimpleClick()
        {
            MethodInfo shouldDiscard = typeof(HandCard).GetMethod(
                "ShouldDiscardOnRelease", BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(shouldDiscard, "HandCard must expose deterministic release behavior for regression tests.");
            Assert.IsFalse((bool)shouldDiscard.Invoke(null,
                new object[] { GamePhase.EndDiscard, false, new Vector2(100f, 100f), new Vector2(100f, 100f) }));
            Assert.IsTrue((bool)shouldDiscard.Invoke(null,
                new object[] { GamePhase.EndDiscard, false, new Vector2(100f, 100f), new Vector2(125f, 100f) }));
            Assert.IsFalse((bool)shouldDiscard.Invoke(null,
                new object[] { GamePhase.Main, false, new Vector2(100f, 100f), new Vector2(125f, 100f) }));
            Assert.IsFalse((bool)shouldDiscard.Invoke(null,
                new object[] { GamePhase.EndDiscard, true, new Vector2(100f, 100f), new Vector2(125f, 100f) }));
        }

        [Test]
        public void EndStage_WhenBothPlayersPassesResolvesScoringAndEntersEndDiscard()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, firstPlayer: 0, vc5TestMode: false);
            Vc5LogicTestHarness.FlushResolve(logic);

            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.IsTrue(game.GetPlayer(0).EndTurn);
            Assert.IsTrue(game.GetPlayer(1).EndTurn);
            Assert.AreEqual(GamePhase.EndDiscard, game.phase);
        }

        [Test]
        public void EndDiscard_WhenBothPlayersPassesStartsNextRound()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, firstPlayer: 0, vc5TestMode: false);
            Vc5LogicTestHarness.FlushResolve(logic);

            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(GamePhase.EndDiscard, game.phase);

            logic.PassEndDiscard(game.GetPlayer(0));
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(GamePhase.EndDiscard, game.phase);

            logic.PassEndDiscard(game.GetPlayer(1));
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(GamePhase.Main, game.phase);
            Assert.AreEqual(2, game.turn_count);
        }

        [Test]
        public void FastAction_DoesNotConsumeMainAction_ButNormalCardDoes()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(
                out Game game, Vc5DemoBootstrap.MobileAssaultDeckId, vc5TestMode: false);
            Player player = game.GetPlayer(0);
            player.cards_board.Clear();
            game.GetPlayer(1).cards_board.Clear();
            player.mana = 10;
            player.mana_max = 10;
            Card scout = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_scout", new Slot(2, 2, Slot.GetP(0)));
            Card enemy = Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_guard", new Slot(7, 4, Slot.GetP(1)));

            Card quick = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_quick_move");
            logic.PlayCard(quick, scout.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(scout);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectSlot(new Slot(2, 3, Slot.GetP(0)));
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.IsFalse(player.main_action_used);
            Assert.AreEqual(0, game.current_player, "A fast action must keep the current action player.");

            Card attack = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_basic_attack");
            logic.PlayCard(attack, scout.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(scout);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(enemy);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.IsTrue(player.main_action_used);
            Assert.AreEqual(1, game.current_player, "A resolved normal action must pass action priority to the opponent.");
            Assert.IsFalse(player.EndTurn, "Taking a normal action must not count as passing for the round.");
        }

        [Test]
        public void NormalAction_WhenOpponentAlreadyPassed_KeepsPriorityWithActivePlayer()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(
                out Game game, Vc5DemoBootstrap.MobileAssaultDeckId, vc5TestMode: false);
            Player player = game.GetPlayer(0);
            Player opponent = game.GetPlayer(1);
            player.cards_board.Clear();
            opponent.cards_board.Clear();
            player.mana = 10;
            player.mana_max = 10;
            opponent.EndTurn = true;
            Card scout = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_scout", new Slot(2, 2, Slot.GetP(0)));
            Card enemy = Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_guard", new Slot(7, 4, Slot.GetP(1)));
            Card attack = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_basic_attack");

            logic.PlayCard(attack, scout.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(scout);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(enemy);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(0, game.current_player);
            Assert.IsFalse(player.main_action_used, "The unpassed player receives a fresh action opportunity.");
            Assert.IsFalse(player.EndTurn);
            Assert.IsTrue(opponent.EndTurn);
        }

        [Test]
        public void CancelSelection_RefundsCardManaAndMainAction()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(
                out Game game, Vc5DemoBootstrap.MobileAssaultDeckId, vc5TestMode: false);
            Player player = game.GetPlayer(0);
            player.cards_board.Clear();
            game.GetPlayer(1).cards_board.Clear();
            player.mana = 5;
            player.mana_max = 5;
            Card scout = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_scout", new Slot(2, 2, Slot.GetP(0)));
            Card attack = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_basic_attack");
            int handBefore = player.cards_hand.Count;

            logic.PlayCard(attack, scout.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(scout);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.IsTrue(player.main_action_used);

            logic.CancelSelection();
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(SelectorType.None, game.selector);
            Assert.AreEqual(5, player.mana);
            Assert.IsFalse(player.main_action_used);
            Assert.AreEqual(handBefore, player.cards_hand.Count);
            Assert.Contains(attack, player.cards_hand);
        }

        [Test]
        public void EndDiscard_AllowsDiscardingAChosenCardBeforePassing()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(
                out Game game, Vc5DemoBootstrap.MobileAssaultDeckId, vc5TestMode: false);
            Player player = game.GetPlayer(0);
            Card card = player.cards_hand[0];

            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(GamePhase.EndDiscard, game.phase);

            logic.DiscardEndPhaseCard(player, card);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.IsFalse(player.cards_hand.Contains(card));
            Assert.Contains(card, player.cards_discard);
            Assert.IsFalse(player.end_discard_passed, "Discarding does not implicitly pass the end-discard phase.");
        }
    }
}
#endif
