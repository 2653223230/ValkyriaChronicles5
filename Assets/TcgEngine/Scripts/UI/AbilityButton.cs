using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Ability button on a BoardCard, let you activate abilities
    /// </summary>

    public class AbilityButton : MonoBehaviour
    {
        public Text text;
        public Image focus_highlight;

        private Card card;
        private AbilityData iability;

        private CanvasGroup canvas_group;
        private float target_alpha = 0f;
        private bool focus = false;
        private bool nextfocus = false;
        private bool interactable = false;

        private static List<AbilityButton> button_list = new List<AbilityButton>();

        void Awake()
        {
            button_list.Add(this);
            canvas_group = GetComponent<CanvasGroup>();
            if (canvas_group == null)
            {
                canvas_group = gameObject.AddComponent<CanvasGroup>();
            }
            canvas_group.alpha = 0f;
            if (focus_highlight != null)
                focus_highlight.enabled = false;
            
            // 确保Text组件已初始化
            if (text == null)
            {
                text = GetComponentInChildren<Text>(true);
            }
        }

        private void OnDestroy()
        {
            button_list.Remove(this);
        }

        void Update()
        {
            canvas_group.alpha = Mathf.MoveTowards(canvas_group.alpha, target_alpha, 5f * Time.deltaTime);
            focus = nextfocus;

            if (focus_highlight != null && IsVisible())
                focus_highlight.enabled = focus && interactable;
        }

        public void SetAbility(Card card, AbilityData iability)
        {
            // 确保canvas_group已初始化
            if (canvas_group == null)
                canvas_group = GetComponent<CanvasGroup>();
            
            // 确保text组件已初始化（包括未激活的子对象）
            if (text == null)
            {
                text = GetComponentInChildren<Text>(true); // 包括未激活的子对象
            }
            
            // 如果还是找不到，尝试从Button组件获取Text
            if (text == null)
            {
                Button btn = GetComponent<Button>();
                if (btn != null)
                {
                    text = btn.GetComponentInChildren<Text>(true);
                }
            }
            
            this.card = card;
            this.iability = iability;
            
            if (text != null && iability != null)
            {
                // 确保Text组件的GameObject是激活的
                if (!text.gameObject.activeSelf)
                {
                    text.gameObject.SetActive(true);
                }
                
                text.text = iability.title;
                if (this.iability.mana_cost > 0)
                    text.text += " (" + this.iability.mana_cost + ")";
                
                // 确保Text组件可见
                text.enabled = true;
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1f); // 确保文字完全不透明
                
                // 确保Text的RectTransform正确设置
                RectTransform text_rect = text.GetComponent<RectTransform>();
                if (text_rect != null)
                {
                    text_rect.localScale = Vector3.one;
                }
                
                Debug.Log($"[AbilityButton] SetAbility: 设置技能名称='{text.text}', text.enabled={text.enabled}, text.gameObject.activeSelf={text.gameObject.activeSelf}, color.a={text.color.a}");
            }
            else
            {
                if (text == null)
                    Debug.LogWarning($"[AbilityButton] SetAbility: text组件为null！GameObject: {gameObject.name}");
                if (iability == null)
                    Debug.LogWarning("[AbilityButton] SetAbility: iability为null！");
            }
            
            if (canvas_group == null)
            {
                canvas_group = gameObject.AddComponent<CanvasGroup>();
            }
            
            if (canvas_group != null)
            {
                canvas_group.interactable = true;
                canvas_group.blocksRaycasts = true;
                canvas_group.alpha = 1f; // 立即设置为可见
            }
            target_alpha = 1f;
        }

        public void SetInteractable(bool interact)
        {
            interactable = interact;
        }

        public void Hide()
        {
            if (canvas_group == null)
                canvas_group = GetComponent<CanvasGroup>();

            this.card = null;
            this.iability = null;
            canvas_group.interactable = false;
            canvas_group.blocksRaycasts = false;
            target_alpha = 0f;
        }

        public void OnClick()
        {
            if (card != null && iability != null)
            {
                Game gdata = GameClient.Get().GetGameData();
                Player player = GameClient.Get().GetPlayer();
                if (gdata == null || player == null || !gdata.CanCastAbility(card, iability))
                {
                    if (card.IsAbilityOnCooldown(iability))
                        WarningText.ShowCooldown();
                    else if (player != null && player.main_action_used && !iability.fast_action && !gdata.IsVc5TestMode(player))
                        WarningText.ShowMainActionUsed();
                    else if (player != null && !player.CanPayAbility(card, iability))
                        WarningText.ShowNoMana();
                    else
                        WarningText.ShowExhausted();
                    return;
                }
                GameClient.Get().CastAbility(card, iability);
                PlayerControls.Get().UnselectAll();
            }
        }

        public AbilityData GetAbility()
        {
            return iability;
        }

        public bool IsVisible()
        {
            return canvas_group.alpha > 0.5f;
        }

        public bool IsInteractable()
        {
            return interactable && IsVisible();
        }

        public void MouseEnter()
        {
            focus = true;
            nextfocus = true;
        }

        public void MouseExit()
        {
            nextfocus = false; //Keep it focused 1 more frame to work on mobile
        }

        public static AbilityButton GetFocus(Vector3 pos, float range = 999f)
        {
            AbilityButton nearest = null;
            float min_dist = range;
            foreach (AbilityButton button in button_list)
            {
                float dist = (button.transform.position - pos).magnitude;
                if (button.focus && button.IsVisible() && dist < min_dist)
                {
                    min_dist = dist;
                    nearest = button;
                }
            }
            return nearest;
        }

        public static AbilityButton GetNearest(Vector3 pos, float range = 999f)
        {
            AbilityButton nearest = null;
            float min_dist = range;
            foreach (AbilityButton button in button_list)
            {
                float dist = (button.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = button;
                }
            }
            return nearest;
        }

    }
}
