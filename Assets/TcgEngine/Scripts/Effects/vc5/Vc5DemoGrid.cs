using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    public static class Vc5DemoGrid
    {
        private const int BoardRotationXSum = 10;
        private const int BoardRotationYSum = 6;

        private static readonly int[,] NeighborOffsets =
        {
            { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 1, -1 }, { -1, 1 }
        };

        public static int HexDistance(Slot a, Slot b)
        {
            b = ToPerspective(b, a.p);
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            int dz = (a.x + a.y) - (b.x + b.y);
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
        }

        public static Slot ToPerspective(Slot slot, int perspectiveP)
        {
            if (slot.p == perspectiveP)
                return slot;

            return new Slot(BoardRotationXSum - slot.x, BoardRotationYSum - slot.y, perspectiveP);
        }

        public static int AttackRange(Card card)
        {
            return card != null ? Mathf.Max(0, card.attack_Range + card.GetStatusValue(StatusType.Vc5AttackRangeBonus)) : 0;
        }

        public static bool InAttackRange(Card attacker, Card target)
        {
            return attacker != null && target != null && HexDistance(attacker.slot, target.slot) <= AttackRange(attacker);
        }

        public static IEnumerable<Slot> NeighborSlots(Slot center)
        {
            for (int i = 0; i < NeighborOffsets.GetLength(0); i++)
                yield return new Slot(center.x + NeighborOffsets[i, 0], center.y + NeighborOffsets[i, 1], center.p);
        }

        public static Card GetDisplayedSlotCard(Game data, Slot slot, Card ignoredCard = null)
        {
            if (data == null || !slot.IsValid())
                return null;

            foreach (Player player in data.players)
            {
                if (player == null)
                    continue;

                foreach (Card card in player.cards_board)
                {
                    if (card == null || card == ignoredCard)
                        continue;

                    Slot displayedSlot = ToPerspective(card.slot, slot.p);
                    if (displayedSlot.x == slot.x && displayedSlot.y == slot.y)
                        return card;
                }
            }

            return null;
        }

        public static bool IsEmptyBoardSlot(Game data, Slot slot, Card ignoredCard = null)
        {
            return slot.IsValid() && !slot.IsPlayerSlot()
                && GetDisplayedSlotCard(data, slot, ignoredCard) == null;
        }

        public static bool FindBestAdjacentSlot(Game data, Card mover, Card target, int maxMove, out Slot bestSlot)
        {
            bestSlot = Slot.None;
            if (data == null || mover == null || target == null)
                return false;

            int allowedMove = maxMove >= 0 ? maxMove : int.MaxValue;
            int bestScore = int.MaxValue;
            Slot targetInMoverPerspective = ToPerspective(target.slot, mover.slot.p);

            foreach (Slot slot in NeighborSlots(targetInMoverPerspective))
            {
                Slot ownSlot = new Slot(slot.x, slot.y, mover.slot.p);
                if (!IsEmptyBoardSlot(data, ownSlot, mover))
                    continue;

                int moveDist = HexDistance(mover.slot, ownSlot);
                if (moveDist > allowedMove)
                    continue;

                int score = moveDist * 10 + HexDistance(ownSlot, target.slot);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestSlot = ownSlot;
                }
            }

            return bestSlot.IsValid();
        }

        public static Slot FindBestStepTowardScoring(Game data, Card card, int maxSteps)
        {
            Slot best = card.slot;
            int bestScore = DistanceToNearestScoringZone(card.slot);
            int range = Mathf.Max(1, maxSteps);

            foreach (Slot slot in CandidateSlots(card.slot, range))
            {
                if (!IsEmptyBoardSlot(data, slot))
                    continue;

                int score = DistanceToNearestScoringZone(slot);
                if (score < bestScore || (score == bestScore && HexDistance(card.slot, slot) > HexDistance(card.slot, best)))
                {
                    bestScore = score;
                    best = slot;
                }
            }

            return best;
        }

        public static Slot FindBestStepAwayFromEnemies(Game data, Card card, int maxSteps)
        {
            Slot best = card.slot;
            int bestScore = DistanceToNearestEnemy(data, card, card.slot);
            int range = Mathf.Max(1, maxSteps);

            foreach (Slot slot in CandidateSlots(card.slot, range))
            {
                if (!IsEmptyBoardSlot(data, slot))
                    continue;

                int score = DistanceToNearestEnemy(data, card, slot);
                if (score > bestScore || (score == bestScore && HexDistance(card.slot, slot) > HexDistance(card.slot, best)))
                {
                    bestScore = score;
                    best = slot;
                }
            }

            return best;
        }

        private static IEnumerable<Slot> CandidateSlots(Slot origin, int range)
        {
            for (int x = origin.x - range; x <= origin.x + range; x++)
            {
                for (int y = origin.y - range; y <= origin.y + range; y++)
                {
                    Slot slot = new Slot(x, y, origin.p);
                    if (slot.IsValid() && HexDistance(origin, slot) <= range)
                        yield return slot;
                }
            }
        }

        private static int DistanceToNearestScoringZone(Slot slot)
        {
            int best = int.MaxValue;
            foreach (SlotXY cell in Vc5ScoringZone.GetCells())
                best = Mathf.Min(best, HexDistance(slot, new Slot(cell.x, cell.y, slot.p)));
            return best == int.MaxValue ? 0 : best;
        }

        private static int DistanceToNearestEnemy(Game data, Card card, Slot slot)
        {
            int best = int.MaxValue;
            foreach (Player player in data.players)
            {
                foreach (Card other in player.cards_board)
                {
                    if (other.player_id != card.player_id)
                        best = Mathf.Min(best, HexDistance(slot, other.slot));
                }
            }

            return best == int.MaxValue ? 0 : best;
        }
    }
}
