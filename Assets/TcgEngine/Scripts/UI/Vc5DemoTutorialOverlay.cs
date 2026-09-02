using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Interactive six-step onboarding for the C3 Solo Demo match.
    /// Built at runtime so scene prefabs and network flows remain unchanged.
    /// </summary>
    public class Vc5DemoTutorialOverlay : MonoBehaviour
    {
        public const string RootName = "VC5 Demo Tutorial Overlay";
        public const string MobileShotId = "vc5_demo_c3_mobile_shot";

        public enum TutorialStep
        {
            Intro,
            WaitCardDrag,
            Preview,
            Result,
            HudInfo,
            ScoreFlow
        }

        private static readonly Color Panel = new Color(0.035f, 0.085f, 0.11f, 0.98f);
        private static readonly Color Ink = new Color(0.025f, 0.075f, 0.095f, 1f);
        private static readonly Color Cyan = new Color(0.09f, 0.90f, 0.87f, 1f);
        private static readonly Color Yellow = new Color(0.96f, 0.76f, 0.27f, 1f);
        private static readonly Color Red = new Color(0.94f, 0.35f, 0.35f, 1f);
        private static readonly Color Muted = new Color(0.82f, 0.88f, 0.90f, 1f);

        private static Vc5DemoTutorialOverlay active;

        private readonly List<GameObject> pages = new List<GameObject>();
        private Font font;
        private TutorialStep step;
        private string trackedCardUid;
        private float releasedAt = -1f;
        private float showResultAt = -1f;

        public TutorialStep CurrentStep => step;
        public static bool IsBlockingDemoAI => active != null && active.isActiveAndEnabled;

        public static bool IsDemoSoloMatch(GameType gameType, string playerDeckId, string aiDeckId)
        {
            return gameType == GameType.Solo && IsDemoDeck(playerDeckId) && IsDemoDeck(aiDeckId);
        }

        public static bool ShouldShowFor(GameType gameType, string playerDeckId, string aiDeckId)
        {
            return IsDemoSoloMatch(gameType, playerDeckId, aiDeckId)
                && playerDeckId == Vc5DemoBootstrap.RangedPressureC3DeckId;
        }

        public static bool ShouldShowCurrentMatch()
        {
            return ShouldShowFor(GameClient.game_settings.game_type, GetPlayerDeckId(), GetAIDeckId());
        }

        public static bool IsCurrentDemoSoloMatch()
        {
            return IsDemoSoloMatch(GameClient.game_settings.game_type, GetPlayerDeckId(), GetAIDeckId());
        }

        public static bool CanBeginTutorialCardDrag(string cardId)
        {
            if (!IsBlockingDemoAI)
                return true;
            return (active.step == TutorialStep.WaitCardDrag || active.step == TutorialStep.Preview)
                && cardId == MobileShotId;
        }

        public static bool CanPlayTutorialCardOnTarget(string cardId, string targetCardId)
        {
            if (!IsBlockingDemoAI)
                return true;
            return (active.step == TutorialStep.WaitCardDrag || active.step == TutorialStep.Preview)
                && cardId == MobileShotId
                && targetCardId == Vc5C3Rules.Ranger;
        }

        public static Vc5DemoTutorialOverlay Show(Transform parent)
        {
            if (parent == null)
                return null;

            Vc5DemoTutorialOverlay existing = parent.GetComponentInChildren<Vc5DemoTutorialOverlay>(true);
            if (existing != null)
            {
                active = existing;
                existing.gameObject.SetActive(true);
                existing.SetStep(TutorialStep.Intro);
                existing.transform.SetAsLastSibling();
                return existing;
            }

            GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup),
                typeof(Vc5DemoTutorialOverlay));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            Vc5DemoTutorialOverlay overlay = root.GetComponent<Vc5DemoTutorialOverlay>();
            active = overlay;
            overlay.Build();
            root.transform.SetAsLastSibling();
            return overlay;
        }

        public void Dismiss()
        {
            if (active == this)
                active = null;
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        public void ShowResultAfterSuccessfulPlay()
        {
            trackedCardUid = null;
            releasedAt = -1f;
            showResultAt = -1f;
            SetStep(TutorialStep.Result);
        }

        private void OnDestroy()
        {
            if (active == this)
                active = null;
        }

        private void Update()
        {
            if (step != TutorialStep.WaitCardDrag && step != TutorialStep.Preview)
                return;

            if (showResultAt >= 0f && Time.unscaledTime >= showResultAt)
            {
                ShowResultAfterSuccessfulPlay();
                return;
            }

            HandCard dragging = HandCard.GetDrag();
            Card card = dragging != null ? dragging.GetCard() : null;
            if (card != null && card.card_id == MobileShotId)
            {
                trackedCardUid = card.uid;
                releasedAt = -1f;
                Card actor = GetHoveredBoardCard();
                bool correctTarget = actor != null
                    && actor.card_id == Vc5C3Rules.Ranger
                    && actor.player_id == GameClient.Get().GetPlayerID();
                SetStep(correctTarget ? TutorialStep.Preview : TutorialStep.WaitCardDrag);
                return;
            }

            if (string.IsNullOrEmpty(trackedCardUid))
            {
                SetStep(TutorialStep.WaitCardDrag);
                return;
            }

            if (!IsTrackedCardStillInHand())
            {
                if (showResultAt < 0f)
                    showResultAt = Time.unscaledTime + 0.8f;
                return;
            }

            if (releasedAt < 0f)
                releasedAt = Time.unscaledTime;
            if (Time.unscaledTime - releasedAt >= 0.25f)
            {
                trackedCardUid = null;
                releasedAt = -1f;
                SetStep(TutorialStep.WaitCardDrag);
            }
        }

        private void Build()
        {
            font = FindFont();
            BuildIntro();
            BuildWaitCardDrag();
            BuildPreview();
            BuildResult();
            BuildHudInfo();
            BuildScoreFlow();
            SetStep(TutorialStep.Intro);
        }

        private void BuildIntro()
        {
            Transform page = CreatePage("Tutorial_INTRO", 0.48f, true);
            Transform panel = CreatePanel("ObjectivePanel", page, new Vector2(70f, 150f), new Vector2(650f, 610f), Cyan);
            TextAt("Intro_Eyebrow", panel, "最重要的事：", 23, FontStyle.Bold, new Vector2(50f, 55f), new Vector2(550f, 38f), Yellow);
            TextAt("Intro_Title", panel, "你只需要打牌！", 42, FontStyle.Bold, new Vector2(50f, 110f), new Vector2(550f, 64f), Color.white);
            TextAt("Intro_Body", panel, "棋子的移动、攻击和战术行动，都通过打牌完成。\n把卡牌交给棋子，他会自动执行效果！",
                25, FontStyle.Normal, new Vector2(50f, 200f), new Vector2(550f, 120f), Muted);
            Transform goal = CreatePanel("GoalChip", panel, new Vector2(60f, 370f), new Vector2(530f, 82f), Yellow);
            TextAt("GoalText", goal, "目标：率先获得 9 分", 26, FontStyle.Bold, Vector2.zero, new Vector2(530f, 82f), Color.white, TextAnchor.MiddleCenter);
            Button start = ButtonAt("StartTutorialButton", panel, new Vector2(80f, 500f), new Vector2(490f, 70f));
            start.onClick.AddListener(() => SetStep(TutorialStep.WaitCardDrag));
            TextAt("StartTutorialLabel", start.transform, "试着打出一张牌吧！ →", 26, FontStyle.Bold,
                Vector2.zero, new Vector2(490f, 70f), Ink, TextAnchor.MiddleCenter);
            PageLabel(page, "教学 1 / 6");
        }

        private void BuildWaitCardDrag()
        {
            Transform page = CreateInteractivePage("Tutorial_WAIT_CARD_DRAG", 0.40f);
            Transform panel = CreatePanel("WaitPromptPanel", page, new Vector2(70f, 160f), new Vector2(560f, 310f), Cyan);
            TextAt("WaitStep", panel, "现在，轮到你操作了!", 22, FontStyle.Bold, new Vector2(45f, 50f), new Vector2(440f, 38f), Yellow);
            TextAt("WaitTitle", panel, "按住「移动射击」, 打出他！", 36, FontStyle.Bold, new Vector2(45f, 105f), new Vector2(470f, 55f), Color.white);
            TextAt("WaitBody", panel, "从手牌按住这张牌，\n<color=#F6C344>拖到</color>青色框内的「游骑射手」。", 24, FontStyle.Normal,
                new Vector2(45f, 185f), new Vector2(470f, 86f), Muted);
            Highlight("Highlight_Hand", page, new Vector2(600f, 800f), new Vector2(720f, 265f), Cyan, 0.05f);
            Highlight("Highlight_MobileShot", page, new Vector2(585f, 913f), new Vector2(126f, 167f), Yellow, 0.04f);
            Highlight("Highlight_Ranger", page, new Vector2(815f, 715f), new Vector2(120f, 130f), Cyan, 0.08f);
            TextAt("MobileShotLabel", page, "按住并拖动到棋子上！", 20, FontStyle.Bold,
                new Vector2(370f, 958f), new Vector2(230f, 39f), Yellow, TextAnchor.MiddleCenter);
            TextAt("RangerLabel", page, "正确目标\n游骑射手", 21, FontStyle.Bold,
                new Vector2(770f, 650f), new Vector2(210f, 60f), Cyan, TextAnchor.MiddleCenter);
            Transform waiting = CreatePanel("WaitingChip", page, new Vector2(725f, 520f), new Vector2(470f, 62f), Yellow);
            TextAt("WaitingText", waiting, "等待你拖动卡牌……", 22, FontStyle.Bold, Vector2.zero,
                new Vector2(470f, 62f), Yellow, TextAnchor.MiddleCenter);
            TextAt("WrongHint", page, "拖到其他位置：卡牌返回手牌，不扣法力，教程继续等待。", 18, FontStyle.Normal,
                new Vector2(105f, 500f), new Vector2(560f, 45f), Muted);
            PageLabel(page, "操作 2 / 6");
        }

        private void BuildPreview()
        {
            Transform page = CreateInteractivePage("Tutorial_PREVIEW", 0.25f);
            Transform header = CreatePanel("PreviewHeader", page, new Vector2(315f, 45f), new Vector2(1290f, 145f), Yellow);
            TextAt("PreviewTitle", header, "这是效果预览：保持按住，先看结果", 34, FontStyle.Bold,
                new Vector2(35f, 20f), new Vector2(1220f, 48f), Color.white, TextAnchor.MiddleCenter);
            TextAt("PreviewBody", header, "黄色路线是移动路径 · 青色框是落点 · 红色框是将被攻击的敌人", 22,
                FontStyle.Normal, new Vector2(85f, 82f), new Vector2(1120f, 34f), Muted, TextAnchor.MiddleCenter);
            Highlight("Preview_Ranger", page, new Vector2(815f, 715f), new Vector2(120f, 130f), Cyan, 0.10f);
            Highlight("Preview_Destination", page, new Vector2(827f, 437f), new Vector2(108f, 103f), Cyan, 0.18f);
            Highlight("Preview_Enemy", page, new Vector2(822f, 265f), new Vector2(105f, 105f), Red, 0.08f);
            RectAt("PreviewRouteMove", page, new Vector2(875f, 540f), new Vector2(10f, 175f), Yellow, false);
            RectAt("PreviewRouteAttack", page, new Vector2(875f, 370f), new Vector2(10f, 67f), Red, false);
            TextAt("PreviewRangerLabel", page, "目标正确", 22, FontStyle.Bold, new Vector2(740f, 615f), new Vector2(260f, 36f), Cyan, TextAnchor.MiddleCenter);
            TextAt("PreviewDestinationLabel", page, "自动落点", 22, FontStyle.Bold, new Vector2(775f, 455f), new Vector2(210f, 36f), Cyan, TextAnchor.MiddleCenter);
            TextAt("PreviewEnemyLabel", page, "正对面的敌方棋子\n预计受到伤害", 21, FontStyle.Bold, new Vector2(755f, 205f), new Vector2(245f, 60f), Red, TextAnchor.MiddleCenter);
            Transform legend = CreatePanel("PreviewLegend", page, new Vector2(1335f, 720f), new Vector2(500f, 245f), Cyan);
            TextAt("PreviewLegendTitle", legend, "无需再选择敌人", 25, FontStyle.Bold, new Vector2(45f, 30f), new Vector2(410f, 38f), Cyan);
            TextAt("PreviewLegendBody", legend, "系统已经自动决定：\n1. 移动路线和最终落点\n2. 攻击正对面的敌方棋子\n3. 预计伤害与技能加成",
                20, FontStyle.Normal, new Vector2(45f, 85f), new Vector2(405f, 130f), Muted);
            TextAt("ReleaseHint", page, "现在松开鼠标，执行这个效果", 24, FontStyle.Bold,
                new Vector2(665f, 898f), new Vector2(590f, 38f), Yellow, TextAnchor.MiddleCenter);
            PageLabel(page, "操作 3 / 6");
        }

        private void BuildResult()
        {
            Transform page = CreatePage("Tutorial_RESULT", 0.42f, true);
            Transform header = CreatePanel("ResultHeader", page, new Vector2(430f, 50f), new Vector2(1060f, 145f), Cyan);
            TextAt("ResultTitle", header, "一张牌完成移动和攻击", 38, FontStyle.Bold,
                new Vector2(40f, 20f), new Vector2(980f, 52f), Color.white, TextAnchor.MiddleCenter);
            TextAt("ResultBody", header, "预览中的移动和伤害已经执行。", 22, FontStyle.Normal,
                new Vector2(90f, 85f), new Vector2(880f, 34f), Muted, TextAnchor.MiddleCenter);
            Highlight("Result_Enemy", page, new Vector2(825f, 270f), new Vector2(105f, 105f), Red, 0.08f);
            Highlight("Result_Landing", page, new Vector2(825f, 455f), new Vector2(105f, 125f), Cyan, 0.10f);
            RectAt("ResultMoveRoute", page, new Vector2(872f, 580f), new Vector2(10f, 170f), Yellow, false);
            RectAt("ResultAttackRoute", page, new Vector2(872f, 375f), new Vector2(10f, 80f), Red, false);
            TextAt("ResultEnemyLabel", page, "正对面的敌人\n生命值降低", 22, FontStyle.Bold,
                new Vector2(755f, 205f), new Vector2(245f, 60f), Red, TextAnchor.MiddleCenter);
            TextAt("ResultLandingLabel", page, "移动落点", 24, FontStyle.Bold,
                new Vector2(935f, 495f), new Vector2(140f, 36f), Cyan);
            TextAt("ResultAttackLabel", page, "随后攻击 ↑", 22, FontStyle.Bold,
                new Vector2(890f, 395f), new Vector2(180f, 38f), Red);
            Transform passive = CreatePanel("PassivePanel", page, new Vector2(145f, 680f), new Vector2(550f, 245f), Cyan);
            TextAt("PassiveTitle", passive, "为什么伤害增加？", 27, FontStyle.Bold,
                new Vector2(40f, 35f), new Vector2(470f, 38f), Cyan);
            TextAt("PassiveBody", passive, "游骑射手移动后触发「机动火力」：\n下一张伤害牌伤害 +1。\n这里只说明结果，不要求额外操作。",
                21, FontStyle.Normal, new Vector2(40f, 100f), new Vector2(465f, 115f), Color.white);
            Button next = ButtonAt("ResultContinueButton", page, new Vector2(1345f, 930f), new Vector2(445f, 72f));
            next.onClick.AddListener(() => SetStep(TutorialStep.HudInfo));
            TextAt("ResultContinueLabel", next.transform, "看懂了，继续教学", 24, FontStyle.Bold,
                Vector2.zero, new Vector2(445f, 72f), Ink, TextAnchor.MiddleCenter);
            PageLabel(page, "结果 4 / 6");
        }

        private void BuildHudInfo()
        {
            Transform page = CreatePage("Tutorial_HUD_INFO", 0.42f, true);
            Transform panel = CreatePanel("ResourcePanel", page, new Vector2(70f, 180f), new Vector2(650f, 390f), Cyan);
            TextAt("HudEyebrow", panel, "看懂左下角", 22, FontStyle.Bold, new Vector2(45f, 45f), new Vector2(500f, 38f), Yellow);
            TextAt("HudTitle", panel, "得分和法力是两回事", 36, FontStyle.Bold, new Vector2(45f, 100f), new Vector2(550f, 55f), Color.white);
            TextAt("HudBody", panel, "0/9：当前得分 / 获胜所需分数。\n蓝色圆点：当前可用法力，打牌会消耗。每回合+1\n\n卡牌左上角的橙色数字是费用。",
                23, FontStyle.Normal, new Vector2(45f, 180f), new Vector2(550f, 170f), Muted);
            Highlight("Highlight_CurrentScore", page, new Vector2(125f, 985f), new Vector2(135f, 82f), Yellow, 0.08f);
            Highlight("Highlight_Mana", page, new Vector2(255f, 985f), new Vector2(185f, 82f), Cyan, 0.08f);
            TextAt("CurrentScoreLabel", page, "当前得分", 20, FontStyle.Bold, new Vector2(110f, 935f), new Vector2(165f, 40f), Yellow, TextAnchor.MiddleCenter);
            TextAt("ManaLabel", page, "法力值", 20, FontStyle.Bold, new Vector2(255f, 935f), new Vector2(185f, 40f), Cyan, TextAnchor.MiddleCenter);
            Button next = ButtonAt("HudContinueButton", page, new Vector2(1350f, 900f), new Vector2(420f, 72f));
            next.onClick.AddListener(() => SetStep(TutorialStep.ScoreFlow));
            TextAt("HudContinueLabel", next.transform, "继续", 24, FontStyle.Bold, Vector2.zero, new Vector2(420f, 72f), Ink, TextAnchor.MiddleCenter);
            PageLabel(page, "界面 5 / 6");
        }

        private void BuildScoreFlow()
        {
            Transform page = CreatePage("Tutorial_SCORE_FLOW", 0.38f, true);
            Highlight("Highlight_ScoreZone", page, new Vector2(805f, 395f), new Vector2(315f, 315f), Cyan, 0.10f);
            Transform flow = CreatePanel("TurnFlowPanel", page, new Vector2(350f, 60f), new Vector2(1220f, 270f), Cyan);
            TextAt("TurnFlowTitle", flow, "接下来，一回合会这样进行", 27, FontStyle.Bold,
                new Vector2(0f, 25f), new Vector2(1220f, 45f), Yellow, TextAnchor.MiddleCenter);
            FlowStep(flow, "FlowStep1", new Vector2(50f, 90f), Cyan, "① 你打出一张牌\n通常交出行动权");
            FlowStep(flow, "FlowStep2", new Vector2(450f, 90f), Yellow, "② AI 行动\n然后再次轮到你");
            FlowStep(flow, "FlowStep3", new Vector2(850f, 90f), Red, "③ 双方都放弃\n结算得分并进入弃牌");
            TextAt("FastCardNote", flow, "快速牌使用后不交出行动权", 18, FontStyle.Normal,
                new Vector2(310f, 195f), new Vector2(600f, 34f), Yellow, TextAnchor.MiddleCenter);
            Transform panel = CreatePanel("ScoreZonePanel", page, new Vector2(70f, 360f), new Vector2(630f, 310f), Cyan);
            TextAt("ZoneEyebrow", panel, "地图上的得分目标", 22, FontStyle.Bold, new Vector2(45f, 45f), new Vector2(500f, 38f), Yellow);
            TextAt("ZoneTitle", panel, "中央七格是得分区", 36, FontStyle.Bold, new Vector2(45f, 100f), new Vector2(540f, 55f), Color.white);
            TextAt("ZoneBody", panel, "得分阶段，得分区人数更多的一方 +2 分；\n人数相同，双方各 +1 分。", 23, FontStyle.Normal,
                new Vector2(45f, 185f), new Vector2(540f, 88f), Muted);
            TextAt("ZoneLabel", page, "得分区", 28, FontStyle.Bold, new Vector2(835f, 520f), new Vector2(255f, 50f), Color.white, TextAnchor.MiddleCenter);
            Button finish = ButtonAt("FinishTutorialButton", page, new Vector2(1350f, 900f), new Vector2(420f, 72f));
            finish.onClick.AddListener(Dismiss);
            TextAt("FinishTutorialLabel", finish.transform, "知道了，开始自由对战", 24, FontStyle.Bold,
                Vector2.zero, new Vector2(420f, 72f), Ink, TextAnchor.MiddleCenter);
            PageLabel(page, "目标 6 / 6");
        }

        private void SetStep(TutorialStep next)
        {
            step = next;
            for (int i = 0; i < pages.Count; i++)
                pages[i].SetActive(i == (int)next);
        }

        private Transform CreatePage(string name, float dimAlpha, bool blocksRaycasts)
        {
            GameObject page = new GameObject(name, typeof(RectTransform));
            page.layer = gameObject.layer;
            page.transform.SetParent(transform, false);
            Stretch(page.GetComponent<RectTransform>());
            pages.Add(page);
            Image dim = RectAt("DimMask", page.transform, Vector2.zero, new Vector2(1920f, 1080f),
                new Color(0.01f, 0.03f, 0.045f, dimAlpha), blocksRaycasts);
            Stretch(dim.rectTransform);
            return page.transform;
        }

        private Transform CreateInteractivePage(string name, float dimAlpha)
        {
            Transform page = CreatePage(name, dimAlpha, false);
            // Leave only the first hand-card area open. Pointer drag/up events continue
            // to the originating HandCard while the cursor crosses these blockers.
            RectAt("InputBlocker_Top", page, Vector2.zero, new Vector2(1920f, 820f), Color.clear, true);
            RectAt("InputBlocker_Left", page, new Vector2(0f, 820f), new Vector2(580f, 260f), Color.clear, true);
            RectAt("InputBlocker_Right", page, new Vector2(790f, 820f), new Vector2(1130f, 260f), Color.clear, true);
            return page;
        }

        private void FlowStep(Transform parent, string name, Vector2 position, Color border, string value)
        {
            Transform box = CreatePanel(name, parent, position, new Vector2(320f, 82f), border);
            TextAt(name + "Text", box, value, 20, FontStyle.Bold, Vector2.zero, new Vector2(320f, 82f), Color.white, TextAnchor.MiddleCenter);
        }

        private void PageLabel(Transform page, string value)
        {
            TextAt("PageLabel", page, value, 20, FontStyle.Bold, new Vector2(1680f, 35f), new Vector2(170f, 36f), Muted, TextAnchor.MiddleCenter);
        }

        private Transform CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color border)
        {
            Image image = RectAt(name, parent, position, size, Panel, false);
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
            return image.transform;
        }

        private Image Highlight(string name, Transform parent, Vector2 position, Vector2 size, Color color, float alpha)
        {
            // The legacy Game UI material renders low-alpha Image fills as opaque on
            // some cameras. Build an explicit hollow frame so highlights never cover
            // the card, unit, score zone, or HUD value they are explaining.
            Image image = RectAt(name, parent, position, size, Color.clear, false);
            const float border = 5f;
            RectAt(name + "_Top", image.transform, Vector2.zero, new Vector2(size.x, border), color, false);
            RectAt(name + "_Bottom", image.transform, new Vector2(0f, size.y - border),
                new Vector2(size.x, border), color, false);
            RectAt(name + "_Left", image.transform, Vector2.zero, new Vector2(border, size.y), color, false);
            RectAt(name + "_Right", image.transform, new Vector2(size.x - border, 0f),
                new Vector2(border, size.y), color, false);
            return image;
        }

        private Button ButtonAt(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image image = RectAt(name, parent, position, size, Cyan, true);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private Image RectAt(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool raycast)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);
            SetTopLeft(obj.GetComponent<RectTransform>(), position, size);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private Text TextAt(string name, Transform parent, string value, int size, FontStyle style,
            Vector2 position, Vector2 bounds, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);
            SetTopLeft(obj.GetComponent<RectTransform>(), position, bounds);
            Text text = obj.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Card GetHoveredBoardCard()
        {
            GameClient client = GameClient.Get();
            GameBoard board = GameBoard.Get();
            if (client == null || board == null || !client.IsReady())
                return null;
            Vector3 world = board.RaycastMouseBoard();
            BSlot bslot = BSlot.GetNearest(world);
            if (bslot == null)
                return null;
            return Vc5DemoGrid.GetDisplayedSlotCard(client.GetGameData(), bslot.GetSlot(world));
        }

        private bool IsTrackedCardStillInHand()
        {
            GameClient client = GameClient.Get();
            Player player = client != null ? client.GetPlayer() : null;
            return player != null && player.GetHandCard(trackedCardUid) != null;
        }

        private Font FindFont()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Text source = canvas != null ? canvas.GetComponentInChildren<Text>(true) : null;
            if (source != null && source.font != null)
                return source.font;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static string GetPlayerDeckId()
        {
            return GameClient.player_settings != null && GameClient.player_settings.deck != null
                ? GameClient.player_settings.deck.tid : string.Empty;
        }

        private static string GetAIDeckId()
        {
            return GameClient.ai_settings != null && GameClient.ai_settings.deck != null
                ? GameClient.ai_settings.deck.tid : string.Empty;
        }

        private static bool IsDemoDeck(string deckId)
        {
            return deckId == Vc5DemoBootstrap.MobileAssaultDeckId
                || deckId == Vc5DemoBootstrap.RangedPressureDeckId
                || deckId == Vc5DemoBootstrap.RangedPressureC3DeckId;
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
