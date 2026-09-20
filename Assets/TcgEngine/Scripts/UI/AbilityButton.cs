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
        private bool panelPresentation;
        private Text description;
        private Text availability;
        private Image panelBackground;

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
            if (panelPresentation)
                RefreshPanelState();
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

        // Opt-in: board-card ability buttons keep their existing presentation.
        public void ConfigurePanelPresentation()
        {
            panelPresentation = true;
            if (text == null || iability == null)
                return;
            panelBackground = GetComponent<Image>();
            if (panelBackground != null)
                panelBackground.sprite = null;
            Button button = GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.8f, 1f, 0.96f);
                colors.pressedColor = new Color(0.6f, 0.85f, 0.8f);
                colors.disabledColor = Color.white;
                button.colors = colors;
            }
            if (focus_highlight != null)
                focus_highlight.color = new Color(0.25f, 0.8f, 0.7f, 0.12f);
            ConfigureLine(text, 10f, 28f, 19);
            text.supportRichText = true;
            string title = iability.id == Vc5R4Rules.PrepareSkill ? "战术筹划" : iability.title;
            text.text = "<b>" + title + "</b>";
            Text metadata = CreatePanelLine("SkillCost", 38f, 20f, 14);
            metadata.color = new Color(0.54f, 0.85f, 0.79f);
            metadata.text = iability.mana_cost + " 法力" + (iability.fast_action ? " · 快速" : " · 主行动")
                + (iability.uses_per_turn > 0 ? " · 每回合 " + iability.uses_per_turn + " 次" : "");
            description = CreatePanelLine("SkillDescription", 60f, 20f, 15);
            description.text = iability.id == Vc5R4Rules.PrepareSkill ? "弃 1 张非临时牌，获得临时指令。"
                : iability.id == Vc5R4Rules.MoveSkill ? "移动至相邻空格；取消不扣费。"
                : iability.GetDesc(card.CardData).Replace('\n', ' ');
            availability = CreatePanelLine("SkillAvailability", 82f, 18f, 13);
            RefreshPanelState();
        }

        private Text CreatePanelLine(string name, float top, float height, int size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(transform, false);
            Text label = obj.GetComponent<Text>();
            label.font = text.font;
            ConfigureLine(label, top, height, size);
            return label;
        }

        private static void ConfigureLine(Text label, float top, float height, int size)
        {
            label.fontSize = size;
            label.resizeTextForBestFit = false;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = new Color(0.91f, 0.95f, 0.96f);
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-24f, height);
        }

        private void RefreshPanelState()
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || card == null || iability == null || availability == null)
                return;
            Player player = data.GetPlayer(card.player_id);
            bool actionTurn = data.phase == GamePhase.Main && data.IsPlayerActionTurn(player) && !player.EndTurn;
            bool usable = actionTurn && data.CanCastAbility(card, iability);
            SetInteractable(usable);
            Button button = GetComponent<Button>();
            if (button != null)
                button.interactable = usable;
            if (panelBackground != null)
                panelBackground.color = usable ? new Color(0.10f, 0.27f, 0.28f) : new Color(0.14f, 0.18f, 0.21f);
            string reason = "点击使用";
            if (!usable)
            {
                if (!actionTurn) reason = "等待你的行动回合";
                else if (card.IsAbilityOnCooldown(iability)) reason = "本回合次数已用完";
                else if (!player.CanPayAbility(card, iability)) reason = "费用不足";
                else if (!card.CanDoActivatedAbilities()) reason = "单位当前无法使用技能";
                else if (player.main_action_used && !iability.fast_action && !data.IsVc5TestMode(player)) reason = "本回合主行动已用完";
                else if (iability.id == Vc5R4Rules.PrepareSkill) reason = "没有可弃的非临时手牌";
                else if (iability.id == Vc5R4Rules.MoveSkill) reason = "没有可移动的相邻空格";
                else reason = "当前不满足使用条件";
            }
            availability.text = reason;
            availability.color = usable ? new Color(0.61f, 0.89f, 0.79f) : new Color(0.92f, 0.72f, 0.53f);
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
            if (panelPresentation)
            {
                RefreshPanelState();
                if (!interactable)
                    return;
            }
            if (card != null && iability != null)
            {
                if (!Vc5DemoTutorialOverlay.CanUseTutorialAbility(card.card_id, iability.id))
                    return;
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
                Vc5DemoTutorialOverlay.NotifyAbilityStarted(iability.id, card.card_id);
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
