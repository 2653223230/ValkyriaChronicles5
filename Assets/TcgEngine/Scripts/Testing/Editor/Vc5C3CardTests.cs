#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TcgEngine.Gameplay;
using TcgEngine.UI;
using TcgEngine.AI;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5C3CardTests : Vc5LogicTestBase
    {
        const string Prefix = "vc5_demo_c3_";
        const string DeckId = "deck_vc5_demo_ranged_pressure_c3";

        [Test]
        public void Deck_IsIndependent_AndContainsThreeHeroesAndTwentyCards()
        {
            Vc5LogicTestHarness.LoadGameData();
            DeckData deck = DeckData.Get(DeckId);
            Assert.NotNull(deck, "C3 must be registered independently of the original ranged deck.");
            Assert.AreEqual(3, deck.heroes.Length);
            Assert.AreEqual(20, deck.cards.Length);
            Assert.AreEqual(3, Vc5DemoMatchSetup.GetPlayableDemoDecks().Length);
            Assert.AreNotSame(deck, DeckData.Get(Vc5DemoBootstrap.RangedPressureDeckId));
        }

        [TestCase("tactical_move", 1, true)]
        [TestCase("forced_march", 2, false)]
        [TestCase("temp_calibration", 1, true)]
        [TestCase("scope_upgrade", 3, false)]
        [TestCase("fire_coverage", 3, false)]
        [TestCase("heavy_break", 2, false)]
        [TestCase("weakpoint_snipe", 3, false)]
        [TestCase("mobile_shot", 2, false)]
        public void Card_UsesDirectPlayTarget(string id, int cost, bool fast)
        {
            CardData card = CardData.Get(Prefix + id);
            Assert.NotNull(card);
            Assert.AreEqual(cost, card.mana);
            Assert.AreEqual(fast, card.fast_action);
            Assert.IsTrue(card.IsRequireTargetSpell());
        }

        [Test]
        public void TemporaryRange_DoesNotStackOrBecomePermanent()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 4, 3);
            Play(logic, game, actor, "scope_upgrade");
            Play(logic, game, actor, "scope_upgrade");
            Play(logic, game, actor, "temp_calibration");
            Play(logic, game, actor, "temp_calibration");
            Assert.AreEqual(5, Vc5DemoGrid.AttackRange(actor));
            actor.ReduceStatusDurations();
            Assert.AreEqual(4, Vc5DemoGrid.AttackRange(actor));
        }

        [Test]
        public void HeavyBreak_SelectsHighestCurrentHp_AndNeverCounters()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 4, 3);
            Card low = Place(game, 1, "fire_guard", 5, 3);
            low.damage = 3;
            Card high = Place(game, 1, "fire_guard", 6, 3);
            Play(logic, game, actor, "heavy_break");
            Assert.AreEqual(4, high.GetHP());
            Assert.AreEqual(4, low.GetHP());
            Assert.AreEqual(5, actor.GetHP());
            Assert.AreEqual(SelectorType.None, game.selector);
        }

        [Test]
        public void WeakpointSnipe_UsesExtendedRangeAndLowestCurrentHp()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 3, 3);
            Card high = Place(game, 1, "fire_guard", 4, 3);
            Card low = Place(game, 1, "fire_guard", 7, 3);
            low.damage = 3;
            Play(logic, game, actor, "weakpoint_snipe");
            Assert.AreEqual(2, low.GetHP());
            Assert.AreEqual(7, high.GetHP());
        }

        [Test]
        public void Coverage_UsesFullAttackAndMobileBonusForEveryTargetThenConsumesIt()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 3, 3);
            Card a = Place(game, 1, "fire_guard", 5, 3);
            Card b = Place(game, 1, "fire_guard", 4, 4);
            logic.MoveCard(actor, new Slot(4, 3, actor.slot.p), true, true);
            actor.ReduceStatusDurations(); // Mobile fire survives the round until used.
            Play(logic, game, actor, "fire_coverage");
            Assert.AreEqual(4, a.GetHP());
            Assert.AreEqual(4, b.GetHP());
            Play(logic, game, actor, "fire_coverage");
            Assert.AreEqual(2, a.GetHP());
            Assert.AreEqual(2, b.GetHP());
        }

        [Test]
        public void Sniper_BonusStopsAfterMovementAndReturnsNextRound()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "sniper", 4, 3);
            Card target = Place(game, 1, "fire_guard", 6, 3);
            Play(logic, game, actor, "fire_coverage");
            Assert.AreEqual(3, target.GetHP());
            target.damage = 0;
            logic.MoveCard(actor, new Slot(4, 4, actor.slot.p), true, true);
            Play(logic, game, actor, "fire_coverage");
            Assert.AreEqual(4, target.GetHP());
            actor.ReduceStatusDurations();
            target.damage = 0;
            Play(logic, game, actor, "fire_coverage");
            Assert.AreEqual(3, target.GetHP());
        }

        [Test]
        public void Guard_FirstActualMoveOnlyDealsOneDamageEachRound()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "fire_guard", 3, 3);
            Card target = Place(game, 1, "fire_guard", 5, 3);
            logic.MoveCard(actor, new Slot(4, 3, actor.slot.p), true, true);
            Assert.AreEqual(6, target.GetHP());
            logic.MoveCard(actor, new Slot(4, 4, actor.slot.p), true, true);
            Assert.AreEqual(6, target.GetHP());
            actor.ReduceStatusDurations();
            logic.MoveCard(actor, new Slot(4, 3, actor.slot.p), true, true);
            Assert.AreEqual(5, target.GetHP());
        }

        [TestCase("tactical_move", 1)]
        [TestCase("forced_march", 3)]
        public void PreciseMove_UsesActorMovementAndRejectsOccupiedDestination(string id, int distance)
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "sniper", 3, 3);
            Card enemy = Place(game, 1, "fire_guard", 4, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + id);
            logic.PlayCard(spell, actor.slot, true);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreNotEqual(SelectorType.None, game.selector);
            logic.SelectSlot(new Slot(4, 3, actor.slot.p));
            Assert.AreEqual(3, actor.slot.x);
            Slot destination = new Slot(3, 3 + (distance == 1 ? 1 : 2), actor.slot.p);
            logic.SelectSlot(destination);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(destination, actor.slot);
            Assert.AreEqual(1, actor.move_Range);
        }

        [Test]
        public void MobileShot_NoMovementWhenAlreadyInRange_OtherwiseMovesWithoutOverlap()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 3, 3);
            Card target = Place(game, 1, "fire_guard", 7, 3);
            Play(logic, game, actor, "mobile_shot");
            Assert.AreEqual(2, Vc5DemoGrid.HexDistance(actor.slot, target.slot));
            Assert.AreEqual(4, target.GetHP());
            Assert.IsTrue(Vc5DemoGrid.IsPlayableBoardCell(actor.slot));
            Assert.AreNotEqual(0, Vc5DemoGrid.HexDistance(actor.slot, target.slot));
            Slot position = actor.slot;
            Play(logic, game, actor, "mobile_shot");
            Assert.AreEqual(position, actor.slot);
            Assert.AreEqual(2, target.GetHP());
        }

        [Test]
        public void InvalidActor_NoEnemyOrEnemyActorDoesNotSpendManaOrCard()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 3, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "heavy_break");
            int mana = game.players[0].mana;
            Assert.IsFalse(game.CanPlayCard(spell, actor.slot));
            logic.PlayCard(spell, actor.slot);
            Assert.AreEqual(mana, game.players[0].mana);
            Assert.Contains(spell, game.players[0].cards_hand);
        }

        [Test]
        public void AI_PicksLegalActorEvenWhenStrongerAllyCannotReach()
        {
            GameLogic logic = Setup(out Game game);
            game.players[0].cards_hand.Clear();
            Place(game, 0, "sniper", 3, 1);
            Card actor = Place(game, 0, "mobile_ranger", 6, 3);
            Place(game, 1, "fire_guard", 8, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "heavy_break");
            AIAction action = Vc5DemoAIPlanner.ChooseNextAction(game, 0);
            Assert.AreEqual(GameAction.PlayCard, action.type);
            Assert.AreEqual(actor.slot, action.slot);
            Assert.AreEqual(spell.uid, action.card_uid);
        }

        static GameLogic Setup(out Game game)
        {
            Assert.NotNull(DeckData.Get(DeckId), "C3 is not registered.");
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out game, DeckId, vc5TestMode:false);
            foreach (Player player in game.players) { player.cards_board.Clear(); player.mana = 99; }
            return logic;
        }

        static Card Place(Game game, int player, string id, int x, int y)
        {
            Slot slot = Vc5DemoGrid.ToPerspective(new Slot(x, y, Slot.GetP(0)), Slot.GetP(player));
            return Vc5LogicTestHarness.PlaceOnBoard(game, player, Prefix + id, slot);
        }

        static void Play(GameLogic logic, Game game, Card actor, string id)
        {
            game.current_player = actor.player_id;
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, actor.player_id, Prefix + id);
            logic.PlayCard(spell, actor.slot, true);
            Vc5LogicTestHarness.FlushResolve(logic);
        }
    }
}
#endif
