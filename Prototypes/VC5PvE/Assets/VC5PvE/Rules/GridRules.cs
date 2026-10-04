using System;
using System.Collections.Generic;

namespace VC5PvE
{
    public static class GridRules
    {
        private static readonly GridPos[] Directions = { new GridPos(0, -1), new GridPos(1, 0), new GridPos(0, 1), new GridPos(-1, 0) };

        public static List<GridPos> Reachable(BattleState state, UnitState unit, int maxSteps)
        {
            var result = new List<GridPos>();
            if (state == null || unit == null || !unit.IsAlive || maxSteps < 0) return result;
            var distances = new Dictionary<GridPos, int>();
            var queue = new Queue<GridPos>();
            distances[unit.Position] = 0; queue.Enqueue(unit.Position);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue(); var distance = distances[current];
                if (distance > 0) result.Add(current);
                if (distance == maxSteps) continue;
                foreach (var direction in Directions)
                {
                    var next = new GridPos(current.X + direction.X, current.Y + direction.Y);
                    if (!next.IsInside(state.Width, state.Height) || distances.ContainsKey(next) || state.Obstacles.Contains(next)) continue;
                    var occupant = state.UnitAt(next);
                    if (occupant != null && occupant.Id != unit.Id) continue;
                    distances[next] = distance + 1; queue.Enqueue(next);
                }
            }
            return result;
        }

        public static bool HasLineOfSight(BattleState state, GridPos from, GridPos to)
        {
            if (state == null) return false;
            foreach (var obstacle in state.Obstacles)
            {
                if (obstacle == from || obstacle == to) continue;
                if (CrossesInterior(from.X + 0.5, from.Y + 0.5, to.X + 0.5, to.Y + 0.5, obstacle.X, obstacle.Y)) return false;
            }
            return true;
        }

        private static bool CrossesInterior(double x0, double y0, double x1, double y1, int cellX, int cellY)
        {
            const double epsilon = 1e-8;
            var low = 0.0; var high = 1.0;
            if (!Clip(x0, x1 - x0, cellX + epsilon, cellX + 1.0 - epsilon, ref low, ref high)) return false;
            if (!Clip(y0, y1 - y0, cellY + epsilon, cellY + 1.0 - epsilon, ref low, ref high)) return false;
            return high - low > epsilon;
        }

        private static bool Clip(double start, double delta, double min, double max, ref double low, ref double high)
        {
            if (Math.Abs(delta) < 1e-12) return start > min && start < max;
            var a = (min - start) / delta; var b = (max - start) / delta;
            if (a > b) { var swap = a; a = b; b = swap; }
            low = Math.Max(low, a); high = Math.Min(high, b);
            return high > low;
        }
    }
}
