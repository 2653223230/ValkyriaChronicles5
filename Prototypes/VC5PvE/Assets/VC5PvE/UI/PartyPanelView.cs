using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE
{
    public sealed class PartyPanelView : MonoBehaviour
    {
        public Action<string> SelectUnit;
        private readonly List<Row> rows = new List<Row>();
        private Sprite[] sprites;
        private sealed class Row
        {
            public RectTransform root, hp; public Image portrait, backing, stripe;
            public Text name, role, health, exhausted; public Button button;
            public readonly List<GameObject> ap = new List<GameObject>();
            public Image[] borders; public string id;
        }
        public void Build(RectTransform parent, Font font)
        {
            transform.SetParent(parent,false);
            var panel=BattleHud.Panel(transform,"Party panel",26,160,300,470,new Color(.047f,.145f,.173f,.97f));
            // The component root must share the reference canvas coordinate system.
            var own=(RectTransform)transform;own.anchorMin=Vector2.zero;own.anchorMax=Vector2.one;own.offsetMin=own.offsetMax=Vector2.zero;
            BattleHud.Label(panel,"Party title",18,14,260,27,20,BattleHud.Gold,font).text="出战小队";
            for(int i=0;i<3;i++)
            {
                var r=new Row(); r.root=BattleHud.Panel(panel,"Party row "+i,10,52+i*134,280,124,new Color(.082f,.188f,.224f),BattleHud.Jade);
                r.backing=r.root.GetComponent<Image>();r.borders=new Image[4];
                string[] names={"Top border","Bottom border","Left border","Right border"};
                for(int j=0;j<4;j++){r.borders[j]=r.root.Find(names[j]).GetComponent<Image>();r.borders[j].raycastTarget=false;}
                r.stripe=BattleHud.Panel(r.root,"Selected stripe",0,12,4,98,BattleHud.Gold).GetComponent<Image>();r.stripe.raycastTarget=false;
                var crop=BattleHud.Panel(r.root,"Portrait crop",10,10,78,98,new Color(.14f,.28f,.32f));crop.gameObject.AddComponent<RectMask2D>();
                r.portrait=BattleHud.Rect(crop,"Portrait",-43,-20,164,246).gameObject.AddComponent<Image>();r.portrait.preserveAspect=true;r.portrait.raycastTarget=false;
                r.name=BattleHud.Label(r.root,"Name",101,12,165,28,22,BattleHud.Paper,font);
                r.role=BattleHud.Label(r.root,"Role and status",102,42,166,20,14,BattleHud.Jade,font);
                BattleHud.Panel(r.root,"HP track",101,75,163,16,new Color(.025f,.10f,.13f));
                r.hp=BattleHud.Panel(r.root,"HP fill",103,77,159,12,new Color(.416f,.898f,.639f));
                r.health=BattleHud.Label(r.root,"HP value",210,58,54,16,12,BattleHud.Paper,font);r.health.alignment=TextAnchor.MiddleRight;
                BattleHud.Label(r.root,"AP label",102,97,28,21,13,BattleHud.Jade,font).text="AP";
                r.exhausted=BattleHud.Label(r.root,"No AP",139,96,125,22,14,new Color(.50f,.62f,.61f),font);
                for(int j=0;j<8;j++)
                {
                    var diamond=BattleHud.Panel(r.root,"AP "+(j+1),139+j*27,100,17,17,new Color(.027f,.216f,.329f));
                    diamond.pivot=new Vector2(.5f,.5f);diamond.anchoredPosition=new Vector2(147+j*27,-105);diamond.localRotation=Quaternion.Euler(0,0,45);
                    BattleHud.Panel(diamond,"Blue core",3,3,11,11,new Color(.208f,.812f,1f));r.ap.Add(diamond.gameObject);
                }
                r.button=r.root.gameObject.AddComponent<Button>();r.button.targetGraphic=r.backing;r.button.onClick.AddListener(()=>SelectUnit?.Invoke(r.id));
                foreach(var g in r.root.GetComponentsInChildren<Graphic>())g.raycastTarget=g==r.backing;
                foreach(var t in r.root.GetComponentsInChildren<Text>())t.verticalOverflow=VerticalWrapMode.Overflow;
                rows.Add(r);
            }
            foreach(var t in panel.GetComponentsInChildren<Text>())t.verticalOverflow=VerticalWrapMode.Overflow;
        }
        public void Configure(Sprite[] value){sprites=value;}
        public string UnitAtScreen(Vector2 screenPosition)
        {
            var canvas=GetComponentInParent<Canvas>();
            Camera camera=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            foreach(var row in rows)
                if(!string.IsNullOrEmpty(row.id) && row.button!=null && row.button.interactable && row.root.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(row.root,screenPosition,camera))return row.id;
            return null;
        }
        public void Render(BattleState state,UnitState selected,bool busy)
        {
            int i=0;foreach(var u in state.Units)
            {
                if(u.Team!=Team.Player || i>=rows.Count)continue;var r=rows[i++];r.id=u.Id;
                bool chosen=selected!=null && selected.Id==u.Id;bool alive=u.IsAlive;
                r.root.name="Party row "+u.Id;r.name.text=BattleHud.Name(u.Role);r.name.color=chosen?BattleHud.Gold:BattleHud.Paper;
                r.backing.color=chosen?new Color(.14f,.26f,.245f):new Color(.082f,.188f,.224f);
                r.stripe.gameObject.SetActive(chosen);foreach(var b in r.borders)b.color=chosen?BattleHud.Gold:new Color(.255f,.396f,.42f);
                r.role.text=!alive?"失去战斗能力":u.Shield>0?"护盾 "+u.Shield:u.InspireExpiresRound>0?"鼓舞 · 下次伤害+1":Definitions.PartyDescription(u.Role);
                r.health.text=Mathf.Max(0,u.Hp)+" / "+u.MaxHp;
                float ratio=u.MaxHp>0?Mathf.Clamp01((float)u.Hp/u.MaxHp):0;r.hp.sizeDelta=new Vector2(159*ratio,12);
                r.hp.GetComponent<Image>().color=HealthBarColor.Evaluate(ratio);
                for(int j=0;j<r.ap.Count;j++)r.ap[j].SetActive(alive && j<u.Ap);
                r.exhausted.text=!alive?"已倒下":u.Ap<=0?"行动耗尽":"";
                r.portrait.color=alive?Color.white:new Color(.42f,.42f,.42f,.7f);
                if(sprites!=null && (int)u.Role<sprites.Length)r.portrait.sprite=sprites[(int)u.Role];
                r.button.interactable=alive && !busy && (state.Phase==BattlePhase.Player||state.Phase==BattlePhase.Deployment);
            }
        }
    }
}
