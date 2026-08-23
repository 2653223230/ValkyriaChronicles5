using System.Collections;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    public class AIPlayerVc5Demo : AIPlayer
    {
        private bool is_playing = false;

        public AIPlayerVc5Demo(GameLogic gameplay, int id, int level)
        {
            this.gameplay = gameplay;
            player_id = id;
            ai_level = Mathf.Clamp(level, 1, 10);
        }

        public override void Update()
        {
            if (!CanPlay() || is_playing)
                return;

            Game data = gameplay.GetGameData();
            Player player = data.GetPlayer(player_id);
            if (player == null)
                return;

            if (data.phase == GamePhase.EndDiscard && !player.end_discard_passed)
            {
                is_playing = true;
                TimeTool.StartCoroutine(AiEndDiscard());
                return;
            }

            if ((data.current_player == player_id || data.selector_player_id == player_id) && data.IsPlayerTurn(player))
            {
                is_playing = true;
                TimeTool.StartCoroutine(AiStep());
            }
        }

        private IEnumerator AiStep()
        {
            yield return new WaitForSeconds(0.35f);
            ExecuteAction(Vc5DemoAIPlanner.ChooseNextAction(gameplay.GetGameData(), player_id));
            yield return new WaitForSeconds(0.15f);
            is_playing = false;
        }

        private IEnumerator AiEndDiscard()
        {
            yield return new WaitForSeconds(0.25f);
            Player player = gameplay.GetGameData().GetPlayer(player_id);
            if (player != null)
                gameplay.PassEndDiscard(player);
            is_playing = false;
        }

        private void ExecuteAction(AIAction action)
        {
            if (action == null || !CanPlay())
                return;

            Game data = gameplay.GetGameData();
            Player player = data.GetPlayer(player_id);

            if (action.type == GameAction.PlayCard)
            {
                Card card = data.GetCard(action.card_uid);
                if (card != null)
                    gameplay.PlayCard(card, action.slot);
            }
            else if (action.type == GameAction.CastAbility)
            {
                Card card = data.GetCard(action.card_uid);
                AbilityData ability = AbilityData.Get(action.ability_id);
                if (card != null && ability != null)
                    gameplay.CastAbility(card, ability);
            }
            else if (action.type == GameAction.SelectCard)
            {
                Card target = data.GetCard(action.target_uid);
                if (target != null)
                    gameplay.SelectCard(target);
            }
            else if (action.type == GameAction.SelectSlot)
            {
                if (action.slot.IsValid())
                    gameplay.SelectSlot(action.slot);
            }
            else if (action.type == GameAction.SelectChoice)
            {
                gameplay.SelectChoice(action.value);
            }
            else if (action.type == GameAction.SelectCost)
            {
                gameplay.SelectCost(action.value);
            }
            else if (action.type == GameAction.CancelSelect)
            {
                gameplay.CancelSelection();
            }
            else if (action.type == GameAction.EndTurn)
            {
                if (data.phase == GamePhase.EndDiscard)
                    gameplay.PassEndDiscard(player);
                else
                    gameplay.EndTurn();
            }
        }
    }
}
