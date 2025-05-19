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
            bool yourturn = GameClient.Get().IsYourTurn();

            if (yourturn && selected_card != null)
            {
                Card card = selected_card.GetCard();
                Vector3 wpos = GameBoard.Get().RaycastMouseBoard();
                BSlot tslot = BSlot.GetNearest(wpos);
                Card target = tslot?.GetSlotCard(wpos);
                AbilityButton ability = AbilityButton.GetFocus(wpos, 1f);

                if (ability != null && ability.IsInteractable())
                {
                    GameClient.Get().CastAbility(card, ability.GetAbility());
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
                }
                else if (tslot != null && tslot is BoardSlot)
                {
                    GameClient.Get().Move(card, tslot.GetSlot());//移动

                }
            }
            UnselectAll();
        }

        //取消选择全部
        public void UnselectAll()
        {
            selected_card = null;
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