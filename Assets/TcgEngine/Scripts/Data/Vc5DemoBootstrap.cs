using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    public static class Vc5DemoBootstrap
    {
        public const string MobileAssaultDeckId = "deck_vc5_demo_mobile_assault";
        public const string RangedPressureDeckId = "deck_vc5_demo_ranged_pressure";

        private static readonly object registerLock = new object();
        private static bool registered;
        private static TeamData team;
        private static RarityData rarity;

        public static void Register()
        {
            lock (registerLock)
            {
                team = TeamData.GetAll().Count > 0 ? TeamData.GetAll()[0] : null;
                rarity = RarityData.GetAll().Count > 0 ? RarityData.GetAll()[0] : null;

                RegisterMobileAssault();
                RegisterRangedPressure();
                registered = true;
            }
        }

        private static void RegisterMobileAssault()
        {
            CardData cavalry = RegisterHero("vc5_demo_cavalry", "骑兵", 2, 5, 3, 1,
                "本回合移动2格以上后，攻击伤害+1。（Demo版暂以移动攻击牌的额外伤害体现）");
            CardData assassin = RegisterHero("vc5_demo_assassin", "刺客", 3, 4, 2, 1,
                "攻击生命低于一半的敌人时，伤害+1。（Demo版保留为角色说明）");
            CardData scout = RegisterHero("vc5_demo_scout", "斥候", 1, 5, 3, 2,
                "快速技能：移动1格，每回合1次。");
            scout.abilities = new AbilityData[] { BuildScoutQuickMove() };

            CardData quickMove = BuildMoveSlotCard("vc5_demo_quick_move", "快速移动", 1, true, 1,
                "选择己方角色，移动到1格内空格。快速行动。");
            CardData raid = BuildAutoMoveCard("vc5_demo_raid", "奔袭", 2, false, true, 1,
                "选择己方角色，自动向争夺区移动，最多移动其当前移动力格数。");
            CardData closeAssault = BuildMoveAdjacentCard("vc5_demo_close_assault", "贴身攻击", 2, false, -1, false, false, 0,
                "选择己方角色，再选择敌人；该角色移动到敌人身边。");
            CardData basicAttack = BuildAttackCard("vc5_demo_basic_attack", "普通攻击", 1, false, 0, 0, false,
                "选择己方角色，按自身攻击力攻击范围内敌人。");
            CardData pursuit = BuildMoveAdjacentCard("vc5_demo_pursuit", "追击", 2, false, -1, true, false, 0,
                "选择己方角色，再选择受伤敌人；该角色移动到敌人身边。");
            CardData retreat = BuildAutoAwayCard("vc5_demo_retreat_2", "回撤", 1, true, 2,
                "选择己方角色，自动远离最近敌人最多2格。快速行动。");
            CardData chargeAttack = BuildMoveAdjacentCard("vc5_demo_charge_attack", "冲锋攻击", 3, false, 2, false, true, 0,
                "选择己方角色，再选择敌人；该角色最多移动2格到敌人身边并攻击。");
            CardData fullSpeed = BuildAllMoveCard("vc5_demo_full_speed_advance", "全速突入", 3, true,
                "所有己方角色各自动向争夺区移动1格。快速行动。");
            CardData decisive = BuildMoveAdjacentCard("vc5_demo_decisive_charge", "决胜突击", 4, false, -1, false, false, 1,
                "选择己方角色，再选择敌人；该角色移动到敌人身边，并造成自身攻击力+1伤害。");

            RegisterDeck(BuildDeck(
                MobileAssaultDeckId,
                "VC5 Demo 机动突击",
                new[] { cavalry, assassin, scout },
                new[]
                {
                    (quickMove, 3), (raid, 3), (closeAssault, 3), (basicAttack, 3), (pursuit, 2),
                    (retreat, 2), (chargeAttack, 2), (fullSpeed, 1), (decisive, 1)
                }));
        }

        private static void RegisterRangedPressure()
        {
            CardData sniper = RegisterHero("vc5_demo_sniper", "狙击手", 3, 4, 1, 3,
                "若本回合没有移动，攻击伤害+1。（Demo版保留为角色说明）");
            CardData rifleman = RegisterHero("vc5_demo_rifleman", "步枪兵", 2, 5, 2, 2, "无被动。");
            CardData guard = RegisterHero("vc5_demo_guard", "护卫", 1, 7, 1, 1,
                "相邻友方受到伤害-1。（Demo版保留为角色说明）");

            CardData shoot = BuildAttackCard("vc5_demo_shoot", "射击", 1, false, 0, 0, false,
                "选择己方角色，对攻击范围内敌人造成自身攻击力伤害。");
            CardData aimedShot = BuildAttackCard("vc5_demo_aimed_shot", "瞄准射击", 2, false, 1, 0, false,
                "选择己方角色，本次攻击范围+1。");
            CardData fallback = BuildAutoAwayCard("vc5_demo_fallback", "后撤", 1, true, 1,
                "选择己方角色，自动远离最近敌人1格。快速行动。");
            CardData highGround = BuildHighGroundCard();
            CardData focusFire = BuildAttackCard("vc5_demo_focus_fire", "集火", 2, false, 0, 0, true,
                "选择己方角色攻击范围内敌人；若另有友方也能攻击该敌人，伤害+1。");
            CardData lineAdvance = BuildLineAdvanceCard();
            CardData suppression = BuildAttackCard("vc5_demo_ranged_suppression", "远程压制", 2, false, 0, 1, false,
                "选择己方角色，对攻击范围内敌人造成自身攻击力+1伤害。");
            CardData protectShooter = BuildStatusCard("vc5_demo_protect_shooter", "保护射手", 1, false, StatusType.Armor, 2, 1,
                "选择己方角色，获得2点护盾，持续1回合。");
            CardData volley = BuildVolleyCard();

            RegisterDeck(BuildDeck(
                RangedPressureDeckId,
                "VC5 Demo 射击压制",
                new[] { sniper, rifleman, guard },
                new[]
                {
                    (shoot, 3), (aimedShot, 3), (fallback, 3), (highGround, 2), (focusFire, 2),
                    (lineAdvance, 2), (suppression, 2), (protectShooter, 2), (volley, 1)
                }));
        }

        private static CardData RegisterHero(string id, string title, int attack, int hp, int move, int range, string text)
        {
            CardData card = BaseCard(id, title, CardType.Character, 0, attack, hp, move, range);
            card.text = text;
            card.desc = $"攻击{attack} / 生命{hp} / 移动{move} / 攻击范围{range}";
            return RegisterCard(card);
        }

        private static AbilityData BuildScoutQuickMove()
        {
            AbilityData move = BaseAbility("vc5_demo_scout_quick_move", "斥候快移", AbilityTrigger.Activate, AbilityTarget.SelectTarget);
            move.mana_cost = 0;
            move.fast_action = true;
            move.uses_per_turn = 1;
            move.conditions_target = new ConditionData[] { MoveSlotCondition(1, false) };
            move.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectMoveTriggererToSlot>() };
            RegisterAbility(move);
            return move;
        }

        private static CardData BuildMoveSlotCard(string id, string title, int cost, bool fast, int range, string text)
        {
            AbilityData pick = PickAllyAbility(id + "_pick");
            AbilityData move = BaseAbility(id + "_move", title, AbilityTrigger.None, AbilityTarget.SelectTarget);
            move.conditions_target = new ConditionData[] { MoveSlotCondition(range, false) };
            move.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectMoveTriggererToSlot>() };
            RegisterAbility(move);
            pick.chain_abilities = new[] { move };
            RegisterAbility(pick);

            return RegisterSpell(id, title, cost, fast, text, pick);
        }

        private static CardData BuildAutoMoveCard(string id, string title, int cost, bool fast, bool useMoveRange, int fixedSteps, string text)
        {
            AbilityData pick = PickAllyAbility(id + "_pick");
            EffectVc5MoveTargetTowardScoring effect = ScriptableObject.CreateInstance<EffectVc5MoveTargetTowardScoring>();
            effect.use_move_range = useMoveRange;
            effect.fixed_steps = fixedSteps;
            pick.effects = AppendEffect(pick.effects, effect);
            RegisterAbility(pick);
            return RegisterSpell(id, title, cost, fast, text, pick);
        }

        private static CardData BuildAutoAwayCard(string id, string title, int cost, bool fast, int steps, string text)
        {
            AbilityData pick = PickAllyAbility(id + "_pick");
            EffectVc5MoveTargetAwayFromEnemies effect = ScriptableObject.CreateInstance<EffectVc5MoveTargetAwayFromEnemies>();
            effect.steps = steps;
            pick.effects = AppendEffect(pick.effects, effect);
            RegisterAbility(pick);
            return RegisterSpell(id, title, cost, fast, text, pick);
        }

        private static CardData BuildMoveAdjacentCard(string id, string title, int cost, bool fast, int maxMove,
            bool damagedOnly, bool attackAfterMove, int bonusDamage, string text)
        {
            AbilityData pick = PickAllyAbility(id + "_pick");
            AbilityData target = BaseAbility(id + "_target", title, AbilityTrigger.None, AbilityTarget.SelectTarget);
            List<ConditionData> conds = new List<ConditionData>(EnemyCardConds())
            {
                CanMoveAdjacentCondition(maxMove)
            };
            if (damagedOnly)
                conds.Add(ScriptableObject.CreateInstance<ConditionVc5TargetDamaged>());
            target.conditions_target = conds.ToArray();

            EffectVc5MoveTriggererAdjacentToTarget move = ScriptableObject.CreateInstance<EffectVc5MoveTriggererAdjacentToTarget>();
            move.max_move = maxMove;

            if (bonusDamage > 0)
            {
                EffectVc5DamageFromTriggererAttack damage = ScriptableObject.CreateInstance<EffectVc5DamageFromTriggererAttack>();
                damage.require_range = false;
                target.value = bonusDamage;
                target.effects = new EffectData[] { move, damage };
            }
            else if (attackAfterMove)
            {
                EffectVc5DamageFromTriggererAttack damage = ScriptableObject.CreateInstance<EffectVc5DamageFromTriggererAttack>();
                damage.require_range = false;
                target.effects = new EffectData[] { move, damage };
            }
            else
            {
                target.effects = new EffectData[] { move };
            }

            RegisterAbility(target);
            pick.chain_abilities = new[] { target };
            RegisterAbility(pick);

            return RegisterSpell(id, title, cost, fast, string.IsNullOrEmpty(text) ? GetMoveAdjacentText(title, bonusDamage, damagedOnly) : text, pick);
        }

        private static CardData BuildAttackCard(string id, string title, int cost, bool fast, int rangeOffset, int bonusDamage, bool supportBonus, string text)
        {
            AbilityData pick = PickAllyAbility(id + "_pick");
            AbilityData attack = BaseAbility(id + "_attack", title, AbilityTrigger.None, AbilityTarget.SelectTarget);
            attack.conditions_target = new ConditionData[] { EnemyCardConds()[0], EnemyCardConds()[1], AttackRangeCondition(rangeOffset) };
            attack.value = bonusDamage;

            EffectVc5DamageFromTriggererAttack damage = ScriptableObject.CreateInstance<EffectVc5DamageFromTriggererAttack>();
            damage.require_range = rangeOffset == 0;
            damage.add_support_bonus = supportBonus;
            attack.effects = new EffectData[] { damage };

            RegisterAbility(attack);
            pick.chain_abilities = new[] { attack };
            RegisterAbility(pick);

            return RegisterSpell(id, title, cost, fast, text, pick);
        }

        private static CardData BuildHighGroundCard()
        {
            AbilityData pick = PickAllyAbility("vc5_demo_high_ground_pick");
            AbilityData move = BaseAbility("vc5_demo_high_ground_move", "占据高位", AbilityTrigger.None, AbilityTarget.SelectTarget);
            move.conditions_target = new ConditionData[] { MoveSlotCondition(1, false) };
            move.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectMoveTriggererToSlot>() };

            AbilityData buff = BaseAbility("vc5_demo_high_ground_range", "射程+1", AbilityTrigger.None, AbilityTarget.AbilityTriggerer);
            EffectVc5AddStatusToTarget effect = ScriptableObject.CreateInstance<EffectVc5AddStatusToTarget>();
            effect.status_type = StatusType.Vc5AttackRangeBonus;
            buff.value = 1;
            buff.duration = 1;
            buff.effects = new EffectData[] { effect };
            RegisterAbility(buff);

            move.chain_abilities = new[] { buff };
            RegisterAbility(move);
            pick.chain_abilities = new[] { move };
            RegisterAbility(pick);

            return RegisterSpell("vc5_demo_high_ground", "占据高位", 1, false,
                "选择己方角色，移动到1格内空格；其下一回合内攻击范围+1。", pick);
        }

        private static CardData BuildLineAdvanceCard()
        {
            AbilityData pick = PickAllyAbility("vc5_demo_line_advance_pick");
            AbilityData move = BaseAbility("vc5_demo_line_advance_move", "防线推进", AbilityTrigger.None, AbilityTarget.SelectTarget);
            move.conditions_target = new ConditionData[] { MoveSlotCondition(1, false) };
            move.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectMoveTriggererToSlot>() };

            AbilityData armor = BuildTargetStatusAbility("vc5_demo_line_advance_armor", "获得护盾", StatusType.Armor, 1, 1);
            move.chain_abilities = new[] { armor };
            RegisterAbility(move);
            pick.chain_abilities = new[] { move };
            RegisterAbility(pick);

            return RegisterSpell("vc5_demo_line_advance", "防线推进", 2, false,
                "选择己方角色，移动到1格内空格并获得1点护盾，持续1回合。", pick);
        }

        private static CardData BuildStatusCard(string id, string title, int cost, bool fast, StatusType status, int value, int duration, string text)
        {
            AbilityData pick = PickAllyAbility(id + "_pick");
            AbilityData addStatus = BuildTargetStatusAbility(id + "_status", title, status, value, duration);
            pick.chain_abilities = new[] { addStatus };
            RegisterAbility(pick);
            return RegisterSpell(id, title, cost, fast, text, pick);
        }

        private static AbilityData BuildTargetStatusAbility(string id, string title, StatusType status, int value, int duration)
        {
            AbilityData ability = BaseAbility(id, title, AbilityTrigger.None, AbilityTarget.AbilityTriggerer);
            EffectVc5AddStatusToTarget effect = ScriptableObject.CreateInstance<EffectVc5AddStatusToTarget>();
            effect.status_type = status;
            ability.value = value;
            ability.duration = duration;
            ability.effects = new EffectData[] { effect };
            RegisterAbility(ability);
            return ability;
        }

        private static CardData BuildAllMoveCard(string id, string title, int cost, bool fast, string text)
        {
            AbilityData ability = BaseAbility(id + "_all", title, AbilityTrigger.OnPlay, AbilityTarget.None);
            EffectVc5MoveAllAlliesTowardScoring effect = ScriptableObject.CreateInstance<EffectVc5MoveAllAlliesTowardScoring>();
            effect.steps = 1;
            ability.effects = new EffectData[] { effect };
            RegisterAbility(ability);
            return RegisterSpell(id, title, cost, fast, text, ability);
        }

        private static CardData BuildVolleyCard()
        {
            AbilityData ability = BaseAbility("vc5_demo_volley_attack", "齐射", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            ability.conditions_target = EnemyCardConds();
            ability.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectVc5AllAlliesDamageTarget>() };
            RegisterAbility(ability);
            return RegisterSpell("vc5_demo_volley", "齐射", 4, false,
                "选择一个敌人；所有当前能攻击该敌人的己方角色各造成一次自身攻击力伤害。", ability);
        }

        private static AbilityData PickAllyAbility(string id)
        {
            AbilityData pick = BaseAbility(id, "选择己方角色", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            pick.conditions_target = AllyCardConds();
            pick.effects = new EffectData[] { ScriptableObject.CreateInstance<EffectStoreTargetStatsToCaster>() };
            return pick;
        }

        private static CardData RegisterSpell(string id, string title, int cost, bool fast, string text, AbilityData ability)
        {
            CardData card = BaseCard(id, title, CardType.Spell, cost, 0, 0, 0, 0);
            card.fast_action = fast;
            card.text = text;
            card.desc = text;
            card.abilities = new AbilityData[] { ability };
            return RegisterCard(card);
        }

        private static string GetMoveAdjacentText(string title, int bonusDamage, bool damagedOnly)
        {
            if (bonusDamage > 0)
                return "选择己方角色，再选择敌人；该角色移动到敌人身边，并造成自身攻击力+1伤害。";
            if (title.Contains("攻击"))
                return "选择己方角色，再选择敌人；该角色移动到敌人身边并攻击。";
            return damagedOnly
                ? "选择己方角色，再选择受伤敌人；该角色移动到敌人身边。"
                : "选择己方角色，再选择敌人；该角色移动到敌人身边。";
        }

        private static ConditionData MoveSlotCondition(int range, bool useMoveRange)
        {
            ConditionVc5SlotMoveFromTriggerer cond = ScriptableObject.CreateInstance<ConditionVc5SlotMoveFromTriggerer>();
            cond.fixed_range = range;
            cond.use_triggerer_move_range = useMoveRange;
            cond.require_empty = true;
            return cond;
        }

        private static ConditionData AttackRangeCondition(int offset)
        {
            ConditionSlotDistFromTriggerer cond = ScriptableObject.CreateInstance<ConditionSlotDistFromTriggerer>();
            cond.range_offset = offset;
            return cond;
        }

        private static ConditionData CanMoveAdjacentCondition(int maxMove)
        {
            ConditionVc5CanMoveTriggererAdjacent cond = ScriptableObject.CreateInstance<ConditionVc5CanMoveTriggererAdjacent>();
            cond.max_move = maxMove;
            return cond;
        }

        private static ConditionData[] AllyCardConds()
        {
            ConditionTarget cType = ScriptableObject.CreateInstance<ConditionTarget>();
            cType.type = ConditionTargetType.Card;
            cType.oper = ConditionOperatorBool.IsTrue;

            ConditionOwner cOwner = ScriptableObject.CreateInstance<ConditionOwner>();
            cOwner.oper = ConditionOperatorBool.IsTrue;
            return new ConditionData[] { cType, cOwner };
        }

        private static ConditionData[] EnemyCardConds()
        {
            ConditionTarget cType = ScriptableObject.CreateInstance<ConditionTarget>();
            cType.type = ConditionTargetType.Card;
            cType.oper = ConditionOperatorBool.IsTrue;

            ConditionOwner cOwner = ScriptableObject.CreateInstance<ConditionOwner>();
            cOwner.oper = ConditionOperatorBool.IsFalse;
            return new ConditionData[] { cType, cOwner };
        }

        private static EffectData[] AppendEffect(EffectData[] source, EffectData effect)
        {
            List<EffectData> effects = new List<EffectData>(source ?? new EffectData[0]);
            effects.Add(effect);
            return effects.ToArray();
        }

        private static CardData BaseCard(string id, string title, CardType type, int mana, int attack, int hp, int move, int range)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.id = id;
            card.title = title;
            card.type = type;
            card.team = team;
            card.rarity = rarity;
            card.mana = mana;
            card.attack = attack;
            card.hp = hp;
            card.move_Range = move;
            card.attack_Range = range;
            card.abilities = new AbilityData[0];
            card.traits = new TraitData[0];
            card.stats = new TraitStat[0];
            card.fields = new TraitData[0];
            card.packs = new PackData[0];
            card.series = "VC5 Demo";
            card.deckbuilding = true;
            return card;
        }

        private static AbilityData BaseAbility(string id, string title, AbilityTrigger trigger, AbilityTarget target)
        {
            AbilityData ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.id = id;
            ability.title = title;
            ability.trigger = trigger;
            ability.target = target;
            ability.conditions_trigger = new ConditionData[0];
            ability.conditions_target = new ConditionData[0];
            ability.filters_target = new FilterData[0];
            ability.effects = new EffectData[0];
            ability.status = new StatusData[0];
            ability.chain_abilities = new AbilityData[0];
            return ability;
        }

        private static CardData RegisterCard(CardData card)
        {
            CardData existing = CardData.Get(card.id);
            if (existing == null)
            {
                CardData.card_list.Add(card);
                CardData.card_dict[card.id] = card;
                return card;
            }

            existing.title = card.title;
            existing.type = card.type;
            existing.team = card.team;
            existing.rarity = card.rarity;
            existing.mana = card.mana;
            existing.attack = card.attack;
            existing.hp = card.hp;
            existing.move_Range = card.move_Range;
            existing.attack_Range = card.attack_Range;
            existing.abilities = card.abilities;
            existing.text = card.text;
            existing.desc = card.desc;
            existing.fast_action = card.fast_action;
            existing.deckbuilding = card.deckbuilding;
            existing.series = card.series;
            return existing;
        }

        private static AbilityData RegisterAbility(AbilityData ability)
        {
            AbilityData existing = AbilityData.Get(ability.id);
            if (existing == null)
            {
                AbilityData.ability_list.Add(ability);
                AbilityData.ability_dict[ability.id] = ability;
                return ability;
            }

            existing.title = ability.title;
            existing.trigger = ability.trigger;
            existing.target = ability.target;
            existing.conditions_trigger = ability.conditions_trigger;
            existing.conditions_target = ability.conditions_target;
            existing.filters_target = ability.filters_target;
            existing.effects = ability.effects;
            existing.status = ability.status;
            existing.value = ability.value;
            existing.duration = ability.duration;
            existing.chain_abilities = ability.chain_abilities;
            existing.mana_cost = ability.mana_cost;
            existing.hp_cost = ability.hp_cost;
            existing.discard_cost = ability.discard_cost;
            existing.exhaust = ability.exhaust;
            existing.fast_action = ability.fast_action;
            existing.uses_per_turn = ability.uses_per_turn;
            existing.desc = ability.desc;
            return existing;
        }

        private static DeckData BuildDeck(string id, string title, CardData[] heroes, (CardData card, int count)[] entries)
        {
            DeckData deck = ScriptableObject.CreateInstance<DeckData>();
            deck.id = id;
            deck.title = title;
            deck.heroes = heroes;
            deck.hero = heroes.Length > 0 ? heroes[0] : null;
            deck.monsters = new CardData[0];

            List<CardData> cards = new List<CardData>();
            foreach ((CardData card, int count) entry in entries)
            {
                for (int i = 0; i < entry.count; i++)
                    cards.Add(entry.card);
            }
            deck.cards = cards.ToArray();
            return deck;
        }

        private static void RegisterDeck(DeckData deck)
        {
            DeckData existing = null;
            for (int i = DeckData.deck_list.Count - 1; i >= 0; i--)
            {
                DeckData item = DeckData.deck_list[i];
                if (item == null || item.id != deck.id)
                    continue;

                if (existing == null)
                    existing = item;
                else
                    DeckData.deck_list.RemoveAt(i);
            }

            if (existing == null)
            {
                DeckData.deck_list.Add(deck);
            }
            else
            {
                existing.title = deck.title;
                existing.hero = deck.hero;
                existing.heroes = deck.heroes;
                existing.cards = deck.cards;
                existing.monsters = deck.monsters;
                deck = existing;
            }

            GameplayData gdata = GameplayData.Get();
            if (gdata == null)
                return;

            gdata.ai_type = TcgEngine.AI.AIType.Vc5Demo;
            List<DeckData> free = new List<DeckData>();
            bool found = false;
            foreach (DeckData item in gdata.free_decks ?? new DeckData[0])
            {
                if (item == null)
                    continue;

                if (item.id == deck.id)
                {
                    if (found)
                        continue;
                    free.Add(deck);
                    found = true;
                }
                else
                {
                    free.Add(item);
                }
            }

            if (!found)
                free.Add(deck);
            gdata.free_decks = free.ToArray();

            List<DeckData> ai = new List<DeckData>();
            bool foundAi = false;
            foreach (DeckData item in gdata.ai_decks ?? new DeckData[0])
            {
                if (item == null)
                    continue;

                if (item.id == deck.id)
                {
                    if (foundAi)
                        continue;
                    ai.Add(deck);
                    foundAi = true;
                }
                else
                {
                    ai.Add(item);
                }
            }

            if (!foundAi)
                ai.Add(deck);
            gdata.ai_decks = ai.ToArray();
        }
    }
}
