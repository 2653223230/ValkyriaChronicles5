using UnityEngine;

namespace TcgEngine
{
    public static partial class Vc5DemoBootstrap
    {
        public const string CommandR4DeckId = "deck_vc5_demo_command_r4";

        private static void RegisterCommandR4()
        {
            RegisterAbility(BaseAbility(Vc5R4Rules.WatchReaction, "警戒射击", AbilityTrigger.None, AbilityTarget.None));
            CardData ranger = RegisterHero(Vc5R4Rules.Ranger, "游骑射手 R4", 2, 5, 3, 2,
                "机动火力：移动后，下张伤害牌 +1，不叠加。\n侧翼机动：1 法力，快速移动 1 格，每回合一次。");
            CardData sniper = RegisterHero(Vc5R4Rules.Sniper, "阵地狙击手 R4", 3, 4, 1, 3,
                "稳固射击：本回合未移动时，伤害牌伤害 +1。");
            CardData commander = RegisterHero(Vc5R4Rules.Commander, "战场指挥官 R4", 1, 6, 2, 2,
                "战术筹划：每回合一次，弃 1 张非临时手牌，三选一获得临时指令：\n"
                + "推进（1费）：其他友军移动最多 2 格\n"
                + "开火（1费）：其他友军攻击，基础伤害为自身攻击力\n"
                + "掩护（0费）：其他友军获得 2 护盾\n"
                + "*指令仅本回合可用，只能用于射程内其他友军。");
            AbilityData moveSkill = BaseAbility(Vc5R4Rules.MoveSkill, "侧翼机动", AbilityTrigger.Activate, AbilityTarget.SelectTarget);
            moveSkill.desc = "快速移动 1 格，每回合一次；取消不扣费。";
            moveSkill.mana_cost = 1;
            moveSkill.fast_action = true;
            moveSkill.uses_per_turn = 1;
            moveSkill.conditions_trigger = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5R4SkillReady>() };
            moveSkill.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5R4Skill>() };
            RegisterAbility(moveSkill);
            ranger.abilities = new[] { moveSkill };
            AbilityData prepare = BaseAbility(Vc5R4Rules.PrepareSkill, "战术筹划：选择要弃的非临时手牌", AbilityTrigger.Activate, AbilityTarget.CardSelector);
            prepare.fast_action = true;
            prepare.desc = "0 法力，快速，每回合一次。先选弃牌，再选指令；取消不消耗。";
            prepare.uses_per_turn = 1;
            prepare.conditions_trigger = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5R4SkillReady>() };
            prepare.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5R4Skill>() };
            RegisterAbility(prepare);
            commander.abilities = new[] { prepare };
            AbilityData choices = BaseAbility(Vc5R4Rules.ChooseOrder, "选择临时指令（确认后弃牌）", AbilityTrigger.None, AbilityTarget.ChoiceSelector);
            choices.chain_abilities = new AbilityData[3];
            string[] labels = { "推进指令", "开火指令", "掩护指令" };
            string[] instructions = { "1 法力 · 快速\n移动最多 2 格", "1 法力 · 快速\n基础伤害为自身攻击力", "0 法力 · 快速\n获得 2 护盾" };
            for (int i = 0; i < labels.Length; i++)
            {
                choices.chain_abilities[i] = BaseAbility(Vc5R4Rules.ChooseOrder + i, labels[i], AbilityTrigger.None, AbilityTarget.None);
                choices.chain_abilities[i].desc = instructions[i];
                RegisterAbility(choices.chain_abilities[i]);
            }
            RegisterAbility(choices);
            CardData move = BuildR4Card("tactical_move", "战术移动", 1, true, "移动至自身移动力范围内的空格。", true);
            CardData march = BuildR4Card("forced_march", "强行军", 2, false, "移动至自身移动力 +2 范围内的空格。", true);
            CardData calibration = BuildR4Card("temp_calibration", "临时校准", 1, true, "本回合射程 +1，不叠加。");
            CardData scope = BuildR4Card("scope_upgrade", "改装瞄具", 3, false, "本局射程永久 +1，可叠加。");
            CardData cover = BuildR4Card("cover_deploy", "掩护部署", 2, false, "一名友军移动最多 2 格，获得 1 护盾，至下回合结束。", true);
            CardData watch = BuildR4Card("watch_deploy", "警戒部署", 2, false, "选择一名友军移动最多 2 格，然后进入警戒。若射程内已有敌人，立即对最近敌人造成 1 点伤害；否则，本回合下一名移动后位于其射程内的敌人受到 1 点伤害。触发后解除；警戒者移动、攻击或使用主动技能也会解除。校准、改装和获得护盾不会解除警戒。", true);
            watch.desc = watch.text;
            watch.text = "友军移动最多 2 格，然后警戒。已有敌人时立即造成 1 点伤害；否则，下一名移动后位于射程内的敌人受到 1 点伤害。触发或警戒者主动行动后解除。";
            CardData coverage = BuildR4Card("fire_coverage", "火力覆盖", 3, false, "对射程内所有敌人造成自身攻击力的伤害。");
            CardData heavy = BuildR4Card("heavy_break", "重点击破", 2, false, "攻击射程内生命最高的敌人，伤害 +1。");
            CardData weak = BuildR4Card("weakpoint_snipe", "弱点狙击", 3, false, "在射程 +2 内攻击生命最低的敌人。");
            CardData mobile = BuildR4Card("mobile_shot", "移动射击", 2, false, "若可攻击则攻击最近敌人；否则自动移动后攻击。");
            CardData advance = BuildR4Card("advance_order", "推进指令", 1, true, "指挥范围内其他友军移动最多 2 格。", true);
            CardData fire = BuildR4Card("fire_order", "开火指令", 1, true, "指挥范围内另一名友军攻击其自身射程内最近敌人，基础伤害为自身攻击力。");
            CardData shield = BuildR4Card("cover_order", "掩护指令", 0, true, "指挥范围内其他友军获得 2 护盾，至下回合结束。");
            foreach (CardData order in new[] { advance, fire, shield })
            {
                order.deckbuilding = false;
                order.text = "临时·本回合\n" + order.text;
                order.desc = order.text;
            }
            RegisterDeck(BuildDeck(CommandR4DeckId, "VC5 Demo 机动支援与阵地狙击 R4",
                new[] { ranger, sniper, commander }, new[] { (move, 2), (march, 2), (calibration, 2), (scope, 2),
                    (cover, 1), (watch, 2), (coverage, 2), (heavy, 2), (weak, 2), (mobile, 3) }));
        }

        private static CardData BuildR4Card(string suffix, string title, int cost, bool fast, string text, bool move = false)
        {
            return BuildC3Card(suffix, title, cost, fast, text, move, Vc5R4Rules.Prefix);
        }
    }
}
