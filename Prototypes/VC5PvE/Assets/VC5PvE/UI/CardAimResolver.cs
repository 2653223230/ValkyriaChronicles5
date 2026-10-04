using System;
using System.Collections.Generic;
using UnityEngine;
namespace VC5PvE
{
    public static class CardAimResolver
    {
        public static UnitState ResolveActor(BattleState state,string cardId,string selectedId,UnitState hovered=null)
        {
            var card=state.Hand.Find(c=>c.Id==cardId);if(card==null)return null;
            if(card.Kind==CardKind.Cover && hovered!=null && hovered.Team==Team.Player && hovered.IsAlive)return hovered;
            var def=Definitions.Card(card.Kind);var selected=state.FindUnit(selectedId);
            if(selected!=null && selected.IsAlive && selected.Team==Team.Player && (!def.AllowedRole.HasValue||selected.Role==def.AllowedRole))return selected;
            if(def.AllowedRole.HasValue)return state.Units.Find(u=>u.Team==Team.Player&&u.IsAlive&&u.Role==def.AllowedRole);
            return null;
        }
        public static ActionPlan Resolve(BattleState state,string cardId,string selectedId,UnitState hovered,GridPos? cell,Vector2 pointer,Func<GridPos,Vector2> project)
        {
            var card=state.Hand.Find(c=>c.Id==cardId);var actor=ResolveActor(state,cardId,selectedId,hovered);
            var req=new ActionRequest{Kind=ActionKind.Card,CardId=cardId,ActorId=actor==null?null:actor.Id,TargetId=hovered==null?null:hovered.Id,Destination=cell};
            if(card==null||actor==null)return Invalid(state,req,"请先选择可用的己方执行者");
            if(card.Kind==CardKind.Cover && (hovered==null||hovered.Team!=Team.Player||!hovered.IsAlive))return Invalid(state,req,"拖向要施放掩护的己方棋子");
            if(card.Kind!=CardKind.Charge)return ActionPlanner.Plan(state,req);
            if(hovered==null||hovered.Team!=Team.Enemy||!hovered.IsAlive)return Invalid(state,req,"拖向敌人，预览突入落点和击退方向");
            ActionPlan best=null;float score=float.MaxValue;
            foreach(var d in new[]{new GridPos(0,-1),new GridPos(1,0),new GridPos(0,1),new GridPos(-1,0)})
            {
                var dest=new GridPos(hovered.Position.X+d.X,hovered.Position.Y+d.Y);
                var p=ActionPlanner.Plan(state,new ActionRequest{Kind=ActionKind.Card,CardId=cardId,ActorId=actor.Id,TargetId=hovered.Id,Destination=dest});
                if(!p.IsValid)continue;float distance=project==null?p.Path.Count:(project(dest)-pointer).sqrMagnitude;
                if(best==null||distance<score-.001f){best=p;score=distance;}
            }
            return best??Invalid(state,req,"没有可达的邻敌落点（最多移动3格）");
        }
        public static List<GridPos> LegalTargets(BattleState state,string cardId,string selectedId)
        {
            var list=new List<GridPos>();var card=state.Hand.Find(c=>c.Id==cardId);if(card==null)return list;
            for(int y=0;y<state.Height;y++)for(int x=0;x<state.Width;x++)
            {
                var cell=new GridPos(x,y);var unit=state.UnitAt(cell);
                if(card.Kind!=CardKind.Advance&&unit==null)continue;
                if(Resolve(state,cardId,selectedId,unit,cell,Vector2.zero,null).IsValid)list.Add(cell);
            }
            return list;
        }
        private static ActionPlan Invalid(BattleState s,ActionRequest r,string reason){return new ActionPlan{Request=r,Revision=s.Revision,IsValid=false,Reason=reason};}
    }
}
