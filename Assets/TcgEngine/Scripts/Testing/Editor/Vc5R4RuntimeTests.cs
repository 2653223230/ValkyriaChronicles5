#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TcgEngine.Client;
using TcgEngine.Server;
using TcgEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace TcgEngine.Testing.Editor
{
    public class Vc5R4RuntimeTests
    {
        [UnityTest, Category("VC5R4Runtime")]
        public IEnumerator TutorialFinish_ReloadsAFreshFormalMatchWithoutReopeningTutorial()
        {
            EditorSceneManager.OpenScene("Assets/TcgEngine/Scenes/Menu/Menu.unity");
            yield return new EnterPlayMode();
            yield return WaitUntil(() => Object.FindObjectOfType<MainMenu>() != null);
            for (int i = 0; i < 30; i++) yield return null;
            yield return WaitUntil(() => IsDeckReady(Vc5DemoBootstrap.CommandR4DeckId)
                && IsDeckReady(Vc5DemoBootstrap.SteadyAssaultDeckId));
            Vc5DemoBootstrap.Register();
            Assert.IsTrue(Vc5DemoMatchSetup.TryStartSoloAIMatch(Object.FindObjectOfType<MainMenu>(),
                Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId, out string error), error);
            yield return WaitUntil(() => GameClient.Get() != null && GameClient.Get().IsReady());

            GameClient tutorialClient = GameClient.Get();
            Vc5DemoTutorialOverlay tutorial = Object.FindObjectOfType<Vc5DemoTutorialOverlay>();
            Assert.NotNull(tutorial);
            Assert.NotNull(tutorialClient.GetPlayer().cards_hand);
            Assert.Greater(tutorialClient.GetPlayer().cards_hand.Count, 0);
            Assert.AreEqual(Vc5DemoTutorialOverlay.R4MobileShotId,
                tutorialClient.GetPlayer().cards_hand[0].card_id);

            int tutorialClientId = tutorialClient.GetInstanceID();
            tutorial.BeginFormalMatch();
            yield return WaitUntil(() => GameClient.Get() != null && GameClient.Get().IsReady()
                && GameClient.Get().GetInstanceID() != tutorialClientId);
            for (int i = 0; i < 10; i++) yield return null;

            Assert.IsNull(Object.FindObjectOfType<Vc5DemoTutorialOverlay>(),
                "正式对局不应再次显示教学遮罩");
            Assert.IsFalse(Vc5DemoTutorialOverlay.IsBlockingDemoAI,
                "正式对局必须解除教学期间的 AI 输入阻塞");
            Assert.AreEqual(0, GameClient.Get().GetPlayer().kill_count);
            Assert.AreEqual(0, GameClient.Get().GetOpponentPlayer().kill_count);
            yield return new ExitPlayMode();
        }

        [UnityTest, Category("VC5R4Runtime")]
        public IEnumerator MobileShotTutorialSuccess_WaitsForPresentationThenFullTwoSeconds()
        {
            EditorSceneManager.OpenScene("Assets/TcgEngine/Scenes/Menu/Menu.unity");
            yield return new EnterPlayMode();
            yield return WaitUntil(() => Object.FindObjectOfType<MainMenu>() != null);
            for (int i = 0; i < 30; i++) yield return null;
            yield return WaitUntil(() => IsDeckReady(Vc5DemoBootstrap.CommandR4DeckId)
                && IsDeckReady(Vc5DemoBootstrap.SteadyAssaultDeckId));
            Vc5DemoBootstrap.Register();
            Assert.IsTrue(Vc5DemoMatchSetup.TryStartSoloAIMatch(Object.FindObjectOfType<MainMenu>(),
                Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId, out string error), error);
            yield return WaitUntil(IsR4TutorialReady);

            GameClient client = GameClient.Get();
            Vc5DemoTutorialOverlay tutorial = Object.FindObjectOfType<Vc5DemoTutorialOverlay>();
            Card shot = client.GetPlayer().cards_hand.First(c => c.card_id == Vc5DemoTutorialOverlay.R4MobileShotId);
            Card ranger = client.GetPlayer().cards_board.First(c => c.card_id == Vc5R4Rules.Ranger);
            SetTutorialStep(tutorial, Vc5DemoTutorialOverlay.TutorialStep.R4WaitCardDrag);
            typeof(Vc5DemoTutorialOverlay).GetField("trackedCardUid",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(tutorial, shot.uid);

            client.PlayCard(shot, ranger.slot);
            var deadlineField = typeof(Vc5DemoTutorialOverlay).GetField("showResultAt",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            yield return WaitUntil(() => (float)deadlineField.GetValue(tutorial) >= 0f);
            float deadline = (float)deadlineField.GetValue(tutorial);
            float scheduledAt = Time.unscaledTime;
            Assert.GreaterOrEqual(deadline - scheduledAt, 1.90f,
                "The reviewed two seconds must begin only after client presentation is complete.");
            Assert.AreNotEqual(Vc5DemoTutorialOverlay.TutorialStep.R4CardSuccess, tutorial.CurrentStep);
            Vc5C3RuntimeTests.CaptureCurrentView("r4-thirdpass-03-presentation-complete-delay", 1920, 1080);

            while (Time.unscaledTime < deadline - 0.03f)
            {
                Assert.AreNotEqual(Vc5DemoTutorialOverlay.TutorialStep.R4CardSuccess, tutorial.CurrentStep);
                yield return null;
            }
            yield return WaitUntil(() => tutorial.CurrentStep == Vc5DemoTutorialOverlay.TutorialStep.R4CardSuccess);
            Assert.GreaterOrEqual(Time.unscaledTime - scheduledAt, 1.90f);
            Vc5C3RuntimeTests.CaptureCurrentView("r4-thirdpass-035-success", 1920, 1080);
            yield return new ExitPlayMode();
        }

        [UnityTest, Category("VC5R4Runtime")]
        public IEnumerator Menu_CommanderSelection_AndTemporaryPreview()
        {
            EditorSceneManager.OpenScene("Assets/TcgEngine/Scenes/Menu/Menu.unity");
            yield return new EnterPlayMode();
            yield return WaitUntil(() => Object.FindObjectOfType<MainMenu>() != null);
            for (int i = 0; i < 30; i++) yield return null;
            yield return WaitUntil(() => IsDeckReady(Vc5DemoBootstrap.CommandR4DeckId)
                && IsDeckReady(Vc5DemoBootstrap.SteadyAssaultDeckId));
            Vc5DemoBootstrap.Register();
            Assert.IsTrue(Vc5DemoMatchSetup.TryStartSoloAIMatch(Object.FindObjectOfType<MainMenu>(),
                Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId, out string error), error);
            yield return WaitUntil(IsR4TutorialReady);
            for (int i = 0; i < 5; i++) yield return null;
            yield return WaitUntil(IsR4TutorialReady);
            GameClient tutorialClient = GameClient.Get();
            Assert.NotNull(tutorialClient);
            Assert.IsTrue(tutorialClient.IsReady());
            Player tutorialPlayer = tutorialClient.GetPlayer();
            Assert.NotNull(tutorialPlayer);
            Assert.AreEqual(Vc5DemoBootstrap.CommandR4DeckId, tutorialPlayer.deck);
            Assert.NotNull(tutorialPlayer.cards_hand);
            Assert.Greater(tutorialPlayer.cards_hand.Count, 0);
            Assert.IsTrue(Vc5DemoTutorialOverlay.ShouldShowFor(GameType.Solo,
                Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId));
            Vc5DemoTutorialOverlay tutorial = Object.FindObjectOfType<Vc5DemoTutorialOverlay>();
            Assert.NotNull(tutorial);
            Assert.AreEqual(Vc5DemoTutorialOverlay.R4MobileShotId,
                tutorialPlayer.cards_hand[0].card_id);
            System.IO.Directory.CreateDirectory("TestResults");
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-01-intro", 1920, 1080);
            SetTutorialStep(tutorial, Vc5DemoTutorialOverlay.TutorialStep.R4Ranger);
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-04-ranger", 1920, 1080);
            SetPromptStage(tutorial, "SetR4RangerPromptStage", 1);
            yield return null;
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-04b-ranger-skill", 1920, 1080);
            BeginRangerMove();
            yield return WaitUntil(() => GameClient.Get().GetGameData().selector == SelectorType.SelectTarget);
            SetPromptStage(tutorial, "SetR4RangerPromptStage", 2);
            yield return null;
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-04c-ranger-destination", 1920, 1080);
            GameClient.Get().CancelSelection();
            yield return WaitUntil(() => GameClient.Get().GetGameData().selector == SelectorType.None);
            SetTutorialStep(tutorial, Vc5DemoTutorialOverlay.TutorialStep.R4Commander);
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-06-commander", 1920, 1080);
            SetPromptStage(tutorial, "SetR4CommanderPromptStage", 1);
            yield return null;
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-06b-commander-skill", 1920, 1080);
            BeginPrepare();
            yield return WaitUntil(() => GameClient.Get().GetGameData().selector == SelectorType.SelectorCard);
            yield return WaitUntil(() => CardSelector.Get().IsFullyVisible());
            SelectDiscard();
            yield return WaitUntil(() => GameClient.Get().GetGameData().selector == SelectorType.SelectorChoice);
            yield return WaitUntil(() => ChoiceSelector.Get().IsFullyVisible());
            Assert.AreEqual(3, ChoiceSelector.Get().choices.Count(c => c.gameObject.activeInHierarchy));
            SetPromptStage(tutorial, "SetR4CommanderPromptStage", 3);
            yield return null;
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-06d-command-choice", 1920, 1080);
            System.IO.Directory.CreateDirectory("TestResults");
            Vc5C3RuntimeTests.CaptureCurrentView("r4-command-choice", 1920, 1080);
            GameClient.Get().SelectChoice(1);
            yield return WaitUntil(() => GameClient.Get().GetPlayer().cards_hand.Any(c => c.card_id == Vc5R4Rules.Orders[1]));
            yield return WaitUntil(() => tutorial.CurrentStep == Vc5DemoTutorialOverlay.TutorialStep.R4CommandRange);
            for (int i = 0; i < 3; i++) yield return null;
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-07-command-range", 1920, 1080);
            SetTutorialStep(tutorial, Vc5DemoTutorialOverlay.TutorialStep.R4Flow);
            Vc5C3RuntimeTests.CaptureCurrentView("r4-tutorial-09-flow", 1920, 1080);
            tutorial.Dismiss();
            yield return null;
            foreach (ServerManagerLocal manager in Object.FindObjectsOfType<ServerManagerLocal>()) manager.enabled = false;
            PrepareVisibleFixture();
            for (int i = 0; i < 120; i++) yield return null;
            CapturePreview();
            yield return new ExitPlayMode();
        }

        static void BeginPrepare()
        {
            Card commander = GameClient.Get().GetPlayer().cards_board.First(c => c.card_id == Vc5R4Rules.Commander);
            GameClient.Get().CastAbility(commander, AbilityData.Get(Vc5R4Rules.PrepareSkill));
        }
        static bool IsR4TutorialReady()
        {
            GameClient client = GameClient.Get();
            if (client == null || !client.IsReady())
                return false;
            Player player = client.GetPlayer();
            return player != null && player.cards_hand != null
                && player.cards_hand.Count > 0
                && player.cards_hand[0] != null
                && Camera.allCameras.Length > 0
                && Camera.allCameras.All(camera => camera != null)
                && Object.FindObjectOfType<Vc5C3PreviewOverlay>() != null
                && Object.FindObjectOfType<Vc5DemoTutorialOverlay>() != null;
        }
        static bool IsDeckReady(string deckId)
        {
            DeckData deck = DeckData.Get(deckId);
            return deck != null && new UserDeckData(deck).IsValid();
        }
        static void BeginRangerMove()
        {
            Card ranger = GameClient.Get().GetPlayer().cards_board.First(c => c.card_id == Vc5R4Rules.Ranger);
            GameClient.Get().CastAbility(ranger, AbilityData.Get(Vc5R4Rules.MoveSkill));
        }
        static void SetTutorialStep(Vc5DemoTutorialOverlay tutorial, Vc5DemoTutorialOverlay.TutorialStep step)
        {
            typeof(Vc5DemoTutorialOverlay).GetMethod("SetStep",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(tutorial, new object[] { step });
        }
        static void SetPromptStage(Vc5DemoTutorialOverlay tutorial, string methodName, int stage)
        {
            typeof(Vc5DemoTutorialOverlay).GetMethod(methodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(tutorial, new object[] { stage });
        }
        static void SelectDiscard()
        {
            GameClient.Get().SelectCard(GameClient.Get().GetPlayer().cards_hand.First(c => !Vc5R4Rules.IsTemporary(c)));
        }
        static void PrepareVisibleFixture()
        {
            Game game = GameClient.Get().GetGameData();
            Card commander = game.players[0].cards_board.First(c => c.card_id == Vc5R4Rules.Commander);
            Card sniper = game.players[0].cards_board.First(c => c.card_id == Vc5R4Rules.Sniper);
            commander.slot = new Slot(3, 3, Slot.GetP(0));
            sniper.slot = new Slot(5, 3, Slot.GetP(0));
            sniper.r4_watch = true;
            Vc5R4Rules.Shield(sniper, 2);
            game.players[1].cards_board[0].slot = Vc5DemoGrid.ToPerspective(new Slot(7, 3, Slot.GetP(0)), Slot.GetP(1));
            game.players[0].mana = 9;
        }
        static void CapturePreview()
        {
            Game game = GameClient.Get().GetGameData();
            Card sniper = game.players[0].cards_board.First(c => c.card_id == Vc5R4Rules.Sniper);
            Card order = game.players[0].cards_hand.First(c => c.card_id == Vc5R4Rules.Orders[1]);
            Assert.IsTrue(Object.FindObjectsOfType<Vc5TemporaryCardGraphic>().Any(g => g.isActiveAndEnabled));
            var overlay = Object.FindObjectOfType<Vc5C3PreviewOverlay>();
            overlay.enabled = false;
            overlay.Refresh(game, order, sniper);
            Assert.IsTrue(overlay.Preview.plan.valid);
            Assert.Greater(overlay.Preview.damage.Count, 0);
            Vc5C3RuntimeTests.CaptureCurrentView("r4-command-preview", 1920, 1080);
            var detail = Object.FindObjectOfType<CardPreviewUI>();
            detail.enabled = false;
            detail.card_ui.SetCard(order);
            detail.desc.text = CardPreviewUI.BuildAdditionalDescription(order.CardData);
            detail.ui_panel.Show(true);
            Vc5DemoBattleFeedback.SetCardPreviewVisible(true);
            Vc5C3RuntimeTests.CaptureCurrentView("r4-temporary-card", 1280, 720);
        }
        static IEnumerator WaitUntil(System.Func<bool> ready)
        {
            double end = UnityEditor.EditorApplication.timeSinceStartup + 30;
            while (!ready() && UnityEditor.EditorApplication.timeSinceStartup < end) yield return null;
            Assert.IsTrue(ready(), "R4 runtime step timed out");
        }
    }
}
#endif
