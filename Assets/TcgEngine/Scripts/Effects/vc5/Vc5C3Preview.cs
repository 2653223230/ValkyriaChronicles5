using System.Collections.Generic;
using TcgEngine.Gameplay;
using UnityEngine;

namespace TcgEngine
{
    public sealed class Vc5C3Preview
    {
        public Vc5C3Plan plan;
        public readonly Dictionary<string, int> damage = new Dictionary<string, int>();
        public readonly List<string> sequence = new List<string>();
        public static Vc5C3Preview Build(Game game, Card spell, Card actor, Slot? destination = null)
        {
            var preview = new Vc5C3Preview { plan = Vc5C3Rules.Plan(game, spell, actor, destination) };
            if (!preview.plan.valid || (Vc5C3Rules.IsPreciseMove(spell) && !destination.HasValue)) return preview;
            // Run the real resolver on an isolated state, including shield consumption and death retargeting.
            Game clone = Game.CloneNew(game);
            var logic = new PredictionLogic(preview);
            logic.SetData(clone);
            Vc5C3Rules.Execute(logic, clone.GetCard(spell.uid), clone.GetCard(actor.uid), destination);
            if (actor.CanDoAbilities())
            {
                if (actor.card_id == Vc5C3Rules.Ranger && preview.plan.path.Count > 1)
                    preview.sequence.Insert(0, Vc5C3Rules.IsPreciseMove(spell) ? "机动火力：下一张伤害牌 +1" : "机动火力：本次伤害 +1");
                if (actor.card_id == Vc5C3Rules.Sniper)
                    preview.sequence.Insert(0, preview.plan.path.Count > 1 || actor.HasStatus(StatusType.Vc5C3MovedThisTurn)
                        ? "已移动：本回合无稳固加伤" : "稳固射击：伤害 +1");
            }
            return preview;
        }

        sealed class PredictionLogic : GameLogic
        {
            readonly Vc5C3Preview preview;
            bool moving;
            public PredictionLogic(Vc5C3Preview preview) : base(true) { this.preview = preview; }
            public override void MoveCard(Card card, Slot slot, bool skip_cost = false, bool ignore_range = false)
            {
                moving = true;
                try { base.MoveCard(card, slot, skip_cost, ignore_range); }
                finally { moving = false; }
            }
            public override void DamageCard(Card attacker, Card target, int value, bool spell_damage = false)
            {
                int before = target.damage;
                base.DamageCard(attacker, target, value, spell_damage);
                int dealt = Mathf.Max(0, target.damage - before);
                preview.damage.TryGetValue(target.uid, out int previous);
                preview.damage[target.uid] = previous + dealt;
                preview.sequence.Add((moving ? "行进警戒" : "卡牌伤害") + "：" + target.CardData.title + " -" + dealt);
            }
        }
    }
}
