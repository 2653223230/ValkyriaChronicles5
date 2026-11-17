using System;
using System.Collections.Generic;
using System.Linq;

namespace VC5PvE.SharedAP
{
    public struct SPos : IEquatable<SPos>
    {
        public int X; public int Y;
        public SPos(int x,int y){X=x;Y=y;}
        public bool Equals(SPos o)=>X==o.X&&Y==o.Y;
        public override bool Equals(object o)=>o is SPos&&Equals((SPos)o);
        public override int GetHashCode()=>(X*397)^Y;
        public static bool operator ==(SPos a,SPos b)=>a.Equals(b);
        public static bool operator !=(SPos a,SPos b)=>!a.Equals(b);
        public int Distance(SPos o)=>Math.Abs(X-o.X)+Math.Abs(Y-o.Y);
        public override string ToString()=>"("+X+","+Y+")";
    }
    public enum SRole{Vanguard,Archer,Mage,Patrol,Crossbow,Guard,Cannon}
    public enum SCard{Attack,Heavy,Move,Charge,Skill,Shield,Heal}
    public enum SPhase{Player,Enemy,Won,Lost}
    public enum SActionKind{Card,ShortStep,BasicAttack}
    [Serializable] public sealed class SUnit
    {
        public string Id; public SRole Role; public string Name; public bool Player; public int Hp,MaxHp,Shield; public SPos Position; public bool StepUsed,AttackUsed,Braced; public int BraceExpiresRound;
        public bool Alive=>Hp>0;
        public SUnit Clone()=>(SUnit)MemberwiseClone();
    }
    [Serializable] public sealed class SCardInstance{public string Id;public SCard Kind;public SCardInstance Clone()=>(SCardInstance)MemberwiseClone();}
    [Serializable] public sealed class SState
    {
        public int Round,Ap,Revision,Seed;public SPhase Phase;public List<SUnit> Units=new List<SUnit>();public List<SCardInstance> Hand=new List<SCardInstance>(),Deck=new List<SCardInstance>(),Discard=new List<SCardInstance>();public HashSet<SPos> Obstacles=new HashSet<SPos>();public List<SPos> Telegraph=new List<SPos>();
        public SUnit Find(string id)=>Units.FirstOrDefault(x=>x.Id==id);public SUnit UnitAt(SPos p)=>Units.FirstOrDefault(x=>x.Alive&&x.Position==p);
        public SState Clone(){var s=(SState)MemberwiseClone();s.Units=Units.Select(x=>x.Clone()).ToList();s.Hand=Hand.Select(x=>x.Clone()).ToList();s.Deck=Deck.Select(x=>x.Clone()).ToList();s.Discard=Discard.Select(x=>x.Clone()).ToList();s.Obstacles=new HashSet<SPos>(Obstacles);s.Telegraph=new List<SPos>(Telegraph);return s;}
    }
    public sealed class SAction{public SActionKind Kind;public string ActorId,CardId,TargetId;public SPos? Destination;}
    public sealed class SStep{public SState Before,After;public SAction Action;public string Summary;public List<SPos> FiredCells;public List<SPos> Path=new List<SPos>();}
    public sealed class SPlan{public bool Valid;public string Reason,Summary;public int Revision;public SState After;public List<SPos> Path=new List<SPos>(),Affected=new List<SPos>();public List<SStep> Steps=new List<SStep>();}
    public static class SharedRules
    {
        public static int Cost(SCard c) => c == SCard.Heavy || c == SCard.Charge ? 2 : 1;
        public static string CardName(SCard c) => new[] { "攻击", "猛攻", "机动", "突击", "战技", "护盾", "治疗" }[(int)c];
        public static string CardText(SCard c) => new[] { "按执行者的武器攻击一次。", "武器攻击一次，主目标伤害 +2。", "移动 1–2 格。", "移动 1–2 格后武器攻击。", "先锋：冲撞得盾\n射手：架弩\n术师：星火爆裂", "受益者获得 3 护盾，至下次己方回合开始。", "受益者恢复 3 生命，不超过上限。" }[(int)c];
        public static string Coord(SPos p) => ((char)('A' + p.X)).ToString() + (p.Y + 1);
        static bool Inside(SPos p) => p.X >= 0 && p.X < 7 && p.Y >= 0 && p.Y < 7;
        static bool Block(SState s, SPos p) => !Inside(p) || s.Obstacles.Contains(p) || s.UnitAt(p) != null;

        public static SState Create(int seed, bool demo)
        {
            var s = new SState { Round = 1, Ap = 6, Phase = SPhase.Player, Seed = seed };
            s.Obstacles.UnionWith(new[] { new SPos(1,2), new SPos(1,3), new SPos(3,4), new SPos(5,2), new SPos(5,3) });
            Add(s,"vanguard",SRole.Vanguard,"先锋",true,9,3,6);
            Add(s,"archer",SRole.Archer,"射手",true,6,2,6);
            Add(s,"mage",SRole.Mage,"术师",true,6,4,6);
            Add(s,"patrol",SRole.Patrol,"巡兵",false,4,2,3);
            Add(s,"crossbow",SRole.Crossbow,"弩手",false,3,3,3);
            Add(s,"guard",SRole.Guard,"护卫",false,5,3,2);
            Add(s,"cannon",SRole.Cannon,"炮座",false,10,3,1);
            int[] counts = {4,2,3,2,3,2,2}; int id=0;
            for(int kind=0;kind<counts.Length;kind++)
                for(int i=0;i<counts[kind];i++) s.Deck.Add(new SCardInstance { Id="card-"+(++id), Kind=(SCard)kind });
            s.Telegraph.AddRange(new[] { new SPos(1,5), new SPos(2,5), new SPos(3,5) });
            if(demo) foreach(var kind in new[] {SCard.Attack,SCard.Move,SCard.Charge,SCard.Shield,SCard.Heal})
            { var c=s.Deck.First(x=>x.Kind==kind); s.Deck.Remove(c); s.Hand.Add(c); }
            Shuffle(s.Deck,seed);
            if(demo)
            {
                var healIndex=s.Hand.FindIndex(c=>c.Id=="card-17");
                var skillIndex=s.Deck.FindIndex(c=>c.Id=="card-12");
                if(healIndex>=0&&skillIndex>=0)
                { var heal=s.Hand[healIndex];s.Hand[healIndex]=s.Deck[skillIndex];s.Deck[skillIndex]=heal; }
            }
            Draw(s); return s;
        }
        static void Add(SState s,string id,SRole r,string name,bool player,int hp,int x,int y)
        { s.Units.Add(new SUnit { Id=id,Role=r,Name=name,Player=player,Hp=hp,MaxHp=hp,Position=new SPos(x,y) }); }
        static void Shuffle(List<SCardInstance> cards,int seed)
        {
            var random=new Random(seed);
            for(int i=cards.Count-1;i>0;i--) { int j=random.Next(i+1); var c=cards[i];cards[i]=cards[j];cards[j]=c; }
        }
        static void Draw(SState s)
        {
            while(s.Hand.Count<5)
            {
                if(s.Deck.Count==0) { s.Deck.AddRange(s.Discard);s.Discard.Clear();Shuffle(s.Deck,unchecked(s.Seed+s.Round*7919)); }
                if(s.Deck.Count==0)break;
                s.Hand.Add(s.Deck[0]);s.Deck.RemoveAt(0);
            }
        }
        static IEnumerable<SPos> Neighbors(SPos p)
        { yield return new SPos(p.X,p.Y-1);yield return new SPos(p.X-1,p.Y);yield return new SPos(p.X+1,p.Y);yield return new SPos(p.X,p.Y+1); }
        static Dictionary<SPos,List<SPos>> Paths(SState s,SUnit unit,int steps)
        {
            var paths=new Dictionary<SPos,List<SPos>>(); if(unit==null || !unit.Alive)return paths;
            paths[unit.Position]=new List<SPos>{unit.Position};var q=new Queue<SPos>();q.Enqueue(unit.Position);
            while(q.Count>0)
            {
                var p=q.Dequeue();if(paths[p].Count>steps)continue;
                foreach(var n in Neighbors(p))
                    if(!paths.ContainsKey(n)&&!Block(s,n)) { var path=new List<SPos>(paths[p]){n};paths[n]=path;q.Enqueue(n); }
            }
            return paths;
        }
        public static List<SPos> Reachable(SState s,string id,int steps)
        { var u=s.Find(id);return Paths(s,u,steps).Keys.Where(p=>u!=null&&p!=u.Position).ToList(); }
        public static bool HasSight(SState s,SPos a,SPos b)
        {
            foreach(var wall in s.Obstacles)
            {
                if(wall==a||wall==b)continue;
                const double e=1e-8;double lo=0,hi=1;
                if(Clip(a.X+.5,b.X-a.X,wall.X+e,wall.X+1-e,ref lo,ref hi) &&
                   Clip(a.Y+.5,b.Y-a.Y,wall.Y+e,wall.Y+1-e,ref lo,ref hi) && hi-lo>e)return false;
            }
            return true;
        }
        static bool Clip(double start,double delta,double min,double max,ref double lo,ref double hi)
        {
            if(Math.Abs(delta)<1e-12)return start>min&&start<max;
            double a=(min-start)/delta,b=(max-start)/delta;
            if(a>b){double swap=a;a=b;b=swap;}
            lo=Math.Max(lo,a);hi=Math.Min(hi,b);return hi>lo;
        }
        public static SPlan Plan(SState s,SAction a) => Build(s,a);
        public static SPlan Apply(SState s,SAction a)
        { var plan=Build(s,a);if(plan.Valid)Copy(s,plan.After);return plan; }
        static void Copy(SState s,SState n)
        {
            s.Round=n.Round;s.Ap=n.Ap;s.Revision=n.Revision;s.Phase=n.Phase;s.Seed=n.Seed;
            s.Units=n.Units;s.Hand=n.Hand;s.Deck=n.Deck;s.Discard=n.Discard;s.Obstacles=n.Obstacles;s.Telegraph=n.Telegraph;
        }
        static SPlan Invalid(SPlan p,string reason){p.Reason=reason;return p;}
        static SPlan Build(SState before,SAction a)
        {
            var p=new SPlan{Revision=before.Revision,After=before.Clone()};var s=p.After;
            if(s.Phase!=SPhase.Player||a==null)return Invalid(p,"当前不能行动");
            var u=s.Find(a.ActorId);
            if(u==null||!u.Alive||!u.Player)return Invalid(p,"请拖给存活的己方棋子");
            SCardInstance card=null;SCard? kind=null;
            if(a.Kind==SActionKind.Card)
            {
                card=s.Hand.FirstOrDefault(x=>x.Id==a.CardId);
                if(card==null)return Invalid(p,"这张牌已不在手中");kind=card.Kind;
            }
            int cost=kind.HasValue?Cost(kind.Value):a.Kind==SActionKind.BasicAttack?2:0;
            if(s.Ap<cost)return Invalid(p,"全队 AP 不足");
            if(a.Kind==SActionKind.ShortStep)
            {
                if(u.StepUsed)return Invalid(p,"本轮短步已用");
                if(!TryMove(s,u,a.Destination,1,p))return Invalid(p,"请选择四邻空格");
                u.StepUsed=true;
            }
            else if(kind==SCard.Move)
            { if(!TryMove(s,u,a.Destination,2,p))return Invalid(p,"请选择可走到的 1–2 格内空格"); }
            else if(a.Kind==SActionKind.BasicAttack||kind==SCard.Attack||kind==SCard.Heavy||kind==SCard.Charge)
            {
                if(a.Kind==SActionKind.BasicAttack&&u.AttackUsed)return Invalid(p,"本轮普攻已用");
                if(kind==SCard.Charge&&!TryMove(s,u,a.Destination,2,p))return Invalid(p,"突击需要 1–2 格内合法落点");
                if(!Hit(s,u,s.Find(a.TargetId),kind==SCard.Heavy?2:0,p))return Invalid(p,"目标不在武器射程或被障碍遮挡");
                if(a.Kind==SActionKind.BasicAttack)u.AttackUsed=true;
            }
            else if(kind==SCard.Heal||kind==SCard.Shield)
            {
                if(a.TargetId!=null&&a.TargetId!=u.Id)return Invalid(p,"此牌直接作用于接牌棋子");
                if(kind==SCard.Heal)
                { if(u.Hp>=u.MaxHp)return Invalid(p,"生命已满");u.Hp=Math.Min(u.MaxHp,u.Hp+3); }
                else { if(u.Shield>=3)return Invalid(p,"已有不少于 3 点护盾");u.Shield=3; }
                p.Affected.Add(u.Position);
            }
            else if(kind==SCard.Skill)
            { if(!Skill(s,u,a,p))return Invalid(p,"战技没有合法目标或已生效"); }
            else return Invalid(p,"未知行动");
            if(card!=null){s.Hand.Remove(card);s.Discard.Add(card);}
            s.Ap-=cost;s.Revision++;CheckOutcome(s);p.Valid=true;
            p.Summary=u.Name+" · "+(kind.HasValue?CardName(kind.Value):a.Kind==SActionKind.ShortStep?"短步":"普攻")+"  −"+cost+" AP";
            foreach(var after in s.Units)
            {
                var old=before.Find(after.Id);if(old==null)continue;
                if(old.Position!=after.Position)p.Summary+="\n"+after.Name+" "+Coord(old.Position)+" → "+Coord(after.Position);
                if(old.Hp!=after.Hp||old.Shield!=after.Shield)p.Summary+="\n"+after.Name+" 生命 "+old.Hp+"→"+after.Hp+"  盾 "+old.Shield+"→"+after.Shield;
            }
            if(kind==SCard.Skill&&u.Role==SRole.Archer)p.Summary+="\n架弩：下一击 +2 伤害 / +1 射程；移动解除";
            return p;
        }
        static bool TryMove(SState s,SUnit u,SPos? dest,int steps,SPlan p)
        {
            if(!dest.HasValue||dest.Value==u.Position)return false;
            var paths=Paths(s,u,steps);if(!paths.ContainsKey(dest.Value))return false;
            p.Path.AddRange(paths[dest.Value]);Move(u,dest.Value);return true;
        }
        static void Move(SUnit u,SPos dest)
        { if(u.Position!=dest){u.Position=dest;u.Braced=false;} }
        public static int WeaponRange(SUnit u) => u.Role==SRole.Archer?3+(u.Braced?1:0):u.Role==SRole.Mage?2:u.Role==SRole.Crossbow?3:1;
        public static int WeaponDamage(SUnit u) => u.Role==SRole.Vanguard||u.Role==SRole.Patrol||u.Role==SRole.Guard?3:u.Role==SRole.Mage?1:2;
        static bool CanHit(SState s,SUnit u,SPos from,SUnit target)
        { return target!=null&&target.Alive&&target.Player!=u.Player&&from.Distance(target.Position)<=WeaponRange(u)&&HasSight(s,from,target.Position); }
        static bool Hit(SState s,SUnit u,SUnit target,int bonus,SPlan p)
        {
            if(!CanHit(s,u,u.Position,target))return false;
            int damage=WeaponDamage(u);
            var center=target.Position;
            Damage(s,target,damage+bonus+(u.Role==SRole.Archer&&u.Braced?2:0));p.Affected.Add(center);
            if(u.Role==SRole.Archer)u.Braced=false;
            if(u.Role==SRole.Mage)
                foreach(var splash in s.Units.Where(x=>x.Alive&&!x.Player&&x.Id!=target.Id&&x.Position.Distance(center)==1&&HasSight(s,center,x.Position)).ToList())
                { Damage(s,splash,1);p.Affected.Add(splash.Position); }
            CheckOutcome(s);return true;
        }
        static void Damage(SState s,SUnit target,int amount,HashSet<string> guardedCannons=null)
        {
            bool guarded=guardedCannons!=null
                ? guardedCannons.Contains(target.Id)
                : s.Units.Any(x=>x.Alive&&x.Role==SRole.Guard&&x.Position.Distance(target.Position)==1);
            if(target.Role==SRole.Cannon&&guarded)amount=Math.Max(0,amount-2);
            int shield=Math.Min(target.Shield,amount);target.Shield-=shield;target.Hp=Math.Max(0,target.Hp-(amount-shield));
        }
        static bool Skill(SState s,SUnit u,SAction a,SPlan p)
        {
            if(u.Role==SRole.Archer)
            { if(u.Braced&&u.BraceExpiresRound==s.Round+1)return false;u.Braced=true;u.BraceExpiresRound=s.Round+1;return true; }
            var t=s.Find(a.TargetId);if(t==null||!t.Alive||t.Player)return false;
            if(u.Role==SRole.Vanguard)
            {
                if(u.Position.Distance(t.Position)!=1)return false;
                var dest=new SPos(t.Position.X+t.Position.X-u.Position.X,t.Position.Y+t.Position.Y-u.Position.Y);
                if(t.Role!=SRole.Cannon&&!Block(s,dest)){p.Path.Add(t.Position);Move(t,dest);p.Path.Add(dest);}
                u.Shield=Math.Max(u.Shield,2);p.Affected.Add(u.Position);return true;
            }
            if(u.Role!=SRole.Mage||u.Position.Distance(t.Position)>2||!HasSight(s,u.Position,t.Position))return false;
            var center=t.Position;
            var targets=s.Units.Where(x=>x.Alive&&!x.Player&&
                (x.Id==t.Id||x.Position.Distance(center)==1&&HasSight(s,center,x.Position))).ToList();
            var guardedCannons=new HashSet<string>(targets
                .Where(x=>x.Role==SRole.Cannon&&s.Units.Any(g=>g.Alive&&g.Role==SRole.Guard&&g.Position.Distance(x.Position)==1))
                .Select(x=>x.Id));
            foreach(var target in targets)
            { Damage(s,target,2,guardedCannons);p.Affected.Add(target.Position); }
            CheckOutcome(s);
            return true;
        }
        static void CheckOutcome(SState s)
        {
            if(s.Units.Any(x=>x.Role==SRole.Cannon&&!x.Alive)||
               !s.Units.Any(x=>(x.Role==SRole.Patrol||x.Role==SRole.Crossbow||x.Role==SRole.Guard)&&x.Alive))s.Phase=SPhase.Won;
            else if(!s.Units.Any(x=>x.Player&&x.Alive))s.Phase=SPhase.Lost;
        }
        /// <summary>
        /// Ends the player turn, discarding the selected hand cards. Cards not selected are kept,
        /// and the next player turn draws until the hand contains up to five cards.
        /// A null collection is equivalent to discarding no cards.
        /// </summary>
        public static SPlan EndTurn(SState before,IEnumerable<string> discardIds)
        {
            var p=new SPlan{Revision=before.Revision,After=before.Clone()};var s=p.After;
            var discard=(discardIds??Enumerable.Empty<string>()).ToList();
            if(s.Phase!=SPhase.Player)return Invalid(p,"当前不能结束回合");
            if(discard.Distinct().Count()!=discard.Count||discard.Any(id=>!s.Hand.Any(c=>c.Id==id)))return Invalid(p,"弃牌列表包含重复 ID 或不在手牌中的 ID");
            foreach(var c in s.Hand.Where(c=>discard.Contains(c.Id)).ToList()){s.Hand.Remove(c);s.Discard.Add(c);}
            foreach(var u in s.Units)if(u.Braced&&u.BraceExpiresRound<=s.Round)u.Braced=false;
            Enemy(s,p);
            if(s.Phase!=SPhase.Won&&s.Phase!=SPhase.Lost)
            {
                s.Round++;s.Ap=6;s.Phase=SPhase.Player;
                foreach(var u in s.Units.Where(x=>x.Player)){u.StepUsed=false;u.AttackUsed=false;u.Shield=0;}
                Draw(s);
            }
            s.Revision++;p.Valid=true;Copy(before,s);return p;
        }
        public static SPlan EnemyPreview(SState before)
        {
            var p=new SPlan{Valid=true,Revision=before.Revision,After=before.Clone(),Summary=""};
            if(before.Phase==SPhase.Won||before.Phase==SPhase.Lost){p.Summary="战斗已结束";return p;}
            foreach(var u in p.After.Units)if(u.Braced&&u.BraceExpiresRound<=p.After.Round)u.Braced=false;
            Enemy(p.After,p);return p;
        }
        static void Enemy(SState s,SPlan p)
        {
            s.Phase=SPhase.Enemy;var lines=new List<string>();CheckOutcome(s);
            foreach(var role in new[]{SRole.Patrol,SRole.Crossbow,SRole.Guard,SRole.Cannon})
            {
                if(s.Phase==SPhase.Won||s.Phase==SPhase.Lost)break;
                var u=s.Units.FirstOrDefault(x=>x.Alive&&x.Role==role);if(u==null)continue;
                if(role==SRole.Cannon)
                {
                    var stepBefore=s.Clone();
                    var firedCells=new List<SPos>();
                    foreach(var cell in s.Telegraph)
                    { firedCells.Add(cell);var target=s.UnitAt(cell);if(target!=null){Damage(s,target,2);p.Affected.Add(cell);}CheckOutcome(s);if(s.Phase==SPhase.Won||s.Phase==SPhase.Lost)break; }
                    lines.Add("炮座：红框齐射 2 点");CheckOutcome(s);
                    if(s.Phase!=SPhase.Won&&s.Phase!=SPhase.Lost)s.Telegraph=NextTelegraph(s);
                    p.Steps.Add(new SStep
                    {
                        Before=stepBefore,FiredCells=firedCells,
                        After=s.Clone(),
                        Action=new SAction{Kind=SActionKind.Card,ActorId=u.Id,CardId="cannon-salvo",TargetId=null},
                        Summary=lines[lines.Count-1]
                    });
                    continue;
                }
                var stepStart=s.Clone();
                var start=u.Position;
                var path=new List<SPos>{start};
                if(Target(s,u)==null)Advance(s,u,path);
                var targetUnit=Target(s,u);
                string line=u.Name+(start==u.Position?" 原地":" "+Coord(start)+"→"+Coord(u.Position));
                if(targetUnit!=null)
                {
                    int damage=WeaponDamage(u);
                    Damage(s,targetUnit,damage);
                    p.Affected.Add(targetUnit.Position);
                    line+=" → "+targetUnit.Name+" "+damage+" 点";
                }
                else line+="，无攻击";
                lines.Add(line);CheckOutcome(s);
                p.Steps.Add(new SStep
                {
                    Path=path,Before=stepStart,
                    After=s.Clone(),
                    Action=new SAction{Kind=SActionKind.BasicAttack,ActorId=u.Id,TargetId=targetUnit==null?null:targetUnit.Id},
                    Summary=line
                });
            }
            p.Summary=string.Join("\n",lines);
        }
        static SUnit Target(SState s,SUnit u)
        { return s.Units.Where(x=>CanHit(s,u,u.Position,x)).OrderBy(x=>u.Position.Distance(x.Position)).ThenBy(x=>x.Hp).FirstOrDefault(); }
        static void Advance(SState s,SUnit u,List<SPos> path)
        {
            var paths=Paths(s,u,49);
            var options=(from t in s.Units where t.Alive&&t.Player
                         from pair in paths where CanHit(s,u,pair.Key,t)
                         select new {Target=t,Path=pair.Value,Pos=pair.Key}).OrderBy(x=>x.Path.Count).ThenBy(x=>x.Target.Hp)
                         .ThenBy(x=>s.Units.IndexOf(x.Target)).ThenBy(x=>x.Pos.Y).ThenBy(x=>x.Pos.X).FirstOrDefault();
            if(options==null)return;
            int allowance=u.Role==SRole.Guard?2:1;
            for(int i=1;i<options.Path.Count&&i<=allowance;i++)
            {
                Move(u,options.Path[i]);
                path.Add(u.Position);
                if(Target(s,u)!=null)break;
            }
        }
        static List<SPos> NextTelegraph(SState s)
        {
            var choices=new List<List<SPos>>();
            for(int y=0;y<7;y++)for(int x=0;x<5;x++)choices.Add(new List<SPos>{new SPos(x,y),new SPos(x+1,y),new SPos(x+2,y)});
            return choices.OrderByDescending(c=>s.Units.Count(u=>u.Alive&&u.Player&&c.Contains(u.Position)))
                .ThenBy(c=>s.Units.Where(u=>u.Alive&&u.Player&&c.Contains(u.Position)).Select(u=>u.Hp).DefaultIfEmpty(int.MaxValue).Min())
                .ThenBy(c=>c[0].Y).ThenBy(c=>c[0].X).First();
        }
    }
}
