using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Script that contain main controls for clicking on cards, attacking, activating abilities
    /// Holds the currently selected card and will send action to GameClient on click release
    /// 包含点击卡片，攻击，激活技能的主控件的脚本
    /// 保存当前选中的卡片，并在点击释放时向GameClient发送动作
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        private BoardCard selected_card = null;
        private bool selection_just_set = false;

        private static PlayerControls instance;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            if (Input.GetMouseButtonDown(1))
                UnselectAll();

            if (selected_card != null)
            {
                if (Input.GetMouseButtonUp(0))
                    ReleaseClick();
            }
        }

        public void SelectCard(BoardCard bcard)
        {
            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card card = bcard.GetFocusCard();

            if (gdata.IsPlayerSelectorTurn(player) && gdata.selector == SelectorType.SelectTarget)
            {
                //Target selector, select this card
                //目标选择器，选择这张卡
                GameClient.Get().SelectCard(card);
            }
            else if (gdata.IsPlayerActionTurn(player) && card.player_id == player.player_id)
            {
                //Start dragging card
                //开始拖拽卡片
                selected_card = bcard;
                selection_just_set = true;

                // 显示技能按钮面板（右下角）
                AbilityPanel ability_panel = AbilityPanel.Get();
                if (ability_panel != null)
                {
                    ability_panel.ShowAbilities(card);
                }
            }
        }

        public void SelectCardRight(BoardCard card)
        {
            if (!Input.GetMouseButton(0))
            {
                //Nothing on right-click
            }
        }

        //发布点击
        private void ReleaseClick()
        {
            if (selected_card == null)
                return;

            // 如果点击在技能按钮面板上，则不取消选择，允许点击按钮
            AbilityPanel ability_panel = AbilityPanel.Get();
            if (ability_panel != null && ability_panel.IsVisible())
            {
                RectTransform panel_rect = ability_panel.GetComponent<RectTransform>();
                Canvas canvas = panel_rect != null ? panel_rect.GetComponentInParent<Canvas>() : null;
                if (canvas != null && GameUI.IsOverRectTransform(canvas, panel_rect))
                {
                    return;
                }
            }

            // 如果点击在其他UI上，也不取消选择
            if (GameUI.IsOverUI())
                return;

            // 如果这是选中后的第一次抬起鼠标，仅保持选中状态，不执行行动
            if (selection_just_set)
            {
                selection_just_set = false;
                return;
            }

            bool yourturn = GameClient.Get().IsYourTurn();
            bool action_performed = false;

            if (selected_card != null)
            {
                Card card = selected_card.GetCard();
                Vector3 wpos = GameBoard.Get().RaycastMouseBoard();
                BSlot tslot = BSlot.GetNearest(wpos);
                Card target = tslot?.GetSlotCard(wpos);
                AbilityButton ability = AbilityButton.GetFocus(wpos, 1f);
                Slot destination_slot = Slot.None;

                if (tslot is BoardSlot boardSlot)
                    destination_slot = boardSlot.GetSlot(wpos);

                if (yourturn)
                {
                    if (ability != null && ability.IsInteractable())
                    {
                        GameClient.Get().CastAbility(card, ability.GetAbility());
                        action_performed = true;
                    }
                    // else if (tslot is BoardSlotPlayer)
                    // {
                    //     if (card.exhausted)
                    //         WarningText.ShowExhausted();//卡牌处于“疲惫”状态（exhausted）时显示警告
                    //     else
                    //         GameClient.Get().AttackPlayer(card, tslot.GetPlayer());//攻击该槽位对应的玩家
                    // }
                    else if (target != null && target.uid != card.uid && target.player_id != card.player_id)//如果点击的是敌方卡牌（非自身且不属于当前玩家）
                    {
                        if(card.exhausted)
                            WarningText.ShowExhausted();
                        else
                            GameClient.Get().AttackTarget(card, target);//攻击敌方卡牌
                        action_performed = true;
                    }
                    else if (tslot != null && tslot is BoardSlot && destination_slot != Slot.None && destination_slot != card.slot)
                    {
                        Game gdata = GameClient.Get().GetGameData();
                        if (gdata != null && gdata.CanMoveCard(card, destination_slot))
                        {
                            GameClient.Get().Move(card, destination_slot);//移动
                            action_performed = true;
                        }
                    }
                }

                if (!action_performed && !GameUI.IsOverUI() && tslot == null)
                {
                    // 点击到棋盘外的空白区域时，退出选中
                    UnselectAll();
                }
            }
        }

        //取消选择全部
        public void UnselectAll()
        {
            selected_card = null;
            selection_just_set = false;

            // 隐藏技能按钮面板
            AbilityPanel ability_panel = AbilityPanel.Get();
            if (ability_panel != null)
            {
                ability_panel.Hide();
            }
        }

        public BoardCard GetSelected()
        {
            return selected_card;
        }

        public static PlayerControls Get()
        {
            return instance;
        }
    }
}