using System;
using System.Collections.Generic;

namespace VC5PvE
{
    public enum UnitRole { Warrior, Mage, Support, Guard, Shooter }
    public enum Team { Player, Enemy }
    public enum BattlePhase { Deployment, Player, Enemy, Victory, Defeat }
    public enum ActionKind { Move, Attack, Heal, Card }
    public enum CardKind { Advance, HeavyAttack, Cover, Charge, SparkMark, StarBurst, Inspire }

    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public int X;
        public int Y;
        public GridPos(int x, int y) { X = x; Y = y; }
        public bool IsInside(int width, int height) { return X >= 0 && Y >= 0 && X < width && Y < height; }
        public int ManhattanDistance(GridPos other) { return Math.Abs(X - other.X) + Math.Abs(Y - other.Y); }
        public bool Equals(GridPos other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is GridPos && Equals((GridPos)obj); }
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public static bool operator ==(GridPos a, GridPos b) { return a.Equals(b); }
        public static bool operator !=(GridPos a, GridPos b) { return !a.Equals(b); }
        public override string ToString() { return "(" + X + "," + Y + ")"; }
    }

    [Serializable]
    public sealed class UnitState
    {
        public string Id;
        public UnitRole Role;
        public Team Team;
        public GridPos Position;
        public int Hp;
        public int MaxHp;
        public int Ap;
        public int MaxAp;
        public int Attack;
        public int AttackRange;
        public int MoveSteps;
        public bool AttackUsed;
        public bool HealUsed;
        public int Shield;
        public int MarkExpiresRound;
        public int InspireExpiresRound;
        public bool EnemyMoveUsed;
        public bool EnemyAttackUsed;
        public UnitState Clone() { return (UnitState)MemberwiseClone(); }
        public bool IsAlive { get { return Hp > 0; } }
    }

    [Serializable]
    public sealed class CardInstance
    {
        public string Id;
        public CardKind Kind;
        public CardInstance Clone() { return (CardInstance)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class BattleState
    {
        public int Width = 8;
        public int Height = 8;
        public List<UnitState> Units = new List<UnitState>();
        public List<CardInstance> Hand = new List<CardInstance>();
        public List<CardInstance> DrawPile = new List<CardInstance>();
        public List<CardInstance> DiscardPile = new List<CardInstance>();
        public HashSet<GridPos> Obstacles = new HashSet<GridPos>();
        public int Round;
        public int Revision;
        public int RandomSeed;
        public BattlePhase Phase = BattlePhase.Deployment;
        public bool ExchangedThisTurn;
        public bool FirstActionMade;
        public HashSet<string> GuardReducedTargetsThisEnemyTurn = new HashSet<string>();

        public UnitState FindUnit(string id)
        { foreach (var unit in Units) if (unit.Id == id) return unit; return null; }
        public UnitState UnitAt(GridPos pos)
        { foreach (var unit in Units) if (unit.IsAlive && unit.Position == pos) return unit; return null; }
        public BattleState Clone()
        {
            var copy = new BattleState { Width = Width, Height = Height, Round = Round, Revision = Revision, RandomSeed = RandomSeed, Phase = Phase, ExchangedThisTurn = ExchangedThisTurn, FirstActionMade = FirstActionMade };
            foreach (var id in GuardReducedTargetsThisEnemyTurn) copy.GuardReducedTargetsThisEnemyTurn.Add(id);
            foreach (var unit in Units) copy.Units.Add(unit.Clone());
            foreach (var card in Hand) copy.Hand.Add(card.Clone());
            foreach (var card in DrawPile) copy.DrawPile.Add(card.Clone());
            foreach (var card in DiscardPile) copy.DiscardPile.Add(card.Clone());
            foreach (var pos in Obstacles) copy.Obstacles.Add(pos);
            return copy;
        }
    }

    public sealed class CardDefinition
    {
        public CardKind Kind;
        public string Name;
        public string Description;
        public int Cost;
        public UnitRole? AllowedRole;
        public int Copies;
        public string UserLabel { get { return AllowedRole.HasValue ? Definitions.UnitName(AllowedRole.Value)+"专用" : "中立 · 全部棋子"; } }
    }

    public static class Definitions
    {
        private static readonly Dictionary<CardKind, CardDefinition> Cards = new Dictionary<CardKind, CardDefinition>
        {
            { CardKind.Advance, new CardDefinition { Kind = CardKind.Advance, Name = "稳步推进", Description = "移动1–2格。\n自身获得1护盾，\n持续至下次己方回合。", Cost = 1, Copies = 3 } },
            { CardKind.HeavyAttack, new CardDefinition { Kind = CardKind.HeavyAttack, Name = "重攻击", Description = "按自身普攻射程，\n对敌人造成3伤害。\n需视线，不占普攻次数。", Cost = 2, Copies = 3 } },
            { CardKind.Cover, new CardDefinition { Kind = CardKind.Cover, Name = "掩护", Description = "自身获得2护盾，\n持续至下次己方回合。\n不叠加，只保留较高值。", Cost = 1, Copies = 3 } },
            { CardKind.Charge, new CardDefinition { Kind = CardKind.Charge, Name = "破阵突入", Description = "移动1–3格，靠近敌人。\n造成2伤害，推开1格。\n推不开时仍造成伤害。", Cost = 2, AllowedRole = UnitRole.Warrior, Copies = 2 } },
            { CardKind.SparkMark, new CardDefinition { Kind = CardKind.SparkMark, Name = "星火印", Description = "射程3，需视线。\n造成1伤害，附加印记。\n印记持续至下个\n己方回合结束。", Cost = 1, AllowedRole = UnitRole.Mage, Copies = 2 } },
            { CardKind.StarBurst, new CardDefinition { Kind = CardKind.StarBurst, Name = "贯星术", Description = "射程3，需视线。\n造成2伤害。\n有印记时消耗印记，\n改为4伤害。", Cost = 1, AllowedRole = UnitRole.Mage, Copies = 2 } },
            { CardKind.Inspire, new CardDefinition { Kind = CardKind.Inspire, Name = "战术鼓舞", Description = "距离2内另一名友军，\n下次伤害+1（不叠加）。\n最迟于下个己方回合\n结束时失效。", Cost = 1, AllowedRole = UnitRole.Support, Copies = 2 } }
        };
        public static CardDefinition Card(CardKind kind)
        {
            CardDefinition value;
            if (!Cards.TryGetValue(kind, out value)) throw new ArgumentOutOfRangeException("kind");
            return value;
        }
        public static string UnitName(UnitRole role)
        {
            switch(role) { case UnitRole.Warrior:return "守铃者";case UnitRole.Mage:return "星纹师";case UnitRole.Support:return "回响医者";case UnitRole.Guard:return "林地守卫";default:return "遗迹咒射手"; }
        }
        public static string PartyDescription(UnitRole role)
        { return role==UnitRole.Warrior?"前排 · 守护":role==UnitRole.Mage?"法使 · 印记":"辅助 · 支援"; }
        public static UnitState Unit(string id)
        {
            switch (id)
            {
                case "warrior": return Make(id, UnitRole.Warrior, Team.Player, 9, 3, 2, 1, 1, new GridPos(2, 6));
                case "mage": return Make(id, UnitRole.Mage, Team.Player, 5, 2, 2, 3, 1, new GridPos(3, 7));
                case "support": return Make(id, UnitRole.Support, Team.Player, 6, 2, 1, 2, 2, new GridPos(1, 7));
                case "guard-a": return Make(id, UnitRole.Guard, Team.Enemy, 6, 0, 2, 1, 1, new GridPos(3, 3));
                case "guard-b": return Make(id, UnitRole.Guard, Team.Enemy, 6, 0, 2, 1, 1, new GridPos(5, 4));
                case "shooter": return Make(id, UnitRole.Shooter, Team.Enemy, 10, 0, 3, 3, 1, new GridPos(5, 1));
                default: throw new ArgumentOutOfRangeException("id");
            }
        }
        private static UnitState Make(string id, UnitRole role, Team team, int hp, int ap, int attack, int range, int steps, GridPos pos)
        { return new UnitState { Id = id, Role = role, Team = team, Hp = hp, MaxHp = hp, Ap = ap, MaxAp = ap, Attack = attack, AttackRange = range, MoveSteps = steps, Position = pos }; }
    }

    public sealed class ActionRequest
    {
        public ActionKind Kind;
        public string ActorId;
        public string CardId;
        public string TargetId;
        public GridPos? Destination;
    }

    public sealed class DamageSummary
    {
        public string TargetId;
        public int BaseDamage;
        public int BonusDamage;
        public int GuardReduction;
        public int ShieldAbsorbed;
        public int HpLost;
        public bool Killed;
        public override string ToString() { return TargetId + ":" + HpLost + " HP (shield " + ShieldAbsorbed + ")"; }
    }

    public sealed class ActionPlan
    {
        public bool IsValid;
        public string Reason;
        public int Cost;
        public List<GridPos> Path = new List<GridPos>();
        public ActionRequest Request;
        public int Revision;
        public string Summary;
        public DamageSummary DamageSummary;
        public int PlannedDamage;
        public int DamageBase;
        public int DamageBonus;
    }

    public sealed class ActionResult
    {
        public bool Success;
        public string Reason;
        public int Damage;
        public int Healing;
        public DamageSummary DamageSummary;
        public string Summary;
    }

    public sealed class EnemyTurnPlan
    {
        public List<ActionPlan> Actions = new List<ActionPlan>();
        public List<DamageSummary> DamageSummary = new List<DamageSummary>();
    }
}
