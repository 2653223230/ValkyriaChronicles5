using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE
{
    public sealed class BattleHud : MonoBehaviour
    {
        public static readonly Color Ink = new Color(.025f,.085f,.10f,.91f);
        public static readonly Color Gold = new Color(.88f,.76f,.43f);
        public static readonly Color Jade = new Color(.35f,.75f,.68f);
        public static readonly Color Paper = new Color(.88f,.91f,.85f);
        public Font Font;
        public RectTransform Root;
        public Text Header, Left, Right, Hint;
        public Button Move, Attack, Heal, End, Exchange, Confirm, Cancel, Begin;
        public RectTransform HandRoot, PreviewRoot, DeploymentRoot;
        public RectTransform ActionMenu;
        public PartyPanelView PartyPanel { get; private set; }
        private Text actionTitle, remainingApLabel, exhaustedApLabel;
        private readonly List<GameObject> remainingApDiamonds = new List<GameObject>();
        public Action<ActionKind> SelectAction;
        public Action EndTurn, ExchangeMode, ConfirmAction, CancelAction, BeginBattle;
        private Text previewText;
        private bool livePreview;

        public void Build(RectTransform root, Font font)
        {
            Root = root; Font = font;
            Panel(root, "Top rail", 0,0,1920,90,new Color(.01f,.025f,.035f,.9f));
            Header = Label(root,"回合",55,25,1810,42,27,Gold,font);
            PartyPanel = new GameObject("Approved party panel", typeof(RectTransform)).AddComponent<PartyPanelView>();
            PartyPanel.Build(root, font);
            Left = Label(root,"队伍",44,178,240,318,20,Paper,font);
            Left.gameObject.SetActive(false);
            Panel(root,"Intent panel",1590,160,304,610,Ink,Jade);
            Right = Label(root,"敌方意图",1608,182,270,572,19,Paper,font);
            var hintPanel=Panel(root,"Hint backing",390,104,1140,48,new Color(.035f,.11f,.13f,.82f));
            Hint = Label(hintPanel,"操作提示",14,5,1112,38,21,Paper,font);
            HandRoot = Panel(root,"Hand",380,844,1488,216,new Color(.02f,.06f,.075f,.68f));
            End = ButtonAt(root,"结束回合",38,902,288,70,font,()=>EndTurn?.Invoke(),true);
            Exchange = ButtonAt(root,"换牌 1 次",38,992,288,54,font,()=>ExchangeMode?.Invoke(),false);
            ActionMenu=Panel(root,"Piece action menu",600,300,360,310,new Color(.035f,.10f,.13f,.98f),Gold);
            actionTitle=Label(ActionMenu,"行动棋子",18,10,326,28,21,Gold,font);
            remainingApLabel=Label(ActionMenu,"Remaining AP label",18,38,96,32,18,Gold,font);
            remainingApLabel.text="剩余AP：";
            actionTitle.verticalOverflow=VerticalWrapMode.Overflow;remainingApLabel.verticalOverflow=VerticalWrapMode.Overflow;
            remainingApLabel.alignment=TextAnchor.MiddleLeft;
            exhaustedApLabel=Label(ActionMenu,"AP exhausted",122,38,200,32,16,new Color(.50f,.62f,.61f),font);
            exhaustedApLabel.verticalOverflow=VerticalWrapMode.Overflow;exhaustedApLabel.alignment=TextAnchor.MiddleLeft;
            Move=ButtonAt(ActionMenu,"移动",14,76,332,64,font,()=>SelectAction?.Invoke(ActionKind.Move));
            Attack=ButtonAt(ActionMenu,"普通攻击",14,150,332,64,font,()=>SelectAction?.Invoke(ActionKind.Attack));
            Heal=ButtonAt(ActionMenu,"急救",14,224,332,64,font,()=>SelectAction?.Invoke(ActionKind.Heal));
            AddActionCostDiamond(Move,"移动");
            AddActionCostDiamond(Attack,"普通攻击");
            AddActionCostDiamond(Heal,"急救");
            ActionMenu.gameObject.SetActive(false);
            PreviewRoot = Panel(root,"Target preview",1498,170,394,630,new Color(.025f,.085f,.10f,1f),Gold);
            previewText = Label(PreviewRoot,"预览详情",24,25,344,430,23,Paper,font);
            Cancel = ButtonAt(PreviewRoot,"取消",18,551,155,54,font,()=>CancelAction?.Invoke());
            Confirm = ButtonAt(PreviewRoot,"确认出牌",213,551,162,54,font,()=>ConfirmAction?.Invoke(),true);
            PreviewRoot.gameObject.SetActive(false);
            PartyPanel.SelectUnit = id => SelectUnit?.Invoke(id);
            DeploymentRoot = Panel(root,"Deploy controls",30,842,1860,216,new Color(.025f,.085f,.10f,1f),Jade);
            Label(DeploymentRoot,"部署说明",45,42,1260,90,25,Paper,font).text="队伍部署\n拖动角色到 B–D / 7–8 格交换位置，也可点选角色后点格子。";
            Begin = ButtonAt(DeploymentRoot,"开始战斗",1430,47,335,82,font,()=>BeginBattle?.Invoke(),true);
        }

        public void Render(BattleState state, UnitState selected, EnemyTurnPlan intent, bool busy)
        {
            bool deploying = state.Phase == BattlePhase.Deployment;
            bool canAct = state.Phase == BattlePhase.Player && !busy;
            DeploymentRoot.gameObject.SetActive(deploying);
            HandRoot.gameObject.SetActive(!deploying);
            End.gameObject.SetActive(!deploying);Exchange.gameObject.SetActive(!deploying);
            if(!canAct) HideActions();
            Header.text = deploying ? "部署阶段   ·   森钟遗迹" : state.Phase == BattlePhase.Enemy ? "敌方回合   ·   敌军行动中   ·   操作已锁定" : "己方回合 " + state.Round + "   ·   目标：全灭敌人";
            PartyPanel.Render(state, selected, busy);
            var info=new StringBuilder("首关目标\n击败全部敌方单位\n\n");
            foreach(var u in state.Units)
                if(u.Team==Team.Enemy && u.IsAlive) { info.AppendLine(Name(u.Role)+"  "+Coord(u.Position)+"   HP "+u.Hp); if(u.MarkExpiresRound>0) info.AppendLine("  印记：至第"+u.MarkExpiresRound+"轮结束"); }
            info.AppendLine("\n若现在结束回合：");
            if(intent==null || intent.Actions.Count==0) info.AppendLine("暂无可执行敌方行动");
            else foreach(var p in intent.Actions)
            {
                var u=state.FindUnit(p.Request.ActorId);
                string n=u==null ? "敌军" : Name(u.Role);
                if(p.Request.Kind==ActionKind.Move) info.AppendLine(n+" → "+Coord(p.Request.Destination.Value));
                else if(p.DamageSummary!=null) { var target=state.FindUnit(p.Request.TargetId); info.AppendLine(n+" → "+(target==null ? "目标" : Name(target.Role))+" -"+p.DamageSummary.HpLost+" HP"); }
            }
            info.AppendLine("\n手牌 "+state.Hand.Count+" / 6  ·  牌库 "+state.DrawPile.Count+"\n弃牌 "+state.DiscardPile.Count);
            intentCopy=info.ToString();Right.text=intentCopy;
            Move.interactable=canAct && selected!=null && selected.IsAlive && selected.Ap>=1;
            Attack.interactable=Move.interactable && !selected.AttackUsed;
            Heal.interactable=Move.interactable && selected.Role==UnitRole.Support && !selected.HealUsed;
            End.interactable=canAct;
            Exchange.interactable=canAct && !state.ExchangedThisTurn && !state.FirstActionMade && state.DrawPile.Count+state.DiscardPile.Count>0;
            Begin.interactable=!busy;
            if(busy) HidePreview();
        }

        public Action<string> SelectUnit;
        public void ConfigureParty(Sprite[] sprites) { if(PartyPanel!=null) PartyPanel.Configure(sprites); }
        public string PartyUnitAtScreen(Vector2 screenPosition) { return PartyPanel==null?null:PartyPanel.UnitAtScreen(screenPosition); }

        public void ShowPreview(string text, bool valid)
        {
            HideActions();
            livePreview=false;
            PreviewRoot.gameObject.SetActive(true); previewText.text=text;
            Cancel.gameObject.SetActive(true);Confirm.gameObject.SetActive(true);
            Confirm.interactable=valid;
        }
        public void ShowLivePreview(string text, bool valid)
        {
            HideActions();
            livePreview=true;
            PreviewRoot.gameObject.SetActive(true);previewText.text=text;
            Cancel.gameObject.SetActive(false);Confirm.gameObject.SetActive(false);
        }
        public void HidePreview()
        {
            PreviewRoot.gameObject.SetActive(false);
            livePreview=false;
            if(Cancel!=null) Cancel.gameObject.SetActive(true);
            if(Confirm!=null) Confirm.gameObject.SetActive(true);
        }
        public void HideActions() { if(ActionMenu!=null) ActionMenu.gameObject.SetActive(false); }
        public void ShowActions(UnitState unit, Vector2 screenPosition)
        {
            if(unit==null || !unit.IsAlive) { HideActions();return; }
            bool support=unit.Role==UnitRole.Support;
            ActionMenu.sizeDelta=new Vector2(360,support?310:236);
            ((RectTransform)ActionMenu.Find("Bottom border")).anchoredPosition=new Vector2(0,-ActionMenu.sizeDelta.y+2);
            ((RectTransform)ActionMenu.Find("Left border")).sizeDelta=new Vector2(2,ActionMenu.sizeDelta.y);
            ((RectTransform)ActionMenu.Find("Right border")).sizeDelta=new Vector2(2,ActionMenu.sizeDelta.y);
            Heal.gameObject.SetActive(support);
            actionTitle.text=Name(unit.Role)+"  ·  "+Coord(unit.Position);
            exhaustedApLabel.text=unit.Ap<=0?"行动耗尽":"";
            SetRemainingApDiamonds(unit.Ap);
            Move.GetComponentInChildren<Text>().text=ActionLabel("移动","最多 "+unit.MoveSteps+" 格");
            Attack.GetComponentInChildren<Text>().text=ActionLabel("普通攻击",unit.Attack+"伤害 / 射程"+unit.AttackRange+(unit.AttackUsed?" / 已用":""));
            Heal.GetComponentInChildren<Text>().text=ActionLabel("急救","治疗2 / 距离2"+(unit.HealUsed?" / 已用":""));
            Move.interactable=unit.Ap>=1;
            Attack.interactable=unit.Ap>=1 && !unit.AttackUsed;
            Heal.interactable=unit.Ap>=1 && support && !unit.HealUsed;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root,screenPosition,null,out local);
            float x=local.x-Root.rect.xMin+35;
            float y=Root.rect.yMax-local.y-110;
            // Keep enemy intent visible; flip to the unit's left near that panel.
            if(x+360>1570) x-=430;
            ActionMenu.anchoredPosition=new Vector2(Mathf.Clamp(x,318,Mathf.Min(1210,Root.rect.width-378)),-Mathf.Clamp(y,158,Root.rect.height-246-ActionMenu.sizeDelta.y));
            ActionMenu.SetAsLastSibling();ActionMenu.gameObject.SetActive(true);
        }
        public void SetHint(string value) { Hint.text=value; }
        private string intentCopy;
        public void RestoreIntent() { if(!PreviewRoot.gameObject.activeSelf) Right.text=intentCopy; }
        public void ShowCardDetails(CardDefinition card)
        {
            if(PreviewRoot.gameObject.activeSelf) return;
            Right.text=card.Name+"\n"+card.UserLabel+"\n\n"+card.Description+"\n\n"+CardTargetHint(card.Kind);
        }
        private static string CardTargetHint(CardKind kind)
        {
            switch(kind)
            {
                case CardKind.Advance:return "拖动卡牌至合法执行者，再选择合法移动格子。";
                case CardKind.HeavyAttack: case CardKind.SparkMark: case CardKind.StarBurst:return "拖动卡牌至合法执行者，再选择合法敌人。";
                case CardKind.Cover:return "拖动卡牌至合法执行者，掩护由该棋子立即结算。";
                case CardKind.Charge:return "拖动卡牌至合法执行者，再选择敌人并确认突入落点。";
                case CardKind.Inspire:return "拖动卡牌至合法执行者，再选择其他己方棋子。";
                default:return "拖动卡牌至合法执行者，再选择合法目标。";
            }
        }
        private static string ActionLabel(string name,string details)
        { return name+"  ·\n"+details; }
        private void AddActionCostDiamond(Button button,string actionName)
        {
            var label=button.GetComponentInChildren<Text>();
            label.fontSize=20;
            label.text=ActionLabel(actionName,"");
            float firstLineWidth=actionName.Length*20f+38f;
            float centerX=8f+(316f+firstLineWidth)*.5f+11f;
            BlueDiamond((RectTransform)button.transform,"Action AP diamond",centerX,18f,16f);
        }
        private void SetRemainingApDiamonds(int amount)
        {
            while(remainingApDiamonds.Count<amount)
            {
                int index=remainingApDiamonds.Count;
                var diamond=BlueDiamond(ActionMenu,"Remaining AP diamond "+(index+1),124f+index*24f,54f,17f);
                remainingApDiamonds.Add(diamond.gameObject);
            }
            for(int i=0;i<remainingApDiamonds.Count;i++)remainingApDiamonds[i].SetActive(i<amount);
        }
        private static RectTransform BlueDiamond(RectTransform parent,string name,float centerX,float centerY,float size)
        {
            var frame=Panel(parent,name,centerX-size*.5f,centerY-size*.5f,size,size,new Color(.027f,.216f,.329f));
            frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=new Vector2(centerX,-centerY);frame.localRotation=Quaternion.Euler(0,0,45);
            frame.GetComponent<Image>().raycastTarget=false;
            float inset=size*.19f;
            var core=Panel(frame,"Blue core",inset,inset,size-2*inset,size-2*inset,new Color(.208f,.812f,1f));
            core.GetComponent<Image>().raycastTarget=false;
            return frame;
        }
        public static string Coord(GridPos p) { return ((char)('A'+p.X)).ToString()+(p.Y+1); }
        public static string Name(UnitRole r)
        {
            return Definitions.UnitName(r);
        }

        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform)); var r=(RectTransform)go.transform;
            r.SetParent(parent,false); r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
        }
        public static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color fill,Color? border=null)
        {
            var r=Rect(parent,name,x,y,w,h); var i=r.gameObject.AddComponent<Image>();i.color=fill;
            if(border.HasValue)
            {
                Panel(r,"Top border",0,0,w,2,border.Value);
                Panel(r,"Bottom border",0,h-2,w,2,border.Value);
                Panel(r,"Left border",0,0,2,h,border.Value);
                Panel(r,"Right border",w-2,0,2,h,border.Value);
            }
            return r;
        }
        public static Text Label(Transform parent,string name,float x,float y,float w,float h,int size,Color color,Font font)
        {
            var r=Rect(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<Text>();
            t.font=font;t.fontSize=size;t.color=color;t.supportRichText=true;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            return t;
        }
        public static Button ButtonAt(Transform parent,string text,float x,float y,float w,float h,Font font,Action action,bool gold=false)
        {
            var r=Panel(parent,text,x,y,w,h,gold ? new Color(.29f,.25f,.13f,.97f):new Color(.06f,.17f,.18f,.96f),gold?Gold:Jade);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
            var colors=b.colors; colors.normalColor=Color.white;colors.highlightedColor=new Color(1.2f,1.17f,1.05f);colors.pressedColor=new Color(.7f,.9f,.9f);colors.disabledColor=new Color(.35f,.4f,.4f,.5f);b.colors=colors;
            b.onClick.AddListener(()=>action?.Invoke());
            var t=Label(r,text+" label",8,6,w-16,h-12,24,gold?Gold:Paper,font);t.text=text;t.alignment=TextAnchor.MiddleCenter;
            t.verticalOverflow=VerticalWrapMode.Overflow;
            return b;
        }
    }
}
