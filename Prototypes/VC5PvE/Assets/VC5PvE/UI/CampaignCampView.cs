using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE
{
    /// <summary>Three-step between-battle growth screen. Rule changes happen only after an explicit confirmation.</summary>
    public sealed class CampaignCampView : MonoBehaviour
    {
        private static readonly Color Deep = new Color(.018f,.054f,.067f,.98f);
        private static readonly Color PanelColor = new Color(.045f,.115f,.13f,.98f);
        private static readonly Color Soft = new Color(.09f,.19f,.20f,.96f);
        private static readonly Color Line = new Color(.28f,.48f,.44f);
        private static readonly Color Muted = new Color(.64f,.75f,.71f);
        private static readonly UnitRole[] PartyRoles = { UnitRole.Warrior, UnitRole.Mage, UnitRole.Support };

        private RectTransform root, viewRoot, content;
        private Font font;
        private Sprite[] portraits;
        private RunState run;
        private Action onComplete;
        private int reviewStep = -1;
        private UnitRole? selectedRole;
        private TalentKind? selectedTalent;
        private CardKind? selectedReward;
        private string selectedCardId;
        private bool? upgradeSelection;
        public bool EditorReviewOnPlay;
        public int EditorReviewSeed = 20261021;
        public Font EditorReviewFont;
        public Sprite[] EditorReviewPortraits;
        private Text headerTitle, campCounter;
        private Text heading, subheading, status;
        private Button confirmButton, skipButton;
        private Button[] stepButtons = new Button[3];
        private Text[] partyTalentLabels = new Text[3];
        private RectTransform reviewControls;
        private Text confirmLabel;

        /// <summary>Build the complete camp canvas as children of an existing screen-space root.</summary>
        public void Build(RectTransform parent, Font uiFont, Sprite[] unitSprites)
        {
            root = parent; font = uiFont; portraits = unitSprites;
            viewRoot=BattleHud.Rect(parent,"Campaign camp view",0,0,1920,1080);
            var own = viewRoot;
            var veil = BattleHud.Panel(own,"Camp backdrop",0,0,1920,1080,Deep);
            for (int i=0;i<8;i++)
                BattleHud.Panel(veil,"Atmosphere band "+i,0,88+i*118,1920,1,new Color(.18f,.32f,.31f,.18f));

            var top = BattleHud.Panel(own,"Camp header",0,0,1920,112,new Color(.025f,.075f,.084f,.99f),BattleHud.Gold);
            BattleHud.Label(top,"Camp eyebrow",54,13,310,24,17,BattleHud.Jade,font).text="FIELD CAMP   /   BETWEEN BATTLES";
            headerTitle = BattleHud.Label(top,"Camp title",53,42,420,50,38,BattleHud.Paper,font);
            headerTitle.text = "整备营地";
            campCounter = BattleHud.Label(top,"Camp counter",470,47,300,40,24,BattleHud.Gold,font);
            for (int i=0;i<3;i++)
            {
                int index=i;
                stepButtons[i]=BattleHud.ButtonAt(top,"Camp step "+i,1090+i*250,28,220,60,font,()=>SelectReviewStep(index),i==0);
                stepButtons[i].GetComponentInChildren<Text>().fontSize=22;
            }

            var party = BattleHud.Panel(own,"Party and direction panel",38,136,405,620,PanelColor,Line);
            BattleHud.Label(party,"Party heading",24,18,350,40,27,BattleHud.Gold,font).text="同行者";
            BattleHud.Label(party,"Party caption",25,60,350,32,19,Muted,font).text="三人小队 · 本轮成长";
            for (int i=0;i<3;i++) BuildPartyCard(party,i);
            var relic = BattleHud.Panel(party,"Camp note",18,439,369,150,new Color(.055f,.145f,.15f),new Color(.40f,.55f,.42f));
            BattleHud.Label(relic,"Camp note title",17,13,330,31,22,BattleHud.Gold,font).text="整备原则";
            BattleHud.Label(relic,"Camp note copy",18,51,330,88,19,BattleHud.Paper,font).text="每一步先选择，再确认。\n新卡可跳过；升级或删牌\n只影响一张实体牌。";

            var stage = BattleHud.Panel(own,"Selection panel",467,136,1415,620,PanelColor,Line);
            heading = BattleHud.Label(stage,"Selection title",34,20,1000,48,31,BattleHud.Paper,font);
            subheading = BattleHud.Label(stage,"Selection subtitle",36,69,1320,34,20,Muted,font);
            content = BattleHud.Rect(stage,"Selection content",26,116,1363,397);
            status = BattleHud.Label(stage,"Selection status",37,520,810,38,20,BattleHud.Jade,font);
            status.text="选择一项内容后，再按确认。";
            confirmButton=BattleHud.ButtonAt(stage,"确认选择",1010,526,190,68,font,ConfirmCurrent,true);
            confirmLabel=confirmButton.GetComponentInChildren<Text>();
            skipButton=BattleHud.ButtonAt(stage,"跳过此步",1215,526,165,68,font,SkipCurrent,false);
            confirmLabel.fontSize=20;
            skipButton.GetComponentInChildren<Text>().text="跳过";
            skipButton.GetComponentInChildren<Text>().fontSize=20;

            BuildNextLevel(own);
            foreach (Text label in viewRoot.GetComponentsInChildren<Text>(true))
                label.verticalOverflow=VerticalWrapMode.Overflow;
            viewRoot.gameObject.SetActive(false);
        }

        private void BuildPartyCard(RectTransform parent,int index)
        {
            var card=BattleHud.Panel(parent,"Party member "+index,17,102+index*106,371,92,new Color(.072f,.16f,.17f),new Color(.20f,.37f,.36f));
            var crop=BattleHud.Panel(card,"Portrait crop",8,8,68,76,new Color(.13f,.23f,.23f));
            crop.gameObject.AddComponent<RectMask2D>();
            var image=BattleHud.Rect(crop,"Portrait",-16,-9,101,119).gameObject.AddComponent<Image>();
            image.preserveAspect=true;image.raycastTarget=false;
            if(portraits!=null && index<portraits.Length) image.sprite=portraits[index];
            var role=PartyRoles[index];
            BattleHud.Label(card,"Role name",89,12,260,33,25,BattleHud.Paper,font).text=Definitions.UnitName(role);
                BattleHud.Label(card,"Role description",90,48,260,28,17,BattleHud.Jade,font).text=Definitions.PartyDescription(role);
            partyTalentLabels[index]=BattleHud.Label(card,"Owned talents",90,68,260,20,15,Muted,font);
            partyTalentLabels[index].text="尚未习得天赋";
        }

        private void BuildNextLevel(RectTransform parent)
        {
            var panel=BattleHud.Panel(parent,"Next battle preview",38,781,1844,265,new Color(.035f,.096f,.106f,.98f),Line);
            BattleHud.Label(panel,"Next battle label",24,14,350,36,23,BattleHud.Gold,font).text="下一战 · 地图预览";
            var map=BattleHud.Panel(panel,"Miniature map",28,62,316,180,new Color(.014f,.045f,.054f),new Color(.35f,.5f,.42f));
            for(int y=0;y<8;y++) for(int x=0;x<8;x++)
            {
                float tileW=34f,tileH=20f;
                var cell=BattleHud.Panel(map,"Map tile "+x+" "+y,5+x*38,5+y*21,34,20,((x+y)%2==0)?new Color(.10f,.20f,.19f):new Color(.075f,.16f,.16f));
                cell.GetComponent<Image>().raycastTarget=false;
            }
            AddMapLegend(panel,370,68);
            BattleHud.Label(panel,"Mechanic label",370,108,300,34,22,BattleHud.Jade,font).text="本关机制摘要";
            var mechanism=BattleHud.Label(panel,"Mechanic summary",370,148,1415,90,22,BattleHud.Paper,font);
            mechanism.text="选择成长后，这里会显示下一关布局与机制。";
            mechanism.horizontalOverflow=HorizontalWrapMode.Wrap; mechanism.verticalOverflow=VerticalWrapMode.Overflow;
            // Keep a reference for the map/mechanic data refresh without relying on scene lookups.
            nextMap=map; nextMechanic=mechanism;
        }

        private void AddMapLegend(RectTransform panel,float x,float y)
        {
            var names=new[]{"队伍","敌军","设施","障碍"};
            var colors=new[]{new Color(.22f,.57f,.51f),new Color(.67f,.30f,.27f),new Color(.69f,.30f,.34f),new Color(.32f,.40f,.38f)};
            for(int i=0;i<names.Length;i++)
            {
                float left=x+i*112;
                var swatch=BattleHud.Label(panel,"Map legend swatch "+i,left,y,24,28,22,colors[i],font);
                swatch.text="■";swatch.alignment=TextAnchor.MiddleCenter;
                BattleHud.Label(panel,"Map legend label "+i,left+27,y+2,82,25,17,Muted,font).text=names[i];
            }
        }

        private RectTransform nextMap;
        private Text nextMechanic;

        public void Show(RunState state,Action completed)
        {
            run=state;onComplete=completed;reviewStep=-1;selectedRole=null;selectedTalent=null;selectedReward=null;selectedCardId=null;upgradeSelection=null;
            if(run==null) throw new ArgumentNullException("state");
            if(viewRoot!=null)viewRoot.gameObject.SetActive(true);Refresh();
        }

        private void Awake()
        {
            if(!EditorReviewOnPlay)return;
            var parent=transform.parent as RectTransform;
            if(parent==null)throw new InvalidOperationException("Campaign review view requires a Canvas parent.");
            Build(parent,EditorReviewFont!=null?EditorReviewFont:Resources.GetBuiltinResource<Font>("Arial.ttf"),EditorReviewPortraits);
            var recoveryReview=new GameObject("Recovery review",typeof(RectTransform)).AddComponent<RecoveryPickerView>();
            recoveryReview.transform.SetParent(parent,false);recoveryReview.Build(parent,font);
            var reviewButton=BattleHud.ButtonAt(viewRoot,"查看回收界面",820,30,220,56,font,()=>
                recoveryReview.Show(new List<CardInstance>{
                    new CardInstance{Id="review-heavy",Kind=CardKind.HeavyAttack},
                    new CardInstance{Id="review-heavy-plus",Kind=CardKind.HeavyAttack,Upgraded=true},
                    new CardInstance{Id="review-relay",Kind=CardKind.RelayStrike}},
                    "战术回收 · 选择一张牌","支援已生效。可收回一张中立攻击牌，或跳过；本次回收不再花费行动点。",
                    id=>{},()=>{},true));
            reviewButton.GetComponentInChildren<Text>().fontSize=20;
            RunState sample=CampaignRules.NewRun(EditorReviewSeed);
            if(sample!=null&&CampaignRules.BeginCamp(sample))
            {
                if(CampaignRules.ChooseTalent(sample,TalentKind.MarkFlow))CampaignRules.ChooseReward(sample,null);
                ShowReview(sample,0);
            }
        }

        /// <summary>Shows one of the three states in the editor review scene without committing rule changes.</summary>
        public void ShowReview(RunState state,int step)
        {
            run=state;onComplete=null;reviewStep=Mathf.Clamp(step,0,2);selectedRole=null;selectedTalent=null;selectedReward=null;selectedCardId=null;upgradeSelection=null;
            if(run==null) throw new ArgumentNullException("state");
            if(viewRoot!=null)viewRoot.gameObject.SetActive(true);Refresh();
        }

        public void Hide()
        {
            if(viewRoot!=null)viewRoot.gameObject.SetActive(false);run=null;onComplete=null;reviewStep=-1;
        }

        private int CurrentStep { get { return reviewStep>=0?reviewStep:Mathf.Clamp(run==null?0:run.CampStep,0,2); } }

        private void Refresh()
        {
            if(run==null)return;
            int step=CurrentStep;
            if(reviewStep<0) campCounter.text="第 "+Mathf.Min(2,run.CompletedCamps+1)+" / 2 次营地";
            else campCounter.text="审核预览 · 状态 "+(step+1)+" / 3";
            for(int i=0;i<PartyRoles.Length;i++)
            {
                List<TalentKind> owned;
                if(run.Talents!=null&&run.Talents.TryGetValue(PartyRoles[i],out owned)&&owned.Count>0)
                {
                    var names=new List<string>();foreach(var kind in owned){var definition=CampaignRules.Talent(kind);if(definition!=null)names.Add(definition.Name);}
                    partyTalentLabels[i].text=string.Join(" · ",names.ToArray());
                    partyTalentLabels[i].color=BattleHud.Gold;
                }
                else {partyTalentLabels[i].text="尚未习得天赋";partyTalentLabels[i].color=Muted;}
            }
            for(int i=0;i<stepButtons.Length;i++)
            {
                var label=stepButtons[i].GetComponentInChildren<Text>();
                label.text=(i==step?"◆  ":"○  ")+(i==0?"棋子成长":i==1?"新卡奖励":"卡组整备");
                stepButtons[i].interactable=reviewStep>=0;
            }
            UpdateNextLevel();
            Clear(content);
            confirmButton.interactable=false;skipButton.gameObject.SetActive(false);
            heading.text=step==0?"选择一名棋子，再习得一项天赋":step==1?"从三张新卡中选一张，或跳过":"整备一张实体牌：升级或永久删除";
            subheading.text=step==0?"先选角色，再选择该角色尚未拥有的天赋。确认后本次成长生效。":step==1?"三张卡牌各不相同；卡面会显示费用与作用。新牌加入卡组，不会直接进入手牌。":"选中卡牌和处理方式后，先查看前后变化，再确认执行。";
            if(step==0)DrawGrowth();else if(step==1)DrawRewards();else DrawMaintenance();
            UpdateConfirmState();
            EnableCampLabels();
        }

        private void DrawGrowth()
        {
            status.text=selectedRole.HasValue?"已选择："+Definitions.UnitName(selectedRole.Value)+"。再挑一项天赋。":"第一步 · 选择培养对象";
            for(int i=0;i<3;i++)
            {
                UnitRole role=PartyRoles[i];bool chosen=selectedRole.HasValue&&selectedRole.Value==role;
                var button=BattleHud.ButtonAt(content,"Choose role "+role,18+i*445,10,425,76,font,()=>
                { selectedRole=role;selectedTalent=null;Refresh(); },chosen);
                button.GetComponentInChildren<Text>().text=(chosen?"◆  ":"◇  ")+Definitions.UnitName(role)+"   ·   "+Definitions.PartyDescription(role);
                button.GetComponentInChildren<Text>().fontSize=22;
                List<TalentKind> owned;
                bool full=run.Talents!=null&&run.Talents.TryGetValue(role,out owned)&&owned.Count>=2;
                button.interactable=!full;
            }
            if(!selectedRole.HasValue)
            {
                BattleHud.Label(content,"Growth prompt",26,126,1280,110,25,Muted,font).text="选择守铃者、星纹师或回响医者，查看其尚未获得的天赋。\n两次营地可培养同一人，也可分别培养两人。";
                return;
            }
            var talents=CampaignRules.TalentsFor(selectedRole.Value);
            List<TalentKind> existing;
            if(run.Talents!=null&&run.Talents.TryGetValue(selectedRole.Value,out existing))
                talents.RemoveAll(value=>existing.Contains(value.Kind));
            int index=0;
            foreach(var talent in talents)
            {
                int captured=index++; bool chosen=selectedTalent.HasValue&&selectedTalent.Value==talent.Kind;
                var card=BattleHud.ButtonAt(content,"Choose talent "+talent.Kind,20+captured*445,121,425,245,font,()=>
                { selectedTalent=talent.Kind;Refresh(); },chosen);
                var texts=card.GetComponentsInChildren<Text>();
                texts[0].text="";
                var title=BattleHud.Label(card.transform,"Talent title",18,8,386,42,25,chosen?BattleHud.Gold:BattleHud.Paper,font);
                title.text=(chosen?"◆  ":"")+talent.Name;
                var desc=BattleHud.Label(card.transform,"Talent description",19,59,384,128,21,BattleHud.Paper,font);
                desc.text=talent.Description.Replace("1点AP","<color=#27C9FF>◆</color>");
                desc.verticalOverflow=VerticalWrapMode.Overflow;
                var reward=BattleHud.Label(card.transform,"Talent reward",18,192,386,34,18,BattleHud.Jade,font);
                reward.text="相关奖励卡 · "+Definitions.Card(talent.RewardCard).Name;
            }
            if(index==0) BattleHud.Label(content,"No talents left",20,133,1200,64,23,BattleHud.Paper,font).text="该棋子已获得所有可选天赋。请返回选择另一名棋子。";
        }

        private void DrawRewards()
        {
            status.text=selectedReward.HasValue?"已选："+Definitions.Card(selectedReward.Value).Name+"。确认后加入卡组。":"第二步 · 三选一，或明确跳过";
            if(run.Offers==null||run.Offers.Count==0)
            {
                BattleHud.Label(content,"Reward unavailable",26,38,1200,80,24,BattleHud.Paper,font).text="当前没有可选奖励卡。请跳过此步继续。";
                skipButton.gameObject.SetActive(true);skipButton.interactable=true;return;
            }
            int count=Mathf.Min(3,run.Offers.Count);
            for(int i=0;i<count;i++)
            {
                CardKind kind=run.Offers[i];CardDefinition definition=Definitions.Card(kind);bool chosen=selectedReward.HasValue&&selectedReward.Value==kind;
                var card=BattleHud.ButtonAt(content,"Reward card "+i,18+i*445,18,425,319,font,()=>{selectedReward=kind;Refresh();},chosen);
                var labels=card.GetComponentsInChildren<Text>();labels[0].text="";
                var title=BattleHud.Label(card.transform,"Reward title",20,8,380,42,27,chosen?BattleHud.Gold:BattleHud.Paper,font);title.text=(chosen?"◆  ":"")+definition.Name;
                DrawCostBadge(card.transform,22,59,definition.Cost,38);
                var cost=BattleHud.Label(card.transform,"Card role",69,62,326,34,20,BattleHud.Jade,font);cost.text=definition.UserLabel;
                var copy=BattleHud.Label(card.transform,"Card description",22,108,380,154,21,BattleHud.Paper,font);copy.text=definition.Description;copy.verticalOverflow=VerticalWrapMode.Overflow;
                BattleHud.Label(card.transform,"Card acquisition",22,272,380,28,17,BattleHud.Jade,font).text="加入卡组 · 未升级实体牌";
            }
            skipButton.gameObject.SetActive(true);skipButton.interactable=true;
        }

        private void DrawMaintenance()
        {
            int count=run.Deck==null?0:run.Deck.Count;
            int sameKindOrdinal=CardOrdinal(selectedCardId);
            status.text=selectedCardId==null?"第三步 · 从卡组中选择一张牌":"已选："+CardName(selectedCardId)+"（同类第 "+sameKindOrdinal+" 张，共 "+count+" 张）";
            var scrollRect=BattleHud.Rect(content,"Deck scroll viewport",12,7,1335,176).gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal=true;scrollRect.vertical=false;scrollRect.movementType=ScrollRect.MovementType.Clamped;scrollRect.viewport=(RectTransform)scrollRect.transform;
            scrollRect.viewport.gameObject.AddComponent<RectMask2D>();
            var row=BattleHud.Rect(scrollRect.transform,"Deck cards",0,0,Mathf.Max(1335,count*190),168);
            row.anchorMin=new Vector2(0,1);row.anchorMax=new Vector2(0,1);row.pivot=new Vector2(0,1);
            scrollRect.content=row;
            for(int i=0;i<count;i++)
            {
                CardInstance instance=run.Deck[i];if(instance==null)continue;
                bool chosen=selectedCardId==instance.Id;
                int cardIndex=i;
                var button=BattleHud.ButtonAt(row,"Deck instance "+instance.Id,6+i*190,4,178,160,font,()=>
                { selectedCardId=instance.Id;upgradeSelection=null;Refresh(); },chosen);
                var def=Definitions.Card(instance);
                button.GetComponentInChildren<Text>().text="";
                var title=BattleHud.Label(button.transform,"Deck card title",10,7,158,34,19,chosen?BattleHud.Gold:BattleHud.Paper,font);title.text=def.Name;
                DrawCostBadge(button.transform,10,43,def.Cost,31);
                var desc=BattleHud.Label(button.transform,"Description",10,82,158,48,15,BattleHud.Paper,font);desc.text=def.UserLabel+"\n"+(instance.Upgraded?"已升级":"未升级");desc.verticalOverflow=VerticalWrapMode.Overflow;
                BattleHud.Label(button.transform,"Entity ordinal",10,136,155,19,13,Muted,font).text="同类第 "+CardOrdinalAt(cardIndex)+" 张";
            }
            BattleHud.Label(content,"Scroll hint",18,185,620,22,15,Muted,font).text="横向滚动查看卡组；每张牌单独整备。";
            var selected=FindCard(selectedCardId);
            bool upgradeAllowed=selected!=null&&!selected.Upgraded;
            bool deleteAllowed=selected!=null&&count>15;
            var upgrade=BattleHud.ButtonAt(content,"Upgrade selected",16,211,250,48,font,()=>{upgradeSelection=true;Refresh();},upgradeSelection==true);
            upgrade.GetComponentInChildren<Text>().text="选择升级";upgrade.GetComponentInChildren<Text>().fontSize=20;
            upgrade.interactable=upgradeAllowed;
            var delete=BattleHud.ButtonAt(content,"Delete selected",282,211,250,48,font,()=>{upgradeSelection=false;Refresh();},upgradeSelection==false);
            delete.GetComponentInChildren<Text>().text="选择永久删除";delete.GetComponentInChildren<Text>().fontSize=20;
            delete.interactable=deleteAllowed;
            if(selected!=null)
            {
                var before=Definitions.Card(selected);
                var afterInstance=selected.Clone();afterInstance.Upgraded=true;
                var after=Definitions.Card(afterInstance);
                bool deleting=upgradeSelection==false;
                bool noOperation=!upgradeSelection.HasValue;
                var preview=BattleHud.Panel(content,"Change preview",550,207,775,178,new Color(.07f,.17f,.17f),noOperation?Line:deleting?new Color(.74f,.42f,.31f):BattleHud.Gold);
                if(noOperation)
                {
                    var prompt=BattleHud.Label(preview,"Change preview prompt",18,14,735,62,19,Muted,font);
                    prompt.text=before.Name+"："+before.Description.Replace("\n"," ")+"\n请选择“升级”或“永久删除”。";
                }
                else if(deleting)
                {
                    var left=BattleHud.Panel(preview,"Before delete",12,12,354,148,new Color(.09f,.19f,.20f));
                    BattleHud.Label(left,"Before delete title",12,10,324,27,19,BattleHud.Gold,font).text="删除前 · 卡组保留";
                    DrawCostBadge(left,12,45,before.Cost,32);
                    BattleHud.Label(left,"Before delete card",54,43,282,103,16,BattleHud.Paper,font).text=before.Name+"\n"+before.Description.Replace("\n"," ");
                    var right=BattleHud.Panel(preview,"After delete",377,12,382,148,new Color(.20f,.115f,.11f));
                    BattleHud.Label(right,"After delete title",12,10,350,27,19,new Color(1f,.69f,.57f),font).text="删除后 · 永久移出";
                    BattleHud.Label(right,"After delete card",12,44,350,70,18,BattleHud.Paper,font).text="卡组剩余 "+(count-1)+" 张"+(count<=15?"\n至少保留 15 张，当前不可删除":"\n确认后不可撤销");
                }
                else
                {
                    var left=BattleHud.Panel(preview,"Before upgrade",12,12,354,148,new Color(.09f,.19f,.20f));
                    BattleHud.Label(left,"Before upgrade title",12,10,324,27,19,Muted,font).text="升级前";
                    DrawCostBadge(left,12,45,before.Cost,32);
                    BattleHud.Label(left,"Before upgrade card",54,43,282,103,16,BattleHud.Paper,font).text=before.Name+"\n"+before.Description.Replace("\n"," ");
                    var right=BattleHud.Panel(preview,"After upgrade",377,12,382,148,new Color(.13f,.20f,.16f));
                    BattleHud.Label(right,"After upgrade title",12,10,350,27,19,BattleHud.Gold,font).text="升级后";
                    DrawCostBadge(right,12,45,after.Cost,32);
                    BattleHud.Label(right,"After upgrade card",54,43,310,103,16,BattleHud.Paper,font).text=after.Name+"\n"+after.Description.Replace("\n"," ");
                }
            }
            skipButton.gameObject.SetActive(true);skipButton.interactable=true;
        }

        private void UpdateConfirmState()
        {
            if(CurrentStep==0)
            {
                bool valid=selectedRole.HasValue&&selectedTalent.HasValue;
                confirmButton.interactable=valid;
                confirmLabel.text=valid?"确认习得":"请选择对象";
                skipButton.gameObject.SetActive(false);
            }
            else if(CurrentStep==1)
            {
                confirmButton.interactable=selectedReward.HasValue;
                confirmLabel.text="确认加入";
                skipButton.gameObject.SetActive(true);
            }
            else
            {
                var card=FindCard(selectedCardId);
                bool valid=card!=null&&upgradeSelection.HasValue&&(upgradeSelection.Value?!card.Upgraded:run.Deck.Count>15);
                confirmButton.interactable=valid;confirmLabel.text=valid?(upgradeSelection.Value?"确认升级":"确认删除"):"先选操作";
                skipButton.gameObject.SetActive(true);
            }
        }

        private void ConfirmCurrent()
        {
            if(run==null)return;
            bool success=false;
            switch(CurrentStep)
            {
                case 0:
                    if(!selectedRole.HasValue||!selectedTalent.HasValue)return;
                    success=reviewStep>=0||CampaignRules.ChooseTalent(run,selectedTalent.Value);
                    break;
                case 1:
                    if(!selectedReward.HasValue)return;
                    success=reviewStep>=0||CampaignRules.ChooseReward(run,selectedReward);
                    break;
                case 2:
                    if(string.IsNullOrEmpty(selectedCardId)||!upgradeSelection.HasValue)return;
                    success=reviewStep>=0||CampaignRules.MaintainDeck(run,selectedCardId,upgradeSelection.Value);
                    break;
            }
            if(!success)return;
            if(reviewStep>=0) { reviewStep=Mathf.Min(2,reviewStep+1);ResetSelection();Refresh();return; }
            if(run.CampStep>=3)
            {
                if(!CampaignRules.Advance(run))return;
                Action callback=onComplete;Hide();callback?.Invoke();return;
            }
            ResetSelection();
            Refresh();
        }

        private void SkipCurrent()
        {
            if(run==null)return;
            bool success=false;
            if(CurrentStep==1) success=reviewStep>=0||CampaignRules.ChooseReward(run,null);
            else if(CurrentStep==2) success=reviewStep>=0||CampaignRules.MaintainDeck(run,null,false);
            else return;
            if(!success)return;
            if(reviewStep>=0){reviewStep=Mathf.Min(2,reviewStep+1);ResetSelection();Refresh();return;}
            if(run.CampStep>=3)
            {
                if(!CampaignRules.Advance(run))return;
                Action callback=onComplete;Hide();callback?.Invoke();return;
            }
            ResetSelection();
            Refresh();
        }

        private void SelectReviewStep(int step)
        {
            if(reviewStep<0)return;
            reviewStep=step;ResetSelection();Refresh();
        }

        private void ResetSelection(){selectedRole=null;selectedTalent=null;selectedReward=null;selectedCardId=null;upgradeSelection=null;}

        private CardInstance FindCard(string id)
        {
            if(run==null||run.Deck==null||string.IsNullOrEmpty(id))return null;
            for(int i=0;i<run.Deck.Count;i++)if(run.Deck[i]!=null&&run.Deck[i].Id==id)return run.Deck[i];
            return null;
        }

        private string CardName(string id){var card=FindCard(id);return card==null?"":Definitions.Card(card).Name;}

        private int CardOrdinal(string id)
        {
            if(string.IsNullOrEmpty(id)||run==null||run.Deck==null)return 0;
            for(int i=0;i<run.Deck.Count;i++)if(run.Deck[i]!=null&&run.Deck[i].Id==id)return CardOrdinalAt(i);
            return 0;
        }

        private int CardOrdinalAt(int index)
        {
            if(run==null||run.Deck==null||index<0||index>=run.Deck.Count||run.Deck[index]==null)return 0;
            CardKind kind=run.Deck[index].Kind;
            int ordinal=0;
            for(int i=0;i<=index;i++)if(run.Deck[i]!=null&&run.Deck[i].Kind==kind)ordinal++;
            return ordinal;
        }

        private void DrawCostBadge(Transform parent,float x,float y,int cost,float size)
        {
            float diamondSize=size*.68f;
            var diamond=BattleHud.Rect(parent,"AP cost diamond",x+(size-diamondSize)*.5f,y+(size-diamondSize)*.5f,diamondSize,diamondSize);
            diamond.pivot=new Vector2(.5f,.5f);
            diamond.anchoredPosition=new Vector2(x+size*.5f,-(y+size*.5f));
            var image=diamond.gameObject.AddComponent<Image>();image.color=new Color(.027f,.216f,.329f);image.raycastTarget=false;
            diamond.localRotation=Quaternion.Euler(0f,0f,45f);
            var number=BattleHud.Label(parent,"AP cost number",x,y,size,size,18,new Color(.45f,.9f,1f),font);
            number.text=cost.ToString();number.alignment=TextAnchor.MiddleCenter;
        }

        private void EnableCampLabels()
        {
            foreach(Text label in viewRoot.GetComponentsInChildren<Text>(true))
                label.verticalOverflow=VerticalWrapMode.Overflow;
        }

        private void Clear(RectTransform parent)
        {
            for(int i=parent.childCount-1;i>=0;i--)
            {
                GameObject child=parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void UpdateNextLevel()
        {
            int level=Mathf.Min(3,Mathf.Max(1,run.LevelIndex+1));
            string name=CampaignRules.LevelName(level),hint=CampaignRules.LevelHint(level);
            nextMechanic.text=name+"\n"+hint;
            // Clear previous map tokens while preserving the 64 walkable tiles.
            for(int i=nextMap.childCount-1;i>=64;i--)
            {
                GameObject marker=nextMap.GetChild(i).gameObject;
                marker.SetActive(false);
                Destroy(marker);
            }
            Action<int,int,string,Color> mark=(x,y,label,color)=>
            {
                var cell=BattleHud.Panel(nextMap,label+" "+x+" "+y,5+x*38,5+y*21,34,20,color);
                var text=BattleHud.Label(cell,"Map symbol",0,0,34,20,16,Color.white,font);text.text=label;text.alignment=TextAnchor.MiddleCenter;
            };
            if(level==2)
            {
                mark(2,2,"#",new Color(.26f,.34f,.32f));mark(2,3,"#",new Color(.26f,.34f,.32f));mark(4,3,"#",new Color(.26f,.34f,.32f));mark(4,4,"#",new Color(.26f,.34f,.32f));
                mark(3,1,"P",new Color(.69f,.48f,.22f));mark(6,1,"P",new Color(.69f,.48f,.22f));mark(3,3,"G",new Color(.67f,.30f,.27f));mark(5,4,"G",new Color(.67f,.30f,.27f));mark(6,2,"G",new Color(.67f,.30f,.27f));mark(5,1,"R",new Color(.69f,.30f,.34f));
            }
            else if(level==3)
            {
                mark(3,2,"#",new Color(.26f,.34f,.32f));mark(3,3,"#",new Color(.26f,.34f,.32f));mark(4,3,"#",new Color(.26f,.34f,.32f));mark(1,2,"A",new Color(.20f,.55f,.57f));mark(6,2,"A",new Color(.20f,.55f,.57f));mark(2,3,"G",new Color(.67f,.30f,.27f));mark(5,3,"G",new Color(.67f,.30f,.27f));mark(4,1,"B",new Color(.70f,.37f,.27f));
            }
            else
            {
                mark(3,3,"G",new Color(.67f,.30f,.27f));mark(5,4,"G",new Color(.67f,.30f,.27f));mark(5,1,"R",new Color(.69f,.30f,.34f));
            }
            mark(2,6,"W",new Color(.22f,.57f,.51f));mark(3,7,"M",new Color(.22f,.57f,.51f));mark(1,7,"H",new Color(.22f,.57f,.51f));
        }
    }
}
