#if UNITY_EDITOR
using TcgEngine.Client;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
    public class Vc5BattlefieldArtTests
    {
        [Test]
        public void Component_CanBeAttachedWithoutNativeCallsInItsConstructor()
        {
            GameObject obj = new GameObject("Battlefield attach check");
            try { Assert.NotNull(obj.AddComponent<Vc5BattlefieldView>()); }
            finally { Object.DestroyImmediate(obj); }
        }

        [TestCase(GameType.Solo, "deck_vc5_demo_command_r4", "deck_vc5_demo_steady_assault", true)]
        [TestCase(GameType.Multiplayer, "deck_vc5_demo_command_r4", "deck_vc5_demo_steady_assault", false)]
        [TestCase(GameType.HostP2P, "deck_vc5_demo_command_r4", "deck_vc5_demo_steady_assault", false)]
        [TestCase(GameType.Adventure, "deck_vc5_demo_command_r4", "deck_vc5_demo_steady_assault", false)]
        [TestCase(GameType.Solo, "deck_vc5_demo_ranged_pressure_c3", "deck_vc5_demo_steady_assault", false)]
        [TestCase(GameType.Solo, "deck_vc5_demo_command_r4", "other_deck", false)]
        public void Battlefield_IsLimitedToApprovedSoloDeckPair(GameType type, string player, string ai, bool expected)
        {
            bool actual = Vc5BattlefieldView.ShouldApply(type, player, ai);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Background_IsBuildIncludedSpriteWithoutReadableTextureOrMipmaps()
        {
            Sprite sprite = Resources.Load<Sprite>("VC5/Battlefield/command-table");
            Assert.NotNull(sprite);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.IsFalse(importer.isReadable);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
            Assert.That(sprite.bounds.size.x / sprite.bounds.size.y, Is.InRange(1.7f, 1.9f));
        }
    }
}
#endif
