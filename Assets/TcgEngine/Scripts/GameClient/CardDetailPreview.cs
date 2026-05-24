using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 对局内右侧大卡预览：悬停或点击手牌/棋盘卡时显示卡图与可读效果（CardData.text / desc）。
    /// 若场景无挂载实例，会在首次显示时挂到 HandCardArea 所在 Canvas 下并自动创建 UI。
    /// </summary>
    public class CardDetailPreview : MonoBehaviour
    {
        private static CardDetailPreview instance;

        [Tooltip("可选：手动指定用于展示的大号 CardUI（与手牌同 Prefab 结构即可）。留空则使用下方简易 UI。")]
        public CardUI cardUiPrefabInstance;

        private CanvasGroup rootGroup;
        private RectTransform rootRt;
        private CardUI cardUi;
        private Image artImage;
        private Text titleText;
        private Text bodyText;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void ShowCard(Card card)
        {
            if (card == null || card.CardData == null)
                return;
            EnsureInstance();
            if (instance == null)
                return;
            instance.ShowInternal(card.CardData, card.VariantData, card);
        }

        public static void Hide()
        {
            if (instance == null || instance.rootGroup == null)
                return;
            instance.rootGroup.alpha = 0f;
            instance.rootGroup.blocksRaycasts = false;
            instance.rootGroup.interactable = false;
        }

        private static void EnsureInstance()
        {
            if (instance != null)
                return;

            CardDetailPreview existing = FindObjectOfType<CardDetailPreview>();
            if (existing != null)
            {
                instance = existing;
                existing.BuildIfNeeded();
                return;
            }

            HandCardArea hca = FindObjectOfType<HandCardArea>();
            Canvas canvas = hca != null ? hca.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            GameObject go = new GameObject("CardDetailPreview");
            go.transform.SetParent(canvas.transform, false);
            instance = go.AddComponent<CardDetailPreview>();
            instance.BuildIfNeeded();
        }

        private void BuildIfNeeded()
        {
            if (rootGroup != null)
                return;

            rootRt = gameObject.GetComponent<RectTransform>();
            if (rootRt == null)
                rootRt = gameObject.AddComponent<RectTransform>();

            rootRt.anchorMin = new Vector2(1f, 0.5f);
            rootRt.anchorMax = new Vector2(1f, 0.5f);
            rootRt.pivot = new Vector2(1f, 0.5f);
            rootRt.anchoredPosition = new Vector2(-24f, 0f);
            rootRt.sizeDelta = new Vector2(320f, 460f);

            rootGroup = gameObject.GetComponent<CanvasGroup>();
            if (rootGroup == null)
                rootGroup = gameObject.AddComponent<CanvasGroup>();

            if (cardUiPrefabInstance != null)
            {
                cardUi = cardUiPrefabInstance;
                cardUi.gameObject.transform.SetParent(transform, false);
                RectTransform crt = cardUi.GetComponent<RectTransform>();
                if (crt != null)
                {
                    crt.anchorMin = Vector2.zero;
                    crt.anchorMax = Vector2.one;
                    crt.offsetMin = Vector2.zero;
                    crt.offsetMax = Vector2.zero;
                }
            }
            else
            {
                Font font = ResolveUIFontForPreview();

                GameObject artGo = new GameObject("Art");
                artGo.transform.SetParent(transform, false);
                RectTransform artRt = artGo.AddComponent<RectTransform>();
                artRt.anchorMin = new Vector2(0f, 0.38f);
                artRt.anchorMax = new Vector2(1f, 1f);
                artRt.offsetMin = new Vector2(8f, 0f);
                artRt.offsetMax = new Vector2(-8f, -8f);
                artImage = artGo.AddComponent<Image>();
                artImage.preserveAspect = true;
                artImage.color = Color.white;

                GameObject titleGo = new GameObject("Title");
                titleGo.transform.SetParent(transform, false);
                RectTransform titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 0.32f);
                titleRt.anchorMax = new Vector2(1f, 0.38f);
                titleRt.offsetMin = new Vector2(10f, 0f);
                titleRt.offsetMax = new Vector2(-10f, 0f);
                titleText = titleGo.AddComponent<Text>();
                if (font != null)
                    titleText.font = font;
                titleText.fontSize = 18;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
                titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                titleText.verticalOverflow = VerticalWrapMode.Truncate;

                GameObject bodyGo = new GameObject("Body");
                bodyGo.transform.SetParent(transform, false);
                RectTransform bodyRt = bodyGo.AddComponent<RectTransform>();
                bodyRt.anchorMin = new Vector2(0f, 0f);
                bodyRt.anchorMax = new Vector2(1f, 0.32f);
                bodyRt.offsetMin = new Vector2(10f, 8f);
                bodyRt.offsetMax = new Vector2(-10f, 4f);
                bodyText = bodyGo.AddComponent<Text>();
                if (font != null)
                    bodyText.font = font;
                bodyText.fontSize = 14;
                bodyText.alignment = TextAnchor.UpperLeft;
                bodyText.color = new Color(0.95f, 0.95f, 0.95f);
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyText.verticalOverflow = VerticalWrapMode.Overflow;
                bodyText.supportRichText = false;

                Image bg = gameObject.GetComponent<Image>();
                if (bg == null)
                    bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.75f);
                bg.raycastTarget = false;
            }

            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }

        /// <summary>
        /// 避免调用已移除的 LegacyRuntime.ttf（会刷错误日志）。优先系统字体，再内置 Arial，最后复用场景里已有 UI.Text 的字体。
        /// </summary>
        private static Font ResolveUIFontForPreview()
        {
            try
            {
                Font os = Font.CreateDynamicFontFromOSFont(
                    new[]
                    {
                        "Microsoft YaHei UI",
                        "Microsoft YaHei",
                        "SimHei",
                        "PingFang SC",
                        "Arial",
                        "Segoe UI",
                        "Helvetica",
                    },
                    16);
                if (os != null)
                    return os;
            }
            catch
            {
                // 部分平台 CreateDynamicFontFromOSFont 不可用
            }

            Font built = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (built != null)
                return built;

            Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                Text t = texts[i];
                if (t == null || t.font == null)
                    continue;
                if (!t.gameObject.scene.IsValid())
                    continue;
                return t.font;
            }

            return null;
        }

        private void ShowInternal(CardData data, VariantData variant, Card runtimeCard)
        {
            BuildIfNeeded();

            if (cardUi != null)
            {
                if (runtimeCard != null)
                    cardUi.SetCard(runtimeCard);
                else
                    cardUi.SetCard(data, variant ?? VariantData.GetDefault());
            }
            else
            {
                if (artImage != null)
                    artImage.sprite = data.GetFullArt(variant ?? VariantData.GetDefault());
                if (titleText != null)
                    titleText.text = data.GetTitle();
                if (bodyText != null)
                    bodyText.text = BuildRuntimeDisplayText(data, runtimeCard);
            }

            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }

        private string BuildRuntimeDisplayText(CardData data, Card runtimeCard)
        {
            string text = data != null ? data.GetDisplayText() : "";
            string status = GetRuntimeStatusText(runtimeCard);
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!string.IsNullOrWhiteSpace(text))
                    text += "\n\n";
                text += "当前状态：" + status;
            }
            return text;
        }

        private string GetRuntimeStatusText(Card runtimeCard)
        {
            if (runtimeCard == null)
                return "";

            string txt = "";
            foreach (CardStatus status in runtimeCard.GetAllStatus())
            {
                string seg = Vc5StatusDisplay.FormatSingle(status);
                if (!string.IsNullOrEmpty(seg))
                    txt += seg + ", ";
            }

            if (txt.Length > 2)
                txt = txt.Substring(0, txt.Length - 2);
            return txt;
        }
    }
}
