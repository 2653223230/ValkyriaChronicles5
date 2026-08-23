#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5ScoringZoneTests : Vc5LogicTestBase
    {
        [Test]
        public void ScoringZone_ContainsAllDesignDocCells()
        {
            Assert.AreEqual(7, Vc5ScoringZone.GetCells().Count);
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(6, 3));
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(6, 2));
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(5, 4));
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(5, 3));
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(5, 2));
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(4, 4));
            Assert.IsTrue(Vc5ScoringZone.IsScoringCell(4, 3));
            Assert.IsFalse(Vc5ScoringZone.IsScoringCell(3, 2));
            Assert.IsFalse(Vc5ScoringZone.IsScoringCell(4, 2));
            Assert.IsFalse(Vc5ScoringZone.IsScoringCell(3, 4));
            Assert.IsFalse(Vc5ScoringZone.IsScoringCell(3, 3));
        }

        [Test]
        public void ResolveScoringZone_MoreCharacters_AwardsTwoPoints()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);

            Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_hero_slime_hard", new Slot(6, 3, Slot.GetP(0)));
            Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_hero_slime_blood", new Slot(5, 2, Slot.GetP(0)));
            Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_hero_slime_corrosive", new Slot(4, 4, Slot.GetP(1)));

            logic.ResolveScoringZone();

            Assert.AreEqual(2, p0.kill_count, "得分区人数多者应+2");
            Assert.AreEqual(0, p1.kill_count);
        }

        [Test]
        public void ResolveScoringZone_Tie_AwardsOneEach()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);

            Vc5LogicTestHarness.PlaceOnBoard(game, 0, "vc5_hero_slime_hard", new Slot(6, 3, Slot.GetP(0)));
            Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_hero_slime_blood", new Slot(4, 3, Slot.GetP(1)));

            logic.ResolveScoringZone();

            Assert.AreEqual(1, p0.kill_count);
            Assert.AreEqual(1, p1.kill_count);
        }

        [Test]
        public void ResolveScoringZone_EmptyTie_AwardsOneEach()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);

            logic.ResolveScoringZone();

            Assert.AreEqual(1, p0.kill_count, "双方均为0时也各+1");
            Assert.AreEqual(1, p1.kill_count);
        }
    }
}
#endif
