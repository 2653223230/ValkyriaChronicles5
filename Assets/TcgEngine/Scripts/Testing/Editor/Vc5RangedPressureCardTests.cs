#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5RangedPressureCardTests : Vc5LogicTestBase
    {
        [Test]
        public void Shoot_DealsSelectedShooterAttackAndRejectsOutOfRangeTarget()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card rifleman = Place(game, 0, "vc5_demo_rifleman", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 6, 4);
            int riflemanHp = rifleman.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_shoot");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, rifleman.slot, rifleman, target);
            Assert.AreEqual(target.CardData.hp - rifleman.GetAttack(), target.GetHP());
            Assert.AreEqual(riflemanHp, rifleman.GetHP(),
                "Shoot must not inherit the TCG template's automatic counter damage.");

            Card farTarget = Place(game, 1, "vc5_demo_guard", 4, 4);
            Card secondSpell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_shoot");
            BeginCardAndSelectAlly(logic, secondSpell, rifleman);
            logic.SelectCard(farTarget);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(farTarget.CardData.hp, farTarget.GetHP());
            Assert.AreNotEqual(SelectorType.None, game.selector);
            logic.CancelSelection();
        }

        [Test]
        public void AimedShot_CanHitAtAttackRangePlusOne()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card sniper = Place(game, 0, "vc5_demo_sniper", 1, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 5, 4);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_aimed_shot");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, sniper.slot, sniper, target);

            Assert.AreEqual(target.CardData.hp - sniper.GetAttack(), target.GetHP());
        }

        [Test]
        public void Fallback_MovesOneCellFartherFromNearestEnemy()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card rifleman = Place(game, 0, "vc5_demo_rifleman", 4, 3);
            Card enemy = Place(game, 1, "vc5_demo_guard", 5, 3);
            Slot before = rifleman.slot;
            int distanceBefore = Vc5DemoGrid.HexDistance(before, enemy.slot);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_fallback");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, rifleman.slot, rifleman);

            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(before, rifleman.slot));
            Assert.Greater(Vc5DemoGrid.HexDistance(rifleman.slot, enemy.slot), distanceBefore);
        }

        [Test]
        public void HighGround_RejectsOccupiedSlotAndRangeBonusExpiresAfterRound()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card sniper = Place(game, 0, "vc5_demo_sniper", 2, 2);
            Card blocker = Place(game, 0, "vc5_demo_guard", 3, 2);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_high_ground");

            BeginCardAndSelectAlly(logic, spell, sniper);
            logic.SelectSlot(blocker.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(new Slot(2, 2, Slot.GetP(0)), sniper.slot);
            Assert.AreNotEqual(SelectorType.None, game.selector);

            Slot destination = new Slot(2, 3, Slot.GetP(0));
            logic.SelectSlot(destination);
            Vc5LogicTestHarness.FlushResolve(logic);
            AssertStatus(game, sniper, StatusType.Vc5AttackRangeBonus, 1);

            FinishRound(logic, game);
            AssertStatus(game, sniper, StatusType.Vc5AttackRangeBonus, 0);
        }

        [Test]
        public void FocusFire_AddsOneDamageWhenAnotherAllyCanAttackTarget()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card sniper = Place(game, 0, "vc5_demo_sniper", 2, 2);
            Place(game, 0, "vc5_demo_rifleman", 2, 3);
            Card target = Place(game, 1, "vc5_demo_guard", 6, 4);
            int hp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_focus_fire");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, sniper.slot, sniper, target);

            Assert.AreEqual(hp - sniper.GetAttack() - 1, target.GetHP());
        }

        [Test]
        public void LineAdvance_MovesOneCellAndGrantsOneArmorForOneRound()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card guard = Place(game, 0, "vc5_demo_guard", 2, 2);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_line_advance");
            Slot destination = new Slot(2, 3, Slot.GetP(0));

            BeginCardAndSelectAlly(logic, spell, guard);
            logic.SelectSlot(destination);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(destination, guard.slot);
            AssertStatus(game, guard, StatusType.Armor, 1);
            FinishRound(logic, game);
            AssertStatus(game, guard, StatusType.Armor, 0);
        }

        [Test]
        public void RangedSuppression_DealsAttackPlusOne()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card rifleman = Place(game, 0, "vc5_demo_rifleman", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 6, 4);
            int hp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_ranged_suppression");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, rifleman.slot, rifleman, target);

            Assert.AreEqual(hp - rifleman.GetAttack() - 1, target.GetHP());
        }

        [Test]
        public void ProtectShooter_GrantsTwoArmorToSelectedAlly()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card sniper = Place(game, 0, "vc5_demo_sniper", 2, 2);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_protect_shooter");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, sniper.slot, sniper);

            AssertStatus(game, sniper, StatusType.Armor, 2);
        }

        [Test]
        public void Volley_EachAllyInRangeDealsItsOwnAttackOnce()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card sniper = Place(game, 0, "vc5_demo_sniper", 2, 2);
            Card rifleman = Place(game, 0, "vc5_demo_rifleman", 2, 3);
            Place(game, 0, "vc5_demo_guard", 1, 5);
            Card target = Place(game, 1, "vc5_demo_guard", 6, 4);
            int hp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_volley");

            logic.PlayCard(spell, sniper.slot, skip_cost: true);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(target);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(hp - sniper.GetAttack() - rifleman.GetAttack(), target.GetHP());
            Assert.AreEqual(SelectorType.None, game.selector);
        }

        private static GameLogic CreateLogic(out Game game)
        {
            return Vc5LogicTestHarness.CreateLogic(out game, Vc5DemoBootstrap.RangedPressureDeckId, vc5TestMode: false);
        }

        private static void ClearBoard(Game game)
        {
            game.GetPlayer(0).cards_board.Clear();
            game.GetPlayer(1).cards_board.Clear();
        }

        private static Card Place(Game game, int playerId, string cardId, int x, int y)
        {
            return Vc5LogicTestHarness.PlaceOnBoard(game, playerId, cardId, new Slot(x, y, Slot.GetP(playerId)));
        }

        private static void BeginCardAndSelectAlly(GameLogic logic, Card spell, Card ally)
        {
            logic.PlayCard(spell, ally.slot, skip_cost: true);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectCard(ally);
            Vc5LogicTestHarness.FlushResolve(logic);
        }

        private static void FinishRound(GameLogic logic, Game game)
        {
            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.EndStage();
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.PassEndDiscard(game.GetPlayer(0));
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.PassEndDiscard(game.GetPlayer(1));
            Vc5LogicTestHarness.FlushResolve(logic);
        }
    }
}
#endif
