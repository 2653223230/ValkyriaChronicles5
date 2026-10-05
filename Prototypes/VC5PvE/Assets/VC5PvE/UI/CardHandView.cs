using System;
using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE
{
    public sealed class CardHandView : MonoBehaviour
    {
        public Action<string> Selected;
        public Action<string,Vector2> Dropped;
        public Action<string> DragStarted;
        public Action<string> DragCancelled;
        public Func<Vector2,bool> CanDrop;
        public Action<CardDefinition> Hovered;
        public Action HoverExited;
        public Font Font;
        private RectTransform root;
        private Sprite[] unitSprites;

        public void Build(RectTransform parent, Font font, Sprite[] portraits = null) { root=parent;Font=font;unitSprites=portraits; }
        public void Render(BattleState state, string selectedId, bool enabled, string selectedUnitId = null)
        {
            foreach(Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            for(int i=0;i<state.Hand.Count;i++)
            {
                var instance=state.Hand[i]; var definition=Definitions.Card(instance.Kind); string id=instance.Id;
                var color=definition.AllowedRole.HasValue ? new Color(.045f,.14f,.17f,.96f):new Color(.04f,.115f,.13f,.96f);
                var r=BattleHud.Panel(root,definition.Name,i*248,0,232,212,color,selectedId==id?BattleHud.Gold:BattleHud.Jade);
                var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.interactable=enabled;
                button.onClick.AddListener(()=>Selected?.Invoke(id));
                var title=BattleHud.Label(r,"Card name",57,7,definition.AllowedRole.HasValue?122:170,46,25,BattleHud.Gold,Font); title.text=definition.Name;
                BuildApBadge(r, definition.Cost);
                if(definition.AllowedRole.HasValue)BuildUserPortrait(r,definition.AllowedRole.Value);
                var role=BattleHud.Label(r,"Executor",14,51,205,33,18,BattleHud.Jade,Font);
                role.text=definition.UserLabel;
                BattleHud.Panel(r,"Card separator",14,81,204,1,new Color(.34f,.49f,.42f,.6f));
                var copy=BattleHud.Label(r,"Card effect",14,96,204,108,18,BattleHud.Paper,Font);copy.text=definition.Description;copy.lineSpacing=.85f;
                var drag=r.gameObject.AddComponent<CardDragInput>();drag.CardId=id;drag.Enabled=enabled;drag.SetSelected(selectedId==id);
                drag.SetUsable(selectedUnitId!=null && CardAimResolver.CanExecute(state,id,selectedUnitId));
                drag.Dropped=(card,p)=>Dropped?.Invoke(card,p);drag.Hovered=()=>Hovered?.Invoke(definition);
                drag.DragStarted=card=>DragStarted?.Invoke(card);drag.DragCancelled=card=>DragCancelled?.Invoke(card);drag.CanDrop=p=>CanDrop==null || CanDrop(p);
                drag.HoverExited=()=>HoverExited?.Invoke();
            }
            var cg=root.GetComponent<CanvasGroup>();
            if(cg==null) cg=root.gameObject.AddComponent<CanvasGroup>();
            cg.interactable=enabled;cg.alpha=enabled?1f:.35f;cg.blocksRaycasts=enabled;
        }
        public void SetSelection(string cardId)
        {
            if(root==null)return;
            foreach(Transform child in root)
            {
                var drag=child.GetComponent<CardDragInput>();
                // Retired cards await Destroy at end of frame; they must not regain selection.
                if(drag!=null && child.gameObject.activeInHierarchy)drag.SetSelected(drag.CardId==cardId);
            }
        }
        public static string Role(UnitRole role)
        { return Definitions.UnitName(role); }
        private void BuildUserPortrait(RectTransform card, UnitRole role)
        {
            int index=(int)role;
            if(unitSprites==null || index<0 || index>=unitSprites.Length || unitSprites[index]==null)return;
            var crop=BattleHud.Panel(card,"User portrait",182,7,38,48,new Color(.14f,.28f,.32f));
            crop.gameObject.AddComponent<RectMask2D>();
            // Same source and framing as the approved PartyPanelView portrait, scaled to card size.
            const float scale=38f/78f;
            var portrait=BattleHud.Rect(crop,"Portrait",-43*scale,-20*scale,164*scale,246*scale).gameObject.AddComponent<Image>();
            portrait.sprite=unitSprites[index];portrait.preserveAspect=true;
            Color gold=new Color(.725f,.651f,.42f);
            BattleHud.Panel(crop,"Portrait top border",0,0,38,1,gold);
            BattleHud.Panel(crop,"Portrait bottom border",0,47,38,1,gold);
            BattleHud.Panel(crop,"Portrait left border",0,0,1,48,gold);
            BattleHud.Panel(crop,"Portrait right border",37,0,1,48,gold);
            foreach(var graphic in crop.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
        }
        private void BuildApBadge(RectTransform card, int amount)
        {
            var edge = BattleHud.Panel(card,"AP diamond frame",5,-6,38,38,new Color(.035f,.20f,.31f));
            edge.pivot = new Vector2(.5f,.5f); edge.anchoredPosition = new Vector2(24,-13);
            edge.localRotation = Quaternion.Euler(0,0,45);
            edge.GetComponent<Image>().raycastTarget = false;
            var blue = BattleHud.Panel(edge,"AP blue enamel",3,3,32,32,new Color(.08f,.72f,.94f));
            blue.GetComponent<Image>().raycastTarget = false;
            var shine = BattleHud.Panel(blue,"AP rim glint",1,1,30,2,new Color(.65f,.95f,1f));
            shine.GetComponent<Image>().raycastTarget = false;
            var number = BattleHud.Label(card,"AP cost",3,-11,42,48,27,Color.white,Font);
            number.text = amount.ToString(); number.alignment = TextAnchor.MiddleCenter;
            number.verticalOverflow = VerticalWrapMode.Overflow; number.raycastTarget = false;
            var shadow = number.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.02f,.22f,.36f,.9f); shadow.effectDistance = new Vector2(0,-1);
        }
    }
}
