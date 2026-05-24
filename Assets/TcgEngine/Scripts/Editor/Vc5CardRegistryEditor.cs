using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TcgEngine;

namespace TcgEngine.EditorTool
{
    /// <summary>
    /// 卡牌/英雄注册表：导出、从 CSV 应用、进入 Play 前自动应用。
    /// </summary>
    public static class Vc5CardRegistryEditor
    {
        [MenuItem("TcgEngine/卡牌注册表/从游戏导出到 CSV", false, 200)]
        public static void ExportFromGame()
        {
            ReloadAllCardData();
            Dictionary<string, string> assetPaths = BuildAssetPathById();
            Dictionary<string, Vc5CardRegistry.RegistryRow> existing = Vc5CardRegistry.LoadMergedLookup();

            List<Vc5CardRegistry.RegistryRow> heroes = new List<Vc5CardRegistry.RegistryRow>();
            List<Vc5CardRegistry.RegistryRow> cards = new List<Vc5CardRegistry.RegistryRow>();

            foreach (CardData card in CardData.GetAll())
            {
                if (card == null || string.IsNullOrEmpty(card.id))
                    continue;

                string source = "asset";
                string path = "";
                if (assetPaths.TryGetValue(card.id, out string ap))
                {
                    path = ap;
                    source = "asset";
                }
                else
                {
                    source = "runtime_vc5";
                }

                Vc5CardRegistry.RegistryRow row = Vc5CardRegistry.FromCardData(card, source, path);
                if (existing.TryGetValue(card.id, out Vc5CardRegistry.RegistryRow old))
                {
                    row.enabled = old.enabled;
                    if (!string.IsNullOrEmpty(old.notes))
                        row.notes = old.notes;
                }

                if (Vc5CardRegistry.IsHeroEntry(card))
                    heroes.Add(row);
                else
                    cards.Add(row);
            }

            heroes.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            cards.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

            Vc5CardRegistry.SaveMerged(heroes, cards);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("卡牌注册表",
                $"已导出：英雄 {heroes.Count} 条 → {Vc5CardRegistry.HeroesFileName}\n" +
                $"卡牌 {cards.Count} 条 → {Vc5CardRegistry.CardsFileName}\n\n" +
                $"目录：{Vc5CardRegistry.GetRegistryDirectory()}",
                "确定");
        }

        [MenuItem("TcgEngine/卡牌注册表/从 CSV 应用到游戏（并保存资源）", false, 201)]
        public static void ApplyFromCsvAndSaveAssets()
        {
            ReloadAllCardData();
            Vc5CardRegistry.Apply();

            int saved = 0;
            foreach (Vc5CardRegistry.RegistryRow row in Vc5CardRegistry.LoadFile(Vc5CardRegistry.GetHeroesPath()))
                saved += TrySaveAssetRow(row);
            foreach (Vc5CardRegistry.RegistryRow row in Vc5CardRegistry.LoadFile(Vc5CardRegistry.GetCardsPath()))
                saved += TrySaveAssetRow(row);

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("卡牌注册表",
                "已从 CSV 应用到当前编辑器数据。\n" +
                $"已写回 {saved} 个 .asset 资源（仅 source=asset 且有 asset_path 的行）。\n" +
                "enabled=0 的条目已从运行时列表移除。",
                "确定");
        }

        [MenuItem("TcgEngine/卡牌注册表/打开注册表文件夹", false, 202)]
        public static void OpenRegistryFolder()
        {
            string dir = Vc5CardRegistry.GetRegistryDirectory();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        [InitializeOnLoad]
        private static class PlayModeApplyHook
        {
            static PlayModeApplyHook()
            {
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }

            private static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                if (state != PlayModeStateChange.ExitingEditMode)
                    return;
                if (!File.Exists(Vc5CardRegistry.GetHeroesPath()) && !File.Exists(Vc5CardRegistry.GetCardsPath()))
                    return;
                ReloadAllCardData();
                Vc5CardRegistry.Apply();
            }
        }

        private static void ReloadAllCardData()
        {
            CardData.Reload();
            Vc5SlimeBootstrap.ResetForDataReload();
            Vc5SlimeBootstrap.Register();
        }

        private static Dictionary<string, string> BuildAssetPathById()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData c = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (c == null || string.IsNullOrEmpty(c.id))
                    continue;
                if (!map.ContainsKey(c.id))
                    map[c.id] = path;
            }
            return map;
        }

        private static int TrySaveAssetRow(Vc5CardRegistry.RegistryRow row)
        {
            if (row == null || !row.enabled || string.IsNullOrEmpty(row.asset_path))
                return 0;
            if (!row.asset_path.StartsWith("Assets/"))
                return 0;

            CardData asset = AssetDatabase.LoadAssetAtPath<CardData>(row.asset_path);
            if (asset == null)
                return 0;

            Vc5CardRegistry.ApplyRowToCard(asset, row);
            EditorUtility.SetDirty(asset);
            return 1;
        }
    }
}
