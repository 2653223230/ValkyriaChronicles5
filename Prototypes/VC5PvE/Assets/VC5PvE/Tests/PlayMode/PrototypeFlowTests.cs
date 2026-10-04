using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace VC5PvE.Tests
{
    public class PrototypeFlowTests
    {
        [UnityTest]
        public IEnumerator ChargeViewReachesPushedCellAfterAnimation()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;
            var flow=Object.FindObjectOfType<PrototypeFlow>();var c=flow.Controller;c.BeginBattle();
            var warrior=c.State.FindUnit("warrior");warrior.Position=new GridPos(3,5);
            var charge=c.State.DrawPile.Find(card=>card.Kind==CardKind.Charge);
            c.State.DrawPile.Remove(charge);c.State.DrawPile.Add(c.State.Hand[0]);c.State.Hand.RemoveAt(0);c.State.Hand.Add(charge);
            c.Board.Refresh(c.State);
            var targetView=GameObject.Find("Unit guard-a").transform;
            var plan=c.Preview(new ActionRequest{Kind=ActionKind.Card,ActorId="warrior",CardId=charge.Id,TargetId="guard-a",Destination=new GridPos(3,4)});
            Assert.IsTrue(plan.IsValid);
            var routine=c.Confirm(plan);Assert.IsTrue(routine.MoveNext());
            Assert.AreEqual(new GridPos(3,2),c.State.FindUnit("guard-a").Position);
            Assert.Less(Vector3.Distance(c.Board.World(new GridPos(3,3)),targetView.position),.01f);
            yield return routine.Current;
            while(routine.MoveNext())yield return routine.Current;
            Assert.Less(Vector3.Distance(c.Board.World(new GridPos(3,2)),targetView.position),.01f);
            Assert.AreEqual(1,warrior.Ap);Assert.AreEqual(4,c.State.FindUnit("guard-a").Hp);
        }

        [UnityTest]
        public IEnumerator LastAvailableApHandsOverToEnemyAndRestoresNextRound()
        {
            var go=new GameObject("Automatic turn test");var c=go.AddComponent<BattleController>();
            c.CreateBattle();c.BeginBattle();
            c.State.FindUnit("warrior").Ap=1;c.State.FindUnit("mage").Ap=0;c.State.FindUnit("support").Ap=0;
            yield return c.Confirm(c.Preview(new ActionRequest{Kind=ActionKind.Move,ActorId="warrior",Destination=new GridPos(2,5)}));
            Assert.AreEqual(2,c.State.Round);
            Assert.AreEqual(BattlePhase.Player,c.State.Phase);
            Assert.AreEqual(3,c.State.FindUnit("warrior").Ap);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator ScenesCardsCancelEnemyTurnAndBothResultsWorkTogether()
        {
            SceneManager.LoadScene("Title"); yield return null;
            Assert.IsTrue(Object.FindObjectOfType<PrototypeFlow>().IsTitle);
            Click("开始游戏"); yield return null;
            var flow = Object.FindObjectOfType<PrototypeFlow>();
            var controller = flow.Controller;
            Assert.AreEqual(BattlePhase.Deployment, controller.State.Phase);
            Assert.IsTrue(controller.Deploy("warrior", new GridPos(1, 6)));
            flow.Hud.Begin.onClick.Invoke(); yield return null;
            Assert.AreEqual(BattlePhase.Player, controller.State.Phase);
            var hand = Object.FindObjectOfType<CardHandView>();
            var mage = controller.State.FindUnit("mage");
            var cover = controller.State.Hand.Find(c=>c.Kind==CardKind.Cover);
            hand.Selected(cover.Id);
            typeof(PrototypeFlow).GetMethod("PresentPlan",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,new object[]{CardAimResolver.Resolve(controller.State,cover.Id,"mage",mage,mage.Position,Vector2.zero,null)});
            Assert.IsTrue(controller.CurrentPreview.IsValid);
            flow.Hud.Cancel.onClick.Invoke();
            Assert.AreEqual(2, mage.Ap); Assert.AreEqual(6, controller.State.Hand.Count);
            var advance = controller.State.Hand.Find(c=>c.Kind==CardKind.Advance);
            flow.Hud.SelectUnit(mage.Id);hand.Selected(advance.Id);
            WorldClick(flow, null, new GridPos(3,5));
            yield return new WaitUntil(()=>!controller.IsBusy);
            Assert.AreEqual(new GridPos(3,5), mage.Position);
            Assert.AreEqual(1, mage.Ap); Assert.AreEqual(1, mage.Shield);
            Assert.AreEqual(5, controller.State.Hand.Count);
            flow.Hud.End.onClick.Invoke();
            Assert.AreEqual(BattlePhase.Enemy, controller.State.Phase);
            Assert.IsFalse(flow.Hud.End.interactable);
            int revision = controller.State.Revision;
            hand.Selected(controller.State.Hand[0].Id);
            Assert.AreEqual(revision, controller.State.Revision);
            yield return new WaitUntil(()=>!controller.IsBusy);
            Assert.AreEqual(2, controller.State.Round);
            Assert.AreEqual(6, controller.State.Hand.Count);

            // A reduced final-enemy fixture exercises actual killing action and result navigation.
            var enemy = controller.State.FindUnit("guard-a"); enemy.Hp=1; enemy.Position=new GridPos(3,4);
            controller.State.FindUnit("guard-b").Hp=0;controller.State.FindUnit("shooter").Hp=0;
            var final = controller.Preview(new ActionRequest{Kind=ActionKind.Attack,ActorId="mage",TargetId=enemy.Id});
            Assert.IsTrue(final.IsValid);
            yield return controller.Confirm(final);
            Assert.AreEqual(BattlePhase.Victory,controller.State.Phase);
            int seed=controller.State.RandomSeed;
            Click("重新挑战"); yield return null;
            flow=Object.FindObjectOfType<PrototypeFlow>();controller=flow.Controller;
            Assert.AreEqual(BattlePhase.Deployment,controller.State.Phase);
            Assert.AreEqual(seed,controller.State.RandomSeed);
            Assert.AreEqual(6,controller.State.FindUnit("guard-a").Hp);
            flow.Hud.Begin.onClick.Invoke();
            controller.State.FindUnit("warrior").Hp=0;controller.State.FindUnit("support").Hp=0;
            controller.State.FindUnit("mage").Hp=1;
            controller.State.FindUnit("guard-a").Position=new GridPos(3,6);
            controller.State.FindUnit("guard-b").Hp=0;controller.State.FindUnit("shooter").Hp=0;
            yield return controller.EndTurn();
            Assert.AreEqual(BattlePhase.Defeat,controller.State.Phase);
            Click("返回标题");yield return null;
            Assert.IsTrue(Object.FindObjectOfType<PrototypeFlow>().IsTitle);
        }

        private static void WorldClick(PrototypeFlow flow, UnitState unit, GridPos position)
        {
            typeof(PrototypeFlow).GetMethod("ClickBattle", BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(flow,new object[]{unit,(GridPos?)position});
        }

        [UnityTest]
        public IEnumerator BasicActionsAppearOnlyAfterSelectingAPieceAndCloseOnActionOrEmptyCell()
        {
            SceneManager.LoadScene("ForestRuins"); yield return null;
            var flow=Object.FindObjectOfType<PrototypeFlow>();
            flow.Hud.Begin.onClick.Invoke(); yield return null;
            Assert.IsFalse(flow.Hud.Move.gameObject.activeInHierarchy,"基础行动不能常驻底栏");
            var warrior=flow.Controller.State.FindUnit("warrior");
            WorldClick(flow,warrior,warrior.Position);
            Assert.IsTrue(flow.Hud.Move.gameObject.activeInHierarchy);
            StringAssert.Contains("1 AP",flow.Hud.Move.GetComponentInChildren<Text>().text);
            Assert.IsFalse(flow.Hud.Heal.gameObject.activeInHierarchy);
            WorldClick(flow,null,new GridPos(0,7));
            Assert.IsFalse(flow.Hud.Move.gameObject.activeInHierarchy);
            var support=flow.Controller.State.FindUnit("support");
            WorldClick(flow,support,support.Position);
            Assert.IsTrue(flow.Hud.Heal.gameObject.activeInHierarchy);
            StringAssert.Contains("2",flow.Hud.Move.GetComponentInChildren<Text>().text);
            flow.Hud.Move.onClick.Invoke();
            Assert.IsFalse(flow.Hud.Move.gameObject.activeInHierarchy);
            flow.Hud.Cancel.onClick.Invoke();
            support.Ap=0;WorldClick(flow,support,support.Position);
            Assert.IsFalse(flow.Hud.Move.interactable);
            SceneManager.LoadScene("Title");yield return null;
        }
        private static void Click(string name)
        {
            foreach(var b in Object.FindObjectsOfType<Button>())
                if(b.name==name) { Assert.IsTrue(b.interactable);b.onClick.Invoke();return; }
            Assert.Fail("Missing button: "+name);
        }

        [UnityTest]
        public IEnumerator PreviewAndCancelPreserveResourcesThenConfirmedActionSpendsOnce()
        {
            var go = new GameObject("Controller test");
            var controller = go.AddComponent<BattleController>();
            controller.CreateBattle();
            controller.BeginBattle();
            var actor = controller.State.FindUnit("warrior");
            var request = new ActionRequest { Kind = ActionKind.Move, ActorId = "warrior", Destination = new GridPos(2,5) };
            var preview = controller.Preview(request);
            Assert.IsTrue(preview.IsValid);
            Assert.AreEqual(3, actor.Ap);
            Assert.AreEqual(new GridPos(2,6), actor.Position);
            controller.CancelPreview();
            Assert.AreEqual(3, actor.Ap);
            yield return controller.Confirm(preview);
            Assert.AreEqual(2, actor.Ap);
            Assert.AreEqual(new GridPos(2,5), actor.Position);
            var stale = BattleResolver.Apply(controller.State, preview);
            Assert.IsFalse(stale.Success);
            Assert.AreEqual(2, actor.Ap);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator RestartDuringEnemyFeedbackCannotMutateFreshBattle()
        {
            var go = new GameObject("Controller session test");
            var controller = go.AddComponent<BattleController>();
            controller.CreateBattle(); controller.BeginBattle();
            int seed = controller.State.RandomSeed;
            var enemyRoutine = controller.EndTurn();
            Assert.IsTrue(enemyRoutine.MoveNext());
            controller.CreateBattle();
            Assert.AreEqual(seed, controller.State.RandomSeed, "重试应恢复相同牌库种子");
            while (enemyRoutine.MoveNext()) yield return enemyRoutine.Current;
            Assert.AreEqual(BattlePhase.Deployment, controller.State.Phase);
            Assert.AreEqual(0, controller.State.Round);
            Assert.AreEqual(6, controller.State.Hand.Count);
            Assert.AreEqual(new GridPos(3,3), controller.State.FindUnit("guard-a").Position);
            Object.Destroy(go);
        }
    }
}
