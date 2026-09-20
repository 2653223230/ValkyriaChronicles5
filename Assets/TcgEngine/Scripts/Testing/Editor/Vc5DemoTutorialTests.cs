#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.AI;
using TcgEngine.Gameplay;
using TcgEngine.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5DemoTutorialTests : Vc5LogicTestBase
    {
        private const string TutorialTypeName = "TcgEngine.UI.Vc5DemoTutorialOverlay, Assembly-CSharp";
        private const string MobileShotId = "vc5_demo_c3_mobile_shot";
        private const string R4MobileShotId = "vc5_demo_r4_mobile_shot";

        [Test]
        public void TutorialPolicy_ShowsC3AndR4InteractiveFlowsOnlyInSoloDemo()
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
            Assert.IsTrue(InvokeShouldShow(shouldShow, GameType.Solo,
                Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId));
            Assert.IsFalse(InvokeShouldShow(shouldShow, GameType.Solo,
                Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureC3DeckId));
            Assert.IsFalse(InvokeShouldShow(shouldShow, GameType.Multiplayer,
                Vc5DemoBootstrap.RangedPressureC3DeckId, Vc5DemoBootstrap.MobileAssaultDeckId));
            Assert.IsFalse(InvokeShouldShow(shouldShow, GameType.Multiplayer,
                Vc5DemoBootstrap.CommandR4DeckId, Vc5DemoBootstrap.SteadyAssaultDeckId));
            Assert.IsTrue(InvokeShouldShow(isDemoSolo, GameType.Solo,
                Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId));
        }

        [Test]
        public void R4SoloTutorialDeck_DrawsMobileShotFirst_WithoutFreezingRemainingShuffle()
        {
            DeckData deck = DeckData.Get(Vc5DemoBootstrap.CommandR4DeckId);
            Assert.NotNull(deck);

            Game game = new Game("vc5_r4_tutorial_opening", 2);
            game.settings.game_type = GameType.Solo;
            GameLogic logic = new GameLogic(true);
            logic.SetData(game);
            HashSet<string> secondCards = new HashSet<string>();

            for (int i = 0; i < 24; i++)
            {
                logic.SetPlayerDeck(game.players[0], deck);
                Assert.AreEqual(20, game.players[0].cards_deck.Count);
                Assert.AreEqual(R4MobileShotId, game.players[0].cards_deck[0].card_id);
                secondCards.Add(game.players[0].cards_deck[1].card_id);
            }

            Assert.Greater(secondCards.Count, 1);
        }

        [Test]
        public void R4TutorialOverlay_BuildsTenFigmaStatesAndAdvancesByRealActionSignals()
        {
            Type type = GetTutorialType();
            MethodInfo show = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Transform), typeof(string) }, null);
            Assert.NotNull(show, "R4 tutorial needs a deterministic deck-specific construction entry.");

            GameObject canvasObject = new GameObject("R4 Tutorial Test Canvas");
            try
            {
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Component overlay = show.Invoke(null, new object[] { canvas.transform, Vc5DemoBootstrap.CommandR4DeckId }) as Component;
                Assert.NotNull(overlay);

                string[] pages =
                {
                    "Tutorial_R4_INTRO", "Tutorial_R4_WAIT_CARD_DRAG", "Tutorial_R4_PREVIEW",
                    "Tutorial_R4_CARD_SUCCESS", "Tutorial_R4_RANGER", "Tutorial_R4_SNIPER", "Tutorial_R4_COMMANDER",
                    "Tutorial_R4_COMMAND_RANGE", "Tutorial_R4_VICTORY", "Tutorial_R4_FLOW",
                    "Tutorial_R4_COMPLETE"
                };
                for (int i = 0; i < pages.Length; i++)
                {
                    Transform page = FindTransform(overlay.transform, pages[i]);
                    Assert.NotNull(page, "Missing approved Figma state: " + pages[i]);
                    Assert.AreEqual(i == 0, page.gameObject.activeSelf);
                }

                string[] requiredText =
                {
                    "最主要操作是：打牌！",
                    "你需要通过打牌来控制棋子！",
                    "按住「移动射击」",
                    "*点击卡牌可在左侧查看卡牌效果",
                    "按住不放，先看效果预览",
                    "确认预览后松开，打出这张牌",
                    "恭喜你，成功打出了一张牌！",
                    "游戏的主要操作依然是打牌",
                    "特性：实际移动后，下一张伤害牌伤害 +1。\n\n技能「侧翼机动」：支付 1 法力，快速移动 1 格，每回合一次。",
                    "特性「稳固射击」：本回合没有实际移动时，自己执行的伤害牌伤害 +1。",
                    "① 点击游骑射手",
                    "② 点击「侧翼机动」",
                    "点击棋子后出现技能按钮，点击它！",
                    "③ 选择相邻青色格",
                    "青色区域：指挥官攻击范围（2 格）",
                    "临时卡牌，只能在本回合使用！",
                    "范围外：不能被指挥",
                    "范围内：可被指挥",
                    "指挥官技能：战术筹划",
                    "率先获得 9 胜利分",
                    "快速牌和快速技能不会交出行动权。",
                    "准备好后，开始正式对战"
                };
                string[] actualText = overlay.GetComponentsInChildren<Text>(true).Select(item => item.text).ToArray();
                foreach (string expected in requiredText)
                    Assert.IsTrue(actualText.Any(item => item.Contains(expected)), "Missing Figma copy: " + expected);

                AssertRect(FindComponent<Image>(overlay.transform, "Highlight_R4MobileShot").rectTransform,
                    new Vector2(583f, -903f), new Vector2(133f, 177f));
                AssertRect(FindComponent<Image>(overlay.transform, "Highlight_R4Ranger").rectTransform,
                    new Vector2(831f, -723f), new Vector2(98f, 116f));
                AssertRect(FindComponent<Image>(overlay.transform, "R4PreviewEnemy").rectTransform,
                    new Vector2(824f, -243f), new Vector2(95f, 115f));
                AssertRect(FindComponent<Image>(overlay.transform, "R4PreviewLanding").rectTransform,
                    new Vector2(825f, -415f), new Vector2(110f, 125f));
                AssertRect(FindComponent<Image>(overlay.transform, "R4ReleaseHint").rectTransform,
                    new Vector2(640f, -900f), new Vector2(640f, 82f));

                FindComponent<Button>(overlay.transform, "StartTutorialButton").onClick.Invoke();
                Assert.IsTrue(FindTransform(overlay.transform, pages[1]).gameObject.activeSelf);

                type.GetMethod("ShowResultAfterSuccessfulPlay", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(overlay, null);
                Assert.IsTrue(FindTransform(overlay.transform, pages[3]).gameObject.activeSelf);
                FindComponent<Button>(overlay.transform, "R4CardSuccessContinueButton").onClick.Invoke();
                Assert.IsTrue(FindTransform(overlay.transform, pages[4]).gameObject.activeSelf);

                AssertReadableTitle(overlay.transform, "R4RangerTitle", "游骑射手");
                AssertReadableTitle(overlay.transform, "R4SniperTitle", "狙击手");
                AssertReadableTitle(overlay.transform, "R4CommanderTitle", "战场指挥官");

                Transform rangerSelectUnit = FindTransform(overlay.transform, "R4RangerPrompt_SelectUnit");
                Transform rangerSelectAbility = FindTransform(overlay.transform, "R4RangerPrompt_SelectAbility");
                Transform rangerSelectDestination = FindTransform(overlay.transform, "R4RangerPrompt_SelectDestination");
                Assert.IsTrue(rangerSelectUnit.gameObject.activeSelf);
                Assert.IsFalse(rangerSelectAbility.gameObject.activeSelf);
                Assert.IsFalse(rangerSelectDestination.gameObject.activeSelf);

                MethodInfo notifySelected = type.GetMethod("NotifyBoardCardSelected", BindingFlags.Public | BindingFlags.Static);
                MethodInfo notifyAbilityStarted = type.GetMethod("NotifyAbilityStarted", BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(notifySelected);
                Assert.NotNull(notifyAbilityStarted);
                notifySelected.Invoke(null, new object[] { Vc5R4Rules.Ranger });
                Assert.IsTrue(rangerSelectAbility.gameObject.activeSelf);
                notifyAbilityStarted.Invoke(null, new object[] { Vc5R4Rules.MoveSkill, Vc5R4Rules.Ranger });
                Assert.IsTrue(rangerSelectDestination.gameObject.activeSelf);

                MethodInfo notifyAbility = type.GetMethod("NotifyAbilityResolved", BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(notifyAbility);
                notifyAbility.Invoke(null, new object[] { Vc5R4Rules.MoveSkill, Vc5R4Rules.Ranger });
                Assert.IsTrue(FindTransform(overlay.transform, pages[5]).gameObject.activeSelf);

                Button sniperContinue = FindComponent<Button>(overlay.transform, "SniperContinueButton");
                Assert.IsTrue(sniperContinue.interactable, "The sniper page must never require clicking the unit.");
                sniperContinue.onClick.Invoke();
                Assert.IsTrue(FindTransform(overlay.transform, pages[6]).gameObject.activeSelf);

                Transform commanderSelectUnit = FindTransform(overlay.transform, "R4CommanderPrompt_SelectUnit");
                Transform commanderSelectAbility = FindTransform(overlay.transform, "R4CommanderPrompt_SelectAbility");
                Transform commanderSelectDiscard = FindTransform(overlay.transform, "R4CommanderPrompt_SelectDiscard");
                Transform commanderSelectChoice = FindTransform(overlay.transform, "R4CommanderPrompt_SelectChoice");
                Assert.IsTrue(commanderSelectUnit.gameObject.activeSelf);
                notifySelected.Invoke(null, new object[] { Vc5R4Rules.Commander });
                Assert.IsTrue(commanderSelectAbility.gameObject.activeSelf);
                notifyAbilityStarted.Invoke(null, new object[] { Vc5R4Rules.PrepareSkill, Vc5R4Rules.Commander });
                Assert.IsTrue(commanderSelectDiscard.gameObject.activeSelf);
                type.GetMethod("NotifyCommanderChoiceShown", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                Assert.IsTrue(commanderSelectChoice.gameObject.activeSelf);

                notifyAbility.Invoke(null, new object[] { Vc5R4Rules.PrepareSkill, Vc5R4Rules.Commander });
                Assert.IsTrue(FindTransform(overlay.transform, pages[7]).gameObject.activeSelf);
                Assert.IsNull(FindTransform(overlay.transform, "R4CommandRangeHighlight"));
                Assert.NotNull(FindTransform(overlay.transform, "R4CommandRangeArea"));
                Assert.NotNull(FindTransform(overlay.transform, "R4CommanderRangeOriginHighlight"));
                Assert.NotNull(FindTransform(overlay.transform, "R4CommandRangeSniperHighlight"));
                Assert.NotNull(FindTransform(overlay.transform, "R4CommandRangeRangerHighlight"));
                FindComponent<Button>(overlay.transform, "CommandRangeContinueButton").onClick.Invoke();
                FindComponent<Button>(overlay.transform, "VictoryContinueButton").onClick.Invoke();
                Assert.NotNull(FindTransform(overlay.transform, "Highlight_R4OpponentResources"));
                AssertRect(FindComponent<Image>(overlay.transform, "R4FlowPanel").rectTransform,
                    new Vector2(407f, -105f), new Vector2(1095f, 275f));
                AssertRect(FindComponent<Image>(overlay.transform, "Highlight_R4Resources").rectTransform,
                    new Vector2(129f, -1029f), new Vector2(381f, 51f));
                AssertRect(FindComponent<Image>(overlay.transform, "Highlight_R4OpponentResources").rectTransform,
                    new Vector2(1184f, -35f), new Vector2(410f, 70f));
                AssertRect(FindComponent<Image>(overlay.transform, "Highlight_R4Cost").rectTransform,
                    new Vector2(630f, -888f), new Vector2(90f, 90f));
                FindComponent<Button>(overlay.transform, "FlowContinueButton").onClick.Invoke();
                Assert.IsTrue(FindTransform(overlay.transform, pages[10]).gameObject.activeSelf);
                Assert.AreEqual("开始正式对战",
                    FindComponent<Text>(overlay.transform, "FormalMatchButtonLabel").text);
            }
            finally
            {
                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void R4TutorialOverlay_UsesApprovedSecondPassTimingAndFocusedHighlights()
        {
            Type type = GetTutorialType();
            FieldInfo delay = type.GetField("R4CardSuccessDelaySeconds",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(delay, "The successful card play transition needs an explicit reviewed delay.");
            Assert.AreEqual(2f, (float)delay.GetRawConstantValue(), 0.001f);

            MethodInfo canStartSuccessDelay = type.GetMethod("CanStartR4SuccessDelay",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(canStartSuccessDelay,
                "The two-second delay must start from presentation completion, not when the card merely leaves hand.");
            Assert.IsFalse((bool)canStartSuccessDelay.Invoke(null, new object[] { true, false, false, false }));
            Assert.IsFalse((bool)canStartSuccessDelay.Invoke(null, new object[] { true, true, false, true }));
            Assert.IsFalse((bool)canStartSuccessDelay.Invoke(null, new object[] { true, true, true, false }));
            Assert.IsTrue((bool)canStartSuccessDelay.Invoke(null, new object[] { true, true, true, true }));

            Assert.NotNull(type.GetMethod("PlaceRangerBoardLabel",
                BindingFlags.NonPublic | BindingFlags.Instance),
                "The ranger label panel, not its clipped child text, must follow the runtime unit highlight.");
            MethodInfo chooseDestination = type.GetMethod("ChoosePreferredR4Destination",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(chooseDestination,
                "04C must choose one recommended adjacent destination instead of merging all legal slots.");
            int chosen = (int)chooseDestination.Invoke(null, new object[]
            {
                new Vector2(100f, 100f),
                new List<Vector2> { new Vector2(40f, 100f), new Vector2(160f, 160f), new Vector2(100f, 40f) }
            });
            Assert.AreEqual(1, chosen, "Prefer the single up-right adjacent cell shown by the current tutorial state.");

            GameObject canvasObject = new GameObject("R4 Tutorial Second Pass Canvas");
            try
            {
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Component overlay = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Transform), typeof(string) }, null)
                    .Invoke(null, new object[] { canvas.transform, Vc5DemoBootstrap.CommandR4DeckId }) as Component;
                Assert.NotNull(overlay);

                Assert.AreEqual("这是一个卡牌战棋游戏！",
                    FindComponent<Text>(overlay.transform, "R4IntroEyebrow").text);
                Assert.AreEqual("最主要操作是：打牌！",
                    FindComponent<Text>(overlay.transform, "R4IntroTitle").text);
                Assert.AreEqual("你需要通过打牌来控制棋子！",
                    FindComponent<Text>(overlay.transform, "R4IntroBody").text);
                Assert.AreEqual("跳过教学，我是高手",
                    FindComponent<Text>(overlay.transform, "IntroSkipTutorialLabel").text);
                Assert.AreEqual("*点击卡牌可在左侧查看卡牌效果",
                    FindComponent<Text>(overlay.transform, "R4WaitCardDetailHint").text);
                Assert.AreEqual("点击棋子后出现技能按钮，点击它！",
                    FindComponent<Text>(overlay.transform, "R4RangerAbilityLabel").text);
                Assert.AreEqual("指挥官技能：战术筹划",
                    FindComponent<Text>(overlay.transform, "R4CommanderAbilityLabel").text);

                Image choiceTop = FindComponent<Image>(overlay.transform, "R4CommanderChoiceHighlight_Top");
                Assert.NotNull(choiceTop);
                Assert.AreEqual(0.96f, choiceTop.color.r, 0.02f);
                Assert.AreEqual(0.76f, choiceTop.color.g, 0.02f);
                Assert.AreEqual("三选一", FindComponent<Text>(overlay.transform, "R4CommanderChoiceLabel").text);

                Transform rangeArea = FindTransform(overlay.transform, "R4CommandRangeArea");
                Assert.NotNull(rangeArea);
                Assert.NotNull(rangeArea.GetComponent("Vc5TutorialEllipseGraphic"),
                    "07 must render a cyan ellipse, not a filled rectangular union box.");
                Assert.NotNull(rangeArea.GetComponent<CanvasRenderer>(),
                    "The range ellipse needs a CanvasRenderer or it exists but remains invisible in game.");
                Assert.LessOrEqual(FindComponent<Text>(overlay.transform, "R4TemporaryCardNote")
                    .rectTransform.sizeDelta.x, 400f);
            }
            finally
            {
                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void R4TutorialDynamicHighlight_ResizesVisibleBordersWithRuntimeRect()
        {
            Type type = GetTutorialType();
            MethodInfo resize = type.GetMethod("SetHighlightTopLeft",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(resize,
                "Dynamic tutorial highlights must resize their four visible borders, not only the transparent parent.");

            GameObject canvasObject = new GameObject("R4 Dynamic Highlight Canvas");
            try
            {
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Component overlay = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Transform), typeof(string) }, null)
                    .Invoke(null, new object[] { canvas.transform, Vc5DemoBootstrap.CommandR4DeckId }) as Component;
                RectTransform highlight = FindComponent<Image>(overlay.transform,
                    "R4RangerAbilityHighlight").rectTransform;

                resize.Invoke(overlay, new object[]
                {
                    highlight,
                    new Vector2(100f, 200f),
                    new Vector2(320f, 140f),
                });

                AssertRect(FindComponent<Image>(highlight, "R4RangerAbilityHighlight_Top").rectTransform,
                    Vector2.zero, new Vector2(320f, 5f));
                AssertRect(FindComponent<Image>(highlight, "R4RangerAbilityHighlight_Bottom").rectTransform,
                    new Vector2(0f, -135f), new Vector2(320f, 5f));
                AssertRect(FindComponent<Image>(highlight, "R4RangerAbilityHighlight_Left").rectTransform,
                    Vector2.zero, new Vector2(5f, 140f));
                AssertRect(FindComponent<Image>(highlight, "R4RangerAbilityHighlight_Right").rectTransform,
                    new Vector2(315f, 0f), new Vector2(5f, 140f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void R4CommanderDiscardHighlight_SwitchesFromFirstCardToConfirmButton()
        {
            Type type = GetTutorialType();
            MethodInfo chooseTarget = type.GetMethod("ChooseCommanderDiscardHighlightTarget",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(chooseTarget);

            GameObject firstObject = new GameObject("First discard card", typeof(RectTransform), typeof(CardSelectorCard));
            GameObject secondObject = new GameObject("Second discard card", typeof(RectTransform), typeof(CardSelectorCard));
            GameObject confirmObject = new GameObject("Confirm discard", typeof(RectTransform));
            try
            {
                CardSelectorCard first = firstObject.GetComponent<CardSelectorCard>();
                CardSelectorCard second = secondObject.GetComponent<CardSelectorCard>();
                first.SetIndex(0);
                second.SetIndex(1);
                CardSelectorCard[] cards = { first, second };
                RectTransform confirm = confirmObject.GetComponent<RectTransform>();

                Assert.AreSame(first.transform, chooseTarget.Invoke(null, new object[] { -1, confirm, cards }),
                    "Before choosing a discard, 06C should highlight only the first card.");
                Assert.AreSame(confirm, chooseTarget.Invoke(null, new object[] { 0, confirm, cards }),
                    "After choosing a discard, 06C should highlight only the confirm button.");
                Assert.AreSame(first.transform, chooseTarget.Invoke(null, new object[] { -1, confirm, cards }),
                    "Cancelling the selection should return the highlight to the first card.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
                UnityEngine.Object.DestroyImmediate(confirmObject);
            }
        }

        [Test]
        public void R4TutorialInputGate_AllowsOnlyTheExpectedCardAndSkills()
        {
            Type type = GetTutorialType();
            MethodInfo show = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Transform), typeof(string) }, null);
            MethodInfo canUseAbility = type.GetMethod("CanUseTutorialAbility", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(show);
            Assert.NotNull(canUseAbility);

            GameObject canvasObject = new GameObject("R4 Tutorial Gate Canvas");
            try
            {
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                Component overlay = show.Invoke(null, new object[] { canvas.transform, Vc5DemoBootstrap.CommandR4DeckId }) as Component;
                FindComponent<Button>(overlay.transform, "StartTutorialButton").onClick.Invoke();

                MethodInfo canDrag = type.GetMethod("CanBeginTutorialCardDrag", BindingFlags.Public | BindingFlags.Static);
                MethodInfo canPlay = type.GetMethod("CanPlayTutorialCardOnTarget", BindingFlags.Public | BindingFlags.Static);
                Assert.IsTrue((bool)canDrag.Invoke(null, new object[] { R4MobileShotId }));
                Assert.IsFalse((bool)canDrag.Invoke(null, new object[] { "vc5_demo_r4_heavy_break" }));
                Assert.IsTrue((bool)canPlay.Invoke(null, new object[] { R4MobileShotId, Vc5R4Rules.Ranger }));
                Assert.IsFalse((bool)canPlay.Invoke(null, new object[] { R4MobileShotId, Vc5R4Rules.Sniper }));

                type.GetMethod("ShowResultAfterSuccessfulPlay", BindingFlags.Public | BindingFlags.Instance).Invoke(overlay, null);
                FindComponent<Button>(overlay.transform, "R4CardSuccessContinueButton").onClick.Invoke();
                Assert.IsTrue((bool)canUseAbility.Invoke(null,
                    new object[] { Vc5R4Rules.Ranger, Vc5R4Rules.MoveSkill }));
                Assert.IsFalse((bool)canUseAbility.Invoke(null,
                    new object[] { Vc5R4Rules.Commander, Vc5R4Rules.PrepareSkill }));

                type.GetMethod("NotifyAbilityResolved", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { Vc5R4Rules.MoveSkill, Vc5R4Rules.Ranger });
                FindComponent<Button>(overlay.transform, "SniperContinueButton").onClick.Invoke();
                Assert.IsTrue((bool)canUseAbility.Invoke(null,
                    new object[] { Vc5R4Rules.Commander, Vc5R4Rules.PrepareSkill }));
            }
            finally
            {
                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
            }
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

        private static void AssertReadableTitle(Transform root, string name, string expected)
        {
            Text title = FindComponent<Text>(root, name);
            Assert.NotNull(title, "Missing character title node: " + name);
            Assert.AreEqual(expected, title.text);
            Assert.GreaterOrEqual(title.fontSize, 30);
            Assert.Greater(title.color.a, 0.99f);
            Assert.AreEqual(VerticalWrapMode.Overflow, title.verticalOverflow,
                "Character names must not disappear because the legacy Text bounds truncate the glyphs.");
        }
    }
}
#endif
