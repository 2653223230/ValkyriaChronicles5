#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.AI;
using TcgEngine.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5DemoTutorialTests : Vc5LogicTestBase
    {
        private const string TutorialTypeName = "TcgEngine.UI.Vc5DemoTutorialOverlay, Assembly-CSharp";
        private const string MobileShotId = "vc5_demo_c3_mobile_shot";

        [Test]
        public void TutorialPolicy_ShowsInteractiveFlowOnlyForPlayerC3SoloDemo()
        {
            Type type = GetTutorialType();
            MethodInfo shouldShow = type.GetMethod("ShouldShowFor", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(shouldShow);
            MethodInfo isDemoSolo = type.GetMethod("IsDemoSoloMatch", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(isDemoSolo, "Battle feedback still needs the broader B/C/C3 Solo policy.");

            Assert.IsTrue(InvokeShouldShow(shouldShow, GameType.Solo,
                Vc5DemoBootstrap.RangedPressureC3DeckId, Vc5DemoBootstrap.MobileAssaultDeckId));
            Assert.IsTrue(InvokeShouldShow(shouldShow, GameType.Solo,
                Vc5DemoBootstrap.RangedPressureC3DeckId, Vc5DemoBootstrap.RangedPressureC3DeckId));
            Assert.IsFalse(InvokeShouldShow(shouldShow, GameType.Solo,
                Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureC3DeckId));
            Assert.IsFalse(InvokeShouldShow(shouldShow, GameType.Multiplayer,
                Vc5DemoBootstrap.RangedPressureC3DeckId, Vc5DemoBootstrap.MobileAssaultDeckId));
            Assert.IsTrue(InvokeShouldShow(isDemoSolo, GameType.Solo,
                Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId));
        }

        [Test]
        public void C3SoloPlayerDeck_AlwaysDrawsMobileShotFirst_WithoutFreezingRemainingShuffle()
        {
            DeckData deck = DeckData.Get(Vc5DemoBootstrap.RangedPressureC3DeckId);
            Assert.NotNull(deck);

            Game game = new Game("vc5_c3_tutorial_opening", 2);
            game.settings.game_type = GameType.Solo;
            GameLogic logic = new GameLogic(true);
            logic.SetData(game);
            HashSet<string> secondCards = new HashSet<string>();

            for (int i = 0; i < 24; i++)
            {
                logic.SetPlayerDeck(game.players[0], deck);
                Assert.AreEqual(20, game.players[0].cards_deck.Count);
                Assert.AreEqual(MobileShotId, game.players[0].cards_deck[0].card_id,
                    "The tutorial card must be the first card drawn after the normal shuffle.");
                secondCards.Add(game.players[0].cards_deck[1].card_id);
            }

            Assert.Greater(secondCards.Count, 1,
                "Only the first card is fixed; the remaining shuffled order must not become a fixed deck list.");
        }

        [Test]
        public void TutorialOverlay_BuildsSixEditableFigmaStatesAndAdvancesByConfirmedActions()
        {
            Type type = GetTutorialType();
            MethodInfo show = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Transform) }, null);
            Assert.NotNull(show);

            GameObject canvasObject = new GameObject("Tutorial Test Canvas");
            try
            {
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                Component overlay = show.Invoke(null, new object[] { canvas.transform }) as Component;
                Assert.NotNull(overlay);
                Assert.AreEqual("VC5 Demo Tutorial Overlay", overlay.gameObject.name);
                Assert.AreEqual(canvas.transform.childCount - 1, overlay.transform.GetSiblingIndex());

                string[] pages =
                {
                    "Tutorial_INTRO", "Tutorial_WAIT_CARD_DRAG", "Tutorial_PREVIEW",
                    "Tutorial_RESULT", "Tutorial_HUD_INFO", "Tutorial_SCORE_FLOW"
                };
                for (int i = 0; i < pages.Length; i++)
                {
                    Transform page = FindTransform(overlay.transform, pages[i]);
                    Assert.NotNull(page, "Missing Figma state: " + pages[i]);
                    Assert.AreEqual(i == 0, page.gameObject.activeSelf, pages[i] + " initial visibility is wrong.");
                }

                string[] requiredText =
                {
                    "你只需要打牌！",
                    "棋子的移动、攻击和战术行动，都通过打牌完成。\n把卡牌交给棋子，他会自动执行效果！",
                    "目标：率先获得 9 分",
                    "按住「移动射击」, 打出他！",
                    "从手牌按住这张牌，\n<color=#F6C344>拖到</color>青色框内的「游骑射手」。",
                    "按住并拖动到棋子上！",
                    "这是效果预览：保持按住，先看结果",
                    "黄色路线是移动路径 · 青色框是落点 · 红色框是将被攻击的敌人",
                    "一张牌完成移动和攻击",
                    "得分和法力是两回事",
                    "中央七格是得分区",
                    "① 你打出一张牌\n通常交出行动权",
                    "② AI 行动\n然后再次轮到你",
                    "③ 双方都放弃\n结算得分并进入弃牌",
                    "快速牌使用后不交出行动权"
                };
                string[] actualText = overlay.GetComponentsInChildren<Text>(true).Select(item => item.text).ToArray();
                foreach (string expected in requiredText)
                    CollectionAssert.Contains(actualText, expected);

                Image mobileShotHighlight = FindComponent<Image>(overlay.transform, "Highlight_MobileShot");
                Assert.NotNull(mobileShotHighlight);
                AssertRect(mobileShotHighlight.rectTransform, new Vector2(585f, -913f), new Vector2(126f, 167f));

                Text mobileShotLabel = FindComponent<Text>(overlay.transform, "MobileShotLabel");
                Assert.NotNull(mobileShotLabel);
                Assert.AreEqual("按住并拖动到棋子上！", mobileShotLabel.text);
                AssertRect(mobileShotLabel.rectTransform, new Vector2(370f, -958f), new Vector2(230f, 39f));

                RectTransform leftInputBlocker = FindTransform(overlay.transform, "InputBlocker_Left") as RectTransform;
                RectTransform rightInputBlocker = FindTransform(overlay.transform, "InputBlocker_Right") as RectTransform;
                Assert.NotNull(leftInputBlocker);
                Assert.NotNull(rightInputBlocker);
                Assert.AreEqual(260f, leftInputBlocker.sizeDelta.y, 0.01f,
                    "The complete first card height must remain available for pointer-down input.");
                Assert.AreEqual(260f, rightInputBlocker.sizeDelta.y, 0.01f);
                Assert.IsNull(FindTransform(overlay.transform, "InputBlocker_Bottom"),
                    "A bottom blocker would cover the lower edge of the first card.");
                Assert.NotNull(FindComponent<Image>(overlay.transform, "Highlight_Ranger"));
                Assert.NotNull(FindComponent<Image>(overlay.transform, "Highlight_ScoreZone"));
                Assert.NotNull(FindComponent<Image>(overlay.transform, "Highlight_CurrentScore"));
                Assert.NotNull(FindComponent<Image>(overlay.transform, "Highlight_Mana"));

                Button start = FindComponent<Button>(overlay.transform, "StartTutorialButton");
                Assert.NotNull(start);
                start.onClick.Invoke();
                Assert.IsTrue(FindTransform(overlay.transform, "Tutorial_WAIT_CARD_DRAG").gameObject.activeSelf);
                Assert.IsFalse(FindTransform(overlay.transform, "Tutorial_INTRO").gameObject.activeSelf);

                MethodInfo showResult = type.GetMethod("ShowResultAfterSuccessfulPlay",
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(showResult, "The successful-play transition must be explicit and testable.");
                showResult.Invoke(overlay, null);
                Assert.IsTrue(FindTransform(overlay.transform, "Tutorial_RESULT").gameObject.activeSelf);

                FindComponent<Button>(overlay.transform, "ResultContinueButton").onClick.Invoke();
                Assert.IsTrue(FindTransform(overlay.transform, "Tutorial_HUD_INFO").gameObject.activeSelf);
                FindComponent<Button>(overlay.transform, "HudContinueButton").onClick.Invoke();
                Assert.IsTrue(FindTransform(overlay.transform, "Tutorial_SCORE_FLOW").gameObject.activeSelf);

                PropertyInfo blocking = type.GetProperty("IsBlockingDemoAI", BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(blocking);
                Assert.IsTrue((bool)blocking.GetValue(null));
                MethodInfo aiBlocked = typeof(AIPlayerVc5Demo).GetMethod("IsTutorialBlocked",
                    BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(aiBlocked, "The Demo AI update gate must expose the tutorial boundary.");
                Assert.IsTrue((bool)aiBlocked.Invoke(null, null));

                FindComponent<Button>(overlay.transform, "FinishTutorialButton").onClick.Invoke();
                Assert.IsTrue(overlay == null, "The final button should destroy the tutorial overlay.");
                Assert.IsFalse((bool)blocking.GetValue(null), "Destroying the tutorial must release the AI gate.");
            }
            finally
            {
                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void TutorialInputGate_AllowsOnlyMobileShotUntilTheTutorialPlaySucceeds()
        {
            Type type = GetTutorialType();
            MethodInfo canDrag = type.GetMethod("CanBeginTutorialCardDrag",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(canDrag);
            MethodInfo canPlay = type.GetMethod("CanPlayTutorialCardOnTarget",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(canPlay);

            PropertyInfo blocking = type.GetProperty("IsBlockingDemoAI", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(blocking);

            GameObject canvasObject = new GameObject("Tutorial Input Gate Canvas");
            try
            {
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Component overlay = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Transform) }, null).Invoke(null, new object[] { canvas.transform }) as Component;
                Assert.NotNull(overlay);
                Assert.IsTrue((bool)blocking.GetValue(null));
                FindComponent<Button>(overlay.transform, "StartTutorialButton").onClick.Invoke();
                Assert.IsTrue((bool)canDrag.Invoke(null, new object[] { MobileShotId }));
                Assert.IsFalse((bool)canDrag.Invoke(null, new object[] { "vc5_demo_c3_heavy_break" }));
                Assert.IsTrue((bool)canPlay.Invoke(null, new object[] { MobileShotId, Vc5C3Rules.Ranger }));
                Assert.IsFalse((bool)canPlay.Invoke(null, new object[] { MobileShotId, Vc5C3Rules.Sniper }));

                type.GetMethod("Dismiss", BindingFlags.Public | BindingFlags.Instance).Invoke(overlay, null);
                Assert.IsFalse((bool)blocking.GetValue(null));
                Assert.IsTrue((bool)canDrag.Invoke(null, new object[] { "vc5_demo_c3_heavy_break" }),
                    "Outside the tutorial, the gate must not restrict normal card input.");
                Assert.IsTrue((bool)canPlay.Invoke(null, new object[] { "vc5_demo_c3_heavy_break", Vc5C3Rules.Sniper }));
            }
            finally
            {
                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static Type GetTutorialType()
        {
            Type type = Type.GetType(TutorialTypeName);
            Assert.NotNull(type, "Vc5DemoTutorialOverlay should exist.");
            return type;
        }

        private static bool InvokeShouldShow(MethodInfo method, GameType type, string playerDeck, string aiDeck)
        {
            return (bool)method.Invoke(null, new object[] { type, playerDeck, aiDeck });
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            Transform target = FindTransform(root, name);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static Transform FindTransform(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
        }

        private static void AssertRect(RectTransform rect, Vector2 expectedPosition, Vector2 expectedSize)
        {
            Assert.AreEqual(expectedPosition.x, rect.anchoredPosition.x, 0.01f);
            Assert.AreEqual(expectedPosition.y, rect.anchoredPosition.y, 0.01f);
            Assert.AreEqual(expectedSize.x, rect.sizeDelta.x, 0.01f);
            Assert.AreEqual(expectedSize.y, rect.sizeDelta.y, 0.01f);
        }
    }
}
#endif
