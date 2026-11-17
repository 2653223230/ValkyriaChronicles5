using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE.SharedAP
{
    /// <summary>Shared AP presentation. Legacy fields remain available for scene and test compatibility.</summary>
    public sealed class SharedHud : MonoBehaviour
    {
        SharedBattleController flow;
        public BattleHud Legacy { get; private set; }
        CardHandView hand;
        SharedRoleDiagram diagram;
        RectTransform apRow, lessonMarker;
        Text markerText;
        Text apText, apPreview, statusText, operationHint;
        readonly Image[] apPips=new Image[6];
        Button confirm, cancel, skip;
        RectTransform result;
        Text resultText;
        string handKey, diagramKey;
        bool rebuilding;
        bool cardCancelQueued;
        SharedFocusOverlay focusOverlay;

        void LateUpdate()
        {
            if(!cardCancelQueued)return;
            cardCancelQueued=false;
            if(flow!=null&&flow.isActiveAndEnabled&&!rebuilding&&flow.Stage!=SharedInputStage.Retain)flow.Cancel();
        }

        public void Build(SharedBattleController owner)
        {
            flow=owner;
            var font=owner.UiFont!=null?owner.UiFont:Resources.GetBuiltinResource<Font>("Arial.ttf");
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;gameObject.AddComponent<GraphicRaycaster>();
            var root=(RectTransform)transform;
            Legacy=gameObject.AddComponent<BattleHud>();Legacy.Build(root,font);Legacy.ConfigureParty(owner.UnitSprites);
            Legacy.SelectUnit=flow.ChooseUnit;Legacy.SelectAction=a=>flow.ChooseBasic(a==ActionKind.Move?SActionKind.ShortStep:SActionKind.BasicAttack);
            Legacy.EndTurn=flow.BeginEndTurn;Legacy.ExchangeMode=flow.BeginEndTurn;Legacy.CancelAction=flow.Cancel;
            Legacy.DeploymentRoot.gameObject.SetActive(false);Legacy.PreviewRoot.gameObject.SetActive(false);Legacy.ActionMenu.gameObject.SetActive(false);
            Legacy.End.gameObject.SetActive(false);Legacy.Exchange.gameObject.SetActive(false);
            Legacy.Header.gameObject.SetActive(false);
            Legacy.transform.Find("Top rail")?.gameObject.SetActive(false);
            Legacy.transform.Find("Hint backing")?.gameObject.SetActive(false);
            Legacy.transform.Find("Active growth")?.gameObject.SetActive(false);
            Legacy.transform.Find("Intent panel")?.gameObject.SetActive(false);
            Legacy.transform.Find("Current acting unit")?.gameObject.SetActive(false);
            Legacy.transform.Find("Target preview")?.gameObject.SetActive(false);
            Legacy.transform.Find("Piece action menu")?.gameObject.SetActive(false);
            Legacy.transform.Find("Deploy controls")?.gameObject.SetActive(false);
            Legacy.PartyPanel.transform.SetParent(root,false);
            var party=(RectTransform)Legacy.PartyPanel.transform;
            party.anchorMin=party.anchorMax=new Vector2(0,1);party.pivot=new Vector2(0,1);party.anchoredPosition=Vector2.zero;party.sizeDelta=new Vector2(380,1080);
            var partyPanel=party.Find("Party panel") as RectTransform;
            if(partyPanel!=null){partyPanel.anchoredPosition=new Vector2(14,-78);partyPanel.sizeDelta=new Vector2(332,340);}
            Legacy.PartyPanel.ConfigureSharedLayout(312);
            hand=gameObject.AddComponent<CardHandView>();hand.Build(Legacy.HandRoot,font,owner.UnitSprites);
            var handRect=Legacy.HandRoot;handRect.anchorMin=handRect.anchorMax=new Vector2(0,1);handRect.pivot=new Vector2(0,1);
            handRect.anchoredPosition=new Vector2(356,-838);handRect.sizeDelta=new Vector2(1490,220);handRect.GetComponent<Image>().color=Color.clear;handRect.GetComponent<Image>().raycastTarget=false;
            hand.Selected=id=>{if(flow.Stage==SharedInputStage.Retain)flow.ToggleDiscard(id);else if(flow.PendingCardId==id)flow.Cancel();else flow.SelectCard(id);};
            hand.SharedDragSummary=()=>flow.PreviewExecutorId==null?"拖到队员查看效果":flow.Hint.Split('\n')[0];
            hand.DragStarted=flow.BeginCardDrag;hand.Dropped=(id,pos)=>flow.DropAt(pos);
            hand.DragCancelled=id=>{if(!rebuilding)cardCancelQueued=true;};hand.CanDrop=flow.CanDropAt;hand.AimOrigin=flow.AimOrigin;
            hand.Hovered=c=>{if(!rebuilding)flow.InspectCard(c.Name);};hand.HoverExited=()=>{if(!rebuilding)flow.InspectCard(null);};
            BuildLeftPanel(root,font);
            BattleHud.Panel(root,"Shared hint backing",586,24,850,76,SharedPaper.Paper).GetComponent<Image>().raycastTarget=false;
            operationHint=BattleHud.Label(root,"Current shared operation",604,32,812,62,20,SharedPaper.Ink,font);
            lessonMarker=BattleHud.Panel(root,"Lesson board cue",0,0,136,32,SharedPaper.Paper,SharedPaper.Gold);
            markerText=BattleHud.Label(lessonMarker,"Next action",4,2,128,28,17,SharedPaper.Green,font);
            markerText.alignment=TextAnchor.MiddleCenter;
            foreach(var image in lessonMarker.GetComponentsInChildren<Graphic>())image.raycastTarget=false;
            operationHint.verticalOverflow=VerticalWrapMode.Overflow;
            apRow=BattleHud.Panel(root,"Shared AP row",356,779,640,43,SharedPaper.Paper);
            apText=BattleHud.Label(apRow,"Shared AP count",12,2,196,36,20,SharedPaper.Ink,font);apText.text="全队共享 AP";
            for(int i=0;i<apPips.Length;i++)
            {
                var pip=BattleHud.Panel(apRow,"Shared AP pip "+i,212+i*20,12,14,18,new Color(.20f,.72f,.89f));
                apPips[i]=pip.GetComponent<Image>();
                foreach(var graphic in pip.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
            }
            apPreview=BattleHud.Label(apRow,"Shared AP preview",342,3,292,34,18,SharedPaper.Green,font);
            var statusPanel=BattleHud.Panel(root,"Turn slip",1004,779,308,43,SharedPaper.Paper);
            statusPanel.GetComponent<Image>().raycastTarget=false;
            statusText=BattleHud.Label(statusPanel,"Shared turn status",12,5,290,30,17,SharedPaper.Ink,font);
            confirm=BattleHud.ButtonAt(root,"确认回合",1610,840,278,58,font,ConfirmTurn,true);
            cancel=BattleHud.ButtonAt(root,"返回",1610,914,278,50,font,flow.Cancel);
            skip=BattleHud.ButtonAt(root,"跳过教学",24,950,225,48,font,flow.RequestSkipTutorial);skip.targetGraphic.color=SharedPaper.Green;
            confirm.targetGraphic.color=SharedPaper.Green;cancel.targetGraphic.color=SharedPaper.Green;
            result=BattleHud.Panel(root,"Shared result",520,300,880,420,SharedPaper.Paper,SharedPaper.Edge);
            resultText=BattleHud.Label(result,"Result",40,28,800,210,30,SharedPaper.Ink,font);resultText.verticalOverflow=VerticalWrapMode.Overflow;
            BattleHud.ButtonAt(result,"再试一次",40,272,370,84,font,()=>flow.Restart(true),true);
            BattleHud.ButtonAt(result,"随机手牌再挑战",470,272,370,84,font,()=>flow.Restart(false));
            result.gameObject.SetActive(false);
            focusOverlay=gameObject.AddComponent<SharedFocusOverlay>();focusOverlay.Build(flow,this,font);
            Render();
        }

        void BuildLeftPanel(RectTransform root,Font font)
        {
            var sidebar=BattleHud.Panel(root,"Shared role information",0,0,380,1080,Color.clear);
            sidebar.anchorMin=sidebar.anchorMax=new Vector2(0,1);sidebar.pivot=new Vector2(0,1);
            sidebar.SetAsFirstSibling();sidebar.GetComponent<Image>().raycastTarget=false;
            var mission=BattleHud.Panel(sidebar,"Mission slip",24,22,304,70,SharedPaper.Paper);mission.GetComponent<Image>().raycastTarget=false;
            var title=BattleHud.Label(mission,"Shared encounter",16,4,270,34,24,SharedPaper.Ink,font);title.text="晴岚炮台";title.verticalOverflow=VerticalWrapMode.Overflow;
            var caption=BattleHud.Label(mission,"Shared goal",16,40,270,26,16,SharedPaper.Green,font);caption.text="击毁炮座 或 击败三名敌兵";
            diagram=gameObject.AddComponent<SharedRoleDiagram>();diagram.Build(sidebar,font);
        }

        public void ClearDragVisuals(){if(hand!=null)hand.CancelDragVisuals();}
        public string PartyAt(Vector2 p)=>Legacy==null?null:Legacy.PartyUnitAtScreen(p);
        // Resolve current instances, including rebuilt and lifted cards; never use Figma coordinates.
        public bool TryGetFocusUiRect(string key,out Rect screenRect)
        {
            RectTransform target=null;
            if(key=="ap")target=apRow;
            else if(key=="confirm")target=confirm.transform as RectTransform;
            else if(key=="cancel")target=cancel.transform as RectTransform;
            else if(key=="skip")target=skip.transform as RectTransform;
            else if(key=="role")target=transform.Find("Shared role information/Role diagram") as RectTransform;
            else if(key.StartsWith("party:"))target=Legacy.PartyPanel.transform.Find("Party panel/Party row "+key.Substring(6)) as RectTransform;
            else if(key.StartsWith("card:"))
            {
                var input=Legacy.HandRoot.GetComponentsInChildren<CardDragInput>().FirstOrDefault(c=>c.CardId==key.Substring(5));
                if(input!=null)target=input.transform as RectTransform;
            }
            screenRect=default(Rect);
            if(target==null||!target.gameObject.activeInHierarchy)return false;
            var corners=new Vector3[4];target.GetWorldCorners(corners);
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
            var canvas=target.GetComponentInParent<Canvas>();var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(cam,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            screenRect=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return screenRect.width>0&&screenRect.height>0;
        }
        public void Render()
        {
            if(!isActiveAndEnabled||Legacy==null||flow==null||!flow.isActiveAndEnabled||flow.State==null)return;
            var s=flow.State;var actor=s.Find(flow.SelectedId);var previewActor=s.Find(flow.PreviewExecutorId);
            if(previewActor==null)previewActor=actor;
            var visual=SharedVisualAdapter.ToBattleState(s);var selected=actor==null?null:visual.FindUnit(actor.Id);
            bool active=s.Phase==SPhase.Player&&!flow.Busy;
            Legacy.PartyPanel.RenderPresentation(visual,selected,!active,id=>s.Find(id).Name,id=>WeaponLine(s.Find(id)),id=>RoleStatus(s.Find(id)));
            string candidateCard=flow.PendingExecutorId==null?flow.PendingCardId:null;
            var highlighted=flow.PreviewExecutorId!=null&&flow.IsDragging?new[]{flow.PreviewExecutorId}:
                string.IsNullOrEmpty(candidateCard)?null:s.Units.Where(u=>u.Player&&u.Alive&&flow.CanExecuteCard(u.Id,candidateCard)).Select(u=>u.Id).ToArray();
            if(highlighted==null&&active&&flow.PendingCardId==null&&(flow.TutorialStep==1||flow.TutorialStep==7))
                highlighted=new[]{flow.TutorialFocusUnitId};
            if(flow.FocusTutorialActive&&highlighted!=null)highlighted=highlighted.Where(flow.FocusAllowsUnit).ToArray();
            Legacy.PartyPanel.SetSharedExecutors(highlighted);
            Legacy.SetActingUnit(selected);
            string detailKey=(previewActor==null?"none":previewActor.Id+":"+previewActor.Braced)+"/"+s.Revision+"/"+flow.ShowRoleDetails+"/"+(flow.PreviewExecutorId!=null);
            if(diagramKey!=detailKey){diagramKey=detailKey;diagram.Render(flow.ShowRoleDetails?previewActor:null,s);}
            string cardKey=string.Join("|",s.Hand.Select(c=>c.Id))+"/"+s.Revision+"/"+flow.PendingCardId+"/"+(previewActor==null?"":previewActor.Id)+"/"+active+"/"+flow.Stage+"/"+flow.ShowRoleDetails+"/"+flow.TutorialStep+"/"+flow.FocusStep;
            if(!flow.IsDragging&&handKey!=cardKey)
            {
                handKey=cardKey;rebuilding=true;
                hand.RenderPresentation(s.Hand.Select(c=>{
                    var preferred=previewActor;
                    var executor=preferred!=null&&flow.CanExecuteCard(preferred.Id,c.Id)?preferred:
                        s.Units.FirstOrDefault(u=>u.Player&&u.Alive&&flow.CanExecuteCard(u.Id,c.Id));
                    bool availableAnywhere=s.Units.Any(u=>u.Player&&u.Alive&&flow.CanExecuteCard(u.Id,c.Id));
                    bool usable=flow.Stage==SharedInputStage.Retain||(flow.ShowRoleDetails&&preferred!=null?flow.CanExecuteCard(preferred.Id,c.Id):availableAnywhere);
                    return new CardHandView.Presentation{Id=c.Id,Name=SharedRules.CardName(c.Kind),Cost=SharedRules.Cost(c.Kind),
                        UserLabel=c.Id==flow.TutorialFocusCardId&&flow.PendingCardId==null?"拖给术师":flow.Stage==SharedInputStage.Retain?"待选弃牌":flow.ShowRoleDetails&&usable&&executor!=null?executor.Name+"可执行":availableAnywhere?"":"当前无合法执行者",
                        Description=SharedRules.CardText(c.Kind),Usable=usable&&flow.FocusAllowsCard(c.Id),Draggable=availableAnywhere&&flow.FocusAllowsCard(c.Id)};
                }).ToList(),flow.Stage==SharedInputStage.Retain?null:flow.PendingCardId,active);
                rebuilding=false;
            }
            if(flow.Stage==SharedInputStage.Retain)hand.SetSelections(flow.DiscardIds);
            else hand.SetSelection(flow.PendingCardId);
            if(flow.Stage==SharedInputStage.Retain)
                foreach(var input in Legacy.HandRoot.GetComponentsInChildren<CardDragInput>())
                {input.SetUsable(false);input.SetInteractionEnabled(false);}
            else if(flow.FocusTutorialActive)
                foreach(var input in Legacy.HandRoot.GetComponentsInChildren<CardDragInput>())
                    input.SetInteractionEnabled(active&&flow.FocusAllowsCard(input.CardId));
            apText.text="全队共享 AP     "+s.Ap+" / 6";
            for(int i=0;i<apPips.Length;i++)apPips[i].color=i>=s.Ap?SharedPaper.Edge:
                flow.PreviewApCost>0&&i>=flow.PreviewApRemaining?SharedPaper.Gold:SharedPaper.Green;
            apPreview.text=flow.PreviewApCost>0?"拖牌预览 −"+flow.PreviewApCost+" AP  ·  剩余 "+flow.PreviewApRemaining:
                flow.Preview!=null&&flow.Preview.Valid?"行动后剩余 "+(flow.Preview.After==null?s.Ap:flow.Preview.After.Ap):"本轮剩余 "+s.Ap;
            statusText.text=flow.Stage==SharedInputStage.Retain?"弃 "+flow.DiscardIds.Count+" · 留 "+(s.Hand.Count-flow.DiscardIds.Count)+" · 下轮补 "+Mathf.Max(0,5-s.Hand.Count+flow.DiscardIds.Count)+" 张":
                "第 "+s.Round+" 回合"+(flow.Busy?" · 行动演出中":" · 我方行动");
            confirm.gameObject.SetActive(active);cancel.gameObject.SetActive(active&&(flow.Stage!=SharedInputStage.Idle||flow.PendingCardId!=null));
            confirm.GetComponentInChildren<Text>().text=flow.Stage==SharedInputStage.Retain?(flow.DiscardIds.Count==0?"直接结束（不弃牌）":"弃牌并结束（"+flow.DiscardIds.Count+"）"):"结束回合";
            confirm.interactable=active&&(!flow.FocusTutorialActive||flow.FocusStep==FocusTutorialStep.EndTurn||flow.FocusStep==FocusTutorialStep.Retain);
            if(active&&flow.ActionMenuVisible)Legacy.HideActions();
            Legacy.HideActions();
            string actorText=previewActor==null?"点击卡牌查看合法执行队员；可直接把卡拖给队员。":previewActor.Name+" · "+WeaponLine(previewActor);
            Legacy.SetPresentationCopy("共享 AP · 晴岚炮台    第 "+s.Round+" 回合    全队 AP "+s.Ap+" / 6",flow.Hint,
                flow.Preview==null?"":"行动预览\n"+(flow.Preview.Valid?flow.Preview.Summary:flow.Preview.Reason),
                previewActor==null?"":SharedRoleInfo.Skill(previewActor),actorText);
            string lesson=flow.TutorialText;
            operationHint.text=lesson??flow.Hint;
            if(flow.IsDragging&&flow.PreviewExecutorId!=null)
            {
                var card=s.Hand.FirstOrDefault(c=>c.Id==flow.PendingCardId);
                bool immediate=card!=null&&(card.Kind==SCard.Heal||card.Kind==SCard.Shield||
                    card.Kind==SCard.Skill&&previewActor.Role==SRole.Archer);
                operationHint.text=immediate?"松手生效，移出队员取消。":
                    "松手锁定"+previewActor.Name+"，再"+(card!=null&&(card.Kind==SCard.Move||card.Kind==SCard.Charge)?"选落点。":"选敌人。");
            }
            if(active && flow.TutorialStep>0&&!flow.FocusTutorialActive)
                operationHint.text=flow.TutorialStep+" / 9    "+operationHint.text;
            if(flow.FocusTutorialActive)operationHint.text=flow.FocusChapter;
            ShowLessonMarker(active);
            skip.gameObject.SetActive(flow.Teaching);skip.interactable=active&&!flow.FocusExitOpen&&string.IsNullOrEmpty(flow.FocusRecoveryReason);
            bool done=!flow.Busy&&(s.Phase==SPhase.Won||s.Phase==SPhase.Lost);result.gameObject.SetActive(done);
            resultText.text=(s.Phase==SPhase.Won?flow.VictoryReason+" · 胜利":"小队倒下 · 挑战结束")+"\n\n晴岚炮台 · 第 "+s.Round+" 回合\n"+
                (s.Phase==SPhase.Won?"晴岚炮台作战完成。":"调整站位和用牌顺序，再试一次。");
            if(done)result.SetAsLastSibling();
        }
        void ShowLessonMarker(bool active)
        {
            lessonMarker.gameObject.SetActive(false);
            if(flow.FocusTutorialActive||!active||flow.Board==null||flow.ViewCamera==null||flow.IsDragging||flow.Stage==SharedInputStage.Retain)return;
            GridPos cell;
            string caption;
            switch(flow.TutorialStep)
            {
                case 2: if(flow.SelectedId!="mage"||flow.PendingCardId!=null)return;cell=new GridPos(4,5);caption="E6 · 免费短步";break;
                case 4: if(flow.PendingExecutorId!="mage")return;cell=new GridPos(4,3);caption="E4 · 落点";break;
                case 6: if(flow.PendingExecutorId!="mage")return;cell=new GridPos(3,3);caption="D4 · 引爆";break;
                case 8: if(flow.SelectedId!="archer"||flow.PendingCardId!=null)return;cell=new GridPos(2,3);caption="C4 · 普攻";break;
                default:return;
            }
            var screen=flow.ViewCamera.WorldToScreenPoint(flow.Board.World(cell));
            var canvas=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out var point))return;
            lessonMarker.localPosition=new Vector3(point.x+30,point.y+10,0);
            markerText.text=caption;lessonMarker.gameObject.SetActive(true);
        }

        void ConfirmTurn(){if(flow.Stage==SharedInputStage.Retain)flow.ConfirmEndTurn();else flow.BeginEndTurn();}
        static string RoleStatus(SUnit u)=>"短步 "+(u.StepUsed?"已用":"可用")+" · 护盾 "+u.Shield;
        static string WeaponLine(SUnit u)=>u.Role==SRole.Vanguard?"3伤 · 邻格单体":u.Role==SRole.Archer?(u.Braced?"4伤 · 射程4 · 移动解除架弩":"2伤 · 射程3"):"1伤 · 射程2 · 邻格溅射";
    }
    // Retained marker for scene compatibility; the legacy portrait panel handles hit testing.
    public sealed class SharedUnitDropZone:MonoBehaviour{public string UnitId;}
}
