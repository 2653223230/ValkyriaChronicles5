using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/VC5/ApplyStatusRandom", order = 10)]
    public class EffectApplyStatusRandom : EffectData
    {
        public StatusType status;
        public bool use_target_hp;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int count = use_target_hp && target != null ? Mathf.Max(target.GetHP(), logic.GameData.selected_value) : ability.value;
            if (count <= 0)
                return;

            List<Card> enemies = new List<Card>();
            foreach (Player player in logic.GameData.players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card.player_id != caster.player_id)
                        enemies.Add(card);
                }
            }

            if (enemies.Count == 0)
                return;

            System.Random rand = logic.GetRandom();
            for (int i = 0; i < count; i++)
            {
                Card chosen = enemies[rand.Next(0, enemies.Count)];
                chosen.AddStatus(status, 1, 0);
            }
        }
    }
}
