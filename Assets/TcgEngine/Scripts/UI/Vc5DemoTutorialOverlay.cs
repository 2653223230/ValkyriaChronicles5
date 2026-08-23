using System;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Two-page onboarding overlay for VC5 Demo Solo battles.
    /// It is created at runtime so the existing game prefab and network flows stay unchanged.
    /// </summary>
    public class Vc5DemoTutorialOverlay : MonoBehaviour
    {
        public const string RootName = "VC5 Demo Tutorial Overlay";

        private static readonly Color DimColor = new Color(0.012f, 0.02f, 0.03f, 0.58f);
        private static readonly Color PanelColor = new Color(0.055f, 0.075f, 0.095f, 0.96f);
        private static readonly Color InkColor = new Color(0.035f, 0.055f, 0.075f, 1f);
        private static readonly Color CyanColor = new Color(0.10f, 0.92f, 0.88f, 1f);
        private static readonly Color YellowColor = new Color(1f, 0.76f, 0.14f, 1f);
        private static readonly Color MutedColor = new Color(0.79f, 0.85f, 0.88f, 1f);

        private Font uiFont;
        
        private static Sprite generatedBadgeSprite;
        private Sprite badgeSprite;
        private Transform contentParent;
        private GameObject page1;
        private GameObject page2;

        public static bool ShouldShowFor(GameType gameType, string playerDeckId, string aiDeckId)
        {
            return gameType == GameType.Solo
                && IsDemoDeck(playerDeckId)
                && IsDemoDeck(aiDeckId);
        }

        public static bool ShouldShowCurrentMatch()
        {
            string playerDeckId = GameClient.player_settings != null
                && GameClient.player_settings.deck != null
                ? GameClient.player_settings.deck.tid
                : "";
            string aiDeckId = GameClient.ai_settings != null
                && GameClient.ai_settings.deck != null
                ? GameClient.ai_settings.deck.tid
                : "";

            return ShouldShowFor(GameClient.game_settings.game_type, playerDeckId, aiDeckId);
        }

        public static Vc5DemoTutorialOverlay Show(Transform parent)
        {
            if (parent == null)
                return null;

            Vc5DemoTutorialOverlay existing = parent.GetComponentInChildren<Vc5DemoTutorialOverlay>(true);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.ShowFirstPage();
                existing.transform.SetAsLastSibling();
                return existing;
            }

            GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Vc5DemoTutorialOverlay));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
            group.ignoreParentGroups = false;

            Vc5DemoTutorialOverlay overlay = root.GetComponent<Vc5DemoTutorialOverlay>();
            overlay.Build();
            root.transform.SetAsLastSibling();
            return overlay;
        }

        public void Dismiss()
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        private void Build()
        {
            uiFont = FindFont();
            badgeSprite = GetBadgeSprite();

            Image dim = CreateImage("DimMask", transform, Vector2.zero, new Vector2(1920f, 1080f), DimColor, true);
            SetStretch(dim.rectTransform);

            page1 = CreatePage("TutorialPage_1");
            page2 = CreatePage("TutorialPage_2");

            contentParent = page1.transform;
            BuildFirstPage();

            contentParent = page2.transform;
            BuildSecondPage();

            contentParent = null;
            ShowFirstPage();
        }

        private GameObject CreatePage(string name)
        {
            GameObject page = new GameObject(name, typeof(RectTransform));
            page.layer = gameObject.layer;
            page.transform.SetParent(transform, false);
            SetStretch(page.GetComponent<RectTransform>());
            return page;
        }

        private void ShowFirstPage()
        {
            if (page1 != null)
                page1.SetActive(true);
            if (page2 != null)
                page2.SetActive(false);
        }

        private void ShowSecondPage()
        {
            if (page1 != null)
                page1.SetActive(false);
            if (page2 != null)
                page2.SetActive(true);
        }

        private void BuildFirstPage()
        {
            CreatePanel("CoreMessage", new Vector2(72f, 52f), new Vector2(560f, 140f), YellowColor);
            CreateText("CoreMessage_Title", Find("CoreMessage"), "你只需要打牌", 38, FontStyle.Bold,
                new Vector2(28f, 20f), new Vector2(504f, 58f), Color.white, TextAnchor.MiddleCenter);
            CreateText("CoreMessage_Subtitle", Find("CoreMessage"), "移动、攻击、战术行动，都从手牌开始", 20, FontStyle.Normal,
                new Vector2(28f, 78f), new Vector2(504f, 36f), MutedColor, TextAnchor.MiddleCenter);
            CreateText("PageIndicator_1", contentParent, "1 / 2", 20, FontStyle.Bold,
                new Vector2(860f, 64f), new Vector2(100f, 44f), MutedColor, TextAnchor.MiddleCenter);

            CreateHighlight("Highlight_Hand", new Vector2(591f, 847f), new Vector2(629f, 233f));
            CreateBadge("Badge_1", "1", new Vector2(565f, 824f));
            CreateCallout("Callout_Hand", new Vector2(124f, 824f), new Vector2(430f, 120f),
                "拖出一张卡牌", "移动和攻击都要先打牌。拖出卡牌并松开，开始行动。");

            CreateHighlight("Highlight_Board", new Vector2(720f, 170f), new Vector2(486f, 660f));
            CreateBadge("Badge_2", "2", new Vector2(1192f, 146f));
            CreateCallout("Callout_Board", new Vector2(1270f, 153f), new Vector2(420f, 143f),
                "按照提示选择目标", "打出卡牌后，按提示选择己方棋子、敌人或目标格。");

            CreateHighlight("Highlight_Score", new Vector2(830f, 393f), new Vector2(260f, 300f));
            CreateBadge("Badge_3", "3", new Vector2(804f, 367f));
            CreateCallout("Callout_Score", new Vector2(366f, 460f), new Vector2(430f, 120f),
                "占领中央得分区", "让棋子进入得分区来获得分数，先达到 9 分获胜。");

            CreateHighlight("Highlight_PhaseButton", new Vector2(1656f, 815f), new Vector2(190f, 226f));
            CreateBadge("Badge_4", "4", new Vector2(1630f, 795f));
            CreateCallout("Callout_Phase", new Vector2(1282f, 675f), new Vector2(450f, 120f),
                "没有想继续打的牌？", "点击“放弃行动”，结束当前主要阶段。");

            Button next = CreateButton("NextButton", contentParent, new Vector2(1240f, 979f), new Vector2(390f, 74f));
            next.onClick.AddListener(ShowSecondPage);
            CreateText("NextButton_Label", next.transform, "下一页：资源与技能", 24, FontStyle.Bold,
                Vector2.zero, new Vector2(390f, 74f), InkColor, TextAnchor.MiddleCenter);
        }

        private void BuildSecondPage()
        {
            CreatePanel("CoreMessage_Page2", new Vector2(72f, 52f), new Vector2(650f, 140f), YellowColor);
            CreateText("CoreMessage_Page2_Title", Find("CoreMessage_Page2"), "资源、得分与棋子技能", 36, FontStyle.Bold,
                new Vector2(28f, 16f), new Vector2(594f, 58f), Color.white, TextAnchor.MiddleCenter);
            CreateText("CoreMessage_Page2_Subtitle", Find("CoreMessage_Page2"), "看懂左下角，再决定怎样赢下对局", 20,
                FontStyle.Normal, new Vector2(28f, 76f), new Vector2(594f, 38f), MutedColor, TextAnchor.MiddleCenter);
            CreateText("PageIndicator_2", contentParent, "2 / 2", 20, FontStyle.Bold,
                new Vector2(860f, 64f), new Vector2(100f, 44f), MutedColor, TextAnchor.MiddleCenter);

            CreateHighlight("Highlight_PlayerResources", new Vector2(0f, 850f), new Vector2(380f, 225f));
            CreateBadge("Badge_Resources", "1", new Vector2(356f, 824f));
            CreateCallout("Callout_Resources", new Vector2(400f, 785f), new Vector2(500f, 170f),
                "法力值与胜利分数", "蓝色法力点用于打牌和主动技能；0/9 显示当前得分，先达到 9 分获胜。");

            Transform scoring = CreatePanel("ScoringMethods", new Vector2(72f, 235f), new Vector2(550f, 330f), CyanColor);
            CreateText("ScoringMethods_Title", scoring, "两种得分方式", 28, FontStyle.Bold,
                new Vector2(28f, 20f), new Vector2(494f, 48f), Color.white, TextAnchor.MiddleLeft);
            Image killChip = CreateImage("ScoreChip_Kill", scoring, new Vector2(28f, 92f), new Vector2(92f, 72f), YellowColor, false);
            CreateText("ScoreChip_Kill_Text", killChip.transform, "+3", 30, FontStyle.Bold,
                Vector2.zero, new Vector2(92f, 72f), InkColor, TextAnchor.MiddleCenter);
            CreateText("ScoreKill_Title", scoring, "击杀敌方棋子", 23, FontStyle.Bold,
                new Vector2(146f, 86f), new Vector2(350f, 40f), Color.white, TextAnchor.MiddleLeft);
            CreateText("ScoreKill_Body", scoring, "敌方棋子死亡时，你获得 3 分。", 18, FontStyle.Normal,
                new Vector2(146f, 124f), new Vector2(350f, 42f), MutedColor, TextAnchor.MiddleLeft);

            Image zoneChip = CreateImage("ScoreChip_Zone", scoring, new Vector2(28f, 202f), new Vector2(92f, 82f),
                new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.82f), false);
            CreateText("ScoreChip_Zone_Text", zoneChip.transform, "+2 / +1", 22, FontStyle.Bold,
                Vector2.zero, new Vector2(92f, 82f), InkColor, TextAnchor.MiddleCenter);
            CreateText("ScoreZone_Title", scoring, "得分阶段", 23, FontStyle.Bold,
                new Vector2(146f, 196f), new Vector2(350f, 40f), Color.white, TextAnchor.MiddleLeft);
            CreateText("ScoreZone_Body", scoring, "得分区人数更多的一方 +2 分；人数相同，双方各 +1 分。", 18, FontStyle.Normal,
                new Vector2(146f, 234f), new Vector2(360f, 64f), MutedColor, TextAnchor.MiddleLeft);
            CreateBadge("Badge_Scoring", "2", new Vector2(596f, 209f));

            CreateHighlight("Highlight_PlayerUnits", new Vector2(695f, 630f), new Vector2(505f, 215f));
            CreateBadge("Badge_Skills", "3", new Vector2(1174f, 606f));
            CreateCallout("Callout_Skills", new Vector2(1240f, 520f), new Vector2(520f, 250f),
                "查看与使用棋子技能",
                "把鼠标移到棋子上，可以查看主动技能和被动技能。\n\n点击己方棋子，会显示可用主动技能；再点击技能按钮即可释放。被动技能会自动生效。");

            Button back = CreateSecondaryButton("BackButton", contentParent, new Vector2(930f, 979f), new Vector2(74f, 74f));
            back.onClick.AddListener(ShowFirstPage);
            CreateText("BackButton_Label", back.transform, "←", 24, FontStyle.Bold,
                Vector2.zero, new Vector2(74f, 74f), Color.white, TextAnchor.MiddleCenter);

            Button confirm = CreateButton("ConfirmButton_Page2", contentParent,
                new Vector2(1030f, 979f), new Vector2(560f, 74f));
            confirm.onClick.AddListener(Dismiss);
            CreateText("ConfirmButton_Page2_Label", confirm.transform, "知道了，开始对战", 24, FontStyle.Bold,
                Vector2.zero, new Vector2(560f, 74f), InkColor, TextAnchor.MiddleCenter);
        }






        private static bool IsDemoDeck(string deckId)
        {
            return deckId == Vc5DemoBootstrap.MobileAssaultDeckId
                || deckId == Vc5DemoBootstrap.RangedPressureDeckId;
        }

        private void CreateHighlight(string name, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(name, contentParent != null ? contentParent : transform, position, size,
                new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.08f), false);

            CreateBorder(image.transform, "Border_Top",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 6f), new Vector2(0f, -3f));
            CreateBorder(image.transform, "Border_Bottom",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f), new Vector2(0f, 3f));
            CreateBorder(image.transform, "Border_Left",
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(3f, 0f));
            CreateBorder(image.transform, "Border_Right",
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(-3f, 0f));
        }

        private void CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image border = obj.GetComponent<Image>();
            border.color = YellowColor;
            border.raycastTarget = false;
        }


        private void CreateBadge(string name, string value, Vector2 position)
        {
            Image image = CreateImage(name, contentParent != null ? contentParent : transform,
                position, new Vector2(52f, 52f), YellowColor, false);
            if (badgeSprite != null)
                image.sprite = badgeSprite;
            AddOutline(image.gameObject, InkColor, 3f);
            CreateText(name + "_Text", image.transform, value, 25, FontStyle.Bold,
                Vector2.zero, new Vector2(52f, 52f), InkColor, TextAnchor.MiddleCenter);
        }

        private void CreateCallout(string name, Vector2 position, Vector2 size, string title, string body)
        {
            Transform panel = CreatePanel(name, position, size, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.72f));
            float contentWidth = size.x - 44f;
            CreateText(name + "_Title", panel, title, 24, FontStyle.Bold,
                new Vector2(22f, 14f), new Vector2(contentWidth, 40f), Color.white, TextAnchor.MiddleLeft);
            CreateText(name + "_Body", panel, body, 19, FontStyle.Normal,
                new Vector2(22f, 51f), new Vector2(contentWidth, size.y - 60f), MutedColor, TextAnchor.UpperLeft);
        }

        private Transform CreatePanel(string name, Vector2 position, Vector2 size, Color border)
        {
            Image panel = CreateImage(name, contentParent != null ? contentParent : transform,
                position, size, PanelColor, false);
            AddOutline(panel.gameObject, border, 2f);
            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.useGraphicAlpha = true;
            return panel.transform;
        }

        private Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(name, parent, position, size, YellowColor, true);
            AddOutline(image.gameObject, new Color(1f, 1f, 1f, 0.65f), 2f);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = YellowColor;
            colors.highlightedColor = new Color(1f, 0.84f, 0.34f, 1f);
            colors.pressedColor = new Color(0.86f, 0.61f, 0.08f, 1f);
            colors.selectedColor = YellowColor;
            button.colors = colors;
            return button;
        }

        private Button CreateSecondaryButton(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(name, parent, position, size, PanelColor, true);
            AddOutline(image.gameObject, CyanColor, 2f);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = PanelColor;
            colors.highlightedColor = new Color(0.10f, 0.18f, 0.22f, 1f);
            colors.pressedColor = new Color(0.04f, 0.10f, 0.13f, 1f);
            colors.selectedColor = PanelColor;
            button.colors = colors;
            return button;
        }

        private Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool raycast)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            SetTopLeft(rect, position, size);

            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle,
            Vector2 position, Vector2 size, Color color, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            SetTopLeft(rect, position, size);

            Text text = obj.GetComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void AddOutline(GameObject target, Color color, float width)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
            outline.useGraphicAlpha = false;
        }

        private Font FindFont()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Text source = canvas.GetComponentInChildren<Text>(true);
                if (source != null && source.font != null)
                    return source.font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private Transform Find(string name)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                    return children[i];
            }
            return transform;
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
    

        private static Sprite GetBadgeSprite()
        {
            if (generatedBadgeSprite != null)
                return generatedBadgeSprite;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "VC5 Tutorial Badge Circle";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radiusSquared = center * center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    byte alpha = dx * dx + dy * dy <= radiusSquared ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            generatedBadgeSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            generatedBadgeSprite.name = "VC5 Tutorial Badge Circle";
            generatedBadgeSprite.hideFlags = HideFlags.HideAndDontSave;
            return generatedBadgeSprite;
        }
    }
}
