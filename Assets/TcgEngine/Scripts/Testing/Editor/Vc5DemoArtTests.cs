#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate"), Category("VC5DemoArt")]
    public class Vc5DemoArtTests : Vc5LogicTestBase
    {
        [TestCase("vc5_demo_c3_tactical_move", false)]
        [TestCase("vc5_demo_c3_forced_march", false)]
        [TestCase("vc5_demo_c3_temp_calibration", false)]
        [TestCase("vc5_demo_c3_scope_upgrade", false)]
        [TestCase("vc5_demo_c3_fire_coverage", false)]
        [TestCase("vc5_demo_c3_heavy_break", false)]
        [TestCase("vc5_demo_c3_weakpoint_snipe", false)]
        [TestCase("vc5_demo_c3_mobile_shot", false)]
        [TestCase("vc5_demo_c3_sniper", true)]
        [TestCase("vc5_demo_c3_fire_guard", true)]
        [TestCase("vc5_demo_c3_mobile_ranger", true)]
        [TestCase("vc5_demo_cavalry", true)]
        [TestCase("vc5_demo_assassin", true)]
        [TestCase("vc5_demo_scout", true)]
        public void RegisteredArt_MatchesResource_AndSurvivesReregistration(string id, bool hero)
        {
            CardData card = CardData.Get(id);
            Assert.NotNull(card.art_full, id + " missing full art");
            Sprite expected = Resources.Load<Sprite>("VC5/DemoArt/" + id);
            Assert.NotNull(expected, "Missing build-included sprite " + id);
            Assert.AreSame(expected, card.GetFullArt(VariantData.GetDefault()));
            if (hero)
            {
                Assert.AreSame(expected, card.GetBoardArt(VariantData.GetDefault()));
                Assert.That(expected.bounds.size.x, Is.EqualTo(8f).Within(0.03f));
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(expected));
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.LessOrEqual(expected.texture.width, 1024);
            Assert.LessOrEqual(expected.texture.height, 1024);
            Assert.IsFalse(importer.isReadable);
            Assert.IsFalse(importer.mipmapEnabled);
            Vc5DemoBootstrap.Register();
            Assert.AreSame(card, CardData.Get(id));
            Assert.AreSame(expected, card.art_full);
            if (hero) Assert.AreSame(expected, card.art_board);
        }

        [Test]
        public void Register_DoesNotReplaceOriginalRangedDeckArt()
        {
            CardData oldSniper = CardData.Get("vc5_demo_sniper");
            Sprite full = oldSniper.art_full;
            Sprite board = oldSniper.art_board;
            Vc5DemoBootstrap.Register();
            Assert.AreSame(full, oldSniper.art_full);
            Assert.AreSame(board, oldSniper.art_board);
        }

        [TestCase("Assets/TcgEngine/Prefabs/Gameplay/HandCard.prefab")]
        [TestCase("Assets/TcgEngine/Prefabs/UI/CardUI.prefab")]
        public void CardArt_DoesNotOverlapDescription_AndRestoresLegacyLayout(string prefabPath)
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            try
            {
                var ui = instance.GetComponent<TcgEngine.UI.CardUI>();
                Vector2 originalPosition = ui.card_image.rectTransform.anchoredPosition;
                Vector2 originalSize = ui.card_image.rectTransform.sizeDelta;
                Vector2 originalTextPosition = ui.card_text.rectTransform.anchoredPosition;
                Vector2 originalTitlePosition = ui.card_title.rectTransform.anchoredPosition;
                ui.SetCard(CardData.Get("vc5_demo_c3_fire_coverage"), VariantData.GetDefault());
                Canvas.ForceUpdateCanvases();
                Bounds art = RectTransformUtility.CalculateRelativeRectTransformBounds(instance.transform, ui.card_image.rectTransform);
                Bounds description = RectTransformUtility.CalculateRelativeRectTransformBounds(instance.transform, ui.card_text.rectTransform);
                Bounds title = RectTransformUtility.CalculateRelativeRectTransformBounds(instance.transform, ui.card_title.rectTransform);
                Assert.Greater(art.min.y, description.max.y, "Art overlaps effect text");
                Assert.Less(art.max.y, title.min.y, "Art overlaps title");
                Assert.IsTrue(ui.card_image.preserveAspect);
                float displayedWidth = Mathf.Min(ui.card_image.rectTransform.rect.width, ui.card_image.rectTransform.rect.height);
                Assert.GreaterOrEqual(displayedWidth, originalSize.x * 0.88f, "Square art leaves excessive side margins");
                ui.SetCard(CardData.Get("vc5_demo_shoot"), VariantData.GetDefault());
                Assert.AreEqual(originalPosition, ui.card_image.rectTransform.anchoredPosition);
                Assert.AreEqual(originalSize, ui.card_image.rectTransform.sizeDelta);
                Assert.AreEqual(originalTextPosition, ui.card_text.rectTransform.anchoredPosition);
                Assert.AreEqual(originalTitlePosition, ui.card_title.rectTransform.anchoredPosition);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("Assets/TcgEngine/Prefabs/Gameplay/HandCard.prefab")]
        [TestCase("Assets/TcgEngine/Prefabs/UI/CardUI.prefab")]
        public void HeroArt_LeavesSpaceForEffectsAndStats(string prefabPath)
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            try
            {
                var ui = instance.GetComponent<TcgEngine.UI.CardUI>();
                foreach (string id in new[] { "c3_sniper", "c3_fire_guard", "c3_mobile_ranger", "cavalry", "assassin", "scout" })
                {
                    ui.SetCard(CardData.Get("vc5_demo_" + id), VariantData.GetDefault());
                    Canvas.ForceUpdateCanvases();
                    Bounds art = RectTransformUtility.CalculateRelativeRectTransformBounds(instance.transform, ui.card_image.rectTransform);
                    Bounds effect = RectTransformUtility.CalculateRelativeRectTransformBounds(instance.transform, ui.card_text.rectTransform);
                    Bounds hp = RectTransformUtility.CalculateRelativeRectTransformBounds(instance.transform, ui.hp_icon.rectTransform);
                    Assert.Greater(art.min.y, effect.max.y, id);
                    Assert.Greater(effect.min.y, hp.max.y, id);
                    Assert.IsTrue(ui.card_image.preserveAspect);
                    Assert.IsFalse(ui.team_icon.enabled, "Template team badge must not cover effect text");
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void DetailDescription_DoesNotRepeatSpellText_ButKeepsHeroStats()
        {
            CardData spell = CardData.Get("vc5_demo_c3_tactical_move");
            Assert.IsFalse(TcgEngine.UI.CardPreviewUI.BuildAdditionalDescription(spell).Contains(spell.GetDisplayText()));
            CardData hero = CardData.Get("vc5_demo_scout");
            StringAssert.Contains(hero.GetDesc(), TcgEngine.UI.CardPreviewUI.BuildAdditionalDescription(hero));
        }
    }
}
#endif
