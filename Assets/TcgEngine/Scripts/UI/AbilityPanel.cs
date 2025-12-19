using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 技能按钮面板，显示在游戏界面右下方
    /// 当点击棋子时，会显示该棋子可用的技能按钮
    /// </summary>
    public class AbilityPanel : UIPanel
    {
        [Header("Ability Buttons")]
        public RectTransform buttons_container;
        public GameObject ability_button_prefab;
        
        private Card selected_card = null;
        private List<AbilityButton> ability_buttons = new List<AbilityButton>();
        
        private static AbilityPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            
            Debug.Log("[AbilityPanel] Awake 被调用，instance已设置");
            
            // 检查必要的引用
            if (ability_button_prefab == null)
            {
                Debug.LogWarning("[AbilityPanel] Awake: ability_button_prefab 未设置！");
            }
            if (buttons_container == null)
            {
                Debug.LogWarning("[AbilityPanel] Awake: buttons_container 未设置！");
            }
            
            // 确保面板初始隐藏
            Hide(true);
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            // 重写Update以确保面板在显示时保持完全可见
            // 基类的Update会根据visible状态淡入淡出，但我们希望立即显示
            if (visible && canvas_group != null)
            {
                // 如果面板应该显示，确保alpha为1
                if (canvas_group.alpha < 0.99f)
                {
                    canvas_group.alpha = Mathf.MoveTowards(canvas_group.alpha, 1f, display_speed * Time.deltaTime);
                }
                else
                {
                    canvas_group.alpha = 1f;
                }
                
                // 确保buttons_container和所有按钮都是可见的
                if (buttons_container != null)
                {
                    if (!buttons_container.gameObject.activeSelf)
                    {
                        buttons_container.gameObject.SetActive(true);
                        Debug.Log("[AbilityPanel] 激活buttons_container");
                    }
                    
                    // 确保所有按钮都是激活的
                    for (int i = 0; i < buttons_container.childCount; i++)
                    {
                        Transform child = buttons_container.GetChild(i);
                        if (!child.gameObject.activeSelf)
                        {
                            child.gameObject.SetActive(true);
                            Debug.Log($"[AbilityPanel] 激活按钮 {i}: {child.name}");
                        }
                    }
                }
            }
            else
            {
                // 如果面板应该隐藏，使用基类的淡出逻辑
                base.Update();
            }
        }

        /// <summary>
        /// 显示指定卡片的技能按钮
        /// </summary>
        public void ShowAbilities(Card card)
        {
            Debug.Log($"[AbilityPanel] ShowAbilities 被调用，卡片: {(card != null ? card.card_id : "null")}");
            
            if (card == null)
            {
                Debug.LogWarning("[AbilityPanel] 卡片为null，隐藏面板");
                Hide();
                return;
            }

            selected_card = card;
            ClearButtons();

            Game data = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();

            if (data == null || player == null)
            {
                Debug.LogError("[AbilityPanel] Game或Player为null！");
                return;
            }

            // 只显示当前玩家自己的卡片的技能
            if (card.player_id != player.player_id)
            {
                Debug.Log($"[AbilityPanel] 卡片不属于当前玩家 (card.player_id={card.player_id}, player.player_id={player.player_id})，隐藏面板");
                Hide();
                return;
            }

            // 检查prefab和container是否设置
            if (ability_button_prefab == null)
            {
                Debug.LogError("[AbilityPanel] ability_button_prefab 未设置！请在Unity编辑器中设置。");
                return;
            }
            
            if (buttons_container == null)
            {
                Debug.LogError("[AbilityPanel] buttons_container 未设置！请在Unity编辑器中设置。");
                return;
            }

            // 获取卡片的所有激活技能
            List<AbilityData> abilities = card.GetAbilities();
            Debug.Log($"[AbilityPanel] 卡片 {card.card_id} 共有 {abilities.Count} 个技能");
            
            int index = 0;
            int activate_count = 0;
            
            foreach (AbilityData iability in abilities)
            {
                if (iability != null)
                {
                    Debug.Log($"[AbilityPanel] 检查技能: {iability.id}, trigger={iability.trigger}");
                    if (iability.trigger == AbilityTrigger.Activate)
                    {
                        Debug.Log($"[AbilityPanel] 找到激活技能: {iability.id}，创建按钮");
                        CreateAbilityButton(card, iability, index);
                        index++;
                        activate_count++;
                    }
                }
            }

            // 检查装备卡的技能
            Card equip = data.GetEquipCard(card.equipped_uid);
            if (equip != null)
            {
                Debug.Log($"[AbilityPanel] 检查装备卡技能");
                List<AbilityData> equip_abilities = equip.GetAbilities();
                foreach (AbilityData iability in equip_abilities)
                {
                    if (iability != null && iability.trigger == AbilityTrigger.Activate)
                    {
                        Debug.Log($"[AbilityPanel] 找到装备激活技能: {iability.id}，创建按钮");
                        CreateAbilityButton(equip, iability, index);
                        index++;
                        activate_count++;
                    }
                }
            }

            Debug.Log($"[AbilityPanel] 总共找到 {activate_count} 个激活技能，创建了 {ability_buttons.Count} 个按钮");

            // 如果有技能按钮，显示面板
            if (ability_buttons.Count > 0)
            {
                Debug.Log($"[AbilityPanel] 显示面板，按钮数量: {ability_buttons.Count}");
                Debug.Log($"[AbilityPanel] 面板gameObject.activeSelf: {gameObject.activeSelf}, CanvasGroup.alpha: {canvas_group.alpha}");
                
                // 强制刷新布局（如果使用布局组件）
                if (buttons_container != null)
                {
                    LayoutGroup layout = buttons_container.GetComponent<LayoutGroup>();
                    if (layout != null)
                    {
                        // 确保布局组件设置正确
                        if (layout is VerticalLayoutGroup vlg)
                        {
                            vlg.childControlHeight = true;
                            vlg.childControlWidth = true;
                            vlg.childForceExpandHeight = false;
                            vlg.childForceExpandWidth = true;
                            vlg.spacing = 10f; // 按钮间距
                            vlg.padding = new RectOffset(10, 10, 10, 10); // 内边距
                            Debug.Log($"[AbilityPanel] VerticalLayoutGroup设置: spacing={vlg.spacing}, childControlHeight={vlg.childControlHeight}, childControlWidth={vlg.childControlWidth}");
                        }
                        
                        // 强制刷新布局
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate(buttons_container);
                        Debug.Log($"[AbilityPanel] 强制刷新布局组件: {layout.GetType().Name}");
                    }
                    
                    // 检查buttons_container的子对象数量
                    Debug.Log($"[AbilityPanel] buttons_container子对象数量: {buttons_container.childCount}");
                    for (int i = 0; i < buttons_container.childCount; i++)
                    {
                        Transform child = buttons_container.GetChild(i);
                        RectTransform childRect = child.GetComponent<RectTransform>();
                        if (childRect != null)
                        {
                            Debug.Log($"[AbilityPanel] 子对象 {i}: {child.name}, activeSelf={child.gameObject.activeSelf}");
                            Debug.Log($"[AbilityPanel]    anchoredPosition={childRect.anchoredPosition}, sizeDelta={childRect.sizeDelta}");
                            Debug.Log($"[AbilityPanel]    anchorMin={childRect.anchorMin}, anchorMax={childRect.anchorMax}, pivot={childRect.pivot}");
                            
                            // 检查CanvasGroup
                            CanvasGroup cg = child.GetComponent<CanvasGroup>();
                            if (cg != null)
                            {
                                Debug.Log($"[AbilityPanel]    CanvasGroup: alpha={cg.alpha}, interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}");
                            }
                            
                            // 检查Text组件
                            Text txt = child.GetComponentInChildren<Text>();
                            if (txt != null)
                            {
                                Debug.Log($"[AbilityPanel]    Text: text='{txt.text}', enabled={txt.enabled}, color.a={txt.color.a}, gameObject.activeSelf={txt.gameObject.activeSelf}");
                            }
                        }
                    }
                }
                
                Show();
                Debug.Log($"[AbilityPanel] Show()调用后，gameObject.activeSelf: {gameObject.activeSelf}, visible: {visible}, CanvasGroup.alpha: {canvas_group.alpha}");
            }
            else
            {
                Debug.LogWarning($"[AbilityPanel] 没有找到激活技能，隐藏面板。卡片 {card.card_id} 可能没有 AbilityTrigger.Activate 类型的技能。");
                Hide();
            }
        }

        /// <summary>
        /// 创建技能按钮
        /// </summary>
        private void CreateAbilityButton(Card card, AbilityData iability, int index)
        {
            if (ability_button_prefab == null || buttons_container == null)
            {
                Debug.LogError("AbilityPanel: ability_button_prefab 或 buttons_container 未设置！");
                return;
            }

            GameObject button_obj = Instantiate(ability_button_prefab, buttons_container);
            
            // 确保按钮对象是激活的，这样Awake才会执行
            button_obj.SetActive(true);
            
            // 确保按钮的RectTransform正确设置（使用布局组件的自动布局）
            RectTransform btn_rect = button_obj.GetComponent<RectTransform>();
            if (btn_rect != null)
            {
                // 确保scale为1，这样按钮大小正常
                btn_rect.localScale = Vector3.one;

                // 为垂直布局提供显式尺寸，避免高度被压成一条灰线
                LayoutElement layoutElement = button_obj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = button_obj.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = 50f;
                layoutElement.minHeight = 40f;
                layoutElement.flexibleHeight = 0f;
                layoutElement.flexibleWidth = 1f;
                
                // 检查是否有布局组件
                LayoutGroup layout = buttons_container.GetComponent<LayoutGroup>();
                if (layout != null && layout is VerticalLayoutGroup)
                {
                    // 如果有VerticalLayoutGroup，需要确保按钮的RectTransform设置正确
                    // 对于VerticalLayoutGroup，按钮应该：
                    // 1. anchor设置为顶部拉伸（top-stretch）
                    // 2. pivot设置为顶部中心（0.5, 1）
                    // 3. sizeDelta设置高度，宽度会自动拉伸
                    
                    // 直接设置RectTransform（布局组件会自动处理位置）
                    btn_rect.anchorMin = new Vector2(0f, 1f); // 左上角
                    btn_rect.anchorMax = new Vector2(1f, 1f); // 右上角（水平拉伸）
                    btn_rect.pivot = new Vector2(0.5f, 1f); // 顶部中心
                    btn_rect.anchoredPosition = Vector2.zero; // 位置由布局组件控制
                    
                    // 设置按钮大小（宽度会自动拉伸，只需要设置高度）
                    btn_rect.sizeDelta = new Vector2(0f, 50f); // 高度50像素，宽度自动拉伸
                    
                    Debug.Log($"[AbilityPanel] 按钮 {index} 使用VerticalLayoutGroup，RectTransform设置: anchorMin={btn_rect.anchorMin}, anchorMax={btn_rect.anchorMax}, pivot={btn_rect.pivot}, sizeDelta={btn_rect.sizeDelta}");
                }
                else if (layout != null)
                {
                    // 其他类型的布局组件
                    btn_rect.anchorMin = new Vector2(0f, 1f);
                    btn_rect.anchorMax = new Vector2(1f, 1f);
                    btn_rect.pivot = new Vector2(0.5f, 1f);
                    btn_rect.anchoredPosition = Vector2.zero;
                    btn_rect.sizeDelta = new Vector2(0f, 50f);
                    Debug.Log($"[AbilityPanel] 按钮 {index} 使用其他布局组件: {layout.GetType().Name}");
                }
                else
                {
                    // 如果没有布局组件，手动设置位置
                    btn_rect.anchorMin = new Vector2(0.5f, 1f);
                    btn_rect.anchorMax = new Vector2(0.5f, 1f);
                    btn_rect.pivot = new Vector2(0.5f, 1f);
                    btn_rect.anchoredPosition = new Vector2(0, -index * 60); // 每个按钮间隔60像素
                    btn_rect.sizeDelta = new Vector2(280, 50); // 设置按钮大小
                    
                    Debug.Log($"[AbilityPanel] 按钮 {index} 无布局组件，手动设置位置: anchoredPosition={btn_rect.anchoredPosition}, sizeDelta={btn_rect.sizeDelta}");
                }
                
                Debug.Log($"[AbilityPanel] 按钮 {index} RectTransform最终设置: anchoredPosition={btn_rect.anchoredPosition}, sizeDelta={btn_rect.sizeDelta}, localScale={btn_rect.localScale}");
            }
            
            AbilityButton button = button_obj.GetComponent<AbilityButton>();
            
            if (button != null)
            {
                // 设置技能（SetAbility内部会处理null检查）
                button.SetAbility(card, iability);
                
                // 立即设置按钮可见（因为SetAbility设置了target_alpha=1f，但可能需要立即显示）
                CanvasGroup btn_canvas = button_obj.GetComponent<CanvasGroup>();
                if (btn_canvas == null)
                {
                    btn_canvas = button_obj.AddComponent<CanvasGroup>();
                }
                
                if (btn_canvas != null)
                {
                    btn_canvas.alpha = 1f;
                    btn_canvas.interactable = true;
                    btn_canvas.blocksRaycasts = true;
                }
                
                // 确保Text组件正确显示
                Text btn_text = button_obj.GetComponentInChildren<Text>(true); // 包括未激活的子对象
                if (btn_text == null)
                {
                    // 如果找不到Text组件，尝试从AbilityButton组件获取
                    btn_text = button.text;
                }
                
                if (btn_text != null)
                {
                    btn_text.enabled = true;
                    // 确保Text组件的GameObject是激活的
                    btn_text.gameObject.SetActive(true);
                    btn_text.color = new Color(btn_text.color.r, btn_text.color.g, btn_text.color.b, 1f); // 确保文字不透明
                    Debug.Log($"[AbilityPanel] 按钮 {index} Text组件: text='{btn_text.text}', enabled={btn_text.enabled}, color.a={btn_text.color.a}, gameObject.activeSelf={btn_text.gameObject.activeSelf}");
                }
                else
                {
                    Debug.LogWarning($"[AbilityPanel] 按钮 {index} 找不到Text组件！按钮对象: {button_obj.name}");
                }
                
                Game data = GameClient.Get().GetGameData();
                if (data != null)
                {
                    button.SetInteractable(data.CanCastAbility(card, iability));
                }
                
                ability_buttons.Add(button);
                
                if (btn_rect != null)
                {
                    Debug.Log($"[AbilityPanel] 成功创建按钮 {index}: {iability.title}");
                    Debug.Log($"[AbilityPanel] 按钮RectTransform: anchoredPosition={btn_rect.anchoredPosition}, sizeDelta={btn_rect.sizeDelta}, localScale={btn_rect.localScale}");
                    Debug.Log($"[AbilityPanel] 按钮CanvasGroup: alpha={btn_canvas?.alpha}, interactable={btn_canvas?.interactable}, blocksRaycasts={btn_canvas?.blocksRaycasts}");
                }
                
                // 确保Button组件的OnClick事件绑定到AbilityButton的OnClick方法
                Button btn_component = button_obj.GetComponent<Button>();
                if (btn_component != null)
                {
                    btn_component.onClick.RemoveAllListeners();
                    btn_component.onClick.AddListener(button.OnClick);
                    Debug.Log($"[AbilityPanel] 按钮 {index} Button组件onClick事件已绑定");
                }
                else
                {
                    Debug.LogWarning("AbilityPanel: AbilityButton prefab缺少Button组件！");
                }
            }
            else
            {
                Debug.LogError("AbilityPanel: 无法从prefab获取AbilityButton组件！prefab名称: " + ability_button_prefab.name);
            }
        }

        /// <summary>
        /// 清除所有技能按钮
        /// </summary>
        private void ClearButtons()
        {
            foreach (AbilityButton button in ability_buttons)
            {
                if (button != null)
                    button.Hide();
            }
            ability_buttons.Clear();

            // 销毁所有子对象
            if (buttons_container != null)
            {
                for (int i = buttons_container.childCount - 1; i >= 0; i--)
                {
                    Destroy(buttons_container.GetChild(i).gameObject);
                }
            }
        }

        /// <summary>
        /// 显示面板（重写以确保正确显示）
        /// </summary>
        public override void Show(bool instant = false)
        {
            Debug.Log($"[AbilityPanel] Show() 被调用, instant={instant}, display_speed={display_speed}");
            Debug.Log($"[AbilityPanel] Show前: gameObject.activeSelf={gameObject.activeSelf}, canvas_group.alpha={canvas_group.alpha}");
            
            visible = true;
            gameObject.SetActive(true);

            // 强制立即显示
            if (canvas_group != null)
            {
                canvas_group.alpha = 1f;
                canvas_group.interactable = true;
                canvas_group.blocksRaycasts = true;
            }

            // 检查并确保RectTransform设置正确
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                // 确保面板的RectTransform设置正确
                // 如果anchor设置为右下角，sizeDelta应该直接设置大小
                // 如果anchor设置为stretch，需要使用offsetMin和offsetMax
                
                Vector2 anchoredPos = rect.anchoredPosition;
                Vector2 sizeDelta = rect.sizeDelta;
                Vector2 anchorMin = rect.anchorMin;
                Vector2 anchorMax = rect.anchorMax;
                
                Debug.Log($"[AbilityPanel] RectTransform当前设置: anchoredPosition={anchoredPos}, anchorMin={anchorMin}, anchorMax={anchorMax}, sizeDelta={sizeDelta}");
                Debug.Log($"[AbilityPanel] RectTransform offsetMin={rect.offsetMin}, offsetMax={rect.offsetMax}");
                
                // 检查是否是stretch模式（anchorMin和anchorMax不同）
                bool isStretch = (anchorMin.x != anchorMax.x) || (anchorMin.y != anchorMax.y);
                
                if (isStretch)
                {
                    // Stretch模式：使用offsetMin和offsetMax设置大小
                    // 确保面板有正确的高度
                    rect.offsetMin = new Vector2(-300f, 0f); // 左边距，下边距
                    rect.offsetMax = new Vector2(0f, 400f); // 右边距，上边距
                    Debug.Log($"[AbilityPanel] Stretch模式：设置offsetMin={rect.offsetMin}, offsetMax={rect.offsetMax}");
                }
                else
                {
                    // 非Stretch模式：使用sizeDelta设置大小
                    // 确保面板有正确的大小
                    if (sizeDelta.x < 200f || sizeDelta.y < 200f)
                    {
                        rect.sizeDelta = new Vector2(300f, 400f);
                        Debug.Log($"[AbilityPanel] 非Stretch模式：修复sizeDelta={rect.sizeDelta}");
                    }
                }
                
                // 确保buttons_container也有正确的大小
                if (buttons_container != null)
                {
                    RectTransform containerRect = buttons_container;
                    Debug.Log($"[AbilityPanel] buttons_container RectTransform: anchorMin={containerRect.anchorMin}, anchorMax={containerRect.anchorMax}, sizeDelta={containerRect.sizeDelta}");
                    Debug.Log($"[AbilityPanel] buttons_container offsetMin={containerRect.offsetMin}, offsetMax={containerRect.offsetMax}");
                    
                    // 确保buttons_container填充整个面板
                    bool containerStretch = (containerRect.anchorMin.x != containerRect.anchorMax.x) || 
                                           (containerRect.anchorMin.y != containerRect.anchorMax.y);
                    
                    if (containerStretch)
                    {
                        // Stretch模式：填充整个面板，留10像素边距
                        containerRect.offsetMin = new Vector2(10f, 10f);
                        containerRect.offsetMax = new Vector2(-10f, -10f);
                        // 在stretch模式下，sizeDelta应该接近(0, 0)
                        containerRect.sizeDelta = new Vector2(0f, 0f);
                        Debug.Log($"[AbilityPanel] buttons_container设置为Stretch填充，offsetMin={containerRect.offsetMin}, offsetMax={containerRect.offsetMax}, sizeDelta={containerRect.sizeDelta}");
                    }
                    else
                    {
                        // 非Stretch模式：设置大小
                        containerRect.sizeDelta = new Vector2(280f, 380f);
                        Debug.Log($"[AbilityPanel] buttons_container设置sizeDelta={containerRect.sizeDelta}");
                    }
                }
                
                // 如果位置太偏（可能是设置错误），给出警告
                if (Mathf.Abs(anchoredPos.x) > 2000 || Mathf.Abs(anchoredPos.y) > 2000)
                {
                    Debug.LogWarning($"[AbilityPanel] RectTransform位置异常！可能需要检查Unity编辑器中的设置。");
                }
            }

            if (onShow != null)
                onShow.Invoke();
                
            Debug.Log($"[AbilityPanel] Show后: gameObject.activeSelf={gameObject.activeSelf}, canvas_group.alpha={canvas_group.alpha}, visible={visible}");
            Debug.Log($"[AbilityPanel] 面板世界位置: {transform.position}, RectTransform位置: {(rect != null ? rect.anchoredPosition.ToString() : "N/A")}");
            Debug.Log($"[AbilityPanel] 面板在屏幕上的位置: {(rect != null && rect.parent != null ? RectTransformUtility.WorldToScreenPoint(null, rect.position).ToString() : "N/A")}");
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            selected_card = null;
            ClearButtons();
        }

        /// <summary>
        /// 获取当前选中的卡片
        /// </summary>
        public Card GetSelectedCard()
        {
            return selected_card;
        }

        /// <summary>
        /// 延迟刷新布局（确保所有按钮都已创建）
        /// </summary>
        private IEnumerator DelayedLayoutRefresh()
        {
            // 等待一帧，确保所有按钮都已创建
            yield return null;
            
            if (buttons_container != null)
            {
                LayoutGroup layout = buttons_container.GetComponent<LayoutGroup>();
                if (layout != null)
                {
                    // 强制刷新布局
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(buttons_container);
                    Debug.Log($"[AbilityPanel] 延迟刷新布局组件: {layout.GetType().Name}");
                    
                    // 再次检查所有按钮的位置
                    for (int i = 0; i < buttons_container.childCount; i++)
                    {
                        Transform child = buttons_container.GetChild(i);
                        RectTransform childRect = child.GetComponent<RectTransform>();
                        if (childRect != null)
                        {
                            Debug.Log($"[AbilityPanel] 刷新后按钮 {i}: anchoredPosition={childRect.anchoredPosition}, sizeDelta={childRect.sizeDelta}");
                        }
                    }
                }
            }
        }

        public static AbilityPanel Get()
        {
            return instance;
        }
    }
}

