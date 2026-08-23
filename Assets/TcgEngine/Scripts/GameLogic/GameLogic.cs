using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// Execute and resolves game rules and logic
    /// 执行并解析游戏规则和逻辑
    /// </summary>

    public class GameLogic
    {
        public UnityAction onGameStart;
        public UnityAction<Player> onGameEnd;          //Winner

        public UnityAction onTurnStart;
        public UnityAction onTurnPlay;
        public UnityAction onTurnEnd;

        public UnityAction<Card, Slot> onCardPlayed;
        public UnityAction<Card, Slot> onCardSummoned;
        public UnityAction<Card, Slot> onCardMoved;
        public UnityAction<Card> onCardTransformed;
        public UnityAction<Card> onCardDiscarded;
        public UnityAction<int> onCardDrawn;
        public UnityAction<int> onRollValue;

        public UnityAction<AbilityData, Card> onAbilityStart;
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;  //Ability, Caster, Target
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;
        public UnityAction<AbilityData, Card> onAbilityEnd;

        public UnityAction<Card, Card> onAttackStart;  //Attacker, Defender
        public UnityAction<Card, Card> onAttackEnd;     //Attacker, Defender
        public UnityAction<Card, Player> onAttackPlayerStart;
        public UnityAction<Card, Player> onAttackPlayerEnd;

        public UnityAction<Card, int> onCardDamaged;
        public UnityAction<Card, int> onCardHealed;
        public UnityAction<Player, int> onPlayerDamaged;
        public UnityAction<Player, int> onPlayerHealed;

        public UnityAction<Card, Card> onSecretTrigger;    //Secret, Triggerer
        public UnityAction<Card, Card> onSecretResolve;    //Secret, Triggerer

        public UnityAction onRefresh;

        private Game game_data;

        private ResolveQueue resolve_queue;
        private bool is_ai_predict = false;

        private const string TraitSlime = "slime";
        private const string TraitSlimeBlood = "slime_blood";
        private const string TraitSlimeCorrosive = "slime_corrosive";
        private const string TraitSlimeHard = "slime_hard";
        private const string TraitSlimeSpawn = "slime_spawn";

        private System.Random random = new System.Random();

        private ListSwap<Card> card_array = new ListSwap<Card>();
        private ListSwap<Player> player_array = new ListSwap<Player>();
        private ListSwap<Slot> slot_array = new ListSwap<Slot>();
        private ListSwap<CardData> card_data_array = new ListSwap<CardData>();
        private List<Card> cards_to_clear = new List<Card>();
        private HashSet<string> pending_play_cards = new HashSet<string>();
        private Dictionary<string, List<Card>> pending_play_discards = new Dictionary<string, List<Card>>();
        private HashSet<string> pending_play_main_action = new HashSet<string>();

        public GameLogic(bool is_ai)
        {
            //is_instant ignores all gameplay delays and process everything immediately, needed for AI prediction
            resolve_queue = new ResolveQueue(null, is_ai);
            is_ai_predict = is_ai;
        }

        public GameLogic(Game game)
        {
            game_data = game;
            resolve_queue = new ResolveQueue(game, false);
        }

        public virtual void SetData(Game game)
        {
            game_data = game;
            resolve_queue.SetData(game);
        }

        public virtual void Update(float delta)
        {
            resolve_queue.Update(delta);
        }

        //----- Turn Phases ----------
        //----- 回合阶段 ----------

        //开始游戏
        public virtual void StartGame()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            //Choose first player
            //选择第一个玩家
            game_data.state = GameState.Play;
            game_data.first_player = IsVc5DemoSoloMatch() ? 0 : (random.NextDouble() < 0.5 ? 0 : 1);
            game_data.current_player = game_data.first_player;
            game_data.turn_count = 1;

            //Adventure settings
            //冒险设置
            LevelData level = game_data.settings.GetLevel();
            if (level != null)
            {
                if (level != null && level.first_player == LevelFirst.Player)
                    game_data.first_player = 0;
                if (level != null && level.first_player == LevelFirst.AI)
                    game_data.first_player = 1;
                game_data.current_player = game_data.first_player;
            }

            //Init each players
            //启动每个玩家
            foreach (Player player in game_data.players)
            {
                //Puzzle level deck
                //拼图水平甲板
                DeckPuzzleData pdeck = DeckPuzzleData.Get(player.deck);

                //Hp / mana
                player.hp_max = 9;
                player.hp = pdeck != null ? pdeck.start_hp : GameplayData.Get().hp_start;
                player.mana_max = pdeck != null ? pdeck.start_mana : GameplayData.Get().mana_start;
                player.mana = player.mana_max;
                if (game_data.IsVc5TestMode(player))
                {
                    player.mana_max = 99;
                    player.mana = 99;
                }

                //Draw starting cards
                //绘制起始牌
                int dcards = pdeck != null ? pdeck.start_cards : GameplayData.Get().cards_start;
                if (game_data.IsVc5TestMode(player))
                    dcards = player.cards_deck.Count;
                DrawCard(player, dcards);

                //Add coin second player
                //添加硬币第二玩家
                // bool is_random = level == null || level.first_player == LevelFirst.Random;
                // if (is_random && player.player_id != game_data.first_player && GameplayData.Get().second_bonus != null)
                // {
                //     Card card = Card.Create(GameplayData.Get().second_bonus, VariantData.GetDefault(), player);
                //     player.cards_hand.Add(card);
                // }
            }

            //Start state
            RefreshData();
            
            // 开局为每个玩家在最下方部署三个英雄棋子
            DeployInitialHeroes();
            
            onGameStart?.Invoke();

            StartTurn();
        }
        
        //开始阶段
        public virtual void StartStage()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            ClearTurnData();
            game_data.phase = GamePhase.StartTurn;
            onTurnStart?.Invoke();
            RefreshData();

            Player player = game_data.GetActivePlayer();
            player.ResetMainAction();

            // VC5: no turn countdown.
            game_data.turn_timer = 999f;
            player.history_list.Clear();

            if (player.hero != null)
                player.hero.Refresh();

            resolve_queue.AddCallback(StartMainPhase);
            resolve_queue.ResolveAll(0.2f);
        }

        //开始回合
        public virtual void StartTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            ClearTurnData();
            game_data.phase = GamePhase.StartTurn;
            onTurnStart?.Invoke();
            RefreshData();

            game_data.AllPlayersStartTurn();
            foreach (Player aplayer in game_data.players)
            {
                aplayer.ResetMainAction();
                aplayer.ResetAbilityUses();
            }

            Player player = game_data.GetActivePlayer();

            //Cards draw
            //给所有玩家补充手牌到5张（包括第一回合的先手玩家）
            foreach (Player aplayer in game_data.players)
            {
                // 计算需要补卡的数量
                int cardsNeeded = GameplayData.Get().cards_per_turn - aplayer.cards_hand.Count;
                if (cardsNeeded > 0)
                    DrawCard(aplayer, cardsNeeded);
                if (game_data.state == GameState.GameEnded)
                    return;
            }

            //Mana 法力值
            bool is_opening_turn = game_data.opening_turn;
            foreach (Player playerMana in game_data.players)
            {
                if (!is_opening_turn)
                {
                    playerMana.mana_max += GameplayData.Get().mana_per_turn;
                    playerMana.mana_max = Mathf.Min(playerMana.mana_max, GameplayData.Get().mana_max);
                }
                playerMana.mana = playerMana.mana_max;   
                if (game_data.IsVc5TestMode(playerMana))
                {
                    playerMana.mana_max = 99;
                    playerMana.mana = 99;
                }
            }
            if (is_opening_turn)
                game_data.opening_turn = false;

            // VC5: no turn countdown.
            game_data.turn_timer = 999f;
            player.history_list.Clear();

            //Player poison 状态效果处理
            if (player.HasStatus(StatusType.Poisoned))
                player.hp -= player.GetStatusValue(StatusType.Poisoned);

            //英雄技能重置
            if (player.hero != null)
                player.hero.Refresh();

            //Refresh Cards and Status Effects 场上单位处理
            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];

                if (!card.HasStatus(StatusType.Sleep))
                    card.Refresh();

                if (card.HasStatus(StatusType.Poisoned))
                    DamageCard(card, card.GetStatusValue(StatusType.Poisoned));
            }

            //Ongoing Abilities 持续效果更新
            UpdateOngoing();

            //StartTurn Abilities
            // 单位技能
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.StartOfTurn);
            // 奥秘卡触发
            TriggerPlayerSecrets(player, AbilityTrigger.StartOfTurn);

            resolve_queue.AddCallback(StartMainPhase);
            resolve_queue.ResolveAll(0.2f);
        }

        //开始下一个阶段
        public virtual void StartNextStage()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            Player nextPlayer = game_data.GetPlayer((game_data.current_player + 1) % game_data.settings.nb_players);
            if (nextPlayer.EndTurn == false)
            {
                game_data.current_player = (game_data.current_player + 1) % game_data.settings.nb_players;//设置下一回合当前玩家
            }

            CheckForWinner();//判断输赢
            StartStage();//开始回合
        }

        //开始下一个回合
        public virtual void StartNextTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.current_player = (game_data.current_player + 1) % game_data.settings.nb_players;//设置下一回合当前玩家

            if (game_data.current_player == game_data.first_player)
                game_data.turn_count++;

            CheckForWinner();//判断输赢
            StartTurn();//开始回合
        }

        //启动主阶段
        public virtual void StartMainPhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.phase = GamePhase.Main;
            onTurnPlay?.Invoke();
            RefreshData();
        }

        //结束阶段
        public virtual void EndStage()
        {
            EndTurn();
        }

        //结束回合
        public virtual void EndTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;
            if (game_data.phase != GamePhase.Main)
                return;

            Player player = game_data.GetActivePlayer();
            player.EndTurn = true;

            if (game_data.AllPlayersEndTurn() == true)
            {
                Debug.Log("双方结束主要阶段，进入得分阶段");
                game_data.selector = SelectorType.None;
                game_data.phase = GamePhase.Scoring;

                resolve_queue.AddCallback(ResolveScoringZonePhase);
                resolve_queue.ResolveAll(0.2f);
            }
            else
            {
                Debug.Log("结束阶段");
                resolve_queue.AddCallback(StartNextStage);
                resolve_queue.ResolveAll(0.2f);
            }
        }

        //End game with winner
        //以胜利者结束游戏
        public virtual void EndGame(int winner)
        {
            if (game_data.state != GameState.GameEnded)
            {
                game_data.state = GameState.GameEnded;
                game_data.phase = GamePhase.None;
                game_data.selector = SelectorType.None;
                game_data.current_player = winner; //Winner player 获胜者玩家
                resolve_queue.Clear();
                Player player = game_data.GetPlayer(winner);
                onGameEnd?.Invoke(player);
                RefreshData();
            }
        }

        //Progress to the next step/phase 
        //下一步/阶段的进展
        public virtual void NextStep()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            CancelSelection();

            //Add to resolve queue in case its still resolving
            resolve_queue.AddCallback(EndStage);
            resolve_queue.ResolveAll();
        }
        //
        public virtual void NextPhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            CancelSelection();

            //Add to resolve queue in case its still resolving
            resolve_queue.AddCallback(EndTurn);
            resolve_queue.ResolveAll();
        }

        /// <summary>VC5 得分阶段：统计中央得分区存活角色并加分。</summary>
        public virtual void ResolveScoringZonePhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            ResolveScoringZone();
            CheckForWinner();
            if (game_data.state == GameState.GameEnded)
                return;

            BeginEndDiscardPhase();
        }

        /// <summary>按设计文档结算得分区：多者+2，相同各+1（含双方均为0）。</summary>
        public virtual void ResolveScoringZone()
        {
            Player p0 = game_data.GetPlayer(0);
            Player p1 = game_data.GetPlayer(1);
            int count0 = Vc5ScoringZone.CountCharactersInZone(p0);
            int count1 = Vc5ScoringZone.CountCharactersInZone(p1);

            if (count0 > count1)
                p0.kill_count += 2;
            else if (count1 > count0)
                p1.kill_count += 2;
            else
            {
                p0.kill_count += 1;
                p1.kill_count += 1;
            }

            RefreshData();
        }

        public virtual void BeginEndDiscardPhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.phase = GamePhase.EndDiscard;
            foreach (Player aplayer in game_data.players)
                aplayer.end_discard_passed = false;

            onTurnPlay?.Invoke();
            RefreshData();
        }

        public virtual void PassEndDiscard(Player player)
        {
            if (game_data.state == GameState.GameEnded || game_data.phase != GamePhase.EndDiscard)
                return;
            if (player == null || player.end_discard_passed)
                return;

            player.end_discard_passed = true;
            RefreshData();

            if (game_data.AllPlayersEndDiscardPassed())
            {
                resolve_queue.AddCallback(FinishTurnAfterEndDiscard);
                resolve_queue.ResolveAll(0.2f);
            }
        }

        public virtual void DiscardEndPhaseCard(Player player, Card card)
        {
            if (game_data.state == GameState.GameEnded || game_data.phase != GamePhase.EndDiscard)
                return;
            if (player == null || player.end_discard_passed)
                return;
            if (card == null || !player.HasCard(player.cards_hand, card))
                return;

            DiscardCard(card);
            RefreshData();
        }

        protected virtual void FinishTurnAfterEndDiscard()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.phase = GamePhase.EndTurn;
            Player player = game_data.GetActivePlayer();

            RestoreMoveRangesForAllPlayers();

            foreach (Player aplayer in game_data.players)
            {
                aplayer.ReduceStatusDurations();
                foreach (Card card in aplayer.cards_board)
                    card.ReduceStatusDurations();
                foreach (Card card in aplayer.cards_equip)
                    card.ReduceStatusDurations();
            }

            TriggerPlayerCardsAbilityType(player, AbilityTrigger.EndOfTurn);

            onTurnEnd?.Invoke();
            RefreshData();

            resolve_queue.AddCallback(StartNextTurn);
            resolve_queue.ResolveAll(0.2f);
        }

        //Check if a player is winning the game, if so end the game
        //Change or edit this function for a new win condition
        //检查玩家是否赢得了游戏，如果是，结束游戏
        //更改或编辑此功能以获得新的获胜条件
        protected virtual void CheckForWinner()
        {
            // int count_alive = 0;
            // Player alive = null;
            // foreach (Player player in game_data.players)
            // {
            //     if (!player.IsDead())
            //     {
            //         alive = player;
            //         count_alive++;
            //     }
            // }

            // if (count_alive == 0)
            // {
            //     EndGame(-1); //Everyone is dead, Draw 所有人都死了，平局
            // }
            // else if (count_alive == 1)
            // {
            //     EndGame(alive.player_id); //Player win 玩家获胜
            // }

            int count_alive = 0;
            Player alive = null;
            foreach (Player player in game_data.players)
            {
                if (player.kill_count >= 9)
                {
                    alive = player;
                    count_alive++;
                }
            }

            if (count_alive == 1)
            {
                EndGame(alive.player_id); //Player win 玩家获胜
            }
            else if (count_alive > 1)
            {
                EndGame(-1);
            }

        }

        //清除回合数据
        protected virtual void ClearTurnData()
        {
            game_data.selector = SelectorType.None;
            resolve_queue.Clear();
            card_array.Clear();
            player_array.Clear();
            slot_array.Clear();
            card_data_array.Clear();
            game_data.last_played = null;
            game_data.last_destroyed = null;
            game_data.last_target = null;
            game_data.last_summoned = null;
            game_data.ability_triggerer = null;
            game_data.selected_value = 0;
            game_data.ability_played.Clear();
            game_data.cards_attacked.Clear();
            pending_play_cards.Clear();
            pending_play_discards.Clear();
            pending_play_main_action.Clear();
        }

        private void PayCardCost(Player player, Card card)
        {
            if (player == null || card == null)
                return;

            pending_play_discards.Remove(card.uid);

            if (!card.CardData.IsDynamicManaCost())
                player.mana -= card.GetMana();

            player.hp -= card.CardData.hp_cost;
            if (player.hp < 0)
                player.hp = 0;

            List<Card> discarded = null;
            for (int i = 0; i < card.CardData.discard_cost; i++)
            {
                if (player.cards_hand.Count == 0)
                    break;
                int idx = random.Next(0, player.cards_hand.Count);
                Card to_discard = player.cards_hand[idx];
                if (to_discard.uid == card.uid && player.cards_hand.Count > 1)
                    idx = (idx + 1) % player.cards_hand.Count;
                to_discard = player.cards_hand[idx];
                player.cards_hand.RemoveAt(idx);
                player.cards_discard.Add(to_discard);

                if (discarded == null)
                    discarded = new List<Card>();
                discarded.Add(to_discard);
            }

            if (discarded != null)
                pending_play_discards[card.uid] = discarded;
        }

        private void RestoreMoveRangesForAllPlayers()
        {
            foreach (Player aplayer in game_data.players)
            {
                RestoreMoveRange(aplayer.hero);
                foreach (Card card in aplayer.cards_board)
                    RestoreMoveRange(card);
            }
        }

        private void RestoreMoveRange(Card card)
        {
            if (card == null || card.CardData == null)
                return;
            card.move_Range = card.CardData.move_Range;
        }

        //--- Setup ------

        //Set deck using a Deck in Resources
        //使用资源中的卡组设置卡组
        public virtual void SetPlayerDeck(Player player, DeckData deck)
        {
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.monsters_deck.Clear();
            player.deck = deck.id;
            player.hero = null;

            VariantData variant = VariantData.GetDefault();
            if (deck.hero != null)
            {
                player.hero = Card.Create(deck.hero, variant, player);
            }

            foreach (CardData card in deck.cards)
            {
                if (card != null && !Vc5CardRegistry.IsDisabled(card.id))
                {
                    Card acard = Card.Create(card, variant, player);
                    player.cards_deck.Add(acard);
                }
            }
            
            if (deck.monsters != null)
            {
                foreach (CardData card in deck.monsters)
                {
                    if (card != null && !Vc5CardRegistry.IsDisabled(card.id))
                    {
                        Card acard = Card.Create(card, variant, player);
                        player.monsters_deck.Add(acard);
                    }
                }
            }

            DeckPuzzleData puzzle = deck as DeckPuzzleData;

            //Board cards
            if (puzzle != null)
            {
                foreach (DeckCardSlot card in puzzle.board_cards)
                {
                    Card acard = Card.Create(card.card, variant, player);
                    acard.slot = new Slot(card.slot, Slot.GetP(player.player_id));
                    player.cards_board.Add(acard);
                }
            }

            //Shuffle deck
            //洗牌
            if (puzzle == null || !puzzle.dont_shuffle_deck)
                ShuffleDeck(player.cards_deck);
        }

        //Set deck using custom deck in save file or database
        //使用保存文件或数据库中的自定义卡组设置卡组
        public virtual void SetPlayerDeck(Player player, UserDeckData deck)
        {
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.monsters_deck.Clear();
            player.deck = deck.tid;
            player.hero = null;
            player.vc5_deploy_hero_ids = null;

            if (deck.hero != null)
            {
                CardData hdata = CardData.Get(deck.hero.tid);
                VariantData hvariant = VariantData.Get(deck.hero.variant);
                if (hdata != null && hvariant != null)
                    player.hero = Card.Create(hdata, hvariant, player);
            }

            FillDeployHeroIdsFromUserDeck(player, deck);

            foreach (UserCardData card in deck.cards)
            {
                CardData icard = CardData.Get(card.tid);
                VariantData variant = VariantData.Get(card.variant);
                if (icard != null && variant != null)
                {
                    for (int i = 0; i < card.quantity; i++)
                    {
                        Card acard = Card.Create(icard, variant, player);
                        player.cards_deck.Add(acard);
                    }
                }
            }

            //Shuffle deck
            //洗牌
            ShuffleDeck(player.cards_deck);
        }

        private void FillDeployHeroIdsFromUserDeck(Player player, UserDeckData deck)
        {
            player.vc5_deploy_hero_ids = new string[3];

            UserCardData[] deploy = deck != null ? deck.heroes_deploy : null;
            if (deploy != null && deploy.Length >= 3 &&
                deploy[0] != null && deploy[1] != null && deploy[2] != null &&
                !string.IsNullOrEmpty(deploy[0].tid) &&
                !string.IsNullOrEmpty(deploy[1].tid) &&
                !string.IsNullOrEmpty(deploy[2].tid))
            {
                for (int i = 0; i < 3; i++)
                    player.vc5_deploy_hero_ids[i] = Vc5CsvIdMaps.GetEngineHeroId(deploy[i].tid.Trim());
                return;
            }

            UserCardData h = deck != null ? deck.hero : null;
            if (h != null && !string.IsNullOrEmpty(h.tid))
            {
                string hid = Vc5CsvIdMaps.GetEngineHeroId(h.tid.Trim());
                player.vc5_deploy_hero_ids[0] = hid;
                player.vc5_deploy_hero_ids[1] = hid;
                player.vc5_deploy_hero_ids[2] = hid;
                return;
            }

            player.vc5_deploy_hero_ids = null;
        }

        //---- Gameplay Actions --------------
        //---- 游戏操作 --------------

        //打出牌
        public virtual void PlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (game_data.CanPlayCard(card, slot, skip_cost))
            {
                //获取卡牌的所有玩家
                Player player = game_data.GetPlayer(card.player_id);
                if (!skip_cost && !card.CardData.fast_action)
                {
                    if (!game_data.IsVc5TestMode(player))
                    {
                        player.main_action_used = true;
                        pending_play_main_action.Add(card.uid);
                    }
                }

                //Cost
                //成本
                if (!skip_cost)
                {
                    pending_play_cards.Add(card.uid);
                    PayCardCost(player, card);
                }

                //Play card
                //移除牌库中的卡牌
                player.RemoveCardFromAllGroups(card);

                //Add to board
                CardData icard = card.CardData;
                if (icard.IsBoardCard())
                {
                    //如果是棋盘卡牌
                    player.cards_board.Add(card);
                    card.slot = slot;
                    card.exhausted = true; //Cant attack first turn
                }
                else if (icard.IsEquipment())
                {
                    //如果是装备卡
                    Card bearer = game_data.GetSlotCard(slot);
                    EquipCard(bearer, card);
                    card.exhausted = true;
                }
                else if (icard.IsSecret())
                {
                    //如果是秘密卡
                    player.cards_secret.Add(card);
                }
                else
                {
                    //否则加入弃牌堆，并保存槽位
                    player.cards_discard.Add(card);
                    card.slot = slot; //Save slot in case spell has PlayTarget
                }

                //History
                if (!is_ai_predict && !icard.IsSecret())
                    player.AddHistory(GameAction.PlayCard, card);

                //Update ongoing effects
                game_data.last_played = card.uid;
                UpdateOngoing();

                //Trigger abilities
                if (card.CardData.IsDynamicManaCost())
                {
                    GoToSelectorCost(card);
                }
                else
                {
                    TriggerSecrets(AbilityTrigger.OnPlayOther, card); //After playing card
                    TriggerCardAbilityType(AbilityTrigger.OnPlay, card);
                    TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, card);
                }

                if (pending_play_main_action.Contains(card.uid))
                {
                    string playedCardUid = card.uid;
                    int actionPlayerId = player.player_id;
                    resolve_queue.AddCallback(() => CompletePlayedMainAction(playedCardUid, actionPlayerId));
                }

                RefreshData();

                onCardPlayed?.Invoke(card, slot);
                resolve_queue.ResolveAll(0.3f);
            }
        }

        //移动卡牌
        public virtual void MoveCard(Card card, Slot slot, bool skip_cost = false, bool ignore_range = false)
        {
            if (game_data.CanMoveCard(card, slot, skip_cost, ignore_range))
            {
                Card slot_card = game_data.GetSlotCard(slot);
                if (slot_card != null && slot_card.player_id == card.player_id && slot_card.HasTrait(TraitSlimeSpawn))
                {
                    DiscardCard(slot_card);
                    HealCard(card, 1);
                }
                int dx = slot.x - card.slot.x;
                int dy = slot.y - card.slot.y;
                int dz = (card.slot.x + card.slot.y) - (slot.x + slot.y);
                int hexDistance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
                if (!ignore_range)
                    card.move_Range -= hexDistance;

                //正方形网格移动
                //card.move_Range -= Mathf.Abs(slot.x - card.slot.x)+ Mathf.Abs(slot.y - card.slot.y);
                card.slot = slot;

                //Moving doesn't really have any effect in demo so can be done indefinitely
                //if(!skip_cost)
                //card.exhausted = true;
                //card.RemoveStatus(StatusEffect.Stealth);
                //player.AddHistory(GameAction.Move, card);

                //Also move the equipment
                //同时移动设备
                Card equip = game_data.GetEquipCard(card.equipped_uid);
                if (equip != null)
                    equip.slot = slot;
                
                //正在更新
                UpdateOngoing();
                //刷新数据
                RefreshData();

                onCardMoved?.Invoke(card, slot);
                resolve_queue.ResolveAll(0.2f);
            }
        }

        public virtual void CastAbility(Card card, AbilityData iability)
        {
            if (game_data.CanCastAbility(card, iability))
            {
                Player player = game_data.GetPlayer(card.player_id);
                if (!iability.fast_action && !game_data.IsVc5TestMode(player))
                {
                    player.main_action_used = true;
                    int actionPlayerId = player.player_id;
                    resolve_queue.AddCallback(() => CompleteMainActionOpportunity(actionPlayerId));
                }
                card.IncrementAbilityUse(iability.id);
                if (!is_ai_predict && iability.target != AbilityTarget.SelectTarget)
                    player.AddHistory(GameAction.CastAbility, card, iability);
                card.RemoveStatus(StatusType.Stealth);
                TriggerCardAbility(iability, card);
                resolve_queue.ResolveAll();
            }
        }

        private void CompletePlayedMainAction(string cardUid, int playerId)
        {
            if (!pending_play_main_action.Remove(cardUid))
                return; // The play was cancelled while waiting for target selection.

            CompleteMainActionOpportunity(playerId);
        }

        private void CompleteMainActionOpportunity(int playerId)
        {
            Player player = game_data.GetPlayer(playerId);
            if (game_data.phase == GamePhase.Main && game_data.current_player == playerId
                && player != null && !player.EndTurn)
            {
                StartNextStage();
            }
        }

        //攻击目标
        public virtual void AttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (game_data.CanAttackTarget(attacker, target, skip_cost))
            {
                Player player = game_data.GetPlayer(attacker.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.Attack, attacker, target);

                game_data.last_target = target.uid;

                //Trigger before attack abilities
                //攻击前触发能力
                TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);
                TriggerCardAbilityType(AbilityTrigger.OnBeforeDefend, target, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
                TriggerSecrets(AbilityTrigger.OnBeforeDefend, target);

                //Resolve attack
                //解决攻击
                resolve_queue.AddAttack(attacker, target, ResolveAttack, skip_cost);
                resolve_queue.ResolveAll();
            }
        }

        //解决攻击
        protected virtual void ResolveAttack(Card attacker, Card target, bool skip_cost)
        {
            if (!game_data.IsOnBoard(attacker) || !game_data.IsOnBoard(target))
                return;

            onAttackStart?.Invoke(attacker, target);

            attacker.RemoveStatus(StatusType.Stealth);
            UpdateOngoing();

            resolve_queue.AddAttack(attacker, target, ResolveAttackHit, skip_cost);
            resolve_queue.ResolveAll(0.3f);
        }
        
        //解析攻击命中
        protected virtual void ResolveAttackHit(Card attacker, Card target, bool skip_cost)
        {
            //Count attack damage
            //计算攻击伤害
            int datt1 = attacker.GetAttack();
            int datt2 = target.GetAttack();

            //Damage Cards
            //损坏卡片
            DamageCard(attacker, target, datt1);

            //Counter Damage
            //抗损伤
            if (!attacker.HasStatus(StatusType.Intimidate))
                DamageCard(target, attacker, datt2);

            //Save attack and exhaust
            //节省攻击和排气
            if (!skip_cost)
                ExhaustBattle(attacker);

            //Recalculate bonus
            //重新计算奖金
            UpdateOngoing();

            //Abilities
            bool att_board = game_data.IsOnBoard(attacker);
            bool def_board = game_data.IsOnBoard(target);
            if (att_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            if (def_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterDefend, target, attacker);
            if (att_board)
                TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            if (def_board)
                TriggerSecrets(AbilityTrigger.OnAfterDefend, target);

            onAttackEnd?.Invoke(attacker, target);
            RefreshData();
            CheckForWinner();

            resolve_queue.ResolveAll(0.2f);
        }

        public virtual void AttackPlayer(Card attacker, Player target, bool skip_cost = false)
        {
            if (target == null)
                return;

            if (!game_data.CanAttackTarget(attacker, target, skip_cost))
                return;

            Player player = game_data.GetPlayer(attacker.player_id);
            if (!is_ai_predict)
                player.AddHistory(GameAction.AttackPlayer, attacker, target);

            //Resolve abilities
            TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);

            //Resolve attack
            resolve_queue.AddAttack(attacker, target, ResolveAttackPlayer, skip_cost);
            resolve_queue.ResolveAll();
        }

        protected virtual void ResolveAttackPlayer(Card attacker, Player target, bool skip_cost)
        {
            if (!game_data.IsOnBoard(attacker))
                return;

            onAttackPlayerStart?.Invoke(attacker, target);

            attacker.RemoveStatus(StatusType.Stealth);
            UpdateOngoing();

            resolve_queue.AddAttack(attacker, target, ResolveAttackPlayerHit, skip_cost);
            resolve_queue.ResolveAll(0.3f);
        }

        //解析攻击玩家命中
        protected virtual void ResolveAttackPlayerHit(Card attacker, Player target, bool skip_cost)
        {
            DamagePlayer(attacker, target, attacker.GetAttack());

            //Save attack and exhaust
            if (!skip_cost)
                ExhaustBattle(attacker);

            //Recalculate bonus
            UpdateOngoing();

            if (game_data.IsOnBoard(attacker))
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);

            TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);

            onAttackPlayerEnd?.Invoke(attacker, target);
            RefreshData();
            CheckForWinner();

            resolve_queue.ResolveAll(0.2f);
        }

        //Exhaust after battle
        //战斗后的废气
        public virtual void ExhaustBattle(Card attacker)
        {
            bool attacked_before = game_data.cards_attacked.Contains(attacker.uid);
            game_data.cards_attacked.Add(attacker.uid);
            bool attack_again = attacker.HasStatus(StatusType.Fury) && !attacked_before;
            attacker.exhausted = !attack_again;
        }

        //Redirect attack to a new target
        //将攻击重定向到新目标
        public virtual void RedirectAttack(Card attacker, Card new_target)
        {
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.target = new_target;
                    att.ptarget = null;
                    att.callback = ResolveAttack;
                    att.pcallback = null;
                }
            }
        }

        public virtual void RedirectAttack(Card attacker, Player new_target)
        {
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.ptarget = new_target;
                    att.target = null;
                    att.pcallback = ResolveAttackPlayer;
                    att.callback = null;
                }
            }
        }

        public virtual void ShuffleDeck(List<Card> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                Card temp = cards[i];
                int randomIndex = random.Next(i, cards.Count);
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }
        }

        //抽卡
        public virtual void DrawCard(Player player, int nb = 1)
        {
            int drawn = 0;
            for (int i = 0; i < nb; i++)
            {
                bool handHasSpace = player.cards_hand.Count < GameplayData.Get().cards_max
                    || game_data.IsVc5TestMode(player);
                if (!handHasSpace)
                    break;

                if (player.cards_deck.Count == 0)
                {
                    int winner = (player.player_id + 1) % game_data.settings.nb_players;
                    EndGame(winner);
                    break;
                }

                Card card = player.cards_deck[0];
                player.cards_deck.RemoveAt(0);
                player.cards_hand.Add(card);
                drawn++;
            }

            if (drawn > 0)
                onCardDrawn?.Invoke(drawn);
        }

        //Put a card from deck into discard
        //将牌组中的一张牌丢弃
        public virtual void DrawDiscardCard(Player player, int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_discard.Add(card);
                }
            }
        }

        //Summon copy of an exiting card
        public virtual Card SummonCopy(Player player, Card copy, Slot slot)
        {
            CardData icard = copy.CardData;
            return SummonCard(player, icard, copy.VariantData, slot);
        }

        //Summon copy of an exiting card into hand
        public virtual Card SummonCopyHand(Player player, Card copy)
        {
            CardData icard = copy.CardData;
            return SummonCardHand(player, icard, copy.VariantData);
        }

        //Create a new card and send it to the board
        //创建一张新卡并将其发送到董事会
        public virtual Card SummonCard(Player player, CardData card, VariantData variant, Slot slot)
        {
            if (!slot.IsValid())
                return null;

            if (game_data.GetSlotCard(slot) != null)
                return null;

            Card acard = SummonCardHand(player, card, variant);
            PlayCard(acard, slot, true);

            onCardSummoned?.Invoke(acard, slot);

            return acard;
        }

        //Create a new card and send it to your hand
        public virtual Card SummonCardHand(Player player, CardData card, VariantData variant)
        {
            Card acard = Card.Create(card, variant, player);
            player.cards_hand.Add(acard);
            game_data.last_summoned = acard.uid;
            return acard;
        }

        //Transform card into another one
        public virtual Card TransformCard(Card card, CardData transform_to)
        {
            card.SetCard(transform_to, card.VariantData);

            onCardTransformed?.Invoke(card);

            return card;
        }

        public virtual void EquipCard(Card card, Card equipment)
        {
            if (card != null && equipment != null && card.player_id == equipment.player_id)
            {
                if (!card.CardData.IsEquipment() && equipment.CardData.IsEquipment())
                {
                    UnequipAll(card); //Unequip previous cards, only 1 equip at a time

                    Player player = game_data.GetPlayer(card.player_id);
                    player.RemoveCardFromAllGroups(equipment);
                    player.cards_equip.Add(equipment);
                    card.equipped_uid = equipment.uid;
                    equipment.slot = card.slot;
                }
            }
        }

        public virtual void UnequipAll(Card card)
        {
            if (card != null && card.equipped_uid != null)
            {
                Player player = game_data.GetPlayer(card.player_id);
                Card equip = player.GetEquipCard(card.equipped_uid);
                if (equip != null)
                {
                    card.equipped_uid = null;
                    DiscardCard(equip);
                }
            }
        }

        //Change owner of a card
        //更改卡的所有者
        public virtual void ChangeOwner(Card card, Player owner)
        {
            if (card.player_id != owner.player_id)
            {
                Player powner = game_data.GetPlayer(card.player_id);
                powner.RemoveCardFromAllGroups(card);
                powner.cards_all.Remove(card.uid);
                owner.cards_all[card.uid] = card;
                card.player_id = owner.player_id;
            }
        }

        //Damage a player
        public virtual void DamagePlayer(Card attacker, Player target, int value)
        {
            //Damage player
            target.hp -= value;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);

            //Lifesteal
            Player aplayer = game_data.GetPlayer(attacker.player_id);
            if (attacker.HasStatus(StatusType.LifeSteal))
                aplayer.hp += value;

            onPlayerDamaged?.Invoke(target, value);
        }

        //Heal a card
        public virtual void HealCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return;

            target.damage -= value;
            target.damage = Mathf.Max(target.damage, 0);

            onCardHealed?.Invoke(target, value);
        }

        public virtual void HealPlayer(Player target, int value)
        {
            if (target == null)
                return;

            target.hp += value;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);

            onPlayerHealed?.Invoke(target, value);
        }

        //Generic damage that doesnt come from another card
        public virtual void DamageCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity))
                return; //Spell immunity

            target.damage += value;

            onCardDamaged?.Invoke(target, value);

            if (target.GetHP() <= 0)
                KillCard(null, target);
        }

        //Damage a card with attacker/caster
        //用攻击者/施法者损坏卡片
        public virtual void DamageCard(Card attacker, Card target, int value, bool spell_damage = false)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity) && attacker.CardData.type != CardType.Character)
                return; //Spell immunity

            // VC5：黏黏磨刀等——按「每一次伤害结算」附加，与同回合内多次伤害（连打额外段等）分别叠加。
            if (attacker != null && value > 0)
            {
                int db = attacker.GetStatusValue(StatusType.Vc5DealDamageBonus);
                if (db > 0)
                    value += db;
            }

            //Shell
            bool doublelife = target.HasStatus(StatusType.Shell);
            if (doublelife && value > 0)
            {
                target.RemoveStatus(StatusType.Shell);
                return;
            }

            //Slime guard reduce damage (hard slime passive)
            if (target.HasStatus(StatusType.Slime) && game_data.PlayerHasTraitOnBoard(target.player_id, TraitSlimeHard))
            {
                int reduce = Mathf.Min(value, target.GetStatusValue(StatusType.Slime));
                if (reduce > 0)
                {
                    value -= reduce;
                    target.ConsumeStatus(StatusType.Slime, reduce);
                }
            }

            //Armor
            if (!spell_damage && target.HasStatus(StatusType.Armor))
                value = Mathf.Max(value - target.GetStatusValue(StatusType.Armor), 0);

            //Damage
            //损坏
            int damage_max = Mathf.Min(value, target.GetHP());
            int extra = value - target.GetHP();
            target.damage += value;

            //Trample
            Player tplayer = game_data.GetPlayer(target.player_id);
            if (!spell_damage && extra > 0 && attacker.player_id == game_data.current_player && attacker.HasStatus(StatusType.Trample))
                tplayer.hp -= extra;

            //Lifesteal
            Player player = game_data.GetPlayer(attacker.player_id);
            if (!spell_damage && attacker.HasStatus(StatusType.LifeSteal))
                player.hp += damage_max;

            //Remove sleep on damage
            target.RemoveStatus(StatusType.Sleep);

            //Callback
            onCardDamaged?.Invoke(target, value);

            //Blood slime passive heal
            if (value > 0 && attacker.HasTrait(TraitSlime) && target.HasStatus(StatusType.Slime)
                && game_data.PlayerHasTraitOnBoard(attacker.player_id, TraitSlimeBlood))
            {
                HealCard(attacker, 1);
            }

            //Deathtouch
            //死亡触摸
            if (value > 0 && attacker.HasStatus(StatusType.Deathtouch) && target.CardData.type == CardType.Character)
                KillCard(attacker, target);

            //Kill card if no hp
            //如果没有hp，则杀死卡
            if (target.GetHP() <= 0)
                KillCard(attacker, target);
        }

        //A card that kills another card
        //杀死另一张牌的牌
        public virtual void KillCard(Card attacker, Card target)
        {
            if (target == null)
                return;

            if (!game_data.IsOnBoard(target) && !game_data.IsEquipped(target))
                return; //Already killed 已经被杀了

            if (target.HasStatus(StatusType.Invincibility))
                return; //Cant be killed 不能被杀死

            AwardScoreForDeath(target);
            
            DiscardCard(target);

            if (attacker != null && target.HasTrait(TraitSlimeSpawn))
                attacker.AddStatus(StatusType.Slime, 1, 0);

            if (attacker != null)
                TriggerCardAbilityType(AbilityTrigger.OnKill, attacker, target);

            CheckForWinner();
        }

        private void AwardScoreForDeath(Card deadCard)
        {
            if (deadCard == null || game_data.players == null || game_data.players.Length < 2)
                return;

            int opponentId = (deadCard.player_id + 1) % game_data.players.Length;
            Player opponent = game_data.GetPlayer(opponentId);
            if (opponent != null)
                opponent.kill_count += 3;
        }

        //Send card into discard
        //将卡片丢弃
        public virtual void DiscardCard(Card card)
        {
            if (card == null)
                return;

            if (game_data.IsInDiscard(card))
                return; //Already discarded

            CardData icard = card.CardData;
            Player player = game_data.GetPlayer(card.player_id);
            bool was_on_board = game_data.IsOnBoard(card) || game_data.IsEquipped(card);

            //Unequip card
            UnequipAll(card);

            //Remove card from board and add to discard
            player.RemoveCardFromAllGroups(card);
            player.cards_discard.Add(card);
            game_data.last_destroyed = card.uid;

            //Remove from bearer
            Card bearer = player.GetBearerCard(card);
            if (bearer != null)
                bearer.equipped_uid = null;

            if (was_on_board)
            {
                //Trigger on death abilities
                TriggerCardAbilityType(AbilityTrigger.OnDeath, card);
                TriggerOtherCardsAbilityType(AbilityTrigger.OnDeathOther, card);
                TriggerSecrets(AbilityTrigger.OnDeathOther, card);
                UpdateOngoingCards(); //Not UpdateOngoing() here to avoid recursive calls in UpdateOngoingKills
            }

            cards_to_clear.Add(card); //Will be Clear() in the next UpdateOngoing, so that simultaneous damage effects work
            onCardDiscarded?.Invoke(card);
        }

        public int RollRandomValue(int dice)
        {
            return RollRandomValue(1, dice + 1);
        }

        public virtual int RollRandomValue(int min, int max)
        {
            game_data.rolled_value = random.Next(min, max);
            onRollValue?.Invoke(game_data.rolled_value);
            resolve_queue.SetDelay(1f);
            return game_data.rolled_value;
        }

        //--- Abilities --

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Card triggerer = null)
        {
            foreach (AbilityData iability in caster.GetAbilities())
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }

            Card equipped = game_data.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerCardAbilityType(type, equipped, triggerer);
        }

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Player triggerer)
        {
            foreach (AbilityData iability in caster.GetAbilities())
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }

            Card equipped = game_data.GetEquipCard(caster.equipped_uid);
            if (equipped != null)
                TriggerCardAbilityType(type, equipped, triggerer);
        }

        public virtual void TriggerOtherCardsAbilityType(AbilityTrigger type, Card triggerer)
        {
            foreach (Player oplayer in game_data.players)
            {
                if (oplayer.hero != null)
                    TriggerCardAbilityType(type, oplayer.hero, triggerer);

                foreach (Card card in oplayer.cards_board)
                    TriggerCardAbilityType(type, card, triggerer);
            }
        }

        public virtual void TriggerPlayerCardsAbilityType(Player player, AbilityTrigger type)
        {
            if (player.hero != null)
                TriggerCardAbilityType(type, player.hero, player.hero);

            foreach (Card card in player.cards_board)
                TriggerCardAbilityType(type, card, card);
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster)
        {
            TriggerCardAbility(iability, caster, caster);
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; //Triggerer is the caster if not set
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, trigger_card))
            {
                resolve_queue.AddAbility(iability, caster, trigger_card, ResolveCardAbility);
            }
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Player triggerer)
        {
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, triggerer))
            {
                resolve_queue.AddAbility(iability, caster, caster, ResolveCardAbility);
            }
        }

        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster)
        {
            resolve_queue.AddAbility(iability, caster, caster, TriggerCardAbility);
        }

        public virtual void TriggerAbilityDelayed(AbilityData iability, Card caster, Card triggerer)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; //Triggerer is the caster if not set
            resolve_queue.AddAbility(iability, caster, trigger_card, TriggerCardAbility);
        }

        //Resolve a card ability, may stop to ask for target
        protected virtual void ResolveCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            if (!caster.CanDoAbilities())
                return; //Silenced card cant cast

            //Debug.Log("Trigger Ability " + iability.id + " : " + caster.card_id);

            onAbilityStart?.Invoke(iability, caster);
            game_data.ability_triggerer = triggerer.uid;
            game_data.ability_played.Add(iability.id);

            bool is_selector = ResolveCardAbilitySelector(iability, caster);
            if (is_selector)
                return; //Wait for player to select

            ResolveCardAbilityPlayTarget(iability, caster);
            ResolveCardAbilityPlayers(iability, caster);
            ResolveCardAbilityCards(iability, caster);
            ResolveCardAbilitySlots(iability, caster);
            ResolveCardAbilityCardData(iability, caster);
            ResolveCardAbilityNoTarget(iability, caster);
            AfterAbilityResolved(iability, caster);
        }

        protected virtual bool ResolveCardAbilitySelector(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.SelectTarget)
            {
                //Wait for target
                GoToSelectTarget(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.CardSelector)
            {
                GoToSelectorCard(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.ChoiceSelector)
            {
                GoToSelectorChoice(iability, caster);
                return true;
            }
            return false;
        }

        protected virtual void ResolveCardAbilityPlayTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.PlayTarget)
            {
                Slot slot = caster.slot;
                Card slot_card = game_data.GetSlotCard(slot);
                if (slot.IsPlayerSlot())
                {
                    Player tplayer = game_data.GetPlayer(slot.p);
                    if (iability.CanTarget(game_data, caster, tplayer))
                        ResolveEffectTarget(iability, caster, tplayer);
                }
                else if (slot_card != null)
                {
                    if (iability.CanTarget(game_data, caster, slot_card))
                    {
                        game_data.last_target = slot_card.uid;
                        ResolveEffectTarget(iability, caster, slot_card);
                    }
                }
                else
                {
                    if (iability.CanTarget(game_data, caster, slot))
                        ResolveEffectTarget(iability, caster, slot);
                }
            }
        }

        protected virtual void ResolveCardAbilityPlayers(AbilityData iability, Card caster)
        {
            //Get Player Targets based on conditions
            List<Player> targets = iability.GetPlayerTargets(game_data, caster, player_array);

            //Resolve effects
            foreach (Player target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityCards(AbilityData iability, Card caster)
        {
            //Get Cards Targets based on conditions
            List<Card> targets = iability.GetCardTargets(game_data, caster, card_array);

            //Resolve effects
            foreach (Card target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilitySlots(AbilityData iability, Card caster)
        {
            //Get Slot Targets based on conditions
            List<Slot> targets = iability.GetSlotTargets(game_data, caster, slot_array);

            //Resolve effects
            foreach (Slot target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityCardData(AbilityData iability, Card caster)
        {
            //Get Cards Targets based on conditions
            List<CardData> targets = iability.GetCardDataTargets(game_data, caster, card_data_array);

            //Resolve effects
            foreach (CardData target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityNoTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.None)
                iability.DoEffects(this, caster);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Player target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetPlayer?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Card target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetCard?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Slot target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetSlot?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, CardData target)
        {
            iability.DoEffects(this, caster, target);
        }

        //能力解决后
        protected virtual void AfterAbilityResolved(AbilityData iability, Card caster)
        {
            Player player = game_data.GetPlayer(caster.player_id);

            //Pay cost
            if (iability.trigger == AbilityTrigger.Activate || iability.trigger == AbilityTrigger.None)
            {
                player.mana -= iability.mana_cost;
                player.hp -= iability.hp_cost;
                if (player.hp < 0)
                    player.hp = 0;
                for (int i = 0; i < iability.discard_cost; i++)
                {
                    if (player.cards_hand.Count == 0)
                        break;
                    int idx = random.Next(0, player.cards_hand.Count);
                    Card to_discard = player.cards_hand[idx];
                    player.cards_hand.RemoveAt(idx);
                    player.cards_discard.Add(to_discard);
                }
                caster.exhausted = caster.exhausted || iability.exhaust;
            }

            //Recalculate and clear
            UpdateOngoing();
            CheckForWinner();

            //Chain ability
            if (iability.target != AbilityTarget.ChoiceSelector && game_data.state != GameState.GameEnded)
            {
                foreach (AbilityData chain_ability in iability.chain_abilities)
                {
                    if (chain_ability != null)
                    {
                        // 链式能力的目标可用 LastTargeted 读取；triggerer 必须沿用最初选择的释放者，
                        // 否则“连打”等后续段会错误使用被攻击者的攻击力计算伤害。
                        TriggerCardAbility(chain_ability, caster, game_data.GetCard(game_data.ability_triggerer));
                    }
                }
            }

            onAbilityEnd?.Invoke(iability, caster);
            resolve_queue.ResolveAll(0.5f);
            RefreshData();

            if (iability.id == "vc5_hard_slime_awake")
                Vc5AbilityTurnTracker.RecordUse(game_data, caster, Vc5AbilityTurnTracker.HardAwakeKey);
        }

        //This function is called often to update status/stats affected by ongoing abilities
        //It basically first reset the bonus to 0 (CleanOngoing) and then recalculate it to make sure it it still present
        //Only cards in hand and on board are updated in this way
        //经常调用此函数来更新受持续能力影响的状态/统计数据
        //它基本上首先将奖金重置为0（CleanUngoing），然后重新计算以确保它仍然存在
        //只有手上和机上的卡才会以这种方式更新
        public virtual void UpdateOngoing()
        {
            Profiler.BeginSample("Update Ongoing");
            UpdateOngoingCards(); //Update status and stats 更新状态和统计数据
            UpdateOngoingKills(); //Kill cards with 0 HP 以0HP杀死卡牌
            Profiler.EndSample();
        }

        protected virtual void UpdateOngoingCards()
        {
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                player.ClearOngoing();

                for (int c = 0; c < player.cards_board.Count; c++)
                    player.cards_board[c].ClearOngoing();

                for (int c = 0; c < player.cards_equip.Count; c++)
                    player.cards_equip[c].ClearOngoing();

                for (int c = 0; c < player.cards_hand.Count; c++)
                    player.cards_hand[c].ClearOngoing();
            }

            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                UpdateOngoingAbilities(player, player.hero);  //Remove this line if hero is on the board

                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];
                    UpdateOngoingAbilities(player, card);
                }

                for (int c = 0; c < player.cards_equip.Count; c++)
                {
                    Card card = player.cards_equip[c];
                    UpdateOngoingAbilities(player, card);
                }
            }

            //Stats bonus
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];

                    //Taunt effect
                    if (card.HasStatus(StatusType.Protection) && !card.HasStatus(StatusType.Stealth))
                    {
                        player.AddOngoingStatus(StatusType.Protected, 0);

                        for (int tc = 0; tc < player.cards_board.Count; tc++)
                        {
                            Card tcard = player.cards_board[tc];
                            if (!tcard.HasStatus(StatusType.Protection) && !tcard.HasStatus(StatusType.Protected))
                            {
                                tcard.AddOngoingStatus(StatusType.Protected, 0);
                            }
                        }
                    }

                    //Status bonus
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }

                for (int c = 0; c < player.cards_hand.Count; c++)
                {
                    Card card = player.cards_hand[c];
                    //Status bonus
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }
            }
        }

        protected virtual void UpdateOngoingKills()
        {
            //Kill stuff with 0 hp
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int i = player.cards_board.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_board.Count)
                    {
                        Card card = player.cards_board[i];
                        if (card.GetHP() <= 0)
                            KillCard(null, card);
                    }
                }
                for (int i = player.cards_equip.Count - 1; i >= 0; i--)
                {
                    if (i < player.cards_equip.Count)
                    {
                        Card card = player.cards_equip[i];
                        if (card.GetHP() <= 0)
                            KillCard(null, card);
                        Card bearer = player.GetBearerCard(card);
                        if (bearer == null)
                            DiscardCard(card);
                    }
                }
            }

            //Clear cards
            for (int c = 0; c < cards_to_clear.Count; c++)
                cards_to_clear[c].Clear();
            cards_to_clear.Clear();
        }

        protected virtual void UpdateOngoingAbilities(Player player, Card card)
        {
            if (card == null || !card.CanDoAbilities())
                return;

            List<AbilityData> cabilities = card.GetAbilities();
            for (int a = 0; a < cabilities.Count; a++)
            {
                AbilityData ability = cabilities[a];
                if (ability != null && ability.trigger == AbilityTrigger.Ongoing && ability.AreTriggerConditionsMet(game_data, card))
                {
                    if (ability.target == AbilityTarget.Self)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, card))
                        {
                            ability.DoOngoingEffects(this, card, card);
                        }
                    }

                    if (ability.target == AbilityTarget.PlayerSelf)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, player))
                        {
                            ability.DoOngoingEffects(this, card, player);
                        }
                    }

                    if (ability.target == AbilityTarget.AllPlayers || ability.target == AbilityTarget.PlayerOpponent)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            if (ability.target == AbilityTarget.AllPlayers || tp != player.player_id)
                            {
                                Player oplayer = game_data.players[tp];
                                if (ability.AreTargetConditionsMet(game_data, card, oplayer))
                                {
                                    ability.DoOngoingEffects(this, card, oplayer);
                                }
                            }
                        }
                    }

                    if (ability.target == AbilityTarget.EquippedCard)
                    {
                        if (card.CardData.IsEquipment())
                        {
                            //Get bearer of the equipment
                            Card target = player.GetBearerCard(card);
                            if (target != null && ability.AreTargetConditionsMet(game_data, card, target))
                            {
                                ability.DoOngoingEffects(this, card, target);
                            }
                        }
                        else if (card.equipped_uid != null)
                        {
                            //Get equipped card
                            Card target = game_data.GetCard(card.equipped_uid);
                            if (target != null && ability.AreTargetConditionsMet(game_data, card, target))
                            {
                                ability.DoOngoingEffects(this, card, target);
                            }
                        }
                    }

                    if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand || ability.target == AbilityTarget.AllCardsBoard)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            //Looping on all cards is very slow, since there are no ongoing effects that works out of board/hand we loop on those only
                            Player tplayer = game_data.players[tp];

                            //Hand Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand)
                            {
                                for (int tc = 0; tc < tplayer.cards_hand.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_hand[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }

                            //Board Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsBoard)
                            {
                                for (int tc = 0; tc < tplayer.cards_board.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_board[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }

                            //Equip Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles)
                            {
                                for (int tc = 0; tc < tplayer.cards_equip.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_equip[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void AddOngoingStatusBonus(Card card, CardStatus status)
        {
            if (status.type == StatusType.AddAttack)
                card.attack_ongoing += status.value;
            if (status.type == StatusType.AddHP)
                card.hp_ongoing += status.value;
            if (status.type == StatusType.AddManaCost)
                card.mana_ongoing += status.value;
            if (status.type == StatusType.Sharp)
                card.attack_ongoing += status.value;
        }

        //---- Secrets ------------

        public virtual bool TriggerPlayerSecrets(Player player, AbilityTrigger secret_trigger)
        {
            for (int i = player.cards_secret.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_secret[i];
                CardData icard = card.CardData;
                if (icard.type == CardType.Secret && !card.exhausted)
                {
                    if (card.AreAbilityConditionsMet(secret_trigger, game_data, card, card))
                    {
                        resolve_queue.AddSecret(secret_trigger, card, card, ResolveSecret);
                        resolve_queue.SetDelay(0.5f);
                        card.exhausted = true;

                        if (onSecretTrigger != null)
                            onSecretTrigger.Invoke(card, card);

                        return true; //Trigger only 1 secret per trigger
                    }
                }
            }
            return false;
        }

        public virtual bool TriggerSecrets(AbilityTrigger secret_trigger, Card trigger_card)
        {
            if (trigger_card != null && trigger_card.HasStatus(StatusType.SpellImmunity))
                return false; //Spell Immunity, triggerer is the one that trigger the trap, target is the one attacked, so usually the player who played the trap, so we dont check the target

            for (int p = 0; p < game_data.players.Length; p++)
            {
                if (p != game_data.current_player)
                {
                    Player other_player = game_data.players[p];
                    for (int i = other_player.cards_secret.Count - 1; i >= 0; i--)
                    {
                        Card card = other_player.cards_secret[i];
                        CardData icard = card.CardData;
                        if (icard.type == CardType.Secret && !card.exhausted)
                        {
                            Card trigger = trigger_card != null ? trigger_card : card;
                            if (card.AreAbilityConditionsMet(secret_trigger, game_data, card, trigger))
                            {
                                resolve_queue.AddSecret(secret_trigger, card, trigger, ResolveSecret);
                                resolve_queue.SetDelay(0.5f);
                                card.exhausted = true;

                                if (onSecretTrigger != null)
                                    onSecretTrigger.Invoke(card, trigger);

                                return true; //Trigger only 1 secret per trigger
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected virtual void ResolveSecret(AbilityTrigger secret_trigger, Card secret_card, Card trigger)
        {
            CardData icard = secret_card.CardData;
            Player player = game_data.GetPlayer(secret_card.player_id);
            if (icard.type == CardType.Secret)
            {
                Player tplayer = game_data.GetPlayer(trigger.player_id);
                if (!is_ai_predict)
                    tplayer.AddHistory(GameAction.SecretTriggered, secret_card, trigger);

                TriggerCardAbilityType(secret_trigger, secret_card, trigger);
                DiscardCard(secret_card);

                if (onSecretResolve != null)
                    onSecretResolve.Invoke(secret_card, trigger);
            }
        }

        //---- Resolve Selector -----

        public virtual void SelectCard(Card target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Can't target that target

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                game_data.selector = SelectorType.None;
                game_data.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }

            if (game_data.selector == SelectorType.SelectorCard)
            {
                if (!ability.IsCardSelectionValid(game_data, caster, target, card_array))
                    return; //Supports conditions and filters

                game_data.selector = SelectorType.None;
                game_data.last_target = target.uid;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectPlayer(Player target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Can't target that target

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectSlot(Slot target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || !target.IsValid())
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Conditions not met

                Player player = game_data.GetPlayer(caster.player_id);
                if (!is_ai_predict)
                    player.AddHistory(GameAction.CastAbility, caster, ability, target);

                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectChoice(int choice)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || choice < 0)
                return;

            if (game_data.selector == SelectorType.SelectorChoice && ability.target == AbilityTarget.ChoiceSelector)
            {
                if (choice >= 0 && choice < ability.chain_abilities.Length)
                {
                    AbilityData achoice = ability.chain_abilities[choice];
                    if (achoice != null && game_data.CanSelectAbility(caster, achoice))
                    {
                        game_data.selector = SelectorType.None;
                        AfterAbilityResolved(ability, caster);
                        ResolveCardAbility(achoice, caster, caster);
                        resolve_queue.ResolveAll();
                    }
                }
            }
        }

        public virtual void SelectCost(int select_cost)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Player player = game_data.GetPlayer(game_data.selector_player_id);
            Card caster = game_data.GetCard(game_data.selector_caster_uid);

            if (player == null || caster == null || select_cost < 0)
                return;

            if (game_data.selector == SelectorType.SelectorCost)
            {
                if (select_cost >= 0 && select_cost < 10 && select_cost <= player.mana)
                {
                    game_data.selector = SelectorType.None;
                    game_data.selected_value = select_cost;
                    player.mana -= select_cost;
                    RefreshData();

                    TriggerSecrets(AbilityTrigger.OnPlayOther, caster);
                    TriggerCardAbilityType(AbilityTrigger.OnPlay, caster);
                    TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, caster);
                    resolve_queue.ResolveAll();
                }
            }
        }

        public virtual void CancelSelection()
        {
            if (game_data.selector != SelectorType.None)
            {
                // Return card to hand if this selector came from a card just played from hand.
                if (IsPendingPlayedCard(game_data.selector_caster_uid))
                    CancelPlayCard();

                //End selection
                game_data.selector = SelectorType.None;
                RefreshData();
            }
        }

        private bool IsPendingPlayedCard(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return false;
            return pending_play_cards.Contains(uid);
        }

        public void CancelPlayCard()
        {
            Card card = game_data.GetCard(game_data.selector_caster_uid);
            if (card != null)
            {
                Player player = game_data.GetPlayer(card.player_id);
                if (card.CardData.IsDynamicManaCost())
                    player.mana += game_data.selected_value;
                else
                    player.mana += card.GetMana();

                player.hp += card.CardData.hp_cost;
                if (player.hp > player.hp_max)
                    player.hp = player.hp_max;

                if (pending_play_discards.TryGetValue(card.uid, out List<Card> discarded))
                {
                    foreach (Card dcard in discarded)
                    {
                        player.cards_discard.Remove(dcard);
                        if (!player.cards_hand.Contains(dcard))
                            player.cards_hand.Add(dcard);
                    }
                    pending_play_discards.Remove(card.uid);
                }

                pending_play_cards.Remove(card.uid);
                if (pending_play_main_action.Remove(card.uid))
                    player.main_action_used = false;

                RefundSelectedCardHpCost(card);

                player.RemoveCardFromAllGroups(card);
                player.AddCard(player.cards_hand, card);
                card.Clear();
            }
        }

        private void RefundSelectedCardHpCost(Card playedCard)
        {
            if (playedCard == null)
                return;

            Card hpPayer = game_data.GetCard(game_data.ability_triggerer);
            if (hpPayer == null || hpPayer.uid == playedCard.uid || !hpPayer.CardData.IsCharacter())
                return;

            int hpCost = GetPlayedCardSelectedHpCost(playedCard.card_id);
            if (hpCost <= 0)
                return;

            hpPayer.damage = Mathf.Max(0, hpPayer.damage - hpCost);
        }

        private int GetPlayedCardSelectedHpCost(string cardId)
        {
            if (cardId == "vc5_slime_heavy_strike")
            {
                AbilityData pick = AbilityData.Get("vc5_slime_heavy_pick");
                if (pick != null && pick.effects != null && pick.effects.Length > 0 && pick.effects[0] is EffectStoreTargetStatsToCaster store)
                    return store.hp_cost_from_target;
                return 2;
            }
            return 0;
        }

        //-----Trigger Selector-----

        protected virtual void GoToSelectTarget(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectTarget;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorCard(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorCard;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorChoice(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorChoice;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            RefreshData();
        }

        protected virtual void GoToSelectorCost(Card caster)
        {
            game_data.selector = SelectorType.SelectorCost;
            game_data.selector_player_id = caster.player_id;
            game_data.selector_ability_id = "";
            game_data.selector_caster_uid = caster.uid;
            game_data.selected_value = 0;
            RefreshData();
        }

        //-------------

        public virtual void RefreshData()
        {
            onRefresh?.Invoke();
        }

        public virtual void ClearResolve()
        {
            resolve_queue.Clear();
        }

        public virtual bool IsResolving()
        {
            return resolve_queue.IsResolving();
        }

        public virtual bool IsGameStarted()
        {
            return game_data.HasStarted();
        }

        public virtual bool IsGameEnded()
        {
            return game_data.HasEnded();
        }

        public virtual Game GetGameData()
        {
            return game_data;
        }

        public System.Random GetRandom()
        {
            return random;
        }

        /// <summary>
        /// 开局为每名玩家在“自己视角靠近底边”的三角形三格部署三个英雄。
        /// 由于 `Game.unity` 的棋盘格使用的是自方相对坐标，双方都必须落在同一组逻辑格上，
        /// 这样 host / join 两端都会把自己的三个英雄显示在己方底边附近。
        /// </summary>
        private void DeployInitialHeroes()
        {
            VariantData variant = VariantData.GetDefault();

            foreach (Player player in game_data.players)
            {
                CardData[] heroesToDeploy = new CardData[3];
                bool fromUserDeck = player.vc5_deploy_hero_ids != null &&
                                    player.vc5_deploy_hero_ids.Length >= 3 &&
                                    !string.IsNullOrEmpty(player.vc5_deploy_hero_ids[0]) &&
                                    !string.IsNullOrEmpty(player.vc5_deploy_hero_ids[1]) &&
                                    !string.IsNullOrEmpty(player.vc5_deploy_hero_ids[2]);

                if (fromUserDeck)
                {
                    heroesToDeploy[0] = CardData.Get(player.vc5_deploy_hero_ids[0]);
                    heroesToDeploy[1] = CardData.Get(player.vc5_deploy_hero_ids[1]);
                    heroesToDeploy[2] = CardData.Get(player.vc5_deploy_hero_ids[2]);
                    if (heroesToDeploy[0] == null || heroesToDeploy[1] == null || heroesToDeploy[2] == null)
                    {
                        Debug.LogWarning($"玩家 {player.player_id} 卡组英雄 id 有误，改用 DeckData 或默认英雄");
                        fromUserDeck = false;
                    }
                }

                DeckData deck = DeckData.Get(player.deck);
                if (!fromUserDeck)
                {
                if (deck == null)
                {
                    Debug.LogWarning($"玩家 {player.player_id} 的卡组 {player.deck} 不存在，无法部署英雄");
                    continue;
                }

                bool hasConfiguredHeroes = deck.heroes != null && deck.heroes.Length == 3 &&
                                          deck.heroes[0] != null && deck.heroes[1] != null && deck.heroes[2] != null;

                if (hasConfiguredHeroes)
                {
                    heroesToDeploy = deck.heroes;
                }
                else
                {
                    CardData defaultHero = CardData.Get("elf_swordsman");
                    if (defaultHero == null)
                        defaultHero = Resources.Load<CardData>("Cards/vc5/hero_elf_swordsman");
                    if (defaultHero == null)
                    {
                        Debug.LogWarning($"无法加载默认英雄（精灵剑士），玩家 {player.player_id} 无法部署英雄");
                        continue;
                    }
                    heroesToDeploy[0] = defaultHero;
                    heroesToDeploy[1] = defaultHero;
                    heroesToDeploy[2] = defaultHero;
                    Debug.Log($"玩家 {player.player_id} 的卡组未配置三个英雄，使用默认英雄（精灵剑士）");
                }

                }

                int deploy_p = Slot.GetP(player.player_id);
                // `Game.unity` 里的棋盘格全部是 `BoardSlotType.PlayerSelf`，
                // 所以这里必须使用“自方相对坐标”，不能再按 player_id 做上下镜像。
                // 这组三角形正对应测试图里两个客户端各自底边附近的三个红叉位置。
                SlotXY[] deploy_slots = new SlotXY[]
                {
                    new SlotXY { x = 3, y = 2 }, // 左上
                    new SlotXY { x = 2, y = 4 }, // 右上
                    new SlotXY { x = 2, y = 3 }, // 下方中心
                };

                int deployedCount = 0;

                for (int i = 0; i < deploy_slots.Length && i < heroesToDeploy.Length; i++)
                {
                    if (heroesToDeploy[i] == null || Vc5CardRegistry.IsDisabled(heroesToDeploy[i].id))
                        continue;

                    int deploy_x_i = deploy_slots[i].x;
                    int deploy_y_i = deploy_slots[i].y;
                    Slot deploy_slot = new Slot(deploy_x_i, deploy_y_i, deploy_p);

                    if (!deploy_slot.IsValid())
                    {
                        Debug.LogWarning($"玩家 {player.player_id} 的槽位 ({deploy_x_i}, {deploy_y_i}, {deploy_p}) 无效");
                        continue;
                    }

                    if (game_data.GetSlotCard(deploy_slot) != null)
                    {
                        Debug.LogWarning($"玩家 {player.player_id} 的槽位 ({deploy_x_i}, {deploy_y_i}, {deploy_p}) 已被占用，跳过部署");
                        continue;
                    }

                    Card deployed_card = SummonCard(player, heroesToDeploy[i], variant, deploy_slot);
                    if (deployed_card != null)
                    {
                        deployedCount++;
                        Debug.Log($"成功为玩家 {player.player_id} 在槽位 ({deploy_x_i}, {deploy_y_i}, {deploy_p}) 部署英雄: {heroesToDeploy[i].title}，移动={deployed_card.move_Range}，攻距={deployed_card.attack_Range}");
                    }
                    else
                    {
                        Debug.LogWarning($"无法为玩家 {player.player_id} 在槽位 ({deploy_x_i}, {deploy_y_i}, {deploy_p}) 部署英雄: {heroesToDeploy[i].title}");
                    }
                }

                if (deployedCount < 3)
                {
                    Debug.LogWarning($"玩家 {player.player_id} 只成功部署了 {deployedCount}/3 个英雄");
                }
            }
        }

        public Game GameData { get { return game_data; } }
        public ResolveQueue ResolveQueue { get { return resolve_queue; } }


        private static bool IsVc5DemoDeck(string deckId)
        {
            return deckId == Vc5DemoBootstrap.MobileAssaultDeckId
                || deckId == Vc5DemoBootstrap.RangedPressureDeckId;
        }


        private bool IsVc5DemoSoloMatch()
        {
            if (game_data == null || game_data.settings == null
                || game_data.settings.game_type != GameType.Solo
                || game_data.players == null || game_data.players.Length < 2)
                return false;

            return IsVc5DemoDeck(game_data.players[0].deck)
                && IsVc5DemoDeck(game_data.players[1].deck);
        }
    }
}
