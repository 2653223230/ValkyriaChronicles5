using NUnit.Framework;
using TcgEngine.UI;

namespace TcgEngine.Tests
{
    [Category("VC5DemoGate")]
    public class Vc5DemoBattleFeedbackTests
    {
        [TestCase(2, 1, 2, 0)]
        [TestCase(1, 2, 0, 2)]
        [TestCase(2, 2, 1, 1)]
        [TestCase(0, 0, 1, 1)]
        public void ScoreForecastMatchesAuthoritativeScoringRule(
            int localCount, int opponentCount, int expectedLocal, int expectedOpponent)
        {
            Vc5DemoBattleFeedbackModel.GetScoreAwards(
                localCount, opponentCount, out int localAward, out int opponentAward);

            Assert.AreEqual(expectedLocal, localAward);
            Assert.AreEqual(expectedOpponent, opponentAward);
        }

        [Test]
        public void ScoreSummaryShowsScoresCountsAndForecast()
        {
            string summary = Vc5DemoBattleFeedbackModel.BuildScoreSummary(4, 6, 2, 1);

            StringAssert.Contains("我方 4/9", summary);
            StringAssert.Contains("AI 6/9", summary);
            StringAssert.Contains("得分区  我方 2 | AI 1", summary);
            StringAssert.Contains("若现在结算：我方 +2，AI +0", summary);
        }

        [Test]
        public void ActionLogKeepsOnlyLatestThreeEntries()
        {
            Vc5DemoBattleFeedbackModel model = new Vc5DemoBattleFeedbackModel(3);

            model.AddAction("行动一");
            model.AddAction("行动二");
            model.AddAction("行动三");
            model.AddAction("行动四");

            CollectionAssert.AreEqual(
                new[] { "行动二", "行动三", "行动四" },
                model.GetActions());
        }

        [Test]
        public void AiCardMessageNamesCardAndManaCost()
        {
            Assert.AreEqual(
                "AI 打出【普通攻击】（1费）",
                Vc5DemoBattleFeedbackModel.BuildAiCardMessage("普通攻击", 1));
        }

        [Test]
        public void CardSummaryUsesOneReadableLine()
        {
            Assert.AreEqual(
                "选择己方角色，向敌人移动。",
                Vc5DemoBattleFeedbackModel.BuildShortDescription(
                    "<b>选择己方角色</b>，\n向敌人移动。"));
        }

        [TestCase(2, 0, "我方得分  +2")]
        [TestCase(0, 3, "AI 得分  +3")]
        [TestCase(1, 1, "双方得分  我方 +1，AI +1")]
        public void ScoreChangeMessageNamesEveryScoringSide(
            int localDelta, int opponentDelta, string expected)
        {
            Assert.AreEqual(expected,
                Vc5DemoBattleFeedbackModel.BuildScoreChangeMessage(localDelta, opponentDelta));
        }

        [TestCase(true, 9, 4, 2, 8, "胜利：我方达到 9 分")]
        [TestCase(false, 4, 9, 8, 2, "失败：AI 达到 9 分")]
        [TestCase(true, 6, 4, 5, 0, "胜利：AI 需要抽牌时牌库为空")]
        [TestCase(false, 4, 6, 0, 5, "失败：我方需要抽牌时牌库为空")]
        public void EndReasonExplainsScoreOrDeckExhaustion(
            bool localWon, int localScore, int opponentScore,
            int localDeckCount, int opponentDeckCount, string expected)
        {
            Assert.AreEqual(expected, Vc5DemoBattleFeedbackModel.BuildEndReason(
                localWon, localScore, opponentScore, localDeckCount, opponentDeckCount));
        }
    }
}
