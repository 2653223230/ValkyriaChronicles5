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

        [Test]
        public void BuildSettings_StartsWithMenuAndIncludesGameScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.IsNotEmpty(scenes);
            Assert.IsTrue(scenes[0].enabled);
            Assert.AreEqual(MenuScene, scenes[0].path);
            Assert.IsTrue(scenes.Any(scene => scene.enabled && scene.path == GameScene));
        }

        [Test]
        public void MenuScene_CreatesDemoPanelWithOnlyTwoPlayableDecks()
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
                Assert.AreEqual(2, decks.Length);
                CollectionAssert.AreEquivalent(
                    new[] { Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId },
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
