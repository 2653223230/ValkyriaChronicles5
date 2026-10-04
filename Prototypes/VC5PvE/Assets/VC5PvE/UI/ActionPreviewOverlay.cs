using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace VC5PvE
{
    public sealed class ActionPreviewOverlay : MonoBehaviour
    {
        private RectTransform root;private BoardView board;private Camera cameraView;private Font font;
        private PreviewGeometry graphic;private readonly List<GameObject> labels=new List<GameObject>();
        private readonly List<PreviewGeometry.Segment> ranges=new List<PreviewGeometry.Segment>();
        public void Initialize(RectTransform canvas,BoardView b,Camera c,Font f)
        {
            root=BattleHud.Rect(canvas,"Live result overlay",0,0,1920,1080);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var g=new GameObject("Preview paths and ranges",typeof(RectTransform),typeof(CanvasRenderer),typeof(PreviewGeometry));
            g.transform.SetParent(root,false);graphic=g.GetComponent<PreviewGeometry>();var r=graphic.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;graphic.raycastTarget=false;
            board=b;cameraView=c;font=f;
        }
        private Vector2 Point(Vector3 world)
        {Vector2 p;RectTransformUtility.ScreenPointToLocalPointInRectangle(graphic.rectTransform,cameraView.WorldToScreenPoint(world),null,out p);return p;}
        private void Edge(List<PreviewGeometry.Segment> list,Vector3 a,Vector3 b,Color color,float width=2)
        {list.Add(new PreviewGeometry.Segment{A=Point(a),B=Point(b),Color=color,Width=width});}
        private void Cell(List<PreviewGeometry.Segment> list,GridPos cell,Color color,float size=.46f,float width=2)
        {
            var p=board.World(cell)+Vector3.up*.08f;var q=new[]{p+new Vector3(-size,0,-size),p+new Vector3(size,0,-size),p+new Vector3(size,0,size),p+new Vector3(-size,0,size)};
            for(int i=0;i<4;i++)Edge(list,q[i],q[(i+1)%4],color,width);
        }
        public void ShowRange(BattleState state,UnitState actor,ActionKind action,CardKind? card)
        {
            ranges.Clear();if(actor!=null)
            {
                int radius=action==ActionKind.Heal?2:actor.AttackRange;
                if(action==ActionKind.Move||card==CardKind.Advance||card==CardKind.Cover||card==CardKind.Charge)radius=0;
                if(card==CardKind.SparkMark||card==CardKind.StarBurst)radius=3;
                if(card==CardKind.Inspire)radius=2;
                for(int y=0;y<state.Height;y++)for(int x=0;x<state.Width;x++)
                {var p=new GridPos(x,y);if(radius>0&&p.ManhattanDistance(actor.Position)<=radius)Cell(ranges,p,new Color(.98f,.84f,.42f,.42f),.48f,1.5f);}
            }
            ClearResult();
        }
        public void ClearResult()
        {
            if(board!=null)board.PreviewAp(null,0);
            foreach(var go in labels)if(go!=null){go.SetActive(false);Destroy(go);}labels.Clear();
            if(graphic!=null){graphic.Lines.Clear();graphic.Lines.AddRange(ranges);graphic.SetVerticesDirty();}
        }
        public void Hide(){ranges.Clear();ClearResult();}
        public void Show(BattleState before,ActionPlan plan,BattleState after)
        {
            ClearResult();if(plan==null||!plan.IsValid||after==null)return;
            var actor=before.FindUnit(plan.Request.ActorId);if(actor==null)return;
            board.PreviewAp(actor.Id,Mathf.Max(0,actor.Ap-after.FindUnit(actor.Id).Ap));
            var a=board.World(actor.Position)+Vector3.up*.16f;
            foreach(var cell in plan.Path){var b=board.World(cell)+Vector3.up*.16f;Edge(graphic.Lines,a,b,new Color(.35f,1f,.88f),4);a=b;}
            if(plan.Path.Count>0)
            {
                var dest=plan.Path[plan.Path.Count-1];Cell(graphic.Lines,dest,new Color(1f,.85f,.35f),.40f,4);
                Ghost(actor,dest);Tag(board.World(dest)+Vector3.up*1.9f,"落点 "+BattleHud.Coord(dest),BattleHud.Gold);
                // Movement decisions also show the attack reach from the proposed landing.
                for(int y=0;y<before.Height;y++)for(int x=0;x<before.Width;x++){var cell=new GridPos(x,y);if(cell.ManhattanDistance(dest)<=actor.AttackRange)Cell(graphic.Lines,cell,new Color(1f,.85f,.35f,.55f),.43f,1.4f);}
            }
            foreach(var unit in before.Units)
            {
                var next=after.FindUnit(unit.Id);if(next==null)continue;string text="";
                if(unit.Hp!=next.Hp)text="HP "+unit.Hp+" → "+Mathf.Max(0,next.Hp)+(next.Hp<=0?"  击败":"");
                if(unit.Shield!=next.Shield)text+=(text.Length>0?"\n":"")+"护盾 "+unit.Shield+" → "+next.Shield;
                if(unit.MarkExpiresRound!=next.MarkExpiresRound)text+=(text.Length>0?"\n":"")+(next.MarkExpiresRound>0?"施加印记":"消耗印记");
                if(unit.InspireExpiresRound!=next.InspireExpiresRound)text+=(text.Length>0?"\n":"")+(next.InspireExpiresRound>0?"鼓舞 +1":"消耗鼓舞");
                if(text.Length>0)Tag(board.World(unit.Position)+Vector3.up*2.1f,text,next.Hp<unit.Hp?new Color(1f,.63f,.48f):BattleHud.Paper);
                if(unit.Position!=next.Position&&unit.Id!=actor.Id)
                {Edge(graphic.Lines,board.World(unit.Position)+Vector3.up*.22f,board.World(next.Position)+Vector3.up*.22f,new Color(1f,.66f,.26f),5);Cell(graphic.Lines,next.Position,new Color(1f,.66f,.26f),.35f,3);Tag(board.World(next.Position)+Vector3.up*.5f,"击退落点",BattleHud.Gold);}
            }
            graphic.SetVerticesDirty();
        }
        private void Tag(Vector3 world,string text,Color color)
        {
            Vector2 p;RectTransformUtility.ScreenPointToLocalPointInRectangle(root,cameraView.WorldToScreenPoint(world),null,out p);
            float x=Mathf.Clamp(p.x-root.rect.xMin+20,335,1328),y=Mathf.Clamp(root.rect.yMax-p.y-22,155,720);
            int count=text.Split('\n').Length;var panel=BattleHud.Panel(root,"Predicted result",x,y,154,10+count*23,new Color(.02f,.08f,.10f,.92f));
            panel.GetComponent<Image>().raycastTarget=false;var label=BattleHud.Label(panel,"Result",7,4,140,count*23,18,color,font);label.text=text;label.verticalOverflow=VerticalWrapMode.Overflow;labels.Add(panel.gameObject);
        }
        private void Ghost(UnitState actor,GridPos dest)
        {
            if(board.UnitSprites==null||(int)actor.Role>=board.UnitSprites.Length)return;
            Vector2 p;RectTransformUtility.ScreenPointToLocalPointInRectangle(root,cameraView.WorldToScreenPoint(board.World(dest)),null,out p);
            var r=BattleHud.Rect(root,"Landing silhouette",p.x-root.rect.xMin-38,root.rect.yMax-p.y-118,76,118);
            var i=r.gameObject.AddComponent<Image>();i.sprite=board.UnitSprites[(int)actor.Role];i.preserveAspect=true;i.raycastTarget=false;i.color=new Color(.6f,1f,.95f,.48f);labels.Add(r.gameObject);
        }
        private void OnDisable(){if(graphic!=null)Hide();}
    }
    public sealed class PreviewGeometry : MaskableGraphic
    {
        public struct Segment{public Vector2 A,B;public Color Color;public float Width;}
        public readonly List<Segment> Lines=new List<Segment>();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();foreach(var s in Lines)
            {var d=(s.B-s.A).normalized;var n=new Vector2(-d.y,d.x)*s.Width*.5f;int v=vh.currentVertCount;vh.AddVert(s.A+n,s.Color,Vector2.zero);vh.AddVert(s.B+n,s.Color,Vector2.zero);vh.AddVert(s.B-n,s.Color,Vector2.zero);vh.AddVert(s.A-n,s.Color,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
        }
    }
}
