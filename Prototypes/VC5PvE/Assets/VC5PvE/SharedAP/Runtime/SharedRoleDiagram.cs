using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE.SharedAP
{
    /// <summary>Non-interactive shared role and range diagrams sized for a four-cell weapon reach.</summary>
    public sealed class SharedRoleDiagram : MonoBehaviour
    {
        RectTransform root;
        Font font;
        Text heading, weapon, skill, footer, secondaryTitle;
        readonly Text[] beforeLabels=new Text[9],afterLabels=new Text[9];
        readonly Image[] cells=new Image[81];
        readonly Image[] beforeCells=new Image[9];
        readonly Image[] afterCells=new Image[9];
        const int N=9, Cell=16, Gap=3;
        public void Build(RectTransform parent,Font uiFont)
        {
            font=uiFont;root=BattleHud.Panel(parent,"Role diagram",24,410,330,520,SharedPaper.Paper);
            heading=BattleHud.Label(root,"Role and weapon",14,10,312,30,20,SharedPaper.Green,font);
            weapon=BattleHud.Label(root,"Weapon facts",14,41,312,39,17,SharedPaper.Ink,font);weapon.verticalOverflow=VerticalWrapMode.Overflow;
            for(int i=0;i<cells.Length;i++)
            {
                int x=i%N,y=i/N;
                var r=BattleHud.Panel(root,"Range cell "+i,14+x*(Cell+Gap),86+y*(Cell+Gap),Cell,Cell,new Color(.83f,.86f,.78f));
                cells[i]=r.GetComponent<Image>();DisableRaycasts(r);
            }
            var key=BattleHud.Label(root,"Range key",197,91,130,110,16,SharedPaper.Green,font);
            key.text="青：自己\n绿：攻击范围\n金：示例目标\n示意不含障碍";key.verticalOverflow=VerticalWrapMode.Overflow;
            skill=BattleHud.Label(root,"Skill illustration",14,258,312,84,17,SharedPaper.Ink,font);skill.verticalOverflow=VerticalWrapMode.Overflow;
            secondaryTitle=BattleHud.Label(root,"Before and after",14,349,312,26,17,SharedPaper.Green,font);
            for(int i=0;i<beforeCells.Length;i++)
            {
                var a=BattleHud.Panel(root,"Skill before cell "+i,18+(i%3)*22,385+(i/3)*22,20,20,new Color(.83f,.86f,.78f));
                beforeCells[i]=a.GetComponent<Image>();beforeLabels[i]=BattleHud.Label(a,"Before occupant",0,0,20,20,13,BattleHud.Ink,font);DisableRaycasts(a);
                var b=BattleHud.Panel(root,"Skill after cell "+i,230+(i%3)*22,385+(i/3)*22,20,20,new Color(.83f,.86f,.78f));
                afterCells[i]=b.GetComponent<Image>();afterLabels[i]=BattleHud.Label(b,"After occupant",0,0,20,20,13,BattleHud.Ink,font);DisableRaycasts(b);
            }
            footer=BattleHud.Label(root,"Skill diagram caption",14,457,312,60,16,SharedPaper.Green,font);footer.verticalOverflow=VerticalWrapMode.Overflow;
            foreach(var text in root.GetComponentsInChildren<Text>())text.raycastTarget=false;
        }
        public void Render(SUnit unit,SState state)
        {
            if(root==null)return;
            bool has=unit!=null;root.gameObject.SetActive(has);if(!has)return;
            heading.text=unit.Name+" · "+(unit.Role==SRole.Vanguard?"前排肉盾／突进近战":unit.Role==SRole.Archer?"远程单体":"远程范围");
            weapon.text=unit.Role==SRole.Vanguard?"武器 3伤 · 射程1 · 单体":unit.Role==SRole.Archer?
                (unit.Braced?"武器 4伤 · 射程4 · 单体\n移动解除架弩。":"武器 2伤 · 射程3 · 单体\n架弩后4伤、射程4，移动解除。"):
                "武器 1伤 · 射程2\n主目标与四邻可见敌人各1伤。";
            skill.text=unit.Role==SRole.Vanguard?"冲撞 · 打出战技牌，消耗 1 AP\n推邻敌1格，自己护盾至少2。\n推不开仍得盾；炮座不移动。":unit.Role==SRole.Archer?"架弩 · 打出战技牌，消耗 1 AP\n下一击主目标＋2伤、射程＋1。\n移动解除；至下轮行动结束。":"星火爆裂 · 消耗 1 AP\n点 2 格内的敌人，目标及四邻\n可见敌人各受 2 伤。友军安全。";
            secondaryTitle.text="施放前                       →              施放后";
            Color idle=new Color(.83f,.86f,.78f),range=new Color(.24f,.80f,.72f),center=new Color(.94f,.75f,.34f),effect=new Color(.98f,.47f,.26f),friendly=new Color(.27f,.70f,.96f);
            foreach(var image in cells)image.color=idle;
            int cx=4,cy=4;cells[cy*N+cx].color=friendly;
            int reach=SharedRules.WeaponRange(unit);
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                int dist=Mathf.Abs(x-cx)+Mathf.Abs(y-cy);
                if(dist>0&&dist<=reach)cells[y*N+x].color=range;
            }
            cells[cy*N+cx+1].color=center;
            foreach(var image in beforeCells)image.color=idle;
            foreach(var image in afterCells)image.color=idle;
            foreach(var label in beforeLabels)label.text="";foreach(var label in afterLabels)label.text="";
            if(unit.Role==SRole.Vanguard)
            {
                beforeCells[3].color=friendly;beforeCells[4].color=center;beforeCells[5].color=new Color(.68f,.77f,.76f);
                afterCells[3].color=friendly;afterCells[5].color=effect;beforeLabels[3].text="己";beforeLabels[4].text="敌";beforeLabels[5].text="空";afterLabels[3].text="己";afterLabels[5].text="敌";
                footer.text="邻敌向后推1格；自己护盾补至至少2。";
            }
            else if(unit.Role==SRole.Archer)
            {
                beforeCells[3].color=range;beforeCells[5].color=range;beforeCells[4].color=center;
                afterCells[1].color=range;afterCells[2].color=range;afterCells[3].color=range;afterCells[5].color=center;
                beforeLabels[4].text="己";afterLabels[4].text="己";afterCells[4].color=friendly;
                footer.text="2伤 / 射程3 → 4伤 / 射程4\n移动会解除架弩。";
            }
            else
            {
                foreach(int i in new[]{1,3,4,5,7})afterCells[i].color=range;
                foreach(int i in new[]{1,3,4})
                {
                    beforeCells[i].color=center;beforeLabels[i].text="敌";
                    afterCells[i].color=effect;afterLabels[i].text="-2";
                }
                beforeCells[5].color=afterCells[5].color=friendly;
                beforeLabels[5].text=afterLabels[5].text="己";
                footer.text="点中心弩手，一次命中三名敌兵。";

            }
        }
        static string RoleName(SRole role)=>role==SRole.Vanguard?"先锋":role==SRole.Archer?"射手":"术师";
        static void DisableRaycasts(RectTransform rect)
        {foreach(var graphic in rect.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;}
    }
}
