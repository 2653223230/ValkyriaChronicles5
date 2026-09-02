using TcgEngine.Gameplay;

namespace TcgEngine
{
    public class EffectVc5C3Action : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Vc5C3Rules.Execute(logic, caster, target);
        }
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            Vc5C3Rules.Execute(logic, caster, logic.GameData.GetCard(logic.GameData.ability_triggerer), target);
        }
    }
}
