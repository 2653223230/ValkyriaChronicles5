#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TcgEngine.Client;
using TcgEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DemoStartupTests : Vc5LogicTestBase
    {
        private const string MenuScene = "Assets/TcgEngine/Scenes/Menu/Menu.unity";
        private const string GameScene = "Assets/TcgEngine/Scenes/Game/Game.unity";
        private const string AppIconPath = "Assets/TcgEngine/Images/VC5/AppIcon.png";

        [Test]
        public void BuildSettings_ContainsOnlyMenuThenGame()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();

            Assert.AreEqual(2, scenes.Length);
            Assert.AreEqual(MenuScene, scenes[0].path);
            Assert.AreEqual(GameScene, scenes[1].path);
        }

        [Test]
        public void AndroidBranding_UsesValkyriaNameAndProjectIcon()
        {
            Texture2D expectedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            Texture2D[] configuredIcons = UnityEditor.PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);

            Assert.AreEqual("ValkyriaChronicles5", UnityEditor.PlayerSettings.productName);
            Assert.NotNull(expectedIcon, $"Android icon must be imported at {AppIconPath}.");
            CollectionAssert.Contains(configuredIcons, expectedIcon,
                "Android legacy icons must include the VC5 project icon.");
        }

        [Test]
        public void MenuScene_CreatesDemoPanelWithThreePlayableDecks()
        {
            string restoreScene = GetRestorableScene();
            try
            {
                EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
                Vc5LogicTestHarness.LoadGameData();
                MainMenu menu = Object.FindObjectOfType<MainMenu>(true);
                Assert.NotNull(menu, "Menu scene must contain MainMenu.");

                Vc5DemoAIBattlePanel panel = Vc5DemoAIBattlePanel.Ensure(menu);
                DeckData[] decks = Vc5DemoMatchSetup.GetPlayableDemoDecks();

                Assert.NotNull(panel);
                Assert.IsTrue(panel.gameObject.activeSelf);
                Assert.AreEqual("VC5 Demo AI Battle Panel", panel.gameObject.name);
                Assert.AreEqual(3, decks.Length);
                CollectionAssert.AreEquivalent(
                    new[] { Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId, Vc5DemoBootstrap.RangedPressureC3DeckId },
                    decks.Select(deck => deck.id).ToArray());
            }
            finally
            {
                RestoreScene(restoreScene);
            }
        }

        [Test]
        public void SoloSetup_PreservesSelectedDecksWhenGameSceneLoads()
        {
            string restoreScene = GetRestorableScene();
            try
            {
                Assert.IsTrue(Vc5DemoMatchSetup.ApplySoloAIMatch(
                    Vc5DemoBootstrap.RangedPressureDeckId,
                    Vc5DemoBootstrap.MobileAssaultDeckId));

                Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

                Assert.IsTrue(scene.IsValid());
                Assert.IsTrue(scene.isLoaded);
                Assert.Greater(scene.rootCount, 0);
                Assert.AreEqual(GameType.Solo, GameClient.game_settings.game_type);
                Assert.AreEqual(Vc5DemoBootstrap.RangedPressureDeckId, GameClient.player_settings.deck.tid);
                Assert.AreEqual(Vc5DemoBootstrap.MobileAssaultDeckId, GameClient.ai_settings.deck.tid);
            }
            finally
            {
                RestoreScene(restoreScene);
            }
        }

        [Test]
        public void GameScene_HasExactlyOnePhaseActionButton()
        {
            string restoreScene = GetRestorableScene();
            try
            {
                EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
                GameUI ui = Object.FindObjectOfType<GameUI>(true);
                Assert.NotNull(ui);

                UnityEngine.UI.Button[] phaseButtons = ui.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .Where(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                        .Any(index =>
                        {
                            string method = button.onClick.GetPersistentMethodName(index);
                            return method == "OnClickNextStage" || method == "OnClickNextTurn";
                        }))
                    .ToArray();

                Assert.AreEqual(1, phaseButtons.Length,
                    "The responsive prefab button is the only phase action; the scene must not add a duplicate.");
            }
            finally
            {
                RestoreScene(restoreScene);
            }
        }

        private static string GetRestorableScene()
        {
            Scene active = SceneManager.GetActiveScene();
            return active.IsValid() ? active.path : "";
        }

        private static void RestoreScene(string scenePath)
        {
            if (!string.IsNullOrEmpty(scenePath))
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }
}
#endif
