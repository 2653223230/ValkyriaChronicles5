using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    public class Vc5TutorialEllipseGraphic : MaskableGraphic
    {
        public Color fillColor = new Color(0.09f, 0.90f, 0.87f, 0.035f);
        public Color strokeColor = new Color(0.09f, 0.90f, 0.87f, 0.90f);
        public float strokeWidth = 5f;

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect area = rectTransform.rect;
            const int segments = 64;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = area.center;
            vertex.color = fillColor;
            helper.AddVert(vertex);

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vertex.position = area.center + new Vector2(Mathf.Cos(angle) * area.width * 0.5f,
                    Mathf.Sin(angle) * area.height * 0.5f);
                vertex.color = fillColor;
                helper.AddVert(vertex);
                if (i > 0)
                    helper.AddTriangle(0, i, i + 1);
            }

            int ringStart = helper.currentVertCount;
            float innerX = Mathf.Max(0f, area.width * 0.5f - strokeWidth);
            float innerY = Mathf.Max(0f, area.height * 0.5f - strokeWidth);
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertex.color = strokeColor;
                vertex.position = area.center + new Vector2(cos * area.width * 0.5f, sin * area.height * 0.5f);
                helper.AddVert(vertex);
                vertex.position = area.center + new Vector2(cos * innerX, sin * innerY);
                helper.AddVert(vertex);
                if (i > 0)
                {
                    int previous = ringStart + (i - 1) * 2;
                    int current = ringStart + i * 2;
                    helper.AddTriangle(previous, current, previous + 1);
                    helper.AddTriangle(current, current + 1, previous + 1);
                }
            }
        }
    }

    /// <summary>
    /// Interactive six-step onboarding for the C3 Solo Demo match.
    /// Built at runtime so scene prefabs and network flows remain unchanged.
    /// </summary>
    public class Vc5DemoTutorialOverlay : MonoBehaviour
    {
        public const string RootName = "VC5 Demo Tutorial Overlay";
        public const string MobileShotId = "vc5_demo_c3_mobile_shot";
        public const string R4MobileShotId = "vc5_demo_r4_mobile_shot";
        private const float R4CardSuccessDelaySeconds = 2f;

        public enum TutorialStep
        {
            Intro,
            WaitCardDrag,
            Preview,
            Result,
            HudInfo,
            ScoreFlow,
            R4Intro,
            R4WaitCardDrag,
            R4Preview,
            R4CardSuccess,
            R4Ranger,
            R4Sniper,
            R4Commander,
            R4CommandRange,
            R4Victory,
            R4Flow,
            R4Complete
        }

        private static readonly Color Panel = new Color(0.035f, 0.085f, 0.11f, 0.98f);
        private static readonly Color Ink = new Color(0.025f, 0.075f, 0.095f, 1f);
        private static readonly Color Cyan = new Color(0.09f, 0.90f, 0.87f, 1f);
        private static readonly Color Yellow = new Color(0.96f, 0.76f, 0.27f, 1f);
        private static readonly Color Red = new Color(0.94f, 0.35f, 0.35f, 1f);
        private static readonly Color Muted = new Color(0.82f, 0.88f, 0.90f, 1f);

        private static Vc5DemoTutorialOverlay active;
        private static bool suppressNextR4Tutorial;

        private readonly List<GameObject> pages = new List<GameObject>();
        private Font font;
        private TutorialStep step;
        private string trackedCardUid;
        private float releasedAt = -1f;
        private float showResultAt = -1f;
        private bool r4CardAbilityResolved;
        private bool r4CardDamagePresentationComplete;
        private bool r4Mode;
        private Button sniperContinueButton;
        private int r4RangerPromptStage;
        private int r4CommanderPromptStage;
        private readonly List<GameObject> r4RangerPrompts = new List<GameObject>();
        private readonly List<GameObject> r4CommanderPrompts = new List<GameObject>();

        public TutorialStep CurrentStep => step;
        public static bool IsBlockingDemoAI => active != null && active.isActiveAndEnabled;

        public static bool IsDemoSoloMatch(GameType gameType, string playerDeckId, string aiDeckId)
        {
            return gameType == GameType.Solo && IsDemoDeck(playerDeckId) && IsDemoDeck(aiDeckId);
        }

        public static bool ShouldShowFor(GameType gameType, string playerDeckId, string aiDeckId)
        {
            return IsDemoSoloMatch(gameType, playerDeckId, aiDeckId)
                && (playerDeckId == Vc5DemoBootstrap.RangedPressureC3DeckId
                    || playerDeckId == Vc5DemoBootstrap.CommandR4DeckId);
        }

        public static bool ShouldShowCurrentMatch()
        {
            string playerDeckId = GetPlayerDeckId();
            if (suppressNextR4Tutorial && playerDeckId == Vc5DemoBootstrap.CommandR4DeckId)
            {
                suppressNextR4Tutorial = false;
                return false;
            }
            return ShouldShowFor(GameClient.game_settings.game_type, playerDeckId, GetAIDeckId());
        }

        public static bool ShouldPrioritizeTutorialOpening(GameType gameType, string playerDeckId)
        {
            if (gameType != GameType.Solo)
                return false;
            if (playerDeckId == Vc5DemoBootstrap.CommandR4DeckId)
                return !suppressNextR4Tutorial;
            return playerDeckId == Vc5DemoBootstrap.RangedPressureC3DeckId;
        }

        public static bool IsCurrentDemoSoloMatch()
        {
            return IsDemoSoloMatch(GameClient.game_settings.game_type, GetPlayerDeckId(), GetAIDeckId());
        }

        public static bool CanBeginTutorialCardDrag(string cardId)
        {
            if (!IsBlockingDemoAI)
                return true;
            if (active.r4Mode)
                return (active.step == TutorialStep.R4WaitCardDrag || active.step == TutorialStep.R4Preview)
                    && cardId == R4MobileShotId;
            return (active.step == TutorialStep.WaitCardDrag || active.step == TutorialStep.Preview) && cardId == MobileShotId;
        }

        public static bool CanPlayTutorialCardOnTarget(string cardId, string targetCardId)
        {
            if (!IsBlockingDemoAI)
                return true;
            if (active.r4Mode)
                return (active.step == TutorialStep.R4WaitCardDrag || active.step == TutorialStep.R4Preview)
                    && cardId == R4MobileShotId && targetCardId == Vc5R4Rules.Ranger;
            return (active.step == TutorialStep.WaitCardDrag || active.step == TutorialStep.Preview)
                && cardId == MobileShotId && targetCardId == Vc5C3Rules.Ranger;
        }

        public static bool CanUseTutorialAbility(string cardId, string abilityId)
        {
            if (!IsBlockingDemoAI)
                return true;
            if (!active.r4Mode)
                return false;
            if (active.step == TutorialStep.R4Ranger)
                return cardId == Vc5R4Rules.Ranger && abilityId == Vc5R4Rules.MoveSkill;
            if (active.step == TutorialStep.R4Commander)
                return cardId == Vc5R4Rules.Commander && abilityId == Vc5R4Rules.PrepareSkill;
            return false;
        }

        public static bool KeepsTutorialActionOpportunity(string playerDeckId, string cardId)
        {
            return active != null && active.r4Mode
                && playerDeckId == Vc5DemoBootstrap.CommandR4DeckId
                && cardId == R4MobileShotId
                && (active.step == TutorialStep.R4WaitCardDrag || active.step == TutorialStep.R4Preview);
        }

        public static void NotifyBoardCardSelected(string cardId)
        {
            if (active == null || !active.r4Mode)
                return;
            if (active.step == TutorialStep.R4Ranger && cardId == Vc5R4Rules.Ranger)
                active.SetR4RangerPromptStage(1);
            else if (active.step == TutorialStep.R4Commander && cardId == Vc5R4Rules.Commander)
                active.SetR4CommanderPromptStage(1);
        }

        public static void NotifyAbilityStarted(string abilityId, string casterCardId)
        {
            if (active == null || !active.r4Mode)
                return;
            if (active.step == TutorialStep.R4Ranger && abilityId == Vc5R4Rules.MoveSkill
                && casterCardId == Vc5R4Rules.Ranger)
                active.SetR4RangerPromptStage(2);
            else if (active.step == TutorialStep.R4Commander && abilityId == Vc5R4Rules.PrepareSkill
                && casterCardId == Vc5R4Rules.Commander)
                active.SetR4CommanderPromptStage(2);
        }

        public static void NotifyCommanderChoiceShown()
        {
            if (active != null && active.r4Mode && active.step == TutorialStep.R4Commander)
                active.SetR4CommanderPromptStage(3);
        }

        public static void NotifyAbilityResolved(string abilityId, string casterCardId)
        {
            if (active == null || !active.r4Mode)
                return;
            if ((active.step == TutorialStep.R4WaitCardDrag || active.step == TutorialStep.R4Preview)
                && abilityId == R4MobileShotId + "_play" && casterCardId == R4MobileShotId)
                active.r4CardAbilityResolved = true;
            else if (active.step == TutorialStep.R4Ranger && abilityId == Vc5R4Rules.MoveSkill
                && casterCardId == Vc5R4Rules.Ranger)
                active.SetStep(TutorialStep.R4Sniper);
            else if (active.step == TutorialStep.R4Commander && abilityId == Vc5R4Rules.PrepareSkill
                && casterCardId == Vc5R4Rules.Commander)
                active.SetStep(TutorialStep.R4CommandRange);
        }

        public static void NotifyDamagePresentationComplete()
        {
            if (active != null && active.r4Mode
                && (active.step == TutorialStep.R4WaitCardDrag || active.step == TutorialStep.R4Preview)
                && !string.IsNullOrEmpty(active.trackedCardUid))
                active.r4CardDamagePresentationComplete = true;
        }

        public static Vc5DemoTutorialOverlay Show(Transform parent)
        {
            return Show(parent, GetPlayerDeckId());
        }

        public static Vc5DemoTutorialOverlay Show(Transform parent, string playerDeckId)
        {
            if (parent == null)
                return null;

            bool useR4 = playerDeckId == Vc5DemoBootstrap.CommandR4DeckId;

            Vc5DemoTutorialOverlay existing = parent.GetComponentInChildren<Vc5DemoTutorialOverlay>(true);
            if (existing != null)
            {
                if (existing.r4Mode != useR4)
                {
                    if (Application.isPlaying) Destroy(existing.gameObject);
                    else DestroyImmediate(existing.gameObject);
                }
                else
                {
                    active = existing;
                    existing.gameObject.SetActive(true);
                    existing.SetStep(useR4 ? TutorialStep.R4Intro : TutorialStep.Intro);
                    existing.transform.SetAsLastSibling();
                    return existing;
                }
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
            overlay.r4Mode = useR4;
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
            r4CardAbilityResolved = false;
            r4CardDamagePresentationComplete = false;
            SetStep(r4Mode ? TutorialStep.R4CardSuccess : TutorialStep.Result);
        }

        public void BeginFormalMatch()
        {
            suppressNextR4Tutorial = true;
            Vc5DemoMatchSetup.RequestFormalR4Match();
            GameClient.Get()?.Disconnect();
            Dismiss();
            if (Application.isPlaying)
                SceneNav.GoTo("Menu");
        }

        private void OnDestroy()
        {
            if (active == this)
                active = null;
        }

        private void Update()
        {
            UpdateR4RuntimeHighlights();
            bool dragStep = step == TutorialStep.WaitCardDrag || step == TutorialStep.Preview
                || step == TutorialStep.R4WaitCardDrag || step == TutorialStep.R4Preview;
            if (!dragStep)
                return;

            if (showResultAt >= 0f && Time.unscaledTime >= showResultAt)
            {
                ShowResultAfterSuccessfulPlay();
                return;
            }

            HandCard dragging = HandCard.GetDrag();
            Card card = dragging != null ? dragging.GetCard() : null;
            string mobileShotId = r4Mode ? R4MobileShotId : MobileShotId;
            string rangerId = r4Mode ? Vc5R4Rules.Ranger : Vc5C3Rules.Ranger;
            if (card != null && card.card_id == mobileShotId)
            {
                if (trackedCardUid != card.uid)
                {
                    showResultAt = -1f;
                    r4CardAbilityResolved = false;
                    r4CardDamagePresentationComplete = false;
                }
                trackedCardUid = card.uid;
                releasedAt = -1f;
                Card actor = GetHoveredBoardCard();
                bool correctTarget = actor != null
                    && actor.card_id == rangerId
                    && actor.player_id == GameClient.Get().GetPlayerID();
                SetStep(correctTarget
                    ? (r4Mode ? TutorialStep.R4Preview : TutorialStep.Preview)
                    : (r4Mode ? TutorialStep.R4WaitCardDrag : TutorialStep.WaitCardDrag));
                return;
            }

            if (string.IsNullOrEmpty(trackedCardUid))
            {
                SetStep(r4Mode ? TutorialStep.R4WaitCardDrag : TutorialStep.WaitCardDrag);
                return;
            }

            if (!IsTrackedCardStillInHand())
            {
                bool canStartDelay = !r4Mode || CanStartR4SuccessDelay(true, r4CardAbilityResolved,
                    r4CardDamagePresentationComplete, IsR4CardPresentationSettled());
                if (showResultAt < 0f && canStartDelay)
                    showResultAt = Time.unscaledTime + (r4Mode ? R4CardSuccessDelaySeconds : 0.8f);
                return;
            }

            if (releasedAt < 0f)
                releasedAt = Time.unscaledTime;
            if (Time.unscaledTime - releasedAt >= 0.25f)
            {
                trackedCardUid = null;
                releasedAt = -1f;
                SetStep(r4Mode ? TutorialStep.R4WaitCardDrag : TutorialStep.WaitCardDrag);
            }
        }

        private static bool CanStartR4SuccessDelay(bool cardLeftHand, bool abilityResolved,
            bool damagePresentationComplete, bool boardSettled)
        {
            return cardLeftHand && abilityResolved && damagePresentationComplete && boardSettled;
        }

        private static bool IsR4CardPresentationSettled()
        {
            foreach (BoardCard boardCard in BoardCard.GetAll())
            {
                Card card = boardCard != null ? boardCard.GetCard() : null;
                if (card != null && card.card_id == Vc5R4Rules.Ranger && boardCard.gameObject.activeInHierarchy)
                    return boardCard.IsAtTargetPosition(0.02f);
            }
            return false;
        }

        private void Build()
        {
            font = FindFont();
            if (r4Mode)
            {
                BuildR4();
                SetStep(TutorialStep.R4Intro);
                return;
            }
            BuildIntro();
            BuildWaitCardDrag();
            BuildPreview();
            BuildResult();
            BuildHudInfo();
            BuildScoreFlow();
            SetStep(TutorialStep.Intro);
        }

        private void BuildR4()
        {
            BuildR4Intro();
            BuildR4WaitCardDrag();
            BuildR4Preview();
            BuildR4CardSuccess();
            BuildR4Ranger();
            BuildR4Sniper();
            BuildR4Commander();
            BuildR4CommandRange();
            BuildR4Victory();
            BuildR4Flow();
            BuildR4Complete();
        }

        private void BuildR4Intro()
        {
            Transform page = CreatePage("Tutorial_R4_INTRO", 0.48f, true);
            Transform panel = CreatePanel("R4IntroPanel", page, new Vector2(520f, 165f), new Vector2(880f, 251f), Cyan);
            TextAt("R4IntroEyebrow", panel, "这是一个卡牌战棋游戏！", 24, FontStyle.Bold,
                new Vector2(50f, 22f), new Vector2(780f, 40f), Yellow, TextAnchor.MiddleCenter);
            TextAt("R4IntroTitle", panel, "最主要操作是：打牌！", 44, FontStyle.Bold,
                new Vector2(50f, 63f), new Vector2(780f, 62f), Color.white, TextAnchor.MiddleCenter);
            TextAt("R4IntroBody", panel, "你需要通过打牌来控制棋子！",
                27, FontStyle.Normal, new Vector2(95f, 146f), new Vector2(690f, 55f), Muted, TextAnchor.MiddleCenter);
            Button start = ButtonAt("StartTutorialButton", page, new Vector2(649f, 436f), new Vector2(390f, 78f));
            start.onClick.AddListener(() => SetStep(TutorialStep.R4WaitCardDrag));
            TextAt("StartTutorialLabel", start.transform, "开始教学", 30, FontStyle.Bold,
                Vector2.zero, new Vector2(390f, 78f), Color.white, TextAnchor.MiddleCenter);
            Button skip = DarkButtonAt("IntroSkipTutorialButton", page, new Vector2(1080f, 436f), new Vector2(250f, 78f));
            skip.onClick.AddListener(BeginFormalMatch);
            TextAt("IntroSkipTutorialLabel", skip.transform, "跳过教学，我是高手", 21, FontStyle.Bold,
                Vector2.zero, new Vector2(250f, 78f), Muted, TextAnchor.MiddleCenter);
            R4StepLabel(page, 1);
        }

        private void BuildR4WaitCardDrag()
        {
            Transform page = CreateInteractivePage("Tutorial_R4_WAIT_CARD_DRAG", 0.40f);
            R4Chrome(page, 2, "开始拖动指定卡牌");
            Transform panel = CreatePanel("R4WaitPanel", page, new Vector2(300f, 235f), new Vector2(410f, 260f), Cyan);
            TextAt("R4WaitEyebrow", panel, "轮到你操作", 23, FontStyle.Bold, new Vector2(38f, 35f), new Vector2(334f, 36f), Yellow, TextAnchor.MiddleCenter);
            TextAt("R4WaitTitle", panel, "按住「移动射击」", 32, FontStyle.Bold, new Vector2(38f, 83f), new Vector2(334f, 45f), Color.white, TextAnchor.MiddleCenter);
            TextAt("R4WaitBody", panel, "从最左侧手牌按住这张牌，\n拖到青色框内的「游骑射手」。", 23, FontStyle.Normal,
                new Vector2(40f, 150f), new Vector2(330f, 66f), Muted, TextAnchor.MiddleCenter);
            TextAt("R4WaitCardDetailHint", panel, "*点击卡牌可在左侧查看卡牌效果", 18, FontStyle.Bold,
                new Vector2(20f, 217f), new Vector2(370f, 30f), Yellow, TextAnchor.MiddleCenter);
            Highlight("Highlight_R4MobileShot", page, new Vector2(583f, 903f), new Vector2(133f, 177f), Yellow, 0f);
            TextAt("R4MobileShotLabel", page, "按住并拖动", 23, FontStyle.Bold, new Vector2(590f, 861f), new Vector2(190f, 34f), Yellow, TextAnchor.MiddleCenter);
            Highlight("Highlight_R4Ranger", page, new Vector2(831f, 723f), new Vector2(98f, 116f), Cyan, 0f);
            TextAt("R4RangerTargetLabel", page, "目标：游骑射手", 23, FontStyle.Bold, new Vector2(745f, 685f), new Vector2(270f, 34f), Cyan, TextAnchor.MiddleCenter);
            TextAt("R4WrongHint", page, "拖错位置时卡牌会回到手牌，不扣法力。", 20, FontStyle.Normal,
                new Vector2(720f, 92f), new Vector2(480f, 36f), Muted, TextAnchor.MiddleCenter);
        }

        private void BuildR4Preview()
        {
            Transform page = CreateInteractivePage("Tutorial_R4_PREVIEW", 0.25f);
            R4Chrome(page, 3, "松开且卡牌成功结算");
            Transform panel = CreatePanel("R4PreviewPanel", page, new Vector2(520f, 92f), new Vector2(900f, 165f), Yellow);
            TextAt("R4PreviewTitle", panel, "按住不放，先看效果预览", 38, FontStyle.Bold,
                new Vector2(40f, 25f), new Vector2(900f, 54f), Color.white, TextAnchor.MiddleCenter);
            TextAt("R4PreviewBody", panel, "黄色＝移动路线　青色＝最终落点　红色＝将被攻击的敌人", 24, FontStyle.Normal,
                new Vector2(40f, 95f), new Vector2(900f, 40f), Muted, TextAnchor.MiddleCenter);
            Highlight("R4PreviewLanding", page, new Vector2(825f, 415f), new Vector2(110f, 125f), Cyan, 0f);
            Highlight("R4PreviewEnemy", page, new Vector2(824f, 243f), new Vector2(95f, 115f), Red, 0f);
            TextAt("R4LandingLabel", page, "最终落点", 22, FontStyle.Bold, new Vector2(795f, 375f), new Vector2(170f, 34f), Cyan, TextAnchor.MiddleCenter);
            TextAt("R4EnemyLabel", page, "预计受到伤害", 22, FontStyle.Bold, new Vector2(780f, 205f), new Vector2(180f, 34f), Red, TextAnchor.MiddleCenter);
            Transform release = CreatePanel("R4ReleaseHint", page, new Vector2(640f, 900f), new Vector2(640f, 82f), Yellow);
            TextAt("R4ReleaseHintText", release, "确认预览后松开，打出这张牌", 28, FontStyle.Bold,
                Vector2.zero, new Vector2(640f, 82f), Yellow, TextAnchor.MiddleCenter);
            TextAt("R4PreviewCancelHint", page, "移开合法目标会返回上一步；只有成功结算才继续。", 20, FontStyle.Normal,
                new Vector2(720f, 285f), new Vector2(480f, 34f), Muted, TextAnchor.MiddleCenter);
        }

        private void BuildR4CardSuccess()
        {
            Transform page = CreatePage("Tutorial_R4_CARD_SUCCESS", 0.52f, true);
            Transform panel = CreatePanel("R4CardSuccessPanel", page, new Vector2(470f, 255f), new Vector2(980f, 420f), Cyan);
            TextAt("R4CardSuccessTitle", panel, "恭喜你，成功打出了一张牌！", 40, FontStyle.Bold,
                new Vector2(80f, 55f), new Vector2(820f, 62f), Color.white, TextAnchor.MiddleCenter);
            TextAt("R4CardSuccessSubtitle", panel, "游戏的主要操作依然是打牌", 29, FontStyle.Bold,
                new Vector2(110f, 145f), new Vector2(760f, 46f), Yellow, TextAnchor.MiddleCenter);
            TextAt("R4CardSuccessBody", panel,
                "除了通过卡牌控制棋子外，部分棋子也拥有自己的技能。\n接下来认识三名棋子的特性与操作。",
                25, FontStyle.Normal, new Vector2(120f, 220f), new Vector2(740f, 92f), Muted, TextAnchor.MiddleCenter);
            Button next = ButtonAt("R4CardSuccessContinueButton", page, new Vector2(735f, 720f), new Vector2(450f, 76f));
            next.onClick.AddListener(() => SetStep(TutorialStep.R4Ranger));
            TextAt("R4CardSuccessContinueLabel", next.transform, "认识棋子技能", 27, FontStyle.Bold,
                Vector2.zero, new Vector2(450f, 76f), Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildR4Ranger()
        {
            Transform page = CreatePage("Tutorial_R4_RANGER", 0f, false);
            R4Chrome(page, 4, "按提示完成侧翼机动");

            Transform selectUnit = CreatePromptGroup("R4RangerPrompt_SelectUnit", page);
            AddR4ActionPrompt(selectUnit, "R4RangerSelectUnit", "① 点击游骑射手",
                "先选中棋子，右侧才会显示它的技能。", "① 选棋子　② 点技能　③ 选落点");
            Highlight("R4RangerBoardHighlight", selectUnit, new Vector2(836f, 727f), new Vector2(92f, 108f), Cyan, 0f);
            Transform rangerLabel = CreatePanel("R4RangerBoardLabelPanel", selectUnit,
                new Vector2(750f, 683f), new Vector2(250f, 42f), Cyan);
            TextAt("R4RangerBoardLabel", rangerLabel, "游骑射手", 23, FontStyle.Bold,
                Vector2.zero, new Vector2(250f, 42f), Color.white, TextAnchor.MiddleCenter);

            Transform selectAbility = CreatePromptGroup("R4RangerPrompt_SelectAbility", page);
            AddR4RangerInfo(selectAbility);
            AddR4ActionPrompt(selectAbility, "R4RangerSelectAbility", "② 点击「侧翼机动」",
                "技能面板已出现；点击技能按钮，不要点击手牌。", "① 已选中　② 点技能　③ 选落点");
            Highlight("R4RangerAbilityHighlight", selectAbility, new Vector2(1248f, 402f), new Vector2(430f, 185f), Yellow, 0f);
            Transform rangerAbilityLabel = CreatePanel("R4RangerAbilityLabelPanel", selectAbility,
                new Vector2(1248f, 350f), new Vector2(430f, 42f), Cyan);
            TextAt("R4RangerAbilityLabel", rangerAbilityLabel, "点击棋子后出现技能按钮，点击它！", 21, FontStyle.Bold,
                Vector2.zero, new Vector2(430f, 42f), Color.white, TextAnchor.MiddleCenter);

            Transform selectDestination = CreatePromptGroup("R4RangerPrompt_SelectDestination", page);
            AddR4RangerInfo(selectDestination);
            AddR4ActionPrompt(selectDestination, "R4RangerSelectDestination", "③ 选择相邻青色格",
                "点击一个合法落点，游骑射手完成 1 格移动。", "① 已选中　② 已点技能　③ 选落点");
            Highlight("R4RangerDestinationHighlight", selectDestination, new Vector2(820f, 560f), new Vector2(125f, 125f), Cyan, 0f);
            SetR4RangerPromptStage(0);
        }

        private void AddR4RangerInfo(Transform parent)
        {
            Transform info = CreatePanel("R4RangerInfo", parent, new Vector2(300f, 220f), new Vector2(445f, 305f), Cyan);
            TextAt("R4RangerIndex", info, "棋子 1/3", 23, FontStyle.Bold, new Vector2(35f, 28f), new Vector2(375f, 36f), Yellow, TextAnchor.MiddleCenter);
            CharacterTitle("R4RangerTitle", info, "游骑射手", 34, new Vector2(35f, 78f), new Vector2(375f, 52f));
            TextAt("R4RangerBody", info, "特性：实际移动后，下一张伤害牌伤害 +1。\n\n技能「侧翼机动」：支付 1 法力，快速移动 1 格，每回合一次。",
                23, FontStyle.Normal, new Vector2(35f, 145f), new Vector2(375f, 135f), Muted, TextAnchor.MiddleCenter);
        }

        private void BuildR4Sniper()
        {
            Transform page = CreatePage("Tutorial_R4_SNIPER", 0.34f, false);
            R4Chrome(page, 5, "看懂后直接继续");
            TextAt("R4SniperClickHint", page, "狙击手没有主动技能，无需点击棋子", 23, FontStyle.Bold,
                new Vector2(1010f, 120f), new Vector2(360f, 40f), Color.white, TextAnchor.MiddleCenter);
            Highlight("R4SniperBoardHighlight", page, new Vector2(986f, 716f), new Vector2(110f, 145f), Cyan, 0f);
            Transform info = CreatePanel("R4SniperInfo", page, new Vector2(1045f, 225f), new Vector2(500f, 330f), Cyan);
            TextAt("R4SniperIndex", info, "棋子 2/3", 23, FontStyle.Bold, new Vector2(40f, 25f), new Vector2(420f, 36f), Yellow, TextAnchor.MiddleCenter);
            CharacterTitle("R4SniperTitle", info, "狙击手", 36, new Vector2(40f, 75f), new Vector2(420f, 54f));
            TextAt("R4SniperBody", info, "射程 3，移动 1。\n\n特性「稳固射击」：本回合没有实际移动时，自己执行的伤害牌伤害 +1。\n\n没有主动技能，主要通过卡牌行动。",
                22, FontStyle.Normal, new Vector2(40f, 145f), new Vector2(420f, 165f), Muted, TextAnchor.MiddleCenter);
            sniperContinueButton = ButtonAt("SniperContinueButton", page, new Vector2(1130f, 610f), new Vector2(330f, 70f));
            sniperContinueButton.interactable = true;
            sniperContinueButton.onClick.AddListener(() => SetStep(TutorialStep.R4Commander));
            TextAt("SniperContinueLabel", sniperContinueButton.transform, "看懂了，继续", 25, FontStyle.Bold,
                Vector2.zero, new Vector2(330f, 70f), Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildR4Commander()
        {
            Transform page = CreatePage("Tutorial_R4_COMMANDER", 0f, false);
            R4Chrome(page, 6, "生成一张临时指令");

            Transform selectUnit = CreatePromptGroup("R4CommanderPrompt_SelectUnit", page);
            AddR4ActionPrompt(selectUnit, "R4CommanderSelectUnit", "① 点击战场指挥官",
                "先选中指挥官，再使用「战术筹划」。", "① 选指挥官　② 点技能　③ 选牌并确认　④ 选指令");
            Highlight("R4CommanderBoardHighlight", selectUnit, new Vector2(917f, 774f), new Vector2(86f, 112f), Cyan, 0f);
            Transform commanderLabel = CreatePanel("R4CommanderBoardLabelPanel", selectUnit,
                new Vector2(840f, 700f), new Vector2(300f, 42f), Cyan);
            TextAt("R4CommanderBoardLabel", commanderLabel, "战场指挥官", 23, FontStyle.Bold,
                Vector2.zero, new Vector2(300f, 42f), Color.white, TextAnchor.MiddleCenter);

            Transform selectAbility = CreatePromptGroup("R4CommanderPrompt_SelectAbility", page);
            AddR4CommanderInfo(selectAbility);
            AddR4ActionPrompt(selectAbility, "R4CommanderSelectAbility", "② 点击「战术筹划」",
                "打开弃牌界面；技能本身不消耗主要行动。", "① 已选中　② 点技能　③ 选牌并确认　④ 选指令");
            Highlight("R4CommanderAbilityHighlight", selectAbility, new Vector2(1200f, 450f), new Vector2(285f, 180f), Yellow, 0f);
            Transform commanderAbilityLabel = CreatePanel("R4CommanderAbilityLabelPanel", selectAbility,
                new Vector2(1200f, 398f), new Vector2(285f, 42f), Cyan);
            TextAt("R4CommanderAbilityLabel", commanderAbilityLabel, "指挥官技能：战术筹划", 21, FontStyle.Bold,
                Vector2.zero, new Vector2(285f, 42f), Color.white, TextAnchor.MiddleCenter);

            Transform selectDiscard = CreatePromptGroup("R4CommanderPrompt_SelectDiscard", page);
            AddR4ActionPrompt(selectDiscard, "R4CommanderSelectDiscard", "③ 点击一张手牌，再点「确定弃牌」",
                "选中的牌会上浮；再次点击可以取消选择。", "① 已选中　② 已点技能　③ 选牌并确认　④ 选指令");
            Highlight("R4CommanderDiscardHighlight", selectDiscard, new Vector2(748f, 365f), new Vector2(165f, 345f), Yellow, 0f);

            Transform selectChoice = CreatePromptGroup("R4CommanderPrompt_SelectChoice", page);
            AddR4ActionPrompt(selectChoice, "R4CommanderSelectChoice", "④ 选择一张临时指令",
                "选中的指令加入手牌，仅本回合可用。", "① 已选中　② 已点技能　③ 已弃牌　④ 选指令");
            Highlight("R4CommanderChoiceHighlight", selectChoice, new Vector2(820f, 575f), new Vector2(285f, 390f), Yellow, 0f);
            Transform choiceLabel = CreatePanel("R4CommanderChoiceLabelPanel", selectChoice,
                new Vector2(820f, 522f), new Vector2(285f, 42f), Cyan);
            TextAt("R4CommanderChoiceLabel", choiceLabel, "三选一", 23, FontStyle.Bold,
                Vector2.zero, new Vector2(285f, 42f), Color.white, TextAnchor.MiddleCenter);
            SetR4CommanderPromptStage(0);
        }

        private void AddR4CommanderInfo(Transform parent)
        {
            Transform info = CreatePanel("R4CommanderInfo", parent, new Vector2(300f, 195f), new Vector2(420f, 300f), Cyan);
            TextAt("R4CommanderIndex", info, "棋子 3/3", 23, FontStyle.Bold, new Vector2(35f, 28f), new Vector2(350f, 36f), Yellow, TextAnchor.MiddleCenter);
            CharacterTitle("R4CommanderTitle", info, "战场指挥官", 34, new Vector2(35f, 78f), new Vector2(350f, 52f));
            TextAt("R4CommanderBody", info, "技能「战术筹划」：弃 1 张非临时手牌，从推进、开火、掩护中选择获得 1 张临时指令。\n0 法力 · 快速 · 每回合一次。",
                22, FontStyle.Normal, new Vector2(35f, 145f), new Vector2(350f, 125f), Muted, TextAnchor.MiddleCenter);
        }

        private void BuildR4CommandRange()
        {
            Transform page = CreatePage("Tutorial_R4_COMMAND_RANGE", 0.28f, true);
            R4Chrome(page, 7, "确认说明后继续");
            Transform info = CreatePanel("R4CommandRangeInfo", page, new Vector2(300f, 245f), new Vector2(525f, 360f), Cyan);
            TextAt("R4CommandRangeEyebrow", info, "临时指令的指挥范围", 23, FontStyle.Bold, new Vector2(35f, 32f), new Vector2(400f, 36f), Yellow, TextAnchor.MiddleCenter);
            TextAt("R4CommandRangeTitle", info, "青色区域：指挥官攻击范围（2 格）", 27, FontStyle.Bold,
                new Vector2(35f, 88f), new Vector2(455f, 52f), Color.white, TextAnchor.MiddleCenter);
            TextAt("R4CommandRangeBody", info, "临时指令生成时不绑定目标。\n出牌时，只能选择指挥范围内的其他友军。\n\n不能指挥战场指挥官自己。",
                22, FontStyle.Normal, new Vector2(35f, 170f), new Vector2(455f, 145f), Muted, TextAnchor.MiddleCenter);
            EllipseAt("R4CommandRangeArea", page, new Vector2(790f, 650f), new Vector2(390f, 260f));
            TextAt("R4CommandRangeLabel", page, "指挥官攻击范围", 22, FontStyle.Bold,
                new Vector2(868f, 594f), new Vector2(235f, 38f), Cyan, TextAnchor.MiddleCenter);
            Highlight("R4CommanderRangeOriginHighlight", page, new Vector2(940f, 785f), new Vector2(92f, 118f), Yellow, 0f);
            Highlight("R4CommandRangeSniperHighlight", page, new Vector2(1035f, 760f), new Vector2(90f, 115f), Cyan, 0f);
            TextAt("R4CommandRangeSniperLabel", page, "范围内：可被指挥", 21, FontStyle.Bold,
                new Vector2(1010f, 716f), new Vector2(260f, 36f), Cyan, TextAnchor.MiddleCenter);
            Highlight("R4CommandRangeRangerHighlight", page, new Vector2(930f, 415f), new Vector2(96f, 115f), Red, 0f);
            TextAt("R4CommandRangeRangerLabel", page, "范围外：不能被指挥", 21, FontStyle.Bold,
                new Vector2(850f, 370f), new Vector2(320f, 36f), Red, TextAnchor.MiddleCenter);
            TextAt("R4TemporaryCardNote", page, "临时卡牌，只能在本回合使用！", 22, FontStyle.Bold,
                new Vector2(1089f, 961f), new Vector2(390f, 32f), Yellow, TextAnchor.MiddleCenter);
            Button next = ButtonAt("CommandRangeContinueButton", page, new Vector2(1270f, 750f), new Vector2(300f, 70f));
            next.onClick.AddListener(() => SetStep(TutorialStep.R4Victory));
            TextAt("CommandRangeContinueLabel", next.transform, "明白了，继续", 25, FontStyle.Bold,
                Vector2.zero, new Vector2(300f, 70f), Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildR4Victory()
        {
            Transform page = CreatePage("Tutorial_R4_VICTORY", 0.36f, true);
            R4Chrome(page, 8, "点击继续");
            Transform info = CreatePanel("R4VictoryInfo", page, new Vector2(300f, 205f), new Vector2(480f, 390f), Cyan);
            TextAt("R4VictoryEyebrow", info, "如何获胜", 23, FontStyle.Bold, new Vector2(35f, 35f), new Vector2(410f, 36f), Yellow, TextAnchor.MiddleCenter);
            TextAt("R4VictoryTitle", info, "率先获得 9 胜利分", 35, FontStyle.Bold, new Vector2(35f, 85f), new Vector2(410f, 50f), Color.white, TextAnchor.MiddleCenter);
            TextAt("R4VictoryBody", info, "击杀敌方棋子：+3 分。\n\n双方都放弃后结算中央得分区：\n人数更多的一方： +2 分；\n人数相同：各 +1 分。",
                23, FontStyle.Normal, new Vector2(35f, 165f), new Vector2(410f, 180f), Muted, TextAnchor.MiddleCenter);
            Highlight("Highlight_R4ScoreZone", page, new Vector2(835f, 398f), new Vector2(250f, 295f), Cyan, 0f);
            TextAt("R4ScoreZoneLabel", page, "中央七格＝得分区", 27, FontStyle.Bold,
                new Vector2(830f, 350f), new Vector2(260f, 38f), Cyan, TextAnchor.MiddleCenter);
            TextAt("R4ScoreHudLabel", page, "胜利分会显示在双方头像附近", 21, FontStyle.Bold,
                new Vector2(1130f, 130f), new Vector2(360f, 34f), Yellow, TextAnchor.MiddleCenter);
            Button next = ButtonAt("VictoryContinueButton", page, new Vector2(1245f, 760f), new Vector2(320f, 72f));
            next.onClick.AddListener(() => SetStep(TutorialStep.R4Flow));
            TextAt("VictoryContinueLabel", next.transform, "继续", 25, FontStyle.Bold,
                Vector2.zero, new Vector2(320f, 72f), Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildR4Flow()
        {
            Transform page = CreatePage("Tutorial_R4_FLOW", 0.40f, true);
            R4Chrome(page, 9, "点击继续");
            Transform flow = CreatePanel("R4FlowPanel", page, new Vector2(407f, 105f), new Vector2(1095f, 275f), Cyan);
            TextAt("R4FlowTitle", flow, "一轮对局这样进行", 32, FontStyle.Bold,
                new Vector2(163f, 25f), new Vector2(830f, 46f), Color.white, TextAnchor.MiddleCenter);
            FlowStep(flow, "R4FlowStep1", new Vector2(74f, 106f), new Vector2(265f, 90f), Cyan, "① 你打出普通牌\n通常交出行动权");
            FlowStep(flow, "R4FlowStep2", new Vector2(418f, 105f), new Vector2(265f, 90f), Yellow, "② AI 行动\n然后再次轮到你");
            FlowStep(flow, "R4FlowStep3", new Vector2(743f, 106f), new Vector2(250f, 90f), Red, "③ 双方都放弃\n结算、弃牌、下一轮");
            TextAt("R4FastNote", flow, "快速牌和快速技能不会交出行动权。", 21, FontStyle.Bold,
                new Vector2(282f, 215f), new Vector2(570f, 32f), Yellow, TextAnchor.MiddleCenter);
            Transform info = CreatePanel("R4HudInfo", page, new Vector2(293f, 423f), new Vector2(429f, 307f), Cyan);
            CharacterTitle("R4HudInfoTitle", info, "常用界面", 30,
                new Vector2(40f, 32f), new Vector2(340f, 46f));
            TextAt("R4HudInfoBody", info, "胜利分：当前分，达到 9 获胜。\n法力值：支付卡牌和技能费用。\n橙色数字：卡牌费用。\n放弃行动：本轮不再继续操作。",
                22, FontStyle.Normal, new Vector2(42f, 100f), new Vector2(336f, 150f), Muted, TextAnchor.MiddleCenter);
            Highlight("Highlight_R4Resources", page, new Vector2(129f, 1029f), new Vector2(381f, 51f), Yellow, 0f);
            TextAt("R4PlayerResourcesLabel", page, "我方胜利分与法力值", 21, FontStyle.Bold,
                new Vector2(220f, 990f), new Vector2(260f, 34f), Yellow, TextAnchor.MiddleCenter);
            Highlight("Highlight_R4Cost", page, new Vector2(630f, 888f), new Vector2(90f, 90f), Yellow, 0f);
            TextAt("R4CostLabel", page, "卡牌费用", 21, FontStyle.Bold,
                new Vector2(572f, 850f), new Vector2(205f, 34f), Yellow, TextAnchor.MiddleCenter);
            Highlight("Highlight_R4Pass", page, new Vector2(1725f, 870f), new Vector2(180f, 180f), Cyan, 0f);
            TextAt("R4PassLabel", page, "放弃行动", 21, FontStyle.Bold,
                new Vector2(1705f, 820f), new Vector2(220f, 34f), Cyan, TextAnchor.MiddleCenter);
            Highlight("Highlight_R4OpponentResources", page, new Vector2(1184f, 35f), new Vector2(410f, 70f), Cyan, 0f);
            TextAt("R4OpponentResourcesLabel", page, "AI 胜利分与法力值", 21, FontStyle.Bold,
                new Vector2(1598f, 55f), new Vector2(270f, 34f), Cyan, TextAnchor.MiddleCenter);
            Button next = ButtonAt("FlowContinueButton", page, new Vector2(1247f, 718f), new Vector2(323f, 72f));
            next.onClick.AddListener(() => SetStep(TutorialStep.R4Complete));
            TextAt("FlowContinueLabel", next.transform, "继续", 25, FontStyle.Bold,
                Vector2.zero, new Vector2(320f, 72f), Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildR4Complete()
        {
            Transform page = CreatePage("Tutorial_R4_COMPLETE", 0.48f, true);
            Transform panel = CreatePanel("R4CompletePanel", page, new Vector2(530f, 190f), new Vector2(860f, 300f), Cyan);
            TextAt("R4CompleteEyebrow", panel, "教学演练结束", 24, FontStyle.Bold,
                new Vector2(50f, 40f), new Vector2(760f, 38f), Yellow, TextAnchor.MiddleCenter);
            TextAt("R4CompleteTitle", panel, "准备好后，开始正式对战", 40, FontStyle.Bold,
                new Vector2(50f, 100f), new Vector2(760f, 58f), Color.white, TextAnchor.MiddleCenter);
            TextAt("R4CompleteBody", panel, "接下来会结束教学对局，并重新开始一局：", 26, FontStyle.Normal,
                new Vector2(90f, 190f), new Vector2(680f, 92f), Muted, TextAnchor.MiddleCenter);
            Button start = ButtonAt("FormalMatchButton", page, new Vector2(710f, 522f), new Vector2(500f, 88f));
            start.onClick.AddListener(BeginFormalMatch);
            TextAt("FormalMatchButtonLabel", start.transform, "开始正式对战", 32, FontStyle.Bold,
                Vector2.zero, new Vector2(500f, 88f), Color.white, TextAnchor.MiddleCenter);
            R4StepLabel(page, 10);
            Transform condition = CreatePanel("R4CompleteCondition", page, new Vector2(1315f, 20f), new Vector2(320f, 48f), Cyan);
            TextAt("R4CompleteConditionText", condition, "点击开始正式对战", 18, FontStyle.Normal,
                Vector2.zero, new Vector2(320f, 48f), Muted, TextAnchor.MiddleCenter);
        }

        private void R4Chrome(Transform page, int number, string condition)
        {
            R4StepLabel(page, number);
            Transform chip = CreatePanel("R4Condition_" + number, page, new Vector2(1315f, 20f), new Vector2(320f, 48f), Cyan);
            TextAt("R4ConditionText_" + number, chip, condition, 18, FontStyle.Normal,
                Vector2.zero, new Vector2(320f, 48f), Muted, TextAnchor.MiddleCenter);
            Button skip = DarkButtonAt("SkipTutorialButton_" + number, page, new Vector2(1680f, 20f), new Vector2(190f, 48f));
            skip.onClick.AddListener(BeginFormalMatch);
            TextAt("SkipTutorialLabel_" + number, skip.transform, "跳过教学", 19, FontStyle.Normal,
                Vector2.zero, new Vector2(190f, 48f), Muted, TextAnchor.MiddleCenter);
        }

        private void R4StepLabel(Transform page, int number)
        {
            Transform chip = CreatePanel("R4StepChip_" + number, page, new Vector2(292f, 20f), new Vector2(180f, 48f), Cyan);
            TextAt("R4StepLabel_" + number, chip, "教学 " + number + "/10", 22, FontStyle.Bold,
                Vector2.zero, new Vector2(180f, 48f), new Color(0.73f, 1f, 0.98f), TextAnchor.MiddleCenter);
        }

        private Button DarkButtonAt(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image image = RectAt(name, parent, position, size, new Color(0.09f, 0.15f, 0.17f, 0.96f), true);
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.50f, 0.58f, 0.60f);
            outline.effectDistance = new Vector2(1f, -1f);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
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
            int activeIndex = r4Mode ? (int)next - (int)TutorialStep.R4Intro : (int)next;
            for (int i = 0; i < pages.Count; i++)
                pages[i].SetActive(i == activeIndex);
            if (next == TutorialStep.R4Ranger)
                SetR4RangerPromptStage(0);
            else if (next == TutorialStep.R4Commander)
                SetR4CommanderPromptStage(0);
        }

        private Transform CreatePromptGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name, typeof(RectTransform));
            group.layer = gameObject.layer;
            group.transform.SetParent(parent, false);
            Stretch(group.GetComponent<RectTransform>());
            if (name.StartsWith("R4RangerPrompt_"))
                r4RangerPrompts.Add(group);
            else if (name.StartsWith("R4CommanderPrompt_"))
                r4CommanderPrompts.Add(group);
            return group.transform;
        }

        private void AddR4ActionPrompt(Transform parent, string namePrefix, string titleValue,
            string bodyValue, string progressValue)
        {
            Transform panel = CreatePanel(namePrefix + "Panel", parent,
                new Vector2(530f, 20f), new Vector2(860f, 133f), Yellow);
            TextAt(namePrefix + "Title", panel, titleValue, 32, FontStyle.Bold,
                new Vector2(45f, 20f), new Vector2(770f, 50f), Color.white, TextAnchor.MiddleCenter);
            TextAt(namePrefix + "Body", panel, bodyValue, 22, FontStyle.Normal,
                new Vector2(45f, 75f), new Vector2(770f, 38f), Muted, TextAnchor.MiddleCenter);
            Transform progress = CreatePanel(namePrefix + "Progress", parent,
                new Vector2(650f, 158f), new Vector2(620f, 46f), Cyan);
            TextAt(namePrefix + "ProgressText", progress, progressValue, 19, FontStyle.Bold,
                Vector2.zero, new Vector2(620f, 46f), Cyan, TextAnchor.MiddleCenter);
        }

        private void SetR4RangerPromptStage(int value)
        {
            r4RangerPromptStage = Mathf.Clamp(value, 0, Mathf.Max(0, r4RangerPrompts.Count - 1));
            for (int i = 0; i < r4RangerPrompts.Count; i++)
                r4RangerPrompts[i].SetActive(i == r4RangerPromptStage);
        }

        private void SetR4CommanderPromptStage(int value)
        {
            r4CommanderPromptStage = Mathf.Clamp(value, 0, Mathf.Max(0, r4CommanderPrompts.Count - 1));
            for (int i = 0; i < r4CommanderPrompts.Count; i++)
                r4CommanderPrompts[i].SetActive(i == r4CommanderPromptStage);
        }

        private void UpdateR4RuntimeHighlights()
        {
            if (!r4Mode || !isActiveAndEnabled)
                return;

            if (step == TutorialStep.R4Ranger)
            {
                if (r4RangerPromptStage == 0)
                    FitHighlightToBoardCard("R4RangerBoardHighlight", Vc5R4Rules.Ranger, 10f);
                else if (r4RangerPromptStage == 1)
                    FitHighlightToAbilityButton("R4RangerAbilityHighlight", Vc5R4Rules.MoveSkill);
                else if (r4RangerPromptStage == 2)
                    FitHighlightToLegalAbilitySlots("R4RangerDestinationHighlight");
            }
            else if (step == TutorialStep.R4Commander)
            {
                if (r4CommanderPromptStage == 0)
                    FitHighlightToBoardCard("R4CommanderBoardHighlight", Vc5R4Rules.Commander, 10f);
                else if (r4CommanderPromptStage == 1)
                    FitHighlightToAbilityButton("R4CommanderAbilityHighlight", Vc5R4Rules.PrepareSkill);
                else if (r4CommanderPromptStage == 2)
                    FitHighlightToCommanderDiscardControls();
                else if (r4CommanderPromptStage == 3)
                    FitHighlightToCommanderChoices();
            }
            else if (step == TutorialStep.R4CommandRange)
            {
                FitHighlightToBoardCard("R4CommanderRangeOriginHighlight", Vc5R4Rules.Commander, 8f);
                FitHighlightToBoardCard("R4CommandRangeSniperHighlight", Vc5R4Rules.Sniper, 8f);
                FitHighlightToBoardCard("R4CommandRangeRangerHighlight", Vc5R4Rules.Ranger, 8f);
                FitCommandRangeArea();
                PlaceTemporaryCardNote();
            }
            else if (step == TutorialStep.R4Flow)
            {
                FitHighlightToPlayerResources("Highlight_R4Resources", PlayerUI.Get(false));
                FitHighlightToPlayerResources("Highlight_R4OpponentResources", PlayerUI.Get(true));
                PlaceLabelAboveHighlight("R4PlayerResourcesLabel", "Highlight_R4Resources");
                PlaceLabelBesideHighlight("R4OpponentResourcesLabel", "Highlight_R4OpponentResources");
                GameUI gameUI = GameUI.Get();
                FitHighlightToRect("Highlight_R4Pass",
                    gameUI != null && gameUI.end_turn_button != null ? gameUI.end_turn_button.transform as RectTransform : null, 8f);
                PlaceLabelAboveHighlight("R4PassLabel", "Highlight_R4Pass");
                foreach (HandCard handCard in HandCard.GetAll())
                {
                    CardUI cardUI = handCard != null ? handCard.GetComponent<CardUI>() : null;
                    if (cardUI == null || cardUI.cost == null || !handCard.gameObject.activeInHierarchy)
                        continue;
                    FitHighlightToRect("Highlight_R4Cost", cardUI.cost.rectTransform, 8f);
                    PlaceLabelAboveHighlight("R4CostLabel", "Highlight_R4Cost");
                    break;
                }
            }
        }

        private void FitHighlightToAbilityButton(string highlightName, string abilityId)
        {
            AbilityPanel panel = AbilityPanel.Get();
            if (panel == null || !panel.gameObject.activeInHierarchy)
                return;
            foreach (AbilityButton button in Resources.FindObjectsOfTypeAll<AbilityButton>())
            {
                AbilityData ability = button != null ? button.GetAbility() : null;
                if (ability == null || ability.id != abilityId || !button.gameObject.activeInHierarchy
                    || !button.IsVisible() || !button.transform.IsChildOf(panel.transform))
                    continue;
                FitHighlightToRect(highlightName, button.transform as RectTransform, 8f);
                if (highlightName == "R4RangerAbilityHighlight")
                    PlaceLabelAboveHighlight("R4RangerAbilityLabelPanel", highlightName);
                else if (highlightName == "R4CommanderAbilityHighlight")
                    PlaceLabelAboveHighlight("R4CommanderAbilityLabelPanel", highlightName);
                return;
            }
        }

        private void FitHighlightToCommanderDiscardControls()
        {
            CardSelector selector = CardSelector.Get();
            if (selector == null || !selector.gameObject.activeInHierarchy)
                return;
            RectTransform confirm = selector.select_button != null
                ? selector.select_button.transform as RectTransform : null;
            RectTransform target = ChooseCommanderDiscardHighlightTarget(selector.SelectionIndex, confirm,
                selector.GetComponentsInChildren<CardSelectorCard>(true));
            FitHighlightToRect("R4CommanderDiscardHighlight", target, 8f);
        }

        private static RectTransform ChooseCommanderDiscardHighlightTarget(int selectionIndex,
            RectTransform confirm, IList<CardSelectorCard> cards)
        {
            if (selectionIndex >= 0)
                return confirm;
            foreach (CardSelectorCard card in cards)
                if (card != null && card.gameObject.activeInHierarchy)
                    return card.transform as RectTransform;
            return null;
        }

        private void FitHighlightToCommanderChoices()
        {
            ChoiceSelector selector = ChoiceSelector.Get();
            if (selector == null || !selector.gameObject.activeInHierarchy || selector.choices == null)
                return;
            List<RectTransform> targets = new List<RectTransform>();
            foreach (ChoiceSelectorChoice choice in selector.choices)
                if (choice != null && choice.gameObject.activeInHierarchy)
                    targets.Add(choice.transform as RectTransform);
            FitHighlightToOverlaySpaceRects("R4CommanderChoiceHighlight", 8f, targets.ToArray());
            PlaceLabelAboveHighlight("R4CommanderChoiceLabelPanel", "R4CommanderChoiceHighlight");
        }

        private void FitHighlightToOverlaySpaceRects(string highlightName, float padding, params RectTransform[] targets)
        {
            RectTransform root = transform as RectTransform;
            RectTransform highlight = FindRect(highlightName);
            if (root == null || highlight == null)
                return;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool found = false;
            foreach (RectTransform target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                    continue;
                Vector3[] corners = new Vector3[4];
                target.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 local = root.InverseTransformPoint(corner);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                    found = true;
                }
            }
            if (!found)
                return;
            Vector2 position = new Vector2(min.x - root.rect.xMin - padding,
                root.rect.yMax - max.y - padding);
            Vector2 size = new Vector2(max.x - min.x + padding * 2f,
                max.y - min.y + padding * 2f);
            SetHighlightTopLeft(highlight, position, size);
        }

        private void FitHighlightToLegalAbilitySlots(string highlightName)
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || data.selector != SelectorType.SelectTarget)
                return;
            Card caster = data.GetCard(data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(data.selector_ability_id);
            if (caster == null || ability == null)
                return;
            Camera camera = Camera.main;
            if (camera == null)
                return;
            Vector2 casterScreen = Vector2.zero;
            foreach (BoardCard boardCard in BoardCard.GetAll())
            {
                Card card = boardCard != null ? boardCard.GetCard() : null;
                if (card != null && card.uid == caster.uid && boardCard.card_sprite != null)
                {
                    casterScreen = camera.WorldToScreenPoint(boardCard.card_sprite.bounds.center);
                    break;
                }
            }
            List<Renderer> renderers = new List<Renderer>();
            List<Vector2> centers = new List<Vector2>();
            foreach (BSlot bslot in BSlot.GetAll())
            {
                BoardSlot slotView = bslot as BoardSlot;
                Slot slot = slotView != null ? slotView.GetSlot() : Slot.None;
                if (slotView == null || data.GetSlotCard(slot) != null || !ability.CanTarget(data, caster, slot))
                    continue;
                Renderer renderer = slotView.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderers.Add(renderer);
                    centers.Add(camera.WorldToScreenPoint(renderer.bounds.center));
                }
            }
            int preferred = ChoosePreferredR4Destination(casterScreen, centers);
            if (preferred >= 0 && preferred < renderers.Count)
                FitHighlightToRenderer(highlightName, renderers[preferred], 8f);
        }

        private static int ChoosePreferredR4Destination(Vector2 casterScreen, IList<Vector2> candidates)
        {
            int bestIndex = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2 delta = candidates[i] - casterScreen;
                int directionRank = delta.x > 4f && delta.y > 4f ? 0 : (delta.y > 4f ? 1 : 2);
                float score = directionRank * 1000000f + delta.sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private void FitCommandRangeArea()
        {
            GameClient client = GameClient.Get();
            if (client == null || !client.IsReady())
                return;
            Game data = client.GetGameData();
            Card commander = null;
            foreach (BoardCard boardCard in BoardCard.GetAll())
            {
                Card card = boardCard != null ? boardCard.GetCard() : null;
                if (card != null && card.card_id == Vc5R4Rules.Commander && card.player_id == client.GetPlayerID())
                {
                    commander = card;
                    break;
                }
            }
            if (commander == null)
                return;
            int range = Vc5DemoGrid.AttackRange(commander);
            List<Renderer> renderers = new List<Renderer>();
            foreach (BSlot bslot in BSlot.GetAll())
            {
                BoardSlot slotView = bslot as BoardSlot;
                if (slotView == null)
                    continue;
                Slot slot = slotView.GetSlot();
                int dx = slot.x - commander.slot.x;
                int dy = slot.y - commander.slot.y;
                int dz = (commander.slot.x + commander.slot.y) - (slot.x + slot.y);
                if (slot.p != commander.slot.p || Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz)) > range)
                    continue;
                Renderer renderer = slotView.GetComponent<Renderer>();
                if (renderer != null)
                    renderers.Add(renderer);
            }
            FitHighlightToRenderers("R4CommandRangeArea", 4f, renderers);
            PlaceLabelAboveHighlight("R4CommandRangeLabel", "R4CommandRangeArea");
        }

        private void PlaceTemporaryCardNote()
        {
            foreach (HandCard handCard in HandCard.GetAll())
            {
                Card card = handCard != null ? handCard.GetCard() : null;
                if (card == null || !Vc5R4Rules.IsTemporary(card) || !handCard.gameObject.activeInHierarchy)
                    continue;
                PlaceLabelNearCardTopRight("R4TemporaryCardNote", handCard.transform as RectTransform);
                return;
            }
        }

        private void FitHighlightToPlayerResources(string highlightName, PlayerUI playerUI)
        {
            if (playerUI == null || playerUI.hp_txt == null || playerUI.mana_bar == null)
                return;
            FitHighlightToRects(highlightName, 8f, playerUI.hp_txt.rectTransform,
                playerUI.mana_bar.transform as RectTransform);
        }

        private void FitHighlightToBoardCard(string highlightName, string cardId, float padding)
        {
            GameClient client = GameClient.Get();
            if (client == null || !client.IsReady())
                return;
            int playerId = client.GetPlayerID();
            foreach (BoardCard boardCard in BoardCard.GetAll())
            {
                Card card = boardCard != null ? boardCard.GetCard() : null;
                if (card == null || card.card_id != cardId || card.player_id != playerId || boardCard.card_sprite == null)
                    continue;
                FitHighlightToRenderer(highlightName, boardCard.card_sprite, padding);
                if (highlightName == "R4RangerBoardHighlight")
                    PlaceRangerBoardLabel();
                else if (highlightName == "R4CommanderBoardHighlight")
                    PlaceLabelAboveHighlight("R4CommanderBoardLabelPanel", highlightName);
                else if (highlightName == "R4CommandRangeSniperHighlight")
                    PlaceLabelAboveHighlight("R4CommandRangeSniperLabel", highlightName);
                else if (highlightName == "R4CommandRangeRangerHighlight")
                    PlaceLabelAboveHighlight("R4CommandRangeRangerLabel", highlightName);
                return;
            }
        }

        private void PlaceRangerBoardLabel()
        {
            PlaceLabelAboveHighlight("R4RangerBoardLabelPanel", "R4RangerBoardHighlight");
        }

        private void PlaceLabelAboveHighlight(string labelName, string highlightName)
        {
            RectTransform label = FindRect(labelName);
            RectTransform highlight = FindRect(highlightName);
            if (label == null || highlight == null)
                return;
            Vector2 size = label.sizeDelta;
            Vector2 position = new Vector2(highlight.anchoredPosition.x + (highlight.sizeDelta.x - size.x) * 0.5f,
                Mathf.Max(4f, -highlight.anchoredPosition.y - size.y - 8f));
            SetTopLeft(label, position, size);
        }

        private void PlaceLabelBesideHighlight(string labelName, string highlightName)
        {
            RectTransform highlight = FindRect(highlightName);
            if (highlight != null)
                PlaceLabelBesideLocalRect(labelName, highlight, 12f);
        }

        private void PlaceLabelBesideRect(string labelName, RectTransform target, float gap)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera : null;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCamera, corner);
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            RectTransform root = transform as RectTransform;
            RectTransform label = FindRect(labelName);
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 localMin;
            Vector2 localMax;
            if (root == null || label == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, min, camera, out localMin)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, max, camera, out localMax))
                return;
            Vector2 targetPosition = new Vector2(localMax.x - root.rect.xMin + gap,
                root.rect.yMax - localMax.y + (localMax.y - localMin.y - label.sizeDelta.y) * 0.5f);
            float clampedX = Mathf.Clamp(targetPosition.x, 8f, 1920f - label.sizeDelta.x - 8f);
            float clampedY = Mathf.Clamp(targetPosition.y, 8f, 1080f - label.sizeDelta.y - 8f);
            SetTopLeft(label, new Vector2(clampedX, clampedY), label.sizeDelta);
        }

        private void PlaceLabelNearCardTopRight(string labelName, RectTransform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera : null;
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(targetCamera, corners[2]);
            RectTransform root = transform as RectTransform;
            RectTransform label = FindRect(labelName);
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 local;
            if (root == null || label == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, topRight, camera, out local))
                return;
            Vector2 position = new Vector2(local.x - root.rect.xMin - label.sizeDelta.x * 0.72f,
                root.rect.yMax - local.y - label.sizeDelta.y - 8f);
            position.x = Mathf.Clamp(position.x, 8f, 1920f - label.sizeDelta.x - 8f);
            position.y = Mathf.Clamp(position.y, 8f, 1080f - label.sizeDelta.y - 8f);
            SetTopLeft(label, position, label.sizeDelta);
        }

        private void PlaceLabelBesideLocalRect(string labelName, RectTransform target, float gap)
        {
            RectTransform label = FindRect(labelName);
            if (label == null || target == null)
                return;
            Vector2 position = new Vector2(target.anchoredPosition.x + target.sizeDelta.x + gap,
                -target.anchoredPosition.y + (target.sizeDelta.y - label.sizeDelta.y) * 0.5f);
            position.x = Mathf.Clamp(position.x, 8f, 1920f - label.sizeDelta.x - 8f);
            position.y = Mathf.Clamp(position.y, 8f, 1080f - label.sizeDelta.y - 8f);
            SetTopLeft(label, position, label.sizeDelta);
        }

        private RectTransform FindRect(string name)
        {
            foreach (RectTransform candidate in GetComponentsInChildren<RectTransform>(true))
                if (candidate.name == name)
                    return candidate;
            return null;
        }

        private void FitHighlightToRenderer(string highlightName, Renderer target, float padding)
        {
            Camera camera = Camera.main;
            if (target == null || camera == null)
                return;
            Bounds bounds = target.bounds;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                Vector3 screen = camera.WorldToScreenPoint(corner);
                if (screen.z <= 0f)
                    return;
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            FitHighlightToScreenRect(highlightName, min, max, padding);
        }

        private void FitHighlightToRenderers(string highlightName, float padding, IList<Renderer> targets)
        {
            Camera camera = Camera.main;
            if (camera == null || targets == null || targets.Count == 0)
                return;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool found = false;
            foreach (Renderer target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                    continue;
                Bounds bounds = target.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 screen = camera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                    if (screen.z <= 0f)
                        continue;
                    min = Vector2.Min(min, screen);
                    max = Vector2.Max(max, screen);
                    found = true;
                }
            }
            if (found)
                FitHighlightToScreenRect(highlightName, min, max, padding);
        }

        private void FitHighlightToRect(string highlightName, RectTransform target, float padding)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Canvas canvas = target.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            FitHighlightToScreenRect(highlightName, min, max, padding);
        }

        private void FitHighlightToRects(string highlightName, float padding, params RectTransform[] targets)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool found = false;
            foreach (RectTransform target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                    continue;
                Vector3[] corners = new Vector3[4];
                target.GetWorldCorners(corners);
                Canvas targetCanvas = target.GetComponentInParent<Canvas>();
                Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? targetCanvas.worldCamera : null;
                foreach (Vector3 corner in corners)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCamera, corner);
                    min = Vector2.Min(min, screen);
                    max = Vector2.Max(max, screen);
                    found = true;
                }
            }
            if (found)
                FitHighlightToScreenRect(highlightName, min, max, padding);
        }

        private void FitHighlightToScreenRect(string highlightName, Vector2 screenMin, Vector2 screenMax, float padding)
        {
            RectTransform highlight = null;
            RectTransform[] candidates = GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform candidate in candidates)
                if (candidate.name == highlightName) { highlight = candidate; break; }
            RectTransform root = transform as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (highlight == null || root == null)
                return;
            Vector2 localMin;
            Vector2 localMax;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenMin, camera, out localMin)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenMax, camera, out localMax))
                return;
            Vector2 position = new Vector2(localMin.x - root.rect.xMin - padding,
                root.rect.yMax - localMax.y - padding);
            Vector2 size = new Vector2(localMax.x - localMin.x + padding * 2f,
                localMax.y - localMin.y + padding * 2f);
            SetHighlightTopLeft(highlight, position, size);
        }

        private void SetHighlightTopLeft(RectTransform highlight, Vector2 position, Vector2 size)
        {
            SetTopLeft(highlight, position, size);
            RectTransform top = highlight.Find(highlight.name + "_Top") as RectTransform;
            if (top == null)
                return;
            const float border = 5f;
            SetTopLeft(top, Vector2.zero, new Vector2(size.x, border));
            SetTopLeft(highlight.Find(highlight.name + "_Bottom") as RectTransform,
                new Vector2(0f, size.y - border), new Vector2(size.x, border));
            SetTopLeft(highlight.Find(highlight.name + "_Left") as RectTransform,
                Vector2.zero, new Vector2(border, size.y));
            SetTopLeft(highlight.Find(highlight.name + "_Right") as RectTransform,
                new Vector2(size.x - border, 0f), new Vector2(border, size.y));
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
            FlowStep(parent, name, position, new Vector2(320f, 82f), border, value);
        }

        private void FlowStep(Transform parent, string name, Vector2 position, Vector2 size, Color border, string value)
        {
            Transform box = CreatePanel(name, parent, position, size, border);
            TextAt(name + "Text", box, value, 20, FontStyle.Bold, Vector2.zero, size, Color.white, TextAnchor.MiddleCenter);
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

        private Vc5TutorialEllipseGraphic EllipseAt(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Vc5TutorialEllipseGraphic));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);
            SetTopLeft(obj.GetComponent<RectTransform>(), position, size);
            Vc5TutorialEllipseGraphic ellipse = obj.GetComponent<Vc5TutorialEllipseGraphic>();
            ellipse.raycastTarget = false;
            return ellipse;
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

        private Text CharacterTitle(string name, Transform parent, string value, int size, Vector2 position, Vector2 bounds)
        {
            Text title = TextAt(name, parent, value, size, FontStyle.Bold, position, bounds, Color.white, TextAnchor.MiddleCenter);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            Outline outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);
            title.transform.SetAsLastSibling();
            return title;
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
                || deckId == Vc5DemoBootstrap.RangedPressureC3DeckId
                || deckId == Vc5DemoBootstrap.CommandR4DeckId
                || deckId == Vc5DemoBootstrap.SteadyAssaultDeckId;
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
