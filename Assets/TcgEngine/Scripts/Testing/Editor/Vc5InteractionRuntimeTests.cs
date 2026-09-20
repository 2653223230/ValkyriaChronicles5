#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.Client;
using TcgEngine.Server;
using TcgEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace TcgEngine.Testing.Editor
{
    public class Vc5InteractionRuntimeTests
    {
        [UnityTest, Category("VC5InteractionRuntime")]
        public IEnumerator SkillPanel_CommanderClickSelection_AndConfirmedEndDiscard()
        {
            EditorSceneManager.OpenScene("Assets/TcgEngine/Scenes/Menu/Menu.unity");
            yield return new EnterPlayMode();
            yield return Ready(() => Object.FindObjectOfType<MainMenu>() != null);
            for (int i = 0; i < 30; i++) yield return null;
            StartMatch();
            yield return Ready(() => GameClient.Get() != null && GameClient.Get().IsReady()
                && GameClient.Get().GetGameData().phase == GamePhase.Main);
            for (int i = 0; i < 60; i++) yield return null;
            ShowSkill();
            yield return new WaitForSecondsRealtime(0.6f);
            System.IO.Directory.CreateDirectory("TestResults");
            CaptureSkillAndClick();
            yield return Ready(() => CardSelector.Get().IsFullyVisible() && CardSelector.Get().IsCommanderDiscard);
            ClickCommanderCard();
            yield return new WaitForSecondsRealtime(0.6f);
            Vc5C3RuntimeTests.CaptureCurrentView("fix-commander-discard", 1920, 1080);
            CardSelector.Get().OnClickOK();
            yield return Ready(() => GameClient.Get().GetGameData().selector == SelectorType.SelectorChoice);
            GameClient.Get().SelectChoice(0);
            yield return Ready(() => GameClient.Get().GetPlayer().cards_hand.Any(c => c.card_id == Vc5R4Rules.Orders[0]));
            VerifyHudShieldAndSingleDetail();
            yield return new WaitForSecondsRealtime(0.9f);
            VerifyHudShieldAndSingleDetailAfterRefresh();
            yield return new WaitForSecondsRealtime(0.9f);
            VerifySingleDetailVisible();
            Vc5C3RuntimeTests.CaptureCurrentView("formal-r4-commander-detail", 1280, 720);
            Vc5C3RuntimeTests.CaptureCurrentView("fix-hud-shield-detail", 1920, 1080);
            PrepareEndDiscard();
            yield return Ready(() => GameClient.Get().GetGameData().phase == GamePhase.EndDiscard);
            yield return new WaitForSecondsRealtime(0.7f);
            StageEndDiscard();
            yield return new WaitForSecondsRealtime(0.6f);
            VerifyEndDiscardDetailVisible();
            Vc5C3RuntimeTests.CaptureCurrentView("formal-end-discard-detail", 1280, 720);
            Vc5C3RuntimeTests.CaptureCurrentView("fix-end-discard", 1920, 1080);
            HandCardArea.Get().ConfirmDiscardSelection();
            yield return Ready(() => GameClient.Get().GetPlayer().end_discard_passed);
            VerifyEndDiscard();
            yield return new ExitPlayMode();
        }

        static void StartMatch()
        {
            Assert.IsTrue(Vc5DemoMatchSetup.TryStartSoloAIMatch(Object.FindObjectOfType<MainMenu>(),
                Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId, out string error), error);
        }
        static void ShowSkill()
        {
            foreach (HeroUI hero in Object.FindObjectsOfType<HeroUI>()) Assert.IsFalse(hero.power_area.activeSelf);
            Card commander = GameClient.Get().GetPlayer().cards_board.First(c => c.card_id == Vc5R4Rules.Commander);
            AbilityPanel.Get().ShowAbilities(commander);
        }
        static void CaptureSkillAndClick()
        {
            AbilityPanel panel = AbilityPanel.Get();
            Assert.IsTrue(panel.IsFullyVisible());
            Assert.IsTrue(panel.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.Contains("临时指令")));
            Vc5C3RuntimeTests.CaptureCurrentView("fix-skill-panel", 1920, 1080);
            Vc5C3RuntimeTests.CaptureCurrentView("fix-skill-panel", 1280, 720);
            panel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        }
        static void ClickCommanderCard()
        {
            CardSelector panel = CardSelector.Get();
            Assert.AreEqual(-1, panel.SelectionIndex);
            Assert.IsFalse(panel.select_button.interactable);
            var card = panel.GetComponentsInChildren<CardSelectorCard>().First(c => c.GetIndex() == 0);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.ExecuteHierarchy(card.card_ui.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(0, panel.SelectionIndex);
            ExecuteEvents.ExecuteHierarchy(card.card_ui.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(-1, panel.SelectionIndex);
            ExecuteEvents.ExecuteHierarchy(card.card_ui.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(panel.select_button.interactable);
            Assert.IsTrue(GameClient.Get().GetPlayer().cards_hand.Contains(card.GetCard()));
        }

        static string detailCardUid;
        static void VerifyHudShieldAndSingleDetail()
        {
            ServerManagerLocal manager = Object.FindObjectOfType<ServerManagerLocal>();
            manager.enabled = false;
            var server = (GameServer)typeof(ServerManagerLocal).GetField("server", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            Card commander = server.GetGameData().GetPlayer(0).cards_board.First(c => c.card_id == Vc5R4Rules.Commander);
            commander.r4_shield = 2;
            commander.r4_shield_rounds = 2;
            commander.r4_watch = true;
            detailCardUid = commander.uid;
            server.RefreshAll();
        }

        static void VerifyHudShieldAndSingleDetailAfterRefresh()
        {
            foreach (PlayerUI ui in Object.FindObjectsOfType<PlayerUI>())
            {
                StringAssert.IsMatch(@"^胜利分：\d+/9  法力值：$", ui.hp_txt.text);
                StringAssert.DoesNotContain("法力", ui.pname.text);
                Assert.IsFalse(ui.hp_max_txt.enabled);
                Assert.IsFalse(ScreenRectsOverlap(ui.hp_txt.rectTransform, ui.pname.rectTransform),
                    "Resource text must not overlap the player name.");
                AssertManaIconsFollowLabel(ui);
            }

            BoardCard board = Object.FindObjectsOfType<BoardCard>().First(b => b.GetCard() != null && b.GetCard().uid == detailCardUid);
            var badge = board.GetComponentsInChildren<Transform>(true).First(t => t.name == "R4ShieldIcon");
            var badgeImage = badge.GetComponent<UnityEngine.UI.Image>();
            var badgeText = badge.GetComponentInChildren<UnityEngine.UI.Text>(true);
            var badgeRect = badge.GetComponent<RectTransform>();
            Assert.IsTrue(badgeImage.enabled);
            Assert.That(badgeImage.color.r, Is.EqualTo(badgeImage.color.g).Within(0.01f));
            Assert.That(badgeImage.color.g, Is.EqualTo(badgeImage.color.b).Within(0.01f));
            Assert.Greater(badgeRect.anchoredPosition.x, 0f, "R4 shield belongs on the health/right side.");
            Assert.GreaterOrEqual(badgeRect.sizeDelta.x, 60f);
            Assert.GreaterOrEqual(badgeRect.sizeDelta.y, 50f);
            Assert.AreEqual("2", badgeText.text);
            Assert.GreaterOrEqual(badgeText.fontSize, 30);
            AbilityPanel.Get().Hide(true);
            foreach (SelectorPanel panel in Resources.FindObjectsOfTypeAll<SelectorPanel>())
                if (panel.gameObject.scene.IsValid()) panel.Hide(true);
            typeof(BoardCard).GetField("focus", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(board, true);
            CardDetailPreview.ShowCard(board.GetCard());
            Assert.IsNull(Object.FindObjectOfType<CardDetailPreview>(), "VC5 should not create the duplicate right detail panel.");
            Assert.IsTrue(Vc5StatusDisplay.FormatAll(board.GetCard()).Contains("护盾 2"));
            Assert.IsTrue(Vc5StatusDisplay.FormatAll(board.GetCard()).Contains("警戒"));
        }

        static bool ScreenRectsOverlap(RectTransform a, RectTransform b)
        {
            Vector3[] cornersA = new Vector3[4];
            Vector3[] cornersB = new Vector3[4];
            a.GetWorldCorners(cornersA);
            b.GetWorldCorners(cornersB);
            Rect rectA = Rect.MinMaxRect(cornersA[0].x, cornersA[0].y, cornersA[2].x, cornersA[2].y);
            Rect rectB = Rect.MinMaxRect(cornersB[0].x, cornersB[0].y, cornersB[2].x, cornersB[2].y);
            return rectA.Overlaps(rectB);
        }

        static void AssertManaIconsFollowLabel(PlayerUI ui)
        {
            RectTransform label = ui.hp_txt.rectTransform;
            Vector3 labelEnd = label.TransformPoint(new Vector3(ui.hp_txt.preferredWidth, 0f, 0f));
            var icons = ui.mana_bar.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .Where(image => image.enabled && image.gameObject.activeInHierarchy)
                .Select(image => image.rectTransform)
                .ToArray();
            Assert.IsNotEmpty(icons);

            float iconMinX = icons.Min(icon =>
            {
                Vector3[] corners = new Vector3[4];
                icon.GetWorldCorners(corners);
                return corners[0].x;
            });
            Assert.GreaterOrEqual(iconMinX, labelEnd.x - 2f, "Blue mana orbs must follow the 法力值： label.");
            Assert.Less(Mathf.Abs(((RectTransform)ui.mana_bar.transform).position.y - label.position.y), 30f,
                "Mana orbs and their label should read as one row.");
        }

        static void VerifySingleDetailVisible()
        {
            CardPreviewUI preview = Object.FindObjectOfType<CardPreviewUI>();
            Assert.NotNull(preview);
            StringAssert.Contains("当前状态", preview.desc.text);
            StringAssert.Contains("护盾 2", preview.desc.text);
            StringAssert.Contains("警戒", preview.desc.text);
            Assert.AreEqual(Vc5R4Rules.Commander, preview.card_ui.GetCard().id);
            StringAssert.Contains("推进（1费）", preview.card_ui.card_text.text);
            StringAssert.Contains("开火（1费）", preview.card_ui.card_text.text);
            StringAssert.Contains("掩护（0费）", preview.card_ui.card_text.text);
        }

        // Deterministic end-discard fixture, still using the live client/server action channel.
        static string[] discardUids;
        static void PrepareEndDiscard()
        {
            ServerManagerLocal manager = Object.FindObjectOfType<ServerManagerLocal>();
            manager.enabled = false; // Stop AI and clock, not message handlers.
            var server = (GameServer)typeof(ServerManagerLocal).GetField("server", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            Game game = server.GetGameData();
            game.phase = GamePhase.EndDiscard;
            game.selector = SelectorType.None;
            foreach (Player player in game.players) player.end_discard_passed = false;
            Vc5R4Rules.EndMainPhase(game);
            discardUids = game.players[0].cards_hand.Take(2).Select(c => c.uid).ToArray();
            Assert.AreEqual(2, discardUids.Length);
            server.RefreshAll();
        }
        static void StageEndDiscard()
        {
            HandCardArea area = HandCardArea.Get();
            ClickEndDiscardCard(discardUids[0]);
            ClickEndDiscardCard(discardUids[1]);
            ClickEndDiscardCard(discardUids[0]);
            Assert.AreEqual(1, area.DiscardSelectionCount);
            ClickEndDiscardCard(discardUids[0]);
            Assert.AreEqual(2, area.DiscardSelectionCount);
            foreach (string uid in discardUids) Assert.NotNull(GameClient.Get().GetPlayer().GetHandCard(uid));
        }
        static void ClickEndDiscardCard(string uid)
        {
            HandCard handCard = HandCard.Get(uid);
            Assert.NotNull(handCard);
            typeof(HandCard).GetField("discard_press", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(handCard, true);
            typeof(HandCard).GetField("drag_start_screen_pos", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(handCard, (Vector2)Input.mousePosition);
            handCard.OnMouseUpCard();
        }
        static void VerifyEndDiscardDetailVisible()
        {
            Card expected = GameClient.Get().GetPlayer().GetHandCard(discardUids[0]);
            CardPreviewUI preview = Object.FindObjectOfType<CardPreviewUI>();
            Assert.NotNull(expected);
            Assert.NotNull(preview);
            Assert.IsTrue(preview.ui_panel.IsVisible());
            Assert.AreEqual(expected.card_id, preview.card_ui.GetCard().id);
        }
        static void VerifyEndDiscard()
        {
            Player player = GameClient.Get().GetPlayer();
            foreach (string uid in discardUids)
            {
                Assert.IsNull(player.GetHandCard(uid));
                Assert.IsTrue(player.cards_discard.Any(c => c.uid == uid));
            }
            Assert.IsFalse(HandCardArea.Get().IsConfirmingDiscard);
        }
        static IEnumerator Ready(System.Func<bool> ready)
        {
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 30;
            while (!ready() && UnityEditor.EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.IsTrue(ready(), "Interaction runtime step timed out");
        }
    }
}
#endif
