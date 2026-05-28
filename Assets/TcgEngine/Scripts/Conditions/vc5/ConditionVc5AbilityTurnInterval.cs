using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 距上次记录回合至少间隔 N 个完整回合后才可再次发动（用于坚硬黏黏觉醒：每 2 回合 1 次）。
    /// 配合 <see cref="Vc5AbilityTurnTracker"/> 在能力结算后写入回合数。
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/VC5/AbilityTurnInterval", order = 10)]
    public class ConditionVc5AbilityTurnInterval : ConditionData
    {
        public string track_key = Vc5AbilityTurnTracker.HardAwakeKey;
        public int min_turn_interval = 2;

        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            if (caster == null || data == null)
                return false;
            int last = caster.GetTraitValue(track_key);
            if (last <= 0)
                return true;
            return data.turn_count - last >= min_turn_interval;
        }
    }
}
