namespace TcgEngine.Gameplay
{
    public partial class GameLogic
    {
        private bool CanFinishR4Skill(Card caster, string skillId)
        {
            AbilityData skill = AbilityData.Get(skillId);
            return caster != null && game_data.IsOnBoard(caster) && caster.GetHP() > 0
                && game_data.phase == GamePhase.Main && game_data.current_player == caster.player_id
                && !game_data.GetPlayer(caster.player_id).EndTurn
                && caster.CanDoActivatedAbilities() && !caster.IsAbilityOnCooldown(skill)
                && game_data.GetPlayer(caster.player_id).CanPayAbility(caster, skill);
        }

        private bool TrySelectR4Move(Slot destination)
        {
            if (game_data.selector != SelectorType.SelectTarget || game_data.selector_ability_id != Vc5R4Rules.MoveSkill) return false;
            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData skill = AbilityData.Get(Vc5R4Rules.MoveSkill);
            if (!CanFinishR4Skill(caster, skill.id) || !skill.CanTarget(game_data, caster, destination)) return true;
            game_data.GetPlayer(caster.player_id).mana -= skill.mana_cost;
            caster.IncrementAbilityUse(skill.id);
            game_data.selector = SelectorType.None;
            onAbilityStart?.Invoke(skill, caster);
            MoveCard(caster, destination, true, true);
            onAbilityEnd?.Invoke(skill, caster);
            RefreshData();
            return true;
        }

        private bool TrySelectR4Discard(Card target)
        {
            if (game_data.selector != SelectorType.SelectorCard || game_data.selector_ability_id != Vc5R4Rules.PrepareSkill) return false;
            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            if (!CanFinishR4Skill(caster, Vc5R4Rules.PrepareSkill) || !Vc5R4Rules.CanDiscard(game_data, caster, target)) return true;
            caster.r4_pending_discard_uid = target.uid;
            GoToSelectorChoice(AbilityData.Get(Vc5R4Rules.ChooseOrder), caster);
            return true;
        }

        private bool TrySelectR4Order(int choice)
        {
            if (game_data.selector != SelectorType.SelectorChoice || game_data.selector_ability_id != Vc5R4Rules.ChooseOrder) return false;
            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            if (!CanFinishR4Skill(caster, Vc5R4Rules.PrepareSkill) || choice < 0 || choice >= Vc5R4Rules.Orders.Length) return true;
            Card discarded = game_data.GetCard(caster.r4_pending_discard_uid);
            if (!Vc5R4Rules.CanDiscard(game_data, caster, discarded)) return true;
            game_data.selector = SelectorType.None;
            caster.r4_pending_discard_uid = null;
            caster.IncrementAbilityUse(Vc5R4Rules.PrepareSkill);
            caster.r4_watch = false;
            onAbilityStart?.Invoke(AbilityData.Get(Vc5R4Rules.PrepareSkill), caster);
            DiscardCard(discarded);
            Card order = SummonCardHand(game_data.GetPlayer(caster.player_id), CardData.Get(Vc5R4Rules.Orders[choice]), caster.VariantData);
            order.r4_commander_uid = caster.uid;
            onAbilityEnd?.Invoke(AbilityData.Get(Vc5R4Rules.PrepareSkill), caster);
            RefreshData();

            return true;
        }
    }
}
