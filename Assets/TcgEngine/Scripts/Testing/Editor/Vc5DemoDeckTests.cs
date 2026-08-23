#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DemoDeckTests : Vc5LogicTestBase
    {
        [Test]
        public void DemoDecks_AreRegisteredWithThreeHeroesAndTwentyCards()
        {
            AssertDeck(Vc5DemoBootstrap.MobileAssaultDeckId);
            AssertDeck(Vc5DemoBootstrap.RangedPressureDeckId);
        }

        [Test]
        public void MobileAssault_BasicAttack_UsesSelectedAllyAttack()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, Vc5DemoBootstrap.MobileAssaultDeckId);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);
            p0.cards_board.Clear();
            p1.cards_board.Clear();

            Card attacker = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_cavalry", new Slot(2, 2, Slot.GetP(0)));
            Card target = Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_guard", new Slot(7, 4, Slot.GetP(1)));
            Card attack = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_basic_attack");

            int hpBefore = target.GetHP();
            Vc5LogicTestHarness.PlayCardWithSelects(logic, attack, attacker.slot, attacker, target);

            AssertHp(target, hpBefore - attacker.GetAttack(), game, "普通攻击 should deal the selected ally attack value.");
        }

        [Test]
        public void RangedPressure_HighGround_MovesAndAddsTemporaryRange()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, Vc5DemoBootstrap.RangedPressureDeckId);
            Player p0 = game.GetPlayer(0);
            game.GetPlayer(1).cards_board.Clear();
            p0.cards_board.Clear();

            Card sniper = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_sniper", new Slot(2, 2, Slot.GetP(0)));
            Card highGround = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_high_ground");
            Slot targetSlot = new Slot(3, 2, Slot.GetP(0));

            logic.PlayCard(highGround, sniper.slot, skip_cost: true);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(sniper);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectSlot(targetSlot);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(targetSlot, sniper.slot, "占据高位 should move the selected ally to the chosen adjacent slot.");
            AssertStatus(game, sniper, StatusType.Vc5AttackRangeBonus, 1, "占据高位 should add +1 temporary attack range.");
        }

        [Test]
        public void ManualBoardMove_IsDisabledButCardEffectsCanStillMoveCharacters()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, Vc5DemoBootstrap.RangedPressureDeckId);
            Player p0 = game.GetPlayer(0);
            game.GetPlayer(1).cards_board.Clear();
            p0.cards_board.Clear();

            Card sniper = Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_demo_sniper", new Slot(2, 2, Slot.GetP(0)));
            Slot targetSlot = new Slot(3, 2, Slot.GetP(0));

            Assert.IsTrue(game.CanMoveCard(sniper, targetSlot, true), "底层移动能力仍应可用，供卡牌和技能效果调用。");
            Assert.IsFalse(game.CanManualMoveCard(sniper, targetSlot), "试玩版禁止通过点击棋盘直接移动棋子。");

            logic.MoveCard(sniper, targetSlot, true);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(targetSlot, sniper.slot, "卡牌或技能效果仍应可以通过 MoveCard 移动棋子。");
        }

        private static void AssertDeck(string deckId)
        {
            DeckData deck = DeckData.Get(deckId);
            Assert.NotNull(deck, "Deck missing: " + deckId);
            Assert.NotNull(deck.heroes, "Deck heroes missing: " + deckId);
            Assert.AreEqual(3, deck.heroes.Length, "Deck should deploy exactly three heroes: " + deckId);
            Assert.NotNull(deck.cards, "Deck cards missing: " + deckId);
            Assert.AreEqual(20, deck.cards.Length, "Deck should contain 20 playable cards: " + deckId);

            foreach (CardData hero in deck.heroes)
            {
                Assert.NotNull(hero, "Deck hero entry is null: " + deckId);
                Assert.IsTrue(hero.attack_Range >= 1, "Hero should have an attack range: " + hero.id);
                Assert.IsTrue(hero.move_Range >= 1, "Hero should have move range: " + hero.id);
            }
        }
    }
}
#endif
