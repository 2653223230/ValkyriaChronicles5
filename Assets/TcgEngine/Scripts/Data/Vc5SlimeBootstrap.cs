using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Runtime registration for VC5 slime heroes/cards so they can be built from structured data later.
    /// </summary>
    public static class Vc5SlimeBootstrap
    {
        private static bool loaded = false;

        /// <summary>编辑器导出/应用注册表前重置，以便重新 Register。</summary>
        public static void ResetForDataReload()
        {
            loaded = false;
        }

        private static TeamData team;
        private static RarityData rarity;

        private static TraitData traitSlime;
        private static TraitData traitSlimeBlood;
        private static TraitData traitSlimeCorrosive;
        private static TraitData traitSlimeHard;
        private static TraitData traitSlimeSpawn;
        private static TraitData fieldSlime;
        private static TraitData fieldSlimeEffect;
        private static TraitData fieldQuick;
        private static TraitData fieldHeavy;
        private static TraitData tmpSelectedAttack;
        private static TraitData tmpSelectedIsSlime;

        private static StatusData stSlime;
        private static StatusData stSharp;
        private static StatusData stRooted;

        private static EffectDamage effDamage;
        private static EffectHeal effHeal;

        public static void Register()
        {
            if (loaded)
            {
                Vc5ExcelAbilityConfig reCfg = Vc5ExcelAbilityConfig.Load();
                RefreshAllVc5FromCsv(reCfg);
                return;
            }

            team = TeamData.Get("neutral");
            if (team == null && TeamData.GetAll().Count > 0)
                team = TeamData.GetAll()[0];
            rarity = RarityData.Get("common");
            if (rarity == null && RarityData.GetAll().Count > 0)
                rarity = RarityData.GetAll()[0];

            traitSlime = TraitData.Get("slime");
            traitSlimeBlood = TraitData.Get("slime_blood");
            traitSlimeCorrosive = TraitData.Get("slime_corrosive");
            traitSlimeHard = TraitData.Get("slime_hard");
            traitSlimeSpawn = TraitData.Get("slime_spawn");
            fieldSlime = TraitData.Get("field_slime");
            fieldSlimeEffect = TraitData.Get("field_slime_effect");
            fieldQuick = TraitData.Get("field_quick");
            fieldHeavy = EnsureRuntimeTrait("field_heavy", "重击");

            tmpSelectedAttack = EnsureRuntimeTrait("tmp_selected_attack", "临时攻击");
            tmpSelectedIsSlime = EnsureRuntimeTrait("tmp_selected_is_slime", "临时黏黏标记");

            stSlime = StatusData.Get(StatusType.Slime);
            stSharp = StatusData.Get(StatusType.Sharp);
            stRooted = StatusData.Get(StatusType.Rooted);

            effDamage = ScriptableObject.CreateInstance<EffectDamage>();
            effHeal = ScriptableObject.CreateInstance<EffectHeal>();

            CardData spawn = CreateSlimeSpawnCard();
            CardData slimePool = CreateSlimePoolCard();

            Vc5ExcelAbilityConfig cfg = Vc5ExcelAbilityConfig.Load();
            CreateSlimeHeroes(spawn, cfg);
            CreateSlimeCards(spawn, slimePool, cfg);
            CreateSampleDecks();
            RefreshAllVc5FromCsv(cfg);
            loaded = true;
        }

        /// <summary>每次 <see cref="DataLoader"/> 在已注册 VC5 后再次加载时，用 Docs 下最新 CSV 覆盖数值与说明（黏黏+表中已有 id 的其它牌/英雄）。</summary>
        private static void RefreshAllVc5FromCsv(Vc5ExcelAbilityConfig cfg)
        {
            if (cfg == null)
                return;

            cfg.ForEachHero((string csvHid, Vc5ExcelAbilityConfig.HeroRow _) =>
            {
                string eid = Vc5CsvIdMaps.GetEngineHeroId(csvHid);
                CardData h = CardData.Get(eid);
                if (h != null)
                    ApplyHeroCsvStats(h, cfg, csvHid);
            });

            cfg.ForEachCard((string csvCid, Vc5ExcelAbilityConfig.CardRow row) =>
            {
                string eid = Vc5CsvIdMaps.GetEngineCardId(csvCid);
                CardData c = CardData.Get(eid);
                if (c == null)
                    return;
                ApplyCardConfig(c, row, null);
                if (eid == "vc5_slime_heavy_strike")
                    c.hp_cost = 0;
            });

            RefreshDataDrivenSlimeAbilities(cfg);
        }

        private static TraitData EnsureRuntimeTrait(string id, string title)
        {
            TraitData found = TraitData.Get(id);
            if (found != null)
                return found;
            TraitData t = ScriptableObject.CreateInstance<TraitData>();
            t.id = id;
            t.title = title;
            TraitData.trait_list.Add(t);
            return t;
        }

        private static AbilityData RegisterAbility(AbilityData ability)
        {
            AbilityData existing = AbilityData.Get(ability.id);
            if (existing != null)
            {
                CopyAbilityData(existing, ability);
                return existing;
            }
            AbilityData.ability_list.Add(ability);
            AbilityData.ability_dict[ability.id] = ability;
            return ability;
        }

        private static CardData RegisterCard(CardData card)
        {
            CardData existing = CardData.Get(card.id);
            if (existing != null)
            {
                CopyCardData(existing, card);
                return existing;
            }
            CardData.card_list.Add(card);
            CardData.card_dict[card.id] = card;
            return card;
        }

        private static void CopyAbilityData(AbilityData dest, AbilityData src)
        {
            if (dest == null || src == null)
                return;

            dest.trigger = src.trigger;
            dest.conditions_trigger = src.conditions_trigger;
            dest.target = src.target;
            dest.conditions_target = src.conditions_target;
            dest.filters_target = src.filters_target;
            dest.effects = src.effects;
            dest.status = src.status;
            dest.value = src.value;
            dest.duration = src.duration;
            dest.chain_abilities = src.chain_abilities;
            dest.mana_cost = src.mana_cost;
            dest.hp_cost = src.hp_cost;
            dest.discard_cost = src.discard_cost;
            dest.exhaust = src.exhaust;
            dest.fast_action = src.fast_action;
            dest.uses_per_turn = src.uses_per_turn;
            dest.title = src.title;
            dest.desc = src.desc;
        }

        private static void CopyCardData(CardData dest, CardData src)
        {
            if (dest == null || src == null)
                return;

            dest.title = src.title;
            dest.type = src.type;
            dest.team = src.team;
            dest.rarity = src.rarity;
            dest.mana = src.mana;
            dest.hp_cost = src.hp_cost;
            dest.discard_cost = src.discard_cost;
            dest.attack = src.attack;
            dest.hp = src.hp;
            dest.move_Range = src.move_Range;
            dest.attack_Range = src.attack_Range;
            dest.traits = src.traits;
            dest.stats = src.stats;
            dest.fields = src.fields;
            dest.fast_action = src.fast_action;
            dest.abilities = src.abilities;
            dest.text = src.text;
            dest.desc = src.desc;
            dest.deckbuilding = src.deckbuilding;
            dest.cost = src.cost;
            dest.packs = src.packs;
            dest.series = src.series;
        }

        private static CardData BaseCard(string id, string title, CardType type, int mana, int atk, int hp, int move, int range)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.id = id;
            card.title = title;
            card.type = type;
            card.team = team;
            card.rarity = rarity;
            card.mana = mana;
            card.attack = atk;
            card.hp = hp;
            card.move_Range = move;
            card.attack_Range = range;
            card.abilities = new AbilityData[0];
            card.traits = new TraitData[0];
            card.stats = new TraitStat[0];
            card.fields = new TraitData[0];
            card.packs = new PackData[0];
            card.series = "";
            card.deckbuilding = true;
            return card;
        }

        private static AbilityData BaseAbility(string id, string title, AbilityTrigger trigger, AbilityTarget target)
        {
            AbilityData a = ScriptableObject.CreateInstance<AbilityData>();
            a.id = id;
            a.title = title;
            a.trigger = trigger;
            a.target = target;
            a.conditions_trigger = new ConditionData[0];
            a.conditions_target = new ConditionData[0];
            a.filters_target = new FilterData[0];
            a.effects = new EffectData[0];
            a.status = new StatusData[0];
            a.chain_abilities = new AbilityData[0];
            return a;
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

        private static ConditionData[] AllyCardConds()
        {
            ConditionTarget cType = ScriptableObject.CreateInstance<ConditionTarget>();
            cType.type = ConditionTargetType.Card;
            cType.oper = ConditionOperatorBool.IsTrue;

            ConditionOwner cOwner = ScriptableObject.CreateInstance<ConditionOwner>();
            cOwner.oper = ConditionOperatorBool.IsTrue;
            return new ConditionData[] { cType, cOwner };
        }

        private static TraitData[] ParseFieldTraits(string fieldsCsv)
        {
            if (string.IsNullOrEmpty(fieldsCsv))
                return new TraitData[0];

            List<TraitData> outFields = new List<TraitData>();
            string[] tags = fieldsCsv.Split('|');
            foreach (string raw in tags)
            {
                string tag = raw.Trim();
                TraitData t = null;
                if (tag == "黏黏")
                    t = fieldSlime;
                else if (tag == "粘液")
                    t = fieldSlimeEffect;
                else if (tag == "快速行动")
                    t = fieldQuick;
                else if (tag == "重击")
                    t = fieldHeavy;
                if (t != null && !outFields.Contains(t))
                    outFields.Add(t);
            }
            return outFields.ToArray();
        }

        private static void ApplyCardConfig(CardData card, Vc5ExcelAbilityConfig.CardRow row, TraitData[] fallbackFields)
        {
            if (card == null)
                return;
            if (row == null)
            {
                if (fallbackFields != null)
                    card.fields = fallbackFields;
                return;
            }

            card.mana = row.cost_mana;
            card.hp_cost = row.cost_hp;
            card.discard_cost = row.cost_discard;
            card.fast_action = row.fast_action;
            if (!string.IsNullOrWhiteSpace(row.player_read_text))
                card.text = row.player_read_text.Trim();
            if (!string.IsNullOrWhiteSpace(row.display_name))
                card.title = row.display_name.Trim();
            if (row.opt_attack.HasValue)
                card.attack = row.opt_attack.Value;
            if (row.opt_hp.HasValue)
                card.hp = row.opt_hp.Value;
            if (row.opt_move_Range.HasValue)
                card.move_Range = row.opt_move_Range.Value;
            if (row.opt_attack_Range.HasValue)
                card.attack_Range = row.opt_attack_Range.Value;

            if (!string.IsNullOrWhiteSpace(row.series))
                card.series = row.series.Trim();
            TraitData[] parsedFields = ParseFieldTraits(row.fields);
            if (parsedFields.Length > 0)
                card.fields = parsedFields;
            else if (fallbackFields != null && fallbackFields.Length > 0)
                card.fields = fallbackFields;
        }

        private static void ApplyAbilityCosts(AbilityData ability, Vc5ExcelAbilityConfig.AbilityRow row, bool applyValueFromTable = true)
        {
            if (ability == null || row == null)
                return;
            if (applyValueFromTable)
                ability.value = row.value;
            ability.mana_cost = row.mana_cost;
            ability.hp_cost = row.hp_cost;
            ability.discard_cost = row.discard_cost;
            ability.uses_per_turn = row.uses_per_turn;
        }

        /// <summary>用 heroes.csv 中对应 hero_id 行覆盖基础攻防与移距（策划改 cards.csv 中的英雄行不会生效）。</summary>
        private static void ApplyHeroCsvStats(CardData card, Vc5ExcelAbilityConfig cfg, string heroCsvId)
        {
            if (card == null || cfg == null || string.IsNullOrEmpty(heroCsvId))
                return;
            Vc5ExcelAbilityConfig.HeroRow h = cfg.GetHero(heroCsvId);
            if (h == null)
                return;
            card.attack = h.attack;
            card.hp = h.hp;
            card.move_Range = h.move_Range;
            card.attack_Range = h.attack_Range;
            if (!string.IsNullOrWhiteSpace(h.player_read_text))
                card.text = h.player_read_text.Trim();
            if (!string.IsNullOrWhiteSpace(h.series))
                card.series = h.series.Trim();
            Debug.Log($"[VC5 CSV] 英雄 {heroCsvId}->{card.id} 已应用 CSV 数值：攻={card.attack}, 血={card.hp}, 移动={card.move_Range}, 攻距={card.attack_Range}");
        }

        private static void RefreshDataDrivenSlimeAbilities(Vc5ExcelAbilityConfig cfg)
        {
            if (cfg == null)
                return;

            Vc5ExcelAbilityConfig.AbilityRow heavyHitRow = cfg.GetAbility("vc5_slime_heavy_hit");
            Vc5ExcelAbilityConfig.EffectRow heavyHitEff = heavyHitRow != null ? cfg.GetFirstEffect(heavyHitRow.effect_set_id) : null;
            AbilityData heavyHit = AbilityData.Get("vc5_slime_heavy_hit");
            if (heavyHit != null)
            {
                ApplyAbilityCosts(heavyHit, heavyHitRow, false);
                heavyHit.value = heavyHitEff != null ? heavyHitEff.param_int_a : 2;
            }

            AbilityData heavyPick = AbilityData.Get("vc5_slime_heavy_pick");
            ApplyAbilityCosts(heavyPick, cfg.GetAbility("vc5_slime_heavy_pick"), true);
            Vc5ExcelAbilityConfig.CardRow heavyCardRow = cfg.GetCard("card_slime_heavy");
            int heavyHpCost = heavyCardRow != null ? heavyCardRow.cost_hp : 2;
            CardData heavyCard = CardData.Get("vc5_slime_heavy_strike");
            if (heavyCard != null)
                heavyCard.hp_cost = 0;
            if (heavyPick != null)
            {
                if (heavyPick.effects != null && heavyPick.effects.Length > 0 && heavyPick.effects[0] is EffectStoreTargetStatsToCaster heavyStore)
                    heavyStore.hp_cost_from_target = heavyHpCost;
                if (heavyPick.conditions_target != null)
                {
                    foreach (ConditionData cond in heavyPick.conditions_target)
                    {
                        if (cond is ConditionTargetHP hpCond)
                        {
                            hpCond.value = heavyHpCost;
                            hpCond.oper = ConditionOperatorInt.Greater;
                        }
                    }
                }
            }

            Vc5ExcelAbilityConfig.AbilityRow heavyExtraRow = cfg.GetAbility("vc5_slime_heavy_extra");
            AbilityData heavyExtra = AbilityData.Get("vc5_slime_heavy_extra");
            if (heavyExtra != null)
            {
                ApplyAbilityCosts(heavyExtra, heavyExtraRow, false);
                if (heavyExtra.conditions_trigger != null && heavyExtra.conditions_trigger.Length > 0 && heavyExtra.conditions_trigger[0] is ConditionCasterTrait hct)
                {
                    hct.value = 1;
                    if (heavyExtraRow != null && !string.IsNullOrEmpty(heavyExtraRow.condition_triggerer_trait))
                        hct.value = Mathf.Max(1, heavyExtraRow.condition_triggerer_trait_value);
                }
                heavyExtra.value = 1;
                if (heavyExtraRow != null && !string.IsNullOrEmpty(heavyExtraRow.status_type))
                {
                    if (System.Enum.TryParse(heavyExtraRow.status_type, true, out StatusType st))
                        heavyExtra.status = new StatusData[] { StatusData.Get(st) };
                    heavyExtra.value = heavyExtraRow.status_value > 0 ? heavyExtraRow.status_value : 1;
                    heavyExtra.duration = heavyExtraRow.status_duration;
                }
            }

            Vc5ExcelAbilityConfig.AbilityRow comboHitRow = cfg.GetAbility("vc5_slime_combo_hit");
            Vc5ExcelAbilityConfig.EffectRow comboHitEff = comboHitRow != null ? cfg.GetFirstEffect(comboHitRow.effect_set_id) : null;
            AbilityData comboHit = AbilityData.Get("vc5_slime_combo_hit");
            if (comboHit != null)
            {
                ApplyAbilityCosts(comboHit, comboHitRow, false);
                comboHit.value = comboHitEff != null ? comboHitEff.param_int_a : 1;
            }

            Vc5ExcelAbilityConfig.AbilityRow comboExtraRow = cfg.GetAbility("vc5_slime_combo_extra");
            Vc5ExcelAbilityConfig.EffectRow comboExtraEff = comboExtraRow != null ? cfg.GetFirstEffect(comboExtraRow.effect_set_id) : null;
            AbilityData comboExtra = AbilityData.Get("vc5_slime_combo_extra");
            if (comboExtra != null)
            {
                ApplyAbilityCosts(comboExtra, comboExtraRow, false);
                comboExtra.value = comboExtraEff != null ? comboExtraEff.param_int_a : (comboHit != null ? comboHit.value : 1);
                if (comboExtra.conditions_trigger != null && comboExtra.conditions_trigger.Length > 0 && comboExtra.conditions_trigger[0] is ConditionCasterTrait cct)
                {
                    cct.value = 1;
                    if (comboExtraRow != null && comboExtraRow.condition_triggerer_trait_value > 0)
                        cct.value = comboExtraRow.condition_triggerer_trait_value;
                }
            }

            ApplyAbilityCosts(AbilityData.Get("vc5_slime_combo_pick"), cfg.GetAbility("vc5_slime_combo_pick"), true);

            ApplyAbilityCosts(AbilityData.Get("vc5_slime_spray_pick"), cfg.GetAbility("vc5_slime_spray_pick"), true);

            Vc5ExcelAbilityConfig.AbilityRow sprayHitRow = cfg.GetAbility("vc5_slime_spray_hit");
            Vc5ExcelAbilityConfig.EffectRow sprayEff = sprayHitRow != null ? cfg.GetFirstEffect(sprayHitRow.effect_set_id) : null;
            AbilityData sprayHit = AbilityData.Get("vc5_slime_spray_hit");
            if (sprayHit != null && sprayHit.effects != null && sprayHit.effects.Length > 0 && sprayHit.effects[0] is EffectApplyStatusOrHealByTrait sprayFx)
            {
                ApplyAbilityCosts(sprayHit, sprayHitRow, true);
                if (sprayEff != null)
                {
                    sprayFx.status_value = sprayEff.param_int_a > 0 ? sprayEff.param_int_a : 2;
                    sprayFx.heal_value = sprayEff.param_int_b > 0 ? sprayEff.param_int_b : 3;
                }
                else
                {
                    sprayFx.status_value = 2;
                    sprayFx.heal_value = 3;
                }
            }
        }

        private static CardData CreateSlimeSpawnCard()
        {
            CardData card = BaseCard("vc5_slime_spawn", "黏黏幼崽", CardType.Character, 0, 1, 1, 1, 1);
            card.deckbuilding = false;
            card.traits = new TraitData[] { traitSlimeSpawn };
            card.fields = new TraitData[] { fieldSlime };
            card.text = "【效果】1血1攻，敌方不可通过；友方可通过，若停留其上可吞噬并回复1生命。\n【特殊效果】无";
            return RegisterCard(card);
        }

        private static CardData CreateSlimePoolCard()
        {
            CardData card = BaseCard("vc5_slime_pool", "粘液池", CardType.Artifact, 0, 0, 1, 0, 0);
            card.deckbuilding = false;
            card.fields = new TraitData[] { fieldSlimeEffect };
            card.text = "【效果】召唤粘液池，不可被攻击，持续若干回合。\n【特殊效果】若己方存在腐蚀黏黏，持续回合+1。";
            return RegisterCard(card);
        }

        private static void CreateSlimeHeroes(CardData spawn, Vc5ExcelAbilityConfig cfg)
        {
            ConditionSlotDistFromTriggerer inRange = ScriptableObject.CreateInstance<ConditionSlotDistFromTriggerer>();
            inRange.range_offset = 0;

            if (CardData.Get("vc5_hero_slime_blood") != null)
            {
                // loaded from Resources .asset
            }
            else
            {
            AbilityData bloodSkill = BaseAbility("vc5_blood_slime_skill", "血腥喷溅", AbilityTrigger.Activate, AbilityTarget.SelectTarget);
            bloodSkill.mana_cost = 1;
            bloodSkill.conditions_target = EnemyCardConds();
            bloodSkill.conditions_target = new ConditionData[] { bloodSkill.conditions_target[0], bloodSkill.conditions_target[1], inRange };
            EffectDamageFromTargetStatus dmgBySlime = ScriptableObject.CreateInstance<EffectDamageFromTargetStatus>();
            dmgBySlime.status = StatusType.Slime;
            dmgBySlime.divisor = 2;
            bloodSkill.effects = new EffectData[] { dmgBySlime };
            bloodSkill.value = 1;
            bloodSkill.uses_per_turn = 1;
            RegisterAbility(bloodSkill);

            AbilityData bloodAwake = BaseAbility("vc5_blood_slime_awake", "粘液引爆", AbilityTrigger.Activate, AbilityTarget.AllCardsBoard);
            ConditionAnyEnemyHasStatus condAwake = ScriptableObject.CreateInstance<ConditionAnyEnemyHasStatus>();
            condAwake.status = StatusType.Slime;
            condAwake.value = 4;
            bloodAwake.conditions_trigger = new ConditionData[] { condAwake };
            ConditionOwner cEnemy = ScriptableObject.CreateInstance<ConditionOwner>();
            cEnemy.oper = ConditionOperatorBool.IsFalse;
            ConditionStatus cSlime4 = ScriptableObject.CreateInstance<ConditionStatus>();
            cSlime4.has_status = StatusType.Slime;
            cSlime4.value = 4;
            cSlime4.oper = ConditionOperatorBool.IsTrue;
            bloodAwake.conditions_target = new ConditionData[] { cEnemy, cSlime4 };
            EffectDamageFromTargetStatus explode = ScriptableObject.CreateInstance<EffectDamageFromTargetStatus>();
            explode.status = StatusType.Slime;
            explode.divisor = 2;
            explode.clear_status = true;
            bloodAwake.effects = new EffectData[] { explode };
            bloodAwake.value = 1;
            bloodAwake.uses_per_turn = 1;
            RegisterAbility(bloodAwake);

            CardData blood = BaseCard("vc5_hero_slime_blood", "血腥黏黏", CardType.Character, 0, 1, 12, 3, 1);
            blood.traits = new TraitData[] { traitSlime, traitSlimeBlood };
            blood.fields = new TraitData[] { fieldSlime };
            blood.abilities = new AbilityData[] { bloodSkill, bloodAwake };
            blood.text = "【被动】己方黏黏对带粘液敌方单位造成伤害时恢复1生命。\n【技能】消耗1法力，每回合1次：对攻击距离内1名敌人造成1+其粘液层数/2（向下取整）的伤害。\n【觉醒】场上有敌人粘液层数≥4时可发动：对所有满足条件的敌人造成1+其粘液层数/2（向下取整）的伤害并移除其全部粘液。";
            ApplyHeroCsvStats(blood, cfg, "hero_slime_blood");
            RegisterCard(blood);
            }

            if (CardData.Get("vc5_hero_slime_corrosive") != null)
            {
            }
            else
            {
            // 腐蚀黏黏
            AbilityData corAwake = BaseAbility("vc5_corrosive_slime_awake", "腐蚀喷洒", AbilityTrigger.Activate, AbilityTarget.SelectTarget);
            corAwake.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionTarget>() };
            ((ConditionTarget)corAwake.conditions_target[0]).type = ConditionTargetType.Card;
            ((ConditionTarget)corAwake.conditions_target[0]).oper = ConditionOperatorBool.IsTrue;
            corAwake.status = new StatusData[] { stSlime };
            corAwake.value = 4;
            corAwake.uses_per_turn = 1;
            RegisterAbility(corAwake);

            CardData corrosive = BaseCard("vc5_hero_slime_corrosive", "腐蚀黏黏", CardType.Character, 0, 1, 14, 3, 1);
            corrosive.traits = new TraitData[] { traitSlime, traitSlimeCorrosive };
            corrosive.fields = new TraitData[] { fieldSlime };
            corrosive.abilities = new AbilityData[] { corAwake };
            corrosive.text = "【被动】身上持有粘液的敌方单位移动力-1（不叠加）。\n【觉醒】每回合1次：向任意目标施放粘液共4次（可分配给不同单位）。";
            ApplyHeroCsvStats(corrosive, cfg, "hero_slime_corrosive");
            RegisterCard(corrosive);
            }

            if (CardData.Get("vc5_hero_slime_giant") != null)
            {
            }
            else
            {
            // 巨臂黏黏
            AbilityData giantMove = BaseAbility("vc5_giant_slime_move", "巨臂甩击", AbilityTrigger.Activate, AbilityTarget.SelectTarget);
            giantMove.conditions_target = EnemyCardConds();
            ConditionStatus withSlime = ScriptableObject.CreateInstance<ConditionStatus>();
            withSlime.has_status = StatusType.Slime;
            withSlime.value = 1;
            withSlime.oper = ConditionOperatorBool.IsTrue;
            giantMove.conditions_target = new ConditionData[] { giantMove.conditions_target[0], giantMove.conditions_target[1], withSlime, inRange };
            EffectMoveTargetByStatusDistance moveBySlime = ScriptableObject.CreateInstance<EffectMoveTargetByStatusDistance>();
            moveBySlime.status = StatusType.Slime;
            moveBySlime.divisor = 3;
            moveBySlime.base_distance = 1;
            moveBySlime.damage_if_awakened = true;
            giantMove.effects = new EffectData[] { moveBySlime };
            giantMove.uses_per_turn = 1;
            RegisterAbility(giantMove);

            AbilityData giantAwake = BaseAbility("vc5_giant_slime_awake", "真巨臂形态", AbilityTrigger.Activate, AbilityTarget.Self);
            giantAwake.status = new StatusData[] { StatusData.Get(StatusType.GiantArmAwakened) };
            giantAwake.value = 1;
            giantAwake.duration = 0;
            giantAwake.uses_per_turn = 1;
            RegisterAbility(giantAwake);

            CardData giant = BaseCard("vc5_hero_slime_giant", "巨臂黏黏", CardType.Character, 0, 1, 11, 3, 2);
            giant.traits = new TraitData[] { traitSlime };
            giant.fields = new TraitData[] { fieldSlime };
            giant.abilities = new AbilityData[] { giantMove, giantAwake };
            giant.text = "【技能】每回合1次：使攻击距离内带粘液的敌人位移，距离为1+粘液层数/3（向下取整）。\n【觉醒】任意友方角色死亡后激活（全局1次）：进入真巨臂形态；本局内每次对其造成位移时，按位移距离对其造成伤害。";
            ApplyHeroCsvStats(giant, cfg, "hero_slime_giant");
            RegisterCard(giant);
            }

            if (CardData.Get("vc5_hero_slime_hard") != null)
            {
            }
            else
            {
            // 坚硬黏黏
            AbilityData hardAwake = BaseAbility("vc5_hard_slime_awake", "防护粘液", AbilityTrigger.Activate, AbilityTarget.SelectTarget);
            hardAwake.conditions_target = AllyCardConds();
            hardAwake.status = new StatusData[] { stSlime };
            hardAwake.value = 2;
            hardAwake.uses_per_turn = 1;
            RegisterAbility(hardAwake);

            CardData hard = BaseCard("vc5_hero_slime_hard", "坚硬黏黏", CardType.Character, 0, 0, 15, 2, 1);
            hard.traits = new TraitData[] { traitSlime, traitSlimeHard };
            hard.fields = new TraitData[] { fieldSlime };
            hard.abilities = new AbilityData[] { hardAwake };
            hard.text = "【被动】自己的粘液可对友军施加；友军受伤时按粘液层数减伤并移除对应层数。\n【觉醒】每2回合1次：向单个友军施放粘液，次数=当前生命值/2（向下取整）。";
            ApplyHeroCsvStats(hard, cfg, "hero_slime_hard");
            RegisterCard(hard);
            }

            if (CardData.Get("vc5_hero_slime_hard2") != null)
            {
            }
            else
            {
            // 坚硬黏黏2
            AbilityData hard2Skill = BaseAbility("vc5_hard2_slime_skill", "幼崽投掷", AbilityTrigger.Activate, AbilityTarget.SelectTarget);
            hard2Skill.discard_cost = 1;
            hard2Skill.conditions_target = EnemyCardConds();
            ConditionSlotDistFromTriggerer inRange2 = ScriptableObject.CreateInstance<ConditionSlotDistFromTriggerer>();
            inRange2.range_offset = 0;
            hard2Skill.conditions_target = new ConditionData[] { hard2Skill.conditions_target[0], hard2Skill.conditions_target[1], inRange2 };
            EffectDamageFromCasterAttack dmgCaster = ScriptableObject.CreateInstance<EffectDamageFromCasterAttack>();
            EffectSummonAdjacentToTarget summonAdj = ScriptableObject.CreateInstance<EffectSummonAdjacentToTarget>();
            summonAdj.summon = spawn;
            hard2Skill.effects = new EffectData[] { dmgCaster, summonAdj };
            hard2Skill.uses_per_turn = 1;
            RegisterAbility(hard2Skill);

            CardData hard2 = BaseCard("vc5_hero_slime_hard2", "坚硬黏黏2", CardType.Character, 0, 1, 13, 3, 2);
            hard2.traits = new TraitData[] { traitSlime, traitSlimeHard };
            hard2.fields = new TraitData[] { fieldSlime };
            hard2.abilities = new AbilityData[] { hard2Skill };
            hard2.text = "【被动】同坚硬黏黏。\n【技能】每回合1次，弃1张手牌：对攻击距离内1名敌人造成等同攻击力的伤害，并在其相邻空格召唤黏黏幼崽（1/1）。";
            ApplyHeroCsvStats(hard2, cfg, "hero_slime_hard2");
            RegisterCard(hard2);
            }
        }

        private static void CreateSlimeCards(CardData spawn, CardData slimePool, Vc5ExcelAbilityConfig cfg)
        {

            // 通用选择我方单位 -> 选择目标单位结构
            ConditionSlotDistFromTriggerer range0 = ScriptableObject.CreateInstance<ConditionSlotDistFromTriggerer>();
            range0.range_offset = 0;
            ConditionSlotDistFromTriggerer range1 = ScriptableObject.CreateInstance<ConditionSlotDistFromTriggerer>();
            range1.range_offset = 1;

            EffectStoreTargetStatsToCaster store = ScriptableObject.CreateInstance<EffectStoreTargetStatsToCaster>();
            store.attack_trait = tmpSelectedAttack;
            store.slime_flag_trait = tmpSelectedIsSlime;
            store.required_trait = traitSlime;

            // 黏黏重击
            Vc5ExcelAbilityConfig.CardRow heavyCardRow = cfg.GetCard("card_slime_heavy");
            int heavyHpCost = heavyCardRow != null ? heavyCardRow.cost_hp : 2;
            EffectStoreTargetStatsToCaster heavyStore = ScriptableObject.CreateInstance<EffectStoreTargetStatsToCaster>();
            heavyStore.attack_trait = tmpSelectedAttack;
            heavyStore.slime_flag_trait = tmpSelectedIsSlime;
            heavyStore.required_trait = traitSlime;
            heavyStore.hp_cost_from_target = heavyHpCost;
            ConditionTargetHP heavyCasterCanPay = ScriptableObject.CreateInstance<ConditionTargetHP>();
            heavyCasterCanPay.value = heavyHpCost;
            heavyCasterCanPay.oper = ConditionOperatorInt.Greater;
            AbilityData heavyPick = BaseAbility("vc5_slime_heavy_pick", "选择我方单位", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            ConditionData[] heavyAllyConds = AllyCardConds();
            heavyPick.conditions_target = new ConditionData[] { heavyAllyConds[0], heavyAllyConds[1], heavyCasterCanPay };
            heavyPick.effects = new EffectData[] { heavyStore };
            ApplyAbilityCosts(heavyPick, cfg.GetAbility("vc5_slime_heavy_pick"));

            AbilityData heavyHit = BaseAbility("vc5_slime_heavy_hit", "重击", AbilityTrigger.None, AbilityTarget.SelectTarget);
            heavyHit.conditions_target = new ConditionData[] { EnemyCardConds()[0], EnemyCardConds()[1], range0 };
            EffectDamageFromTriggererAttack heavyDmg = ScriptableObject.CreateInstance<EffectDamageFromTriggererAttack>();
            heavyHit.effects = new EffectData[] { heavyDmg };
            Vc5ExcelAbilityConfig.AbilityRow heavyHitRow = cfg.GetAbility("vc5_slime_heavy_hit");
            Vc5ExcelAbilityConfig.EffectRow heavyHitEff = heavyHitRow != null ? cfg.GetFirstEffect(heavyHitRow.effect_set_id) : null;
            ApplyAbilityCosts(heavyHit, heavyHitRow, false);
            heavyHit.value = heavyHitEff != null ? heavyHitEff.param_int_a : 2;
            RegisterAbility(heavyHit);

            AbilityData heavyExtra = BaseAbility("vc5_slime_heavy_extra", "重击附加粘液", AbilityTrigger.None, AbilityTarget.LastTargeted);
            Vc5ExcelAbilityConfig.AbilityRow heavyExtraRow = cfg.GetAbility("vc5_slime_heavy_extra");
            ApplyAbilityCosts(heavyExtra, heavyExtraRow, false);
            ConditionCasterTrait heavyCasterIsSlime = ScriptableObject.CreateInstance<ConditionCasterTrait>();
            heavyCasterIsSlime.trait = tmpSelectedIsSlime;
            heavyCasterIsSlime.oper = ConditionOperatorInt.GreaterEqual;
            heavyCasterIsSlime.value = 1;
            if (heavyExtraRow != null && !string.IsNullOrEmpty(heavyExtraRow.condition_triggerer_trait))
                heavyCasterIsSlime.value = Mathf.Max(1, heavyExtraRow.condition_triggerer_trait_value);
            heavyExtra.conditions_trigger = new ConditionData[] { heavyCasterIsSlime };
            heavyExtra.status = new StatusData[] { stSlime };
            heavyExtra.value = 1;
            if (heavyExtraRow != null && !string.IsNullOrEmpty(heavyExtraRow.status_type))
            {
                if (System.Enum.TryParse(heavyExtraRow.status_type, true, out StatusType st))
                    heavyExtra.status = new StatusData[] { StatusData.Get(st) };
                heavyExtra.value = heavyExtraRow.status_value > 0 ? heavyExtraRow.status_value : 1;
                heavyExtra.duration = heavyExtraRow.status_duration;
            }
            RegisterAbility(heavyExtra);

            heavyHit.chain_abilities = new AbilityData[] { heavyExtra };
            heavyPick.chain_abilities = new AbilityData[] { heavyHit };
            RegisterAbility(heavyHit);
            RegisterAbility(heavyPick);

            CardData heavyCard = BaseCard("vc5_slime_heavy_strike", "黏黏重击", CardType.Spell, 0, 0, 0, 0, 0);
            ApplyCardConfig(heavyCard, heavyCardRow, new TraitData[] { fieldSlime, fieldHeavy });
            // HP cost is paid by the selected releasing hero after target selection, not by the player before selecting.
            heavyCard.hp_cost = 0;
            heavyCard.abilities = new AbilityData[] { heavyPick };
            if (string.IsNullOrWhiteSpace(heavyCard.text))
                heavyCard.text = "【效果】让一个我方英雄，对攻击距离内的一个任意敌人造成2+攻击力的伤害。\n【特殊效果】如果本卡片由我方【黏黏】英雄使用，则对敌人额外附加一层【粘液】。";
            RegisterCard(heavyCard);

            // 黏黏连打
            AbilityData comboPick = BaseAbility("vc5_slime_combo_pick", "选择我方单位", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            comboPick.conditions_target = AllyCardConds();
            comboPick.effects = new EffectData[] { store };
            ApplyAbilityCosts(comboPick, cfg.GetAbility("vc5_slime_combo_pick"));

            AbilityData comboHit = BaseAbility("vc5_slime_combo_hit", "连打", AbilityTrigger.None, AbilityTarget.SelectTarget);
            comboHit.conditions_target = new ConditionData[] { ScriptableObject.CreateInstance<ConditionTarget>(), range0 };
            ((ConditionTarget)comboHit.conditions_target[0]).type = ConditionTargetType.Card;
            ((ConditionTarget)comboHit.conditions_target[0]).oper = ConditionOperatorBool.IsTrue;
            EffectDamageFromTriggererAttack comboDmg = ScriptableObject.CreateInstance<EffectDamageFromTriggererAttack>();
            comboHit.effects = new EffectData[] { comboDmg };
            Vc5ExcelAbilityConfig.AbilityRow comboHitRow = cfg.GetAbility("vc5_slime_combo_hit");
            Vc5ExcelAbilityConfig.EffectRow comboHitEff = comboHitRow != null ? cfg.GetFirstEffect(comboHitRow.effect_set_id) : null;
            ApplyAbilityCosts(comboHit, comboHitRow, false);
            comboHit.value = comboHitEff != null ? comboHitEff.param_int_a : 1;
            RegisterAbility(comboHit);

            AbilityData comboExtra = BaseAbility("vc5_slime_combo_extra", "额外连打", AbilityTrigger.None, AbilityTarget.LastTargeted);
            comboExtra.effects = new EffectData[] { comboDmg };
            Vc5ExcelAbilityConfig.AbilityRow comboExtraRow = cfg.GetAbility("vc5_slime_combo_extra");
            Vc5ExcelAbilityConfig.EffectRow comboExtraEff = comboExtraRow != null ? cfg.GetFirstEffect(comboExtraRow.effect_set_id) : null;
            ApplyAbilityCosts(comboExtra, comboExtraRow, false);
            comboExtra.value = comboExtraEff != null ? comboExtraEff.param_int_a : comboHit.value;
            ConditionCasterTrait casterIsSlime = ScriptableObject.CreateInstance<ConditionCasterTrait>();
            casterIsSlime.trait = tmpSelectedIsSlime;
            casterIsSlime.oper = ConditionOperatorInt.GreaterEqual;
            casterIsSlime.value = 1;
            if (comboExtraRow != null && comboExtraRow.condition_triggerer_trait_value > 0)
                casterIsSlime.value = comboExtraRow.condition_triggerer_trait_value;
            comboExtra.conditions_trigger = new ConditionData[] { casterIsSlime };
            RegisterAbility(comboExtra);
            comboHit.chain_abilities = new AbilityData[] { comboExtra };
            comboPick.chain_abilities = new AbilityData[] { comboHit };
            RegisterAbility(comboHit);
            RegisterAbility(comboPick);

            CardData comboCard = BaseCard("vc5_slime_combo_strike", "黏黏连打", CardType.Spell, 1, 0, 0, 0, 0);
            ApplyCardConfig(comboCard, cfg.GetCard("card_slime_combo"), new TraitData[] { fieldSlime });
            comboCard.abilities = new AbilityData[] { comboPick };
            if (string.IsNullOrWhiteSpace(comboCard.text))
                comboCard.text = "【效果】让一个我方英雄，对攻击距离内的一个任意单位造成1+攻击力的伤害。\n【特殊效果】如果本卡片由我方【黏黏】英雄使用，则对敌人额外释放一次。";
            RegisterCard(comboCard);

            // 粘液池
            AbilityData poolPlay = BaseAbility("vc5_slime_pool_play", "放置粘液池", AbilityTrigger.OnPlay, AbilityTarget.PlayTarget);
            ConditionSlotEmpty cEmpty = ScriptableObject.CreateInstance<ConditionSlotEmpty>();
            cEmpty.oper = ConditionOperatorBool.IsTrue;
            ConditionSlotRangeFromAlly nearAlly = ScriptableObject.CreateInstance<ConditionSlotRangeFromAlly>();
            nearAlly.range = 2;
            poolPlay.conditions_target = new ConditionData[] { cEmpty, nearAlly };
            EffectSummonWithStatusDuration summonPool = ScriptableObject.CreateInstance<EffectSummonWithStatusDuration>();
            summonPool.summon = slimePool;
            summonPool.status = StatusType.Sleep;
            summonPool.base_duration = 2;
            summonPool.bonus_trait = traitSlimeCorrosive;
            summonPool.bonus_duration = 1;
            poolPlay.effects = new EffectData[] { summonPool };
            RegisterAbility(poolPlay);

            CardData poolCard = BaseCard("vc5_slime_pool_spell", "粘液池", CardType.Spell, 1, 0, 0, 0, 0);
            ApplyCardConfig(poolCard, cfg.GetCard("card_slime_pool"), new TraitData[] { fieldSlimeEffect });
            poolCard.abilities = new AbilityData[] { poolPlay };
            if (string.IsNullOrWhiteSpace(poolCard.text))
                poolCard.text = "【效果】在己方任意角色2格范围内无单位的格子召唤粘液池，不可被攻击，持续2回合。\n【特殊效果】若己方存在腐蚀黏黏，持续回合+1。";
            RegisterCard(poolCard);

            // 粘液喷射
            AbilityData sprayPick = BaseAbility("vc5_slime_spray_pick", "选择施法者", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            sprayPick.conditions_target = AllyCardConds();
            sprayPick.effects = new EffectData[] { store };
            ApplyAbilityCosts(sprayPick, cfg.GetAbility("vc5_slime_spray_pick"));
            AbilityData sprayHit = BaseAbility("vc5_slime_spray_hit", "粘液喷射", AbilityTrigger.None, AbilityTarget.SelectTarget);
            ConditionTarget cCard = ScriptableObject.CreateInstance<ConditionTarget>();
            cCard.type = ConditionTargetType.Card;
            cCard.oper = ConditionOperatorBool.IsTrue;
            sprayHit.conditions_target = new ConditionData[] { cCard, range1 };
            EffectApplyStatusOrHealByTrait sprayEffect = ScriptableObject.CreateInstance<EffectApplyStatusOrHealByTrait>();
            sprayEffect.status = StatusType.Slime;
            sprayEffect.required_trait = traitSlime;
            sprayEffect.require_ally_target = true;
            Vc5ExcelAbilityConfig.AbilityRow sprayHitRow = cfg.GetAbility("vc5_slime_spray_hit");
            Vc5ExcelAbilityConfig.EffectRow sprayEff = sprayHitRow != null ? cfg.GetFirstEffect(sprayHitRow.effect_set_id) : null;
            if (sprayEff != null)
            {
                sprayEffect.status_value = sprayEff.param_int_a > 0 ? sprayEff.param_int_a : 2;
                sprayEffect.heal_value = sprayEff.param_int_b > 0 ? sprayEff.param_int_b : 3;
            }
            else
            {
                sprayEffect.status_value = 2;
                sprayEffect.heal_value = 3;
            }
            sprayHit.effects = new EffectData[] { sprayEffect };
            ApplyAbilityCosts(sprayHit, sprayHitRow);
            RegisterAbility(sprayHit);
            sprayPick.chain_abilities = new AbilityData[] { sprayHit };
            RegisterAbility(sprayPick);

            CardData sprayCard = BaseCard("vc5_slime_spray", "粘液喷射", CardType.Spell, 1, 0, 0, 0, 0);
            ApplyCardConfig(sprayCard, cfg.GetCard("card_slime_spray"), new TraitData[] { fieldSlimeEffect });
            sprayCard.abilities = new AbilityData[] { sprayPick };
            if (string.IsNullOrWhiteSpace(sprayCard.text))
                sprayCard.text = "【效果】让一个我方英雄，对攻击距离+1内的一个任意单位施加2层【粘液】。\n【特殊效果】可以对己方【黏黏】使用，效果变为恢复3生命值。";
            RegisterCard(sprayCard);

            // 粘液附着（快速）
            AbilityData attachPick = BaseAbility("vc5_slime_attach_pick", "选择施法者", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            attachPick.conditions_target = AllyCardConds();
            attachPick.effects = new EffectData[] { store };
            AbilityData attachHit = BaseAbility("vc5_slime_attach_hit", "粘液附着", AbilityTrigger.None, AbilityTarget.SelectTarget);
            attachHit.conditions_target = new ConditionData[] { cCard, range1 };
            EffectApplyStatusPairOrSharpByTrait pair = ScriptableObject.CreateInstance<EffectApplyStatusPairOrSharpByTrait>();
            pair.status_a = StatusType.Slime;
            pair.value_a = 1;
            pair.duration_a = 0;
            pair.status_b = StatusType.Rooted;
            pair.value_b = 1;
            pair.duration_b = 1;
            pair.ally_trait_status = StatusType.Sharp;
            pair.ally_trait_value = 1;
            pair.ally_trait_duration = 2;
            pair.required_trait = traitSlime;
            attachHit.effects = new EffectData[] { pair };
            RegisterAbility(attachHit);
            attachPick.chain_abilities = new AbilityData[] { attachHit };
            RegisterAbility(attachPick);

            CardData attachCard = BaseCard("vc5_slime_attach", "粘液附着", CardType.Spell, 0, 0, 0, 0, 0);
            ApplyCardConfig(attachCard, cfg.GetCard("card_slime_attach"), new TraitData[] { fieldSlimeEffect, fieldQuick });
            attachCard.abilities = new AbilityData[] { attachPick };
            if (string.IsNullOrWhiteSpace(attachCard.text))
                attachCard.text = "【效果】对攻击距离+1内任意单位施加1层粘液，并无法移动，持续1阶段。\n【特殊效果】可对己方黏黏使用，效果变为获得1层尖锐，持续2阶段。";
            RegisterCard(attachCard);

            // 黏黏分裂
            AbilityData splitCast = BaseAbility("vc5_slime_split", "黏黏分裂", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            ConditionStatus hasSlime = ScriptableObject.CreateInstance<ConditionStatus>();
            hasSlime.has_status = StatusType.Slime;
            hasSlime.value = 1;
            hasSlime.oper = ConditionOperatorBool.IsTrue;
            splitCast.conditions_target = new ConditionData[] { cCard, hasSlime };
            EffectSummonFromTargetStatus splitEff = ScriptableObject.CreateInstance<EffectSummonFromTargetStatus>();
            splitEff.summon = spawn;
            splitEff.status = StatusType.Slime;
            splitEff.divisor = 2;
            splitEff.min_count = 1;
            splitEff.consume_all = true;
            splitCast.effects = new EffectData[] { splitEff };
            RegisterAbility(splitCast);

            CardData splitCard = BaseCard("vc5_slime_split_card", "黏黏分裂", CardType.Spell, 1, 0, 0, 0, 0);
            ApplyCardConfig(splitCard, cfg.GetCard("card_slime_split"), new TraitData[] { fieldSlime });
            splitCard.abilities = new AbilityData[] { splitCast };
            if (string.IsNullOrWhiteSpace(splitCard.text))
                splitCard.text = "【效果】消耗所有附着有粘液的角色身上的粘液，每消耗2层粘液，在其周围可召唤格召唤1只黏黏幼崽（至少1只）。\n【特殊效果】幼崽死亡时为击杀者附加1层粘液。";
            RegisterCard(splitCard);

            // 黏黏合体
            AbilityData mergeCast = BaseAbility("vc5_slime_merge", "黏黏合体", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            ConditionOwner ally = ScriptableObject.CreateInstance<ConditionOwner>();
            ally.oper = ConditionOperatorBool.IsTrue;
            ConditionTrait spawnTrait = ScriptableObject.CreateInstance<ConditionTrait>();
            spawnTrait.trait = traitSlimeSpawn;
            spawnTrait.oper = ConditionOperatorInt.GreaterEqual;
            spawnTrait.value = 0;
            mergeCast.conditions_target = new ConditionData[] { ally, spawnTrait };
            EffectMergeSummons mergeEff = ScriptableObject.CreateInstance<EffectMergeSummons>();
            mergeEff.trait = traitSlimeSpawn;
            mergeCast.effects = new EffectData[] { mergeEff };
            RegisterAbility(mergeCast);

            CardData mergeCard = BaseCard("vc5_slime_merge_card", "黏黏合体", CardType.Spell, 1, 0, 0, 0, 0);
            ApplyCardConfig(mergeCard, cfg.GetCard("card_slime_merge"), new TraitData[] { fieldSlime });
            mergeCard.abilities = new AbilityData[] { mergeCast };
            if (string.IsNullOrWhiteSpace(mergeCard.text))
                mergeCard.text = "【效果】以己方1只黏黏幼崽为对象，消灭其余黏黏幼崽，将被消灭幼崽的属性以加法叠加至对象身上。";
            RegisterCard(mergeCard);

            // 黏黏爆炸
            AbilityData boomPick = BaseAbility("vc5_slime_boom_pick", "选择施法者", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            boomPick.conditions_target = AllyCardConds();
            boomPick.effects = new EffectData[] { store };

            AbilityData boomCast = BaseAbility("vc5_slime_boom", "黏黏爆炸", AbilityTrigger.None, AbilityTarget.SelectTarget);
            boomCast.conditions_target = AllyCardConds();
            EffectExplodeByHP explodeEff = ScriptableObject.CreateInstance<EffectExplodeByHP>();
            explodeEff.range = 1;
            explodeEff.explode_trait = traitSlimeSpawn;
            explodeEff.explode_all_with_trait = true;
            boomCast.effects = new EffectData[] { explodeEff };
            RegisterAbility(boomCast);

            AbilityData boomExtra = BaseAbility("vc5_slime_boom_extra", "黏黏爆炸附加粘液", AbilityTrigger.None, AbilityTarget.LastTargeted);
            EffectApplyStatusRandom rndSlime = ScriptableObject.CreateInstance<EffectApplyStatusRandom>();
            rndSlime.status = StatusType.Slime;
            rndSlime.use_target_hp = true;
            boomExtra.effects = new EffectData[] { rndSlime };
            ConditionCasterTrait boomCasterIsSlime = ScriptableObject.CreateInstance<ConditionCasterTrait>();
            boomCasterIsSlime.trait = tmpSelectedIsSlime;
            boomCasterIsSlime.oper = ConditionOperatorInt.GreaterEqual;
            boomCasterIsSlime.value = 1;
            boomExtra.conditions_trigger = new ConditionData[] { boomCasterIsSlime };
            RegisterAbility(boomExtra);

            boomCast.chain_abilities = new AbilityData[] { boomExtra };
            boomPick.chain_abilities = new AbilityData[] { boomCast };
            RegisterAbility(boomPick);

            CardData boomCard = BaseCard("vc5_slime_boom_card", "黏黏爆炸", CardType.Spell, 0, 0, 0, 0, 0);
            ApplyCardConfig(boomCard, cfg.GetCard("card_slime_boom"), new TraitData[] { fieldSlime });
            boomCard.abilities = new AbilityData[] { boomPick };
            if (string.IsNullOrWhiteSpace(boomCard.text))
                boomCard.text = "【效果】以己方1个单位为对象发动，使用后该单位死亡，对周围1格内所有单位造成其当前生命值伤害；对黏黏幼崽使用时可同时引爆所有幼崽。\n【特殊效果】若使用者为黏黏，对场上随机存活敌人附加粘液，重复次数等于该单位生命值。";
            RegisterCard(boomCard);

            // 黏黏磨刀
            AbilityData sharpenPick = BaseAbility("vc5_slime_sharpen_pick", "选择单位", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            sharpenPick.conditions_target = AllyCardConds();
            EffectSharpenSlime sharpenEff = ScriptableObject.CreateInstance<EffectSharpenSlime>();
            sharpenEff.buff_status = StatusType.Vc5DealDamageBonus;
            sharpenEff.base_value = 2;
            sharpenEff.bonus_value = 2;
            sharpenEff.duration = 1;
            sharpenEff.consume_status = StatusType.Slime;
            sharpenEff.consume_count = 2;
            sharpenEff.required_trait = traitSlime;
            sharpenPick.effects = new EffectData[] { sharpenEff };
            RegisterAbility(sharpenPick);

            CardData sharpenCard = BaseCard("vc5_slime_sharpen", "黏黏磨刀", CardType.Spell, 1, 0, 0, 0, 0);
            ApplyCardConfig(sharpenCard, cfg.GetCard("card_slime_sharpen"), new TraitData[] { fieldSlime, fieldQuick });
            sharpenCard.abilities = new AbilityData[] { sharpenPick };
            if (string.IsNullOrWhiteSpace(sharpenCard.text))
                sharpenCard.text = "【效果】使用者本回合造成伤害+2。\n【特殊效果】若使用者为黏黏，额外消耗随机2层粘液，本回合造成伤害再+2。";
            RegisterCard(sharpenCard);

            // 黏黏快跑
            AbilityData runPick = BaseAbility("vc5_slime_run_pick", "选择我方单位", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            runPick.conditions_target = AllyCardConds();
            runPick.effects = new EffectData[] { store };
            AbilityData runMove = BaseAbility("vc5_slime_run_move", "移动2格", AbilityTrigger.None, AbilityTarget.SelectTarget);
            ConditionSlimeRunTarget runTarget = ScriptableObject.CreateInstance<ConditionSlimeRunTarget>();
            runTarget.base_range = 2;
            runTarget.triggerer_required_trait = traitSlime;
            runTarget.spawn_trait = traitSlimeSpawn;
            runMove.conditions_target = new ConditionData[] { runTarget };
            EffectMoveTriggererToTarget moveTriggerer = ScriptableObject.CreateInstance<EffectMoveTriggererToTarget>();
            runMove.effects = new EffectData[] { moveTriggerer };
            RegisterAbility(runMove);
            runPick.chain_abilities = new AbilityData[] { runMove };
            RegisterAbility(runPick);

            CardData runCard = BaseCard("vc5_slime_run", "黏黏快跑", CardType.Spell, 1, 0, 0, 0, 0);
            ApplyCardConfig(runCard, cfg.GetCard("card_slime_run"), new TraitData[] { fieldSlime });
            runCard.abilities = new AbilityData[] { runPick };
            if (string.IsNullOrWhiteSpace(runCard.text))
                runCard.text = "【效果】移动2格。\n【特殊效果】若使用者为黏黏，可直接移动到场上任意黏黏幼崽所在格。";
            RegisterCard(runCard);

            // 粘液引爆
            AbilityData detonate = BaseAbility("vc5_slime_detonate", "粘液引爆", AbilityTrigger.OnPlay, AbilityTarget.SelectTarget);
            detonate.conditions_target = new ConditionData[] { cCard };
            EffectDamageRepeatConsumeStatus repeat = ScriptableObject.CreateInstance<EffectDamageRepeatConsumeStatus>();
            repeat.status = StatusType.Slime;
            repeat.consume_per_hit = 1;
            detonate.effects = new EffectData[] { repeat };
            detonate.value = 1;
            RegisterAbility(detonate);

            CardData detonateCard = BaseCard("vc5_slime_detonate_card", "粘液引爆", CardType.Spell, 2, 0, 0, 0, 0);
            ApplyCardConfig(detonateCard, cfg.GetCard("card_slime_detonate"), new TraitData[] { fieldSlimeEffect });
            detonateCard.abilities = new AbilityData[] { detonate };
            if (string.IsNullOrWhiteSpace(detonateCard.text))
                detonateCard.text = "【效果】对任意单位造成1伤害。\n【特殊效果】若目标身上存在粘液，消耗1层粘液，再次执行该效果，直到无法继续。";
            RegisterCard(detonateCard);
        }

        private static void CreateSampleDecks()
        {
            // deck_slime_sample from Docs/vc5_excel_seed_v1/decks_sample.csv
            DeckData slimeDeck = BuildDeck(
                "deck_slime_sample",
                "黏黏示例卡组",
                new string[] { "vc5_hero_slime_corrosive", "vc5_hero_slime_corrosive", "vc5_hero_slime_hard" },
                new string[]
                {
                    "vc5_slime_heavy_strike:3",
                    "vc5_slime_combo_strike:3",
                    "vc5_slime_spray:3",
                    "vc5_slime_attach:3",
                    "vc5_slime_split_card:1",
                    "vc5_slime_merge_card:1",
                    "vc5_slime_boom_card:1",
                    "vc5_slime_detonate_card:2",
                    "vc5_slime_sharpen:2",
                    "vc5_slime_run:2"
                });
            RegisterDeck(slimeDeck);

            // Docs/黏黏试玩卡组1.docx — 正式游玩用（20 张，无测试模式加成）
            DeckData slimeTrialDeck = BuildDeck(
                "deck_slime_trial_1",
                "黏黏试玩卡组1",
                new string[] { "vc5_hero_slime_corrosive", "vc5_hero_slime_corrosive", "vc5_hero_slime_hard" },
                new string[]
                {
                    "vc5_slime_heavy_strike:3",
                    "vc5_slime_combo_strike:3",
                    "vc5_slime_spray:3",
                    "vc5_slime_attach:3",
                    "vc5_slime_boom_card:2",
                    "vc5_slime_detonate_card:2",
                    "vc5_slime_sharpen:2",
                    "vc5_slime_run:2"
                });
            RegisterDeck(slimeTrialDeck);

            // test deck for quickly validating heavy/combo/spray
            DeckData slimeThreeCardsDeck = BuildDeck(
                "deck_slime_3cards_test",
                "黏黏三牌测试",
                new string[] { "vc5_hero_slime_corrosive", "vc5_hero_slime_corrosive", "vc5_hero_slime_hard" },
                new string[]
                {
                    "vc5_slime_heavy_strike:10",
                    "vc5_slime_combo_strike:10",
                    "vc5_slime_spray:10"
                });
            RegisterDeck(slimeThreeCardsDeck);

            // Vol.01 full slime test deck: test mode gives all cards in opening hand, 99 mana, and no main-action limit.
            DeckData slimeVol01TestDeck = BuildDeck(
                "deck_slime_vol01_test",
                "黏黏Vol.01全卡测试",
                new string[] { "vc5_hero_slime_corrosive", "vc5_hero_slime_hard2", "vc5_hero_slime_hard" },
                new string[]
                {
                    "vc5_slime_heavy_strike:1",
                    "vc5_slime_combo_strike:1",
                    "vc5_slime_pool_spell:1",
                    "vc5_slime_spray:1",
                    "vc5_slime_attach:1",
                    "vc5_slime_split_card:1",
                    "vc5_slime_merge_card:1",
                    "vc5_slime_boom_card:1",
                    "vc5_slime_sharpen:1",
                    "vc5_slime_run:1",
                    "vc5_slime_detonate_card:1"
                });
            RegisterDeck(slimeVol01TestDeck);

            // deck_warrior_sample from Docs/vc5_excel_seed_v1/decks_sample.csv
            // Warrior cards may not exist yet; unresolved ids are skipped so deck can still be selected for iterative testing.
            DeckData warriorDeck = BuildDeck(
                "deck_warrior_sample",
                "战士示例卡组",
                new string[] { "hero_warrior_blueeye", "hero_warrior_berserker", "hero_warrior_guard" },
                new string[]
                {
                    "card_warrior_charge_brave:2",
                    "card_warrior_charge_fearless:2",
                    "card_warrior_heavy:3",
                    "card_warrior_rage:3",
                    "card_warrior_slash:3",
                    "card_warrior_soul:1",
                    "card_warrior_blood_spin:1",
                    "card_warrior_roar:1",
                    "card_warrior_sacrifice:3",
                    "card_warrior_heart:3"
                });
            if (warriorDeck.cards.Length == 0)
            {
                // fallback so deck remains playable in TestP2P before warrior implementation is finished
                warriorDeck.cards = new CardData[]
                {
                    CardData.Get("vc5_slime_heavy_strike"), CardData.Get("vc5_slime_heavy_strike"),
                    CardData.Get("vc5_slime_combo_strike"), CardData.Get("vc5_slime_combo_strike"),
                    CardData.Get("vc5_slime_spray"), CardData.Get("vc5_slime_spray"),
                    CardData.Get("vc5_slime_attach"), CardData.Get("vc5_slime_attach"),
                    CardData.Get("vc5_slime_split_card"), CardData.Get("vc5_slime_merge_card"),
                    CardData.Get("vc5_slime_boom_card"), CardData.Get("vc5_slime_detonate_card"),
                    CardData.Get("vc5_slime_sharpen"), CardData.Get("vc5_slime_run"),
                };
            }
            RegisterDeck(warriorDeck);
        }

        private static DeckData BuildDeck(string id, string title, string[] heroIds, string[] cardPairs)
        {
            DeckData deck = ScriptableObject.CreateInstance<DeckData>();
            deck.id = id;
            deck.title = title;

            List<CardData> heroes = new List<CardData>();
            foreach (string heroId in heroIds)
            {
                CardData h = CardData.Get(heroId);
                if (h != null)
                    heroes.Add(h);
            }
            while (heroes.Count < 3)
                heroes.Add(heroes.Count > 0 ? heroes[0] : CardData.Get("elf_swordsman"));
            deck.heroes = new CardData[] { heroes[0], heroes[1], heroes[2] };
            deck.hero = deck.heroes[0];

            List<CardData> cards = new List<CardData>();
            foreach (string pair in cardPairs)
            {
                string[] sp = pair.Split(':');
                if (sp.Length != 2)
                    continue;
                string cid = sp[0];
                if (!int.TryParse(sp[1], out int qty))
                    qty = 0;
                CardData card = CardData.Get(cid);
                if (card == null)
                    continue;
                for (int i = 0; i < qty; i++)
                    cards.Add(card);
            }
            deck.cards = cards.ToArray();
            deck.monsters = new CardData[0];
            return deck;
        }

        private static void RegisterDeck(DeckData deck)
        {
            if (deck == null)
                return;

            DeckData existing = DeckData.Get(deck.id);
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
            List<DeckData> free = new List<DeckData>(gdata.free_decks ?? new DeckData[0]);
            bool found = false;
            foreach (DeckData d in free)
            {
                if (d != null && d.id == deck.id)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                free.Add(deck);
                gdata.free_decks = free.ToArray();
            }
        }
    }
}
