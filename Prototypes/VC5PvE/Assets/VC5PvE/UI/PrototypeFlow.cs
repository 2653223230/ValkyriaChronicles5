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
        private BoardView board;
        private CardHandView hand;
        private RectTransform root, resultRoot;
        private string selected, cardId, dragUnit;
        private ActionPreviewOverlay overlay;
        private string previewKey;
        private bool cardDragging;
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
            Hud.SelectAction=SelectBasic;Hud.BeginBattle=()=>{CancelSelection();Controller.BeginBattle();};
            Hud.EndTurn=()=>{CancelSelection();StartCoroutine(Controller.EndTurn());};
            Hud.ExchangeMode=()=>{CancelSelection();exchanging=true;Hud.SetHint("选择一张手牌换走：放至牌库底，抽取另一张。ESC 取消。");};
            Hud.CancelAction=CancelSelection;Hud.ConfirmAction=null;
            hand.Selected=SelectCard;hand.Hovered=c=>{if(!mode.HasValue)Hud.ShowCardDetails(c);};hand.Dropped=DropCard;
            hand.DragStarted=BeginCardDrag;hand.DragCancelled=CancelCardDrag;hand.CanDrop=CanDropCardAt;
            hand.HoverExited=()=>{if(!mode.HasValue)Hud.RestoreIntent();};
            Controller.Changed+=OnControllerChanged;Controller.Message+=s=>{Hud.SetHint(Localize(s));PlaySound();};
            Controller.CreateBattle();
        }

        private void BuildTitle()
        {
            var image=BattleHud.Rect(root,"Forest illustration",0,0,1920,1080).gameObject.AddComponent<Image>();
            image.sprite=TitleBackground;image.color=Color.white;
            BattleHud.Panel(root,"Title shade",0,0,620,1080,new Color(.005f,.025f,.04f,.48f));
            var name=BattleHud.Label(root,"Game title",102,228,890,120,70,BattleHud.Gold,UiFont);name.text="森 钟 战 术";
            BattleHud.Label(root,"Subtitle",112,359,740,50,26,BattleHud.Paper,UiFont).text="FOREST BELL TACTICS   /   卡牌 · 战棋";
            BattleHud.Label(root,"Demo scope",112,648,640,52,24,BattleHud.Jade,UiFont).text="三人小队，十七张卡牌，一座待夺回的遗迹。";
            BattleHud.ButtonAt(root,"开始游戏",112,750,350,72,UiFont,()=>SceneManager.LoadScene("ForestRuins"),true);
            BattleHud.ButtonAt(root,"退出游戏",112,846,350,64,UiFont,()=>Application.Quit());
            BattleHud.Label(root,"Version",112,989,960,36,18,BattleHud.Paper,UiFont).text="首关玩法验证版  ·  2026.10.01";
        }

        private void Update()
        {
            if(IsTitle||Controller==null||Controller.State==null)return;
            if(Input.GetKeyDown(KeyCode.Escape)){CancelSelection();return;}
            if(Controller.IsBusy)return;
            var state=Controller.State;
            if(state.Phase!=BattlePhase.Deployment&&state.Phase!=BattlePhase.Player)return;
            bool over=OverUi(Input.mousePosition);
            if(mode.HasValue&&!cardDragging)
            {
                if(!over)UpdateAimAt(Input.mousePosition);
                else ClearLivePreview();
            }
            if(Input.GetMouseButtonDown(0)&&!over&&!cardDragging)
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
            if(Controller.IsBusy)return;var unit=Controller.State.FindUnit(id);
            if(unit==null||!unit.IsAlive||unit.Team!=Team.Player)return;
            CancelSelection();selected=id;actionMenuRequested=Controller.State.Phase==BattlePhase.Player;Render();
        }
        private void EnsureSelected()
        {
            var u=Controller.State.FindUnit(selected);
            if(u==null||!u.IsAlive||u.Team!=Team.Player){u=Controller.State.Units.Find(x=>x.Team==Team.Player&&x.IsAlive);selected=u==null?null:u.Id;}
        }
        private void ClickBattle(UnitState unit,GridPos? cell)
        {
            if(Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player)return;
            if(exchanging){Hud.SetHint("请点选要换走的手牌，或按 ESC 取消。");return;}
            if(!mode.HasValue)
            {
                actionMenuRequested=unit!=null&&unit.Team==Team.Player&&unit.IsAlive;
                if(actionMenuRequested)selected=unit.Id;
                Render();return;
            }
            var point=unit!=null?ProjectCell(unit.Position):cell.HasValue?ProjectCell(cell.Value):Vector2.zero;
            PresentPlan(ResolveAim(unit,cell,point));
            CommitPreview();
        }
        private CardInstance FindCard(){return Controller.State.Hand.Find(c=>c.Id==cardId);}
        private void SelectCard(string id)
        {
            if(Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player)return;
            if(exchanging){if(Controller.Exchange(id))CancelSelection();return;}
            ResetSelectionState(true);BeginCardSelection(id);hand.Render(Controller.State,cardId,true);
        }
        private void BeginCardDrag(string id)
        {
            if(Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player||exchanging)return;
            ResetSelectionState(false);cardDragging=true;BeginCardSelection(id);
        }
        private void BeginCardSelection(string id)
        {
            if(!Controller.State.Hand.Exists(c=>c.Id==id))return;
            EnsureSelected();cardId=id;mode=ActionKind.Card;
            var actor=CardAimResolver.ResolveActor(Controller.State,id,selected);
            if(actor!=null)selected=actor.Id;
            Hud.HideActions();Hud.Render(Controller.State,Controller.State.FindUnit(selected),Controller.Intent(),false);board.Select(selected);
            ShowTargets();Hud.ShowCardDetails(Definitions.Card(FindCard().Kind));
            Hud.SetHint("拖向高亮目标，松手即出牌；点击出牌可点击目标。ESC 取消。执行者："+(actor==null?"未选择":BattleHud.Name(actor.Role)));
        }
        private Vector2 ProjectCell(GridPos p){return Controller.ViewCamera.WorldToScreenPoint(board.World(p));}
        private ActionPlan ResolveAim(UnitState unit,GridPos? cell,Vector2 pointer)
        {
            if(mode==ActionKind.Card)return CardAimResolver.Resolve(Controller.State,cardId,selected,unit,cell,pointer,ProjectCell);
            return ActionPlanner.Plan(Controller.State,new ActionRequest{Kind=mode.Value,ActorId=selected,TargetId=unit==null?null:unit.Id,Destination=cell});
        }
        private bool UpdateAimAt(Vector2 screen)
        {
            if(!mode.HasValue||Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player)return false;
            if(OverUi(screen)){ClearLivePreview();return false;}
            GridPos cell;bool inside=board.ScreenToGrid(screen,out cell);var unit=PickUnit(screen,inside?(GridPos?)cell:null);
            if(!inside&&unit==null){ClearLivePreview();return false;}
            var plan=ResolveAim(unit,inside?(GridPos?)cell:null,screen);PresentPlan(plan);return plan.IsValid;
        }
        private bool CanDropCardAt(Vector2 screen){return !exchanging&&UpdateAimAt(screen);}
        private void CancelCardDrag(string id)
        {
            // Render may disable a selected card after a completed click; only an active drag owns cancellation.
            if(cardDragging&&cardId==id)ResetSelectionState(false);
        }
        private void DropCard(string id,Vector2 screen)
        {
            if(!cardDragging||cardId!=id||Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player){CancelSelection();return;}
            bool valid=UpdateAimAt(screen);cardDragging=false;
            if(valid)CommitPreview();else CancelSelection();
        }
        private void SelectBasic(ActionKind action)
        {
            var actor=Controller.State.FindUnit(selected);
            if(actor==null||!actor.IsAlive||Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player)return;
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
            overlay.Show(Controller.State,actual,after);
            string title=mode==ActionKind.Card&&FindCard()!=null?Definitions.Card(FindCard().Kind).Name:r.Kind==ActionKind.Move?"移动":r.Kind==ActionKind.Heal?"急救":"普通攻击";
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
                    if(n.InspireExpiresRound!=u.InspireExpiresRound)text.AppendLine(n.InspireExpiresRound>0?"鼓舞：下次伤害 +1":"消耗鼓舞");
                    if(n.Position!=u.Position&&u.Id!=actor.Id)text.AppendLine("击退至 "+BattleHud.Coord(n.Position));
                }
                if(actual.DamageSummary!=null)text.AppendLine("实际扣血 "+actual.DamageSummary.HpLost+" · 护盾吸收 "+actual.DamageSummary.ShieldAbsorbed+"\n守护减伤 "+actual.DamageSummary.GuardReduction);
                if(mode==ActionKind.Card&&FindCard().Kind==CardKind.Cover)text.AppendLine("护盾取较大值，至下次己方回合开始");
                text.AppendLine(cardDragging?"\n松开立即执行 · ESC 取消":"\n点击目标立即执行 · ESC 取消");
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
            if(mode==ActionKind.Card){var c=FindCard();if(c==null)return;kind=c.Kind;cells=CardAimResolver.LegalTargets(state,cardId,selected);}
            else for(int y=0;y<state.Height;y++)for(int x=0;x<state.Width;x++)
            {var p=new GridPos(x,y);var u=state.UnitAt(p);if(ActionPlanner.Plan(state,new ActionRequest{Kind=mode.Value,ActorId=selected,Destination=p,TargetId=u==null?null:u.Id}).IsValid)cells.Add(p);}
            board.ShowHighlights(cells);overlay.ShowRange(state,state.FindUnit(selected),mode.Value,kind);
        }
        private void CancelSelection(){ResetSelectionState(true);}
        private void ResetSelectionState(bool renderHand)
        {
            cardDragging=false;actionMenuRequested=false;cardId=null;mode=null;exchanging=false;previewKey=null;
            if(Controller!=null)Controller.CancelPreview();if(Hud!=null){Hud.HideActions();Hud.HidePreview();}if(board!=null)board.ClearHighlights();if(overlay!=null)overlay.Hide();
            if(renderHand&&Controller!=null&&Controller.State!=null)Render();
        }
        private void ShowDeploy(){var cells=new List<GridPos>();for(int y=6;y<8;y++)for(int x=1;x<4;x++)cells.Add(new GridPos(x,y));board.ShowHighlights(cells);}
        private void Render()
        {
            var state=Controller.State;EnsureSelected();var intent=Controller.Intent();Hud.Render(state,state.FindUnit(selected),intent,Controller.IsBusy);
            if(actionMenuRequested&&state.Phase==BattlePhase.Player&&!Controller.IsBusy&&!mode.HasValue)
            {var actor=state.FindUnit(selected);if(actor!=null)Hud.ShowActions(actor,Controller.ViewCamera.WorldToScreenPoint(board.World(actor.Position)+Vector3.up*.8f));}
            else Hud.HideActions();
            hand.Render(state,cardId,state.Phase==BattlePhase.Player&&!Controller.IsBusy);board.Select(selected);board.ShowIntent(intent);
            if(!mode.HasValue)board.ClearHighlights();
            if(state.Phase==BattlePhase.Deployment){ShowDeploy();Hud.SetHint("部署：点击或拖动三名棋子，确定前排和后排位置。");}
            else if(state.Phase==BattlePhase.Player&&!mode.HasValue&&!exchanging)Hud.SetHint("点击棋子/头像选择执行者；拖卡到目标，预览后松手出牌。ESC 取消。");
            if(!Controller.IsBusy&&(state.Phase==BattlePhase.Victory||state.Phase==BattlePhase.Defeat))ShowResult(state.Phase==BattlePhase.Victory);
        }
        private void OnControllerChanged(){if(Controller.IsBusy||Controller.State.Phase!=BattlePhase.Player)ResetSelectionState(false);Render();}
        private void ShowResult(bool victory)
        {
            if(resultRoot!=null)return;
            resultRoot=BattleHud.Panel(root,"Battle result",0,0,1920,1080,new Color(.005f,.018f,.025f,.82f));
            var panel=BattleHud.Panel(resultRoot,"Result frame",525,290,870,500,BattleHud.Ink,BattleHud.Gold);
            var title=BattleHud.Label(panel,"Result title",60,65,750,90,60,BattleHud.Gold,UiFont);title.text=victory?"遗 迹 收 复":"小 队 败 北";title.alignment=TextAnchor.MiddleCenter;
            var copy=BattleHud.Label(panel,"Result copy",80,187,710,100,26,BattleHud.Paper,UiFont);copy.text=victory?"钟声重新回荡在林间。\n这次配合，值得再试一种打法。":"调整部署、利用掩护与印记，再发起进攻。";copy.alignment=TextAnchor.MiddleCenter;
            BattleHud.ButtonAt(panel,"重新挑战",100,345,290,70,UiFont,()=>SceneManager.LoadScene("ForestRuins"),true);
            BattleHud.ButtonAt(panel,"返回标题",480,345,290,70,UiFont,()=>SceneManager.LoadScene("Title"));
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
