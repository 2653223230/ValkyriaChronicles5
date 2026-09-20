using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    public static class Vc5BAI1Rules
    {
        public const string Prefix = "vc5_demo_bai1_";
        public const string Frontliner = Prefix + "frontliner";
        public const string Flanker = Prefix + "flanker";
        public const string Rifleman = Prefix + "rifleman";
        public static bool IsCard(Card card) { return card != null && card.card_id.StartsWith(Prefix) && card.CardData.type == CardType.Spell; }
        public static bool IsAttack(Card card) { return IsCard(card) && (card.card_id.EndsWith("basic_attack") || card.card_id.EndsWith("short_charge") || card.card_id.EndsWith("suppression")); }
        public static bool IsCharge(Card card) { return IsCard(card) && card.card_id.EndsWith("short_charge"); }
        public static bool IsMove(Card card) { return IsCard(card) && !card.card_id.EndsWith("guard") && (!IsAttack(card) || IsCharge(card)); }
        public static int ShieldAmount(Card card) { return card.card_id.EndsWith("guard") ? 2 : card.card_id.EndsWith("cover_advance") || card.card_id.EndsWith("regroup") ? 1 : 0; }
        public static Card Actor(Game game) { return game.GetCard(game.ability_triggerer); }

        public static Vc5C3Plan Plan(Game game, Card spell, Card actor, Slot? destination = null, Card target = null)
        {
            var plan = new Vc5C3Plan();
            if (!IsCard(spell) || actor == null || actor.player_id != spell.player_id || !game.IsOnBoard(actor)
                || actor.GetHP() <= 0 || !actor.CardData.IsCharacter() || actor.HasStatus(StatusType.SpellImmunity)) return plan;
            plan.destination = actor.slot;
            plan.range = Vc5DemoGrid.AttackRange(actor);
            if (IsMove(spell))
            {
                int distance = spell.card_id.EndsWith("march") ? Mathf.Max(0, actor.move_Range) : spell.card_id.EndsWith("cover_advance") ? 2 : 1;
                var paths = Vc5C3Rules.Reachable(game, actor, distance);
                // A stationary attack remains legal even when the actor cannot move.
                if (IsCharge(spell) && !paths.ContainsKey(actor.slot)) paths[actor.slot] = new List<Slot> { actor.slot };
                foreach (var pair in paths)
                    if ((pair.Value.Count > 1 || IsCharge(spell)) && (!IsCharge(spell) || Targets(game, actor, pair.Key).Count > 0))
                        plan.legalSlots.Add(pair.Key);
                if (!destination.HasValue)
                { plan.valid = plan.legalSlots.Count > 0; plan.reason = plan.valid ? "选择落点" : "没有可完成的移动/攻击"; return plan; }
                if (!plan.legalSlots.Contains(destination.Value)) { plan.reason = "落点超出距离、被占用或无法完成攻击"; return plan; }
                plan.destination = destination.Value;
                plan.path.AddRange(paths[destination.Value]);
            }
            if (IsAttack(spell))
            {
                List<Card> targets = Targets(game, actor, plan.destination);
                if (target != null)
                {
                    if (!targets.Contains(target)) { plan.reason = "请选择落点射程内的敌人"; return plan; }
                    plan.targets.Add(target);
                }
                plan.valid = targets.Count > 0;
                plan.reason = plan.valid ? target == null ? "选择射程内敌人，确认后执行" : "确认攻击" : "范围内没有敌人";
            }
            else { plan.valid = true; plan.reason = "确认执行"; }
            int shield = ShieldAmount(spell);
            if (shield > 0) plan.reason += "；获得 " + shield + " 护盾，至下回合结束（不叠加）";
            return plan;
        }

        public static List<Card> Targets(Game game, Card actor, Slot from)
        {
            var targets = new List<Card>();
            foreach (Player player in game.players)
                if (player.player_id != actor.player_id)
                    foreach (Card card in player.cards_board)
                        if (card.GetHP() > 0 && Vc5DemoGrid.HexDistance(from, card.slot) <= Vc5DemoGrid.AttackRange(actor)) targets.Add(card);
            return targets;
        }

        public static int Damage(Game game, Card spell, Card actor, Card target)
        {
            int damage = Vc5C3Rules.CardDamage(actor, spell.card_id.EndsWith("suppression") ? 1 : 0);
            if (actor.card_id == Rifleman && actor.CanDoAbilities())
                foreach (Card ally in game.GetPlayer(actor.player_id).cards_board)
                    if (ally != actor && ally.GetHP() > 0 && Vc5DemoGrid.HexDistance(ally.slot, target.slot) == 1) { damage++; break; }
            return damage;
        }

        public static void Execute(GameLogic logic, Card spell, Card actor, Slot? destination = null, Card target = null)
        {
            Game game = logic.GameData;
            Vc5C3Plan plan = Plan(game, spell, actor, destination, target);
            if (!plan.valid || (IsAttack(spell) && target == null) || (IsMove(spell) && !destination.HasValue)) return;
            if (plan.path.Count > 1) logic.MoveCard(actor, plan.destination, true, true);
            if (!game.IsOnBoard(actor) || actor.GetHP() <= 0 || game.HasEnded()) return;
            int shield = ShieldAmount(spell);
            if (shield > 0) Vc5R4Rules.Shield(actor, shield);
            if (target == null || !game.IsOnBoard(target)) return;
            actor.r4_watch = false;
            int before = target.damage;
            logic.DamageCard(actor, target, Damage(game, spell, actor, target));
            if (actor.CanDoAbilities() && (target.damage > before || !game.IsOnBoard(target))) actor.RemoveStatus(StatusType.Vc5C3MobileFire);
        }

        public static void OnMoved(Card actor)
        {
            if (actor.card_id != Frontliner || !actor.CanDoAbilities() || actor.HasStatus(StatusType.Vc5C3GuardMoveUsed)) return;
            actor.AddStatus(StatusType.Vc5C3GuardMoveUsed, 1, 1);
            Vc5R4Rules.Shield(actor, 1);
        }
    }

    public class ConditionVc5BAI1Selection : ConditionData
    {
        public override bool IsTargetConditionMet(Game game, AbilityData ability, Card spell, Slot target)
        {
            return ability.id.EndsWith("_destination") && Vc5BAI1Rules.Plan(game, spell, Vc5BAI1Rules.Actor(game), target).valid;
        }
        public override bool IsTargetConditionMet(Game game, AbilityData ability, Card spell, Card target)
        {
            if (ability.id.EndsWith("_destination"))
                return Vc5BAI1Rules.IsCharge(spell) && target != null && target == Vc5BAI1Rules.Actor(game)
                    && Vc5BAI1Rules.Plan(game, spell, target, target.slot).valid;
            return ability.id.EndsWith("_target") && Vc5BAI1Rules.Plan(game, spell, Vc5BAI1Rules.Actor(game),
                Vc5BAI1Rules.IsCharge(spell) ? (Slot?)spell.bai1_destination : null, target).valid;
        }
        public override bool IsTargetConditionMet(Game game, AbilityData ability, Card spell, Player target) { return false; }
    }

    public class EffectVc5BAI1Action : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card spell, Slot target)
        {
            if (Vc5BAI1Rules.IsCharge(spell)) spell.bai1_destination = target;
            else Vc5BAI1Rules.Execute(logic, spell, Vc5BAI1Rules.Actor(logic.GameData), target);
        }
        public override void DoEffect(GameLogic logic, AbilityData ability, Card spell, Card target)
        {
            if (ability.id.EndsWith("_destination")) { spell.bai1_destination = target.slot; return; }
            if (Vc5BAI1Rules.IsAttack(spell))
                Vc5BAI1Rules.Execute(logic, spell, Vc5BAI1Rules.Actor(logic.GameData),
                    Vc5BAI1Rules.IsCharge(spell) ? (Slot?)spell.bai1_destination : null, target);
            else Vc5BAI1Rules.Execute(logic, spell, target);
        }
    }
}
