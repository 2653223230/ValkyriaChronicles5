using TcgEngine.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Represents the visual aspect of a card in hand.
    /// Will take the data from Card.cs and display it
    /// 表示手中卡片的视觉方面。
    /// 将从Card.cs获取数据并显示
    /// </summary>

    public class HandCard : MonoBehaviour
    {
        public Image card_glow;
        public float move_speed = 10f;
        public float move_rotate_speed = 4f;
        public float move_max_rotate = 10f;

        [HideInInspector]
        public Vector2 deck_position;
        [HideInInspector]
        public float deck_angle;

        private string card_uid = "";

        private CardUI card_ui;
        private RectTransform hand_transform;
        private RectTransform card_transform;
        private Vector3 start_scale;
        private Vector3 current_rotate;
        private Vector3 target_rotate;
        private Vector3 prev_pos;
        private Vector2 drag_start_screen_pos;

        private bool destroyed = false;
        private float focus_timer = 0f;

        private bool focus = false;
        private bool drag = false;
        private bool selected = false;
        private bool discard_press = false;

        private static List<HandCard> card_list = new List<HandCard>();

        public const float Vc5DiscardDragThreshold = 20f;

        void Awake()
        {
            card_list.Add(this);
            card_ui = GetComponent<CardUI>();
            card_transform = transform.GetComponent<RectTransform>();
            hand_transform = transform.parent.GetComponent<RectTransform>();
            start_scale = transform.localScale;
        }

        private void Start()
        {

        }

        private void OnDestroy()
        {
            card_list.Remove(this);
        }

        void Update()
        {
            if (destroyed) return;
            if (!GameClient.Get().IsReady())
                return;

            Card card = GetCard();
            Vector2 target_position = deck_position;
            Vector3 target_size = start_scale;

            focus_timer += Time.deltaTime;

            if (IsFocus())
            {
                target_position = deck_position + Vector2.up * 40f;
            }

            bool discardSelected = HandCardArea.Get().IsDiscardSelected(card_uid);
            if (discardSelected) target_position = deck_position + Vector2.up * 75f;

            if (IsDrag())
            {
                CardDetailPreview.Hide();
                target_position = GetTargetPosition();
                target_size = start_scale * 0.75f;
                Vector3 dir = card_transform.position - prev_pos;
                Vector3 addrot = new Vector3(dir.y * 90f, -dir.x * 90f, 0f);
                target_rotate += addrot * move_rotate_speed * Time.deltaTime;
                target_rotate = new Vector3(Mathf.Clamp(target_rotate.x, -move_max_rotate, move_max_rotate), Mathf.Clamp(target_rotate.y, -move_max_rotate, move_max_rotate), 0f);
                current_rotate = Vector3.Lerp(current_rotate, target_rotate, move_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_rotate = new Vector3(0f, 0f, deck_angle);
                current_rotate = new Vector3(0f, 0f, deck_angle);
            }

            card_transform.anchoredPosition = Vector2.Lerp(card_transform.anchoredPosition, target_position, Time.deltaTime * move_speed);
            card_transform.localRotation = Quaternion.Slerp(card_transform.localRotation, Quaternion.Euler(current_rotate), Time.deltaTime * move_speed);
            card_transform.localScale = Vector3.Lerp(card_transform.localScale, target_size, 5f * Time.deltaTime);

            card_ui.SetCard(card);
            card_glow.enabled = IsFocus() || IsDrag() || discardSelected;
            prev_pos = Vector3.Lerp(prev_pos, card_transform.position, 1f * Time.deltaTime);

            //Unselect
            if (!drag && selected && Input.GetMouseButtonDown(0))
                selected = false;
        }

        private Vector2 GetTargetPosition()
        {
            Card card = GetCard();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hand_transform, Input.mousePosition, Camera.main, out Vector2 tpos);
            if (card.CardData.IsRequireTarget())
            {
                tpos = deck_position + Vector2.up * 150f + Vector2.right * tpos.x / 10f;
            }
            return tpos;
        }

        public void SetCard(Card card)
        {
            this.card_uid = card.uid;
            card_ui.SetCard(card);
            if (Vc5R4Rules.IsTemporary(card))
            {
                Card commander = GameClient.Get().GetGameData().GetCard(card.r4_commander_uid);
                BoardCard source = commander != null ? BoardCard.Get(commander.uid) : null;
                if (source != null)
                {
                    Vector3 screen = Camera.main.WorldToScreenPoint(source.transform.position);
                    Canvas canvas = GetComponentInParent<Canvas>();
                    Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(hand_transform, screen, camera, out Vector2 point);
                    card_transform.anchoredPosition = point;
                }
            }
        }

        public void Kill()
        {
            if (!destroyed)
            {
                destroyed = true;
                if (Vc5R4Rules.IsTemporary(GetCard())) StartCoroutine(DissolveTemporary());
                else Destroy(gameObject);
            }
        }

        IEnumerator DissolveTemporary()
        {
            drag = false; focus = false; selected = false;
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            Vector3 scale = transform.localScale;
            for (float t = 0; t < 0.18f; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / 0.18f;
                transform.localScale = scale * (1f + t * 0.5f);
                yield return null;
            }
            Destroy(gameObject);
        }

        public bool IsFocus()
        {
            if (GameTool.IsMobile())
                return selected && !drag;
            return focus && !drag && focus_timer > 0f;
        }

        public bool IsDrag()
        {
            return drag;
        }

        public Card GetCard()
        {
            Game gdata = GameClient.Get().GetGameData();
            return gdata.GetCard(card_uid);
        }

        public CardData GetCardData()
        {
            Card card = GetCard();
            if (card != null)
                return CardData.Get(card.card_id);
            return null;
        }

        public string GetCardUID()
        {
            return card_uid;
        }

        public void OnMouseEnterCard()
        {
            if (GameUI.IsUIOpened())
                return;

            focus = true;
            Card c = GetCard();
            if (c != null && !drag)
                CardDetailPreview.ShowCard(c);
        }

        public void OnMouseExitCard()
        {
            focus = false;
            focus_timer = -0.2f;
            CardDetailPreview.Hide();
        }

        public void OnMouseDownCard()
        {
            if (GameUI.IsOverUILayer("UI"))
                return;

            Game data = GameClient.Get().GetGameData();
            if (data.phase == GamePhase.EndDiscard)
            {
                Player player = GameClient.Get().GetPlayer();
                discard_press = player != null && !player.end_discard_passed
                    && !GameClient.Get().IsObserveMode() && !HandCardArea.Get().IsConfirmingDiscard;
                drag_start_screen_pos = Input.mousePosition;
                return;
            }

            Card tutorialCard = GetCard();
            if (!Vc5DemoTutorialOverlay.CanBeginTutorialCardDrag(
                tutorialCard != null ? tutorialCard.card_id : string.Empty))
                return;

            UnselectAll();
            drag_start_screen_pos = Input.mousePosition;
            drag = true;
            selected = true;
            PlayerControls.Get().UnselectAll();
            Card c = GetCard();
            if (c != null)
                CardDetailPreview.ShowCard(c);
            AudioTool.Get().PlaySFX("hand_card", AssetData.Get().hand_card_click_audio);
        }

        public void OnMouseUpCard()
        {
            Game gdata = GameClient.Get().GetGameData();
            Player player = gdata?.GetPlayer(GameClient.Get().GetPlayerID());
            Card card = GetCard();

            if (discard_press || gdata.phase == GamePhase.EndDiscard)
            {
                if (discard_press && player != null && card != null
                    && ShouldToggleDiscardOnRelease(gdata.phase, player.end_discard_passed,
                        drag_start_screen_pos, Input.mousePosition))
                {
                    HandCardArea.Get().ToggleDiscardSelection(card.uid);
                    CardPreviewUI.SelectEndDiscardCard(card);
                }
                discard_press = false;
                drag = false;
                return;
            }

            Vector2 mpos = GameCamera.Get().MouseToPercent(Input.mousePosition);
            Vector3 board_pos = GameBoard.Get().RaycastMouseBoard();

            bool confirmC3 = Vc5C3Rules.IsCard(card)
                && Vector2.Distance(drag_start_screen_pos, Input.mousePosition) >= Vc5DiscardDragThreshold;
            if (drag && (Vc5C3Rules.IsCard(card) ? confirmC3 : mpos.y > 0.25f))
                TryPlayCard(board_pos);//尝试出牌
            else if (!GameTool.IsMobile())
                HandCardArea.Get().SortCards();
            drag = false;
            if (IsFocus() && GetCard() != null)
                CardDetailPreview.ShowCard(GetCard());
        }

        public void TryPlayCard(Vector3 board_pos)
        {
            if (!GameClient.Get().IsYourTurn())
            {
                WarningText.ShowNotYourTurn();
                return;
            }

            BSlot bslot = BSlot.GetNearest(board_pos);
            int player_id = GameClient.Get().GetPlayerID();
            Game gdata = GameClient.Get().GetGameData();
            Player player = gdata.GetPlayer(player_id);
            Card card = GetCard();

            Slot slot = Slot.None;
            if (bslot != null)
                slot = bslot.GetEmptySlot(board_pos);
            if(bslot != null && card.CardData.IsRequireTarget())
                slot = bslot.GetSlot(board_pos);

            Card slot_card = bslot?.GetSlotCard(board_pos);
            if (Vc5C3Rules.IsCard(card))
            {
                slot_card = bslot != null ? Vc5DemoGrid.GetDisplayedSlotCard(gdata, bslot.GetSlot(board_pos)) : null;
                slot = slot_card != null ? slot_card.slot : Slot.None;
            }
            if (!Vc5DemoTutorialOverlay.CanPlayTutorialCardOnTarget(
                card != null ? card.card_id : string.Empty,
                slot_card != null ? slot_card.card_id : string.Empty))
                return;
            if (bslot != null && card.CardData.IsRequireTargetSpell() && slot_card != null && slot_card.HasStatus(StatusType.SpellImmunity))
            {
                WarningText.ShowSpellImmune();
                return;
            }

            if (!card.CardData.IsDynamicManaCost() && player.mana < card.GetMana())
            {
                WarningText.ShowNoMana();
                return;
            }
            if (card.CardData.hp_cost > 0 && player.hp <= card.CardData.hp_cost)
            {
                WarningText.ShowNoHP();
                return;
            }
            if (player.cards_hand.Count < card.CardData.discard_cost + 1)
            {
                WarningText.ShowNoDiscard();
                return;
            }

            if (gdata.CanPlayCard(card, slot, true))
            {
                PlayCard(slot);
            }
        }

        public static bool ShouldToggleDiscardOnRelease(GamePhase phase, bool endDiscardPassed,
            Vector2 dragStartScreenPos, Vector2 releaseScreenPos)
        {
            return phase == GamePhase.EndDiscard
                && !endDiscardPassed
                && Vector2.Distance(dragStartScreenPos, releaseScreenPos) < Vc5DiscardDragThreshold;
        }

        public void PlayCard(Slot slot)
        {
            GameClient.Get().PlayCard(GetCard(), slot);
            HandCardArea.Get().DelayRefresh(GetCard());
            Destroy(gameObject);
            if (GameTool.IsMobile())
                BoardCard.UnfocusAll();
        }

        public CardData CardData { get { return GetCardData(); } }

        public static HandCard GetDrag()
        {
            foreach (HandCard card in card_list)
            {
                if (card.IsDrag())
                    return card;
            }
            return null;
        }

        public static HandCard GetFocus()
        {
            foreach (HandCard card in card_list)
            {
                if (card.IsFocus())
                    return card;
            }
            return null;
        }

        public static HandCard Get(string uid)
        {
            foreach (HandCard card in card_list)
            {
                if (card && card.GetCardUID() == uid)
                    return card;
            }
            return null;
        }

        public static void UnselectAll()
        {
            foreach (HandCard card in card_list)
                card.selected = false;
        }

        public static List<HandCard> GetAll()
        {
            return card_list;
        }
    }
}
