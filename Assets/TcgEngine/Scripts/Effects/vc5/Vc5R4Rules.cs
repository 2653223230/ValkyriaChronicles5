using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    public static class Vc5R4Rules
    {
        public const string Prefix = "vc5_demo_r4_";
        public const string Ranger = Prefix + "mobile_ranger";
        public const string Sniper = Prefix + "sniper";
        public const string Commander = Prefix + "commander";
        public const string MoveSkill = Prefix + "short_move";
        public const string PrepareSkill = Prefix + "prepare";
        public const string ChooseOrder = Prefix + "choose_order";
        public const string WatchReaction = Prefix + "watch_reaction";
        public static readonly string[] Orders = { Prefix + "advance_order", Prefix + "fire_order", Prefix + "cover_order" };

        public static bool IsTemporary(Card card) { return card != null && IsTemporary(card.card_id); }
        public static bool IsTemporary(string id) { return id == Orders[0] || id == Orders[1] || id == Orders[2]; }
        public static bool IsSkill(string id) { return id == MoveSkill || id == PrepareSkill; }
        public static bool CanDiscard(Game data, Card caster, Card target)
        {
            return target != null && target.player_id == caster.player_id && !IsTemporary(target)
                && data.GetPlayer(caster.player_id).cards_hand.Contains(target);
        }
        public static Card CommanderFor(Game data, Card order) { return data.GetCard(order.r4_commander_uid); }
        public static bool InCommandRange(Game data, Card order, Card actor)
        {
            Card source = CommanderFor(data, order);
            return source != null && source.card_id == Commander && source.player_id == order.player_id
                && data.IsOnBoard(source) && source.GetHP() > 0 && actor != source
                && Vc5DemoGrid.HexDistance(source.slot, actor.slot) <= Vc5DemoGrid.AttackRange(source);
        }
        public static void Shield(Card actor, int amount)
        {
            actor.r4_shield = Mathf.Max(actor.r4_shield, amount);
            actor.r4_shield_rounds = 2;
        }
        public static void StackShield(Card actor, int amount)
        {
            actor.r4_shield += amount;
            actor.r4_shield_rounds = 2;
        }
        public static int Absorb(Card actor, int damage)
        {
            int absorbed = Mathf.Min(Mathf.Max(damage, 0), actor.r4_shield);
            actor.r4_shield -= absorbed;
            return damage - absorbed;
        }
        public static void ExpireWatch(Game data, int playerId)
        {
            foreach (Card card in data.GetPlayer(playerId).cards_board) card.r4_watch = false;
        }
        public static void EndMainPhase(Game data)
        {
            foreach (Player player in data.players)
            {
                player.cards_hand.RemoveAll(IsTemporary);
            }
        }
        public static void EndRound(Game data)
        {
            foreach (Player player in data.players)
            {
                ExpireWatch(data, player.player_id);
                foreach (Card card in player.cards_board)
                    if (card.r4_shield_rounds > 0 && --card.r4_shield_rounds == 0) card.r4_shield = 0;
            }
        }

        public static void DeployWatch(GameLogic logic, Card watcher)
        {
            watcher.r4_watch = true;
            Card target = Vc5C3Rules.NearestEnemy(logic.GameData, watcher, watcher.slot);
            if (target != null && watcher.CanDoAbilities()) FireWatch(logic, watcher, target);
        }

        static void FireWatch(GameLogic logic, Card watcher, Card target)
        {
            watcher.r4_watch = false;
            logic.onAbilityTargetCard?.Invoke(AbilityData.Get(WatchReaction), watcher, target);
            logic.DamageCard(watcher, target, 1);
        }

        // Resolve after movement reaches its chosen destination. This deliberately also covers a unit
        // that started inside the range and repositioned inside it, matching the visible card wording.
        // Restore the start for the ordinary move resolver; a lethal reaction cancels later effects.
        public static bool ResolveWatchMovement(GameLogic logic, Card mover, Slot destination)
        {
            Game data = logic.GameData;
            if (mover.slot == destination) return true;
            mover.r4_watch = false;
            var watchers = new List<Card>();
            foreach (Player player in data.players)
                if (player.player_id != mover.player_id)
                    foreach (Card card in player.cards_board)
                        if (card.r4_watch) watchers.Add(card);
            if (watchers.Count == 0) return true;
            var paths = Vc5C3Rules.Reachable(data, mover, 34);
            if (!paths.ContainsKey(destination)) return true;
            Slot start = mover.slot;
            mover.slot = destination;
            foreach (Card watcher in watchers)
            {
                int range = Vc5DemoGrid.AttackRange(watcher);
                if (!watcher.r4_watch || !data.IsOnBoard(watcher) || !watcher.CanDoAbilities()
                    || Vc5DemoGrid.HexDistance(destination, watcher.slot) > range) continue;
                FireWatch(logic, watcher, mover);
                if (!data.IsOnBoard(mover) || data.HasEnded()) return false;
            }
            mover.slot = start;
            return true;
        }
    }
}
