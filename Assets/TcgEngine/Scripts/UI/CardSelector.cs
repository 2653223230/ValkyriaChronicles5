using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// The UI for card selector, appears when an ability with CardSelector target is triggered
    /// </summary>

    public class CardSelector : SelectorPanel
    {
        public GameObject card_prefab;

        public RectTransform content;
        public Text title;
        public Text subtitle;

        public Button select_button;
        public Text select_button_text;
        public float card_spacing = 100f;

        private AbilityData iability;

        private List<Card> card_list = new List<Card>();
        private List<CardSelectorCard> selector_list = new List<CardSelectorCard>();

        private Vector2 mouse_start;
        private int mouse_start_index;
        private int selection_index = -1;
        public int SelectionIndex { get { return selection_index; } }
        public bool IsCommanderDiscard { get { return iability != null && iability.id == Vc5R4Rules.PrepareSkill; } }
        private bool drag = false;
        private bool force_show = false;
        private float mouse_scroll = 0f;
        private float timer = 0f;

        private static CardSelector instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            Hide();
        }

        protected override void Update()
        {
            base.Update();

            timer += Time.deltaTime;

            //Drag cards
            Vector2 mouse_pos = GetMouseRectPosition();
            Vector2 move = mouse_pos - mouse_start;
            if (!IsCommanderDiscard && drag && move.magnitude > 0.1f)
            {
                selection_index = mouse_start_index - Mathf.RoundToInt(move.x / card_spacing);
                selection_index = Mathf.Clamp(selection_index, 0, selector_list.Count - 1);
            }

            //Mouse scroll
            if (!IsCommanderDiscard) mouse_scroll += -Input.mouseScrollDelta.y;
            if (mouse_scroll > 0.5f)
            {
                OnClickNext();
                mouse_scroll -= 1f;
            }
            else if (mouse_scroll < -0.5f)
            {
                OnClickPrev();
                mouse_scroll += 1f;
            }

            //Refresh cards
            foreach (CardSelectorCard card in selector_list)
            {
                bool is_selected = card.GetIndex() == selection_index;
                Vector3 pos = GetCardPosition(card);
                Vector3 scale = IsCommanderDiscard ? Vector3.one * 0.65f
                    : (is_selected ? Vector3.one : Vector3.one / 2f);
                card.SetTargetPos(pos);
                card.SetTargetScale(scale);
            }

            //Close on right click if not a selection
            if (iability == null && Input.GetMouseButtonDown(1) && timer > 1f)
                Hide();

            Game game = GameClient.Get().GetGameData();
            select_button.interactable = !IsCommanderDiscard || selection_index >= 0;
            if (game != null && iability != null && game.selector == SelectorType.None)
                Hide(); //Ability was selected already, close panel
        }

        public void RefreshPanel()
        {
            foreach (CardSelectorCard card in selector_list)
                Destroy(card.gameObject);
            selector_list.Clear();
            drag = false;
            mouse_scroll = 0f;

            select_button_text.text = IsCommanderDiscard ? "确定弃牌" : (iability != null ? "Select" : "OK");
            select_button.gameObject.SetActive(iability != null);
            select_button.interactable = !IsCommanderDiscard || selection_index >= 0;

            int index = 0;
            foreach (Button button in GetComponentsInChildren<Button>(true))
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    string method = button.onClick.GetPersistentMethodName(i);
                    if (method == nameof(OnClickNext) || method == nameof(OnClickPrev))
                        button.gameObject.SetActive(!IsCommanderDiscard);
                }
            foreach (Card card in card_list)
            {
                CardData icard = CardData.Get(card.card_id);
                if (icard != null)
                {
                    GameObject obj = Instantiate(card_prefab, content.transform);

                    RectTransform rect = obj.GetComponent<RectTransform>();
                    CardSelectorCard selector_card = obj.GetComponent<CardSelectorCard>();
                    selector_card.SetCard(card);
                    selector_card.SetIndex(index);

                    Vector3 pos = GetCardPosition(selector_card);
                    Vector3 scale = (IsCommanderDiscard ? 0.65f : (index == selection_index ? 1 : 0.5f)) * Vector3.one;
                    selector_card.SetTargetPos(pos);
                    selector_card.SetTargetScale(scale);
                    rect.anchoredPosition = pos;
                    selector_list.Add(selector_card);

                    index++;
                }
            }
        }

        //Show ability
        public override void Show(AbilityData iability, Card caster)
        {
            Game data = GameClient.Get().GetGameData();
            this.card_list = iability.GetCardTargets(data, caster);
            this.iability = iability;
            force_show = false;
            title.text = IsCommanderDiscard ? "战术筹划 · 选择弃牌" : iability.title;
            subtitle.text = IsCommanderDiscard ? "点击选牌，再点取消；选好指令后才弃牌。" : iability.desc;
            selection_index = IsCommanderDiscard ? -1 : 0;
            timer = 0f;
            Show();
        }

        //Show deck/discard
        public void Show(List<Card> card_list, string title)
        {
            this.card_list.Clear();
            this.card_list.AddRange(card_list);
            this.card_list.Sort((Card a, Card b) => { return a.CardData.title.CompareTo(b.CardData.title); }); //Reorder to not show the deck order
            this.iability = null;
            force_show = true;
            this.title.text = title;
            subtitle.text = "";
            selection_index = 0;
            timer = 0f;
            Show();
        }

        public void OnClickOK()
        {
            Game data = GameClient.Get().GetGameData();
            if (iability != null && data.selector == SelectorType.SelectorCard)
            {
                CardSelectorCard selector_card = null;
                if (selection_index >= 0 && selection_index < selector_list.Count)
                    selector_card = selector_list[selection_index];

                if (selector_card != null)
                {
                    Card selected_card = selector_card.GetCard();
                    Card caster = data.GetCard(data.selector_caster_uid);
                    if (selected_card != null && iability.AreTargetConditionsMet(data, caster, selected_card))
                    {
                        GameClient.Get().SelectCard(selected_card);
                        Hide();
                    }
                }
            }
            else
            {
                Hide();
            }
        }

        public void OnClickMouseDown()
        {
            if (IsCommanderDiscard) return;
            mouse_start = GetMouseRectPosition();
            mouse_start_index = selection_index;
            drag = true;
        }

        public void OnClickMouseUp()
        {
            drag = false;
        }

        public void OnClickCancel()
        {
            GameClient.Get().CancelSelection();
            Hide();
        }

        public void OnClickNext()
        {
            if (IsCommanderDiscard) return;
            selection_index += 1;
            selection_index = Mathf.Clamp(selection_index, 0, selector_list.Count - 1);
        }

        public void OnClickPrev()
        {
            if (IsCommanderDiscard) return;
            selection_index -= 1;
            selection_index = Mathf.Clamp(selection_index, 0, selector_list.Count - 1);
        }

        private Vector2 GetCardPosition(CardSelectorCard card)
        {
            if (IsCommanderDiscard)
            {
                float spacing = Mathf.Min(175f, 850f / Mathf.Max(1, card_list.Count - 1));
                return new Vector2((card.GetIndex() - (card_list.Count - 1) * 0.5f) * spacing,
                    card.GetIndex() == selection_index ? 60f : 0f);
            }
            int index_offset = card.GetIndex() - selection_index;
            Vector2 pos = new Vector2(index_offset * card_spacing, (index_offset != 0) ? 50f : 0f);
            float center_offset = (index_offset != 0) ? (Mathf.Sign(index_offset) * 140f) : 0;
            pos += Vector2.right * center_offset;
            return pos;
        }

        private Vector2 GetMouseRectPosition()
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, Input.mousePosition, GetComponentInParent<Canvas>().worldCamera, out localpoint);
            return localpoint;
        }

        public void OnClickCard(int index)
        {
            if (!IsCommanderDiscard) return;
            selection_index = selection_index == index ? -1 : index;
            if (select_button != null) select_button.interactable = selection_index >= 0;
        }

        public bool IsAbility()
        {
            return IsVisible() && iability != null;
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            force_show = false;
            selection_index = -1;
        }
        
        public override bool ShouldShow()
        {
            Game data = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            return force_show || (data.selector == SelectorType.SelectorCard && data.selector_player_id == player_id);
        }

        public static CardSelector Get()
        {
            return instance;
        }
    }
}
