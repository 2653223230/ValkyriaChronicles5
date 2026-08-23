#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.AI;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DemoAITests : Vc5LogicTestBase
    {
        [Test]
        public void AIType20_CreatesVc5DemoAI()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out _, Vc5DemoBootstrap.MobileAssaultDeckId);
            AIPlayer ai = AIPlayer.Create((AIType)20, logic, 1, 10);

            Assert.NotNull(ai);
            Assert.AreEqual("AIPlayerVc5Demo", ai.GetType().Name);
        }

        [Test]
        public void Planner_ChoosesPlayableBasicAttackWhenItCanHit()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, Vc5DemoBootstrap.MobileAssaultDeckId);
            Player ai = game.GetPlayer(1);
            Player human = game.GetPlayer(0);
            ai.cards_board.Clear();
            human.cards_board.Clear();
            ai.cards_hand.Clear();
            ai.mana = 3;
            ai.mana_max = 3;

            Card attacker = Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_cavalry", new Slot(2, 2, Slot.GetP(1)));
            Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_guard", new Slot(7, 4, Slot.GetP(0)));
            Card attackCard = Vc5LogicTestHarness.GiveHandCard(game, 1, "vc5_demo_basic_attack");
            game.current_player = 1;
            game.selector = SelectorType.None;

            Type plannerType = Type.GetType("TcgEngine.AI.Vc5DemoAIPlanner, Assembly-CSharp");
            Assert.NotNull(plannerType, "Vc5DemoAIPlanner type should exist.");

            MethodInfo choose = plannerType.GetMethod("ChooseNextAction", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(choose, "Vc5DemoAIPlanner.ChooseNextAction should exist.");

            AIAction action = choose.Invoke(null, new object[] { game, 1 }) as AIAction;
            Assert.NotNull(action);
            Assert.AreEqual(GameAction.PlayCard, action.type);
            Assert.AreEqual(attackCard.uid, action.card_uid);
            Assert.AreEqual(attacker.slot, action.slot);
        }

        [Test]
        public void GameplayData_DefaultsToVc5DemoAIAndDemoAIDecks()
        {
            Vc5DemoBootstrap.Register();
            GameplayData data = GameplayData.Get();

            Assert.AreEqual(AIType.Vc5Demo, data.ai_type);
            AssertContainsDeck(data.ai_decks, Vc5DemoBootstrap.MobileAssaultDeckId);
            AssertContainsDeck(data.ai_decks, Vc5DemoBootstrap.RangedPressureDeckId);
        }

        [Test]
        public void Planner_CompletesBasicAttackSelectionChain()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, Vc5DemoBootstrap.MobileAssaultDeckId);
            Player ai = game.GetPlayer(1);
            Player human = game.GetPlayer(0);
            ai.cards_board.Clear();
            human.cards_board.Clear();
            ai.cards_hand.Clear();
            ai.mana = 3;
            ai.mana_max = 3;

            Card attacker = Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_cavalry", new Slot(2, 2, Slot.GetP(1)));
            Card target = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_guard", new Slot(7, 4, Slot.GetP(0)));
            Card attackCard = Vc5LogicTestHarness.GiveHandCard(game, 1, "vc5_demo_basic_attack");
            game.current_player = 1;

            logic.PlayCard(attackCard, attacker.slot, skip_cost: true);
            Vc5LogicTestHarness.FlushResolve(logic);

            AIAction chooseAttacker = Vc5DemoAIPlanner.ChooseNextAction(game, 1);
            Assert.AreEqual(GameAction.SelectCard, chooseAttacker.type);
            Assert.AreEqual(attacker.uid, chooseAttacker.target_uid);

            logic.SelectCard(attacker);
            Vc5LogicTestHarness.FlushResolve(logic);

            AIAction chooseTarget = Vc5DemoAIPlanner.ChooseNextAction(game, 1);
            Assert.AreEqual(GameAction.SelectCard, chooseTarget.type);
            Assert.AreEqual(target.uid, chooseTarget.target_uid);
        }

        [Test]
        public void Planner_DoesNotPlayPursuitWithoutDamagedEnemy()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, Vc5DemoBootstrap.MobileAssaultDeckId);
            Player ai = game.GetPlayer(1);
            Player human = game.GetPlayer(0);
            ai.cards_board.Clear();
            human.cards_board.Clear();
            ai.cards_hand.Clear();
            ai.mana = 3;
            ai.mana_max = 3;

            Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_assassin", new Slot(2, 2, Slot.GetP(1)));
            Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_guard", new Slot(5, 2, Slot.GetP(0)));
            Card pursuit = Vc5LogicTestHarness.GiveHandCard(game, 1, "vc5_demo_pursuit");
            game.current_player = 1;
            game.selector = SelectorType.None;

            AIAction action = Vc5DemoAIPlanner.ChooseNextAction(game, 1);

            Assert.AreEqual(GameAction.EndTurn, action.type);
            Assert.AreNotEqual(pursuit.uid, action.card_uid);
        }

        private static void AssertContainsDeck(DeckData[] decks, string deckId)
        {
            foreach (DeckData deck in decks)
            {
                if (deck != null && deck.id == deckId)
                    return;
            }

            Assert.Fail("Expected deck in AI deck list: " + deckId);
        }
    }
}
#endif
