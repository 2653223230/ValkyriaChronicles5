using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>Selected unit skills, sized to their content.</summary>
    public class AbilityPanel : UIPanel
    {
        [Header("Ability Buttons")]
        public RectTransform buttons_container;
        public GameObject ability_button_prefab;

        private Card selected_card;
        private readonly List<AbilityButton> ability_buttons = new List<AbilityButton>();
        private Text heading;
        private static AbilityPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            Hide(true);
        }

        public void ShowAbilities(Card card)
        {
            ClearButtons();
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            Player player = client != null ? client.GetPlayer() : null;
            if (card == null || data == null || player == null || card.player_id != player.player_id
                || ability_button_prefab == null || buttons_container == null)
            {
                Hide(true);
                return;
            }

            selected_card = card;
            AddAbilities(card);
            Card equip = data.GetEquipCard(card.equipped_uid);
            if (equip != null)
                AddAbilities(equip);
            if (ability_buttons.Count == 0)
            {
                Hide(true);
                return;
            }

            ConfigureLayout();
            Show(true);
        }

        private void AddAbilities(Card card)
        {
            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability == null || ability.trigger != AbilityTrigger.Activate)
                    continue;
                GameObject obj = Instantiate(ability_button_prefab, buttons_container);
                obj.SetActive(true);
                AbilityButton button = obj.GetComponent<AbilityButton>();
                if (button == null)
                {
                    Destroy(obj);
                    continue;
                }
                obj.transform.localScale = Vector3.one;
                LayoutElement element = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = 108f;
                element.flexibleHeight = 0f;
                button.SetAbility(card, ability);
                button.ConfigurePanelPresentation();
                Button click = obj.GetComponent<Button>();
                if (click != null)
                {
                    click.onClick.RemoveAllListeners();
                    click.onClick.AddListener(button.OnClick);
                }
                ability_buttons.Add(button);
            }
        }

        private void ConfigureLayout()
        {
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-260f, 0f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 360f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 52f + ability_buttons.Count * 116f);
            Image background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            background.sprite = null;
            background.color = new Color(0.055f, 0.10f, 0.13f, 0.97f);
            background.raycastTarget = true;

            if (heading == null)
            {
                GameObject title = new GameObject("SkillHeading", typeof(RectTransform), typeof(Text));
                title.transform.SetParent(transform, false);
                heading = title.GetComponent<Text>();
                heading.font = ability_buttons[0].text.font;
                heading.fontSize = 18;
                heading.color = new Color(0.72f, 0.87f, 0.88f);
                heading.raycastTarget = false;
                heading.alignment = TextAnchor.MiddleLeft;
                RectTransform titleRect = heading.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = Vector2.one;
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -8f);
                titleRect.sizeDelta = new Vector2(-28f, 30f);
            }
            heading.text = (selected_card.CardData != null ? selected_card.CardData.title : "单位") + " · 技能";
            buttons_container.anchorMin = Vector2.zero;
            buttons_container.anchorMax = Vector2.one;
            buttons_container.offsetMin = new Vector2(12f, 12f);
            buttons_container.offsetMax = new Vector2(-12f, -44f);
            Image containerImage = buttons_container.GetComponent<Image>();
            if (containerImage != null)
                containerImage.enabled = false;
            VerticalLayoutGroup layout = buttons_container.GetComponent<VerticalLayoutGroup>()
                ?? buttons_container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttons_container);
        }

        private void ClearButtons()
        {
            foreach (AbilityButton button in ability_buttons)
            {
                if (button == null)
                    continue;
                button.Hide();
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
            ability_buttons.Clear();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            canvas_group.interactable = canvas_group.blocksRaycasts = true;
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            canvas_group.interactable = canvas_group.blocksRaycasts = false;
            selected_card = null;
            ClearButtons();
        }

        public Card GetSelectedCard() { return selected_card; }
        public static AbilityPanel Get() { return instance; }
    }
}
