using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE.SharedAP
{
    /// <summary>Live anchors only: screen projections are recalculated after UI layout and billboard animation.</summary>
    [DefaultExecutionOrder(250)]
    public sealed class SharedFocusOverlay : MonoBehaviour
    {
        SharedBattleController flow;
        SharedHud hud;
        RectTransform canvasRoot, root, panel;
        SharedFocusGraphic graphic;
        Image modalBlocker;
        Text title, body;
        Button primary, secondary;
        readonly List<Rect> screenTargets=new List<Rect>();
        readonly List<Rect> targets=new List<Rect>(), holes=new List<Rect>();
        readonly List<Vector2[]> polygons=new List<Vector2[]>();
        readonly List<RectTransform> dragLabels=new List<RectTransform>();
        FocusTutorialStep previousStep;
        float missingSince=-1;
        public IReadOnlyList<Rect> FocusTargets => screenTargets;

        public void Build(SharedBattleController controller,SharedHud view,Font font)
        {
            flow=controller;hud=view;canvasRoot=(RectTransform)transform;
            var go=new GameObject("Focused tutorial",typeof(RectTransform));root=(RectTransform)go.transform;
            root.SetParent(canvasRoot,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var shade=new GameObject("Dynamic target outlines and light mask",typeof(RectTransform),typeof(CanvasRenderer));shade.transform.SetParent(root,false);
            var shadeRect=(RectTransform)shade.transform;shadeRect.anchorMin=Vector2.zero;shadeRect.anchorMax=Vector2.one;shadeRect.offsetMin=shadeRect.offsetMax=Vector2.zero;
            graphic=shade.AddComponent<SharedFocusGraphic>();graphic.raycastTarget=false;
            var blocker=new GameObject("Tutorial modal input blocker",typeof(RectTransform),typeof(Image));blocker.transform.SetParent(root,false);
            var blockRect=(RectTransform)blocker.transform;blockRect.anchorMin=Vector2.zero;blockRect.anchorMax=Vector2.one;blockRect.offsetMin=blockRect.offsetMax=Vector2.zero;
            modalBlocker=blocker.GetComponent<Image>();modalBlocker.color=Color.clear;modalBlocker.raycastTarget=true;
            panel=BattleHud.Panel(root,"Focused instruction",0,0,660,200,SharedPaper.Paper,SharedPaper.Gold);
            title=BattleHud.Label(panel,"Instruction title",22,18,616,38,24,SharedPaper.Ink,font);
            body=BattleHud.Label(panel,"Instruction body",22,62,616,98,19,SharedPaper.Green,font);
            title.raycastTarget=false;body.raycastTarget=false;
            primary=BattleHud.ButtonAt(panel,"Tutorial primary",22,182,616,44,font,Primary,true);
            secondary=BattleHud.ButtonAt(panel,"Tutorial secondary",22,182,292,44,font,Secondary);
            primary.targetGraphic.color=SharedPaper.Green;secondary.targetGraphic.color=SharedPaper.Green;
            primary.GetComponentInChildren<Text>().color=SharedPaper.Paper;secondary.GetComponentInChildren<Text>().color=SharedPaper.Paper;
            primary.GetComponentInChildren<Text>().fontSize=20;secondary.GetComponentInChildren<Text>().fontSize=20;
            for(int i=0;i<2;i++)
            {
                var label=BattleHud.Panel(root,"Drag instruction "+i,0,0,130,28,SharedPaper.Green);
                BattleHud.Label(label,"Caption",8,2,116,24,16,SharedPaper.Paper,font).text=i==0?"按住":"拖到这里";
                foreach(var g in label.GetComponentsInChildren<Graphic>())g.raycastTarget=false;
                dragLabels.Add(label);
            }
            root.gameObject.SetActive(false);
        }

        void Primary()
        {
            if(flow.FocusRestartConfirm)flow.ConfirmRestartFocusTutorial();
            else if(flow.FocusExitOpen||!string.IsNullOrEmpty(flow.FocusRecoveryReason))flow.ConfirmSkipTutorial();
            else flow.ContinueFocusTutorial();
        }
        void Secondary()
        {
            if(!string.IsNullOrEmpty(flow.FocusRecoveryReason)&&!flow.FocusRestartConfirm)flow.RequestRestartFocusTutorial();
            else flow.ResumeFocusTutorial();
        }

        void LateUpdate()
        {
            if(root==null||flow==null||flow.State==null)return;
            bool active=flow.FocusTutorialActive&&flow.State.Phase!=SPhase.Won&&flow.State.Phase!=SPhase.Lost;
            root.gameObject.SetActive(active);screenTargets.Clear();if(!active)return;
            root.SetAsLastSibling();targets.Clear();holes.Clear();polygons.Clear();
            foreach(var label in dragLabels)label.gameObject.SetActive(false);
            bool modal=flow.FocusExitOpen||flow.FocusRestartConfirm||!string.IsNullOrEmpty(flow.FocusRecoveryReason);
            modalBlocker.gameObject.SetActive(modal);
            if(previousStep!=flow.FocusStep){missingSince=-1;previousStep=flow.FocusStep;}
            bool anchors=true;
            if(!modal&&(!flow.Busy||flow.FocusStep==FocusTutorialStep.Enemy))anchors=CollectTargets();
            if(!anchors&&!flow.Busy&&!modal)
            {
                if(missingSince<0)missingSince=Time.unscaledTime;
                // A hand rebuild may remove an old object for one frame; do not mistake that for a lost target.
                if(Time.unscaledTime-missingSince>.65f)flow.ReportFocusAnchorMissing("当前操作目标未显示，无法准确定位。");
            }
            else missingSince=-1;
            AddReadable("role");AddReadable("ap");AddReadable("skip");
            if(flow.Stage==SharedInputStage.Landing||flow.Stage==SharedInputStage.Target||flow.Stage==SharedInputStage.Retain)AddReadable("cancel");
            if(flow.IsDragging||flow.FocusStep==FocusTutorialStep.MoveTarget||flow.FocusStep==FocusTutorialStep.SkillTarget)
            {
                // Keep the actual range and damage preview visible, not just the pawn silhouette.
                var boardPoints=new List<Vector2>();
                foreach(var corner in new[]{new SPos(0,0),new SPos(0,6),new SPos(6,0),new SPos(6,6)})
                {Vector2[] p;if(TryGetCellPolygon(corner,out p))boardPoints.AddRange(p.Select(ToCanvas));}
                if(boardPoints.Count>0){var r=Bounds(boardPoints.ToArray());r.yMin-=150;holes.Add(r);}
            }
            UpdatePrompt(modal,anchors);
            float blink=Time.unscaledTime-flow.FocusRejectedAt;
            graphic.SetGeometry(holes,targets,polygons,blink>=0&&blink<.7f?Color.Lerp(SharedPaper.Gold,Color.white,Mathf.PingPong(blink*7f,1f)):SharedPaper.Gold);
        }

        bool CollectTargets()
        {
            switch(flow.FocusStep)
            {
                case FocusTutorialStep.SelectMage:return AddUi("party:mage");
                case FocusTutorialStep.ReadInfo:return AddUi("role")&AddUi("ap");
                case FocusTutorialStep.ShortStep:return AddCell(new SPos(4,5));
                case FocusTutorialStep.DragMove:return AddDrag(SCard.Move);
                case FocusTutorialStep.MoveTarget:return AddCell(new SPos(4,3));
                case FocusTutorialStep.DragSkill:return AddDrag(SCard.Skill);
                case FocusTutorialStep.SkillTarget:return AddUnit("crossbow")&AddCell(new SPos(3,3));
                case FocusTutorialStep.SelectArcher:return AddUnit("archer");
                case FocusTutorialStep.BasicAttack:return AddUnit("patrol")&AddCell(new SPos(2,3));
                case FocusTutorialStep.DragShield:return AddDrag(SCard.Shield);
                case FocusTutorialStep.EndTurn:return AddUi("confirm");
                case FocusTutorialStep.Retain:
                    bool ok=AddUi("confirm");foreach(var c in flow.State.Hand)ok=AddUi("card:"+c.Id)&ok;return ok;
                case FocusTutorialStep.Enemy:
                    if(!string.IsNullOrEmpty(flow.FocusEnemyActorId)&&flow.FocusEnemyActorId!="cannon")return AddUnit(flow.FocusEnemyActorId);
                    foreach(var p in flow.State.Telegraph)AddCell(p);return true;
                case FocusTutorialStep.Ready:
                    bool ready=AddUi("ap");foreach(var c in flow.State.Hand)ready=AddUi("card:"+c.Id)&ready;return ready;
                default:return true;
            }
        }

        bool AddDrag(SCard kind)
        {
            var card=flow.State.Hand.FirstOrDefault(c=>c.Kind==kind&&flow.FocusAllowsCard(c.Id));
            if(card==null)return false;
            if(flow.IsDragging&&flow.PreviewExecutorId=="mage")return AddUnit("mage");
            bool valid=AddUi("card:"+card.Id)&AddUnit("mage");
            if(valid&&targets.Count>=2)
                for(int i=0;i<2;i++)
                {
                    var label=dragLabels[i];var r=targets[i];label.gameObject.SetActive(true);
                    label.anchoredPosition=new Vector2(Mathf.Clamp(r.xMin,8,canvasRoot.rect.width-138),-Mathf.Max(8,r.yMin-34));
                }
            return valid;
        }

        bool AddUi(string key)
        {
            Rect r;if(!hud.TryGetFocusUiRect(key,out r))return false;AddScreenRect(r);return true;
        }
        void AddReadable(string key)
        {
            Rect r;if(hud.TryGetFocusUiRect(key,out r))holes.Add(ToCanvas(r));
        }
        bool AddUnit(string id)
        {
            Rect r;if(!TryGetUnitRect(id,out r))return false;AddScreenRect(r);return true;
        }
        void AddScreenRect(Rect r)
        {
            screenTargets.Add(r);var c=ToCanvas(r);c.xMin-=4;c.xMax+=4;c.yMin-=4;c.yMax+=4;targets.Add(c);holes.Add(c);
        }
        bool AddCell(SPos pos)
        {
            Vector2[] points;if(!TryGetCellPolygon(pos,out points))return false;
            screenTargets.Add(Bounds(points));var canvas=points.Select(ToCanvas).ToArray();polygons.Add(canvas);holes.Add(Bounds(canvas));return true;
        }

        public bool TryGetCellPolygon(SPos pos,out Vector2[] points)
        {
            points=null;if(flow==null||flow.Board==null||flow.ViewCamera==null)return false;
            var center=flow.Board.World(new GridPos(pos.X,pos.Y))+Vector3.up*.085f;
            var offsets=new[]{new Vector3(-.45f,0,-.45f),new Vector3(-.45f,0,.45f),new Vector3(.45f,0,.45f),new Vector3(.45f,0,-.45f)};
            points=new Vector2[4];for(int i=0;i<4;i++){var p=flow.ViewCamera.WorldToScreenPoint(center+offsets[i]);if(p.z<=0)return false;points[i]=p;}
            return true;
        }
        public bool TryGetUnitRect(string id,out Rect screenRect)
        {
            screenRect=default(Rect);if(flow==null||flow.Board==null||flow.ViewCamera==null)return false;
            var view=flow.Board.GetComponentsInChildren<UnitView>().FirstOrDefault(v=>v.UnitId==id);
            if(view==null||flow.State.Find(id)?.Alive!=true)return false;
            var sprite=view.transform.Find("HD-2D unit billboard")?.GetComponent<SpriteRenderer>();
            if(sprite==null||sprite.sprite==null||!sprite.enabled)return false;
            // Same local sprite corners as UnitView.ContainsScreenPoint, including its current billboard transform.
            var b=sprite.sprite.bounds;var min=b.min;var max=b.max;
            var local=new[]{new Vector3(min.x,min.y),new Vector3(min.x,max.y),new Vector3(max.x,max.y),new Vector3(max.x,min.y)};
            var points=new Vector2[4];for(int i=0;i<4;i++){var p=flow.ViewCamera.WorldToScreenPoint(sprite.transform.TransformPoint(local[i]));if(p.z<=0)return false;points[i]=p;}
            screenRect=Bounds(points);return screenRect.width>1&&screenRect.height>1;
        }
        Vector2 ToCanvas(Vector2 screen)
        {
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot,screen,null,out local);
            return new Vector2(local.x-canvasRoot.rect.xMin,canvasRoot.rect.yMax-local.y);
        }
        Rect ToCanvas(Rect screen)
        {
            var tl=ToCanvas(new Vector2(screen.xMin,screen.yMax));var br=ToCanvas(new Vector2(screen.xMax,screen.yMin));
            return Rect.MinMaxRect(tl.x,tl.y,br.x,br.y);
        }
        static Rect Bounds(Vector2[] pts)
        {
            var min=pts[0];var max=pts[0];foreach(var p in pts){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        void UpdatePrompt(bool modal,bool anchors)
        {
            string heading=flow.FocusTitle,copy=flow.FocusBody;
            bool two=false,showPrimary=false;
            string primaryText="",secondaryText="";
            if(flow.FocusRestartConfirm)
            {heading="重新开始教学？";copy="本局已完成的行动将重置。\n确认后从固定开局重新教学。";two=showPrimary=true;primaryText="确认重新开始";secondaryText="返回";}
            else if(!string.IsNullOrEmpty(flow.FocusRecoveryReason))
            {heading="教学暂时无法继续";copy=flow.FocusRecoveryReason+"\n可以保留当前战局，自由行动。";two=showPrimary=true;primaryText="退出引导，继续本局";secondaryText="重新开始教学";}
            else if(flow.FocusExitOpen)
            {heading="退出引导，继续当前战局？";copy="已经完成的行动会保留。\n退出后，可以自由行动。";two=showPrimary=true;primaryText="退出引导，继续本局";secondaryText="继续教学";}
            else if(flow.Busy&&flow.FocusStep!=FocusTutorialStep.Enemy)
            {heading="正在执行，请稍候";copy="行动演出结束后，再进行下一步。";}
            else if(!anchors)
            {heading="正在定位操作目标";copy="稍候，提示框会跟随当前画面。";}
            else if(flow.FocusStep==FocusTutorialStep.ReadInfo)
            {showPrimary=true;primaryText="看看免费短步位置";}
            else if(flow.FocusStep==FocusTutorialStep.Ready)
            {showPrimary=true;primaryText="开始自由战斗";}
            float w=Mathf.Min(660,canvasRoot.rect.width-48),h=showPrimary?252:200;
            panel.sizeDelta=new Vector2(w,h);
            FitBorders(panel);
            title.rectTransform.sizeDelta=new Vector2(w-44,38);body.rectTransform.sizeDelta=new Vector2(w-44,104);
            title.text=heading;body.text=copy;
            primary.gameObject.SetActive(showPrimary);secondary.gameObject.SetActive(two);
            primary.GetComponentInChildren<Text>().text=primaryText;secondary.GetComponentInChildren<Text>().text=secondaryText;
            var pr=(RectTransform)primary.transform;var sr=(RectTransform)secondary.transform;
            pr.anchoredPosition=new Vector2(two?w*.5f+6:22,-(h-62));pr.sizeDelta=new Vector2(two?w*.5f-28:w-44,44);
            sr.anchoredPosition=new Vector2(22,-(h-62));sr.sizeDelta=new Vector2(w*.5f-28,44);
            FitBorders(pr);FitBorders(sr);
            primary.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(pr.sizeDelta.x-16,32);
            secondary.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(sr.sizeDelta.x-16,32);
            Rect chosen=PlacePrompt(w,h,modal);panel.anchoredPosition=new Vector2(chosen.x,-chosen.y);
        }

        static void FitBorders(RectTransform r)
        {
            var top=r.Find("Top border") as RectTransform;var bottom=r.Find("Bottom border") as RectTransform;
            var left=r.Find("Left border") as RectTransform;var right=r.Find("Right border") as RectTransform;
            if(top!=null)top.sizeDelta=new Vector2(r.sizeDelta.x,2);
            if(bottom!=null){bottom.sizeDelta=new Vector2(r.sizeDelta.x,2);bottom.anchoredPosition=new Vector2(0,-r.sizeDelta.y+2);}
            if(left!=null)left.sizeDelta=new Vector2(2,r.sizeDelta.y);
            if(right!=null){right.sizeDelta=new Vector2(2,r.sizeDelta.y);right.anchoredPosition=new Vector2(r.sizeDelta.x-2,0);}
        }

        Rect PlacePrompt(float w,float h,bool modal)
        {
            var size=canvasRoot.rect.size;
            if(modal)return new Rect((size.x-w)/2,(size.y-h)/2,w,h);
            var focus=targets.Count>0?targets[targets.Count-1]:polygons.Count>0?Bounds(polygons[0]):new Rect(size.x*.52f,size.y*.4f,1,1);
            var choices=new List<Vector2>{new Vector2(size.x-w-28,Mathf.Clamp(focus.y-50,140,size.y-h-270)),
                new Vector2(focus.xMax+24,focus.y),new Vector2(focus.x-w-24,focus.y),
                new Vector2(size.x-w-28,140),new Vector2(size.x-w-28,size.y-h-280)};
            Rect roleScreen;var avoid=new List<Rect>(targets);avoid.AddRange(polygons.Select(Bounds));
            if(hud.TryGetFocusUiRect("role",out roleScreen))avoid.Add(ToCanvas(roleScreen));
            foreach(var card in flow.State.Hand){Rect r;if(hud.TryGetFocusUiRect("card:"+card.Id,out r))avoid.Add(ToCanvas(r));}
            if(flow.FocusStep==FocusTutorialStep.ReadInfo||flow.FocusStep==FocusTutorialStep.SelectMage)
            {Rect row;if(hud.TryGetFocusUiRect(flow.FocusStep==FocusTutorialStep.ReadInfo?"role":"party:mage",out row)){var r=ToCanvas(row);choices.Insert(0,new Vector2(r.xMax+24,Mathf.Max(140,r.y)));}}
            if(flow.IsDragging)
            {var mouse=ToCanvas(Input.mousePosition);avoid.Add(new Rect(mouse.x-40,mouse.y-40,290,180));}
            float best=float.MaxValue;var picked=new Rect();
            for(int i=0;i<choices.Count;i++)
            {
                var v=choices[i];var r=new Rect(Mathf.Clamp(v.x,20,size.x-w-20),Mathf.Clamp(v.y,110,size.y-h-30),w,h);
                float score=i;foreach(var a in avoid)score+=Overlap(r,a)*10;
                if(score<best){best=score;picked=r;}
            }
            return picked;
        }
        static float Overlap(Rect a,Rect b)=>Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
    }

    /// <summary>A single non-raycasting mesh. Rectangular mask holes are unioned, avoiding darker overlap seams.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SharedFocusGraphic : MaskableGraphic
    {
        List<Rect> holes=new List<Rect>(),frames=new List<Rect>();List<Vector2[]> polygons=new List<Vector2[]>();Color lineColor;
        public void SetGeometry(List<Rect> clear,List<Rect> outline,List<Vector2[]> cells,Color stroke)
        {
            bool same=stroke==lineColor&&clear.SequenceEqual(holes)&&outline.SequenceEqual(frames)&&cells.Count==polygons.Count;
            if(same)for(int i=0;i<cells.Count;i++)if(!cells[i].SequenceEqual(polygons[i])){same=false;break;}
            if(same)return;
            holes.Clear();holes.AddRange(clear);frames.Clear();frames.AddRange(outline);polygons.Clear();polygons.AddRange(cells);lineColor=stroke;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float w=rectTransform.rect.width,h=rectTransform.rect.height;
            var xs=new List<float>{0,w};var ys=new List<float>{0,h};
            foreach(var r in holes){xs.Add(Mathf.Clamp(r.xMin,0,w));xs.Add(Mathf.Clamp(r.xMax,0,w));ys.Add(Mathf.Clamp(r.yMin,0,h));ys.Add(Mathf.Clamp(r.yMax,0,h));}
            xs=xs.Distinct().OrderBy(v=>v).ToList();ys=ys.Distinct().OrderBy(v=>v).ToList();
            var shade=new Color(SharedPaper.Ink.r,SharedPaper.Ink.g,SharedPaper.Ink.b,.22f);
            for(int x=0;x<xs.Count-1;x++)for(int y=0;y<ys.Count-1;y++)
            {var r=Rect.MinMaxRect(xs[x],ys[y],xs[x+1],ys[y+1]);if(!holes.Any(a=>a.Contains(r.center)))Quad(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax),shade);}
            foreach(var r in frames)Outline(vh,new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax)});
            foreach(var p in polygons)Outline(vh,p);
        }
        void Outline(VertexHelper vh,Vector2[] p)
        {
            for(int i=0;i<p.Length;i++)
            {var a=p[i];var b=p[(i+1)%p.Length];var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*2.2f;Quad(vh,a-n,b-n,b+n,a+n,lineColor);}
        }
        void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int start=vh.currentVertCount;Vertex(vh,a,color);Vertex(vh,b,color);Vertex(vh,c,color);Vertex(vh,d,color);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        void Vertex(VertexHelper vh,Vector2 p,Color color)=>vh.AddVert(new Vector3(rectTransform.rect.xMin+p.x,rectTransform.rect.yMax-p.y),color,Vector2.zero);
    }
}
