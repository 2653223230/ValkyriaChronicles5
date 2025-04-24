using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Represents the visual deck on the board
    /// Will show number of cards in deck/discard when hovering
    /// </summary>
    
    public class BoardDeck : MonoBehaviour
    {
        public bool opponent;
        public UIPanel hover_panel;
        public SpriteRenderer deck_render;
        public Text deck_value;
        public Text discard_value;

        private bool hover = false;
        
        void Start()
        {
            if (GameTool.IsMobile())
            {
                hover_panel?.SetVisible(true);
            }
        }

        void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!GameClient.Get().IsReady())
                return;

            Player player = opponent ? GameClient.Get().GetOpponentPlayer() : GameClient.Get().GetPlayer();
            if (player == null)
                return;

            CardbackData cb = CardbackData.Get(player.cardback);
            if (deck_render != null && cb != null)
                deck_render.sprite = cb.deck;

            if (deck_value != null)
                deck_value.text = player.cards_deck.Count.ToString();
            if (discard_value != null)
                discard_value.text = player.cards_discard.Count.ToString();
        }

        public void ShowDeckCards()
        {
            Player player = GameClient.Get().GetPlayer();
            CardSelector.Get().Show(player.cards_deck, "DECK");
        }

        public void ShowDiscardCards()
        {
            Player player = opponent ? GameClient.Get().GetOpponentPlayer() : GameClient.Get().GetPlayer();
            CardSelector.Get().Show(player.cards_discard, "DISCARD");
        }

        private void ShowHover(bool hover)
        {
            if(!GameTool.IsMobile())
                hover_panel?.SetVisible(hover);
        }

        private void OnMouseEnter()
        {
            hover = true;
            ShowHover(hover);
            Refresh();
        }

        private void OnMouseExit()
        {
            hover = false;
            ShowHover(hover);
        }

        private void OnDisable()
        {
            hover = false;
            ShowHover(hover);
        }

        private void OnMouseOver()
        {
            if (!opponent && Input.GetMouseButtonDown(0))
                ShowDeckCards(); //Cannot see opponent deck
            else if(Input.GetMouseButtonDown(1))
                ShowDiscardCards(); //Cant see both player discard
        }
    }
}
