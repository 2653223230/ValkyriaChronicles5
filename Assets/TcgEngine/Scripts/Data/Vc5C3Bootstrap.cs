using UnityEngine;

namespace TcgEngine
{
    public static partial class Vc5DemoBootstrap
    {
        private static void RegisterRangedPressureC3()
        {
            CardData ranger = RegisterHero(Vc5C3Rules.Ranger, "游骑射手", 2, 5, 3, 2,
                "机动火力：移动后，下一张伤害牌伤害 +1。未使用可保留，不叠加。");
            CardData sniper = RegisterHero(Vc5C3Rules.Sniper, "狙击手 C3", 3, 4, 1, 3,
                "稳固射击：本回合未移动时，伤害牌伤害 +1。");
            CardData guard = RegisterHero(Vc5C3Rules.Guard, "火力护卫", 1, 7, 1, 2,
                "行进警戒：每回合首次移动后，对射程内最近敌人造成 1 点伤害。");
            CardData move = BuildC3Card("tactical_move", "战术移动", 1, true,
                "移动至自身移动力范围内的空格。", true);
            CardData march = BuildC3Card("forced_march", "强行军", 2, false,
                "移动至自身移动力 +2 范围内的空格。", true);
            CardData calibration = BuildC3Card("temp_calibration", "临时校准", 1, true,
                "本回合攻击范围 +1，不叠加。");
            CardData scope = BuildC3Card("scope_upgrade", "改装瞄具", 3, false,
                "本局攻击范围永久 +1，可叠加。");
            CardData coverage = BuildC3Card("fire_coverage", "火力覆盖", 3, false,
                "对射程内所有敌人造成自身攻击力的伤害。");
            CardData heavy = BuildC3Card("heavy_break", "重点击破", 2, false,
                "攻击射程内生命最高的敌人，伤害 +1。");
            CardData weak = BuildC3Card("weakpoint_snipe", "弱点狙击", 3, false,
                "在攻击范围 +2 内攻击生命最低的敌人。");
            CardData mobile = BuildC3Card("mobile_shot", "移动射击", 2, false,
                "若可攻击则攻击最近敌人；否则自动移动后攻击。");
            RegisterDeck(BuildDeck(RangedPressureC3DeckId, "VC5 Demo 射击压制 C3",
                new[] { ranger, sniper, guard }, new[]
                {
                    (move, 3), (march, 2), (calibration, 2), (scope, 1),
                    (coverage, 2), (heavy, 3), (weak, 3), (mobile, 4)
                }));
        }

        private static CardData BuildC3Card(string suffix, string title, int cost, bool fast, string text, bool move = false)
        {
            string id = Vc5C3Rules.Prefix + suffix;
            AbilityData play = BaseAbility(id + "_play", title, AbilityTrigger.OnPlay, AbilityTarget.PlayTarget);
            play.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5C3Actor>() };
            if (move)
            {
                play.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectStoreTargetStatsToCaster>() };
                AbilityData destination = BaseAbility(id + "_destination", "选择移动空格", AbilityTrigger.None, AbilityTarget.SelectTarget);
                destination.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5C3Destination>() };
                destination.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectVc5C3Action>() };
                RegisterAbility(destination);
                play.chain_abilities = new[] { destination };
            }
            else
                play.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectVc5C3Action>() };
            RegisterAbility(play);
            return RegisterSpell(id, title, cost, fast, text, play);
        }
    }
}
