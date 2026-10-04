using System;
using System.Collections.Generic;

namespace VC5PvE
{
    public static class DeckRules
    {
        public static int DrawToLimit(BattleState state, int limit)
        {
            if (state == null || limit < 0) return 0;
            var drawn = 0;
            while (state.Hand.Count < limit)
            {
                if (state.DrawPile.Count == 0) RecycleDiscard(state);
                if (state.DrawPile.Count == 0) break;
                var last = state.DrawPile.Count - 1;
                state.Hand.Add(state.DrawPile[last]); state.DrawPile.RemoveAt(last); drawn++;
            }
            return drawn;
        }

        public static bool TryExchange(BattleState state, string cardId, out string reason)
        {
            reason = null;
            if (state == null || state.Phase != BattlePhase.Player) { reason = "当前不能换牌"; return false; }
            if (state.FirstActionMade) { reason = "本回合已有动作"; return false; }
            if (state.ExchangedThisTurn) { reason = "本回合已换牌"; return false; }
            var index = state.Hand.FindIndex(card => card.Id == cardId);
            if (index < 0) { reason = "手牌不存在"; return false; }
            if (state.DrawPile.Count + state.DiscardPile.Count == 0) { reason = "没有其他可抽卡牌"; return false; }
            if (state.DrawPile.Count == 0) RecycleDiscard(state);
            var outgoing = state.Hand[index];
            state.Hand.RemoveAt(index);
            state.DrawPile.Insert(0, outgoing);
            var drawnIndex = state.DrawPile.Count - 1;
            var drawn = state.DrawPile[drawnIndex];
            state.DrawPile.RemoveAt(drawnIndex);
            state.Hand.Add(drawn);
            state.ExchangedThisTurn = true;
            state.Revision++;
            return true;
        }

        public static bool TryExchange(BattleState state, string cardId)
        { string ignored; return TryExchange(state, cardId, out ignored); }

        internal static void RecycleDiscard(BattleState state)
        {
            if (state.DiscardPile.Count == 0) return;
            var cards = new List<CardInstance>(state.DiscardPile);
            state.DiscardPile.Clear();
            var random = new Random(state.RandomSeed ^ (state.Round * 486187739) ^ state.Revision);
            for (var i = cards.Count - 1; i > 0; i--)
            { var j = random.Next(i + 1); var swap = cards[i]; cards[i] = cards[j]; cards[j] = swap; }
            state.DrawPile.AddRange(cards);
        }
    }
}
