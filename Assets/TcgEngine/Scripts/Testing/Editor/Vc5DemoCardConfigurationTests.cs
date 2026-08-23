#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DemoCardConfigurationTests : Vc5LogicTestBase
    {
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_quick_move", 1, true, 3)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_raid", 2, false, 3)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_close_assault", 2, false, 3)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_basic_attack", 1, false, 3)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_pursuit", 2, false, 2)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_retreat_2", 1, true, 2)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_charge_attack", 3, false, 2)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_full_speed_advance", 3, true, 1)]
        [TestCase(Vc5DemoBootstrap.MobileAssaultDeckId, "vc5_demo_decisive_charge", 4, false, 1)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_shoot", 1, false, 3)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_aimed_shot", 2, false, 3)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_fallback", 1, true, 3)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_high_ground", 1, false, 2)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_focus_fire", 2, false, 2)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_line_advance", 2, false, 2)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_ranged_suppression", 2, false, 2)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_protect_shooter", 1, false, 2)]
        [TestCase(Vc5DemoBootstrap.RangedPressureDeckId, "vc5_demo_volley", 4, false, 1)]
        public void DemoCard_MatchesDocumentedConfiguration(
            string deckId, string cardId, int mana, bool fastAction, int copies)
        {
            CardData card = CardData.Get(cardId);
            DeckData deck = DeckData.Get(deckId);

            Assert.NotNull(card, "Card missing: " + cardId);
            Assert.NotNull(deck, "Deck missing: " + deckId);
            Assert.AreEqual(mana, card.mana, "Mana mismatch: " + cardId);
            Assert.AreEqual(fastAction, card.fast_action, "Fast-action mismatch: " + cardId);
            Assert.AreEqual(copies, deck.cards.Count(item => item != null && item.id == cardId),
                "Deck copy count mismatch: " + cardId);
            Assert.IsNotEmpty(card.abilities, "Playable card needs at least one ability: " + cardId);
        }

        [TestCase("vc5_demo_cavalry", 2, 5, 3, 1)]
        [TestCase("vc5_demo_assassin", 3, 4, 2, 1)]
        [TestCase("vc5_demo_scout", 1, 5, 3, 2)]
        [TestCase("vc5_demo_sniper", 3, 4, 1, 3)]
        [TestCase("vc5_demo_rifleman", 2, 5, 2, 2)]
        [TestCase("vc5_demo_guard", 1, 7, 1, 1)]
        public void DemoCharacter_MatchesDocumentedStats(
            string cardId, int attack, int hp, int move, int range)
        {
            CardData card = CardData.Get(cardId);
            Assert.NotNull(card, "Character missing: " + cardId);
            Assert.AreEqual(attack, card.attack);
            Assert.AreEqual(hp, card.hp);
            Assert.AreEqual(move, card.move_Range);
            Assert.AreEqual(range, card.attack_Range);
        }
    }
}
#endif
