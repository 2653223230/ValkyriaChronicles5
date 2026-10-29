using System.Linq;
using NUnit.Framework;
using VC5PvE;

namespace VC5PvE.Tests
{
    public class CampaignRulesTests
    {
        [Test]
        public void NewRunStartsWithOriginalSeventeenCardDeckAndNoGrowth()
        {
            var run = CampaignRules.NewRun(41);
            Assert.AreEqual(1, run.LevelIndex);
            Assert.AreEqual(41, run.Seed);
            Assert.AreEqual(17, run.Deck.Count);
            Assert.AreEqual(0, run.Talents.Values.Sum(values => values.Count));
        }

        [Test]
        public void TalentDefinitionsExposeTheApprovedRoleAndGuaranteedCardPairs()
        {
            Assert.AreEqual(2, CampaignRules.TalentsFor(UnitRole.Warrior).Count);
            Assert.AreEqual(2, CampaignRules.TalentsFor(UnitRole.Mage).Count);
            Assert.AreEqual(2, CampaignRules.TalentsFor(UnitRole.Support).Count);
            Assert.AreEqual(CardKind.AllyCover, CampaignRules.Talent(TalentKind.CounterReady).RewardCard);
            Assert.AreEqual(CardKind.RelayStrike, CampaignRules.Talent(TalentKind.FlankingStrike).RewardCard);
            Assert.AreEqual(CardKind.RelayStrike, CampaignRules.Talent(TalentKind.MarkFlow).RewardCard);
            Assert.AreEqual(CardKind.RecoverTactics, CampaignRules.Talent(TalentKind.NeutralMastery).RewardCard);
            Assert.AreEqual(CardKind.AllyCover, CampaignRules.Talent(TalentKind.SupportFlow).RewardCard);
            Assert.AreEqual(CardKind.RecoverTactics, CampaignRules.Talent(TalentKind.TacticalRecall).RewardCard);
        }

        [Test]
        public void CampOffersAreUniqueRelevantAndCannotBeClaimedTwice()
        {
            var run = CampaignRules.NewRun(42);
            Assert.IsTrue(CampaignRules.BeginCamp(run));
            Assert.IsFalse(CampaignRules.Advance(run));
            Assert.IsTrue(CampaignRules.ChooseTalent(run, TalentKind.CounterReady));
            Assert.AreEqual(3, run.Offers.Count);
            Assert.AreEqual(3, run.Offers.Distinct().Count());
            Assert.IsTrue(run.Offers.Contains(CardKind.AllyCover));
            Assert.IsFalse(CampaignRules.ChooseTalent(run, TalentKind.CounterReady));
            Assert.IsTrue(CampaignRules.ChooseReward(run, null));
            Assert.IsFalse(CampaignRules.ChooseReward(run, null));
            Assert.IsFalse(CampaignRules.Advance(run));
        }

        [Test]
        public void DeckMaintenanceUpgradesOrDeletesOneEntityAndKeepsFifteenCardFloor()
        {
            var run = CampaignRules.NewRun(43);
            Assert.IsTrue(CampaignRules.BeginCamp(run));
            Assert.IsTrue(CampaignRules.ChooseTalent(run, TalentKind.CounterReady));
            Assert.IsTrue(CampaignRules.ChooseReward(run, null));
            var card = run.Deck[0];
            var otherCopy = run.Deck.First(c => c.Kind == card.Kind && c.Id != card.Id);
            Assert.IsTrue(CampaignRules.MaintainDeck(run, card.Id, true));
            Assert.IsTrue(run.Deck.Single(c => c.Id == card.Id).Upgraded);
            Assert.IsFalse(run.Deck.Single(c => c.Id == otherCopy.Id).Upgraded);
            Assert.AreEqual("升级：稳步推进＋", run.GrowthHistory.Last());
            Assert.IsFalse(CampaignRules.MaintainDeck(run, card.Id, true));

            var deletable = run.Deck.First(c => c.Id != card.Id);
            Assert.IsTrue(CampaignRules.Advance(run));

            Assert.IsTrue(CampaignRules.BeginCamp(run));
            Assert.IsFalse(CampaignRules.ChooseTalent(run, TalentKind.CounterReady));
            Assert.IsTrue(CampaignRules.ChooseTalent(run, TalentKind.FlankingStrike));
            Assert.IsTrue(CampaignRules.ChooseReward(run, null));
            Assert.IsTrue(CampaignRules.MaintainDeck(run, deletable.Id, false));
            Assert.AreEqual(16, run.Deck.Count);
            Assert.IsTrue(run.Deck.Any(c => c.Kind == deletable.Kind && c.Id != deletable.Id));
            Assert.AreEqual("删牌：" + Definitions.Card(deletable.Kind).Name, run.GrowthHistory.Last());

            var floorRun = CampaignRules.NewRun(45);
            floorRun.Deck.RemoveRange(15, floorRun.Deck.Count - 15);
            floorRun.CampStarted = true;
            floorRun.CampStep = 2;
            Assert.IsFalse(CampaignRules.MaintainDeck(floorRun, floorRun.Deck[0].Id, false));
        }

        [Test]
        public void GrowthHistoryUsesChineseTalentAndCardNamesInsteadOfInternalIds()
        {
            var run = CampaignRules.NewRun(47);
            Assert.IsTrue(CampaignRules.BeginCamp(run));
            Assert.IsTrue(CampaignRules.ChooseTalent(run, TalentKind.CounterReady));
            Assert.AreEqual("天赋：反攻准备", run.GrowthHistory[0]);
            Assert.IsTrue(CampaignRules.ChooseReward(run, CardKind.AllyCover));
            Assert.AreEqual("新卡：协防", run.GrowthHistory[1]);
            Assert.IsFalse(run.GrowthHistory.Any(entry => entry.Contains("AllyCover") || entry.Contains("reward-")));
        }

        [Test]
        public void RepeatedRewardsCreateSeparatePhysicalCardInstancesAcrossCamps()
        {
            var run = CampaignRules.NewRun(48);
            Assert.IsTrue(CampaignRules.BeginCamp(run));
            Assert.IsTrue(CampaignRules.ChooseTalent(run, TalentKind.CounterReady));
            Assert.IsTrue(CampaignRules.ChooseReward(run, CardKind.AllyCover));
            Assert.IsTrue(CampaignRules.MaintainDeck(run, null, false));
            Assert.IsTrue(CampaignRules.Advance(run));

            Assert.IsTrue(CampaignRules.BeginCamp(run));
            Assert.IsTrue(CampaignRules.ChooseTalent(run, TalentKind.SupportFlow));
            Assert.IsTrue(CampaignRules.ChooseReward(run, CardKind.AllyCover));
            Assert.AreEqual(2, run.Deck.Count(c => c.Kind == CardKind.AllyCover));
            Assert.AreEqual(2, run.Deck.Where(c => c.Kind == CardKind.AllyCover).Select(c => c.Id).Distinct().Count());
        }

        [Test]
        public void CreateBattleRestoresUnitsInheritsTalentsAndPrioritizesUpgradedFixedOpening()
        {
            var run = CampaignRules.NewRun(44);
            var card = run.Deck.First(c => c.Kind == CardKind.Advance && c.Id.EndsWith("03"));
            card.Upgraded = true;
            run.Talents[UnitRole.Warrior].Add(TalentKind.CounterReady);
            var battle = CampaignRules.CreateBattle(run);
            Assert.AreEqual(1, battle.LevelIndex);
            Assert.AreEqual(6, battle.Hand.Count);
            Assert.AreEqual(CardKind.Advance, battle.Hand[0].Kind);
            Assert.AreEqual(card.Id, battle.Hand[0].Id);
            Assert.IsTrue(battle.Hand[0].Upgraded);
            Assert.AreEqual(1, battle.FindUnit("warrior").Talents.Count);
            Assert.AreEqual(battle.FindUnit("warrior").MaxHp, battle.FindUnit("warrior").Hp);
            Assert.AreEqual(17, battle.Hand.Count + battle.DrawPile.Count);
            battle.Hand[0].Upgraded = false;
            Assert.IsTrue(run.Deck.Single(c => c.Id == card.Id).Upgraded);

            var withoutSparkMark = CampaignRules.NewRun(46);
            withoutSparkMark.Deck.RemoveAll(c => c.Kind == CardKind.SparkMark);
            var fallbackBattle = CampaignRules.CreateBattle(withoutSparkMark);
            Assert.AreEqual(6, fallbackBattle.Hand.Count);
            Assert.IsFalse(fallbackBattle.Hand.Any(c => c.Kind == CardKind.SparkMark));
        }

        [Test]
        public void LevelFactoryUsesApprovedThreeMapCoordinatesAndFacilities()
        {
            var first = BattleFactory.CreateLevel(1, 51);
            Assert.AreEqual(1, first.LevelIndex);
            Assert.AreEqual(0, first.Hand.Count);
            Assert.AreEqual(0, first.DrawPile.Count);
            Assert.AreEqual(new GridPos(3, 3), first.FindUnit("guard-a").Position);
            Assert.AreEqual(new GridPos(5, 4), first.FindUnit("guard-b").Position);
            Assert.AreEqual(new GridPos(5, 1), first.FindUnit("shooter").Position);
            Assert.IsTrue(first.Obstacles.Contains(new GridPos(2, 2)));
            Assert.IsTrue(first.Obstacles.Contains(new GridPos(2, 3)));
            Assert.IsTrue(first.Obstacles.Contains(new GridPos(4, 2)));
            Assert.IsTrue(first.Obstacles.Contains(new GridPos(4, 3)));

            var second = BattleFactory.CreateLevel(2, 52);
            Assert.AreEqual(2, second.LevelIndex);
            Assert.AreEqual(new GridPos(6, 2), second.FindUnit("guard-c").Position);
            Assert.IsTrue(second.FindUnit("pillar-d4").IsFacility);
            Assert.AreEqual(new GridPos(3, 1), second.FindUnit("pillar-d4").Position);
            Assert.AreEqual("guard-a", second.FindUnit("pillar-d4").BoundUnitId);
            Assert.AreEqual(new GridPos(6, 1), second.FindUnit("pillar-g3").Position);
            Assert.AreEqual("guard-c", second.FindUnit("pillar-g3").BoundUnitId);
            Assert.AreEqual(1, second.FindUnit("guard-a").Shield);

            var third = BattleFactory.CreateLevel(3, 53);
            Assert.AreEqual(3, third.LevelIndex);
            Assert.AreEqual(new GridPos(4, 1), third.FindUnit("boss").Position);
            Assert.AreEqual(24, third.FindUnit("boss").Hp);
            Assert.IsTrue(third.FindUnit("resonance-anchor-b3").IsFacility);
            Assert.AreEqual(new GridPos(1, 2), third.FindUnit("resonance-anchor-b3").Position);
            Assert.AreEqual(new GridPos(6, 2), third.FindUnit("resonance-anchor-g3").Position);
            Assert.IsTrue(third.Obstacles.Contains(new GridPos(3, 2)));
            Assert.IsTrue(third.Obstacles.Contains(new GridPos(3, 3)));
            Assert.IsTrue(third.Obstacles.Contains(new GridPos(4, 3)));
            Assert.AreEqual(null, BattleFactory.CreateLevel(4, 54));
        }
    }
}
