using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;
using UnityEngine.EventSystems;

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
            flow.Hud.SelectUnit(mage.Id);
            hand.Selected(cover.Id);
            typeof(PrototypeFlow).GetMethod("PresentPlan",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,new object[]{CardAimResolver.Resolve(controller.State,cover.Id,"mage",mage,mage.Position,Vector2.zero,null)});
            Assert.IsTrue(controller.CurrentPreview.IsValid);
            flow.Hud.Cancel.onClick.Invoke();
            Assert.AreEqual(2, mage.Ap); Assert.AreEqual(6, controller.State.Hand.Count);
            var advance = controller.State.Hand.Find(c=>c.Kind==CardKind.Advance);
            flow.Hud.SelectUnit(mage.Id);
            CardDragInput input=null;foreach(var d in Object.FindObjectsOfType<CardDragInput>())if(d.CardId==advance.Id)input=d;
            var pointer=new PointerEventData(EventSystem.current){position=input.transform.position};input.OnBeginDrag(pointer);
            pointer.position=controller.ViewCamera.WorldToScreenPoint(controller.Board.World(new GridPos(3,5)));
            input.OnDrag(pointer);input.OnEndDrag(pointer);
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
            StringAssert.DoesNotContain("1 AP",flow.Hud.Move.GetComponentInChildren<Text>().text);
            Canvas.ForceUpdateCanvases();
            var remaining=flow.Hud.ActionMenu.Find("Remaining AP label").GetComponent<Text>();
            StringAssert.Contains("剩余AP：",remaining.text);
            Assert.Greater(remaining.cachedTextGenerator.vertexCount,4,"AP label must actually render with the Chinese font");
            int apIcons=0;foreach(Transform child in flow.Hud.ActionMenu)if(child.name.StartsWith("Remaining AP diamond")&&child.gameObject.activeSelf)apIcons++;
            Assert.AreEqual(warrior.Ap,apIcons);
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

        [UnityTest]
        public IEnumerator SelectingSupportPortraitAsLegalCardTargetCommitsWithoutChangingExecutor()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;
            var flow=Object.FindObjectOfType<PrototypeFlow>();flow.Hud.Begin.onClick.Invoke();yield return null;
            var state=flow.Controller.State;var support=state.FindUnit("support");var warrior=state.FindUnit("warrior");
            var inspire=state.Hand[0];inspire.Kind=CardKind.Inspire;
            typeof(PrototypeFlow).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,null);
            flow.Hud.SelectUnit(support.Id);
            typeof(PrototypeFlow).GetMethod("SelectCard",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,new object[]{inspire.Id});
            Assert.AreEqual(2,support.Ap);Assert.AreEqual(6,state.Hand.Count,"Selecting a support card only opens target selection");
            flow.Hud.SelectUnit(warrior.Id);
            yield return new WaitUntil(()=>!flow.Controller.IsBusy);
            Assert.AreEqual(support.Id,typeof(PrototypeFlow).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(flow));
            Assert.AreEqual(1,support.Ap);Assert.AreEqual(5,state.Hand.Count);
            Assert.AreEqual(1,warrior.InspireBonus);Assert.Greater(warrior.InspireExpiresRound,0);
            SceneManager.LoadScene("Title");yield return null;
        }

        [UnityTest]
        public IEnumerator CancellingRecoveryPickerDoesNotSpendCardOrActionPoint()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;
            var flow=Object.FindObjectOfType<PrototypeFlow>();flow.Hud.Begin.onClick.Invoke();yield return null;
            var state=flow.Controller.State;var warrior=state.FindUnit("warrior");var recoveryCard=state.Hand[0];
            recoveryCard.Kind=CardKind.RecoverTactics;
            state.DiscardPile.Add(new CardInstance{Id="recovery-candidate",Kind=CardKind.HeavyAttack});
            typeof(PrototypeFlow).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,null);
            flow.Hud.SelectUnit(warrior.Id);
            typeof(PrototypeFlow).GetMethod("SelectCard",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,new object[]{recoveryCard.Id});
            var picker=Object.FindObjectOfType<RecoveryPickerView>();Assert.IsNotNull(picker);Assert.IsTrue(picker.Visible);
            int ap=warrior.Ap;int handCount=state.Hand.Count;int discardCount=state.DiscardPile.Count;
            Click("取消回收");
            Assert.IsFalse(picker.Visible);Assert.AreEqual(ap,warrior.Ap);Assert.AreEqual(handCount,state.Hand.Count);
            Assert.AreEqual(discardCount,state.DiscardPile.Count);Assert.IsFalse(flow.Controller.IsBusy);
            SceneManager.LoadScene("Title");yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingAdvanceOntoNonPartyUiDoesNotFallThroughToBoard()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;
            var flow=Object.FindObjectOfType<PrototypeFlow>();flow.Hud.Begin.onClick.Invoke();yield return null;
            var state=flow.Controller.State;var warrior=state.FindUnit("warrior");var origin=warrior.Position;
            flow.Hud.SelectUnit(warrior.Id);
            var advance=state.Hand.Find(c=>c.Kind==CardKind.Advance);var input=FindDrag(advance.Id);
            var pointer=new PointerEventData(EventSystem.current){position=input.transform.position};input.OnBeginDrag(pointer);
            var destination=new GridPos(3,5);
            Assert.IsTrue(ActionPlanner.Plan(state,new ActionRequest{Kind=ActionKind.Card,ActorId=warrior.Id,CardId=advance.Id,Destination=destination}).IsValid);
            pointer.position=flow.Controller.ViewCamera.WorldToScreenPoint(flow.Controller.Board.World(destination));
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(flow.Hud.Root,pointer.position,null,out local);
            var blocker=BattleHud.Panel(flow.Hud.Root,"Test UI blocking legal destination",local.x-flow.Hud.Root.rect.xMin-50,flow.Hud.Root.rect.yMax-local.y-50,100,100,Color.white);
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.IsTrue((bool)typeof(PrototypeFlow).GetMethod("OverUi",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,new object[]{pointer.position}),"Fixture UI must be registered by the canvas before testing blocked drops");
            input.OnDrag(pointer);input.OnEndDrag(pointer);
            Assert.AreEqual(origin,warrior.Position,"The action-menu UI is not a board destination");
            Assert.AreEqual(3,warrior.Ap);Assert.AreEqual(6,state.Hand.Count);
            Assert.AreEqual(Time.frameCount,typeof(PrototypeFlow).GetField("suppressBoardClickFrame",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(flow),"A UI drop must suppress same-frame board click-through");
            SceneManager.LoadScene("Title");yield return null;
        }

        [UnityTest]
        public IEnumerator DeadSelectedActorIsClearedWithoutChoosingAnother()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;
            var flow=Object.FindObjectOfType<PrototypeFlow>();flow.Hud.Begin.onClick.Invoke();yield return null;
            var state=flow.Controller.State;var warrior=state.FindUnit("warrior");var heavy=state.Hand.Find(c=>c.Kind==CardKind.HeavyAttack);
            flow.Hud.SelectUnit(warrior.Id);warrior.Hp=0;
            typeof(PrototypeFlow).GetMethod("SelectCard",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,new object[]{heavy.Id});
            Assert.IsNull(typeof(PrototypeFlow).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(flow));
            Assert.IsNull(typeof(PrototypeFlow).GetField("mode",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(flow));
            Assert.AreEqual(6,state.Hand.Count);Assert.AreEqual(3,warrior.Ap);
            Assert.AreEqual(2,state.FindUnit("mage").Ap);Assert.AreEqual(2,state.FindUnit("support").Ap);
            SceneManager.LoadScene("Title");yield return null;
        }

        private static CardDragInput FindDrag(string id)
        {
            foreach(var input in Object.FindObjectsOfType<CardDragInput>())if(input.CardId==id)return input;
            Assert.Fail("Missing card drag input: "+id);return null;
        }

        [UnityTest]
        public IEnumerator UnavailableCardHintUsesTheClickedCardsId()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;
            var flow=Object.FindObjectOfType<PrototypeFlow>();flow.Hud.Begin.onClick.Invoke();yield return null;
            var state=flow.Controller.State;var warrior=state.FindUnit("warrior");
            state.FindUnit("guard-a").Position=new GridPos(7,7);state.FindUnit("guard-b").Position=new GridPos(6,7);
            flow.Hud.SelectUnit(warrior.Id);
            typeof(PrototypeFlow).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,null);
            var spark=state.Hand.Find(c=>c.Kind==CardKind.SparkMark);var heavy=state.Hand.Find(c=>c.Kind==CardKind.HeavyAttack);
            var sparkReason=CardAimResolver.UnavailableReason(state,spark.Id,warrior.Id);
            var heavyReason=CardAimResolver.UnavailableReason(state,heavy.Id,warrior.Id);
            Assert.IsNotEmpty(sparkReason);Assert.IsNotEmpty(heavyReason);Assert.AreNotEqual(sparkReason,heavyReason);
            FindDrag(spark.Id).GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(sparkReason,flow.Hud.Hint.text);
            FindDrag(heavy.Id).GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(heavyReason,flow.Hud.Hint.text,"The displayed reason must belong to the clicked card id");
            Assert.AreEqual(3,warrior.Ap);Assert.AreEqual(6,state.Hand.Count);
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
