using UnityEngine;

namespace TcgEngine
{
    public static partial class Vc5DemoBootstrap
    {
        public const string SteadyAssaultDeckId = "deck_vc5_demo_steady_assault";
        static void RegisterSteadyAssault()
        {
            CardData front = RegisterHero(Vc5BAI1Rules.Frontliner, "重装前锋", 2, 8, 2, 1, "每回合首次移动后，获得 1 护盾，持续至下一回合结束。");
            CardData flank = RegisterHero(Vc5BAI1Rules.Flanker, "机动突击手", 2, 6, 3, 1, "移动后，下一张伤害牌伤害 +1，不叠加。");
            CardData rifle = RegisterHero(Vc5BAI1Rules.Rifleman, "支援步枪手", 2, 5, 2, 2, "攻击与其他友军相邻的敌人时，伤害 +1。");
            CardData step = BuildBAI1Card("short_step", "短步调整", 1, true, "一名友军移动最多 1 格。", true, false);
            CardData march = BuildBAI1Card("march", "稳步行军", 2, false, "一名友军按自身移动力移动。", true, false);
            CardData attack = BuildBAI1Card("basic_attack", "普通攻击", 1, false, "一名友军攻击射程内一名敌人，造成自身攻击力伤害。", false, true);
            CardData charge = BuildBAI1Card("short_charge", "短距突击", 2, false, "一名友军移动最多 1 格，然后攻击射程内一名敌人。可原地攻击。", true, true);
            CardData advance = BuildBAI1Card("cover_advance", "掩护推进", 2, false, "一名友军移动最多 2 格，获得 1 护盾，持续至下一回合结束。", true, false);
            CardData shoot = BuildBAI1Card("suppression", "压制射击", 2, false, "一名友军攻击射程内一名敌人，伤害为攻击力 +1。", false, true);
            CardData retreat = BuildBAI1Card("regroup", "整队回撤", 1, true, "一名友军向任意方向移动最多 1 格，获得 1 护盾，持续至下一回合结束。", true, false);
            CardData guard = BuildBAI1Card("guard", "集中防护", 1, false, "一名友军获得 2 护盾，持续至下一回合结束。", false, false);
            RegisterDeck(BuildDeck(SteadyAssaultDeckId, "VC5 Demo 稳步推进战团 B-AI1", new[] { front, flank, rifle },
                new[] { (step, 3), (march, 3), (attack, 3), (charge, 3), (advance, 2), (shoot, 2), (retreat, 2), (guard, 2) }));
        }

        static CardData BuildBAI1Card(string suffix, string title, int cost, bool fast, string text, bool move, bool attack)
        {
            string id = Vc5BAI1Rules.Prefix + suffix;
            AbilityData play = BaseAbility(id + "_play", title, AbilityTrigger.OnPlay, AbilityTarget.PlayTarget);
            play.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5C3Actor>() };
            AbilityData tail = play;
            if (move || attack) play.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectStoreTargetStatsToCaster>() };
            else play.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectVc5BAI1Action>() };
            if (move)
            {
                AbilityData destination = BaseAbility(id + "_destination", attack ? "选择落点（可原地），再选敌人" : "选择移动落点", AbilityTrigger.None, AbilityTarget.SelectTarget);
                destination.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5BAI1Selection>() };
                destination.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectVc5BAI1Action>() };
                tail.chain_abilities = new[] { destination };
                tail = destination;
            }
            if (attack)
            {
                AbilityData target = BaseAbility(id + "_target", "选择敌人，确认攻击", AbilityTrigger.None, AbilityTarget.SelectTarget);
                target.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionVc5BAI1Selection>() };
                target.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectVc5BAI1Action>() };
                tail.chain_abilities = new[] { target };
                RegisterAbility(target);
            }
            if (tail != play) RegisterAbility(tail);
            RegisterAbility(play);
            return RegisterSpell(id, title, cost, fast, text, play);
        }
    }
}
