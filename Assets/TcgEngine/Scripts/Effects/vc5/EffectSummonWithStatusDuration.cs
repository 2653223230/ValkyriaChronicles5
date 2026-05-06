using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/SummonWithStatusDuration", order = 10)]
    public class EffectSummonWithStatusDuration : EffectData
    {
        public CardData summon;
        public StatusType status;
        public int base_duration = 2;
        public TraitData bonus_trait;
        public int bonus_duration = 1;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            if (summon == null)
                return;
            Player player = logic.GameData.GetPlayer(caster.player_id);
            Card summoned = logic.SummonCard(player, summon, caster.VariantData, target);
            if (summoned == null)
                return;

            int duration = base_duration;
            if (bonus_trait != null && logic.GameData.PlayerHasTraitOnBoard(caster.player_id, bonus_trait.id))
                duration += bonus_duration;

            summoned.AddStatus(status, 1, duration);
        }
    }
}
