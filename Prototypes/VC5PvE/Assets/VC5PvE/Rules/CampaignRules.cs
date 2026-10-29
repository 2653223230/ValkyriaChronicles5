using System;
using System.Collections.Generic;
using System.Linq;

namespace VC5PvE
{
    public enum TalentKind
    {
        CounterReady,
        FlankingStrike,
        MarkFlow,
        NeutralMastery,
        SupportFlow,
        TacticalRecall
    }

    public sealed class TalentDefinition
    {
        public TalentKind Kind;
        public UnitRole Role;
        public string Name;
        public string Description;
        public CardKind RewardCard;
    }

    [Serializable]
    public sealed class RunState
    {
        public int LevelIndex = 1;
        public int Seed;
        public List<CardInstance> Deck = new List<CardInstance>();
        public Dictionary<UnitRole, List<TalentKind>> Talents = new Dictionary<UnitRole, List<TalentKind>>();
        public int CampStep;
        public List<CardKind> Offers = new List<CardKind>();
        public List<string> GrowthHistory = new List<string>();
        public bool CampStarted;
        public int CompletedCamps;
        public TalentKind? CampTalent;
        public bool Finished;
    }

    public static class CampaignRules
    {
        private static readonly TalentDefinition[] TalentDefinitions =
        {
            new TalentDefinition { Kind = TalentKind.CounterReady, Role = UnitRole.Warrior, Name = "反攻准备", Description = "敌方回合中自身护盾挡住伤害后，下回合首张高费攻击牌少花1点AP。", RewardCard = CardKind.AllyCover },
            new TalentDefinition { Kind = TalentKind.FlankingStrike, Role = UnitRole.Warrior, Name = "并肩强袭", Description = "每回合首张攻击牌若目标旁有另一名友军，伤害+1。", RewardCard = CardKind.RelayStrike },
            new TalentDefinition { Kind = TalentKind.MarkFlow, Role = UnitRole.Mage, Name = "印记回流", Description = "每回合首次由友军出牌消耗印记后，抽1张牌。", RewardCard = CardKind.RelayStrike },
            new TalentDefinition { Kind = TalentKind.NeutralMastery, Role = UnitRole.Mage, Name = "中立专精", Description = "每回合首张高费中立攻击牌少花1点AP。", RewardCard = CardKind.RecoverTactics },
            new TalentDefinition { Kind = TalentKind.SupportFlow, Role = UnitRole.Support, Name = "支援续牌", Description = "每回合首次用牌有效支援队友后，抽1张牌。", RewardCard = CardKind.AllyCover },
            new TalentDefinition { Kind = TalentKind.TacticalRecall, Role = UnitRole.Support, Name = "战术回收", Description = "每回合首次用牌有效支援队友后，可收回1张中立攻击牌。", RewardCard = CardKind.RecoverTactics }
        };

        private static readonly UnitRole[] PlayerRoles = { UnitRole.Warrior, UnitRole.Mage, UnitRole.Support };
        private static readonly CardKind[] OriginalCardKinds =
        {
            CardKind.Advance, CardKind.HeavyAttack, CardKind.Cover, CardKind.Charge,
            CardKind.SparkMark, CardKind.StarBurst, CardKind.Inspire
        };
        private static readonly CardKind[] RewardPool =
        {
            CardKind.Advance, CardKind.HeavyAttack, CardKind.Cover, CardKind.Charge,
            CardKind.SparkMark, CardKind.StarBurst, CardKind.Inspire,
            CardKind.RelayStrike, CardKind.AllyCover, CardKind.RecoverTactics
        };

        public static RunState NewRun(int seed)
        {
            var run = new RunState { Seed = seed, LevelIndex = 1 };
            foreach (var role in PlayerRoles) run.Talents.Add(role, new List<TalentKind>());
            foreach (var kind in OriginalCardKinds)
            {
                var copies = Definitions.Card(kind).Copies;
                for (var copy = 1; copy <= copies; copy++)
                    run.Deck.Add(new CardInstance { Id = BattleFactory.CardId(kind, copy), Kind = kind });
            }
            return run;
        }

        public static BattleState CreateBattle(RunState run)
        {
            if (run == null || run.LevelIndex < 1 || run.LevelIndex > 3 || run.Finished) return null;
            var battle = BattleFactory.CreateLevel(run.LevelIndex, unchecked(run.Seed + run.LevelIndex * 7919));
            if (battle == null) return null;
            battle.Hand.Clear();
            battle.DrawPile.Clear();
            battle.DiscardPile.Clear();
            foreach (var role in PlayerRoles)
            {
                var unit = battle.Units.FirstOrDefault(value => value.Role == role && value.Team == Team.Player);
                if (unit == null) continue;
                unit.Talents = run.Talents.ContainsKey(role) ? new List<TalentKind>(run.Talents[role]) : new List<TalentKind>();
                unit.Hp = unit.MaxHp;
                unit.Ap = unit.MaxAp;
            }

            var remaining = run.Deck.Where(card => card != null).Select(card => card.Clone()).ToList();
            var openingKinds = new[] { CardKind.Advance, CardKind.HeavyAttack, CardKind.Cover, CardKind.SparkMark, CardKind.StarBurst, CardKind.Inspire };
            foreach (var kind in openingKinds)
            {
                var selected = remaining.Where(card => card.Kind == kind)
                    .OrderByDescending(card => card.Upgraded)
                    .ThenBy(card => card.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (selected == null) continue;
                battle.Hand.Add(selected);
                remaining.Remove(selected);
            }
            Shuffle(remaining, unchecked(run.Seed + run.LevelIndex * 104729));
            while (battle.Hand.Count < 6 && remaining.Count > 0)
            {
                battle.Hand.Add(remaining[0]);
                remaining.RemoveAt(0);
            }
            battle.DrawPile.AddRange(remaining);
            return battle;
        }

        public static bool BeginCamp(RunState run)
        {
            if (run == null || run.Finished || run.LevelIndex >= 3 || run.CampStarted || run.CompletedCamps >= 2) return false;
            run.CampStep = 0;
            run.CampStarted = true;
            run.CampTalent = null;
            run.Offers.Clear();
            return true;
        }

        public static bool ChooseTalent(RunState run, TalentKind talent)
        {
            if (!AtCampStep(run, 0)) return false;
            var definition = Talent(talent);
            if (definition == null) return false;
            List<TalentKind> owned;
            if (!run.Talents.TryGetValue(definition.Role, out owned))
            {
                owned = new List<TalentKind>();
                run.Talents[definition.Role] = owned;
            }
            if (owned.Contains(talent) || owned.Count >= 2) return false;
            owned.Add(talent);
            run.CampTalent = talent;
            run.GrowthHistory.Add("天赋：" + definition.Name);
            BuildOffers(run, definition);
            run.CampStep = 1;
            return true;
        }

        public static bool ChooseReward(RunState run, CardKind? reward)
        {
            if (!AtCampStep(run, 1)) return false;
            if (reward.HasValue && !run.Offers.Contains(reward.Value)) return false;
            if (reward.HasValue)
            {
                run.Deck.Add(new CardInstance
                {
                    Id = "reward-" + (run.CompletedCamps + 1).ToString("00") + "-" + reward.Value.ToString().ToLowerInvariant(),
                    Kind = reward.Value
                });
                run.GrowthHistory.Add("新卡：" + Definitions.Card(reward.Value).Name);
            }
            run.CampStep = 2;
            return true;
        }

        public static bool MaintainDeck(RunState run, string cardId, bool upgrade)
        {
            if (!AtCampStep(run, 2)) return false;
            if (string.IsNullOrEmpty(cardId))
            {
                if (upgrade) return false;
                run.GrowthHistory.Add("整备：跳过");
                run.CampStep = 3;
                return true;
            }
            var card = run.Deck.FirstOrDefault(value => value != null && value.Id == cardId);
            if (card == null) return false;
            if (upgrade)
            {
                if (card.Upgraded) return false;
                card.Upgraded = true;
                run.GrowthHistory.Add("升级：" + CardDisplayName(card));
            }
            else
            {
                if (run.Deck.Count <= 15) return false;
                run.Deck.Remove(card);
                run.GrowthHistory.Add("删牌：" + CardDisplayName(card));
            }
            run.CampStep = 3;
            return true;
        }

        public static bool Advance(RunState run)
        {
            if (!AtCampStep(run, 3)) return false;
            run.CompletedCamps++;
            run.CampStarted = false;
            run.CampStep = 0;
            run.Offers.Clear();
            run.CampTalent = null;
            if (run.LevelIndex < 3) run.LevelIndex++;
            else run.Finished = true;
            return true;
        }

        public static TalentDefinition Talent(TalentKind kind)
        { return TalentDefinitions.FirstOrDefault(value => value.Kind == kind); }

        public static List<TalentDefinition> TalentsFor(UnitRole role)
        { return TalentDefinitions.Where(value => value.Role == role).ToList(); }

        public static string LevelName(int level)
        {
            switch (level) { case 1: return "林地入口"; case 2: return "双阵遗迹"; case 3: return "钟庭守卫"; default: return string.Empty; }
        }

        public static string LevelHint(int level)
        {
            switch (level)
            {
                case 1: return "沿D列压前线或走东侧通路。";
                case 2: return "可先拆绑定守卫的护阵柱，也可直接集火守卫。";
                case 3: return "摧毁共鸣锚可开启下一己方回合的首领减伤中断。";
                default: return string.Empty;
            }
        }

        private static bool AtCampStep(RunState run, int step)
        { return run != null && !run.Finished && run.CampStarted && run.CampStep == step; }

        private static string CardDisplayName(CardInstance card)
        {
            return Definitions.Card(card).Name;
        }

        private static void BuildOffers(RunState run, TalentDefinition chosen)
        {
            run.Offers.Clear();
            run.Offers.Add(chosen.RewardCard);
            var candidates = RewardPool.Where(kind => kind != chosen.RewardCard).ToList();
            var rng = new Random(unchecked(run.Seed * 486187739 + run.CompletedCamps * 16777619 + (int)chosen.Kind));
            Shuffle(candidates, rng);
            run.Offers.Add(candidates[0]);
            run.Offers.Add(candidates[1]);
        }

        private static void Shuffle<T>(IList<T> values, int seed)
        { Shuffle(values, new Random(seed)); }

        private static void Shuffle<T>(IList<T> values, Random random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var item = values[i]; values[i] = values[j]; values[j] = item;
            }
        }
    }
}
