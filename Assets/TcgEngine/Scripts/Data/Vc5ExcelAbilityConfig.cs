using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Lightweight CSV reader for VC5 seed tables (cards/abilities/effects).
    /// Reads from Docs/vc5_excel_seed_v1 in Editor; falls back to Resources/Data text assets.
    /// </summary>
    public class Vc5ExcelAbilityConfig
    {
        public class CardRow
        {
            public string card_id;
            public string display_name;
            public string fields;
            public int cost_mana;
            public int cost_hp;
            public int cost_discard;
            public bool fast_action;
            public bool is_response;
            public string effect_key;
            public string player_read_text;
            public int? opt_attack;
            public int? opt_hp;
            public int? opt_move_Range;
            public int? opt_attack_Range;
        }

        public class AbilityRow
        {
            public string ability_id;
            public string card_id;
            public string trigger;
            public string target_rule;
            public int value;
            public int mana_cost;
            public int hp_cost;
            public int discard_cost;
            public int uses_per_turn;
            public string effect_set_id;
            public string condition_triggerer_trait;
            public int condition_triggerer_trait_value;
            public string chain_next;
            public string status_type;
            public int status_value;
            public int status_duration;
        }

        public class EffectRow
        {
            public string effect_set_id;
            public int order;
            public string effect_key;
            public int param_int_a;
            public int param_int_b;
            public int param_int_c;
            public string param_str_a;
        }

        /// <summary>
        /// Parsed row from heroes.csv — 黏黏等运行时注册英雄的基础数值以该表为准（非 cards.csv）。
        /// </summary>
        public class HeroRow
        {
            public string hero_id;
            public int attack;
            public int hp;
            public int move_Range;
            public int attack_Range;
            public string player_read_text;
        }

        private readonly Dictionary<string, CardRow> cards = new Dictionary<string, CardRow>();
        private readonly Dictionary<string, AbilityRow> abilities = new Dictionary<string, AbilityRow>();
        private readonly Dictionary<string, List<EffectRow>> effects = new Dictionary<string, List<EffectRow>>();
        private readonly Dictionary<string, HeroRow> heroes = new Dictionary<string, HeroRow>();

        public static Vc5ExcelAbilityConfig Load()
        {
            Vc5ExcelAbilityConfig cfg = new Vc5ExcelAbilityConfig();
            cfg.LoadInternal();
            return cfg;
        }

        public CardRow GetCard(string cardId)
        {
            cards.TryGetValue(cardId, out CardRow row);
            return row;
        }

        public HeroRow GetHero(string heroId)
        {
            heroes.TryGetValue(heroId, out HeroRow row);
            return row;
        }

        public AbilityRow GetAbility(string abilityId)
        {
            abilities.TryGetValue(abilityId, out AbilityRow row);
            return row;
        }

        public EffectRow GetFirstEffect(string effectSetId)
        {
            if (effects.TryGetValue(effectSetId, out List<EffectRow> rows) && rows.Count > 0)
                return rows[0];
            return null;
        }

        public void ForEachCard(Action<string, CardRow> fn)
        {
            foreach (KeyValuePair<string, CardRow> kv in cards)
                fn(kv.Key, kv.Value);
        }

        public void ForEachHero(Action<string, HeroRow> fn)
        {
            foreach (KeyValuePair<string, HeroRow> kv in heroes)
                fn(kv.Key, kv.Value);
        }

        private void LoadInternal()
        {
            LoadCards();
            LoadHeroes();
            LoadAbilities();
            LoadEffects();
        }

        private void LoadCards()
        {
            List<Dictionary<string, string>> rows = ReadCsvRows("cards.csv");
            foreach (Dictionary<string, string> r in rows)
            {
                string cid = Get(r, "card_id", "");
                if (string.IsNullOrEmpty(cid))
                    continue;
                CardRow row = new CardRow
                {
                    card_id = cid,
                    display_name = Get(r, "名称", ""),
                    fields = Get(r, "字段", ""),
                    cost_mana = ParseInt(Get(r, "cost_mana", "0")),
                    cost_hp = ParseInt(Get(r, "cost_hp", "0")),
                    cost_discard = ParseInt(Get(r, "cost_discard", "0")),
                    fast_action = ParseInt(Get(r, "快速行动", "0")) != 0,
                    is_response = ParseInt(Get(r, "响应牌", "0")) != 0,
                    effect_key = Get(r, "effect_key", ""),
                    player_read_text = Get(r, "player_read_text", ""),
                };
                row.opt_attack = ReadOptionalInt(r, "攻击力");
                row.opt_hp = ReadOptionalInt(r, "生命值");
                row.opt_move_Range = ReadOptionalInt(r, "移动力");
                row.opt_attack_Range = ReadOptionalInt(r, "攻击距离");
                cards[cid] = row;
            }
        }

        private void LoadHeroes()
        {
            List<Dictionary<string, string>> rows = ReadCsvRows("heroes.csv");
            foreach (Dictionary<string, string> r in rows)
            {
                string hid = Get(r, "hero_id", "");
                if (string.IsNullOrEmpty(hid))
                    continue;
                HeroRow row = new HeroRow
                {
                    hero_id = hid,
                    attack = ParseInt(Get(r, "攻击力", "0")),
                    hp = ParseInt(Get(r, "生命值", "0")),
                    move_Range = ParseInt(Get(r, "移动力", "0")),
                    attack_Range = ParseInt(Get(r, "攻击距离", "0")),
                    player_read_text = Get(r, "player_read_text", ""),
                };
                heroes[hid] = row;
            }
        }

        private void LoadAbilities()
        {
            List<Dictionary<string, string>> rows = ReadCsvRows("abilities.csv");
            foreach (Dictionary<string, string> r in rows)
            {
                string aid = Get(r, "ability_id", "");
                if (string.IsNullOrEmpty(aid))
                    continue;
                AbilityRow row = new AbilityRow
                {
                    ability_id = aid,
                    card_id = Get(r, "card_id", ""),
                    trigger = Get(r, "trigger", ""),
                    target_rule = Get(r, "target_rule", ""),
                    value = ParseInt(Get(r, "value", "0")),
                    mana_cost = ParseInt(Get(r, "mana_cost", "0")),
                    hp_cost = ParseInt(Get(r, "hp_cost", "0")),
                    discard_cost = ParseInt(Get(r, "discard_cost", "0")),
                    uses_per_turn = ParseInt(Get(r, "uses_per_turn", "0")),
                    effect_set_id = Get(r, "effect_set_id", ""),
                    condition_triggerer_trait = Get(r, "condition_triggerer_trait", ""),
                    condition_triggerer_trait_value = ParseInt(Get(r, "condition_triggerer_trait_value", "1")),
                    chain_next = Get(r, "chain_next", ""),
                    status_type = Get(r, "status_type", ""),
                    status_value = ParseInt(Get(r, "status_value", "0")),
                    status_duration = ParseInt(Get(r, "status_duration", "0")),
                };
                abilities[aid] = row;
            }
        }

        private void LoadEffects()
        {
            List<Dictionary<string, string>> rows = ReadCsvRows("effects.csv");
            foreach (Dictionary<string, string> r in rows)
            {
                string sid = Get(r, "effect_set_id", "");
                if (string.IsNullOrEmpty(sid))
                    continue;

                EffectRow row = new EffectRow
                {
                    effect_set_id = sid,
                    order = ParseInt(Get(r, "order", "0")),
                    effect_key = Get(r, "effect_key", ""),
                    param_int_a = ParseInt(Get(r, "param_int_a", "0")),
                    param_int_b = ParseInt(Get(r, "param_int_b", "0")),
                    param_int_c = ParseInt(Get(r, "param_int_c", "0")),
                    param_str_a = Get(r, "param_str_a", ""),
                };

                if (!effects.ContainsKey(sid))
                    effects[sid] = new List<EffectRow>();
                effects[sid].Add(row);
            }

            foreach (List<EffectRow> setRows in effects.Values)
                setRows.Sort((a, b) => a.order.CompareTo(b.order));
        }

        private static List<Dictionary<string, string>> ReadCsvRows(string fileName)
        {
            string csv = LoadCsvText(fileName);
            if (string.IsNullOrEmpty(csv))
                return new List<Dictionary<string, string>>();

            List<string[]> rawRows = ParseCsv(csv);
            if (rawRows.Count < 2)
                return new List<Dictionary<string, string>>();

            string[] headers = rawRows[0];
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            for (int i = 1; i < rawRows.Count; i++)
            {
                string[] values = rawRows[i];
                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int c = 0; c < headers.Length; c++)
                {
                    string key = headers[c];
                    string val = c < values.Length ? values[c] : "";
                    row[key] = val;
                }
                result.Add(row);
            }
            return result;
        }

        private static string LoadCsvText(string fileName)
        {
#if UNITY_EDITOR
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string docsPath = Path.Combine(projectRoot, "Docs", "vc5_excel_seed_v1", fileName);
            if (File.Exists(docsPath))
                return ReadTextFileWithEncodingFallback(docsPath);
#endif
            TextAsset ta = Resources.Load<TextAsset>("Data/" + Path.GetFileNameWithoutExtension(fileName));
            return ta != null ? ta.text : "";
        }

        /// <summary>
        /// 优先 UTF-8（含 BOM）；严格解码失败时按 GBK 读（兼容 WPS「ANSI/简体中文」另存）。
        /// </summary>
        private static string ReadTextFileWithEncodingFallback(string path)
        {
            byte[] b = File.ReadAllBytes(path);
            if (b.Length == 0)
                return string.Empty;

            int off = 0;
            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
                off = 3;

            Encoding strictUtf8 = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            try
            {
                return strictUtf8.GetString(b, off, b.Length - off);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(936).GetString(b);
            }
        }

        private static List<string[]> ParseCsv(string text)
        {
            if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF')
                text = text.Substring(1);

            List<string[]> rows = new List<string[]>();
            List<string> row = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        row.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else if (c == '\n')
                    {
                        row.Add(sb.ToString());
                        sb.Length = 0;
                        if (HasContent(row))
                            rows.Add(row.ToArray());
                        row.Clear();
                    }
                    else if (c != '\r')
                    {
                        sb.Append(c);
                    }
                }
            }

            row.Add(sb.ToString());
            if (HasContent(row))
                rows.Add(row.ToArray());
            return rows;
        }

        private static bool HasContent(List<string> row)
        {
            foreach (string s in row)
            {
                if (!string.IsNullOrWhiteSpace(s))
                    return true;
            }
            return false;
        }

        private static string Get(Dictionary<string, string> row, string key, string def)
        {
            return row.ContainsKey(key) && !string.IsNullOrEmpty(row[key]) ? row[key] : def;
        }

        private static int ParseInt(string s)
        {
            return int.TryParse(s, out int v) ? v : 0;
        }

        private static int? ReadOptionalInt(Dictionary<string, string> r, string col)
        {
            if (r == null || !r.ContainsKey(col) || string.IsNullOrWhiteSpace(r[col]))
                return null;
            return int.TryParse(r[col].Trim(), out int v) ? (int?)v : null;
        }
    }
}
