namespace TcgEngine
{
    /// <summary>VC5 英雄技能冷却：在 Card 上记录某能力上次发动时的 turn_count。</summary>
    public static class Vc5AbilityTurnTracker
    {
        public const string HardAwakeKey = "vc5_hard_awake_last";

        public static void RecordUse(Game data, Card caster, string trackKey)
        {
            if (data == null || caster == null || string.IsNullOrEmpty(trackKey))
                return;
            caster.SetTrait(trackKey, data.turn_count);
        }
    }
}
