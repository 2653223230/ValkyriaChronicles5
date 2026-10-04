using System;
using System.Collections.Generic;

namespace VC5PvE
{
    public static class BattleFactory
    {
        public static BattleState Create(int seed)
        { return Create(seed, true); }

        public static BattleState Create(bool fixedOpening)
        { return Create(unchecked(Environment.TickCount * 397 ^ Guid.NewGuid().GetHashCode()), fixedOpening); }

        public static BattleState Create(int seed, bool fixedOpening)
        {
            var state = new BattleState { RandomSeed = seed, Round = 0, Phase = BattlePhase.Deployment };
            state.Units.Add(Definitions.Unit("warrior"));
            state.Units.Add(Definitions.Unit("mage"));
            state.Units.Add(Definitions.Unit("support"));
            state.Units.Add(Definitions.Unit("guard-a"));
            state.Units.Add(Definitions.Unit("guard-b"));
            state.Units.Add(Definitions.Unit("shooter"));
            state.Obstacles.Add(new GridPos(2, 2));
            state.Obstacles.Add(new GridPos(2, 3));
            state.Obstacles.Add(new GridPos(4, 2));
            state.Obstacles.Add(new GridPos(4, 3));

            var cards = new List<CardInstance>();
            foreach (CardKind kind in Enum.GetValues(typeof(CardKind)))
            {
                var definition = Definitions.Card(kind);
                for (var i = 1; i <= definition.Copies; i++) cards.Add(new CardInstance { Id = CardId(kind, i), Kind = kind });
            }
            if (fixedOpening)
            {
                var openingKinds = new[] { CardKind.Advance, CardKind.HeavyAttack, CardKind.Cover, CardKind.SparkMark, CardKind.StarBurst, CardKind.Inspire };
                foreach (var kind in openingKinds)
                {
                    var index = cards.FindIndex(c => c.Kind == kind);
                    state.Hand.Add(cards[index]); cards.RemoveAt(index);
                }
                Shuffle(cards, seed);
                state.DrawPile.AddRange(cards);
            }
            else
            {
                Shuffle(cards, seed);
                for (var i = 0; i < 6; i++) state.Hand.Add(cards[i]);
                for (var i = 6; i < cards.Count; i++) state.DrawPile.Add(cards[i]);
            }
            return state;
        }

        public static string CardId(CardKind kind, int copy)
        { return "card-" + kind.ToString().ToLowerInvariant() + "-" + copy.ToString("00"); }

        public static bool TryDeploy(BattleState state, string unitId, GridPos position)
        {
            if (state == null || state.Phase != BattlePhase.Deployment || !IsDeploymentPosition(position) || state.Obstacles.Contains(position)) return false;
            var unit = state.FindUnit(unitId);
            if (unit == null || unit.Team != Team.Player || !unit.IsAlive) return false;
            var occupant = state.UnitAt(position);
            if (occupant != null && occupant.Team != Team.Player) return false;
            var oldPosition = unit.Position;
            if (occupant != null && occupant.Id != unit.Id) occupant.Position = oldPosition;
            unit.Position = position;
            state.Revision++;
            return true;
        }

        public static bool IsDeploymentPosition(GridPos position)
        { return position.X >= 1 && position.X <= 3 && position.Y >= 6 && position.Y <= 7; }

        private static void Shuffle<T>(IList<T> list, int seed)
        {
            var random = new Random(seed);
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1); var item = list[i]; list[i] = list[j]; list[j] = item;
            }
        }
    }
}
