using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Area where all the hand cards are
    /// Will take card of spawning/despawning hand cards based on the refresh data received from server
    /// </summary>

    public class HandCardArea : MonoBehaviour
    {
        public GameObject card_prefab;
        public RectTransform card_area;
        public float card_spacing = 100f;
        public float card_angle = 10f;
        public float card_offset_y = 10f;

        private List<HandCard> cards = new List<HandCard>();

        private bool is_dragging;
        private readonly HashSet<string> discard_selection = new HashSet<string>();
        private bool confirming_discard;
        private int discard_round = -1;
        private int discard_player = -1;
        public int DiscardSelectionCount { get { return discard_selection.Count; } }
        public bool IsConfirmingDiscard { get { return confirming_discard; } }
        public string DiscardError { get; private set; }

        private string last_destroyed;
        private float last_destroyed_timer = 0f;

        private static HandCardArea _instance;

        void Awake()
        {
            _instance = this;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            int player_id = GameClient.Get().GetPlayerID();
            Game data = GameClient.Get().GetGameData();
            Player player = data.GetPlayer(player_id);

            if (data.phase != GamePhase.EndDiscard || player.end_discard_passed
                || discard_round != data.turn_count || discard_player != player_id)
            {
                ClearDiscardSelection();
                discard_round = data.turn_count;
                discard_player = player_id;
            }
            discard_selection.RemoveWhere(uid => player.GetHandCard(uid) == null);

            last_destroyed_timer += Time.deltaTime;

            //Add new cards
            foreach (Card card in player.cards_hand)
            {
                if (!HasCard(card.uid))
                    SpawnNewCard(card);
            }

            //Remove destroyed cards
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                HandCard card = cards[i];
                if (card == null || player.GetHandCard(card.GetCardUID()) == null)
                {
                    cards.RemoveAt(i);
                    if(card != null)
                        card.Kill();
                }
            }

            //Set card index
            int index = 0;
            float count_half = cards.Count / 2f;
            foreach (HandCard card in cards)
            {
                card.deck_position = new Vector2((index - count_half) * card_spacing, (index - count_half) * (index - count_half) * -card_offset_y);
                card.deck_angle = (index - count_half) * -card_angle;
                index++;
            }

            //Set target forcus
            HandCard drag_card = HandCard.GetDrag();
            is_dragging = drag_card != null;
        }

        public void SpawnNewCard(Card card)
        {
            GameObject card_obj = Instantiate(card_prefab, card_area.transform);
            card_obj.GetComponent<HandCard>().SetCard(card);
            if (!Vc5R4Rules.IsTemporary(card))
                card_obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -100f);
            cards.Add(card_obj.GetComponent<HandCard>());
        }

        public bool IsDiscardSelected(string uid)
        {
            return discard_selection.Contains(uid);
        }

        public void ToggleDiscardSelection(string uid)
        {
            if (confirming_discard || string.IsNullOrEmpty(uid)) return;
            if (!discard_selection.Remove(uid)) discard_selection.Add(uid);
            DiscardError = null;
        }

        public void ClearDiscardSelection()
        {
            discard_selection.Clear();
            confirming_discard = false;
            DiscardError = null;
        }

        public void ConfirmDiscardSelection()
        {
            GameClient client = GameClient.Get();
            Game data = client.GetGameData();
            Player player = client.GetPlayer();
            if (confirming_discard || !client.IsReady() || data.phase != GamePhase.EndDiscard
                || player == null || player.end_discard_passed || client.IsObserveMode()) return;
            confirming_discard = true;
            DiscardError = null;
            StartCoroutine(SubmitDiscards(new List<string>(discard_selection), data.turn_count, player.player_id));
        }

        private IEnumerator SubmitDiscards(List<string> uids, int round, int playerId)
        {
            // The existing action channel is Reliable, not sequenced. Wait for each
            // authoritative removal before sending another discard or the final pass.
            GameClient client = GameClient.Get();
            foreach (string uid in uids)
            {
                if (!CanContinueDiscard(client, round, playerId)) yield break;
                Card card = client.GetPlayer().GetHandCard(uid);
                if (card == null) continue;
                client.DiscardEndPhaseCard(card);
                float deadline = Time.realtimeSinceStartup + 8f;
                while (CanContinueDiscard(client, round, playerId)
                    && client.GetPlayer().GetHandCard(uid) != null)
                {
                    if (Time.realtimeSinceStartup > deadline)
                    {
                        confirming_discard = false;
                        DiscardError = "弃牌未确认，请检查连接后重试";
                        yield break;
                    }
                    yield return null;
                }
                discard_selection.Remove(uid);
            }
            if (!CanContinueDiscard(client, round, playerId)) yield break;
            client.EndStage();
            float passDeadline = Time.realtimeSinceStartup + 8f;
            while (CanContinueDiscard(client, round, playerId) && Time.realtimeSinceStartup < passDeadline)
                yield return null;
            if (CanContinueDiscard(client, round, playerId))
                DiscardError = "完成弃牌未确认，请检查连接后重试";
            confirming_discard = false;
        }

        private bool CanContinueDiscard(GameClient client, int round, int playerId)
        {
            Game data = client.GetGameData();
            Player player = client.GetPlayer();
            bool valid = confirming_discard && client.IsReady() && data != null
                && data.state != GameState.GameEnded && data.phase == GamePhase.EndDiscard
                && data.turn_count == round && client.GetPlayerID() == playerId
                && player != null && !player.end_discard_passed;
            if (!valid) confirming_discard = false;
            return valid;
        }

        public void DelayRefresh(Card card)
        {
            last_destroyed_timer = 0f;
            last_destroyed = card.uid;
        }

		public void SortCards()
        {
            cards.Sort(SortFunc);

            int i = 0;
            foreach (HandCard acard in cards)
            {
                acard.transform.SetSiblingIndex(i);
                i++;
            }
        }

        private int SortFunc(HandCard a, HandCard b)
        {
            return a.transform.position.x.CompareTo(b.transform.position.x);
        }

        public bool HasCard(string card_uid)
        {
            HandCard card = HandCard.Get(card_uid);
            bool just_destroyed = card_uid == last_destroyed && last_destroyed_timer < 0.7f;
            return card != null || just_destroyed;
        }

        public bool IsDragging()
        {
            return is_dragging;
        }


        public static HandCardArea Get()
        {
            return _instance;
        }
    }
}
