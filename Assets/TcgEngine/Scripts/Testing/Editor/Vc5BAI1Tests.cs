#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5BAI1Tests : Vc5LogicTestBase
    {
        const string Prefix = "vc5_demo_bai1_";

        [Test]
        public void IndependentDeck_IsPlayableWithTwentyCardsAndOriginalBPortraits()
        {
            DeckData deck = DeckData.Get("deck_vc5_demo_steady_assault");
            Assert.NotNull(deck, "B-AI1 must be independently registered.");
            Assert.IsTrue(new UserDeckData(deck).IsValid());
            Assert.AreEqual(20, deck.cards.Length);
            Assert.AreEqual(3, deck.heroes.Length);
            Assert.AreSame(CardData.Get("vc5_demo_cavalry").art_full, CardData.Get(Prefix + "frontliner").art_full);
            Assert.IsFalse(deck.heroes.Any(h => DeckData.Get(Vc5DemoBootstrap.MobileAssaultDeckId).heroes.Contains(h)));
        }

        [Test]
        public void ShortCharge_SelectsDestinationThenEnemy_CancelDoesNotMoveOrSpend()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "flanker", 3, 3);
            Card enemy = Place(game, 1, "frontliner", 5, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "short_charge");
            int mana = game.players[0].mana;
            logic.PlayCard(spell, actor.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectSlot(new Slot(4, 3, actor.slot.p));
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(3, actor.slot.x, "Selecting a landing must not move before target confirmation.");
            logic.CancelSelection();
            Assert.AreEqual(mana, game.players[0].mana);
            Assert.Contains(spell, game.players[0].cards_hand);
            logic.PlayCard(spell, actor.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectSlot(new Slot(4, 3, actor.slot.p));
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(enemy);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(4, actor.slot.x);
            Assert.AreEqual(3, enemy.damage);
            Assert.AreEqual(mana - 2, game.players[0].mana);
            Assert.AreEqual(1, game.current_player);
        }

        [Test]
        public void ShortCharge_CannotTeleportToDistantEnemy()
        {
            Setup(out Game game);
            Card actor = Place(game, 0, "flanker", 3, 3);
            Place(game, 1, "frontliner", 7, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "short_charge");
            Assert.IsFalse(game.CanPlayCard(spell, actor.slot));
        }

        [Test]
        public void Frontliner_FirstMoveShieldDoesNotStackOrRepeat()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "frontliner", 3, 3);
            logic.MoveCard(actor, new Slot(4, 3, actor.slot.p), true, true);
            Assert.AreEqual(1, actor.r4_shield);
            actor.r4_shield = 0;
            logic.MoveCard(actor, new Slot(5, 3, actor.slot.p), true, true);
            Assert.AreEqual(0, actor.r4_shield);
        }

        [Test]
        public void ApprovedDurability_PreparedTwoHitThresholdsFinishEachShieldedUnit()
        {
            GameLogic logic = Setup(out Game game);
            Card attacker = Vc5LogicTestHarness.PlaceOnBoard(game, 1, Vc5R4Rules.Sniper,
                Vc5DemoGrid.ToPerspective(new Slot(2, 2, Slot.GetP(0)), Slot.GetP(1)));
            Card frontliner = Place(game, 0, "frontliner", 3, 2);
            Card flanker = Place(game, 0, "flanker", 4, 2);
            Card rifleman = Place(game, 0, "rifleman", 5, 2);
            foreach (Card target in new[] { frontliner, flanker, rifleman }) Vc5R4Rules.Shield(target, 2);

            logic.DamageCard(attacker, frontliner, 5);
            logic.DamageCard(attacker, flanker, 5);
            logic.DamageCard(attacker, rifleman, 4);
            Assert.IsTrue(game.IsOnBoard(frontliner), "The frontline role must survive one high-damage hit.");
            Assert.IsTrue(game.IsOnBoard(flanker));
            Assert.IsTrue(game.IsOnBoard(rifleman));

            logic.DamageCard(attacker, frontliner, 5);
            logic.DamageCard(attacker, flanker, 3);
            logic.DamageCard(attacker, rifleman, 3);
            Assert.IsFalse(game.IsOnBoard(frontliner), "8 HP + 2 shield should fall to 5 + 5 prepared damage.");
            Assert.IsFalse(game.IsOnBoard(flanker), "6 HP + 2 shield should fall to 5 + 3 prepared damage.");
            Assert.IsFalse(game.IsOnBoard(rifleman), "5 HP + 2 shield should fall to 4 + 3 prepared damage.");
        }

        static GameLogic Setup(out Game game)
        {
            var logic = Vc5LogicTestHarness.CreateLogic(out game, "deck_vc5_demo_command_r4", vc5TestMode: false);
            foreach (Player player in game.players) { player.cards_board.Clear(); player.cards_hand.Clear(); player.mana = 9; }
            game.current_player = 0;
            return logic;
        }

        [Test]
        public void RiflemanPreview_IncludesAdjacentAllyBonus_WithoutMutatingGame()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "rifleman", 3, 3);
            Place(game, 0, "frontliner", 5, 2);
            Card enemy = Place(game, 1, "frontliner", 5, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "suppression");
            // Preview the same explicit target that the player confirms.
            Vc5C3Preview preview = Vc5C3Preview.Build(game, spell, actor, null, enemy);
            Assert.AreEqual(4, preview.damage[enemy.uid]);
            Assert.AreEqual(0, enemy.damage);
            logic.PlayCard(spell, actor.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(enemy);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(4, enemy.damage);
        }

        [Test]
        public void ShortCharge_WatchKillStopsAttack_AndPreviewMatches()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "flanker", 3, 3);
            actor.damage = actor.CardData.hp - 1;
            Card enemy = Place(game, 1, "frontliner", 5, 3);
            enemy.r4_watch = true;
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "short_charge");
            Slot landing = new Slot(4, 3, actor.slot.p);
            Vc5C3Preview preview = Vc5C3Preview.Build(game, spell, actor, landing, enemy);
            Assert.AreEqual(1, preview.damage[actor.uid]);
            Assert.IsFalse(preview.damage.ContainsKey(enemy.uid));
            logic.PlayCard(spell, actor.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectSlot(landing);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(enemy);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.IsFalse(game.IsOnBoard(actor));
            Assert.AreEqual(0, enemy.damage);
        }

        static Card Place(Game game, int player, string suffix, int x, int y)
        {
            Assert.NotNull(CardData.Get(Prefix + suffix), "Missing B-AI1 registration");
            return Vc5LogicTestHarness.PlaceOnBoard(game, player, Prefix + suffix,
                Vc5DemoGrid.ToPerspective(new Slot(x, y, Slot.GetP(0)), Slot.GetP(player)));
        }

        [Test]
        public void AI_ChoosesAffordableQuickStepThenAttackOverLongMarch()
        {
            Setup(out Game game);
            Card actor = Place(game, 0, "flanker", 3, 3);
            Place(game, 1, "frontliner", 5, 3);
            Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "march");
            Card step = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "short_step");
            Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "basic_attack");
            game.players[0].mana = 2;
            var action = TcgEngine.AI.Vc5DemoAIPlanner.ChooseNextAction(game, 0);
            Assert.AreEqual(GameAction.PlayCard, action.type);
            Assert.AreEqual(step.uid, action.card_uid);
        }

        [Test]
        public void StationaryCharge_ClickingActorSelectsOrigin_AbsorbedHitKeepsMobileFire()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "flanker", 3, 3);
            Card enemy = Place(game, 1, "frontliner", 4, 3);
            actor.AddStatus(StatusType.Vc5C3MobileFire, 1, 0);
            Vc5R4Rules.Shield(enemy, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Prefix + "short_charge");
            logic.PlayCard(spell, actor.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(actor); // The occupied board cell receives a card click, not a slot click.
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(Prefix + "short_charge_target", game.selector_ability_id);
            logic.SelectCard(enemy);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(0, enemy.damage);
            Assert.AreEqual(0, enemy.r4_shield);
            Assert.IsTrue(actor.HasStatus(StatusType.Vc5C3MobileFire));
            Assert.AreEqual(3, actor.slot.x);
        }
    }
}
#endif
