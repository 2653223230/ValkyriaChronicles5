using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using TcgEngine;

namespace TcgEngine.EditorTool
{
    /// <summary>
    /// 从Excel文件导入卡牌和英雄数据，生成CardData资源
    /// </summary>
    public class CardDataImporter : EditorWindow
    {
        private string excelPath = "";
        private string outputFolder = "Assets/TcgEngine/Resources/Cards/vc5";
        private Vector2 scrollPosition;

        [MenuItem("TcgEngine/卡牌数据导入工具")]
        public static void ShowWindow()
        {
            GetWindow<CardDataImporter>("卡牌数据导入工具");
        }

        void OnGUI()
        {
            GUILayout.Label("卡牌数据导入工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Excel文件路径
            EditorGUILayout.LabelField("Excel文件路径:");
            EditorGUILayout.BeginHorizontal();
            excelPath = EditorGUILayout.TextField(excelPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择Excel文件", "", "xlsx,xls");
                if (!string.IsNullOrEmpty(path))
                {
                    excelPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 输出文件夹
            EditorGUILayout.LabelField("输出文件夹:");
            outputFolder = EditorGUILayout.TextField(outputFolder);

            EditorGUILayout.Space();

            // 导入按钮
            if (GUILayout.Button("导入Excel并生成CardData", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
                {
                    EditorUtility.DisplayDialog("错误", "请选择有效的Excel文件", "确定");
                    return;
                }

                ImportFromExcel(excelPath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("注意：此工具需要安装EPPlus库或使用CSV格式。\n" +
                "如果Excel读取失败，请先将Excel导出为CSV格式，然后使用CSV导入功能。\n" +
                "Docs/vc5_excel_seed_v1/cards.csv / heroes.csv 含 player_read_text 列时，将把该列写入已有 CardData.text（运行时注册的黏黏卡/英雄需同步改 Vc5SlimeBootstrap）。", MessageType.Info);

            EditorGUILayout.Space();

            // CSV导入
            if (GUILayout.Button("从CSV导入（如果Excel导入失败）", GUILayout.Height(30)))
            {
                string csvPath = EditorUtility.OpenFilePanel("选择CSV文件", "", "csv");
                if (!string.IsNullOrEmpty(csvPath))
                {
                    ImportFromCSV(csvPath);
                }
            }
        }

        private void ImportFromExcel(string excelPath)
        {
            try
            {
                // 注意：这里需要使用EPPlus或其他Excel读取库
                // 由于Unity可能不支持，我们建议使用CSV格式
                EditorUtility.DisplayDialog("提示", 
                    "Excel直接读取需要额外的库支持。\n" +
                    "建议：\n" +
                    "1. 在Excel中选择'另存为' -> 'CSV UTF-8'\n" +
                    "2. 使用'从CSV导入'功能", 
                    "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"导入失败：{e.Message}", "确定");
            }
        }

        private void ImportFromCSV(string csvPath)
        {
            try
            {
                List<string[]> rows = ParseCsvRows(csvPath);
                if (rows.Count < 2)
                {
                    EditorUtility.DisplayDialog("错误", "CSV文件格式不正确（至少需要表头和数据行）", "确定");
                    return;
                }

                string[] headers = rows[0];

                if (IsVc5CardsPlayerReadSheet(headers))
                {
                    int n = ImportVc5PlayerReadText(rows, headers);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("成功",
                        $"已根据 player_read_text 更新 {n} 个工程内 CardData 资源（未找到对应资源的行已跳过并打日志）。",
                        "确定");
                    return;
                }

                if (IsVc5HeroPlayerReadSheet(headers))
                {
                    int n = ImportVc5HeroPlayerReadText(rows, headers);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("成功",
                        $"已根据 player_read_text 更新 {n} 个工程内英雄 CardData.text（未找到资源的行已跳过并打日志）。",
                        "确定");
                    return;
                }

                bool isHero = headers.Contains("攻击力") && headers.Contains("生命值") && headers.Contains("移动力");

                if (isHero)
                    ImportHeroesFromRows(rows, headers);
                else
                    ImportCardsFromRows(rows, headers);

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("成功", $"已成功导入数据到 {outputFolder}", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"导入失败：{e.Message}\n{e.StackTrace}", "确定");
            }
        }

        /// <summary>
        /// RFC4180 风格解析：支持引号字段、字段内换行、"" 转义、UTF-8 BOM。
        /// </summary>
        private static List<string[]> ParseCsvRows(string csvPath)
        {
            string text = File.ReadAllText(csvPath, Encoding.UTF8);
            if (string.IsNullOrEmpty(text))
                return new List<string[]>();
            if (text[0] == '\uFEFF')
                text = text.Substring(1);

            List<string[]> result = new List<string[]>();
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
                        if (RowHasContent(row))
                            result.Add(row.ToArray());
                        row.Clear();
                    }
                    else if (c != '\r')
                    {
                        sb.Append(c);
                    }
                }
            }

            row.Add(sb.ToString());
            if (RowHasContent(row))
                result.Add(row.ToArray());

            return result;
        }

        private static bool RowHasContent(List<string> row)
        {
            foreach (string s in row)
            {
                if (!string.IsNullOrWhiteSpace(s))
                    return true;
            }
            return false;
        }

        private static bool IsVc5CardsPlayerReadSheet(string[] headers)
        {
            return headers.Contains("card_id") && headers.Contains("player_read_text");
        }

        private static bool IsVc5HeroPlayerReadSheet(string[] headers)
        {
            return headers.Contains("hero_id") && headers.Contains("player_read_text");
        }

        private int ImportVc5HeroPlayerReadText(List<string[]> rows, string[] headers)
        {
            Dictionary<string, CardData> index = BuildCardDataIndexById();
            int updated = 0;
            for (int i = 1; i < rows.Count; i++)
            {
                Dictionary<string, string> data = RowToDict(headers, rows[i]);
                string csvId = GetValue(data, "hero_id", "");
                string readText = GetValue(data, "player_read_text", "");
                if (string.IsNullOrEmpty(csvId))
                    continue;

                string engineId = Vc5CsvIdMaps.GetEngineHeroId(csvId);

                if (!index.TryGetValue(engineId, out CardData card))
                {
                    if (!string.IsNullOrEmpty(readText))
                        Debug.LogWarning($"[VC5 CSV] 未找到英雄 CardData 资源 id={engineId}（hero_id={csvId}）。黏黏英雄多为运行时注册，请改 Vc5SlimeBootstrap。");
                    continue;
                }

                if (string.IsNullOrEmpty(readText))
                    continue;

                if (card.text == readText)
                    continue;

                Undo.RecordObject(card, "Sync hero player_read_text");
                card.text = readText;
                EditorUtility.SetDirty(card);
                updated++;
            }
            return updated;
        }

        private static Dictionary<string, CardData> BuildCardDataIndexById()
        {
            Dictionary<string, CardData> map = new Dictionary<string, CardData>();
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData c = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (c == null || string.IsNullOrEmpty(c.id))
                    continue;
                if (!map.ContainsKey(c.id))
                    map[c.id] = c;
            }
            return map;
        }

        private static Dictionary<string, string> RowToDict(string[] headers, string[] values)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                string v = j < values.Length ? values[j] : "";
                data[headers[j]] = v;
            }
            return data;
        }

        private int ImportVc5PlayerReadText(List<string[]> rows, string[] headers)
        {
            Dictionary<string, CardData> index = BuildCardDataIndexById();
            int updated = 0;
            for (int i = 1; i < rows.Count; i++)
            {
                Dictionary<string, string> data = RowToDict(headers, rows[i]);
                string csvId = GetValue(data, "card_id", "");
                string readText = GetValue(data, "player_read_text", "");
                if (string.IsNullOrEmpty(csvId))
                    continue;

                string engineId = Vc5CsvIdMaps.GetEngineCardId(csvId);

                if (!index.TryGetValue(engineId, out CardData card))
                {
                    if (!string.IsNullOrEmpty(readText))
                        Debug.LogWarning($"[VC5 CSV] 未找到 CardData 资源 id={engineId}（card_id={csvId}）。黏黏牌多为运行时注册，请改 Vc5SlimeBootstrap。");
                    continue;
                }

                if (string.IsNullOrEmpty(readText))
                    continue;

                if (card.text == readText)
                    continue;

                Undo.RecordObject(card, "Sync player_read_text");
                card.text = readText;
                EditorUtility.SetDirty(card);
                updated++;
            }
            return updated;
        }

        private void ImportHeroesFromRows(List<string[]> rows, string[] headers)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            for (int i = 1; i < rows.Count; i++)
            {
                Dictionary<string, string> data = RowToDict(headers, rows[i]);
                if (string.IsNullOrEmpty(GetValue(data, "ID", GetValue(data, "hero_id", ""))))
                    continue;
                CreateHeroCardData(data);
            }
        }

        private void ImportCardsFromRows(List<string[]> rows, string[] headers)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            for (int i = 1; i < rows.Count; i++)
            {
                Dictionary<string, string> data = RowToDict(headers, rows[i]);
                if (string.IsNullOrEmpty(GetValue(data, "ID", GetValue(data, "card_id", ""))))
                    continue;
                CreateCardData(data);
            }
        }

        private void CreateHeroCardData(Dictionary<string, string> data)
        {
            // 获取必要字段
            string id = GetValue(data, "ID", GetValue(data, "hero_id", ""));
            string name = GetValue(data, "名称", "");
            
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"跳过无效的英雄数据：ID={id}, 名称={name}");
                return;
            }

            // 创建CardData
            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.id = id;
            cardData.title = name;
            cardData.type = CardType.Hero;
            
            // 解析数值
            cardData.attack = ParseInt(GetValue(data, "攻击力", "0"));
            cardData.hp = ParseInt(GetValue(data, "生命值", "0"));
            cardData.move_Range = ParseInt(GetValue(data, "移动力", "2"));
            cardData.attack_Range = ParseInt(GetValue(data, "攻击距离", "1"));
            cardData.mana = ParseInt(GetValue(data, "费用", "0"));
            
            // 描述
            cardData.desc = GetValue(data, "描述文本", "");
            cardData.text = GetValue(data, "player_read_text", GetValue(data, "技能1_效果描述", ""));

            // 保存资源
            string assetPath = $"{outputFolder}/hero_{id}.asset";
            AssetDatabase.CreateAsset(cardData, assetPath);
            Debug.Log($"已创建英雄：{assetPath}");
        }

        private void CreateCardData(Dictionary<string, string> data)
        {
            // 获取必要字段
            string id = GetValue(data, "ID", GetValue(data, "card_id", ""));
            string name = GetValue(data, "名称", "");
            string typeStr = GetValue(data, "类型", "Spell");
            
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"跳过无效的卡牌数据：ID={id}, 名称={name}");
                return;
            }

            // 创建CardData
            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.id = id;
            cardData.title = name;
            
            // 解析类型
            cardData.type = ParseCardType(typeStr);
            
            // 解析数值
            cardData.mana = ParseInt(GetValue(data, "费用", GetValue(data, "cost_mana", "0")));
            cardData.hp_cost = ParseInt(GetValue(data, "cost_hp", "0"));
            cardData.discard_cost = ParseInt(GetValue(data, "cost_discard", "0"));
            cardData.attack = ParseInt(GetValue(data, "攻击力", "0"));
            cardData.hp = ParseInt(GetValue(data, "生命值", "0"));
            cardData.move_Range = ParseInt(GetValue(data, "移动力", "0"));
            cardData.attack_Range = ParseInt(GetValue(data, "攻击距离", "0"));
            cardData.fast_action = ParseInt(GetValue(data, "快速行动", "0")) != 0;
            
            // 描述
            cardData.desc = GetValue(data, "描述文本", "");
            cardData.text = GetValue(data, "player_read_text", GetValue(data, "效果1_描述", ""));

            // 保存资源
            string assetPath = $"{outputFolder}/card_{id}.asset";
            AssetDatabase.CreateAsset(cardData, assetPath);
            Debug.Log($"已创建卡牌：{assetPath}");
        }

        private string GetValue(Dictionary<string, string> data, string key, string defaultValue)
        {
            return data.ContainsKey(key) && !string.IsNullOrEmpty(data[key]) ? data[key] : defaultValue;
        }

        private int ParseInt(string value, int defaultValue = 0)
        {
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }

        private CardType ParseCardType(string typeStr)
        {
            switch (typeStr.ToLower())
            {
                case "hero":
                    return CardType.Hero;
                case "character":
                    return CardType.Character;
                case "spell":
                    return CardType.Spell;
                case "artifact":
                    return CardType.Artifact;
                case "secret":
                    return CardType.Secret;
                case "equipment":
                    return CardType.Equipment;
                case "攻击":
                case "法术":
                case "召唤":
                case "响应":
                    return CardType.Spell;
                default:
                    return CardType.Spell;
            }
        }
    }
}




