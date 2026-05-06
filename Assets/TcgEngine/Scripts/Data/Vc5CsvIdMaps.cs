using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// 设计表（card_id / hero_id）与引擎内 <see cref="CardData.id"/> 的映射；黏黏在运行时由 <see cref="Vc5SlimeBootstrap"/> 注册，id 与表不完全一致。
    /// </summary>
    public static class Vc5CsvIdMaps
    {
        private static readonly Dictionary<string, string> DesignCardIdToEngineId = new Dictionary<string, string>
        {
            { "card_slime_heavy", "vc5_slime_heavy_strike" },
            { "card_slime_combo", "vc5_slime_combo_strike" },
            { "card_slime_pool", "vc5_slime_pool_spell" },
            { "card_slime_spray", "vc5_slime_spray" },
            { "card_slime_attach", "vc5_slime_attach" },
            { "card_slime_split", "vc5_slime_split_card" },
            { "card_slime_merge", "vc5_slime_merge_card" },
            { "card_slime_boom", "vc5_slime_boom_card" },
            { "card_slime_sharpen", "vc5_slime_sharpen" },
            { "card_slime_run", "vc5_slime_run" },
            { "card_slime_detonate", "vc5_slime_detonate_card" },
        };

        private static readonly Dictionary<string, string> DesignHeroIdToEngineId = new Dictionary<string, string>
        {
            { "hero_slime_blood", "vc5_hero_slime_blood" },
            { "hero_slime_corrosive", "vc5_hero_slime_corrosive" },
            { "hero_slime_giant", "vc5_hero_slime_giant" },
            { "hero_slime_hard", "vc5_hero_slime_hard" },
            { "hero_slime_hard2", "vc5_hero_slime_hard2" },
        };

        /// <summary>若 design id 有映射则返回引擎 id，否则返回原 id（如战士牌 card_warrior_* 与资源一致）。</summary>
        public static string GetEngineCardId(string designOrEngineId)
        {
            if (string.IsNullOrEmpty(designOrEngineId))
                return designOrEngineId;
            if (DesignCardIdToEngineId.TryGetValue(designOrEngineId, out string engineId))
                return engineId;
            return designOrEngineId;
        }

        public static string GetEngineHeroId(string designOrEngineId)
        {
            if (string.IsNullOrEmpty(designOrEngineId))
                return designOrEngineId;
            if (DesignHeroIdToEngineId.TryGetValue(designOrEngineId, out string engineId))
                return engineId;
            return designOrEngineId;
        }
    }
}
