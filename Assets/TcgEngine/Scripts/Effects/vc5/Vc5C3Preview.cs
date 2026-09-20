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
        public static Vc5C3Preview Build(Game game, Card spell, Card actor, Slot? destination = null, Card target = null)
        {
            bool moveSkill = spell != null && spell.card_id == Vc5R4Rules.Ranger && spell == actor;
            bool bai1 = Vc5BAI1Rules.IsCard(spell);
            var preview = new Vc5C3Preview { plan = bai1 ? Vc5BAI1Rules.Plan(game, spell, actor, destination, target)
                : moveSkill ? MoveSkillPlan(game, actor, destination) : Vc5C3Rules.Plan(game, spell, actor, destination) };
            if (!preview.plan.valid || ((moveSkill || Vc5C3Rules.IsPreciseMove(spell)) && !destination.HasValue)) return preview;
            // Run the real resolver on an isolated state, including shield consumption and death retargeting.
            Game clone = Game.CloneNew(game);
            var logic = new PredictionLogic(preview);
            logic.SetData(clone);
            if (moveSkill) logic.MoveCard(clone.GetCard(actor.uid), destination.Value, true, true);
            else if (bai1)
            {
                Card copyActor = clone.GetCard(actor.uid);
                if (Vc5BAI1Rules.IsAttack(spell) && target == null)
                {
                    // Selecting a landing is only a preview; don't invent an attack target.
                    if (preview.plan.path.Count > 1) logic.MoveCard(copyActor, preview.plan.destination, true, true);
                }
                else Vc5BAI1Rules.Execute(logic, clone.GetCard(spell.uid), copyActor, destination, target == null ? null : clone.GetCard(target.uid));
                if (clone.IsOnBoard(copyActor) && copyActor.r4_shield != actor.r4_shield)
                    preview.sequence.Add("护盾：" + actor.r4_shield + " → " + copyActor.r4_shield + "（至下回合结束）");
                if (target != null && actor.card_id == Vc5BAI1Rules.Rifleman && actor.CanDoAbilities()
                    && Vc5BAI1Rules.Damage(game, spell, actor, target) > Vc5C3Rules.CardDamage(actor, spell.card_id.EndsWith("suppression") ? 1 : 0))
                    preview.sequence.Add("协同射击：伤害 +1");
            }
            else Vc5C3Rules.Execute(logic, clone.GetCard(spell.uid), clone.GetCard(actor.uid), destination);
            Card resultingActor = clone.GetCard(actor.uid);
            if (!clone.IsOnBoard(resultingActor) && preview.plan.path.Count > 1)
            {
                preview.plan.destination = resultingActor.slot;
                int stop = preview.plan.path.IndexOf(resultingActor.slot);
                if (stop >= 0) preview.plan.path.RemoveRange(stop + 1, preview.plan.path.Count - stop - 1);
                preview.sequence.Add("移动途中被击破，后续效果不执行");
            }
            if (actor.CanDoAbilities())
            {
                if (Vc5C3Rules.IsRanger(actor) && preview.plan.path.Count > 1 && clone.IsOnBoard(resultingActor))
                    preview.sequence.Insert(0, moveSkill || (Vc5C3Rules.IsPreciseMove(spell) && !Vc5BAI1Rules.IsAttack(spell)) ? "机动火力：下一张伤害牌 +1" : "机动火力：本次伤害 +1");
                if (Vc5C3Rules.IsSniper(actor))
                    preview.sequence.Insert(0, preview.plan.path.Count > 1 || actor.HasStatus(StatusType.Vc5C3MovedThisTurn)
                        ? "已移动：本回合无稳固加伤" : "稳固射击：伤害 +1");
            }
            return preview;
        }

        static Vc5C3Plan MoveSkillPlan(Game game, Card actor, Slot? destination)
        {
            var plan = new Vc5C3Plan { destination = actor.slot, range = Vc5DemoGrid.AttackRange(actor) };
            var paths = Vc5C3Rules.Reachable(game, actor, 1);
            foreach (var pair in paths) if (pair.Value.Count > 1) plan.legalSlots.Add(pair.Key);
            plan.valid = destination.HasValue ? plan.legalSlots.Contains(destination.Value) : plan.legalSlots.Count > 0;
            if (plan.valid && destination.HasValue)
            { plan.destination = destination.Value; plan.path.AddRange(paths[destination.Value]); }
            plan.reason = plan.valid ? "侧翼机动：1 法力，快速；选择相邻空格" : "没有合法移动路线";
            return plan;
        }

        sealed class PredictionLogic : GameLogic
        {
            readonly Vc5C3Preview preview;
            bool moving;
            bool watchShot;
            public PredictionLogic(Vc5C3Preview preview) : base(true)
            {
                this.preview = preview;
                onAbilityTargetCard += (ability, caster, target) => watchShot = ability != null && ability.id == Vc5R4Rules.WatchReaction;
            }
            public override void MoveCard(Card card, Slot slot, bool skip_cost = false, bool ignore_range = false)
            {
                moving = true;
                try { base.MoveCard(card, slot, skip_cost, ignore_range); }
                finally { moving = false; }
            }
            public override void DamageCard(Card attacker, Card target, int value, bool spell_damage = false)
            {
                int before = target.damage;
                int shield = target.r4_shield;
                bool watch = watchShot;
                watchShot = false;
                base.DamageCard(attacker, target, value, spell_damage);
                int dealt = Mathf.Max(0, target.damage - before);
                preview.damage.TryGetValue(target.uid, out int previous);
                preview.damage[target.uid] = previous + dealt;
                preview.sequence.Add((watch ? "警戒射击" : moving ? "移动触发伤害" : "卡牌伤害") + "：" + target.CardData.title + " -" + dealt
                    + (shield > target.r4_shield ? "（护盾吸收 " + (shield - target.r4_shield) + "）" : ""));
            }
        }
    }
}
