using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    // A bounded one-move/one-attack heuristic, deliberately isolated from the original decks.
    public static class Vc5BAI1Planner
    {
        public sealed class Choice
        {
            public Card actor;
            public Card target;
            public Slot destination;
            public int score = int.MinValue;
        }

        public static Choice Best(Game game, Card spell, Card selectedActor = null, Slot? selectedLanding = null, bool paid = false)
        {
            var best = new Choice();
            foreach (Card actor in game.GetPlayer(spell.player_id).cards_board)
            {
                if (selectedActor != null && actor != selectedActor) continue;
                Vc5C3Plan initial = Vc5BAI1Rules.Plan(game, spell, actor);
                if (!initial.valid) continue;
                var landings = Vc5BAI1Rules.IsMove(spell) ? initial.legalSlots : new List<Slot> { actor.slot };
                foreach (Slot landing in landings)
                {
                    if (selectedLanding.HasValue && landing != selectedLanding.Value) continue;
                    var targets = Vc5BAI1Rules.IsAttack(spell) ? Vc5BAI1Rules.Targets(game, actor, landing) : new List<Card> { null };
                    foreach (Card target in targets)
                    {
                        int score = Score(game, spell, actor, landing, target, paid);
                        if (score > best.score)
                            best = new Choice { actor = actor, target = target, destination = landing, score = score };
                    }
                }
            }
            return best;
        }

        static int Score(Game game, Card spell, Card actor, Slot landing, Card target, bool paid)
        {
            Game copy = Game.CloneNew(game);
            var logic = new GameLogic(true);
            logic.SetData(copy);
            Card mover = copy.GetCard(actor.uid);
            Card victim = target == null ? null : copy.GetCard(target.uid);
            Vc5BAI1Rules.Execute(logic, copy.GetCard(spell.uid), mover,
                Vc5BAI1Rules.IsMove(spell) ? (Slot?)landing : null, victim);
            if (!copy.IsOnBoard(mover)) return int.MinValue; // no damage/score payoff after a lethal watch entry
            int positionGain = Position(copy, mover, mover.slot) - Position(game, actor, actor.slot);
            int score = 0;
            if (target != null)
            {
                int dealt = Mathf.Max(0, victim.damage - target.damage);
                score = 650 + Mathf.Min(dealt, target.GetHP()) * 40 + (!copy.IsOnBoard(victim) ? 320 : 0);
            }
            else if (Vc5BAI1Rules.IsMove(spell))
            {
                if (positionGain <= 0 && mover.r4_shield <= actor.r4_shield) return int.MinValue;
                score = 250;
            }
            else
            {
                int gain = mover.r4_shield - actor.r4_shield;
                if (gain <= 0 || Threat(game, actor, actor.slot) == 0) return int.MinValue;
                score = 340 + gain * 35 + (actor.GetHP() <= 3 ? 150 : 0);
            }
            score += positionGain - Mathf.Max(0, mover.damage - actor.damage) * 90;
            if (spell.CardData.fast_action)
            {
                int budget = game.GetPlayer(actor.player_id).mana - (paid ? 0 : spell.GetMana());
                // Only the AI's own remaining hand is inspected. No opponent hand or deck knowledge.
                foreach (Card next in game.GetPlayer(actor.player_id).cards_hand)
                {
                    if (next.uid == spell.uid || !Vc5BAI1Rules.IsAttack(next) || next.GetMana() > budget) continue;
                    if (Vc5BAI1Rules.Plan(copy, copy.GetCard(next.uid), mover).valid) { score += 650; break; }
                }
            }
            return score;
        }

        static int Position(Game game, Card actor, Slot slot)
        {
            int zoneDistance = int.MaxValue;
            foreach (Slot cell in Slot.GetAll())
            {
                Slot local = Vc5DemoGrid.ToPerspective(cell, slot.p);
                if (Vc5ScoringZone.IsScoringCell(local.x, local.y)) zoneDistance = Mathf.Min(zoneDistance, Vc5DemoGrid.HexDistance(slot, cell));
            }
            int score = -zoneDistance * (actor.card_id == Vc5BAI1Rules.Frontliner ? 65 : 25);
            int nearest = 99;
            foreach (Player player in game.players)
                if (player.player_id != actor.player_id)
                    foreach (Card enemy in player.cards_board)
                    {
                        int distance = Vc5DemoGrid.HexDistance(slot, enemy.slot);
                        nearest = Mathf.Min(nearest, distance);
                        if (actor.card_id == Vc5BAI1Rules.Rifleman && distance <= Vc5DemoGrid.AttackRange(actor))
                        {
                            score += 55;
                            foreach (Card ally in game.GetPlayer(actor.player_id).cards_board)
                                if (ally != actor && Vc5DemoGrid.HexDistance(ally.slot, enemy.slot) == 1) { score += 35; break; }
                        }
                    }
            if (actor.card_id == Vc5BAI1Rules.Flanker && nearest < 99) score -= nearest * 28;
            if (actor.card_id == Vc5BAI1Rules.Rifleman && nearest == 1) score -= 45;
            return score - Threat(game, actor, slot) * (actor.GetHP() <= 3 ? 22 : 5);
        }

        static int Threat(Game game, Card actor, Slot slot)
        {
            int threat = 0;
            foreach (Player player in game.players)
                if (player.player_id != actor.player_id)
                    foreach (Card enemy in player.cards_board)
                        if (Vc5DemoGrid.HexDistance(slot, enemy.slot) <= Vc5DemoGrid.AttackRange(enemy)) threat += enemy.GetAttack();
            return threat;
        }
    }
}
