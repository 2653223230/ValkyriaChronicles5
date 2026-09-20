using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// One choice in the choice selector
    /// Its a button you can click
    /// </summary>

    public class ChoiceSelectorChoice : MonoBehaviour
    {
        public Text title;
        public Text subtitle;
        public Image highlight;

        public UnityAction<int> onClick;

        private Button button;
        private int choice;
        private bool focus = false;
        private Text r4Title;
        private Text r4Subtitle;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void Update()
        {
            if (highlight != null)
                highlight.enabled = focus;
        }

        public void SetChoice(int choice, AbilityData ability)
        {
            this.choice = choice;
            this.title.text = ability.title;
            this.subtitle.text = ability.desc;
            bool r4 = ability.id.StartsWith(Vc5R4Rules.ChooseOrder);
            if (r4 && r4Title == null)
            {
                r4Title = CreateR4Text(title, "R4 title", 0.58f, 0.94f, 32);
                r4Subtitle = CreateR4Text(subtitle, "R4 description", 0.07f, 0.58f, 24);
            }
            title.enabled = !r4;
            subtitle.enabled = !r4;
            if (r4Title != null)
            {
                r4Title.gameObject.SetActive(r4);
                r4Subtitle.gameObject.SetActive(r4);
                r4Title.text = ability.title;
                r4Subtitle.text = ability.desc;
            }
            button.interactable = true;
            gameObject.SetActive(true);

            if (ability.mana_cost > 0)
                this.title.text += " (" + ability.mana_cost + ")";
        }

        public void SetInteractable(bool interact)
        {
            button.interactable = interact;
        }

        private Text CreateR4Text(Text source, string label, float bottom, float top, int maxSize)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Text));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            Text text = go.GetComponent<Text>();
            text.font = source.font;
            text.color = source.color;
            text.fontSize = maxSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = maxSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = new Vector2(0.05f, bottom);
            text.rectTransform.anchorMax = new Vector2(0.95f, top);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            return text;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnClick()
        {
            onClick?.Invoke(choice);
        }

        public void MouseEnter()
        {
            if (button.interactable)
                focus = true;
        }

        public void MouseExit()
        {
            focus = false;
        }
    }
}
