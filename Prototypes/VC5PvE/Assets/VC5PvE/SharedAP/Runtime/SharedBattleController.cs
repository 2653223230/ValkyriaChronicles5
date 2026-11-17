using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VC5PvE.SharedAP
{
    public enum SharedInputStage { Idle, Dragging, Target, Landing, ChargeTarget, Retain }
    /// <summary>Shared rules own the battle; the original prototype supplies presentation only.</summary>
    public sealed partial class SharedBattleController : MonoBehaviour
    {
        public SState State { get; private set; }
        public SharedInputStage Stage { get; private set; }
        public readonly HashSet<string> DiscardIds = new HashSet<string>();
        [System.Obsolete("Use DiscardIds; selected cards are discarded, unselected cards are retained.")]
        public HashSet<string> KeepIds => DiscardIds;
        public string SelectedId, Hint, LastSummary, PendingCardId;
        public bool Busy;
        public bool ShowRoleDetails { get; private set; }
        public SPlan Preview;
        public Font UiFont;
        public Sprite[] UnitSprites,ScenerySprites;
        public AudioClip[] SoundClips;
        public GameObject[] TreePrefabs,RockPrefabs,ShrubPrefabs,RuinPrefabs;
        public Texture2D GroundAtlas;
        public Camera ViewCamera;
        public BoardView Board { get; private set; }
        public bool ActionMenuVisible { get; private set; }
        public bool IsDragging { get; private set; }
        public string PendingExecutorId => pending==null?null:pending.ActorId;
        public string PreviewExecutorId => hasHoverExecutorPreview?previewExecutorId:PendingExecutorId;
        public int PreviewApCost => PendingCard==null||PreviewExecutorId==null||!CanExecuteCard(PreviewExecutorId,PendingCardId)?0:SharedRules.Cost(PendingCard.Kind);
        public int PreviewApRemaining => State==null?0:Mathf.Max(0,State.Ap-PreviewApCost);
        public SCard? InspectedCard => PendingCard!=null?(SCard?)PendingCard.Kind:inspected;
        SharedHud hud;
        SharedArtilleryView artillery;
        ActionPreviewOverlay overlay;
        AudioSource audioSource;
        SAction pending,previewAction;
        SPos? hovered;
        SCard? inspected;
        string previewExecutorId,lastHoveredUnitId,lastRangeKey;
        bool hasHoverExecutorPreview;
        readonly List<GridPos> options=new List<GridPos>();
        readonly HashSet<SPos> idleMoveCells=new HashSet<SPos>(),idleAttackCells=new HashSet<SPos>();
        readonly HashSet<string> legalExecutorIds=new HashSet<string>();
        int seed=17,ignoreClickFrame=-1,tutorialStep;
        bool fixedTutorial, tutorialFinished;
        const string SharedBattleScenePath="Assets/VC5PvE/SharedAP/Scenes/SharedAP_Qinglan.unity";
        bool CanAct => State!=null&&State.Phase==SPhase.Player&&!Busy;
        SCardInstance PendingCard => State==null?null:State.Hand.FirstOrDefault(c=>c.Id==PendingCardId);

        void Start()
        {
            // The authored scene owns a serialized camera; reset stale no-domain-reload state
            // and rebuild its presentation when entering Play. Isolated controller fixtures
            // use InitializeForTests and deliberately keep their presentation optional.
            if(gameObject.scene.path==SharedBattleScenePath){InitializeForTests(seed);BuildPresentation();return;}
            if(State==null){InitializeForTests(seed);BuildPresentation();}
        }
        public void InitializeForTests(int value)
        {
            tutorialSkipped=false;tutorialFinished=false;fixedTutorial=false;tutorialStep=0;ResetLessons();ShowRoleDetails=false;seed=value;State=SharedRules.Create(seed,true);SelectedId="vanguard";ResetInput();
            InitializeFocusTutorial(true);
            Hint="点击术师头像，查看她的武器和战技。";
        }
        public void BuildPresentation()
        {
            if(Board==null)Board=GetComponentInChildren<BoardView>(true);
            if(hud==null)hud=GetComponentInChildren<SharedHud>(true);
            if(overlay==null)overlay=GetComponentInChildren<ActionPreviewOverlay>(true);
            if(audioSource==null)audioSource=GetComponent<AudioSource>();
            if(State==null)InitializeForTests(seed);
            if(Board!=null&&hud!=null&&overlay!=null){RefreshBoard();Render();return;}
            if(ViewCamera==null)ViewCamera=Camera.main;
            if(ViewCamera==null)
            {
                var go=new GameObject("Shared camera");go.transform.SetParent(transform);ViewCamera=go.AddComponent<Camera>();
                ViewCamera.tag="MainCamera";ViewCamera.orthographic=true;ViewCamera.orthographicSize=6.35f;
                var target=new Vector3(.1f,-.55f,-.05f);go.transform.position=target+new Vector3(10,11,-10);go.transform.LookAt(target);
            }
            if(Board==null)
            {
                var b=new GameObject("Shared battle - original presentation");b.transform.SetParent(transform);Board=b.AddComponent<BoardView>();
                Board.UnitSprites=UnitSprites;Board.TreePrefabs=TreePrefabs;Board.RockPrefabs=RockPrefabs;Board.ShrubPrefabs=ShrubPrefabs;
                Board.RuinPrefabs=RuinPrefabs;Board.GroundAtlas=GroundAtlas;Board.ScenerySprites=ScenerySprites;Board.ShowPersonalAp=false;
                Board.Initialize(SharedVisualAdapter.ToBattleState(State),ViewCamera);
            }
            if(EventSystem.current==null)
            {var events=new GameObject("Shared input",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(transform);}
            if(hud==null)
            {var canvas=new GameObject("Shared HUD",typeof(RectTransform));canvas.transform.SetParent(transform);hud=canvas.AddComponent<SharedHud>();hud.Build(this);}
            if(overlay==null)
            {overlay=hud.gameObject.AddComponent<ActionPreviewOverlay>();overlay.Initialize((RectTransform)hud.transform,Board,ViewCamera,UiFont!=null?UiFont:Resources.GetBuiltinResource<Font>("Arial.ttf"));}
            if(audioSource==null){audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;}
            RefreshBoard();Render();
        }
        void Update()
        {
            if(FocusTutorialActive)CheckFocusTutorialState();
            if(FocusTutorialActive&&(FocusExitOpen||FocusRestartConfirm||FocusRecoveryReason!=null))return;
            if(!CanAct)return;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1)){Cancel();return;}
            if(Board==null||IsDragging||Stage==SharedInputStage.Retain)return;
            bool blocked;DropRecipient(Input.mousePosition,out blocked);if(blocked)return;
            SPos cell;if(!TryCell(Input.mousePosition,out cell))return;
            if(FocusTutorialActive&&!FocusAllowsCell(cell))
            {
                if(hovered.HasValue&&pending!=null){hovered=null;Preview=null;previewAction=null;ShowPreview();Render();}
                return;
            }
            if(!hovered.HasValue||hovered.Value!=cell)
            {hovered=cell;if(pending!=null){Preview=PreviewAt(cell);if(PendingCard?.Kind==SCard.Charge&&Stage==SharedInputStage.Landing)ShowChargeLandingRange(cell);ShowPreview();Render();}}
            if(Input.GetMouseButtonDown(0)&&Time.frameCount!=ignoreClickFrame)ClickCell(cell);
        }
        public void ClickScreen(Vector2 point)
        {bool blocked;DropRecipient(point,out blocked);if(blocked)return;SPos cell;if(TryCell(point,out cell))ClickCell(cell);}
        bool TryCell(Vector2 point,out SPos cell)
        {
            cell=default(SPos);if(Board==null)return false;
            GridPos ground;
            if(Stage==SharedInputStage.Idle&&Board.ScreenToGrid(point,out ground))
            {
                var floor=new SPos(ground.X,ground.Y);var occupant=State.UnitAt(floor);
                if(idleMoveCells.Contains(floor)||occupant!=null&&occupant.Player||idleAttackCells.Contains(floor))
                {cell=floor;return true;}
            }
            // Landing is always a ground-cell choice. A tall sprite must not steal its neighbour's landing.
            if(Stage!=SharedInputStage.Landing)
            {
                var hit=PickUnit(point);if(hit!=null){cell=State.Find(hit).Position;return true;}
            }
            GridPos p;if(!Board.ScreenToGrid(point,out p))return false;cell=new SPos(p.X,p.Y);return true;
        }
        string PickUnit(Vector2 point)
        {
            if(Board==null)return null;
            // Use one candidate set for hover and click. Legal targets win an overlap, then the
            // closest ground anchor breaks ties between portraits covering the same screen point.
            var views=Board.GetComponentsInChildren<UnitView>();
            var candidates=State.Units.Where(u=>u.Alive).Select(u=>new
            {
                Unit=u,
                Distance=Vector2.Distance(point,ViewCamera.WorldToScreenPoint(Board.World(Grid(u.Position)))),
                OnPortrait=views.Any(v=>v.UnitId==u.Id&&v.ContainsScreenPoint(point))
            }).Where(x=>x.OnPortrait||x.Distance<20f).ToList();
            bool legalEnemyAtPointer=Stage==SharedInputStage.Idle&&candidates.Any(x=>!x.Unit.Player&&idleAttackCells.Contains(x.Unit.Position));
            return candidates.OrderByDescending(x=>IsPreferredHit(x.Unit)&&(!legalEnemyAtPointer||!x.Unit.Player))
                .ThenBy(x=>x.Distance).Select(x=>x.Unit.Id).FirstOrDefault();
        }
        bool IsPreferredHit(SUnit unit)
        {
            if(unit==null||!unit.Alive)return false;
            if(IsDragging&&PendingCard!=null)return unit.Player&&legalExecutorIds.Contains(unit.Id);
            if(pending!=null&&(Stage==SharedInputStage.Target||Stage==SharedInputStage.ChargeTarget))
                return options.Contains(Grid(unit.Position));
            if(Stage==SharedInputStage.Idle)
            {
                return unit.Player||idleAttackCells.Contains(unit.Position);
            }
            return false;
        }
        public void SelectCard(string id){if(FocusRequiresCardDrag){RejectFocusInput(id);return;}if(!FocusAllowsCard(id)){RejectFocusInput(id);return;}SelectCardInternal(id,false);}
        public bool CanExecuteCard(string unitId,string cardId)
        {
            if(State==null||State.Phase!=SPhase.Player||Busy)return false;
            var unit=State.Find(unitId);var card=State.Hand.FirstOrDefault(c=>c.Id==cardId);
            if(unit==null||!unit.Alive||!unit.Player||card==null||State.Ap<SharedRules.Cost(card.Kind))return false;
            if(card.Kind==SCard.Heal||card.Kind==SCard.Shield||card.Kind==SCard.Skill&&unit.Role==SRole.Archer)
                return SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unitId,CardId=cardId}).Valid;
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)
            {
                var cell=new SPos(x,y);
                if(card.Kind==SCard.Move)
                {if(SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unitId,CardId=cardId,Destination=cell}).Valid)return true;continue;}
                if(card.Kind==SCard.Skill&&unit.Role==SRole.Mage)
                {var enemy=State.UnitAt(cell);if(enemy!=null&&!enemy.Player&&SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unitId,CardId=cardId,TargetId=enemy.Id}).Valid)return true;continue;}
                if(card.Kind==SCard.Skill&&unit.Role==SRole.Vanguard)
                {if(SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unitId,CardId=cardId,TargetId=State.UnitAt(cell)?.Id}).Valid)return true;continue;}
                var target=State.UnitAt(cell);if(target==null||target.Player)continue;
                if(card.Kind==SCard.Charge)
                {for(int dy=0;dy<7;dy++)for(int dx=0;dx<7;dx++)if(SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unitId,CardId=cardId,TargetId=target.Id,Destination=new SPos(dx,dy)}).Valid)return true;}
                else if(SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unitId,CardId=cardId,TargetId=target.Id}).Valid)return true;
            }
            return false;
        }
        void ShowUnitActionOptions(SUnit unit)
        {
            options.Clear();idleMoveCells.Clear();idleAttackCells.Clear();var moves=new HashSet<SPos>();var attacks=new HashSet<SPos>();
            foreach(var pos in SharedRules.Reachable(State,unit.Id,1))if(SharedRules.Plan(State,new SAction{Kind=SActionKind.ShortStep,ActorId=unit.Id,Destination=pos}).Valid)moves.Add(pos);
            foreach(var enemy in State.Units.Where(u=>u.Alive&&!u.Player))if(SharedRules.Plan(State,new SAction{Kind=SActionKind.BasicAttack,ActorId=unit.Id,TargetId=enemy.Id}).Valid)attacks.Add(enemy.Position);
            if(FocusTutorialActive)
            {
                if(FocusStep==FocusTutorialStep.ShortStep)moves.RemoveWhere(p=>p!=new SPos(4,5));else moves.Clear();
                if(FocusStep==FocusTutorialStep.BasicAttack)attacks.RemoveWhere(p=>State.UnitAt(p)?.Id!="patrol");else attacks.Clear();
            }
            idleMoveCells.UnionWith(moves);idleAttackCells.UnionWith(attacks);
            options.AddRange(moves.Select(Grid));options.AddRange(attacks.Select(Grid));
            if(Board!=null)Board.ShowSharedBasicActions(moves.Select(Grid),attacks.Select(Grid));
        }
        void SelectCardInternal(string id,bool keepDragVisual)
        {
            if(!FocusAllowsCard(id)){RejectFocusInput(id);return;}
            if(!CanAct||Stage==SharedInputStage.Retain)return;
            var card=State.Hand.FirstOrDefault(c=>c.Id==id);if(card==null)return;
            if(fixedTutorial&&tutorialStep==4)tutorialStep=3;
            if(fixedTutorial&&tutorialStep==6)tutorialStep=5;
            ResetInput(keepDragVisual);IsDragging=keepDragVisual;PendingCardId=id;
            previewExecutorId=null;lastHoveredUnitId=null;lastRangeKey=null;hasHoverExecutorPreview=false;ShowLegalExecutors(card.Id);
            Hint=State.Ap<SharedRules.Cost(card.Kind)?"全队 AP 不足；仍可短步或结束回合。":"已选「"+SharedRules.CardName(card.Kind)+"」；选择可执行队员，或拖到队员预览范围。";Render();
        }
        void ShowLegalExecutors(string cardId)
        {
            legalExecutorIds.Clear();if(Board==null)return;
            foreach(var unit in State.Units.Where(u=>u.Player&&u.Alive&&CanExecuteCard(u.Id,cardId)&&(!FocusTutorialActive||FocusAllowsUnit(u.Id))))legalExecutorIds.Add(unit.Id);
            Board.ShowExecutors(legalExecutorIds);
        }
        void PrepareCard(SUnit u,SCardInstance card)
        {
            SelectedId=u.Id;pending=new SAction{Kind=SActionKind.Card,ActorId=u.Id,CardId=card.Id};ActionMenuVisible=false;previewExecutorId=null;lastRangeKey=null;lastHoveredUnitId=null;hasHoverExecutorPreview=false;Board?.ShowExecutors(null);
            if(card.Kind==SCard.Move||card.Kind==SCard.Charge)
            {Stage=SharedInputStage.Landing;Hint=card.Kind==SCard.Charge?"突击：先选高亮落点，再选敌人；完成前不扣费。":"机动：选择 1–2 格内高亮空格。";}
            else
            {Stage=SharedInputStage.Target;Hint=card.Kind==SCard.Heal||card.Kind==SCard.Shield||(card.Kind==SCard.Skill&&u.Role==SRole.Archer)?"松手生效。":"选择射程内高亮敌人，悬停查看伤害与范围。";}
            ShowOptions();
            if(FocusTutorialActive)FilterFocusActionTargets();

            if(options.Count==0&&card.Kind==SCard.Charge)Hint="当前没有可完成突击的落点。先用免费短步或机动靠近，再选突击。";
        }
        void ShowCardExecutorRanges(SUnit unit,SCardInstance card)
        {
            if(Board==null||unit==null||card==null)return;
            string key=card.Id+"/"+unit.Id+"/base";if(lastRangeKey==key)return;lastRangeKey=key;
            var moves=new HashSet<SPos>();var attacks=new HashSet<SPos>();
            if(card.Kind==SCard.Move)
            {foreach(var pos in SharedRules.Reachable(State,unit.Id,2))if(SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unit.Id,CardId=card.Id,Destination=pos}).Valid)moves.Add(pos);}
            else if(card.Kind==SCard.Charge)
            {
                foreach(var pos in SharedRules.Reachable(State,unit.Id,2))
                    if(State.Units.Any(enemy=>enemy.Alive&&!enemy.Player&&SharedRules.Plan(State,
                        new SAction{Kind=SActionKind.Card,ActorId=unit.Id,CardId=card.Id,Destination=pos,TargetId=enemy.Id}).Valid))moves.Add(pos);
            }
            else if(card.Kind==SCard.Attack||card.Kind==SCard.Heavy||card.Kind==SCard.Skill&&unit.Role==SRole.Vanguard)
            {
                attacks.UnionWith(WeaponAimCells(State,unit));
            }
            else if(card.Kind==SCard.Skill&&unit.Role==SRole.Mage)
            {foreach(var enemy in State.Units.Where(x=>x.Alive&&!x.Player))if(SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=unit.Id,CardId=card.Id,TargetId=enemy.Id}).Valid)attacks.Add(enemy.Position);}
            Board.ShowSharedActionRanges(moves.Select(Grid),attacks.Select(Grid));
        }
        IEnumerable<SPos> WeaponAimCells(SState state,SUnit unit)
        {
            int range=SharedRules.WeaponRange(unit);
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)
            {
                var pos=new SPos(x,y);if(pos==unit.Position||unit.Position.Distance(pos)>range||state.Obstacles.Contains(pos))continue;
                var occupant=state.UnitAt(pos);if(occupant!=null&&occupant.Player==unit.Player)continue;
                if(SharedRules.HasSight(state,unit.Position,pos))yield return pos;
            }
        }
        void ShowChargeLandingRange(SPos destination,SPos? target=null)
        {
            if(Board==null||pending==null||PendingCard?.Kind!=SCard.Charge)return;
            var plan=new SPlan();
            foreach(var enemy in State.Units.Where(x=>x.Alive&&!x.Player))
            {var candidate=SharedRules.Plan(State,new SAction{Kind=SActionKind.Card,ActorId=pending.ActorId,CardId=PendingCardId,Destination=destination,TargetId=enemy.Id});if(candidate.Valid){plan=candidate;break;}}
            if(!plan.Valid){lastRangeKey=null;Board.ShowHighlights(options);return;}
            var moved=State.Clone();var actor=moved.Find(pending.ActorId);if(actor==null)return;
            actor.Position=destination;actor.Braced=false;
            string key=PendingCardId+"/"+actor.Id+"/landing/"+destination.X+"/"+destination.Y;if(lastRangeKey==key)return;lastRangeKey=key;
            Board.ShowSharedActionRanges(null,null,WeaponAimCells(moved,actor).Select(Grid),target.HasValue?Grid(target.Value):(GridPos?)null);
        }
        public void BeginCardDrag(string id)
        {if(!FocusAllowsCard(id)){RejectFocusInput(id);return;}SelectCardInternal(id,true);if(PendingCard!=null&&State.Ap>=SharedRules.Cost(PendingCard.Kind)){IsDragging=true;ActionMenuVisible=false;}}
        public bool DropCardOn(string id)
        {
            if(!CanAct||!IsDragging)return false;
            if(!FocusAllowsUnit(id)){RejectFocusInput(id);return false;}
            var u=State.Find(id);var card=PendingCard;IsDragging=false;ignoreClickFrame=Time.frameCount;
            if(u==null||!u.Alive||!u.Player||card==null){Cancel();Hint="把卡交给可执行的己方队员。";Render();return false;}
            if(!CanExecuteCard(u.Id,card.Id)){Cancel();Hint=u.Name+"当前无法合法执行这张牌。";Render();return false;}
            previewExecutorId=null;PrepareCard(u,card);
            AdvanceTutorialForCardAssigned(card);
            OnFocusCardAssigned(card,u);
            if(FocusTutorialActive)FilterFocusActionTargets();
            if(card.Kind==SCard.Heal||card.Kind==SCard.Shield||(card.Kind==SCard.Skill&&u.Role==SRole.Archer))return Commit(pending);
            Render();return true;
        }
        public void ChooseUnit(string id)
        {
            if(!FocusAllowsUnit(id)){RejectFocusInput(id);return;}
            if(!CanAct||Stage==SharedInputStage.Retain)return;
            var u=State.Find(id);if(u==null||!u.Alive||!u.Player)return;
            if(PendingCard!=null)
            {
                if(!CanExecuteCard(id,PendingCardId)){previewExecutorId=null;Hint=u.Name+"不能合法执行这张牌。";Render();return;}
                if(pending!=null)return;
                var card=PendingCard;
                PrepareCard(u,card);
                AdvanceTutorialForCardAssigned(card);
                if(card.Kind==SCard.Heal||card.Kind==SCard.Shield||(card.Kind==SCard.Skill&&u.Role==SRole.Archer)){Commit(pending);return;}
                Render();return;
            }
            ResetInput();ShowRoleDetails=true;SelectedId=id;ActionMenuVisible=false;pending=null;Hint=u.Name+"："+SharedRoleInfo.Weapon(u)+"。点击高亮空格短步，或点击高亮敌人普攻。";OnFocusUnitSelected(u);ShowUnitActionOptions(u);
            if(fixedTutorial&&tutorialStep==1&&u.Id=="mage")tutorialStep=2;
            else if(fixedTutorial&&tutorialStep==7&&u.Id=="archer")tutorialStep=8;
            else if(fixedTutorial&&((tutorialStep==2&&u.Id!="mage")||(tutorialStep==8&&u.Id!="archer")))fixedTutorial=false;
            Render();
        }
        public void ChooseBasic(SActionKind kind)
        {
            if(!FocusAllowsBasic(kind)){RejectFocusInput(kind.ToString());return;}
            if(!CanAct||Stage==SharedInputStage.Retain||(kind!=SActionKind.ShortStep&&kind!=SActionKind.BasicAttack))return;
            var u=State.Find(SelectedId);if(u==null||!u.Alive)return;
            ResetInput();pending=new SAction{Kind=kind,ActorId=u.Id};Stage=kind==SActionKind.ShortStep?SharedInputStage.Landing:SharedInputStage.Target;
            Hint=kind==SActionKind.ShortStep?"短步：选择四邻空格，免费，每人每轮一次。":"普攻：选择武器范围内敌人，2 AP，每人每轮一次。";ShowOptions();Render();
        }
        SAction ActionAt(SPos cell)
        {
            if(pending==null)return null;
            var a=new SAction{Kind=pending.Kind,ActorId=pending.ActorId,CardId=pending.CardId,TargetId=pending.TargetId,Destination=pending.Destination};
            if(Stage==SharedInputStage.Landing)a.Destination=cell;
            else a.TargetId=State.UnitAt(cell)?.Id;
            return a;
        }
        SPlan PreviewAt(SPos cell)
        {
            if(pending==null)return null;
            var a=ActionAt(cell);previewAction=a;
            if(Stage==SharedInputStage.Target&&PendingCard!=null&&(PendingCard.Kind==SCard.Heal||PendingCard.Kind==SCard.Shield||(PendingCard.Kind==SCard.Skill&&State.Find(SelectedId).Role==SRole.Archer)))
            {if(a.TargetId!=SelectedId)return new SPlan{Reason="点击当前队员确认，或把卡牌拖给其他队员"};a.TargetId=null;}
            if(Stage==SharedInputStage.Landing&&PendingCard?.Kind==SCard.Charge)
            {
                foreach(var enemy in State.Units.Where(u=>u.Alive&&!u.Player))
                {a.TargetId=enemy.Id;var p=SharedRules.Plan(State,a);if(p.Valid){
                    p.After=State.Clone();p.After.Find(a.ActorId).Position=cell;p.After.Find(a.ActorId).Braced=false;
                    p.After.Ap-=SharedRules.Cost(SCard.Charge);p.Affected.Clear();p.Steps.Clear();a.TargetId=null;
                    p.Summary="突击落点 "+SharedRules.Coord(cell)+"；下一步选择敌人，确认前不扣费。";return p;}}
                return new SPlan{Reason="该落点不能完成合法突击"};
            }
            return SharedRules.Plan(State,a);
        }
        void ShowOptions()
        {
            options.Clear();if(pending==null)return;
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)
            {var cell=new SPos(x,y);var plan=PreviewAt(cell);if(plan!=null&&plan.Valid)options.Add(Grid(cell));}
            Preview=null;previewAction=null;hovered=null;if(overlay!=null)overlay.Hide();if(Board!=null)Board.ShowHighlights(options);
        }
        void ShowPreview()
        {
            if(overlay==null)return;
            if(Preview!=null&&Preview.Valid&&Preview.After!=null&&previewAction!=null)
                overlay.Show(SharedVisualAdapter.ToBattleState(State),SharedVisualAdapter.ToPlan(State,Preview,previewAction),SharedVisualAdapter.ToBattleState(Preview.After),!IsDragging,PendingCard?.Kind==SCard.Skill&&State.Find(previewAction.ActorId)?.Role==SRole.Mage);
            else overlay.Hide();
        }
        public void ClickCell(SPos cell)
        {
            if(!FocusAllowsCell(cell)){RejectFocusInput(SharedRules.Coord(cell));return;}
            if(!CanAct||Stage==SharedInputStage.Retain||IsDragging)return;
            if(Stage==SharedInputStage.Idle&&PendingCard!=null)
            {
                var receiver=State.UnitAt(cell);
                if(receiver!=null&&receiver.Player)ChooseUnit(receiver.Id);
                return;
            }
            if(Stage==SharedInputStage.Idle){var u=State.UnitAt(cell);if(u!=null&&u.Player){ChooseUnit(u.Id);return;}if(!ShowRoleDetails)return;var actor=State.Find(SelectedId);if(actor!=null&&actor.Alive&&actor.Player){var target=State.UnitAt(cell);var basic=target==null?new SAction{Kind=SActionKind.ShortStep,ActorId=actor.Id,Destination=cell}:new SAction{Kind=SActionKind.BasicAttack,ActorId=actor.Id,TargetId=target.Id};var direct=SharedRules.Plan(State,basic);if(direct.Valid){Commit(basic);return;}Preview=direct;Hint=direct.Reason;Render();}return;}
            var preview=PreviewAt(cell);
            if(preview==null||!preview.Valid){Preview=preview;Hint=preview?.Reason??"请先选择行动";ShowPreview();Render();return;}
            if(Stage==SharedInputStage.Landing&&PendingCard?.Kind==SCard.Charge)
            {pending.Destination=cell;Stage=SharedInputStage.ChargeTarget;Hint="突击落点："+SharedRules.Coord(cell)+"。现在选择敌人；取消仍不扣费。";ShowOptions();Render();return;}
            Commit(previewAction);
        }
        bool Commit(SAction action)
        {
            if(!FocusAllowsAction(action)){RejectFocusInput(FocusActionKey(action));return false;}
            SCard? committedCardKind=null;
            if(action.Kind==SActionKind.Card){var usedCard=State.Hand.FirstOrDefault(c=>c.Id==action.CardId);if(usedCard!=null)committedCardKind=usedCard.Kind;}
            var before=State.Clone();var plan=SharedRules.Apply(State,action);
            if(!plan.Valid){Preview=plan;Hint=plan.Reason;Render();return false;}
            OnFocusActionCommitted(action,committedCardKind,plan);
            if(action.Kind==SActionKind.Card)tutorialCardPlayed=true;
            if(action.Kind==SActionKind.ShortStep)tutorialStepUsed=true;
            if(action.Kind==SActionKind.BasicAttack)tutorialAttackUsed=true;
            AdvanceTutorialForAction(action,committedCardKind);
            LastSummary=plan.Summary;DiscardIds.RemoveWhere(id=>!State.Hand.Any(c=>c.Id==id));ResetInput();
            if(Board!=null)StartCoroutine(PlayPlayer(before,plan,action));else{FinishAction();OnFocusActionAnimationCompleted(action,plan);Render();}return true;
        }
        IEnumerator PlayPlayer(SState before,SPlan plan,SAction action)
        {
            Busy=true;artillery?.SetWarning(State,false);Render();yield return Animate(before,plan,action);Busy=false;FinishAction();OnFocusActionAnimationCompleted(action,plan);RefreshBoard();Render();
        }
        IEnumerator Animate(SState before,SPlan plan,SAction action,List<SPos> firedCells=null)
        {
            Board.SetAnimationState(SharedVisualAdapter.ToBattleState(plan.After));
            if(action.ActorId=="cannon")
            {Board.ShowCellOutlines(before.Telegraph.Select(Grid),new Color(1f,.85f,.2f,1f));if(artillery!=null)yield return artillery.PlaySalvo(before,firedCells);}
            if(audioSource!=null&&SoundClips!=null&&SoundClips.Length>0&&SoundClips[0]!=null)audioSource.PlayOneShot(SoundClips[0],.35f);
            yield return Board.Animate(SharedVisualAdapter.ToPlan(before,plan,action),SharedVisualAdapter.ToResult(before,plan,action));
            Board.Refresh(SharedVisualAdapter.ToBattleState(plan.After));
            if(action.ActorId=="cannon")Board.ShowCellOutlines(plan.After.Telegraph.Select(Grid),new Color(1f,.25f,.12f,.9f));
        }
        bool tutorialSkipped, tutorialCardPlayed, tutorialStepUsed, tutorialAttackUsed, tutorialEndedTurn;
        void ResetLessons()
        {
            tutorialCardPlayed=tutorialStepUsed=tutorialAttackUsed=tutorialEndedTurn=false;
        }
        void AdvanceTutorialForCardAssigned(SCardInstance card)
        {
            if(!fixedTutorial||card==null)return;
            if((tutorialStep==3||tutorialStep==5)&&PendingExecutorId!="mage"){fixedTutorial=false;return;}
            if(tutorialStep==3&&card.Kind==SCard.Move)tutorialStep=4;
            else if(tutorialStep==5&&card.Kind==SCard.Skill)tutorialStep=6;
        }
        void AdvanceTutorialForAction(SAction action,SCard? cardKind)
        {
            if(!fixedTutorial||tutorialSkipped||action==null)return;
            // Keep the numbered route only while the board still matches its next illustration.
            bool onRoute = tutorialStep==2 && action.Kind==SActionKind.ShortStep &&
                    action.ActorId=="mage" && action.Destination==new SPos(4,5)
                || tutorialStep==4 && cardKind==SCard.Move && action.ActorId=="mage" && action.Destination==new SPos(4,3)
                || tutorialStep==6 && cardKind==SCard.Skill && action.ActorId=="mage" && action.TargetId=="crossbow"
                || tutorialStep==8 && action.Kind==SActionKind.BasicAttack && action.ActorId=="archer" && action.TargetId=="patrol";
            if(onRoute) tutorialStep++;
            else if(tutorialStep!=9) fixedTutorial=false;
        }

        string FreeLesson()
        {
            if(!tutorialCardPlayed)
            {
                foreach(var c in State.Hand)
                {
                    var receiver=State.Units.FirstOrDefault(u=>u.Alive&&u.Player&&CanExecuteCard(u.Id,c.Id));
                    if(receiver!=null)return "把「"+SharedRules.CardName(c.Kind)+"」拖给"+receiver.Name+"，先看效果。";
                }
            }
            if(!tutorialStepUsed)
            {
                if(idleMoveCells.Count>0)return "点一个足迹格免费短步；每人每轮一次。";
                var mover=State.Units.FirstOrDefault(u=>u.Player&&u.Alive&&!u.StepUsed&&SharedRules.Reachable(State,u.Id,1).Count>0);
                if(mover!=null)return "点"+mover.Name+"的头像，再点足迹免费移动。";
            }
            if(!tutorialAttackUsed&&State.Ap>=2)
            {
                if(idleAttackCells.Count>0)return "点带剑标的敌人普攻。消耗 2 AP，不用出牌。";
                var attacker=State.Units.FirstOrDefault(u=>u.Player&&u.Alive&&!u.AttackUsed&&State.Units.Any(t=>!t.Player&&t.Alive&&SharedRules.Plan(State,new SAction{Kind=SActionKind.BasicAttack,ActorId=u.Id,TargetId=t.Id}).Valid));
                if(attacker!=null)return "点"+attacker.Name+"，查看能普攻的敌人。";
            }
            return "准备好后点「结束回合」。想留下的牌不用点。";
        }


        public string VictoryReason => State.Find("cannon")?.Alive==false?"炮座已毁":"三名敌兵已全部击败";

        void FinishAction(){Hint=State.Phase==SPhase.Won?VictoryReason+"！":State.Ap==0?"AP 用完了，仍可使用未用的免费短步，再结束回合。":"行动完成。继续选择队员、用牌或基础行动。";}
        void ResetInput(bool keepDragVisual=false)
        {
            Stage=SharedInputStage.Idle;PendingCardId=null;pending=null;IsDragging=false;ActionMenuVisible=false;hovered=null;Preview=null;inspected=null;previewAction=null;previewExecutorId=null;lastHoveredUnitId=null;lastRangeKey=null;hasHoverExecutorPreview=false;options.Clear();idleMoveCells.Clear();idleAttackCells.Clear();legalExecutorIds.Clear();
            if(overlay!=null)overlay.Hide();if(Board!=null){Board.ClearHighlights();Board.ShowExecutors(null);Board.PreviewAp(null,0);}if(hud!=null&&!keepDragVisual)hud.ClearDragVisuals();
        }
        public void Cancel()
        {
            if(Busy||!isActiveAndEnabled||FocusTutorialActive&&(FocusExitOpen||FocusRestartConfirm||FocusRecoveryReason!=null))return;
            if(FocusTutorialActive)
            {
                if(FocusStep==FocusTutorialStep.ReadInfo)return;
                if(FocusStep==FocusTutorialStep.ShortStep||FocusStep==FocusTutorialStep.BasicAttack)
                {var actor=State==null?null:State.Find(FocusStep==FocusTutorialStep.ShortStep?"mage":"archer");if(actor!=null&&actor.Alive){ShowRoleDetails=true;SelectedId=actor.Id;ResetInput();ShowUnitActionOptions(actor);Render();}return;}
                if(FocusStep==FocusTutorialStep.MoveTarget)FocusStep=FocusTutorialStep.DragMove;
                else if(FocusStep==FocusTutorialStep.SkillTarget)FocusStep=FocusTutorialStep.DragSkill;
                if(Stage==SharedInputStage.Retain){DiscardIds.Clear();FocusStep=FocusTutorialStep.EndTurn;}
                ResetInput();RefreshFocusCopy();Hint="已取消，未消耗 AP 或手牌。";Render();return;
            }
            if(fixedTutorial&&((tutorialStep==4&&PendingCard?.Kind==SCard.Move)||(tutorialStep==6&&PendingCard?.Kind==SCard.Skill)))tutorialStep--;if(Stage==SharedInputStage.Retain)DiscardIds.Clear();ResetInput();Hint="已取消，未消耗 AP 或手牌。";Render();
        }
        public void OpenRetention()
        {
            if(!FocusAllowsEndTurn){RejectFocusInput("EndTurn");return;}
            if(!CanAct)return;
            if(Stage==SharedInputStage.Retain){ConfirmEndTurn();return;}
            ResetInput();DiscardIds.Clear();Stage=SharedInputStage.Retain;
            OnFocusRetentionOpened();
            Hint="主动选择本回合弃掉的牌；默认零弃牌，其余牌保留并带入下轮。确认后执行敌方行动。";Render();
        }
        public void ToggleKeep(string id)
        {
            ToggleDiscard(id);
        }
        public void ToggleDiscard(string id)
        {if(!FocusAllowsCard(id)){RejectFocusInput(id);return;}if(Stage!=SharedInputStage.Retain||!CanAct||!State.Hand.Any(c=>c.Id==id))return;if(!DiscardIds.Add(id))DiscardIds.Remove(id);RefreshFocusCopy();Render();}
        public void BeginEndTurn()
        {
            if(!FocusAllowsEndTurn){RejectFocusInput("EndTurn");return;}
            if(!CanAct)return;if(Stage!=SharedInputStage.Retain){OpenRetention();return;}ConfirmEndTurn();
        }
        public void ConfirmEndTurn()
        {
            if(!FocusAllowsConfirmEndTurn){RejectFocusInput("ConfirmEndTurn");return;}
            if(!CanAct||Stage!=SharedInputStage.Retain)return;var plan=SharedRules.EndTurn(State.Clone(),DiscardIds.ToArray());
            if(!plan.Valid){Hint=plan.Reason;Render();return;}tutorialEndedTurn=true;focusRetainedCount=State.Hand.Count-DiscardIds.Count;ResetInput();DiscardIds.Clear();
            OnFocusEnemyStarted(plan);
            if(Board==null){State=plan.After;FinishRound(plan);return;}StartCoroutine(PlayEnemy(plan));
        }
        IEnumerator PlayEnemy(SPlan plan)
        {
            Busy=true;artillery?.SetWarning(State,false);Board.ShowIntent(null);State.Phase=SPhase.Enemy;Hint="敌方回合 · 按预告依次行动";Render();yield return new WaitForSeconds(.35f);
            foreach(var step in plan.Steps)
            {
                OnFocusEnemyActionStarted(step.Action);
                State=step.Before.Clone();State.Phase=SPhase.Enemy;LastSummary=step.Summary;Hint=step.Summary;Render();
                var animation=new SPlan{Valid=true,After=step.After,Summary=step.Summary,Path=step.Path};
                yield return Animate(step.Before,animation,step.Action,step.FiredCells);
                State=step.After.Clone();State.Phase=SPhase.Enemy;Render();yield return new WaitForSeconds(.18f);
            }
            State=plan.After;Busy=false;FinishRound(plan);
        }
        void FinishRound(SPlan plan)
        {
            LastSummary=plan.Summary;
            if(fixedTutorial&&tutorialStep==9)tutorialFinished=true;else fixedTutorial=false;
            OnFocusRoundCompleted(plan);
            if(State.Find(SelectedId)==null||!State.Find(SelectedId).Alive)SelectedId=State.Units.FirstOrDefault(u=>u.Player&&u.Alive)?.Id;
            Hint=State.Phase==SPhase.Player?"第 "+State.Round+" 回合：全队 AP 刷新为 6，补牌完成；注意新的炮击红框。":"战斗结束。";RefreshBoard();Render();
        }
        public void Restart(bool demo)
        {
            StopAllCoroutines();ResetLessons();tutorialSkipped=false;tutorialFinished=false;fixedTutorial=false;tutorialStep=0;ShowRoleDetails=false;State=SharedRules.Create(demo?17:++seed,demo);SelectedId="vanguard";Busy=false;LastSummary="";DiscardIds.Clear();ResetInput();
            InitializeFocusTutorial(demo);
            Hint=demo?"固定起手含战技。点术师头像开始首战教学。":"随机起手：保留关键牌，利用短步与普攻补足战术。";RefreshBoard();Render();
        }
        string DropRecipient(Vector2 point,out bool blocked)
        {
            blocked=false;if(EventSystem.current==null)return null;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count==0)return null;blocked=true;var id=hud==null?null:hud.PartyAt(point);
            return IsDragging&&PendingCard!=null&&id!=null&&CanExecuteCard(id,PendingCardId)?id:null;
        }
        public Vector2? AimOrigin()
        {var u=State.Find(SelectedId);return Board==null||u==null?(Vector2?)null:ViewCamera.WorldToScreenPoint(Board.World(Grid(u.Position))+Vector3.up*.6f);}
        public bool CanDropAt(Vector2 point)
        {
            if(!CanAct||PendingCard==null||!IsDragging)return false;
            bool blocked;string id=DropRecipient(point,out blocked);if(id==null&&!blocked)id=PickUnit(point);
            var u=State.Find(id);
            if(u!=null&&u.Alive&&u.Player)
            {
                if(!FocusAllowsUnit(u.Id))
                {
                    lastHoveredUnitId=null;previewExecutorId=null;lastRangeKey=null;hasHoverExecutorPreview=false;
                    Preview=null;previewAction=null;Board?.PreviewAp(null,0);Board?.ClearHighlights();Board?.ShowExecutors(legalExecutorIds);overlay?.Hide();
                    RefreshFocusCopy();RejectFocusInput(u.Id);return false;
                }
                if(lastHoveredUnitId==u.Id)return previewExecutorId==u.Id;
                lastHoveredUnitId=u.Id;lastRangeKey=null;hasHoverExecutorPreview=true;Preview=null;previewAction=null;
                previewExecutorId=CanExecuteCard(u.Id,PendingCardId)?u.Id:null;
                if(previewExecutorId!=null)
                {
                    Hint=u.Name+"执行："+SharedRoleInfo.CardEffect(u,PendingCard.Kind);
                    if(PendingCard.Kind==SCard.Heal||PendingCard.Kind==SCard.Shield||PendingCard.Kind==SCard.Skill&&u.Role==SRole.Archer)
                    {previewAction=new SAction{Kind=SActionKind.Card,ActorId=u.Id,CardId=PendingCardId};Preview=SharedRules.Plan(State,previewAction);}
                    else ShowCardExecutorRanges(u,PendingCard);
                    Board?.ShowExecutors(new[]{u.Id});
                    Board?.PreviewAp(u.Id,SharedRules.Cost(PendingCard.Kind));
                }
                else {Hint=u.Name+"无法合法执行此牌";Board?.ShowSharedActionRanges(null,null);Board?.PreviewAp(null,0);}
                RefreshFocusCopy();ShowPreview();Render();return previewExecutorId!=null;
            }
            if(lastHoveredUnitId!=null)
            {
                lastHoveredUnitId=null;previewExecutorId=null;lastRangeKey=null;hasHoverExecutorPreview=false;
                Preview=null;previewAction=null;Board?.PreviewAp(null,0);Board?.ClearHighlights();
                Board?.ShowExecutors(legalExecutorIds);overlay?.Hide();
                RefreshFocusCopy();
                Hint="拖到队员查看效果，拖到空白处松手取消。";Render();
            }
            return false;
        }
        public void DragTo(Vector2 point){CanDropAt(point);}
        public void DropAt(Vector2 point)
        {
            if(!IsDragging)return;ignoreClickFrame=Time.frameCount;
            bool blocked;string id=DropRecipient(point,out blocked);if(id==null&&!blocked)id=PickUnit(point);
            var u=State.Find(id);if(u!=null&&u.Player){if(!FocusAllowsUnit(id)){Cancel();RejectFocusInput(id);return;}DropCardOn(id);return;}
            Cancel();
        }
        public void InspectCard(string name)
        {
            inspected=null;if(name!=null)foreach(SCard kind in System.Enum.GetValues(typeof(SCard)))if(SharedRules.CardName(kind)==name){inspected=kind;break;}
            if(hud!=null)hud.Render();
        }
        static GridPos Grid(SPos p)=>new GridPos(p.X,p.Y);
        void RefreshBoard()
        {
            if(Board==null)return;
            if(artillery==null)
            {
                artillery=Board.GetComponent<SharedArtilleryView>();
                if(artillery==null)artillery=Board.gameObject.AddComponent<SharedArtilleryView>();
                artillery.Configure(Board);
            }
            artillery.SetWarning(State,State.Phase==SPhase.Player);
            Board.Refresh(SharedVisualAdapter.ToBattleState(State));Board.ShowCellOutlines(State.Telegraph.Select(Grid),new Color(1f,.25f,.12f,.9f));
            var intent=new EnemyTurnPlan();
            if(State.Phase==SPhase.Player)foreach(var step in SharedRules.EnemyPreview(State).Steps)
                intent.Actions.Add(SharedVisualAdapter.ToPlan(step.Before,new SPlan{Valid=true,After=step.After,Summary=step.Summary,Path=step.Path},step.Action));
            Board.ShowIntent(intent);
        }
        void Render(){if(Board!=null)Board.Select(SelectedId);if(hud!=null)hud.Render();}
    }
}
