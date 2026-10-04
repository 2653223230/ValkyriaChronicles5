using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace VC5PvE.Tests
{
    public class CardTargetingGuideTests
    {
        private static PrototypeFlow Flow(){return Object.FindObjectOfType<PrototypeFlow>();}
        private static CardDragInput Drag(string id){foreach(var d in Object.FindObjectsOfType<CardDragInput>())if(d.CardId==id)return d;return null;}
        [UnityTest]
        public IEnumerator ApPreviewFadesOnlySpentDiamondsAndInvalidTargetRestoresThem()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;var f=Flow();f.Hud.Begin.onClick.Invoke();yield return null;
            var s=f.Controller.State;var warrior=s.FindUnit("warrior");warrior.Position=new GridPos(3,4);f.Controller.Board.Refresh(s);
            var card=s.Hand.Find(c=>c.Kind==CardKind.HeavyAttack);var drag=Drag(card.Id);
            var e=new PointerEventData(EventSystem.current){position=drag.transform.position};drag.OnBeginDrag(e);
            e.position=f.Controller.ViewCamera.WorldToScreenPoint(f.Controller.Board.World(s.FindUnit("guard-a").Position)+Vector3.up*1.1f);drag.OnDrag(e);
            UnitView actor=null;foreach(var v in Object.FindObjectsOfType<UnitView>())if(v.UnitId==warrior.Id)actor=v;
            Assert.AreEqual(2,actor.PreviewApCost);Assert.AreEqual(3,warrior.Ap);Assert.AreEqual(6,s.Hand.Count);
            var slots=(Transform[])typeof(UnitView).GetField("apDiamonds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(actor);
            yield return new WaitForSecondsRealtime(.475f);
            var block=new MaterialPropertyBlock();
            slots[0].GetChild(1).GetComponent<Renderer>().GetPropertyBlock(block);Assert.That(block.GetColor("_BaseColor").a,Is.EqualTo(1f).Within(.01f));
            slots[2].GetChild(1).GetComponent<Renderer>().GetPropertyBlock(block);Assert.Less(block.GetColor("_BaseColor").a,.5f);
            foreach(var v in Object.FindObjectsOfType<UnitView>())if(v!=actor)Assert.AreEqual(0,v.PreviewApCost);
            e.position=new Vector2(-80,-80);drag.OnDrag(e);
            Assert.AreEqual(0,actor.PreviewApCost);Assert.AreEqual(3,warrior.Ap);
            slots[2].GetChild(1).GetComponent<Renderer>().GetPropertyBlock(block);Assert.That(block.GetColor("_BaseColor").a,Is.EqualTo(1f).Within(.01f));
            drag.OnEndDrag(e);yield return null;SceneManager.LoadScene("Title");yield return null;
        }
        [UnityTest]
        public IEnumerator DisablingHandDuringDragCancelsWithoutHierarchyErrors()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;var f=Flow();f.Hud.Begin.onClick.Invoke();yield return null;
            var state=f.Controller.State;var d=Drag(state.Hand[0].Id);
            d.OnBeginDrag(new PointerEventData(EventSystem.current){position=d.transform.position});
            d.transform.parent.gameObject.SetActive(false);yield return null;
            Assert.IsNull(Object.FindObjectOfType<CardTargetingArrow>());
            Assert.AreEqual(6,state.Hand.Count);Assert.AreEqual(3,state.FindUnit("warrior").Ap);
            SceneManager.LoadScene("Title");yield return null;
        }
        [UnityTest]
        public IEnumerator LiveCoverPreviewIsReadOnlyAndReleaseSpendsExactlyOnce()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;var f=Flow();f.Hud.Begin.onClick.Invoke();yield return null;
            var s=f.Controller.State;var card=s.Hand.Find(c=>c.Kind==CardKind.Cover);var warrior=s.FindUnit("warrior");
            var d=Drag(card.Id);
            var e=new PointerEventData(EventSystem.current){position=d.transform.position};d.OnBeginDrag(e);
            // BeginDrag retracts a hovered card into its slot; do not use a cursor-dependent hover position.
            var original=d.transform.position;
            e.position=f.Controller.ViewCamera.WorldToScreenPoint(f.Controller.Board.World(warrior.Position)+Vector3.up*1.1f);d.OnDrag(e);
            Assert.IsNotNull(f.Controller.CurrentPreview);Assert.IsTrue(f.Controller.CurrentPreview.IsValid);
            foreach(var v in Object.FindObjectsOfType<UnitView>())Assert.AreEqual(v.UnitId==warrior.Id?1:0,v.PreviewApCost);
            Assert.AreEqual(3,warrior.Ap);Assert.AreEqual(0,warrior.Shield);Assert.AreEqual(6,s.Hand.Count);
            Assert.AreEqual(original,d.transform.position);Assert.IsFalse(f.Hud.Confirm.gameObject.activeInHierarchy);
            d.OnEndDrag(e);d.OnEndDrag(e);
            Assert.AreEqual(2,warrior.Ap);Assert.AreEqual(2,warrior.Shield);Assert.AreEqual(5,s.Hand.Count);
            foreach(var v in Object.FindObjectsOfType<UnitView>())Assert.AreEqual(0,v.PreviewApCost);
            yield return new WaitUntil(()=>!f.Controller.IsBusy);Assert.IsNull(Object.FindObjectOfType<CardTargetingArrow>());
            SceneManager.LoadScene("Title");yield return null;
        }
        [UnityTest]
        public IEnumerator InvalidDropAndEscapeClearPreviewWithoutSpending()
        {
            SceneManager.LoadScene("ForestRuins");yield return null;var f=Flow();f.Hud.Begin.onClick.Invoke();yield return null;
            var s=f.Controller.State;var card=s.Hand.Find(c=>c.Kind==CardKind.Advance);var d=Drag(card.Id);
            var e=new PointerEventData(EventSystem.current){position=d.transform.position};d.OnBeginDrag(e);e.position=new Vector2(-80,-80);d.OnDrag(e);d.OnEndDrag(e);
            Assert.AreEqual(6,s.Hand.Count);Assert.AreEqual(3,s.FindUnit("warrior").Ap);Assert.IsNull(f.Controller.CurrentPreview);
            yield return null;Assert.IsNull(Object.FindObjectOfType<CardTargetingArrow>());
            d=Drag(card.Id);e.position=d.transform.position;d.OnBeginDrag(e);
            typeof(PrototypeFlow).GetMethod("CancelSelection",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,null);
            yield return null;Assert.IsNull(Object.FindObjectOfType<CardTargetingArrow>());Assert.AreEqual(6,s.Hand.Count);
            SceneManager.LoadScene("Title");yield return null;
        }
        [Test]
        public void EveryCardPreviewMatchesAppliedCloneAndDoesNotMutateRealState()
        {
            foreach(CardKind kind in System.Enum.GetValues(typeof(CardKind)))
            {
                var s=BattleFactory.Create(20260930,true);TurnRules.StartBattle(s);s.Obstacles.Clear();
                var warrior=s.FindUnit("warrior");warrior.Position=new GridPos(2,4);
                var mage=s.FindUnit("mage");mage.Position=new GridPos(2,5);
                var support=s.FindUnit("support");support.Position=new GridPos(1,4);
                var enemy=s.FindUnit("guard-a");enemy.Position=new GridPos(3,3);enemy.MarkExpiresRound=s.Round+1;
                var c=s.Hand[0];c.Kind=kind;var hovered=kind==CardKind.Cover||kind==CardKind.Inspire?warrior:enemy;
                GridPos cell=kind==CardKind.Advance?new GridPos(2,3):hovered.Position;
                if(kind==CardKind.HeavyAttack)warrior.Position=new GridPos(3,4);
                var p=CardAimResolver.Resolve(s,c.Id,warrior.Id,hovered,cell,Vector2.zero,null);
                Assert.IsTrue(p.IsValid,kind+": "+p.Reason);int ap=warrior.Ap,rev=s.Revision,hp=enemy.Hp;
                var prediction=s.Clone();var result=BattleResolver.Apply(prediction,p);Assert.IsTrue(result.Success);
                Assert.AreEqual(ap,warrior.Ap);Assert.AreEqual(hp,enemy.Hp);Assert.AreEqual(rev,s.Revision);Assert.AreEqual(6,s.Hand.Count);
                var actual=s.Clone();BattleResolver.Apply(actual,ActionPlanner.Plan(actual,p.Request));
                foreach(var u in prediction.Units){var a=actual.FindUnit(u.Id);Assert.AreEqual(u.Hp,a.Hp);Assert.AreEqual(u.Ap,a.Ap);Assert.AreEqual(u.Position,a.Position);Assert.AreEqual(u.Shield,a.Shield);Assert.AreEqual(u.MarkExpiresRound,a.MarkExpiresRound);}
            }
        }
        [Test]
        public void NeutralCardNeverSilentlySwitchesActorAndHealCapsAtMissingHp()
        {
            var s=BattleFactory.Create(20260930,true);TurnRules.StartBattle(s);var c=s.Hand[0];c.Kind=CardKind.HeavyAttack;
            var w=s.FindUnit("warrior");w.Ap=0;var enemy=s.FindUnit("guard-a");
            var p=CardAimResolver.Resolve(s,c.Id,w.Id,enemy,enemy.Position,Vector2.zero,null);Assert.IsFalse(p.IsValid);Assert.AreEqual(w.Id,p.Request.ActorId);
            var support=s.FindUnit("support");w.Position=new GridPos(1,6);w.Hp=w.MaxHp-1;
            var heal=ActionPlanner.Plan(s,new ActionRequest{Kind=ActionKind.Heal,ActorId=support.Id,TargetId=w.Id});Assert.IsTrue(heal.IsValid);
            var after=s.Clone();BattleResolver.Apply(after,heal);Assert.AreEqual(w.MaxHp,after.FindUnit(w.Id).Hp);Assert.AreEqual(w.MaxHp-1,w.Hp);
        }
    }
}
