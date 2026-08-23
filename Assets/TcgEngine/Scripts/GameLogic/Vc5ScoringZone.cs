using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// 中央得分区 7 格，坐标以 2026-07-13 用户标注截图中的红叉为准。
    /// </summary>
    public static class Vc5ScoringZone
    {
        private static readonly SlotXY[] Cells =
        {
            new SlotXY { x = 6, y = 3 },
            new SlotXY { x = 6, y = 2 },
            new SlotXY { x = 5, y = 4 },
            new SlotXY { x = 5, y = 3 },
            new SlotXY { x = 5, y = 2 },
            new SlotXY { x = 4, y = 4 },
            new SlotXY { x = 4, y = 3 },
        };

        public static bool IsScoringCell(int x, int y)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                if (Cells[i].x == x && Cells[i].y == y)
                    return true;
            }
            return false;
        }

        public static int CountCharactersInZone(Player player)
        {
            if (player == null)
                return 0;

            int count = 0;
            foreach (Card card in player.cards_board)
            {
                if (card == null || card.GetHP() <= 0)
                    continue;
                if (!card.CardData.IsCharacter())
                    continue;
                if (IsScoringCell(card.slot.x, card.slot.y))
                    count++;
            }
            return count;
        }

        public static IReadOnlyList<SlotXY> GetCells()
        {
            return Cells;
        }
    }
}
