using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.AI
{
    public static class Vc5DemoAIPlanner
    {
        private static readonly HashSet<string> AttackCards = new HashSet<string>
        {
            "vc5_demo_basic_attack",
            "vc5_demo_shoot",
            "vc5_demo_aimed_shot",
            "vc5_demo_focus_fire",
            "vc5_demo_ranged_suppression",
        };

        private static readonly HashSet<string> EngageCards = new HashSet<string>
        {
            "vc5_demo_charge_attack",
            "vc5_demo_decisive_charge",
            "vc5_demo_close_assault",
            "vc5_demo_pursuit",
        };

        private static readonly HashSet<string> MoveTowardCards = new HashSet<string>
        {
            "vc5_demo_raid",
            "vc5_demo_full_speed_advance",
            "vc5_demo_quick_move",
            "vc5_demo_line_advance",
            "vc5_demo_high_ground",
        };

        private static readonly HashSet<string> ProtectCards = new HashSet<string>
        {
            "vc5_demo_protect_shooter",
        };

        public static AIAction ChooseNextAction(Game data, int playerId)
        {
            if (data == null)
                return null;

            if (data.selector != SelectorType.None && data.selector_player_id == playerId)
                return ChooseSelectorAction(data, playerId);

            Player player = data.GetPlayer(playerId);
            if (player == null || !data.IsPlayerActionTurn(player))
                return CreateAction(GameAction.EndTurn);

            AIAction cardAction = ChooseCardToPlay(data, player);
            if (cardAction != null)
                return cardAction;

            AIAction abilityAction = ChooseActivatedAbility(data, player);
            if (abilityAction != null)
                return abilityAction;

            return CreateAction(GameAction.EndTurn);
        }

        private static AIAction ChooseCardToPlay(Game data, Player player)
        {
            Card best = null;
            int bestScore = int.MinValue;
            Slot bestSlot = Slot.None;

            foreach (Card card in player.cards_hand)
            {
                Slot slot = ChoosePlaySlotForCard(data, player, card);
                if (!data.CanPlayCard(card, slot))
                    continue;

                int score = ScorePlayableCard(data, player, card);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                    bestSlot = slot;
                }
            }

            if (best == null)
                return null;

            AIAction action = CreateAction(GameAction.PlayCard, best);
            action.slot = bestSlot;
            return action;
        }

        private static Slot ChoosePlaySlotForCard(Game data, Player player, Card card)
        {
            if (card == null)
                return Slot.None;

            if (card.CardData.IsBoardCard())
                return player.GetRandomEmptySlot(new System.Random());

            Card preferred = ChoosePreferredAlly(data, player, card.card_id, null);
            return preferred != null ? preferred.slot : Slot.None;
        }

        private static int ScorePlayableCard(Game data, Player player, Card card)
        {
            string id = card.card_id;
            Card ally = ChoosePreferredAlly(data, player, id, null);
            Card enemy = ChoosePreferredEnemy(data, player, ally, id, null);

            if (id == "vc5_demo_volley")
                return ChooseEnemyForVolley(data, player) != null ? 980 : int.MinValue;

            if (AttackCards.Contains(id) || EngageCards.Contains(id))
            {
                if (ally == null || enemy == null)
                    return int.MinValue;

                int score = 700 + card.GetMana() * 20;
                if (CanLikelyKill(id, ally, enemy))
                    score += 350;
                if (EngageCards.Contains(id))
                    score += 80;
                return score;
            }

            if (MoveTowardCards.Contains(id) && ally != null)
                return 450 + DistanceToScoring(ally.slot) * 30 + (card.CardData.fast_action ? 80 : 0);

            if (ProtectCards.Contains(id) && ally != null)
                return 360 + (ally.GetHPMax() - ally.GetHP()) * 40;

            if (id == "vc5_demo_fallback" || id == "vc5_demo_retreat_2")
                return ally != null && IsThreatened(data, ally) ? 420 : 120;

            return 100 + card.GetMana() * 10;
        }

        private static AIAction ChooseActivatedAbility(Game data, Player player)
        {
            foreach (Card card in player.cards_board)
            {
                foreach (AbilityData ability in card.GetAbilities())
                {
                    if (ability.trigger == AbilityTrigger.Activate && data.CanCastAbility(card, ability) && ability.HasValidSelectTarget(data, card))
                    {
                        AIAction action = CreateAction(GameAction.CastAbility, card);
                        action.ability_id = ability.id;
                        return action;
                    }
                }
            }
            return null;
        }

        private static AIAction ChooseSelectorAction(Game data, int playerId)
        {
            Card caster = data.GetCard(data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(data.selector_ability_id);
            Player player = data.GetPlayer(playerId);
            if (caster == null || ability == null || player == null)
                return CreateAction(GameAction.CancelSelect, caster);

            if (data.selector == SelectorType.SelectTarget)
            {
                Card card = ChooseCardTarget(data, player, caster, ability);
                if (card != null)
                {
                    AIAction action = CreateAction(GameAction.SelectCard, caster);
                    action.target_uid = card.uid;
                    return action;
                }

                Slot slot = ChooseSlotTarget(data, player, caster, ability);
                if (slot.IsValid())
                {
                    AIAction action = CreateAction(GameAction.SelectSlot, caster);
                    action.slot = slot;
                    return action;
                }
            }

            if (data.selector == SelectorType.SelectorChoice && ability.chain_abilities.Length > 0)
            {
                AIAction action = CreateAction(GameAction.SelectChoice, caster);
                action.value = 0;
                return action;
            }

            if (data.selector == SelectorType.SelectorCost)
            {
                AIAction action = CreateAction(GameAction.SelectCost, caster);
                action.value = Mathf.Clamp(player.mana, 0, 9);
                return action;
            }

            return CreateAction(GameAction.CancelSelect, caster);
        }

        private static Card ChooseCardTarget(Game data, Player player, Card caster, AbilityData ability)
        {
            string sourceCardId = caster.card_id;
            List<Card> candidates = new List<Card>();
            foreach (Player p in data.players)
            {
                foreach (Card card in p.cards_board)
                {
                    if (ability.CanTarget(data, caster, card))
                        candidates.Add(card);
                }
            }

            if (candidates.Count == 0)
                return null;

            bool selectingAlly = candidates[0].player_id == player.player_id;
            if (selectingAlly)
                return ChoosePreferredAlly(data, player, sourceCardId, candidates);

            Card triggerer = data.GetCard(data.ability_triggerer);
            return ChoosePreferredEnemy(data, player, triggerer, sourceCardId, candidates);
        }

        private static Slot ChooseSlotTarget(Game data, Player player, Card caster, AbilityData ability)
        {
            Card mover = data.GetCard(data.ability_triggerer);
            Slot best = Slot.None;
            int bestScore = int.MinValue;

            foreach (Slot slot in Slot.GetAll())
            {
                if (!ability.CanTarget(data, caster, slot))
                    continue;

                int score = mover != null ? -DistanceToScoring(slot) * 100 + Vc5DemoGrid.HexDistance(mover.slot, slot) : 0;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = slot;
                }
            }

            return best;
        }

        private static Card ChoosePreferredAlly(Game data, Player player, string sourceCardId, List<Card> candidates)
        {
            IEnumerable<Card> source = candidates ?? player.cards_board;
            Card best = null;
            int bestScore = int.MinValue;

            foreach (Card ally in source)
            {
                if (ally == null || ally.player_id != player.player_id || !ally.CardData.IsCharacter())
                    continue;

                int score = ally.GetAttack() * 30 + ally.GetHP() * 4;
                if (AttackCards.Contains(sourceCardId) || EngageCards.Contains(sourceCardId))
                {
                    Card enemy = ChoosePreferredEnemy(data, player, ally, sourceCardId, null);
                    if (enemy == null)
                        score -= 1000;
                    else
                        score += CanLikelyKill(sourceCardId, ally, enemy) ? 500 : 150;
                }
                if (MoveTowardCards.Contains(sourceCardId))
                    score += DistanceToScoring(ally.slot) * 50;
                if (ProtectCards.Contains(sourceCardId))
                    score += (ally.GetHPMax() - ally.GetHP()) * 80 + (ally.card_id == "vc5_demo_sniper" ? 80 : 0);
                if (sourceCardId == "vc5_demo_fallback" || sourceCardId == "vc5_demo_retreat_2")
                    score += IsThreatened(data, ally) ? 300 : -50;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = ally;
                }
            }

            return best;
        }

        private static Card ChoosePreferredEnemy(Game data, Player player, Card ally, string sourceCardId, List<Card> candidates)
        {
            IEnumerable<Card> source = candidates ?? GetEnemyCards(data, player.player_id);
            Card best = null;
            int bestScore = int.MinValue;

            foreach (Card enemy in source)
            {
                if (enemy == null || enemy.player_id == player.player_id || !enemy.CardData.IsBoardCard())
                    continue;

                if (ally != null && candidates == null && !IsLikelyValidEnemyTarget(data, ally, enemy, sourceCardId))
                    continue;

                int score = enemy.GetAttack() * 50 - enemy.GetHP() * 8;
                if (ally != null && CanLikelyKill(sourceCardId, ally, enemy))
                    score += 500;
                if (Vc5ScoringZone.IsScoringCell(enemy.slot.x, enemy.slot.y))
                    score += 80;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        private static Card ChooseEnemyForVolley(Game data, Player player)
        {
            Card best = null;
            int bestScore = int.MinValue;
            foreach (Card enemy in GetEnemyCards(data, player.player_id))
            {
                int attackers = 0;
                int damage = 0;
                foreach (Card ally in player.cards_board)
                {
                    if (ally.CardData.IsCharacter() && Vc5DemoGrid.InAttackRange(ally, enemy))
                    {
                        attackers++;
                        damage += ally.GetAttack();
                    }
                }
                int score = attackers * 200 + damage * 20 - enemy.GetHP() * 5;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = attackers > 0 ? enemy : null;
                }
            }
            return best;
        }

        private static IEnumerable<Card> GetEnemyCards(Game data, int playerId)
        {
            foreach (Player p in data.players)
            {
                if (p.player_id == playerId)
                    continue;
                foreach (Card card in p.cards_board)
                    yield return card;
            }
        }

        private static bool IsLikelyValidEnemyTarget(Game data, Card ally, Card enemy, string sourceCardId)
        {
            if (sourceCardId == "vc5_demo_volley")
                return Vc5DemoGrid.InAttackRange(ally, enemy);
            if (AttackCards.Contains(sourceCardId))
            {
                int offset = sourceCardId == "vc5_demo_aimed_shot" ? 1 : 0;
                return Vc5DemoGrid.HexDistance(ally.slot, enemy.slot) <= Vc5DemoGrid.AttackRange(ally) + offset;
            }
            if (EngageCards.Contains(sourceCardId))
            {
                if (sourceCardId == "vc5_demo_pursuit" && enemy.GetHP() >= enemy.GetHPMax())
                    return false;
                int maxMove = sourceCardId == "vc5_demo_charge_attack" ? 2 : -1;
                return Vc5DemoGrid.FindBestAdjacentSlot(data, ally, enemy, maxMove, out _);
            }
            return true;
        }

        private static bool CanLikelyKill(string sourceCardId, Card ally, Card enemy)
        {
            int bonus = sourceCardId == "vc5_demo_decisive_charge" || sourceCardId == "vc5_demo_ranged_suppression" ? 1 : 0;
            return ally.GetAttack() + bonus >= enemy.GetHP();
        }

        private static bool IsThreatened(Game data, Card ally)
        {
            foreach (Card enemy in GetEnemyCards(data, ally.player_id))
            {
                if (Vc5DemoGrid.HexDistance(ally.slot, enemy.slot) <= Vc5DemoGrid.AttackRange(enemy) + enemy.move_Range)
                    return true;
            }
            return false;
        }

        private static int DistanceToScoring(Slot slot)
        {
            int best = int.MaxValue;
            for (int x = 4; x <= 6; x++)
            {
                for (int y = 2; y <= 4; y++)
                {
                    if (Vc5ScoringZone.IsScoringCell(x, y))
                        best = Mathf.Min(best, Vc5DemoGrid.HexDistance(slot, new Slot(x, y, slot.p)));
                }
            }
            return best == int.MaxValue ? 0 : best;
        }

        private static AIAction CreateAction(ushort type, Card card = null)
        {
            AIAction action = new AIAction();
            action.Clear();
            action.type = type;
            action.valid = true;
            if (card != null)
                action.card_uid = card.uid;
            return action;
        }
    }
}
