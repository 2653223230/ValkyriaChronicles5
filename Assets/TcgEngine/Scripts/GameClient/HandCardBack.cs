using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Same as HandCard, but simpler version for the opponent's cards
    /// </summary>

    public class HandCardBack : MonoBehaviour
    {
        public Image card_sprite;

        private RectTransform rect;

        private static List<HandCardBack> card_list = new List<HandCardBack>();

        void Awake()
        {
            card_list.Add(this);
            rect = GetComponent<RectTransform>();
            SetCardback(null);
        }

        private void OnDestroy()
        {
            card_list.Remove(this);
        }

        public void SetCardback(CardbackData cb)
        {
            if (cb != null && cb.cardback != null)
                card_sprite.sprite = cb.cardback;
        }

        public RectTransform GetRect()
        {
            if (rect == null)
                return GetComponent<RectTransform>();
            return rect;
        }

    }
}
