#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Gameplay;

namespace TcgEngine.Testing.Editor
{
    public class Vc5SlimeLogicTests : Vc5LogicTestBase
    {
        [Test]
        public void DamageCard_ReducesTargetHP()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);

            Card attacker = Vc5LogicTestHarness.GetFirstBoardHero(p0);
            Card target = Vc5LogicTestHarness.GetFirstBoardHero(p1);
            Assert.NotNull(attacker);
            Assert.NotNull(target);

            int hpBefore = target.GetHP();
            logic.DamageCard(attacker, target, 3, spell_damage: true);
            Vc5LogicTestHarness.FlushResolve(logic);

            AssertHp(target, hpBefore - 3, game, "Direct damage should reduce HP by 3");
        }

        [Test]
        public void SlimeSpray_AppliesTwoSlimeToEnemy()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);

            Card spray = Vc5LogicTestHarness.FindHandCard(p0, "vc5_slime_spray");
            Card caster = Vc5LogicTestHarness.GetFirstBoardHero(p0);
            Card enemy = Vc5LogicTestHarness.GetFirstBoardHero(p1);
            Assert.NotNull(spray);
            Assert.NotNull(caster);
            Assert.NotNull(enemy);

            Slot castSlot = new Slot(caster.slot.x, caster.slot.y, caster.slot.p);
            Vc5LogicTestHarness.PlayCardWithSelects(logic, spray, castSlot, caster, enemy);

            AssertStatus(game, enemy, StatusType.Slime, 2, "粘液喷射应对敌方施加2层粘液");
        }

        [Test]
        public void SlimeAttach_OnEnemy_AppliesSlimeAndRooted()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);

            Card attach = Vc5LogicTestHarness.FindHandCard(p0, "vc5_slime_attach");
            Card caster = Vc5LogicTestHarness.GetFirstBoardHero(p0);
            Card enemy = Vc5LogicTestHarness.GetFirstBoardHero(p1);
            Assert.NotNull(attach);
            Assert.NotNull(caster);
            Assert.NotNull(enemy);

            Slot castSlot = new Slot(caster.slot.x, caster.slot.y, caster.slot.p);
            Vc5LogicTestHarness.PlayCardWithSelects(logic, attach, castSlot, caster, enemy);

            AssertStatus(game, enemy, StatusType.Slime, 1, "粘液附着应对敌方施加1层粘液");
            AssertStatus(game, enemy, StatusType.Rooted, 1, "粘液附着应对敌方施加无法移动");
        }

        [Test]
        public void SlimeAttach_OnAllySlime_GivesSharpInstead()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);

            Card attach = Vc5LogicTestHarness.FindHandCard(p0, "vc5_slime_attach");
            Card ally = Vc5LogicTestHarness.FindBoardCard(p0, "vc5_hero_slime_corrosive");
            if (ally == null)
                ally = Vc5LogicTestHarness.GetFirstBoardHero(p0);

            Assert.NotNull(attach);
            Assert.NotNull(ally);
            Assert.IsTrue(ally.HasTrait("slime") || ally.CardData.HasTrait("slime"), "Caster ally should have slime trait");

            Slot castSlot = new Slot(ally.slot.x, ally.slot.y, ally.slot.p);
            Vc5LogicTestHarness.PlayCardWithSelects(logic, attach, castSlot, ally, ally);

            AssertStatus(game, ally, StatusType.Sharp, 1, "对己方黏黏使用粘液附着应获得1层尖锐");
        }

        [Test]
        public void Summon_SlimeSpawn_AddsUnitToBoard()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            int boardBefore = p0.cards_board.Count;

            Slot slot = new Slot(5, 3, Slot.GetP(0));
            CardData spawnData = CardData.Get("vc5_slime_spawn");
            Assert.NotNull(spawnData);

            Card summoned = logic.SummonCard(p0, spawnData, VariantData.GetDefault(), slot);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.NotNull(summoned);
            AssertBoardCount(p0, boardBefore + 1, game, "SummonCard should add黏黏幼崽 to board");
            Assert.AreEqual("vc5_slime_spawn", summoned.card_id);
        }

        [Test]
        public void SlimeSplit_ConsumesSlimeAndSummonsSpawn()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game);
            Player p0 = game.GetPlayer(0);
            Player p1 = game.GetPlayer(1);

            Card enemy = Vc5LogicTestHarness.GetFirstBoardHero(p1);
            Assert.NotNull(enemy);
            enemy.AddStatus(StatusType.Slime, 4, 0);

            int spawnBefore = CountBoardById(p1, "vc5_slime_spawn");
            Card split = Vc5LogicTestHarness.FindHandCard(p0, "vc5_slime_split_card");
            Assert.NotNull(split);

            Slot castSlot = new Slot(enemy.slot.x, enemy.slot.y, enemy.slot.p);
            Vc5LogicTestHarness.PlayCardWithSelects(logic, split, castSlot, enemy);
            Vc5LogicTestHarness.FlushResolve(logic);

            Assert.AreEqual(0, enemy.GetStatusValue(StatusType.Slime), "分裂应消耗全部粘液层数");
            Assert.Greater(CountBoardById(p1, "vc5_slime_spawn"), spawnBefore, "4层粘液应至少召唤1只幼崽");
        }

        private static int CountBoardById(Player player, string cardId)
        {
            int count = 0;
            foreach (Card card in player.cards_board)
            {
                if (card.card_id == cardId)
                    count++;
            }
            return count;
        }
    }
}
#endif
