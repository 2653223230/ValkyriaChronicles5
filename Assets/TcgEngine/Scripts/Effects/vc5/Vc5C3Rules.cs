using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    public sealed class Vc5C3Plan
    {
        public bool valid;
        public string reason = "请选择己方棋子";
        public Slot destination = Slot.None;
        public readonly List<Slot> path = new List<Slot>();
        public readonly List<Slot> legalSlots = new List<Slot>();
        public readonly List<Card> targets = new List<Card>();
        public int range;
    }

    public static class Vc5C3Rules
    {
        public const string Prefix = "vc5_demo_c3_";
        public const string Ranger = Prefix + "mobile_ranger";
        public const string Sniper = Prefix + "sniper";
        public const string Guard = Prefix + "fire_guard";

        public static bool IsCard(Card card) { return card != null && card.card_id.StartsWith(Prefix) && card.CardData.type == CardType.Spell; }
        public static bool IsPreciseMove(Card card) { return card != null && (card.card_id == Prefix + "tactical_move" || card.card_id == Prefix + "forced_march"); }

        public static Vc5C3Plan Plan(Game data, Card spell, Card actor, Slot? selected = null)
        {
            var plan = new Vc5C3Plan();
            if (data == null || !IsCard(spell) || actor == null || actor.player_id != spell.player_id
                || !data.IsOnBoard(actor) || !actor.CardData.IsCharacter()) return plan;
            if (actor.HasStatus(StatusType.SpellImmunity)) { plan.reason = "该棋子无法被法术选中"; return plan; }
            plan.destination = actor.slot;
            plan.range = Vc5DemoGrid.AttackRange(actor);
            string id = spell.card_id;
            if (IsPreciseMove(spell))
            {
                int range = Mathf.Max(0, actor.move_Range + (id == Prefix + "forced_march" ? 2 : 0));
                var paths = Reachable(data, actor, range);
                foreach (var pair in paths) if (pair.Value.Count > 1) plan.legalSlots.Add(pair.Key);
                if (!selected.HasValue) plan.valid = plan.legalSlots.Count > 0;
                else if (plan.legalSlots.Contains(selected.Value))
                {
                    plan.valid = true;
                    plan.destination = selected.Value;
                    plan.path.AddRange(paths[selected.Value]);
                }
                plan.reason = plan.valid ? "选择高亮空格" : "没有合法移动路线";
                return plan;
            }
            if (id == Prefix + "temp_calibration" || id == Prefix + "scope_upgrade")
            {
                plan.valid = true;
                plan.range += id == Prefix + "temp_calibration" && actor.HasStatus(StatusType.Vc5C3TemporaryRange) ? 0 : 1;
                plan.reason = id == Prefix + "temp_calibration" ? "射程 +1（本回合，不叠加）" : "射程 +1（本局）";
                return plan;
            }
            if (id == Prefix + "weakpoint_snipe") plan.range += 2;
            List<Card> enemies = Enemies(data, actor, actor.slot, plan.range);
            if (id == Prefix + "mobile_shot" && enemies.Count == 0)
            {
                var paths = Reachable(data, actor, Mathf.Max(0, actor.move_Range));
                List<Card> all = Enemies(data, actor, actor.slot, int.MaxValue);
                SortTargets(all, actor.slot, "nearest");
                foreach (Card enemy in all)
                {
                    List<Slot> best = null;
                    Slot bestSlot = Slot.None;
                    foreach (var pair in paths)
                    {
                        if (pair.Value.Count < 2 || Vc5DemoGrid.HexDistance(pair.Key, enemy.slot) > plan.range) continue;
                        if (best == null || CompareLanding(pair.Key, pair.Value.Count, bestSlot, best.Count) < 0)
                        { best = pair.Value; bestSlot = pair.Key; }
                    }
                    if (best == null) continue;
                    plan.path.AddRange(best);
                    plan.destination = bestSlot;
                    enemies.Add(enemy);
                    break;
                }
            }
            string order = id == Prefix + "heavy_break" ? "highest" : id == Prefix + "weakpoint_snipe" ? "lowest" : "nearest";
            SortTargets(enemies, plan.destination, order);
            if (id == Prefix + "fire_coverage") plan.targets.AddRange(enemies);
            else if (enemies.Count > 0) plan.targets.Add(enemies[0]);
            plan.valid = plan.targets.Count > 0;
            plan.reason = plan.valid ? "松开执行" : id == Prefix + "mobile_shot" ? "移动后仍无目标" : "范围内没有敌人";
            return plan;
        }

        // Breadth-first paths use only real, unoccupied hexes, in stable neighbor order.
        public static List<Slot> VisualPath(Game data, Card actor, Slot previous)
        {
            var paths = Reachable(data, actor, 34, previous);
            return paths.TryGetValue(actor.slot, out List<Slot> path) ? path : new List<Slot>();
        }

        public static Dictionary<Slot, List<Slot>> Reachable(Game data, Card actor, int range, Slot? origin = null)
        {
            var paths = new Dictionary<Slot, List<Slot>>();
            if (actor == null || !actor.CanMove(true)) return paths;
            var queue = new Queue<Slot>();
            Slot start = origin ?? actor.slot;
            paths[start] = new List<Slot> { start };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Slot from = queue.Dequeue();
                if (paths[from].Count - 1 >= range) continue;
                foreach (Slot next in Vc5DemoGrid.NeighborSlots(from))
                {
                    if (paths.ContainsKey(next) || !Vc5DemoGrid.IsEmptyBoardSlot(data, next, actor)) continue;
                    var path = new List<Slot>(paths[from]) { next };
                    paths[next] = path;
                    queue.Enqueue(next);
                }
            }
            return paths;
        }

        static int CompareLanding(Slot a, int stepsA, Slot b, int stepsB)
        {
            int compare = stepsA.CompareTo(stepsB);
            if (compare != 0) return compare;
            compare = Vc5ScoringZone.IsScoringCell(b.x, b.y).CompareTo(Vc5ScoringZone.IsScoringCell(a.x, a.y));
            if (compare != 0) return compare;
            Slot center = new Slot(5, 3, a.p);
            compare = Vc5DemoGrid.HexDistance(a, center).CompareTo(Vc5DemoGrid.HexDistance(b, center));
            if (compare != 0) return compare;
            compare = a.x.CompareTo(b.x);
            return compare != 0 ? compare : a.y.CompareTo(b.y);
        }

        static List<Card> Enemies(Game data, Card actor, Slot from, int range)
        {
            var list = new List<Card>();
            foreach (Player player in data.players)
                if (player.player_id != actor.player_id)
                    foreach (Card card in player.cards_board)
                        if (card.GetHP() > 0 && Vc5DemoGrid.HexDistance(from, card.slot) <= range) list.Add(card);
            return list;
        }

        static void SortTargets(List<Card> cards, Slot from, string order)
        {
            // List.Sort is not stable; retain the authoritative board insertion order for exact ties.
            var original = new List<Card>(cards);
            cards.Sort((a, b) =>
            {
                int hp = a.GetHP().CompareTo(b.GetHP());
                int dist = Vc5DemoGrid.HexDistance(from, a.slot).CompareTo(Vc5DemoGrid.HexDistance(from, b.slot));
                int first = order == "highest" ? -hp : order == "lowest" ? hp : dist;
                int second = order == "nearest" ? hp : dist;
                return first != 0 ? first : second != 0 ? second : original.IndexOf(a).CompareTo(original.IndexOf(b));
            });
        }

        public static void OnMoved(GameLogic logic, Card actor, int distance)
        {
            if (distance <= 0) return;
            if (actor.card_id == Sniper) SetFlag(actor, StatusType.Vc5C3MovedThisTurn, 1);
            if (!actor.CanDoAbilities()) return;
            if (actor.card_id == Ranger) SetFlag(actor, StatusType.Vc5C3MobileFire, 0);
            if (actor.card_id != Guard || actor.HasStatus(StatusType.Vc5C3GuardMoveUsed)) return;
            SetFlag(actor, StatusType.Vc5C3GuardMoveUsed, 1);
            List<Card> targets = Enemies(logic.GameData, actor, actor.slot, Vc5DemoGrid.AttackRange(actor));
            SortTargets(targets, actor.slot, "nearest");
            if (targets.Count > 0) logic.DamageCard(actor, targets[0], 1);
        }

        static void SetFlag(Card actor, StatusType type, int duration)
        {
            actor.RemoveStatus(type);
            actor.AddStatus(type, 1, duration);
        }

        public static int CardDamage(Card actor, int bonus)
        {
            if (actor.CanDoAbilities())
            {
                if (actor.card_id == Ranger && actor.HasStatus(StatusType.Vc5C3MobileFire)) bonus++;
                if (actor.card_id == Sniper && !actor.HasStatus(StatusType.Vc5C3MovedThisTurn)) bonus++;
            }
            return Mathf.Max(0, actor.GetAttack() + bonus);
        }

        public static void Execute(GameLogic logic, Card spell, Card actor, Slot? selected = null)
        {
            Vc5C3Plan plan = Plan(logic.GameData, spell, actor, selected);
            if (!plan.valid) return;
            if (plan.path.Count > 1) logic.MoveCard(actor, plan.destination, true, true);
            if (IsPreciseMove(spell)) return;
            if (spell.card_id == Prefix + "temp_calibration") { SetFlag(actor, StatusType.Vc5C3TemporaryRange, 1); return; }
            if (spell.card_id == Prefix + "scope_upgrade") { actor.AddStatus(StatusType.Vc5C3PermanentRange, 1, 0); return; }
            if (logic.GameData.HasEnded()) return;
            if (spell.card_id == Prefix + "mobile_shot" && plan.targets.Count > 0 && !logic.GameData.IsOnBoard(plan.targets[0]))
            {
                plan.targets.Clear();
                var remaining = Enemies(logic.GameData, actor, actor.slot, plan.range);
                SortTargets(remaining, actor.slot, "nearest");
                if (remaining.Count > 0) plan.targets.Add(remaining[0]);
            }
            int damage = CardDamage(actor, spell.card_id == Prefix + "heavy_break" ? 1 : 0);
            bool hasMobileFire = actor.CanDoAbilities() && actor.HasStatus(StatusType.Vc5C3MobileFire);
            bool dealtDamage = false;
            foreach (Card target in plan.targets)
            {
                if (logic.GameData.HasEnded()) break;
                if (!logic.GameData.IsOnBoard(target)) continue;
                int before = target.damage;
                logic.DamageCard(actor, target, damage);
                dealtDamage |= target.damage > before || !logic.GameData.IsOnBoard(target);
            }
            if (hasMobileFire && dealtDamage) actor.RemoveStatus(StatusType.Vc5C3MobileFire);
        }
    }
}
