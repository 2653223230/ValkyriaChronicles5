using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// In the game scene, the CardPreviewUI is what shows the card in big with extra info when hovering a card
    /// </summary>

    public class CardPreviewUI : MonoBehaviour
    {
        public UIPanel ui_panel;
        public CardUI card_ui;
        public Text desc;
        public float hover_delay_board = 0.7f;
        public float hover_delay_hand = 0.4f;
        public float hover_delay_mobile = 0.1f;

        public RectTransform[] side_rows;
        public StatusLine[] status_lines;

        private float preview_timer = 0f;
        private Vector2[] start_pos;
        private int description_font_size;
        private Vector2 description_size;
        private Vector2 description_position;
        private static Card end_discard_card;

        private static readonly Vector2 CommanderDescriptionSize = new Vector2(476f, 312f);
        private static readonly Vector2 CommanderDescriptionPosition = new Vector2(66f, -276.5f);

        private void LateUpdate()
        {
            Vc5DemoBattleFeedback.SetCardPreviewVisible(ui_panel != null
                && ui_panel.gameObject.activeInHierarchy && (ui_panel.IsVisible() || ui_panel.GetAlpha() > 0.01f));
        }

        private void OnDisable()
        {
            Vc5DemoBattleFeedback.SetCardPreviewVisible(false);
        }

        public static string BuildAdditionalDescription(CardData card)
        {
            string description = card.GetDesc();
            if (Vc5DemoBootstrap.HasDemoArt(card.id) && description == card.GetDisplayText())
                description = "";
            if (card.id == Vc5R4Rules.Commander)
            {
                string commanderRules = card.GetDisplayText();
                return string.IsNullOrWhiteSpace(description)
                    ? commanderRules
                    : description + "\n\n" + commanderRules;
            }
            string abilities = card.GetAbilitiesDesc();
            if (string.IsNullOrWhiteSpace(description)) return abilities;
            return string.IsNullOrWhiteSpace(abilities) ? description : description + "\n\n" + abilities;
        }

        public static void SelectEndDiscardCard(Card card)
        {
            end_discard_card = card;
        }

        public static Card ResolvePreviewCard(GamePhase phase, Card focusCard)
        {
            if (phase != GamePhase.EndDiscard)
            {
                end_discard_card = null;
                return focusCard;
            }
            return end_discard_card ?? focusCard;
        }

        public static bool ShouldShowPreview(GamePhase phase, Card card, bool uiOpened, bool hoverOnly)
        {
            bool hasDiscardSelection = phase == GamePhase.EndDiscard && end_discard_card != null;
            return hoverOnly && card != null && (hasDiscardSelection || !uiOpened);
        }

        public static void ApplyDescriptionLayout(Text target, CardData card, int defaultFontSize,
            Vector2 defaultSize, Vector2 defaultPosition)
        {
            bool commander = card != null && card.id == Vc5R4Rules.Commander;
            target.resizeTextForBestFit = false;
            target.fontSize = commander ? 24 : defaultFontSize;
            target.lineSpacing = 1f;
            target.rectTransform.sizeDelta = commander ? CommanderDescriptionSize : defaultSize;
            target.rectTransform.anchoredPosition = commander ? CommanderDescriptionPosition : defaultPosition;
        }

        private void Start()
        {
            description_font_size = desc.fontSize;
            description_size = desc.rectTransform.sizeDelta;
            description_position = desc.rectTransform.anchoredPosition;
            start_pos = new Vector2[side_rows.Length];
            for (int i = 0; i < side_rows.Length; i++)
            {
                start_pos[i] = side_rows[i].anchoredPosition;
            }
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            foreach (StatusLine line in status_lines)
                line.Hide();

            PlayerControls controls = PlayerControls.Get();
            HandCard hcard = HandCard.GetFocus();
            BoardCard bcard = BoardCard.GetFocus();
            HeroUI hero_ui = HeroUI.GetFocus();
            Card histcard = TurnHistoryLine.GetHoverCard();
            Card secret_card = SecretIconUI.GetHoverCard();

            float delay = hcard != null ? hover_delay_hand : hover_delay_board;
            if (GameTool.IsMobile())
                delay = hover_delay_mobile;

            Card pcard = hcard != null ? hcard?.GetCard() : bcard?.GetFocusCard();
            if (pcard == null)
                pcard = histcard;
            if (pcard == null)
                pcard = secret_card;
            if (pcard == null)
                pcard = hero_ui?.GetCard();

            Game game = GameClient.Get().GetGameData();
            pcard = ResolvePreviewCard(game.phase, pcard);

            bool hover_only = !Input.GetMouseButton(0) && !HandCardArea.Get().IsDragging();
            bool should_show_preview = ShouldShowPreview(game.phase, pcard, GameUI.IsUIOpened(), hover_only);

            if (should_show_preview)
                preview_timer += Time.deltaTime;
            else
                preview_timer = 0f;

            bool show_preview = should_show_preview && preview_timer >= delay;
            ui_panel.SetVisible(show_preview);

            if (show_preview)
            {
                CardData icard = pcard.CardData;
                card_ui.SetCard(icard, pcard.VariantData);

                ApplyDescriptionLayout(desc, icard, description_font_size,
                    description_size, description_position);

                string additional = BuildAdditionalDescription(icard);
                string runtimeStatus = Vc5StatusDisplay.FormatAll(pcard);
                if (!string.IsNullOrWhiteSpace(runtimeStatus))
                    desc.text = "当前状态：\n" + runtimeStatus
                        + (string.IsNullOrWhiteSpace(additional) ? "" : "\n\n" + additional);
                else
                    desc.text = additional;

                //Abilities
                int index = 0;
                foreach (AbilityData ability in pcard.GetAbilities())
                {
                    if (index < status_lines.Length)
                    {
                        //Dont display default ability (GetAbilitiesDesc does that already)
                        if (!pcard.CardData.HasAbility(ability) && !string.IsNullOrWhiteSpace(ability.desc))
                        {
                            status_lines[index].SetLine(pcard.CardData, ability);
                            index++;
                        }
                    }
                }

            }

        }
    }
}
