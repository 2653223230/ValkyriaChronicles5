#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.AI;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate"), Category("VC5FeedbackFix")]
    public class Vc5ScoutTargetTests : Vc5LogicTestBase
    {
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void ScoutAI_ChoosesEmptySlot_AndActuallyMoves(int playerId, bool staleTrigger)
        {
            GameLogic logic = Create(playerId, out Game game, out Card scout);
            Card enemy = Vc5LogicTestHarness.PlaceOnBoard(game, 1 - playerId, "vc5_demo_cavalry", new Slot(2, 2, Slot.GetP(1 - playerId)));
            game.ability_triggerer = staleTrigger ? enemy.uid : null;
            AbilityData skill = scout.GetAbilities()[0];
            Assert.IsFalse(skill.CanTarget(game, scout, scout), "Movement skill must reject card targets");
            Assert.IsFalse(skill.CanTarget(game, scout, game.players[playerId]), "Movement skill must reject player targets");
            Assert.IsTrue(skill.HasValidSelectTarget(game, scout), "Pre-cast targets must use the scout itself");
            Slot origin = scout.slot;
            int mana = game.players[playerId].mana;
            AIAction cast = Vc5DemoAIPlanner.ChooseNextAction(game, playerId);
            Assert.AreEqual(GameAction.CastAbility, cast.type);
            logic.CastAbility(scout, skill);
            Vc5LogicTestHarness.FlushResolve(logic);
            AIAction select = Vc5DemoAIPlanner.ChooseNextAction(game, playerId);
            Assert.AreEqual(GameAction.SelectSlot, select.type);
            Assert.IsNull(game.GetSlotCard(select.slot));
            logic.SelectSlot(select.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(select.slot, scout.slot);
            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(origin, scout.slot));
            Assert.AreEqual(playerId, game.current_player, "Fast skill retains action priority");
            Assert.AreEqual(mana, game.players[playerId].mana);
            Assert.AreEqual(SelectorType.None, game.selector);
            Assert.IsFalse(game.CanCastAbility(scout, skill));
        }

        [Test]
        public void SurroundedScout_IsNotAnAIAction()
        {
            Create(0, out Game game, out Card scout);
            foreach (Slot slot in Vc5DemoGrid.NeighborSlots(scout.slot))
                Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_cavalry", slot);
            Assert.IsFalse(scout.GetAbilities()[0].HasValidSelectTarget(game, scout));
            Assert.AreEqual(GameAction.EndTurn, Vc5DemoAIPlanner.ChooseNextAction(game, 0).type);
        }

        [Test]
        public void QuickMoveCard_SecondSelectionMustAlsoBeASlot()
        {
            GameLogic logic = Create(0, out Game game, out Card scout);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_quick_move");
            logic.PlayCard(spell, scout.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(scout);
            Vc5LogicTestHarness.FlushResolve(logic);
            AIAction action = Vc5DemoAIPlanner.ChooseNextAction(game, 0);
            Assert.AreEqual(GameAction.SelectSlot, action.type);
        }

        static GameLogic Create(int player, out Game game, out Card scout)
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out game, Vc5DemoBootstrap.MobileAssaultDeckId, player, false);
            foreach (Player p in game.players) { p.cards_board.Clear(); p.cards_hand.Clear(); }
            scout = Vc5LogicTestHarness.PlaceOnBoard(game, player, "vc5_demo_scout", new Slot(3, 3, Slot.GetP(player)));
            return logic;
        }
    }
}
#endif
