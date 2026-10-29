using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace VC5PvE
{
    public sealed class PrototypeFlow : MonoBehaviour
    {
        public bool IsTitle;
        public Font UiFont;
        public Sprite TitleBackground;
        public Texture2D GroundAtlas;
        public Sprite[] ScenerySprites;
        public Sprite[] UnitSprites;
        public GameObject[] TreePrefabs, RockPrefabs, ShrubPrefabs, RuinPrefabs;
        public AudioClip[] SoundClips;
        public BattleController Controller { get; private set; }
        public BattleHud Hud { get; private set; }
        public RunState Run { get; private set; }
        public CampaignCampView Camp { get; private set; }
        private RecoveryPickerView recovery;
        private bool inCamp;
        private static int? requestedSeed;
        private readonly List<string> highlights = new List<string>();
        private BoardView board;
        private CardHandView hand;
        private RectTransform root, resultRoot;
        private string selected, cardId, dragUnit;
        private ActionPreviewOverlay overlay;
        private string previewKey;
        private bool cardDragging;
        private enum CardStage { None, Target, ChargeTarget }
        private CardStage cardStage;
        private GridPos? chargeLanding;
        private int suppressBoardClickFrame = -1;
        private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        private ActionKind? mode;
        
        private bool exchanging, actionMenuRequested;
        private AudioSource sound;

        private void Awake()
        {
            if(UiFont==null) UiFont=Resources.GetBuiltinResource<Font>("Arial.ttf");
            if(EventSystem.current==null) new GameObject("Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            var canvas=new GameObject("Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            root=(RectTransform)canvas.transform;
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.volume=.3f;
            if(IsTitle) { BuildTitle();return; }
            board=new GameObject("Board View").AddComponent<BoardView>();
            board.TreePrefabs=TreePrefabs;board.RockPrefabs=RockPrefabs;board.ShrubPrefabs=ShrubPrefabs;board.UnitSprites=UnitSprites;
            board.RuinPrefabs=RuinPrefabs;
            board.GroundAtlas=GroundAtlas;
            board.ScenerySprites=ScenerySprites;
            Controller=gameObject.AddComponent<BattleController>();Controller.Board=board;Controller.ViewCamera=Camera.main;
            Hud=canvas.AddComponent<BattleHud>();Hud.Build(root,UiFont);Hud.ConfigureParty(UnitSprites);
            overlay=canvas.AddComponent<ActionPreviewOverlay>();overlay.Initialize(root,board,Camera.main,UiFont);
            Hud.SelectUnit=SelectPartyUnit;
            hand=canvas.AddComponent<CardHandView>();hand.Build(Hud.HandRoot,UiFont,UnitSprites);
            Hud.SelectAction=SelectBasic;Hud.BeginBattle=()=>{CancelSelection();selected=null;Controller.BeginBattle();};
            Hud.EndTurn=()=>{if(Controller.HasPendingRecovery)return;CancelSelection();StartCoroutine(Controller.EndTurn());};
            Hud.ExchangeMode=()=>{CancelSelection();exchanging=true;Hud.SetHint("选择一张手牌换走：放至牌库底，抽取另一张。ESC 取消。");};
            Hud.CancelAction=CancelSelection;Hud.ConfirmAction=null;
            hand.Selected=SelectCard;hand.Hovered=c=>{if(!mode.HasValue)Hud.ShowCardDetails(c);};hand.Dropped=DropCard;
            hand.DragStarted=BeginCardDrag;hand.DragCancelled=CancelCardDrag;hand.CanDrop=CanDropCardAt;
            hand.AimOrigin=()=>{var actor=Controller==null||Controller.State==null?null:Controller.State.FindUnit(selected);return actor==null? (Vector2?)null:Controller.ViewCamera.WorldToScreenPoint(board.World(actor.Position)+Vector3.up*.8f);};
            hand.AvailabilityHint=id=>{var reason=CardAimResolver.UnavailableReason(Controller.State,id,selected);if(!string.IsNullOrEmpty(reason))Hud.SetHint(reason);};
            hand.HoverExited=()=>{if(!mode.HasValue)Hud.RestoreIntent();};
            recovery=canvas.AddComponent<RecoveryPickerView>();recovery.Build(root,UiFont);
            Camp=new GameObject("Campaign Camp",typeof(RectTransform)).AddComponent<CampaignCampView>();
            Camp.Build(root,UiFont,UnitSprites);Camp.Hide();
            Controller.Changed+=OnControllerChanged;Controller.Message+=s=>{Hud.SetHint(Localize(s));if(s!=null&&s.Length>0){highlights.Add(Localize(s));if(highlights.Count>4)highlights.RemoveAt(0);}PlaySound();};
            Run=CampaignRules.NewRun(requestedSeed ?? Controller.BattleSeed);requestedSeed=null;
            Controller.CreateBattle(Run);
        }

        private void BuildTitle()
        {
            var image=BattleHud.Rect(root,"Forest illustration",0,0,1920,1080).gameObject.AddComponent<Image>();
            image.sprite=TitleBackground;image.color=Color.white;
            BattleHud.Panel(root,"Title shade",0,0,620,1080,new Color(.005f,.025f,.04f,.48f));
            var name=BattleHud.Label(root,"Game title",102,228,890,120,70,BattleHud.Gold,UiFont);name.text="森 钟 战 术";
            BattleHud.Label(root,"Subtitle",112,359,740,50,26,BattleHud.Paper,UiFont).text="FOREST BELL TACTICS   /   卡牌 · 战棋";
            BattleHud.Label(root,"Demo scope",112,648,640,52,24,BattleHud.Jade,UiFont).text="三人小队，三场决战。让配合在旅途中成长。";
            BattleHud.ButtonAt(root,"开始游戏",112,750,350,72,UiFont,()=>{requestedSeed=unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode());SceneManager.LoadScene("ForestRuins");},true);
            BattleHud.ButtonAt(root,"退出游戏",112,846,350,64,UiFont,()=>Application.Quit());
            BattleHud.Label(root,"Version",112,989,960,36,18,BattleHud.Paper,UiFont).text="三关成长试玩版  ·  2026.10.21";
        }

        private void Update()
        {
            if(IsTitle||Controller==null||Controller.State==null||inCamp)return;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1))
            {
                if(recovery!=null&&recovery.Visible)
                {
                    recovery.Hide();
                    if(Controller.HasPendingRecovery)Controller.ResolveRecovery(null);
                    else Render();
                    return;
                }
                CancelSelection();return;
            }
            if(recovery!=null&&recovery.Visible)return;
            if(Controller.HasPendingRecovery)return;
            if(Controller.IsBusy)return;
            var state=Controller.State;
            if(state.Phase!=BattlePhase.Deployment&&state.Phase!=BattlePhase.Player)return;
            bool over=OverUi(Input.mousePosition);
            if(mode.HasValue&&!cardDragging)
                UpdateAimAt(Input.mousePosition);
            if(Input.GetMouseButtonDown(0)&&!over&&!cardDragging&&Time.frameCount!=suppressBoardClickFrame)
            {
                GridPos cell;bool onBoard=board.ScreenToGrid(Input.mousePosition,out cell);
                var unit=PickUnit(Input.mousePosition,onBoard?(GridPos?)cell:null);
                if(state.Phase==BattlePhase.Deployment)
                {
                    if(unit!=null&&unit.Team==Team.Player){selected=unit.Id;dragUnit=unit.Id;Render();}
                    else if(onBoard){Controller.Deploy(selected,cell);ShowDeploy();}
                }
                else ClickBattle(unit,onBoard?(GridPos?)cell:null);
            }
            if(Input.GetMouseButtonUp(0)&&dragUnit!=null)
            {
                GridPos cell;if(!over&&board.ScreenToGrid(Input.mousePosition,out cell))Controller.Deploy(dragUnit,cell);
                dragUnit=null;ShowDeploy();
            }
        }
        private bool OverUi(Vector2 screen)
        {
            if(EventSystem.current==null)return false;
            uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},uiHits);
            return uiHits.Count>0;
        }
        private UnitState PickUnit(Vector2 screen,GridPos? cell)
        {
            UnitState portraitHit=null;float nearest=float.MaxValue;
            foreach(var view in board.GetComponentsInChildren<UnitView>())
            {
                if(!view.ContainsScreenPoint(screen)) continue;
                var candidate=Controller.State.FindUnit(view.UnitId);
                if(candidate==null || !candidate.IsAlive)continue;
                float distance=Vector2.Distance(screen,Controller.ViewCamera.WorldToScreenPoint(view.transform.position+Vector3.up*.75f));
                if(distance<nearest){nearest=distance;portraitHit=candidate;}
            }
            if(portraitHit!=null) return portraitHit;
            UnitState closest=null;float best=50f*Screen.height/1080f;
            foreach(var u in Controller.State.Units)
            {
                if(!u.IsAlive)continue;
                Vector2 point=Controller.ViewCamera.WorldToScreenPoint(board.World(u.Position)+Vector3.up*.7f);
                float d=Vector2.Distance(screen,point);if(d<best){best=d;closest=u;}
            }
            return closest ?? (cell.HasValue?Controller.State.UnitAt(cell.Value):null);
        }


        private void SelectPartyUnit(string id)
        {
            if(cardDragging||Controller.IsBusy||Controller.HasPendingRecovery||inCamp)return;var unit=Controller.State.FindUnit(id);
            if(unit==null||!unit.IsAlive||unit.Team!=Team.Player)return;
            if(mode.HasValue)
            {
                var plan=ResolveAim(unit,unit.Position,ProjectCell(unit.Position));
                if(plan.IsValid){PresentPlan(plan);CommitPreview();return;}
            }
            CancelSelection();selected=id;actionMenuRequested=Controller.State.Phase==BattlePhase.Player;Render();
        }
        private void EnsureSelected()
        {
            var u=Controller.State.FindUnit(selected);
            if(u==null||!u.IsAlive||u.Team!=Team.Player)selected=null;
        }
        private void ClickBattle(UnitState unit,GridPos? cell)
        {
            if(Controller.IsBusy||Controller.HasPendingRecovery||inCamp||Controller.State.Phase!=BattlePhase.Player)return;
            if(exchanging){Hud.SetHint("请点选要换走的手牌，或按 ESC 取消。");return;}
            if(!mode.HasValue)
            {
                actionMenuRequested=unit!=null&&unit.Team==Team.Player&&unit.IsAlive;
                if(actionMenuRequested)selected=unit.Id;
                Render();return;
            }
            var point=unit!=null?ProjectCell(unit.Position):cell.HasValue?ProjectCell(cell.Value):Vector2.zero;
            if(mode==ActionKind.Card && FindCard()!=null && FindCard().Kind==CardKind.Charge)
            {
                if(cardStage==CardStage.Target)
                {
                    if(!cell.HasValue||!CardAimResolver.LegalChargeLandings(Controller.State,cardId,selected).Contains(cell.Value))return;
                    chargeLanding=cell.Value;cardStage=CardStage.ChargeTarget;ClearLivePreview();ShowTargets();
                    Hud.SetHint("落点已选。再点相邻的高亮敌人执行突入；此步尚未移动或扣费。ESC 取消。");return;
                }
                if(unit==null||!chargeLanding.HasValue||!CardAimResolver.LegalChargeTargets(Controller.State,cardId,selected,chargeLanding.Value).Contains(unit.Position))return;
            }
            PresentPlan(ResolveAim(unit,cell,point));
            CommitPreview();
        }
        private CardInstance FindCard(){return Controller.State.Hand.Find(c=>c.Id==cardId);}
        private void SelectCard(string id)
        {
            if(Controller.IsBusy||Controller.HasPendingRecovery||inCamp||Controller.State.Phase!=BattlePhase.Player)return;
            if(exchanging){if(Controller.Exchange(id))CancelSelection();return;}
            EnsureSelected();if(selected==null){Hud.SetHint("先点击己方棋子或左侧头像选择行动者。");return;}
            var reason=CardAimResolver.UnavailableReason(Controller.State,id,selected);
            if(!string.IsNullOrEmpty(reason)){Hud.SetHint(reason);return;}
            ResetSelectionState(false);BeginCardSelection(id);hand.Render(Controller.State,cardId,true,selected);
            if(FindCard()!=null&&FindCard().Kind==CardKind.RecoverTactics)OpenRecoveryPicker();
        }
        private void BeginCardDrag(string id)
        {
            if(Controller.IsBusy||Controller.HasPendingRecovery||inCamp||Controller.State.Phase!=BattlePhase.Player||exchanging)return;
            EnsureSelected();if(selected==null){Hud.SetHint("先点击己方棋子或左侧头像选择行动者。");return;}
            var reason=CardAimResolver.UnavailableReason(Controller.State,id,selected);
            if(!string.IsNullOrEmpty(reason)){Hud.SetHint(reason);return;}
            ResetSelectionState(false);BeginCardSelection(id);cardDragging=true;hand.SetSelection(id);
        }
        private void BeginCardSelection(string id)
        {
            if(!Controller.State.Hand.Exists(c=>c.Id==id))return;
            EnsureSelected();if(selected==null)return;
            var reason=CardAimResolver.UnavailableReason(Controller.State,id,selected);
            if(!string.IsNullOrEmpty(reason)){Hud.SetHint(reason);return;}
            cardId=id;mode=ActionKind.Card;cardStage=CardStage.Target;
            Hud.HideActions();Hud.Render(Controller.State,Controller.State.FindUnit(selected),Controller.Intent(),false);board.Select(selected);
            board.ClearHighlights();board.ShowExecutors(null);
            Hud.ShowCardDetails(Definitions.Card(FindCard()));
            Hud.SetHint("行动者："+BattleHud.Name(Controller.State.FindUnit(selected).Role)+"。点击高亮目标出牌；也可拖牌到最终目标。ESC 取消。");
            ShowTargets();
        }
        private Vector2 ProjectCell(GridPos p){return Controller.ViewCamera.WorldToScreenPoint(board.World(p));}
        private ActionPlan ResolveAim(UnitState unit,GridPos? cell,Vector2 pointer)
        {
            if(mode==ActionKind.Card && FindCard()!=null && FindCard().Kind==CardKind.Charge)
                return ActionPlanner.Plan(Controller.State,new ActionRequest{Kind=ActionKind.Card,CardId=cardId,ActorId=selected,TargetId=unit==null?null:unit.Id,Destination=chargeLanding});
            if(mode==ActionKind.Card)return CardAimResolver.Resolve(Controller.State,cardId,selected,unit,cell,pointer,ProjectCell);
            return ActionPlanner.Plan(Controller.State,new ActionRequest{Kind=mode.Value,ActorId=selected,TargetId=unit==null?null:unit.Id,Destination=cell});
        }
        private bool UpdateAimAt(Vector2 screen)
        {
            if(!mode.HasValue||Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player)return false;
            if(OverUi(screen))
            {
                var portraitId=Hud.PartyUnitAtScreen(screen);
                if(portraitId==null){ClearLivePreview();return false;}
                var portraitTarget=Controller.State.FindUnit(portraitId);
                if(portraitTarget==null){ClearLivePreview();return false;}
                var portraitPlan=ResolveAim(portraitTarget,portraitTarget.Position,ProjectCell(portraitTarget.Position));
                PresentPlan(portraitPlan);return portraitPlan.IsValid;
            }
            GridPos cell;bool inside=board.ScreenToGrid(screen,out cell);var unit=PickUnit(screen,inside?(GridPos?)cell:null);
            if(!inside&&unit==null){ClearLivePreview();return false;}
            if(mode==ActionKind.Card&&FindCard()!=null&&FindCard().Kind==CardKind.Charge&&cardStage==CardStage.Target)
            {
                ClearLivePreview();bool legal=inside&&CardAimResolver.LegalChargeLandings(Controller.State,cardId,selected).Contains(cell);
                Hud.ShowLivePreview(legal?"突入落点\n此步只选位置；点击后再选择相邻敌人。\n尚未移动、扣费或造成伤害。":"先选高亮落点，再选择相邻敌人。",legal);
                return legal;
            }
            var plan=ResolveAim(unit,inside?(GridPos?)cell:null,screen);PresentPlan(plan);return plan.IsValid;
        }
        private UnitState UnitAtPointer(Vector2 screen)
        {
            var portraitId=Hud.PartyUnitAtScreen(screen);
            if(portraitId!=null)return Controller.State.FindUnit(portraitId);
            if(OverUi(screen))return null;
            GridPos cell;bool inside=board.ScreenToGrid(screen,out cell);
            return PickUnit(screen,inside?(GridPos?)cell:null);
        }
        private bool CanDropCardAt(Vector2 screen)
        {
            if(!cardDragging || exchanging || Controller.IsBusy || Controller.State.Phase!=BattlePhase.Player)return false;
            var portraitId=Hud.PartyUnitAtScreen(screen);
            if(OverUi(screen)&&portraitId==null){ClearLivePreview();return false;}
            var actor=Controller.State.FindUnit(selected);if(actor==null||!actor.IsAlive){ClearLivePreview();return false;}
            var card=FindCard();if(card==null){ClearLivePreview();return false;}
            var target=UnitAtPointer(screen);GridPos cell;bool inside=board.ScreenToGrid(screen,out cell);
            if(card.Kind==CardKind.Charge)
            {
                bool legal=inside&&CardAimResolver.LegalChargeLandings(Controller.State,cardId,selected).Contains(cell);
                if(legal){board.ShowHighlights(CardAimResolver.LegalChargeTargets(Controller.State,cardId,selected,cell));Hud.ShowLivePreview("突入落点已选。松手后再点击相邻敌人；此步不改变战场。",true);}
                else{ClearLivePreview();Hud.ShowLivePreview("突入须先松手选择高亮落点，再点击相邻敌人。",false);}
                return legal;
            }
            if(card.Kind==CardKind.RecoverTactics)
            {
                bool legal=target!=null&&target.Id==actor.Id;
                if(!legal)ClearLivePreview();
                return legal;
            }
            var plan=ResolveAim(target,inside?(GridPos?)cell:null,screen);
            if(plan.IsValid)PresentPlan(plan);else{ClearLivePreview();Hud.ShowLivePreview(plan.Reason,false);}
            return plan.IsValid;
        }
        private void CancelCardDrag(string id)
        {
            // Render may disable a selected card after a completed click; only an active drag owns cancellation.
            if(cardDragging&&cardId==id)ResetSelectionState(false);
        }
        private void DropCard(string id,Vector2 screen)
        {
            suppressBoardClickFrame=Time.frameCount;
            if(!cardDragging||cardId!=id||Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player){CancelSelection();return;}
            bool valid=CanDropCardAt(screen);var card=FindCard();cardDragging=false;
            if(!valid||card==null){CancelSelection();return;}
            if(card.Kind==CardKind.Charge)
            {
                GridPos landing;if(!board.ScreenToGrid(screen,out landing)){CancelSelection();return;}
                chargeLanding=landing;cardStage=CardStage.ChargeTarget;ClearLivePreview();Render();ShowTargets();
                Hud.SetHint("落点已选。点击相邻的高亮敌人执行突入；此步未移动或扣费。");return;
            }
            if(card.Kind==CardKind.RecoverTactics){OpenRecoveryPicker();return;}
            var target=UnitAtPointer(screen);GridPos cell;bool inside=board.ScreenToGrid(screen,out cell);
            var plan=ResolveAim(target,inside?(GridPos?)cell:null,screen);if(!plan.IsValid){CancelSelection();return;}
            PresentPlan(plan);CommitPreview();
        }
        private void OpenRecoveryPicker()
        {
            var actor=Controller.State.FindUnit(selected);if(actor==null||FindCard()==null)return;
            string recoveryCard=cardId,executor=selected;var recoveryActor=actor;
            ResetSelectionState(false);Render();
            recovery.Show(GrowthRules.RecoveryCandidates(Controller.State),"回收战术 · 选择一张牌","执行者："+BattleHud.Name(recoveryActor.Role)+"。确认前不消耗卡牌或行动点。",
                id=>{var plan=Controller.Preview(new ActionRequest{Kind=ActionKind.Card,ActorId=executor,CardId=recoveryCard,RecoveryCardId=id});if(plan.IsValid){selected=executor;StartCoroutine(Controller.Confirm(plan));}else{Hud.SetHint(plan.Reason);Render();}},
                ()=>{selected=executor;CancelSelection();},false);
        }
        private void SelectBasic(ActionKind action)
        {
            var actor=Controller.State.FindUnit(selected);
            if(actor==null||!actor.IsAlive){Hud.SetHint("先点击己方棋子或左侧头像选择行动者。");return;}
            if(Controller.IsBusy||Controller.HasPendingRecovery||inCamp||Controller.State.Phase!=BattlePhase.Player)return;
            CancelSelection();mode=action;ShowTargets();
            Hud.SetHint("指向目标查看结果，点击合法目标立即执行。ESC 取消。"+(action==ActionKind.Move?"金色虚线表示移动后的攻击距离。":"浅金格表示距离范围，亮圈表示合法目标。"));
        }
        private void PresentPlan(ActionPlan plan)
        {
            var r=plan.Request;string key=Controller.State.Revision+"/"+r.ActorId+"/"+r.CardId+"/"+r.Kind+"/"+r.TargetId+"/"+r.Destination+"/"+plan.IsValid+"/"+plan.Reason;
            if(previewKey==key)return;previewKey=key;
            // Controller retains the same complete request that is used by the preview and release.
            var actual=plan.IsValid?Controller.Preview(r):plan;
            if(!plan.IsValid)Controller.CancelPreview();
            var actor=Controller.State.FindUnit(r.ActorId);
            BattleState after=null;if(actual.IsValid){after=Controller.State.Clone();BattleResolver.Apply(after,actual);}
            overlay.Show(Controller.State,actual,after,!cardDragging);
            string title=mode==ActionKind.Card&&FindCard()!=null?Definitions.Card(FindCard()).Name:r.Kind==ActionKind.Move?"移动":r.Kind==ActionKind.Heal?"急救":"普通攻击";
            var text=new StringBuilder(title+"\n");
            if(actor!=null)text.AppendLine("执行者："+BattleHud.Name(actor.Role));
            if(!actual.IsValid)text.AppendLine("\n无法执行\n"+actual.Reason);
            else
            {
                if(actual.Path.Count>0)text.AppendLine("移动 "+actual.Path.Count+" 格 → "+BattleHud.Coord(actual.Path[actual.Path.Count-1])+"\n落点周围金格：移动后普攻距离");
                var victim=Controller.State.FindUnit(r.TargetId);
                if(victim!=null)text.AppendLine("目标："+BattleHud.Name(victim.Role));
                foreach(var u in Controller.State.Units)
                {
                    var n=after.FindUnit(u.Id);
                    if(n.Hp!=u.Hp)text.AppendLine("HP "+u.Hp+" → "+Mathf.Max(0,n.Hp)+(n.Hp<=0?"（击败）":""));
                    if(n.Shield!=u.Shield)text.AppendLine("护盾 "+u.Shield+" → "+n.Shield);
                    if(n.MarkExpiresRound!=u.MarkExpiresRound)text.AppendLine(n.MarkExpiresRound>0?"印记至第 "+n.MarkExpiresRound+" 轮结束":"消耗印记");
                    if(n.InspireExpiresRound!=u.InspireExpiresRound||n.InspireBonus!=u.InspireBonus)text.AppendLine(n.InspireExpiresRound>0?"鼓舞：下次伤害 +"+Mathf.Max(1,n.InspireBonus):"消耗鼓舞");
                    if(n.Position!=u.Position&&u.Id!=actor.Id)text.AppendLine("击退至 "+BattleHud.Coord(n.Position));
                }
                if(actual.DamageSummary!=null)text.AppendLine("实际扣血 "+actual.DamageSummary.HpLost+" · 护盾吸收 "+actual.DamageSummary.ShieldAbsorbed+"\n守护减伤 "+actual.DamageSummary.GuardReduction);
                if(mode==ActionKind.Card&&FindCard().Kind==CardKind.Cover)text.AppendLine("护盾取较大值，至下次己方回合开始");
                if(!string.IsNullOrEmpty(after.PendingRecoveryActorId))text.AppendLine("战术回收：执行后可选择一张中立攻击牌回手");
                if(mode==ActionKind.Card&&FindCard()!=null&&GrowthRules.CardCost(Controller.State,actor,FindCard())<Definitions.Card(FindCard()).Cost)text.AppendLine("本次天赋减费已生效；回收后不刷新次数");
                if(r.TargetId!=null&&Controller.State.FindUnit(r.TargetId)!=null&&Controller.State.FindUnit(r.TargetId).IsFacility)text.AppendLine(Controller.State.FindUnit(r.TargetId).Role==UnitRole.ResonanceAnchor?"摧毁锚点：下一回合首领减伤暂停":"摧毁阵柱：绑定守卫失去阵柱护盾");
                text.AppendLine(cardDragging?"\n松开执行 · ESC 取消":cardStage==CardStage.Target&&FindCard()!=null&&FindCard().Kind==CardKind.Charge?"\n先选择突入落点，再点击相邻敌人":"\n点击高亮目标/落点执行 · ESC 取消");
            }
            Hud.ShowLivePreview(text.ToString(),actual.IsValid);
        }
        private void CommitPreview()
        {
            var plan=Controller.CurrentPreview;if(plan==null||!plan.IsValid)return;
            var fresh=ActionPlanner.Plan(Controller.State,plan.Request);if(!fresh.IsValid){ClearLivePreview();return;}
            selected=fresh.Request.ActorId;ResetSelectionState(false);Render();StartCoroutine(Controller.Confirm(fresh));
        }
        private void ClearLivePreview()
        {previewKey=null;Controller.CancelPreview();Hud.HidePreview();overlay.ClearResult();}
        private void ShowTargets()
        {
            if(!mode.HasValue)return;var state=Controller.State;var cells=new List<GridPos>();
            CardKind? kind=null;
            if(mode==ActionKind.Card){var c=FindCard();if(c==null)return;kind=c.Kind;
                if(c.Kind==CardKind.Charge)cells=cardStage==CardStage.ChargeTarget&&chargeLanding.HasValue?CardAimResolver.LegalChargeTargets(state,cardId,selected,chargeLanding.Value):CardAimResolver.LegalChargeLandings(state,cardId,selected);
                else cells=CardAimResolver.LegalTargets(state,cardId,selected);}
            else for(int y=0;y<state.Height;y++)for(int x=0;x<state.Width;x++)
            {var p=new GridPos(x,y);var u=state.UnitAt(p);if(ActionPlanner.Plan(state,new ActionRequest{Kind=mode.Value,ActorId=selected,Destination=p,TargetId=u==null?null:u.Id}).IsValid)cells.Add(p);}
            board.ShowHighlights(cells);
            var targetIds=new List<string>();
            if(mode!=ActionKind.Move&&kind!=CardKind.Advance&&!(kind==CardKind.Charge&&cardStage==CardStage.Target))
                foreach(var unit in state.Units)if(unit.IsAlive&&cells.Contains(unit.Position))targetIds.Add(unit.Id);
            board.ShowExecutors(targetIds);
            overlay.ShowRange(state,state.FindUnit(selected),mode.Value,kind);
        }
        private void CancelSelection(){ResetSelectionState(true);}
        private void ResetSelectionState(bool renderHand)
        {
            cardDragging=false;cardStage=CardStage.None;chargeLanding=null;actionMenuRequested=false;cardId=null;mode=null;exchanging=false;previewKey=null;
            if(Controller!=null)Controller.CancelPreview();if(Hud!=null){Hud.HideActions();Hud.HidePreview();}if(board!=null){board.ClearHighlights();board.ShowExecutors(null);}if(overlay!=null)overlay.Hide();
            if(renderHand&&Controller!=null&&Controller.State!=null)Render();
        }
        private void ShowDeploy(){var cells=new List<GridPos>();for(int y=6;y<8;y++)for(int x=1;x<4;x++)cells.Add(new GridPos(x,y));board.ShowHighlights(cells);}
        private void Render()
        {
            if(inCamp)return;
            var state=Controller.State;EnsureSelected();var actor=state.FindUnit(selected);Hud.SetActingUnit(actor);var intent=Controller.Intent();Hud.Render(state,actor,intent,Controller.IsBusy);
            if(actionMenuRequested&&state.Phase==BattlePhase.Player&&!Controller.IsBusy&&!mode.HasValue)
            {var menuActor=state.FindUnit(selected);if(menuActor!=null)Hud.ShowActions(menuActor,Controller.ViewCamera.WorldToScreenPoint(board.World(menuActor.Position)+Vector3.up*.8f));}
            else Hud.HideActions();
            hand.Render(state,cardId,state.Phase==BattlePhase.Player&&!Controller.IsBusy&&!Controller.HasPendingRecovery,selected);board.Select(selected);board.ShowIntent(intent);
            if(!mode.HasValue)board.ClearHighlights();
            if(state.Phase==BattlePhase.Deployment){ShowDeploy();Hud.SetHint("部署：点击或拖动三名棋子，确定前排和后排位置。");}
            else if(state.Phase==BattlePhase.Player&&!mode.HasValue&&!exchanging)Hud.SetHint(actor==null?"先点击己方棋子或左侧头像选择行动者。":"行动者："+BattleHud.Name(actor.Role)+"。点击卡牌后选择目标，或拖牌到最终目标；点击其他己方棋子切换行动者。ESC 取消。");
            if(!Controller.IsBusy&&(state.Phase==BattlePhase.Victory||state.Phase==BattlePhase.Defeat))ShowResult(state.Phase==BattlePhase.Victory);
            else if(!Controller.IsBusy&&Controller.HasPendingRecovery&&!recovery.Visible)
                recovery.Show(GrowthRules.RecoveryCandidates(state),"战术回收 · 选择一张牌","支援已生效。可收回一张中立攻击牌，或跳过；本次回收不再花费行动点。",
                    id=>{if(!Controller.ResolveRecovery(id))Render();},()=>Controller.ResolveRecovery(null),true);
        }
        private void OnControllerChanged(){if(Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player)ResetSelectionState(false);Render();}
        private void ShowResult(bool victory)
        {
            if(resultRoot!=null)return;
            recovery.Hide();CancelSelectionWithoutRender();
            resultRoot=BattleHud.Panel(root,"Battle result",0,0,1920,1080,new Color(.005f,.018f,.025f,.82f));
            var panel=BattleHud.Panel(resultRoot,"Result frame",425,180,1070,720,BattleHud.Ink,BattleHud.Gold);
            var title=BattleHud.Label(panel,"Result title",60,65,750,90,60,BattleHud.Gold,UiFont);title.text=victory?"遗 迹 收 复":"小 队 败 北";title.alignment=TextAnchor.MiddleCenter;
            bool continuing=victory&&Run.LevelIndex<3;
            if(!continuing)Run.Finished=true;
            var copy=BattleHud.Label(panel,"Result copy",60,177,950,98,27,BattleHud.Paper,UiFont);copy.text=continuing?"第 "+Run.LevelIndex+" 关完成 · "+CampaignRules.LevelName(Run.LevelIndex)+"\n进入营地，选择成长并为下一战整备。":victory?"三关完成，钟声重新回荡在林间。":"本轮止步于第 "+Run.LevelIndex+" 关。调整部署与卡组配合，再发起进攻。";
            var progress=new StringBuilder("本轮成长\n");foreach(var entry in Run.GrowthHistory)progress.AppendLine("· "+entry);
            if(Run.GrowthHistory.Count==0)progress.AppendLine("尚未经过营地");
            progress.AppendLine("\n最近的战场行动");foreach(var entry in highlights)progress.AppendLine("· "+entry);
            var detail=BattleHud.Label(panel,"Run summary",60,286,950,295,21,BattleHud.Paper,UiFont);detail.text=progress.ToString();
            if(continuing)BattleHud.ButtonAt(panel,"进入营地",50,624,300,64,UiFont,EnterCamp,true);
            BattleHud.ButtonAt(panel,"重新挑战",continuing?380:180,624,300,64,UiFont,()=>{requestedSeed=Run.Seed;SceneManager.LoadScene("ForestRuins");},!continuing);
            BattleHud.ButtonAt(panel,"返回标题",continuing?710:560,624,300,64,UiFont,()=>{requestedSeed=null;SceneManager.LoadScene("Title");});
        }
        private void CancelSelectionWithoutRender(){ResetSelectionState(false);}
        private void EnterCamp()
        {
            if(inCamp||Controller.State.Phase!=BattlePhase.Victory||!CampaignRules.BeginCamp(Run))return;
            inCamp=true;ResetSelectionState(false);
            if(resultRoot!=null){resultRoot.gameObject.SetActive(false);Destroy(resultRoot.gameObject);resultRoot=null;}
            Camp.Show(Run,()=>{inCamp=false;selected=null;highlights.Clear();Controller.CreateBattle(Run);});
        }
        private void PlaySound() { if(SoundClips!=null && SoundClips.Length>0 && SoundClips[0]!=null)sound.PlayOneShot(SoundClips[0]); }
        private string Localize(string message)
        {
            foreach(var unit in Controller.State.Units)
                message=message.Replace(unit.Id,BattleHud.Name(unit.Role)+" "+BattleHud.Coord(unit.Position));
            return message;
        }
    }
}
