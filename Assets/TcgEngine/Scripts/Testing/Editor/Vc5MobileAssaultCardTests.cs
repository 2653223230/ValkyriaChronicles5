#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5MobileAssaultCardTests : Vc5LogicTestBase
    {
        [Test]
        public void QuickMove_MovesOneCellAndRejectsOccupiedCell()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card scout = Place(game, 0, "vc5_demo_scout", 2, 2);
            Card blocker = Place(game, 0, "vc5_demo_cavalry", 3, 2);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_quick_move");

            BeginCardAndSelectAlly(logic, spell, scout);
            logic.SelectSlot(blocker.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(new Slot(2, 2, Slot.GetP(0)), scout.slot);
            Assert.AreNotEqual(SelectorType.None, game.selector, "Invalid slot must keep target selection active.");

            Slot destination = new Slot(2, 3, Slot.GetP(0));
            logic.SelectSlot(destination);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(destination, scout.slot);
            Assert.AreEqual(SelectorType.None, game.selector);
        }

        [Test]
        public void Raid_MovesTowardActualScoringZoneByMoveRange()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card cavalry = Place(game, 0, "vc5_demo_cavalry", 4, 2);
            Slot before = cavalry.slot;
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_raid");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, cavalry.slot, cavalry);

            Assert.AreNotEqual(before, cavalry.slot);
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(cavalry.slot.x, cavalry.slot.y),
                "Raid must enter the real seven-cell scoring zone when it is within move range.");
            Assert.LessOrEqual(Vc5DemoGrid.HexDistance(before, cavalry.slot), cavalry.CardData.move_Range);
        }

        [Test]
        public void CloseAssault_MovesAdjacentWithoutDamage()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_cavalry", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 5, 2);
            int hp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_close_assault");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, mover.slot, mover, target);

            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(mover.slot, target.slot));
            Assert.AreEqual(hp, target.GetHP());
        }

        [Test]
        public void CloseAssault_CrossPlayerTarget_MovesAdjacentInDisplayedBoardDirection()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_cavalry", 3, 1);
            Card target = Place(game, 1, "vc5_demo_guard", 3, 1);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_close_assault");

            Assert.IsTrue(Vc5DemoGrid.FindBestAdjacentSlot(game, mover, target, -1, out Slot predicted));
            Assert.AreEqual(new Slot(6, 5, Slot.GetP(0)), predicted);
            logic.PlayCard(spell, mover.slot, skip_cost: true);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(SelectorType.SelectTarget, game.selector, "Playing the card must ask for an allied mover.");
            logic.SelectCard(mover);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(SelectorType.SelectTarget, game.selector, "Selecting the mover must ask for an enemy target.");
            logic.SelectCard(target);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(SelectorType.None, game.selector);

            Slot displayedTarget = new Slot(7, 5, Slot.GetP(0));
            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(mover.slot, displayedTarget),
                "The enemy slot must be rotated into the mover's board perspective before choosing an adjacent cell.");
        }

        [Test]
        public void CloseAssault_SkipsAdjacentCellOccupiedByOpponentInDisplayedBoard()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_cavalry", 3, 1);
            Card target = Place(game, 1, "vc5_demo_guard", 3, 1);
            Card blocker = Place(game, 1, "vc5_demo_rifleman", 4, 1);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_close_assault");
            Slot blockerDisplayedSlot = Vc5DemoGrid.ToPerspective(blocker.slot, mover.slot.p);

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, mover.slot, mover, target);

            Assert.AreNotEqual(blockerDisplayedSlot, mover.slot,
                "A logical slot on the other player side may still occupy the same displayed board cell.");
            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(mover.slot, target.slot));
        }

        [Test]
        public void CloseAssault_WhenAllDisplayedAdjacentCellsOccupied_RejectsTarget()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_cavalry", 3, 1);
            Card target = Place(game, 1, "vc5_demo_guard", 3, 1);
            Slot displayedTarget = Vc5DemoGrid.ToPerspective(target.slot, mover.slot.p);
            int blockerCount = 0;
            foreach (Slot displayedNeighbor in Vc5DemoGrid.NeighborSlots(displayedTarget))
            {
                if (!displayedNeighbor.IsValid() || displayedNeighbor.IsPlayerSlot())
                    continue;

                Slot opponentSlot = Vc5DemoGrid.ToPerspective(displayedNeighbor, Slot.GetP(1));
                Place(game, 1, "vc5_demo_rifleman", opponentSlot.x, opponentSlot.y);
                blockerCount++;
            }
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_close_assault");

            Assert.Greater(blockerCount, 0, "The test target must have at least one playable adjacent cell.");
            BeginCardAndSelectAlly(logic, spell, mover);
            logic.SelectCard(target);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(SelectorType.SelectTarget, game.selector,
                "An enemy with no physically empty adjacent cell must remain an invalid target.");
            Assert.AreEqual(new Slot(3, 1, Slot.GetP(0)), mover.slot);
            logic.CancelSelection();
        }

        [Test]
        public void DisplayedOccupiedCell_RejectsGenericMoveAndBoardCardPlay()
        {
            CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_cavalry", 3, 1);
            Card blocker = Place(game, 1, "vc5_demo_guard", 4, 1);
            Slot occupiedDisplayedSlot = Vc5DemoGrid.ToPerspective(blocker.slot, mover.slot.p);
            Card characterInHand = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_scout");

            Assert.IsFalse(game.CanMoveCard(mover, occupiedDisplayedSlot, skip_cost: true, ignore_range: true),
                "Generic movement must reject a cell occupied by an opponent in the displayed board.");
            Assert.IsFalse(game.CanPlayCard(characterInHand, occupiedDisplayedSlot, skip_cost: true),
                "Playing a board card must reject a cell occupied by an opponent in the displayed board.");
        }

        [Test]
        public void MissingPhysicalBoardCell_RejectsGenericMoveAndBoardCardPlay()
        {
            CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_cavalry", 7, 4);
            Card characterInHand = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_scout");
            Slot missingSceneCell = new Slot(9, 3, Slot.GetP(0));

            Assert.IsTrue(missingSceneCell.IsValid(),
                "This regression requires a coordinate accepted by the legacy rectangular Slot bounds.");
            Assert.IsFalse(game.CanMoveCard(mover, missingSceneCell, skip_cost: true, ignore_range: true),
                "Generic movement must reject a coordinate without a BoardSlot in Game.unity.");
            Assert.IsFalse(game.CanPlayCard(characterInHand, missingSceneCell, skip_cost: true),
                "Playing a board card must reject a coordinate without a BoardSlot in Game.unity.");
        }

        [Test]
        public void CloseAssault_SkipsAdjacentCoordinateWithoutPhysicalBoardSlot()
        {
            CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_scout", 7, 5);
            Card target = Place(game, 1, "vc5_demo_guard", 2, 3);
            Place(game, 0, "vc5_demo_cavalry", 8, 4);
            Place(game, 0, "vc5_demo_assassin", 7, 4);

            Assert.IsTrue(Vc5DemoGrid.FindBestAdjacentSlot(game, mover, target, -1, out Slot predicted));
            Assert.AreEqual(new Slot(7, 3, Slot.GetP(0)), predicted,
                "Close assault must skip (9,3), which is inside Slot bounds but absent from Game.unity.");
        }

        [Test]
        public void BasicAttack_RejectsEnemyOutsideSelectedAttackRange()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card attacker = Place(game, 0, "vc5_demo_cavalry", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 5, 2);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_basic_attack");

            BeginCardAndSelectAlly(logic, spell, attacker);
            logic.SelectCard(target);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(target.CardData.hp, target.GetHP());
            Assert.AreNotEqual(SelectorType.None, game.selector);
            logic.CancelSelection();
        }

        [Test]
        public void BasicAttack_DealsDamageWithoutAutomaticCounterattack()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card attacker = Place(game, 0, "vc5_demo_cavalry", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 7, 4);
            int attackerHp = attacker.GetHP();
            int targetHp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_basic_attack");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, attacker.slot, attacker, target);

            Assert.AreEqual(targetHp - attacker.GetAttack(), target.GetHP());
            Assert.AreEqual(attackerHp, attacker.GetHP(),
                "VC5 Demo attacks must not inherit the TCG template's automatic counter damage.");
        }

        [Test]
        public void Pursuit_OnlyAcceptsDamagedEnemyAndMovesAdjacent()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_assassin", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 5, 2);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_pursuit");

            BeginCardAndSelectAlly(logic, spell, mover);
            logic.SelectCard(target);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreNotEqual(SelectorType.None, game.selector, "Undamaged enemy must be rejected.");

            target.damage = 1;
            logic.SelectCard(target);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(mover.slot, target.slot));
            Assert.AreEqual(SelectorType.None, game.selector);
        }

        [Test]
        public void Retreat_MovesAtMostTwoCellsFartherFromEnemy()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card mover = Place(game, 0, "vc5_demo_scout", 4, 3);
            Card enemy = Place(game, 1, "vc5_demo_guard", 5, 3);
            Slot before = mover.slot;
            int distanceBefore = Vc5DemoGrid.HexDistance(before, enemy.slot);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_retreat_2");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, mover.slot, mover);

            Assert.Greater(Vc5DemoGrid.HexDistance(mover.slot, enemy.slot), distanceBefore);
            Assert.LessOrEqual(Vc5DemoGrid.HexDistance(before, mover.slot), 2);
        }

        [Test]
        public void ChargeAttack_MovesAtMostTwoCellsAndDealsAttackDamage()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card attacker = Place(game, 0, "vc5_demo_cavalry", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 6, 4);
            Slot before = attacker.slot;
            int attackerHp = attacker.GetHP();
            int hp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_charge_attack");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, attacker.slot, attacker, target);

            Assert.LessOrEqual(Vc5DemoGrid.HexDistance(before, attacker.slot), 2);
            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(attacker.slot, target.slot));
            Assert.AreEqual(hp - attacker.GetAttack(), target.GetHP());
            Assert.AreEqual(attackerHp, attacker.GetHP(),
                "Charge Attack must deal its listed damage without automatic counter damage.");
        }

        [Test]
        public void FullSpeedAdvance_MovesEveryAllyOneCellTowardScoring()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card first = Place(game, 0, "vc5_demo_cavalry", 2, 1);
            Card second = Place(game, 0, "vc5_demo_scout", 2, 4);
            Slot firstBefore = first.slot;
            Slot secondBefore = second.slot;
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_full_speed_advance");

            logic.PlayCard(spell, first.slot, skip_cost: true);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(firstBefore, first.slot));
            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(secondBefore, second.slot));
            Assert.AreEqual(SelectorType.None, game.selector);
        }

        [Test]
        public void DecisiveCharge_MovesAdjacentAndDealsAttackPlusOne()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card attacker = Place(game, 0, "vc5_demo_assassin", 2, 2);
            Card target = Place(game, 1, "vc5_demo_guard", 5, 2);
            int hp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_decisive_charge");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, attacker.slot, attacker, target);

            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(attacker.slot, target.slot));
            Assert.AreEqual(hp - attacker.GetAttack() - 1, target.GetHP());
        }

        [Test]
        public void DecisiveCharge_CrossPlayerTarget_MovesInDisplayedDirectionAndDealsDamage()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card attacker = Place(game, 0, "vc5_demo_assassin", 3, 1);
            Card target = Place(game, 1, "vc5_demo_guard", 3, 1);
            int hp = target.GetHP();
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_decisive_charge");

            Vc5LogicTestHarness.PlayCardWithSelects(logic, spell, attacker.slot, attacker, target);

            Slot displayedTarget = new Slot(7, 5, Slot.GetP(0));
            Assert.AreEqual(1, Vc5DemoGrid.HexDistance(attacker.slot, displayedTarget));
            Assert.AreEqual(hp - attacker.GetAttack() - 1, target.GetHP());
        }

        [Test]
        public void ScoutQuickMove_IsFastFreeAndLimitedToOncePerTurn()
        {
            GameLogic logic = CreateLogic(out Game game);
            ClearBoard(game);
            Card scout = Place(game, 0, "vc5_demo_scout", 2, 2);
            AbilityData ability = scout.CardData.abilities[0];
            Slot destination = new Slot(2, 3, Slot.GetP(0));

            Assert.AreEqual(0, ability.mana_cost);
            Assert.IsTrue(ability.fast_action);
            Assert.AreEqual(1, ability.uses_per_turn);
            logic.CastAbility(scout, ability);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectSlot(destination);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(destination, scout.slot);
            Assert.IsFalse(game.CanCastAbility(scout, ability), "Scout quick move must be unavailable after one use.");
        }

        private static GameLogic CreateLogic(out Game game)
        {
            return Vc5LogicTestHarness.CreateLogic(out game, Vc5DemoBootstrap.MobileAssaultDeckId, vc5TestMode: false);
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
    }
}
#endif
