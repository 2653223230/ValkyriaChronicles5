using TcgEngine;
using UnityEngine;
using System.Collections.Generic;

namespace TcgEngine.Client
{
    /// <summary>
    /// 棋盘状态条与大卡预览中状态文案的统一格式（黏液层数 / 禁锢剩余回合 / 磨刀每次结算加伤等）。
    /// </summary>
    public static class Vc5StatusDisplay
    {
        public static string FormatAll(Card card)
        {
            if (card == null)
                return "";

            var parts = new List<string>();
            if (card.r4_shield > 0)
                parts.Add("护盾 " + card.r4_shield + "（" + (card.r4_shield_rounds > 1 ? "下" : "本") + "回合末）");
            if (card.r4_watch)
                parts.Add("警戒 1（至回合结束；移动、攻击或主动技能解除）");
            foreach (CardStatus status in card.GetAllStatus())
            {
                string text = FormatSingle(status);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }
            return string.Join("，", parts.ToArray());
        }

        public static string FormatSingle(CardStatus astatus)
        {
            if (astatus == null)
                return "";

            switch (astatus.type)
            {
                case StatusType.Vc5C3PermanentRange: return "改装：射程 +" + astatus.value;
                case StatusType.Vc5C3TemporaryRange: return "校准：射程 +1（本回合）";
                case StatusType.Vc5C3MobileFire: return "机动火力：下一张伤害 +1";
                case StatusType.Vc5C3MovedThisTurn: return "已移动：稳固射击失效";
                case StatusType.Vc5C3GuardMoveUsed: return "行进警戒：本回合已用";
            }

            StatusData istats = StatusData.Get(astatus.type);
            if (istats == null || string.IsNullOrEmpty(istats.title))
                return "";

            switch (astatus.type)
            {
                case StatusType.Slime:
                case StatusType.Sharp:
                    {
                        int stacks = Mathf.Max(1, astatus.value);
                        return istats.GetTitle() + " " + stacks + "层";
                    }
                case StatusType.Vc5DealDamageBonus:
                    return istats.GetTitle() + " +" + Mathf.Max(0, astatus.value) + "(每次结算)";
                case StatusType.Rooted:
                case StatusType.Paralysed:
                case StatusType.Sleep:
                    if (astatus.permanent)
                        return istats.GetTitle();
                    return istats.GetTitle() + " 剩余" + Mathf.Max(0, astatus.duration) + "回合";
                default:
                    {
                        int ival = astatus.value;
                        string suffix = "";
                        if (ival > 1)
                            suffix = " " + ival;

                        string durTxt = "";
                        if (!astatus.permanent && astatus.duration > 0
                            && astatus.type != StatusType.Slime
                            && astatus.type != StatusType.Sharp
                            && astatus.type != StatusType.Vc5DealDamageBonus)
                        {
                            durTxt = " 剩余" + astatus.duration + "回合";
                        }

                        return istats.GetTitle() + suffix + durTxt;
                    }
            }
        }
    }
}
