#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5C3PreviewTests : Vc5LogicTestBase
    {
        [Test]
        public void RuntimePreviewOverlay_IsAvailableForGameUI()
        {
            Assert.NotNull(typeof(Game).Assembly.GetType("TcgEngine.UI.Vc5C3PreviewOverlay"));
        }

        [Test]
        public void Preview_GuardMoveKillThenRetarget_MatchesResolutionWithoutMutatingGame()
        {
            GameLogic logic = Setup(out Game game);
            Card guard = Place(game, 0, "fire_guard", 3, 3);
            Card first = Place(game, 1, "fire_guard", 6, 3);
            first.damage = 6;
            Card second = Place(game, 1, "fire_guard", 5, 4);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5C3Rules.Prefix + "mobile_shot");
            Slot initial = guard.slot;
            Vc5C3Preview preview = Vc5C3Preview.Build(game, spell, guard);
            Assert.IsTrue(preview.plan.valid);
            Assert.AreEqual(initial, guard.slot);
            Assert.AreEqual(1, first.GetHP());
            Assert.AreEqual(7, second.GetHP());
            Assert.IsFalse(guard.HasStatus(StatusType.Vc5C3GuardMoveUsed));
            Assert.AreEqual(1, preview.damage[first.uid]);
            Assert.AreEqual(1, preview.damage[second.uid]);
            logic.PlayCard(spell, guard.slot, true);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(preview.plan.destination, guard.slot);
            Assert.IsFalse(game.IsOnBoard(first));
            Assert.IsTrue(game.IsInDiscard(first));
            Assert.AreEqual(3, game.players[0].kill_count);
            Assert.AreEqual(6, second.GetHP());
        }

        [Test]
        public void Preview_AccountsForArmorShellAndMobileBonus_WithoutConsumingLiveStates()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 3, 3);
            Card armored = Place(game, 1, "fire_guard", 5, 3);
            armored.AddStatus(StatusType.Armor, 1, 1);
            Card shielded = Place(game, 1, "fire_guard", 4, 4);
            shielded.AddStatus(StatusType.Shell, 1, 1);
            logic.MoveCard(actor, new Slot(4, 3, actor.slot.p), true, true);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5C3Rules.Prefix + "fire_coverage");
            Vc5C3Preview preview = Vc5C3Preview.Build(game, spell, actor);
            Assert.AreEqual(2, preview.damage[armored.uid]);
            Assert.AreEqual(0, preview.damage[shielded.uid]);
            Assert.IsTrue(actor.HasStatus(StatusType.Vc5C3MobileFire));
            Assert.IsTrue(shielded.HasStatus(StatusType.Shell));
            logic.PlayCard(spell, actor.slot, true);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(5, armored.GetHP());
            Assert.AreEqual(7, shielded.GetHP());
            Assert.IsFalse(shielded.HasStatus(StatusType.Shell));
        }

        [Test]
        public void Preview_PreciseMoveIncludesGuardPassiveAndSelectedRoute()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "fire_guard", 3, 3);
            Card enemy = Place(game, 1, "fire_guard", 5, 3);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5C3Rules.Prefix + "tactical_move");
            Slot destination = new Slot(4, 3, actor.slot.p);
            Vc5C3Preview preview = Vc5C3Preview.Build(game, spell, actor, destination);
            Assert.AreEqual(2, preview.plan.path.Count);
            Assert.AreEqual(1, preview.damage[enemy.uid]);
            Assert.AreEqual(7, enemy.GetHP());
        }

        [Test]
        public void Movement_CannotCrossOccupiedRingOrUseNonexistentCells()
        {
            Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 5, 3);
            foreach (Slot neighbor in Vc5DemoGrid.NeighborSlots(actor.slot))
                Place(game, 0, "fire_guard", neighbor.x, neighbor.y);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5C3Rules.Prefix + "forced_march");
            Assert.IsFalse(Vc5C3Rules.Plan(game, spell, actor).valid);
            Assert.IsFalse(Vc5C3Rules.Plan(game, spell, actor, new Slot(1, 1, actor.slot.p)).valid);
        }

        [Test]
        public void VisualRoute_AfterNetworkRefreshStillAvoidsBlockerAndReachesDestination()
        {
            Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 5, 3);
            Card blocker = Place(game, 1, "fire_guard", 4, 3);
            Slot previous = new Slot(3, 3, actor.slot.p);
            var route = Vc5C3Rules.VisualPath(game, actor, previous);
            Assert.Greater(route.Count, 3);
            Assert.AreEqual(previous, route[0]);
            Assert.AreEqual(actor.slot, route[route.Count - 1]);
            for (int i = 1; i < route.Count; i++)
            {
                Assert.AreEqual(1, Vc5DemoGrid.HexDistance(route[i - 1], route[i]));
                Assert.Greater(Vc5DemoGrid.HexDistance(route[i], blocker.slot), 0);
                Assert.IsTrue(Vc5DemoGrid.IsPlayableBoardCell(route[i]));
            }
        }

        [Test]
        public void NearestTarget_TiesUseCurrentHpThenStableBoardOrder()
        {
            Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 4, 3);
            Card a = Place(game, 1, "fire_guard", 5, 3);
            Card b = Place(game, 1, "fire_guard", 4, 4);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5C3Rules.Prefix + "mobile_shot");
            Assert.AreEqual(a.uid, Vc5C3Rules.Plan(game, spell, actor).targets[0].uid);
            b.damage = 1;
            Assert.AreEqual(b.uid, Vc5C3Rules.Plan(game, spell, actor).targets[0].uid);
        }

        [Test]
        public void BoardValidity_IsIdenticalAcrossPlayerPerspectives()
        {
            for (int x = 1; x <= 10; x++)
                for (int y = 1; y <= 5; y++)
                {
                    Slot cell = new Slot(x, y, Slot.GetP(0));
                    Slot mirrored = Vc5DemoGrid.ToPerspective(cell, Slot.GetP(1));
                    Assert.AreEqual(Vc5DemoGrid.IsPlayableBoardCell(cell), Vc5DemoGrid.IsPlayableBoardCell(mirrored), $"Physical cell {x},{y}");
                }
        }

        [Test]
        public void PlayerOnePaths_StayOnRealBoardAndReachMirroredEdges()
        {
            Setup(out Game game);
            Card actor = Place(game, 1, "mobile_ranger", 5, 3);
            var paths = Vc5C3Rules.Reachable(game, actor, 34);
            Assert.AreEqual(34, paths.Count);
            foreach (Slot cell in paths.Keys)
                Assert.IsTrue(Vc5DemoGrid.IsPlayableBoardCell(Vc5DemoGrid.ToPerspective(cell, Slot.GetP(0))), $"Off-board player-one cell {cell.x},{cell.y}");
            Assert.IsTrue(paths.ContainsKey(Vc5DemoGrid.ToPerspective(new Slot(9, 1, Slot.GetP(0)), Slot.GetP(1))));
        }

        [TestCase(StatusType.Shell)]
        [TestCase(StatusType.Armor)]
        public void Ranger_PreservesMobileFireWhenEveryTargetBlocksAllDamage(StatusType protection)
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 4, 3);
            actor.AddStatus(StatusType.Vc5C3MobileFire, 1, 0);
            Card enemy = Place(game, 1, "fire_guard", 5, 3);
            enemy.AddStatus(protection, 9, 1);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5C3Rules.Prefix + "fire_coverage");
            var preview = Vc5C3Preview.Build(game, spell, actor);
            Assert.AreEqual(0, preview.damage[enemy.uid]);
            logic.PlayCard(spell, actor.slot, true);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(7, enemy.GetHP());
            Assert.IsTrue(actor.HasStatus(StatusType.Vc5C3MobileFire), "No damage was dealt, so the charge must remain.");
        }

        static GameLogic Setup(out Game game)
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out game, Vc5DemoBootstrap.RangedPressureC3DeckId, vc5TestMode:false);
            foreach (Player p in game.players) { p.cards_board.Clear(); p.mana = 99; }
            return logic;
        }
        static Card Place(Game game, int player, string id, int x, int y)
        {
            Slot slot = Vc5DemoGrid.ToPerspective(new Slot(x, y, Slot.GetP(0)), Slot.GetP(player));
            return Vc5LogicTestHarness.PlaceOnBoard(game, player, Vc5C3Rules.Prefix + id, slot);
        }
    }
}
#endif
