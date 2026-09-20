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
    public class Vc5BAI1RuntimeTests
    {
        [UnityTest, Category("VC5BAI1Runtime")]
        public IEnumerator Menu_IndependentDeckPortraitsAndChargePreview()
        {
            EditorSceneManager.OpenScene("Assets/TcgEngine/Scenes/Menu/Menu.unity");
            yield return new EnterPlayMode();
            yield return WaitUntil(() => Object.FindObjectOfType<MainMenu>() != null);
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsTrue(Vc5DemoMatchSetup.ApplySoloAIMatch(Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId));
            Assert.AreEqual(Vc5DemoBootstrap.SteadyAssaultDeckId, GameClient.ai_settings.deck.tid);
            Assert.IsTrue(Vc5DemoMatchSetup.TryStartSoloAIMatch(Object.FindObjectOfType<MainMenu>(),
                Vc5DemoBootstrap.SteadyAssaultDeckId, Vc5DemoBootstrap.CommandR4DeckId, out string error), error);
            yield return WaitUntil(() => GameClient.Get() != null && GameClient.Get().IsReady()
                && Object.FindObjectOfType<Vc5C3PreviewOverlay>() != null);
            Assert.AreEqual(Vc5DemoBootstrap.SteadyAssaultDeckId, GameClient.Get().GetPlayer().deck);
            Assert.IsFalse(Vc5DemoTutorialOverlay.ShouldShowFor(GameType.Solo, Vc5DemoBootstrap.SteadyAssaultDeckId, Vc5DemoBootstrap.CommandR4DeckId));
            foreach (ServerManagerLocal manager in Object.FindObjectsOfType<ServerManagerLocal>()) manager.enabled = false;
            PrepareFixture();
            for (int i = 0; i < 100; i++) yield return null;
            CaptureFixture();
            yield return new ExitPlayMode();
        }

        static void PrepareFixture()
        {
            Game game = GameClient.Get().GetGameData();
            game.current_player = 0;
            game.phase = GamePhase.Main;
            string[] ids = { Vc5BAI1Rules.Frontliner, Vc5BAI1Rules.Flanker, Vc5BAI1Rules.Rifleman };
            string[] art = { "vc5_demo_cavalry", "vc5_demo_assassin", "vc5_demo_scout" };
            for (int i = 0; i < ids.Length; i++)
            {
                Card card = game.players[0].cards_board.First(c => c.card_id == ids[i]);
                card.slot = new Slot(3, 2 + i, Slot.GetP(0));
                Assert.NotNull(card.CardData.art_board);
                Assert.AreSame(CardData.Get(art[i]).art_board, card.CardData.art_board);
            }
            game.players[1].cards_board[0].slot = Vc5DemoGrid.ToPerspective(new Slot(5, 3, Slot.GetP(0)), Slot.GetP(1));
            Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5BAI1Rules.Prefix + "short_charge");
        }

        static void CaptureFixture()
        {
            Game game = GameClient.Get().GetGameData();
            Card actor = game.players[0].cards_board.First(c => c.card_id == Vc5BAI1Rules.Flanker);
            Card spell = game.players[0].cards_hand.First(c => c.card_id == Vc5BAI1Rules.Prefix + "short_charge");
            Card enemy = game.players[1].cards_board[0];
            var overlay = Object.FindObjectOfType<Vc5C3PreviewOverlay>();
            overlay.enabled = false;
            overlay.Refresh(game, spell, actor, new Slot(4, 3, actor.slot.p), enemy);
            Assert.IsTrue(overlay.Preview.plan.valid);
            Assert.AreEqual(3, overlay.Preview.damage[enemy.uid]);
            System.IO.Directory.CreateDirectory("TestResults");
            Vc5C3RuntimeTests.CaptureCurrentView("bai1-charge-preview", 1920, 1080);
            var detail = Object.FindObjectOfType<CardPreviewUI>();
            detail.enabled = false;
            detail.card_ui.SetCard(actor);
            detail.desc.text = CardPreviewUI.BuildAdditionalDescription(actor.CardData);
            detail.ui_panel.Show(true);
            Vc5DemoBattleFeedback.SetCardPreviewVisible(true);
            Vc5C3RuntimeTests.CaptureCurrentView("bai1-hero", 1280, 720);
        }

        static IEnumerator WaitUntil(System.Func<bool> ready)
        {
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 30;
            while (!ready() && UnityEditor.EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.IsTrue(ready(), "B-AI1 runtime step timed out");
        }
    }
}
#endif
