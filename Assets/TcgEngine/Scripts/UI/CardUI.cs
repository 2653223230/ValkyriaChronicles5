using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Scripts to display all stats of a card, 
    /// is used by other script that display cards like BoardCard, and HandCard, CollectionCard..
    /// 显示卡片所有统计数据的脚本，
    /// 用于显示BoardCard、HandCard、CollectionCard等卡片的其他脚本。。
    /// </summary>

    public class CardUI : MonoBehaviour, IPointerClickHandler
    {
        public Image card_image;
        public Image frame_image;
        public Image team_icon;
        public Image rarity_icon;
        public Image attack_icon;
        public Image hp_icon;
        public Image cost_icon;
        public Text attack;
        public Text hp;
        public Text cost;

        public Text card_title;
        public Text card_text;

        public TraitUI[] stats;

        public UnityAction<CardUI> onClick;
        public UnityAction<CardUI> onClickRight;

        private CardData card;
        private VariantData variant;
        private bool artLayoutCached;
        private Vector2 originalArtPosition, originalArtSize, originalArtRect;
        private Vector2 originalTextPosition, originalTextSize;
        private Vector2 originalTitlePosition, originalTitleSize;
        private bool originalArtAspect;
        private bool originalTextBestFit;
        private int originalTextMinSize, originalTextMaxSize;
        private Image demoBackground;
        private Image[] templateGradients;
        private bool[] originalGradientEnabled;

        void Awake()
        {

        }

        public void SetCard(Card card)
        {
            if (card == null)
                return;

            SetCard(card.CardData, card.VariantData);

            if (cost != null)
                cost.text = card.GetMana().ToString();
            if (cost != null && card.CardData.IsDynamicManaCost())
                cost.text = "X";
            if (attack != null)
                attack.text = card.GetAttack().ToString();
            if (hp != null)
                hp.text = card.GetHP().ToString();

            foreach (TraitUI stat in stats)
                stat.SetCard(card);
        }

        public void SetCard(CardData card, VariantData variant)
        {
            if (card == null)
                return;

            this.card = card;
            this.variant = variant;

            if(card_image != null)
                card_image.sprite = card.GetFullArt(variant);
            UpdateDemoArtLayout(card);
            if (frame_image != null)
                frame_image.sprite = variant.frame;
            if (card_title != null)
                card_title.text = card.GetTitle().ToUpper();
            if (card_text != null)
                card_text.text = card.GetDisplayText();

            if (attack_icon != null)
                attack_icon.enabled = card.IsCharacter();
            if (attack != null)
                attack.enabled = card.IsCharacter();
            if (hp_icon != null)
                hp_icon.enabled = card.IsBoardCard() || card.IsEquipment();
            if (hp != null)
                hp.enabled = card.IsBoardCard() || card.IsEquipment();
            if (cost_icon != null)
                cost_icon.enabled = card.type != CardType.Hero;
            if (cost != null)
                cost.enabled = card.type != CardType.Hero;

            if (cost != null)
                cost.text = card.mana.ToString();
            if (cost != null && card.IsDynamicManaCost())
                cost.text = "X";
            if (attack != null)
                attack.text = card.attack.ToString();
            if (hp != null)
                hp.text = card.hp.ToString();

            if (team_icon != null)
            {
                team_icon.sprite = card.team.icon;
                team_icon.enabled = team_icon.sprite != null && !HasIllustration(card);
            }

            if (rarity_icon != null)
            {
                rarity_icon.sprite = card.rarity.icon;
                rarity_icon.enabled = rarity_icon.sprite != null && card.type != CardType.Hero;
            }

            foreach (TraitUI stat in stats)
                stat.SetCard(card);

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        private void UpdateDemoArtLayout(CardData data)
        {
            if (card_image == null || card_text == null)
                return;

            RectTransform art = card_image.rectTransform;
            RectTransform description = card_text.rectTransform;
            if (!artLayoutCached)
            {
                originalArtPosition = art.anchoredPosition;
                originalArtSize = art.sizeDelta;
                originalArtRect = art.rect.size;
                originalTextPosition = description.anchoredPosition;
                originalTextSize = description.sizeDelta;
                originalArtAspect = card_image.preserveAspect;
                originalTextBestFit = card_text.resizeTextForBestFit;
                originalTextMinSize = card_text.resizeTextMinSize;
                originalTextMaxSize = card_text.resizeTextMaxSize;
                if (card_title != null)
                {
                    originalTitlePosition = card_title.rectTransform.anchoredPosition;
                    originalTitleSize = card_title.rectTransform.sizeDelta;
                }
                var gradients = new List<Image>();
                foreach (Image child in GetComponentsInChildren<Image>(true))
                    if (child.name == "Gradient" || child.name == "Gradient 2") gradients.Add(child);
                templateGradients = gradients.ToArray();
                originalGradientEnabled = new bool[templateGradients.Length];
                for (int i = 0; i < templateGradients.Length; i++) originalGradientEnabled[i] = templateGradients[i].enabled;
                artLayoutCached = true;
            }

            bool illustrated = HasIllustration(data);
            float height = originalArtRect.y;
            float side = originalArtRect.x * (data.IsCharacter() ? 0.82f : 0.90f);
            float artTop = originalArtPosition.y + height * 0.36f;
            float textTop = artTop - side - height * 0.015f;
            float textBottom = originalArtPosition.y - height * (data.IsCharacter() ? 0.35f : 0.44f);
            art.anchoredPosition = illustrated
                ? new Vector2(originalArtPosition.x, artTop - side * 0.5f) : originalArtPosition;
            art.sizeDelta = illustrated ? new Vector2(side, side) : originalArtSize;
            description.anchoredPosition = illustrated
                ? new Vector2(originalTextPosition.x, (textTop + textBottom) * 0.5f) : originalTextPosition;
            description.sizeDelta = illustrated
                ? new Vector2(originalTextSize.x, textTop - textBottom) : originalTextSize;
            card_image.preserveAspect = illustrated || originalArtAspect;
            card_text.resizeTextForBestFit = illustrated || originalTextBestFit;
            card_text.resizeTextMinSize = illustrated ? Mathf.RoundToInt(card_text.fontSize * 0.75f) : originalTextMinSize;
            card_text.resizeTextMaxSize = illustrated ? card_text.fontSize : originalTextMaxSize;
            if (card_title != null)
            {
                card_title.rectTransform.anchoredPosition = illustrated
                    ? new Vector2(originalTitlePosition.x, originalArtPosition.y + height * 0.42f) : originalTitlePosition;
                card_title.rectTransform.sizeDelta = illustrated
                    ? new Vector2(originalTitleSize.x, height * 0.09f) : originalTitleSize;
            }
            for (int i = 0; i < templateGradients.Length; i++)
                templateGradients[i].enabled = !illustrated && originalGradientEnabled[i];

            if (illustrated && demoBackground == null)
            {
                GameObject background = new GameObject("Demo Card Background", typeof(RectTransform), typeof(Image));
                background.layer = gameObject.layer;
                background.transform.SetParent(art.parent, false);
                background.transform.SetAsFirstSibling();
                demoBackground = background.GetComponent<Image>();
                demoBackground.raycastTarget = false;
                demoBackground.color = new Color(0.045f, 0.07f, 0.075f, card_image.color.a);
                demoBackground.rectTransform.anchoredPosition = originalArtPosition;
                demoBackground.rectTransform.sizeDelta = new Vector2(originalArtRect.x * 0.94f, height * 0.96f);
            }
            if (demoBackground != null) demoBackground.enabled = illustrated;
        }

        private static bool HasIllustration(CardData data)
        {
            return Vc5DemoBootstrap.HasDemoArt(data.id) && data.art_full != null;
        }

        public void SetHP(int hp_value)
        {
            if (hp != null)
                hp.text = hp_value.ToString();
        }

        public void SetMaterial(Material mat)
        {
            if (card_image != null)
                card_image.material = mat;
            if (frame_image != null)
                frame_image.material = mat;
            if (team_icon != null)
                team_icon.material = mat;
            if (rarity_icon != null)
                rarity_icon.material = mat;
            if (attack_icon != null)
                attack_icon.material = mat;
            if (hp_icon != null)
                hp_icon.material = mat;
            if (cost_icon != null)
                cost_icon.material = mat;
        }

        public void SetOpacity(float opacity)
        {
            if (demoBackground != null)
                demoBackground.color = new Color(demoBackground.color.r, demoBackground.color.g, demoBackground.color.b, opacity);
            if (card_image != null)
                card_image.color = new Color(card_image.color.r, card_image.color.g, card_image.color.b, opacity);
            if (frame_image != null)
                frame_image.color = new Color(frame_image.color.r, frame_image.color.g, frame_image.color.b, opacity);
            if (team_icon != null)
                team_icon.color = new Color(team_icon.color.r, team_icon.color.g, team_icon.color.b, opacity);
            if (rarity_icon != null)
                rarity_icon.color = new Color(rarity_icon.color.r, rarity_icon.color.g, rarity_icon.color.b, opacity);
            if (attack_icon != null)
                attack_icon.color = new Color(attack_icon.color.r, attack_icon.color.g, attack_icon.color.b, opacity);
            if (hp_icon != null)
                hp_icon.color = new Color(hp_icon.color.r, hp_icon.color.g, hp_icon.color.b, opacity);
            if (cost_icon != null)
                cost_icon.color = new Color(cost_icon.color.r, cost_icon.color.g, cost_icon.color.b, opacity);
            if (attack != null)
                attack.color = new Color(attack.color.r, attack.color.g, attack.color.b, opacity);
            if (hp != null)
                hp.color = new Color(hp.color.r, hp.color.g, hp.color.b, opacity);
            if (cost != null)
                cost.color = new Color(cost.color.r, cost.color.g, cost.color.b, opacity);
            if (card_title != null)
                card_title.color = new Color(card_title.color.r, card_title.color.g, card_title.color.b, opacity);
            if (card_text != null)
                card_text.color = new Color(card_text.color.r, card_text.color.g, card_text.color.b, opacity);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (onClick != null)
                    onClick.Invoke(this);
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (onClickRight != null)
                    onClickRight.Invoke(this);
            }
        }

        public CardData GetCard()
        {
            return card;
        }

        public VariantData GetVariant()
        {
            return variant;
        }
    }
}
