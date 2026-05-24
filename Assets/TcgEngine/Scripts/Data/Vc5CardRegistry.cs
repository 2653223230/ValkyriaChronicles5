using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 卡牌/英雄注册表：以 <c>Docs/vc5_card_registry/*.csv</c> 为权威清单，与游戏中 <see cref="CardData.id"/> 一一对应。
    /// <c>enabled=0</c> 表示从对局与组卡列表中移除；数值列在加载时覆盖到 <see cref="CardData"/>。
    /// </summary>
    public static class Vc5CardRegistry
    {
        private static readonly HashSet<string> disabledIds = new HashSet<string>();

        public const string RegistryFolder = "Docs/vc5_card_registry";
        public const string HeroesFileName = "heroes_registry.csv";
        public const string CardsFileName = "cards_registry.csv";

        public static readonly string[] ColumnHeaders =
        {
            "enabled", "id", "title", "series", "type",
            "mana", "hp_cost", "discard_cost", "attack", "hp", "move_Range", "attack_Range",
            "fast_action", "deckbuilding", "text", "source", "asset_path", "notes"
        };

        public class RegistryRow
        {
            public bool enabled = true;
            public string id;
            public string title;
            public string series;
            public string type;
            public int mana;
            public int hp_cost;
            public int discard_cost;
            public int attack;
            public int hp;
            public int move_Range;
            public int attack_Range;
            public bool fast_action;
            public bool deckbuilding;
            public string text;
            public string source;
            public string asset_path;
            public string notes;
        }

        public static string GetRegistryDirectory()
        {
#if UNITY_EDITOR
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, RegistryFolder.Replace('/', Path.DirectorySeparatorChar));
#else
            return Path.Combine(Application.dataPath, "..", RegistryFolder);
#endif
        }

        public static string GetHeroesPath() => Path.Combine(GetRegistryDirectory(), HeroesFileName);
        public static string GetCardsPath() => Path.Combine(GetRegistryDirectory(), CardsFileName);

        public static bool IsDisabled(string id)
        {
            return !string.IsNullOrEmpty(id) && disabledIds.Contains(id);
        }

        /// <summary>在 <see cref="Vc5SlimeBootstrap.Register"/> 之后调用。</summary>
        public static void Apply()
        {
            disabledIds.Clear();

            List<RegistryRow> heroes = LoadFile(GetHeroesPath());
            List<RegistryRow> cards = LoadFile(GetCardsPath());
            if (heroes.Count == 0 && cards.Count == 0)
            {
                Debug.LogWarning("[Vc5CardRegistry] 未找到注册表或表为空，跳过应用。请用菜单「从游戏导出到 CSV」生成。");
                return;
            }

            HashSet<string> seen = new HashSet<string>();
            ApplyRows(heroes, seen, "heroes_registry");
            ApplyRows(cards, seen, "cards_registry");

            WarnMissingFromRegistry(seen);
        }

        public static List<RegistryRow> LoadFile(string path)
        {
            List<RegistryRow> list = new List<RegistryRow>();
            string text = Vc5CsvIO.ReadTextFile(path);
            if (string.IsNullOrEmpty(text))
                return list;

            foreach (Dictionary<string, string> dict in Vc5CsvIO.ReadRows(text))
            {
                string id = Vc5CsvIO.Get(dict, "id", "").Trim();
                if (string.IsNullOrEmpty(id))
                    continue;
                RegistryRow row = new RegistryRow
                {
                    enabled = Vc5CsvIO.ParseBool01(Vc5CsvIO.Get(dict, "enabled", "1"), true),
                    id = id,
                    title = Vc5CsvIO.Get(dict, "title", ""),
                    series = Vc5CsvIO.Get(dict, "series", ""),
                    type = Vc5CsvIO.Get(dict, "type", ""),
                    mana = Vc5CsvIO.ParseInt(Vc5CsvIO.Get(dict, "mana", "0")),
                    hp_cost = Vc5CsvIO.ParseInt(Vc5CsvIO.Get(dict, "hp_cost", "0")),
                    discard_cost = Vc5CsvIO.ParseInt(Vc5CsvIO.Get(dict, "discard_cost", "0")),
                    attack = Vc5CsvIO.ParseInt(Vc5CsvIO.Get(dict, "attack", "0")),
                    hp = Vc5CsvIO.ParseInt(Vc5CsvIO.Get(dict, "hp", "0")),
                    move_Range = Vc5CsvIO.ParseInt(Vc5CsvIO.Get(dict, "move_Range", "2")),
                    attack_Range = Vc5CsvIO.ParseInt(Vc5CsvIO.Get(dict, "attack_Range", "2")),
                    fast_action = Vc5CsvIO.ParseBool01(Vc5CsvIO.Get(dict, "fast_action", "0"), false),
                    deckbuilding = Vc5CsvIO.ParseBool01(Vc5CsvIO.Get(dict, "deckbuilding", "1"), true),
                    text = Vc5CsvIO.Get(dict, "text", ""),
                    source = Vc5CsvIO.Get(dict, "source", ""),
                    asset_path = Vc5CsvIO.Get(dict, "asset_path", ""),
                    notes = Vc5CsvIO.Get(dict, "notes", ""),
                };
                list.Add(row);
            }
            return list;
        }

        public static Dictionary<string, string> RowToDict(RegistryRow row)
        {
            return new Dictionary<string, string>
            {
                { "enabled", row.enabled ? "1" : "0" },
                { "id", row.id ?? "" },
                { "title", row.title ?? "" },
                { "series", row.series ?? "" },
                { "type", row.type ?? "" },
                { "mana", row.mana.ToString() },
                { "hp_cost", row.hp_cost.ToString() },
                { "discard_cost", row.discard_cost.ToString() },
                { "attack", row.attack.ToString() },
                { "hp", row.hp.ToString() },
                { "move_Range", row.move_Range.ToString() },
                { "attack_Range", row.attack_Range.ToString() },
                { "fast_action", row.fast_action ? "1" : "0" },
                { "deckbuilding", row.deckbuilding ? "1" : "0" },
                { "text", row.text ?? "" },
                { "source", row.source ?? "" },
                { "asset_path", row.asset_path ?? "" },
                { "notes", row.notes ?? "" },
            };
        }

        public static bool IsHeroEntry(CardData card)
        {
            if (card == null)
                return false;
            if (card.type == CardType.Hero)
                return true;
            if (!string.IsNullOrEmpty(card.id) && card.id.StartsWith("vc5_hero_", StringComparison.Ordinal))
                return true;
            return false;
        }

        public static RegistryRow FromCardData(CardData card, string source, string assetPath)
        {
            return new RegistryRow
            {
                enabled = true,
                id = card.id,
                title = card.title ?? "",
                series = card.series ?? "",
                type = card.type.ToString(),
                mana = card.mana,
                hp_cost = card.hp_cost,
                discard_cost = card.discard_cost,
                attack = card.attack,
                hp = card.hp,
                move_Range = card.move_Range,
                attack_Range = card.attack_Range,
                fast_action = card.fast_action,
                deckbuilding = card.deckbuilding,
                text = card.text ?? "",
                source = source,
                asset_path = assetPath ?? "",
                notes = "",
            };
        }

        public static void ApplyRowToCard(CardData card, RegistryRow row)
        {
            if (card == null || row == null)
                return;

            if (!string.IsNullOrWhiteSpace(row.title))
                card.title = row.title.Trim();
            if (!string.IsNullOrWhiteSpace(row.series))
                card.series = row.series.Trim();
            if (!string.IsNullOrWhiteSpace(row.type) && Enum.TryParse(row.type.Trim(), true, out CardType parsedType))
                card.type = parsedType;

            card.mana = row.mana;
            card.hp_cost = row.hp_cost;
            card.discard_cost = row.discard_cost;
            card.attack = row.attack;
            card.hp = row.hp;
            card.move_Range = row.move_Range;
            card.attack_Range = row.attack_Range;
            card.fast_action = row.fast_action;
            card.deckbuilding = row.deckbuilding;
            if (!string.IsNullOrEmpty(row.text))
                card.text = row.text;
        }

        private static void ApplyRows(List<RegistryRow> rows, HashSet<string> seenIds, string fileLabel)
        {
            foreach (RegistryRow row in rows)
            {
                if (string.IsNullOrEmpty(row.id))
                    continue;

                if (seenIds.Contains(row.id))
                {
                    Debug.LogError($"[Vc5CardRegistry] 重复 id={row.id}（{fileLabel}）");
                    continue;
                }
                seenIds.Add(row.id);

                CardData card = CardData.Get(row.id);
                if (card == null)
                {
                    Debug.LogWarning($"[Vc5CardRegistry] CSV 中有 id={row.id}，但游戏中未加载对应 CardData（{fileLabel}）");
                    continue;
                }

                if (!row.enabled)
                {
                    disabledIds.Add(row.id);
                    CardData.Unregister(row.id);
                    continue;
                }

                ApplyRowToCard(card, row);
            }
        }

        private static void WarnMissingFromRegistry(HashSet<string> registryIds)
        {
            List<CardData> all = CardData.GetAll();
            foreach (CardData card in all)
            {
                if (card == null || string.IsNullOrEmpty(card.id))
                    continue;
                if (!registryIds.Contains(card.id))
                    Debug.LogWarning($"[Vc5CardRegistry] 游戏中已加载 id={card.id}，但注册表 CSV 中无此行。请执行「从游戏导出到 CSV」保持一一对应。");
            }
        }

        /// <summary>编辑器：将当前内存中的卡牌状态写回 CSV（合并保留 enabled/notes）。</summary>
        public static void SaveMerged(List<RegistryRow> heroes, List<RegistryRow> cards)
        {
            List<Dictionary<string, string>> heroDicts = new List<Dictionary<string, string>>();
            foreach (RegistryRow r in heroes)
                heroDicts.Add(RowToDict(r));
            List<Dictionary<string, string>> cardDicts = new List<Dictionary<string, string>>();
            foreach (RegistryRow r in cards)
                cardDicts.Add(RowToDict(r));

            Vc5CsvIO.WriteTextFileUtf8Bom(GetHeroesPath(), Vc5CsvIO.WriteRows(ColumnHeaders, heroDicts));
            Vc5CsvIO.WriteTextFileUtf8Bom(GetCardsPath(), Vc5CsvIO.WriteRows(ColumnHeaders, cardDicts));
        }

        public static Dictionary<string, RegistryRow> LoadMergedLookup()
        {
            Dictionary<string, RegistryRow> map = new Dictionary<string, RegistryRow>();
            foreach (RegistryRow r in LoadFile(GetHeroesPath()))
                map[r.id] = r;
            foreach (RegistryRow r in LoadFile(GetCardsPath()))
                map[r.id] = r;
            return map;
        }
    }
}
