using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Linq;

namespace VC5PvE.Tests
{
    public class CampaignFlowTests
    {
        [UnityTest]
        public IEnumerator FirstVictoryOffersCampAndKeepsBattleStopped()
        {
            SceneManager.LoadScene("ForestRuins"); yield return null;
            var flow = Object.FindObjectOfType<PrototypeFlow>();
            var controller = flow.Controller;
            controller.BeginBattle();
            var mage = controller.State.FindUnit("mage");
            mage.Position = new GridPos(3, 5);
            var enemy = controller.State.FindUnit("guard-a");
            enemy.Position = new GridPos(3, 4); enemy.Hp = 1;
            controller.State.FindUnit("guard-b").Hp = 0;
            controller.State.FindUnit("shooter").Hp = 0;
            var attack = controller.Preview(new ActionRequest { Kind = ActionKind.Attack, ActorId = "mage", TargetId = enemy.Id });
            Assert.IsTrue(attack.IsValid);
            yield return controller.Confirm(attack);
            Assert.AreEqual(BattlePhase.Victory, controller.State.Phase);
            Button next = null;
            foreach (var button in Object.FindObjectsOfType<Button>())
                if (button.name == "进入营地") next = button;
            Assert.IsNotNull(next, "前两关胜利必须进入成长营地，不能直接结束整轮");
            Assert.IsTrue(next.interactable);
            int revision = controller.State.Revision;
            next.onClick.Invoke(); yield return null;
            Assert.AreEqual(revision, controller.State.Revision, "进入营地不得继续执行敌人或改变战斗结果");
            Assert.AreEqual(BattlePhase.Victory, controller.State.Phase);
            SceneManager.LoadScene("Title"); yield return null;
        }

        [UnityTest]
        public IEnumerator CampUiGrowthCarriesThroughAllThreeLevelsAndFinalVictoryHasNoCamp()
        {
            SceneManager.LoadScene("ForestRuins"); yield return null;
            var flow = Object.FindObjectOfType<PrototypeFlow>();
            var controller = flow.Controller;
            string upgradedId = flow.Run.Deck[0].Id;
            string deletedId = null;
            string firstRewardId;
            string secondRewardId;

            yield return WinCurrentLevel(flow, "guard-a");
            controller.State.FindUnit("support").Hp = 1;
            yield return EnterCamp();
            yield return ChooseTalentAndReward(TalentKind.FlankingStrike, 0);
            firstRewardId = flow.Run.Deck.Last().Id;
            yield return Click("Deck instance " + upgradedId);
            yield return Click("Upgrade selected");
            yield return Click("确认选择");

            Assert.AreEqual(1, flow.Run.CompletedCamps);
            Assert.IsTrue(flow.Run.Deck.Single(card => card.Id == upgradedId).Upgraded);
            Assert.AreEqual(2, flow.Run.LevelIndex);
            Assert.AreEqual(2, controller.State.LevelIndex);
            AssertBoardMatchesLevel(controller);
            Assert.AreEqual(6, controller.State.Hand.Count);
            Assert.AreEqual(flow.Run.Deck.Count, controller.State.Hand.Count + controller.State.DrawPile.Count);
            Assert.IsTrue(controller.State.Hand.Concat(controller.State.DrawPile).Any(card => card.Id == firstRewardId));
            Assert.IsTrue(controller.State.Hand.Any(card => card.Id == upgradedId && card.Upgraded));
            Assert.AreEqual(1, controller.State.FindUnit("warrior").Talents.Count);
            Assert.AreEqual(controller.State.FindUnit("support").MaxHp, controller.State.FindUnit("support").Hp,
                "从第一关营地进入第二关时自动恢复棋子生命值");

            controller.State.FindUnit("support").Hp = 1;
            yield return WinCurrentLevel(flow, "guard-a");
            yield return EnterCamp();
            yield return Click("Choose role Warrior");
            Assert.IsNull(FindButton("Choose talent FlankingStrike"), "已领取的天赋不能在后续营地再次领取");
            yield return Click("Choose talent CounterReady");
            yield return Click("确认选择");
            yield return Click("Reward card 0");
            yield return Click("确认选择");
            secondRewardId = flow.Run.Deck.Last().Id;
            deletedId = flow.Run.Deck.First(card => card.Id != upgradedId && !card.Upgraded).Id;
            yield return Click("Deck instance " + deletedId);
            yield return Click("Delete selected");
            yield return Click("确认选择");

            Assert.AreEqual(2, flow.Run.CompletedCamps);
            Assert.IsFalse(flow.Run.Deck.Any(card => card.Id == deletedId));
            Assert.AreEqual(3, flow.Run.LevelIndex);
            Assert.AreEqual(3, controller.State.LevelIndex);
            AssertBoardMatchesLevel(controller);
            Assert.AreEqual(6, controller.State.Hand.Count);
            Assert.AreEqual(flow.Run.Deck.Count, controller.State.Hand.Count + controller.State.DrawPile.Count);
            Assert.IsTrue(controller.State.Hand.Any(card => card.Id == upgradedId && card.Upgraded));
            Assert.IsTrue(controller.State.Hand.Concat(controller.State.DrawPile).Any(card => card.Id == firstRewardId));
            Assert.IsTrue(controller.State.Hand.Concat(controller.State.DrawPile).Any(card => card.Id == secondRewardId));
            Assert.AreEqual(2, controller.State.FindUnit("warrior").Talents.Count);
            Assert.AreEqual(controller.State.FindUnit("support").MaxHp, controller.State.FindUnit("support").Hp,
                "从第二关营地进入第三关时自动恢复棋子生命值");
            Assert.AreEqual(18, flow.Run.Deck.Count, "两张奖励牌中删除一张后，实体卡总数应为18");

            yield return WinCurrentLevel(flow, "boss");
            Assert.IsTrue(flow.Run.Finished);
            Assert.AreEqual(2, flow.Run.CompletedCamps, "第三关胜利后不再开启营地或发放第三次奖励");
            Assert.IsNull(FindButton("进入营地"));
            Assert.IsNotNull(FindButton("重新挑战"));

            SceneManager.LoadScene("Title"); yield return null;
        }

        [UnityTest]
        public IEnumerator ControllerEndTurnMatchesEnemyPlannerForPillarsAndBossTelegraphAoe()
        {
            SceneManager.LoadScene("ForestRuins"); yield return null;
            var flow = Object.FindObjectOfType<PrototypeFlow>();
            var controller = flow.Controller;

            var secondRun = CampaignRules.NewRun(81);
            secondRun.LevelIndex = 2;
            controller.CreateBattle(secondRun);
            controller.BeginBattle();
            controller.State.FindUnit("guard-a").Shield = 0;
            var secondExpected = controller.State.Clone();
            TurnRules.EndPlayerTurn(secondExpected);
            EnemyPlanner.ExecuteTurn(secondExpected);
            yield return controller.EndTurn();
            AssertBattleTurnMatches(controller.State, secondExpected);
            Assert.AreEqual(1, controller.State.FindUnit("guard-a").Shield, "存活护阵柱会在敌方行动前刷新绑定守卫的1点盾");

            var thirdRun = CampaignRules.NewRun(82);
            thirdRun.LevelIndex = 3;
            controller.CreateBattle(thirdRun);
            controller.BeginBattle();
            var firstRoundExpected = controller.State.Clone();
            TurnRules.EndPlayerTurn(firstRoundExpected);
            EnemyPlanner.ExecuteTurn(firstRoundExpected);
            yield return controller.EndTurn();
            AssertBattleTurnMatches(controller.State, firstRoundExpected);
            Assert.AreEqual(2, controller.State.Round);
            Assert.IsTrue(controller.State.BossTelegraph.HasValue, "第一轮结束后应生成固定的偶数轮预告区域");

            var secondRoundExpected = controller.State.Clone();
            TurnRules.EndPlayerTurn(secondRoundExpected);
            var telegraph = secondRoundExpected.BossTelegraph;
            Assert.IsTrue(telegraph.HasValue);
            Assert.IsTrue(EnemyPlanner.PlanTurn(secondRoundExpected).Actions.Any(action => action.Request.Kind == ActionKind.BossAoe),
                "偶数轮预告应执行钟鸣震荡");
            EnemyPlanner.ExecuteTurn(secondRoundExpected);
            yield return controller.EndTurn();
            AssertBattleTurnMatches(controller.State, secondRoundExpected);
            Assert.AreNotEqual(telegraph, controller.State.BossTelegraph, "偶数轮结算后应按下一轮规则更新或清除预告");

            SceneManager.LoadScene("Title"); yield return null;
        }

        private static IEnumerator WinCurrentLevel(PrototypeFlow flow, string targetId)
        {
            var controller = flow.Controller;
            if (controller.State.Phase == BattlePhase.Deployment) controller.BeginBattle();
            foreach (var enemy in controller.State.Units.Where(unit => unit.Team == Team.Enemy && !unit.IsFacility)) enemy.Hp = 0;
            var target = controller.State.FindUnit(targetId);
            Assert.IsNotNull(target, "当前关卡缺少胜利目标 " + targetId);
            target.Hp = 1;
            target.Shield = 0;
            target.Position = new GridPos(5, 5);
            controller.State.FindUnit("mage").Position = new GridPos(5, 4);
            var attack = controller.Preview(new ActionRequest { Kind = ActionKind.Attack, ActorId = "mage", TargetId = target.Id });
            Assert.IsTrue(attack.IsValid, "加速战斗的测试攻击必须是合法行动：" + attack.Reason);
            yield return controller.Confirm(attack);
            Assert.AreEqual(BattlePhase.Victory, controller.State.Phase);
        }

        private static IEnumerator EnterCamp()
        {
            var button = FindButton("进入营地");
            Assert.IsNotNull(button);
            Assert.IsTrue(button.interactable);
            button.onClick.Invoke();
            yield return null;
        }

        private static IEnumerator ChooseTalentAndReward(TalentKind talent, int rewardIndex)
        {
            var role = talent == TalentKind.FlankingStrike || talent == TalentKind.CounterReady ? "Choose role Warrior" : "Choose role Mage";
            yield return Click(role);
            yield return Click("Choose talent " + talent);
            yield return Click("确认选择");
            yield return Click("Reward card " + rewardIndex);
            yield return Click("确认选择");
        }

        private static IEnumerator Click(string buttonName)
        {
            var button = FindButton(buttonName);
            Assert.IsNotNull(button, "找不到营地按钮：" + buttonName);
            Assert.IsTrue(button.interactable, "营地按钮当前不可用：" + buttonName);
            button.onClick.Invoke();
            yield return null;
        }

        private static Button FindButton(string buttonName)
        {
            return Object.FindObjectsOfType<Button>().FirstOrDefault(button => button.name == buttonName && button.gameObject.activeInHierarchy);
        }

        private static void AssertBattleTurnMatches(BattleState actual, BattleState expected)
        {
            Assert.AreEqual(expected.Round, actual.Round);
            Assert.AreEqual(expected.Phase, actual.Phase);
            Assert.AreEqual(expected.BossTelegraph, actual.BossTelegraph);
            foreach (var expectedUnit in expected.Units)
            {
                var actualUnit = actual.FindUnit(expectedUnit.Id);
                Assert.IsNotNull(actualUnit);
                Assert.AreEqual(expectedUnit.Hp, actualUnit.Hp, expectedUnit.Id + " HP");
                Assert.AreEqual(expectedUnit.Shield, actualUnit.Shield, expectedUnit.Id + " shield");
                Assert.AreEqual(expectedUnit.Position, actualUnit.Position, expectedUnit.Id + " position");
            }
        }

        private static void AssertBoardMatchesLevel(BattleController controller)
        {
            var ruins=controller.Board.GetComponentsInChildren<Transform>()
                .Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Impassable ruin ")).ToArray();
            Assert.AreEqual(controller.State.Obstacles.Count,ruins.Length,"跨关必须清除上一关的障碍模型");
            foreach(var ruin in ruins)
                foreach(var renderer in ruin.GetComponentsInChildren<Renderer>())
                {
                    Assert.IsNotNull(renderer.sharedMaterial,"跨关障碍不能引用已释放的材质");
                    Assert.IsTrue(renderer.sharedMaterial.shader.isSupported);
                }
        }
    }
}
