using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VC5PvE;

namespace VC5PvE.SharedAP.Tests
{
    public class SharedInteractionTests
    {
        GameObject host;
        SharedBattleController flow;
        [UnitySetUp] public IEnumerator SetUp()
        {
            yield return SharedLegacyInteractionTests.EmptyScene();
            host = new GameObject("Shared AP interaction fixture");
            flow = host.AddComponent<SharedBattleController>();
            flow.InitializeForTests(17);
            flow.SkipTutorial();
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); }

        [Test] public void StartsInSharedPlayerTurnWithoutDeployment()
        { Assert.AreEqual(6, flow.State.Ap); Assert.AreEqual(SPhase.Player, flow.State.Phase); Assert.AreEqual(5, flow.State.Hand.Count); }

        [UnityTest] public IEnumerator FocusTutorialCompletesTheApprovedRouteAndWaitsForResolution()
        {
            flow.Restart(true);
            flow.BuildPresentation();
            yield return null;
            Assert.AreEqual(FocusTutorialStep.SelectMage,flow.FocusStep);
            flow.ChooseUnit("mage");
            Assert.AreEqual(FocusTutorialStep.ReadInfo,flow.FocusStep);
            flow.Cancel();
            Assert.AreEqual("mage",flow.SelectedId,"Cancelling while reading details must not restore the default vanguard.");
            flow.ContinueFocusTutorial();
            Assert.AreEqual(FocusTutorialStep.ShortStep,flow.FocusStep);

            flow.Cancel(); // Rebuild the mage's live move/attack affordances before the direct short-step click.
            Assert.IsTrue(flow.Board.GetComponentsInChildren<LineRenderer>().Any(x=>x.name=="Short step footprint"),
                "Cancelling the short-step prompt must preserve live movement markers.");
            flow.ClickCell(new SPos(4,5)); // E6
            Assert.IsTrue(flow.Busy);
            Assert.AreEqual(FocusTutorialStep.ShortStep,flow.FocusStep,"The tutorial must wait for the action presentation to finish.");
            yield return WaitForResolution();
            Assert.AreEqual(FocusTutorialStep.DragMove,flow.FocusStep);
            Assert.AreEqual(6,flow.State.Ap);
            Assert.AreEqual(5,flow.State.Hand.Count);

            var move=flow.State.Hand.First(c=>c.Kind==SCard.Move);
            flow.BeginCardDrag(move.Id);Assert.IsTrue(flow.DropCardOn("mage"));
            Assert.AreEqual(FocusTutorialStep.MoveTarget,flow.FocusStep);
            Assert.AreEqual(6,flow.State.Ap,"Choosing the executor previews the card without charging AP.");
            Assert.AreEqual(5,flow.State.Hand.Count,"The card remains in hand until a legal destination is confirmed.");
            Assert.AreEqual(1,flow.PreviewApCost);
            flow.ClickCell(new SPos(4,3)); // E4 via E5
            yield return WaitForResolution();
            Assert.AreEqual(new SPos(4,3),flow.State.Find("mage").Position);
            Assert.AreEqual(5,flow.State.Ap);
            Assert.AreEqual(4,flow.State.Hand.Count);
            Assert.AreEqual(FocusTutorialStep.DragSkill,flow.FocusStep);

            var skill=flow.State.Hand.First(c=>c.Kind==SCard.Skill);
            flow.BeginCardDrag(skill.Id);Assert.IsTrue(flow.DropCardOn("mage"));
            Assert.AreEqual(5,flow.State.Ap,"The skill target preview is free.");
            Assert.AreEqual(4,flow.State.Hand.Count);
            Assert.AreEqual(FocusTutorialStep.SkillTarget,flow.FocusStep);
            var mageHp=flow.State.Find("mage").Hp;
            flow.ClickCell(new SPos(3,3)); // D4 crossbow; C4 and D3 are adjacent enemies.
            yield return WaitForResolution();
            Assert.AreEqual(1,flow.State.Find("crossbow").Hp);
            Assert.AreEqual(2,flow.State.Find("patrol").Hp);
            Assert.AreEqual(3,flow.State.Find("guard").Hp);
            Assert.AreEqual(mageHp,flow.State.Find("mage").Hp,"The allied caster is never included in enemy splash.");
            Assert.AreEqual(4,flow.State.Ap);
            Assert.AreEqual(3,flow.State.Hand.Count);
            Assert.AreEqual(FocusTutorialStep.SelectArcher,flow.FocusStep);

            flow.ChooseUnit("archer");
            Assert.AreEqual(FocusTutorialStep.BasicAttack,flow.FocusStep);
            flow.Cancel(); // The selected archer's attack target remains available after cancelling the lesson prompt.
            Assert.IsTrue(flow.Board.GetComponentsInChildren<LineRenderer>().Any(x=>x.name=="Basic sword"),
                "Cancelling the basic-attack prompt must preserve live sword markers.");
            flow.ClickCell(new SPos(2,3)); // C4 patrol: a basic attack spends AP, not a card.
            yield return WaitForResolution();
            Assert.IsFalse(flow.State.Find("patrol").Alive);
            Assert.AreEqual(2,flow.State.Ap);
            Assert.AreEqual(3,flow.State.Hand.Count);
            Assert.AreEqual(FocusTutorialStep.DragShield,flow.FocusStep);

            var shield=flow.State.Hand.First(c=>c.Kind==SCard.Shield);
            var mageHpBeforeShield=flow.State.Find("mage").Hp;
            flow.BeginCardDrag(shield.Id);Assert.IsTrue(flow.DropCardOn("mage"));
            Assert.AreEqual(mageHpBeforeShield,flow.State.Find("mage").Hp,"Shielding must not restore HP.");
            yield return WaitForResolution();
            Assert.AreEqual(1,flow.State.Ap);
            Assert.AreEqual(2,flow.State.Hand.Count);
            Assert.AreEqual(FocusTutorialStep.EndTurn,flow.FocusStep);

            flow.BeginEndTurn();
            Assert.AreEqual(SharedInputStage.Retain,flow.Stage);
            Assert.AreEqual(FocusTutorialStep.Retain,flow.FocusStep);
            Assert.AreEqual(1,flow.State.Ap,"The end-turn retention step leaves the remaining AP available until confirmation.");
            var optionalDiscard=flow.State.Hand[0].Id;
            flow.ToggleDiscard(optionalDiscard);
            Assert.AreEqual(1,flow.DiscardIds.Count);
            flow.Cancel();
            Assert.AreEqual(0,flow.DiscardIds.Count,"Cancelling retention clears the optional discard selection.");
            Assert.AreEqual(1,flow.State.Ap);
            Assert.AreEqual(2,flow.State.Hand.Count);
            Assert.AreEqual(FocusTutorialStep.EndTurn,flow.FocusStep,"Cancelling the discard choice returns to the end-turn instruction.");
            flow.BeginEndTurn();
            Assert.AreEqual(SharedInputStage.Retain,flow.Stage);
            Assert.AreEqual(FocusTutorialStep.Retain,flow.FocusStep);
            flow.ConfirmEndTurn();
            Assert.AreEqual(FocusTutorialStep.Enemy,flow.FocusStep);
            yield return WaitForResolution();
            Assert.AreEqual(2,flow.State.Round);
            Assert.AreEqual(6,flow.State.Ap);
            Assert.AreEqual(5,flow.State.Hand.Count);
            Assert.AreEqual(4,flow.State.Find("mage").Hp);
            Assert.AreEqual(FocusTutorialStep.Ready,flow.FocusStep);
            flow.ContinueFocusTutorial();
            Assert.AreEqual(FocusTutorialStep.Off,flow.FocusStep);
        }

        [Test] public void FocusTutorialRejectsWrongCardExecutorAndLandingWithoutChangingStateAndCancelRestoresDragStep()
        {
            flow.Restart(true);
            var ap=flow.State.Ap;var hand=flow.State.Hand.Select(c=>c.Id).ToArray();
            var magePosition=flow.State.Find("mage").Position;
            flow.ChooseUnit("archer");
            Assert.AreEqual(FocusTutorialStep.SelectMage,flow.FocusStep);
            flow.ChooseUnit("mage");
            Assert.AreEqual(FocusTutorialStep.ReadInfo,flow.FocusStep);
            flow.ChooseBasic(SActionKind.ShortStep);flow.ClickCell(new SPos(4,5));
            Assert.AreEqual(FocusTutorialStep.ReadInfo,flow.FocusStep,"Reading the instruction must precede the short step.");
            Assert.AreEqual(magePosition,flow.State.Find("mage").Position);
            Assert.AreEqual(ap,flow.State.Ap);CollectionAssert.AreEqual(hand,flow.State.Hand.Select(c=>c.Id));
            flow.ContinueFocusTutorial();flow.ChooseBasic(SActionKind.ShortStep);flow.ClickCell(new SPos(4,5));
            Assert.AreEqual(FocusTutorialStep.DragMove,flow.FocusStep);
            flow.BeginCardDrag(flow.State.Hand.First(c=>c.Kind==SCard.Shield).Id);
            Assert.IsFalse(flow.DropCardOn("mage"),"The wrong card cannot be committed in the move lesson.");
            Assert.AreEqual(ap,flow.State.Ap);CollectionAssert.AreEqual(hand,flow.State.Hand.Select(c=>c.Id));
            var move=flow.State.Hand.First(c=>c.Kind==SCard.Move);
            flow.BeginCardDrag(move.Id);
            Assert.IsFalse(flow.DropCardOn("archer"),"The move card must be assigned to the taught mage.");
            flow.Cancel();
            Assert.AreEqual(FocusTutorialStep.DragMove,flow.FocusStep);
            flow.BeginCardDrag(move.Id);
            Assert.IsTrue(flow.DropCardOn("mage"));
            flow.ClickCell(new SPos(3,4));
            Assert.AreEqual(new SPos(4,5),flow.State.Find("mage").Position,"An incorrect landing must not move the mage.");
            Assert.AreEqual(6,flow.State.Ap);Assert.AreEqual(5,flow.State.Hand.Count);
            flow.Cancel();
            Assert.AreEqual(FocusTutorialStep.DragMove,flow.FocusStep,"Cancelling target selection returns to the card-drag step.");
            Assert.AreEqual(SharedInputStage.Idle,flow.Stage);
        }

        [Test] public void InvalidExecutorDropDoesNotSpendCardOrAp()
        {
            string card = flow.State.Hand[0].Id;
            flow.BeginCardDrag(card);
            Assert.IsFalse(flow.DropCardOn(flow.State.Units.First(u => !u.Player).Id));
            Assert.AreEqual(6, flow.State.Ap); Assert.AreEqual(5, flow.State.Hand.Count);
        }

        [Test] public void HealingDropTargetsRecipientDirectly()
        {
            var mage = flow.State.Units.First(u => u.Role == SRole.Mage); mage.Hp = 3;
            var card = EnsureCardInHand(SCard.Heal);
            flow.BeginCardDrag(card.Id); Assert.IsTrue(flow.DropCardOn(mage.Id));
            Assert.AreEqual(6, flow.State.Find(mage.Id).Hp); Assert.AreEqual(5, flow.State.Ap);
            Assert.AreEqual(SharedInputStage.Idle, flow.Stage);
        }

        [Test] public void ChargeLandingAndCancelAreOnlyPreview()
        {
            var actor=flow.State.Units.First(u=>u.Role==SRole.Vanguard); actor.Position=new SPos(3,5);
            var start=actor.Position; string card=flow.State.Hand.First(c=>c.Kind==SCard.Charge).Id;
            flow.BeginCardDrag(card); flow.DropCardOn(actor.Id);
            var previewMethod=typeof(SharedBattleController).GetMethod("PreviewAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var landingPreview=(SPlan)previewMethod.Invoke(flow,new object[]{new SPos(2,4)});
            Assert.IsTrue(landingPreview.Valid);
            Assert.AreEqual(new SPos(2,4),landingPreview.After.Find(actor.Id).Position);
            Assert.AreEqual(4,landingPreview.After.Ap);
            foreach(var enemy in flow.State.Units.Where(u=>!u.Player))
                Assert.AreEqual(enemy.Hp,landingPreview.After.Find(enemy.Id).Hp,"landing preview must not preselect and damage an enemy");
            flow.ClickCell(new SPos(2,4));
            Assert.AreEqual(start,flow.State.Find(actor.Id).Position); Assert.AreEqual(6,flow.State.Ap);
            flow.Cancel(); Assert.AreEqual(5,flow.State.Hand.Count); Assert.AreEqual(SharedInputStage.Idle,flow.Stage);
        }

        [Test] public void TwoStepChargeCommitsOnlyAtFinalTarget()
        {
            var actor=flow.State.Units.First(u=>u.Role==SRole.Vanguard); actor.Position=new SPos(3,5);
            string card=flow.State.Hand.First(c=>c.Kind==SCard.Charge).Id;
            flow.BeginCardDrag(card); flow.DropCardOn(actor.Id);
            flow.ClickCell(new SPos(2,4)); flow.ClickCell(new SPos(2,3));
            Assert.AreEqual(new SPos(2,4),flow.State.Find(actor.Id).Position);
            Assert.AreEqual(4,flow.State.Ap); Assert.AreEqual(4,flow.State.Hand.Count);
        }

        [Test] public void ZeroApStillAllowsShortStepAndDoesNotAutoEnd()
        {
            flow.State.Ap=0; var actor=flow.State.Units.First(u=>u.Role==SRole.Vanguard);
            flow.ChooseUnit(actor.Id); flow.ChooseBasic(SActionKind.ShortStep); flow.ClickCell(new SPos(3,5));
            Assert.AreEqual(new SPos(3,5),flow.State.Find(actor.Id).Position);
            Assert.AreEqual(SPhase.Player,flow.State.Phase); Assert.AreEqual(1,flow.State.Round);
        }

        [Test] public void ClickingEnemyWhileUnitSelectedExecutesBasicAttack()
        {
            var actor=flow.State.Find("vanguard");var enemy=flow.State.Find("patrol");
            actor.Position=new SPos(2,4);enemy.Position=new SPos(2,3);
            var hp=enemy.Hp;flow.ChooseUnit(actor.Id);flow.ClickCell(enemy.Position);
            Assert.AreEqual(hp-3,flow.State.Find(enemy.Id).Hp);
            Assert.AreEqual(4,flow.State.Ap);
        }

        [UnityTest] public IEnumerator IdleScreenClickUsesLegalShortStepGroundBeforeOverlappingPortrait()
        {
            flow.BuildPresentation();yield return null;
            var actor=flow.State.Find("vanguard");actor.Position=new SPos(3,3);RefreshBoard();yield return null;
            flow.ChooseUnit(actor.Id);
            var landing=SharedRules.Reachable(flow.State,actor.Id,1).First(p=>SharedRules.Plan(flow.State,
                new SAction{Kind=SActionKind.ShortStep,ActorId=actor.Id,Destination=p}).Valid);
            var cover=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId=="patrol");
            cover.transform.position=flow.Board.World(new GridPos(landing.X,landing.Y));yield return null;
            var point=FindScreenPointInsideCellAndPortrait(landing);
            Assert.IsTrue(flow.Board.GetComponentsInChildren<UnitView>().Any(v=>v.ContainsScreenPoint(point)),"The test point must overlap a rendered unit portrait.");
            flow.ClickScreen(point);yield return WaitForResolution();
            Assert.AreEqual(landing,flow.State.Find(actor.Id).Position,"A legal ground destination takes priority over the portrait covering it.");
        }

        [UnityTest] public IEnumerator ScreenAttackChoosesLegalEnemyWhenPortraitsOverlap()
        {
            flow.BuildPresentation();yield return null;
            var archer=flow.State.Find("archer");var legal=flow.State.Find("patrol");var outOfRange=flow.State.Find("guard");
            archer.Position=new SPos(0,0);legal.Position=new SPos(1,0);outOfRange.Position=new SPos(6,6);RefreshBoard();yield return null;
            var legalView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId==legal.Id);
            var otherView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId==outOfRange.Id);
            otherView.transform.position=flow.Board.World(new GridPos(legal.Position.X,legal.Position.Y));yield return null;
            var point=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(legal.Position.X,legal.Position.Y))+Vector3.up*.7f);
            Assert.IsTrue(legalView.ContainsScreenPoint(point)&&otherView.ContainsScreenPoint(point),"The screen point must lie in both rendered portraits.");
            var attack=flow.State.Hand.First(c=>c.Kind==SCard.Attack);var before=legal.Hp;
            Assert.IsTrue(SharedRules.Plan(flow.State,new SAction{Kind=SActionKind.Card,ActorId=archer.Id,CardId=attack.Id,TargetId=legal.Id}).Valid);
            flow.BeginCardDrag(attack.Id);Assert.IsTrue(flow.DropCardOn(archer.Id));flow.ClickScreen(point);yield return WaitForResolution();
            Assert.Less(flow.State.Find(legal.Id).Hp,before,"The legal target under the pointer is selected ahead of the overlapping out-of-range enemy.");
        }

        [UnityTest] public IEnumerator IdleScreenClickOnOwnFootCellDoesNotAttackOverlappingEnemyPortrait()
        {
            flow.BuildPresentation();yield return null;
            var actor=flow.State.Find("vanguard");var enemy=flow.State.Find("patrol");
            actor.Position=new SPos(3,3);enemy.Position=new SPos(3,2);RefreshBoard();yield return null;
            var actorView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId==actor.Id);
            var enemyView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId==enemy.Id);
            enemyView.transform.position=flow.Board.World(new GridPos(actor.Position.X,actor.Position.Y));yield return null;
            var point=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(actor.Position.X,actor.Position.Y)));
            Assert.IsTrue(actorView.ContainsScreenPoint(point)&&enemyView.ContainsScreenPoint(point),"The own foot point must overlap both portraits.");
            flow.ChooseUnit(actor.Id);var hp=enemy.Hp;var ap=flow.State.Ap;flow.ClickScreen(point);yield return WaitForResolution();
            Assert.AreEqual(hp,flow.State.Find(enemy.Id).Hp);Assert.AreEqual(ap,flow.State.Ap);
            Assert.AreEqual(actor.Id,flow.SelectedId,"The friendly unit's ground cell remains selectable.");
        }

        [UnityTest] public IEnumerator ScreenCardDragPrefersLegalExecutorAmongOverlappingFriendlyPortraits()
        {
            flow.BuildPresentation();yield return null;
            var mage=flow.State.Find("mage");var vanguard=flow.State.Find("vanguard");
            mage.Position=new SPos(2,2);mage.Hp=3;vanguard.Position=new SPos(0,0);RefreshBoard();yield return null;
            var mageView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId==mage.Id);
            var otherView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId==vanguard.Id);
            otherView.transform.position=flow.Board.World(new GridPos(mage.Position.X,mage.Position.Y));yield return null;
            var point=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(mage.Position.X,mage.Position.Y))+Vector3.up*.7f);
            Assert.IsTrue(mageView.ContainsScreenPoint(point)&&otherView.ContainsScreenPoint(point),"The screen point must lie in both rendered portraits.");
            var heal=EnsureCardInHand(SCard.Heal);flow.BeginCardDrag(heal.Id);
            Assert.IsTrue(flow.CanDropAt(point));Assert.AreEqual(mage.Id,flow.PreviewExecutorId);
        }

        [Test] public void DiscardSelectionDefaultsToKeepAllAndCancelClearsSelection()
        {
            flow.BeginEndTurn();
            Assert.AreEqual(1,flow.State.Round,"The first end-turn call enters discard selection.");
            Assert.AreEqual(SharedInputStage.Retain,flow.Stage);
            Assert.AreEqual(0,flow.DiscardIds.Count);
            flow.ToggleDiscard(flow.State.Hand[0].Id); flow.ToggleDiscard(flow.State.Hand[1].Id);
            flow.ToggleDiscard(flow.State.Hand[2].Id); Assert.AreEqual(3,flow.DiscardIds.Count);
            flow.Cancel(); Assert.AreEqual(SharedInputStage.Idle,flow.Stage); Assert.AreEqual(0,flow.DiscardIds.Count);
            Assert.AreEqual(6,flow.State.Ap); Assert.AreEqual(1,flow.State.Round);
        }

        [Test] public void SelectingCardFirstAllowsOnlyLegalExecutorsAndUnitBindsCard()
        {
            var attack=flow.State.Hand.First(c=>c.Kind==SCard.Move);
            var ap=flow.State.Ap;var revision=flow.State.Revision;var hand=flow.State.Hand.Count;var actorPos=flow.State.Find("vanguard").Position;
            Assert.IsTrue(flow.CanExecuteCard("vanguard",attack.Id));
            Assert.AreEqual(ap,flow.State.Ap);Assert.AreEqual(revision,flow.State.Revision);Assert.AreEqual(hand,flow.State.Hand.Count);Assert.AreEqual(actorPos,flow.State.Find("vanguard").Position);
            flow.SelectCard(attack.Id);
            Assert.AreEqual(SharedInputStage.Idle,flow.Stage);
            Assert.IsTrue(flow.CanExecuteCard("vanguard",attack.Id));
            flow.ChooseUnit("vanguard");
            Assert.AreEqual(attack.Id,flow.PendingCardId);
            Assert.AreEqual("vanguard",flow.PendingExecutorId);
            Assert.AreEqual(SharedInputStage.Landing,flow.Stage);
        }

        [Test] public void ConfirmEndTurnPassesSelectedDiscardsAndKeepsUnselectedCards()
        {
            var kept=flow.State.Hand[0].Id;var discarded=flow.State.Hand[1].Id;
            flow.BeginEndTurn();flow.ToggleDiscard(discarded);flow.ConfirmEndTurn();
            Assert.IsFalse(flow.State.Hand.Any(c=>c.Id==discarded));
            Assert.IsTrue(flow.State.Hand.Any(c=>c.Id==kept));
            Assert.AreEqual(0,flow.DiscardIds.Count);
        }

        [Test] public void RestartClearsPendingCardAndRetention()
        {
            flow.OpenRetention(); flow.ToggleKeep(flow.State.Hand[0].Id); flow.Restart(false);
            Assert.AreEqual(0,flow.KeepIds.Count); Assert.AreEqual(SharedInputStage.Idle,flow.Stage);
            Assert.AreEqual(6,flow.State.Ap); Assert.AreEqual(1,flow.State.Round);
        }

        [UnityTest] public IEnumerator ActualCardDragRoutesToLegacyPartyAndCancelCleansGhost()
        {
            flow.BuildPresentation();yield return null;
            var shield=flow.State.Hand.First(c=>c.Kind==SCard.Shield);
            var hud=host.GetComponentInChildren<BattleHud>();
            Assert.IsNotNull(hud);
            var input=hud.HandRoot.GetComponentsInChildren<CardDragInput>().First(c=>c.CardId==shield.Id);
            var cardPoint=ScreenPoint((RectTransform)input.transform);
            var mageRow=host.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="Party row mage");
            var magePoint=ScreenPoint(mageRow);
            var eventData=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=cardPoint};
            input.OnBeginDrag(eventData);
            Assert.IsNotNull(host.GetComponentsInChildren<RectTransform>().FirstOrDefault(r=>r.name=="Shared drag card"));
            Assert.IsFalse(host.GetComponentsInChildren<CardTargetingArrow>().Any());
            eventData.position=magePoint;input.OnDrag(eventData);input.OnEndDrag(eventData);
            yield return WaitForResolution();
            Assert.AreEqual(3,flow.State.Find("mage").Shield);Assert.AreEqual(5,flow.State.Ap);
            yield return WaitForResolution();
            var beforeAp=flow.State.Ap;var beforeHand=flow.State.Hand.Count;
            input=hud.HandRoot.GetComponentsInChildren<CardDragInput>().First();
            var cancelledCardId=input.CardId;var originalCardAlpha=input.GetComponent<CanvasGroup>().alpha;
            cardPoint=ScreenPoint((RectTransform)input.transform);
            eventData.position=cardPoint;input.OnBeginDrag(eventData);
            eventData.position=new Vector2(Screen.width-10,Screen.height-10);input.OnDrag(eventData);
            flow.Cancel();yield return null;
            Assert.IsFalse(host.GetComponentsInChildren<CardTargetingArrow>(true).Any());
            input=hud.HandRoot.GetComponentsInChildren<CardDragInput>().First(c=>c.CardId==cancelledCardId);
            Assert.AreEqual(originalCardAlpha,input.GetComponent<CanvasGroup>().alpha);
            Assert.AreEqual(beforeAp,flow.State.Ap);Assert.AreEqual(beforeHand,flow.State.Hand.Count);
            Assert.AreEqual(SharedInputStage.Idle,flow.Stage);
        }

        [UnityTest] public IEnumerator FocusLessonUsesCardDragInputAndOverlayAnchorsResolveFromLiveBoard()
        {
            flow.Restart(true);
            flow.BuildPresentation();
            yield return null;
            var overlay=host.GetComponentInChildren<SharedFocusOverlay>();
            Assert.IsNotNull(overlay);
            var focusGraphic=host.GetComponentInChildren<SharedFocusGraphic>();
            Assert.IsNotNull(focusGraphic.GetComponent<CanvasRenderer>(),"The target outline mesh needs a CanvasRenderer to be visible.");
            Assert.IsTrue(focusGraphic.isActiveAndEnabled);
            Assert.IsTrue(overlay.TryGetUnitRect("mage",out var mageRect));
            Assert.Greater(mageRect.width,0);Assert.Greater(mageRect.height,0);
            var mageView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId=="mage");
            var mageSprite=mageView.transform.Find("HD-2D unit billboard").GetComponent<SpriteRenderer>();
            var mageSpriteCenter=flow.ViewCamera.WorldToScreenPoint(mageSprite.transform.TransformPoint(mageSprite.sprite.bounds.center));
            Assert.IsTrue(mageRect.Contains(mageSpriteCenter),
                "The tutorial rectangle must enclose the rendered mage, not a design-time position.");
            AssertScreenRectContainsSprite(mageRect,mageSprite,flow.ViewCamera);
            var originalCameraPosition=flow.ViewCamera.transform.position;
            flow.ViewCamera.transform.position+=flow.ViewCamera.transform.right*.15f+flow.ViewCamera.transform.up*.1f;
            yield return null;
            Assert.IsTrue(overlay.TryGetUnitRect("mage",out var shiftedMageRect));
            Assert.Greater(Vector2.Distance(mageRect.center,shiftedMageRect.center),1f,
                "The anchor must be recalculated after a camera change.");
            AssertScreenRectContainsSprite(shiftedMageRect,mageSprite,flow.ViewCamera);
            flow.ViewCamera.transform.position=originalCameraPosition;
            yield return null;
            var hud=host.GetComponentInChildren<SharedHud>();
            Assert.IsTrue(hud.TryGetFocusUiRect("party:mage",out var magePartyRect));
            Assert.IsTrue(overlay.FocusTargets.Any(r=>r.Contains(magePartyRect.center)),"The active target list must use the live party row's screen-space rectangle.");

            flow.ChooseUnit("mage");flow.ContinueFocusTutorial();
            yield return null;
            Assert.IsTrue(overlay.TryGetCellPolygon(new SPos(4,5),out var polygon));
            Assert.GreaterOrEqual(polygon.Length,4);
            var cellCenter=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(4,5)));
            Assert.IsTrue(overlay.FocusTargets.Any(r=>r.Contains(cellCenter)),"The short-step target must follow the real E6 board cell.");

            flow.ChooseBasic(SActionKind.ShortStep);flow.ClickCell(new SPos(4,5));
            yield return WaitForResolution();
            Assert.IsTrue(overlay.TryGetUnitRect("mage",out var movedMageRect));
            Assert.Greater(Vector2.Distance(mageRect.center,movedMageRect.center),1f,
                "The live sprite rectangle must follow the mage to E6 after movement.");
            mageView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId=="mage");
            mageSprite=mageView.transform.Find("HD-2D unit billboard").GetComponent<SpriteRenderer>();
            AssertScreenRectContainsSprite(movedMageRect,mageSprite,flow.ViewCamera);
            var move=flow.State.Hand.First(c=>c.Kind==SCard.Move);
            var input=hud.Legacy.HandRoot.GetComponentsInChildren<CardDragInput>().First(c=>c.CardId==move.Id);
            var cardPoint=ScreenPoint((RectTransform)input.transform);
            var drag=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=cardPoint};
            input.OnBeginDrag(drag);
            Assert.IsTrue(flow.IsDragging,"The actual CardDragInput must start a controller drag.");
            mageView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId=="mage");
            mageSprite=mageView.transform.Find("HD-2D unit billboard").GetComponent<SpriteRenderer>();
            var magePoint=flow.ViewCamera.WorldToScreenPoint(mageSprite.transform.TransformPoint(mageSprite.sprite.bounds.center));
            drag.position=magePoint;input.OnDrag(drag);input.OnEndDrag(drag);
            Assert.AreEqual(FocusTutorialStep.MoveTarget,flow.FocusStep,"Releasing the real dragged card on the mage selects the taught target step.");
            Assert.AreEqual(6,flow.State.Ap);Assert.AreEqual(5,flow.State.Hand.Count);
        }

        [UnityTest] public IEnumerator AllDiscardCopyDoesNotClaimCardsWillBeKept()
        {
            flow.Restart(true);flow.BuildPresentation();yield return null;
            flow.ChooseUnit("mage");flow.ContinueFocusTutorial();
            flow.ChooseBasic(SActionKind.ShortStep);flow.ClickCell(new SPos(4,5));yield return WaitForResolution();
            var move=flow.State.Hand.First(c=>c.Kind==SCard.Move);flow.BeginCardDrag(move.Id);flow.DropCardOn("mage");flow.ClickCell(new SPos(4,3));yield return WaitForResolution();
            var skill=flow.State.Hand.First(c=>c.Kind==SCard.Skill);flow.BeginCardDrag(skill.Id);flow.DropCardOn("mage");flow.ClickCell(new SPos(3,3));yield return WaitForResolution();
            flow.ChooseUnit("archer");flow.ClickCell(new SPos(2,3));yield return WaitForResolution();
            var shield=flow.State.Hand.First(c=>c.Kind==SCard.Shield);flow.BeginCardDrag(shield.Id);flow.DropCardOn("mage");yield return WaitForResolution();
            flow.BeginEndTurn();
            Assert.AreEqual(FocusTutorialStep.Retain,flow.FocusStep);
            foreach(var card in flow.State.Hand.ToArray())flow.ToggleDiscard(card.Id);
            StringAssert.Contains("弃 2 张",flow.FocusBody);
            StringAssert.Contains("保留 0 张",flow.FocusBody);
            StringAssert.Contains("补 5 张",flow.FocusBody);
            flow.ConfirmEndTurn();
            yield return WaitForResolution();
            Assert.AreEqual(FocusTutorialStep.Ready,flow.FocusStep);
            StringAssert.DoesNotContain("留下的牌还在",flow.FocusTitle);
            StringAssert.Contains("手牌补满",flow.FocusTitle);
            Assert.AreEqual(5,flow.State.Hand.Count);
        }
        [Test] public void MageBurstPreviewIsAtomicAndAffectsOnlyTheEnemyCross()
        {
            flow.State.Find("mage").Position=new SPos(4,3); // E4, within range of D4
            var skill=EnsureCardInHand(SCard.Skill);
            int ap=flow.State.Ap,hand=flow.State.Hand.Count;
            var crossbowHp=flow.State.Find("crossbow").Hp;var patrolHp=flow.State.Find("patrol").Hp;var mageHp=flow.State.Find("mage").Hp;
            flow.BeginCardDrag(skill.Id);Assert.IsTrue(flow.DropCardOn("mage"));
            Assert.AreEqual(ap,flow.State.Ap);Assert.AreEqual(hand,flow.State.Hand.Count);
            Assert.AreEqual(crossbowHp,flow.State.Find("crossbow").Hp);
            flow.Cancel();
            Assert.AreEqual(ap,flow.State.Ap);Assert.AreEqual(hand,flow.State.Hand.Count);
            Assert.AreEqual(new SPos(3,3),flow.State.Find("crossbow").Position);
            flow.BeginCardDrag(skill.Id);Assert.IsTrue(flow.DropCardOn("mage"));flow.ClickCell(new SPos(3,3));
            Assert.AreEqual(crossbowHp-2,flow.State.Find("crossbow").Hp);
            Assert.AreEqual(patrolHp-2,flow.State.Find("patrol").Hp);
            Assert.AreEqual(3,flow.State.Find("guard").Hp);
            Assert.AreEqual(mageHp,flow.State.Find("mage").Hp);
            Assert.AreEqual(ap-1,flow.State.Ap);Assert.AreEqual(hand-1,flow.State.Hand.Count);
        }

        SCardInstance EnsureCardInHand(SCard kind)
        {
            var card=flow.State.Hand.FirstOrDefault(c=>c.Kind==kind);
            if(card!=null)return card;
            card=flow.State.Deck.FirstOrDefault(c=>c.Kind==kind);
            Assert.IsNotNull(card,"The fixture must contain a real "+kind+" card.");
            flow.State.Deck.Remove(card);flow.State.Hand.Add(card);return card;
        }
        [UnityTest] public IEnumerator HudMakesSharedBudgetAndVersionExplicit()
        {
            flow.BuildPresentation(); yield return null;
            var text=string.Join("\n",host.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text));
            StringAssert.Contains("共享 AP",text); StringAssert.Contains("晴岚炮台",text);
            StringAssert.Contains("短步",text); Assert.IsFalse(text.Contains("第 1 / 3 关"));
            var sharedHud=host.GetComponentInChildren<SharedHud>();
            Assert.IsNotNull(sharedHud);
            Assert.AreSame(host.GetComponentInChildren<BattleHud>(),sharedHud.Legacy);
            Assert.IsNotNull(sharedHud.Legacy.PartyPanel);
            Assert.IsNotEmpty(sharedHud.Legacy.HandRoot.GetComponentsInChildren<CardDragInput>());
        }

        [UnityTest] public IEnumerator SelectedCardHighlightsLegalExecutorsAndHoveredHealShowsEffectPreview()
        {
            flow.BuildPresentation();yield return null;
            var shield=flow.State.Hand.First(c=>c.Kind==SCard.Shield);
            flow.SelectCard(shield.Id);
            var vanguardView=host.GetComponentsInChildren<UnitView>().First(v=>v.UnitId=="vanguard");
            Assert.IsTrue(vanguardView.ExecutorHighlighted);
            flow.Cancel();
            var mage=flow.State.Find("mage");mage.Hp=3;
            var heal=EnsureCardInHand(SCard.Heal);flow.BeginCardDrag(heal.Id);
            var screen=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(mage.Position.X,mage.Position.Y)));
            Assert.IsTrue(flow.CanDropAt(screen));
            Assert.IsTrue(flow.Preview.Valid);Assert.AreEqual(6,flow.Preview.After.Find("mage").Hp);
            Assert.AreEqual(3,flow.State.Find("mage").Hp);Assert.AreEqual(6,flow.State.Ap);
            flow.Cancel();yield return null;
        }

        [UnityTest] public IEnumerator AttackCardDrawsAimableWeaponCellsBeyondOccupiedTargets()
        {
            flow.BuildPresentation();yield return null;
            flow.State.Find("archer").Position=new SPos(4,4);flow.State.Find("patrol").Position=new SPos(3,3);
            var attack=flow.State.Hand.First(c=>c.Kind==SCard.Attack);
            Assert.IsTrue(flow.CanExecuteCard("archer",attack.Id));
            flow.BeginCardDrag(attack.Id);
            var archer=flow.State.Find("archer");
            var point=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(archer.Position.X,archer.Position.Y)));
            Assert.IsTrue(flow.CanDropAt(point));
            int edges=flow.Board.GetComponentsInChildren<LineRenderer>().Count(x=>x.gameObject.name=="Shared attack range edge");
            Assert.Greater(edges,8);
            flow.DropAt(point);yield return null;
            Assert.AreEqual("archer",flow.PendingExecutorId);
            Assert.IsFalse(flow.Board.GetComponentsInChildren<LineRenderer>().Any(x=>x.name=="Shared attack range edge"));
            Assert.AreEqual(6,flow.State.Ap);

        }

        [UnityTest] public IEnumerator DiscardButtonsLiftOnlySelectedCardsAndKeepOthers()
        {
            flow.BuildPresentation();yield return null;
            var hud=host.GetComponentInChildren<SharedHud>();
            var confirm=host.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="确认回合");
            confirm.onClick.Invoke();yield return null;
            var cards=hud.Legacy.HandRoot.GetComponentsInChildren<CardDragInput>();
            var selected=cards[0];var retained=cards[1];
            var selectedId=selected.CardId;var retainedId=retained.CardId;
            selected.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            Assert.IsTrue(flow.DiscardIds.Contains(selectedId));
            Assert.Greater(((RectTransform)selected.transform).anchoredPosition.y,((RectTransform)retained.transform).anchoredPosition.y);
            confirm.onClick.Invoke();yield return WaitForResolution();
            Assert.AreEqual(2,flow.State.Round);
            Assert.IsFalse(flow.State.Hand.Any(c=>c.Id==selectedId));
            Assert.IsTrue(flow.State.Hand.Any(c=>c.Id==retainedId));
            Assert.AreEqual(5,flow.State.Hand.Count);
        }

        [UnityTest] public IEnumerator MovingDragBetweenRecipientsAndReleasingOnEmptyGroundCancels()
        {
            flow.BuildPresentation();yield return null;
            var card=flow.State.Hand.First(c=>c.Kind==SCard.Shield);flow.BeginCardDrag(card.Id);
            foreach(var id in new[]{"archer","mage"})
            {
                var row=host.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Party row "+id);
                Assert.IsTrue(flow.CanDropAt(ScreenPoint(row)));Assert.AreEqual(id,flow.PreviewExecutorId);
            }
            var start=flow.State.Find("vanguard").Position;
            var empty=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(0,0)));
            Assert.IsFalse(flow.CanDropAt(empty));Assert.IsNull(flow.PreviewExecutorId);
            flow.DropAt(empty);Assert.IsNull(flow.PendingCardId);Assert.AreEqual(start,flow.State.Find("vanguard").Position);
            Assert.AreEqual(6,flow.State.Ap);Assert.AreEqual(5,flow.State.Hand.Count);
            flow.SelectCard(card.Id);flow.ChooseUnit("mage");
            Assert.AreEqual(3,flow.State.Find("mage").Shield);Assert.AreEqual(5,flow.State.Ap);
            yield return WaitForResolution();
        }

        static Vector2 ScreenPoint(RectTransform rect)
            => RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));

        static void AssertScreenRectContainsSprite(Rect rect,SpriteRenderer sprite,Camera camera)
        {
            var bounds=sprite.sprite.bounds;var min=bounds.min;var max=bounds.max;
            var corners=new[]{new Vector3(min.x,min.y),new Vector3(min.x,max.y),new Vector3(max.x,max.y),new Vector3(max.x,min.y)};
            var tolerant=Rect.MinMaxRect(rect.xMin-.5f,rect.yMin-.5f,rect.xMax+.5f,rect.yMax+.5f);
            foreach(var corner in corners)
            {
                var screen=camera.WorldToScreenPoint(sprite.transform.TransformPoint(corner));
                Assert.IsTrue(tolerant.Contains(screen),"Every projected sprite corner must be inside the live focus rectangle.");
            }
        }

        IEnumerator WaitForResolution()
        {
            float deadline=Time.realtimeSinceStartup+15f;
            while(flow.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(flow.Busy,"Battle presentation did not finish within 15 seconds.");
        }

        Vector2 FindScreenPointInsideCellAndPortrait(SPos cell)
        {
            var center=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(new GridPos(cell.X,cell.Y)));
            var views=host.GetComponentsInChildren<UnitView>();
            for(int y=-40;y<=40;y+=2)for(int x=-40;x<=40;x+=2)
            {
                var point=new Vector2(center.x+x,center.y+y);GridPos hit;
                if(flow.Board.ScreenToGrid(point,out hit)&&hit.X==cell.X&&hit.Y==cell.Y&&views.Any(v=>v.ContainsScreenPoint(point))&&!IsUiBlocked(point))return point;
            }
            Assert.Fail("Could not find a screen coordinate where the legal landing cell and a unit portrait overlap.");return Vector2.zero;
        }

        static bool IsUiBlocked(Vector2 point)
        {
            if(UnityEngine.EventSystems.EventSystem.current==null)return false;
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point},hits);
            return hits.Count>0;
        }

        void RefreshBoard()
            =>typeof(SharedBattleController).GetMethod("RefreshBoard",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(flow,null);
    }
}

