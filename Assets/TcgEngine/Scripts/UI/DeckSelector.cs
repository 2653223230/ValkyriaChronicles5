using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// Deck selector is a dropdown that let the player select a deck before a match
    /// 牌组选择器是一个下拉菜单，让玩家在比赛前选择牌组
    /// </summary>

    public class DeckSelector : MonoBehaviour
    {
        public DropdownValue deck_dropdown;

        public UnityAction<string> onChange;

        void Start()
        {
            if (deck_dropdown != null)
                deck_dropdown.onValueChanged += OnChange;
        }

        void Update()
        {

        }

        public void RefreshDeckList()
        {
            if (deck_dropdown == null)
            {
                Debug.LogError("DeckSelector: assign deck_dropdown (DropdownValue).");
                return;
            }

            deck_dropdown.ClearOptions();

            GameplayData gdata = GameplayData.Get();
            if (gdata != null && gdata.free_decks != null)
            {
                foreach (DeckData deck in gdata.free_decks)
                {
                    if (deck == null || string.IsNullOrEmpty(deck.id))
                        continue;
                    deck_dropdown.AddOption(deck.id, string.IsNullOrEmpty(deck.title) ? deck.id : deck.title);
                }
            }

            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                foreach (UserDeckData deck in udata.decks)
                {
                    if (udata.IsDeckValid(deck))
                    {
                        deck_dropdown.AddOption(deck.tid, deck.title);
                    }
                }
            }
        }

        private void SelectDeck(UserDeckData deck)
        {
            if (deck != null)
            {
                deck_dropdown.SetValue(deck.tid);
            }
        }

        private void SelectDeck(DeckData deck)
        {
            if (deck != null)
            {
                deck_dropdown.SetValue(deck.id);
            }
        }

        public void SelectDeck(string deck)
        {
            //Make sure deck exists, to prevent assigning invalid deck
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = udata?.GetDeck(deck);
            if (udeck != null)
            {
                SelectDeck(udeck);
                return;
            }

            DeckData adeck = DeckData.Get(deck);
            if(adeck != null)
                SelectDeck(adeck);
        }

        public void Lock()
        {
            if (deck_dropdown != null)
                deck_dropdown.interactable = false;
        }

        public void Unlock()
        {
            if (deck_dropdown != null)
                deck_dropdown.interactable = true;
        }

        public void SetLocked(bool locked)
        {
            if (deck_dropdown != null)
                deck_dropdown.interactable = !locked;
        }

        private void OnChange(int i, string val)
        {
            if (deck_dropdown == null)
                return;
            string value = deck_dropdown.GetSelectedValue();
            onChange?.Invoke(value);
        }

        public string GetDeckID()
        {
            return deck_dropdown != null ? deck_dropdown.GetSelectedValue() : "";
        }

        public string GetDeckTitle()
        {
            return deck_dropdown != null ? deck_dropdown.GetSelectedText() : "";
        }

        public UserDeckData GetDeck()
        {
            UserData user = Authenticator.Get().UserData;
            UserDeckData udeck = user.GetDeck(GetDeckID()); //Check for user custom deck
            DeckData deck = DeckData.Get(GetDeckID());     //Check for deck presets
            if (udeck != null)
                return udeck; 
            else if (deck != null)
                return new UserDeckData(deck); 
            return null;
        }
    }
}