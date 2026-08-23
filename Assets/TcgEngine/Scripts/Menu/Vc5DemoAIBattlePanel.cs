using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Runtime-created demo entry for local solo battles against AI.
    /// It avoids scene YAML edits and does not alter LAN/P2P matchmaking flows.
    /// </summary>
    public class Vc5DemoAIBattlePanel : MonoBehaviour
    {
        private MainMenu menu;
        private DeckData[] decks = new DeckData[0];
        private int playerDeckIndex;
        private int aiDeckIndex;

        private Text playerDeckText;
        private Text aiDeckText;
        private Text playerDeckDesc;
        private Text aiDeckDesc;
        private Text errorText;

        public static Vc5DemoAIBattlePanel Ensure(MainMenu menu)
        {
            Vc5DemoAIBattlePanel existing = FindObjectOfType<Vc5DemoAIBattlePanel>(true);
            if (existing != null)
            {
                existing.Initialize(menu);
                return existing;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("VC5 Demo Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject panelObj = new GameObject("VC5 Demo AI Battle Panel");
            panelObj.transform.SetParent(canvas.transform, false);
            Vc5DemoAIBattlePanel panel = panelObj.AddComponent<Vc5DemoAIBattlePanel>();
            panel.BuildUI();
            panel.Initialize(menu);
            return panel;
        }

        public void Initialize(MainMenu mainMenu)
        {
            menu = mainMenu;
            decks = Vc5DemoMatchSetup.GetPlayableDemoDecks();
            playerDeckIndex = FindDeckIndex(Vc5DemoMatchSetup.GetSavedPlayerDeckId(), Vc5DemoMatchSetup.GetDefaultPlayerDeckId());
            aiDeckIndex = FindDeckIndex(Vc5DemoMatchSetup.GetSavedAIDeckId(), Vc5DemoMatchSetup.GetDefaultAIDeckId());
            Refresh();
            gameObject.SetActive(true);
        }

        private void BuildUI()
        {
            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 1f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Image dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.05f, 0.06f, 0.82f);

            GameObject card = CreateChild("Panel", transform);
            RectTransform cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(720f, 520f);
            cardRt.anchoredPosition = Vector2.zero;
            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.12f, 0.14f, 0.16f, 0.96f);

            CreateText("Title", card.transform, "VC5 Demo 对战", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 205f), new Vector2(620f, 50f), Color.white);
            CreateText("Subtitle", card.transform, "选择玩家与 AI 卡组，然后直接开始本地单人对战。", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, 166f), new Vector2(640f, 34f), new Color(0.82f, 0.86f, 0.9f));

            CreateDeckSelector(card.transform, "玩家卡组", 70f, true);
            CreateDeckSelector(card.transform, "AI 卡组", -65f, false);

            Button start = CreateButton("StartButton", card.transform, "开始对战", new Vector2(0f, -203f), new Vector2(260f, 58f), new Color(0.16f, 0.42f, 0.72f));
            start.onClick.AddListener(OnClickStart);

            errorText = CreateText("ErrorText", card.transform, "", 16, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -250f), new Vector2(620f, 26f), new Color(1f, 0.55f, 0.48f));
        }

        private void CreateDeckSelector(Transform parent, string label, float y, bool player)
        {
            CreateText(label + "Label", parent, label, 19, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(-250f, y + 45f), new Vector2(200f, 28f), Color.white);

            Button prev = CreateButton(label + "Prev", parent, "<", new Vector2(-275f, y), new Vector2(46f, 46f), new Color(0.21f, 0.24f, 0.27f));
            Button next = CreateButton(label + "Next", parent, ">", new Vector2(275f, y), new Vector2(46f, 46f), new Color(0.21f, 0.24f, 0.27f));
            if (player)
            {
                prev.onClick.AddListener(() => ChangePlayerDeck(-1));
                next.onClick.AddListener(() => ChangePlayerDeck(1));
                playerDeckText = CreateText(label + "Deck", parent, "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, y + 10f), new Vector2(470f, 34f), Color.white);
                playerDeckDesc = CreateText(label + "Desc", parent, "", 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, y - 25f), new Vector2(470f, 42f), new Color(0.76f, 0.8f, 0.84f));
            }
            else
            {
                prev.onClick.AddListener(() => ChangeAIDeck(-1));
                next.onClick.AddListener(() => ChangeAIDeck(1));
                aiDeckText = CreateText(label + "Deck", parent, "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, y + 10f), new Vector2(470f, 34f), Color.white);
                aiDeckDesc = CreateText(label + "Desc", parent, "", 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, y - 25f), new Vector2(470f, 42f), new Color(0.76f, 0.8f, 0.84f));
            }
        }

        private void OnClickStart()
        {
            if (errorText != null)
                errorText.text = "";

            string error;
            string playerDeck = GetDeckId(playerDeckIndex);
            string aiDeck = GetDeckId(aiDeckIndex);
            if (!Vc5DemoMatchSetup.TryStartSoloAIMatch(menu, playerDeck, aiDeck, out error))
            {
                if (errorText != null)
                    errorText.text = error;
                else
                    Debug.LogWarning(error);
            }
        }

        private void ChangePlayerDeck(int dir)
        {
            playerDeckIndex = WrapIndex(playerDeckIndex + dir);
            Refresh();
        }

        private void ChangeAIDeck(int dir)
        {
            aiDeckIndex = WrapIndex(aiDeckIndex + dir);
            Refresh();
        }

        private void Refresh()
        {
            if (decks == null || decks.Length == 0)
            {
                SetText(playerDeckText, "未找到试玩卡组");
                SetText(aiDeckText, "未找到试玩卡组");
                SetText(playerDeckDesc, "");
                SetText(aiDeckDesc, "");
                return;
            }

            playerDeckIndex = WrapIndex(playerDeckIndex);
            aiDeckIndex = WrapIndex(aiDeckIndex);
            SetDeckText(playerDeckText, playerDeckDesc, decks[playerDeckIndex]);
            SetDeckText(aiDeckText, aiDeckDesc, decks[aiDeckIndex]);
        }

        private int FindDeckIndex(string savedDeckId, string fallbackDeckId)
        {
            int saved = IndexOf(savedDeckId);
            if (saved >= 0)
                return saved;
            int fallback = IndexOf(fallbackDeckId);
            return fallback >= 0 ? fallback : 0;
        }

        private int IndexOf(string deckId)
        {
            if (decks == null)
                return -1;
            for (int i = 0; i < decks.Length; i++)
            {
                if (decks[i] != null && decks[i].id == deckId)
                    return i;
            }
            return -1;
        }

        private int WrapIndex(int index)
        {
            if (decks == null || decks.Length == 0)
                return 0;
            while (index < 0)
                index += decks.Length;
            return index % decks.Length;
        }

        private string GetDeckId(int index)
        {
            if (decks == null || decks.Length == 0)
                return "";
            return decks[WrapIndex(index)].id;
        }

        private static void SetDeckText(Text title, Text desc, DeckData deck)
        {
            if (deck == null)
            {
                SetText(title, "未找到卡组");
                SetText(desc, "");
                return;
            }

            SetText(title, deck.title);
            SetText(desc, BuildDeckDescription(deck));
        }

        private static string BuildDeckDescription(DeckData deck)
        {
            string heroes = "";
            if (deck.heroes != null)
            {
                for (int i = 0; i < deck.heroes.Length; i++)
                {
                    if (deck.heroes[i] == null)
                        continue;
                    if (heroes.Length > 0)
                        heroes += " / ";
                    heroes += deck.heroes[i].title;
                }
            }

            return heroes + "    卡牌 " + deck.GetQuantity() + "/" + GameplayData.Get().deck_size;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, TextAnchor anchor, Vector2 pos, Vector2 sizeDelta, Color color)
        {
            GameObject obj = CreateChild(name, parent);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = pos;

            Text uiText = obj.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            uiText.text = text;
            uiText.fontSize = size;
            uiText.fontStyle = style;
            uiText.alignment = anchor;
            uiText.color = color;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Truncate;
            return uiText;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 pos, Vector2 sizeDelta, Color bgColor)
        {
            GameObject obj = CreateChild(name, parent);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = pos;

            Image bg = obj.AddComponent<Image>();
            bg.color = bgColor;
            Button button = obj.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.highlightedColor = bgColor * 1.18f;
            colors.pressedColor = bgColor * 0.82f;
            colors.selectedColor = bgColor;
            button.colors = colors;

            CreateText("Text", obj.transform, text, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta, Color.white);
            return button;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetText(Text target, string text)
        {
            if (target != null)
                target.text = text;
        }
    }
}
