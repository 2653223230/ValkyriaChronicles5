using System;
using System.Collections.Generic;
using TcgEngine.Gameplay;
using UnityEngine;

namespace TcgEngine.Testing
{
    /// <summary>
    /// Headless helpers for driving GameLogic in automated tests (no UI / no network).
    /// </summary>
    public static class Vc5LogicTestHarness
    {
        private const string DefaultDeckId = "deck_slime_vol01_test";

        public static bool DataLoaded { get; private set; }
        public static Vc5GameSnapshot LastSnapshot { get; set; }

        public static void LoadGameData()
        {
            if (DataLoaded)
                return;

            CardData.Load();
            TeamData.Load();
            RarityData.Load();
            TraitData.Load();
            VariantData.Load();
            DeckData.Load();
            AbilityData.Load();
            StatusData.Load();
            Vc5SlimeBootstrap.Register();
            Vc5CardRegistry.Apply();

            DataLoaded = true;
        }

        public static GameLogic CreateLogic(out Game game, string deckId = DefaultDeckId, int firstPlayer = 0)
        {
            LoadGameData();

            DeckData deck = DeckData.Get(deckId);
            if (deck == null)
                throw new InvalidOperationException("Deck not found: " + deckId);

            game = new Game("vc5_logic_test", 2);
            GameLogic logic = new GameLogic(true);
            logic.SetData(game);

            logic.SetPlayerDeck(game.players[0], deck);
            logic.SetPlayerDeck(game.players[1], deck);
            logic.StartGame();

            game.first_player = firstPlayer;
            game.current_player = firstPlayer;
            logic.RefreshData();
            FlushResolve(logic);

            return logic;
        }

        public static void FlushResolve(GameLogic logic, int maxSteps = 64)
        {
            if (logic == null)
                return;

            for (int i = 0; i < maxSteps; i++)
            {
                logic.ResolveQueue.ResolveAll();
                if (!logic.IsResolving() && logic.GameData.selector == SelectorType.None)
                    return;
            }
        }

        public static Card FindHandCard(Player player, string cardId)
        {
            foreach (Card card in player.cards_hand)
            {
                if (card.card_id == cardId)
                    return card;
            }
            return null;
        }

        public static Card FindBoardCard(Player player, string cardId)
        {
            foreach (Card card in player.cards_board)
            {
                if (card.card_id == cardId)
                    return card;
            }
            return null;
        }

        public static Card GetFirstBoardHero(Player player)
        {
            foreach (Card card in player.cards_board)
            {
                if (card.CardData != null && card.CardData.type == CardType.Hero)
                    return card;
            }
            if (player.cards_board.Count > 0)
                return player.cards_board[0];
            return null;
        }

        public static Card GiveHandCard(Game game, int playerId, string cardId)
        {
            Player player = game.GetPlayer(playerId);
            CardData data = CardData.Get(cardId);
            if (data == null)
                throw new InvalidOperationException("Card not found: " + cardId);

            Card card = Card.Create(data, VariantData.GetDefault(), player);
            player.cards_hand.Add(card);
            return card;
        }

        public static Card PlaceOnBoard(Game game, int playerId, string cardId, Slot slot)
        {
            Player player = game.GetPlayer(playerId);
            CardData data = CardData.Get(cardId);
            if (data == null)
                throw new InvalidOperationException("Card not found: " + cardId);

            if (game.GetSlotCard(slot) != null)
                throw new InvalidOperationException($"Slot occupied: ({slot.x},{slot.y},{slot.p})");

            Card card = Card.Create(data, VariantData.GetDefault(), player);
            card.slot = slot;
            player.cards_board.Add(card);
            return card;
        }

        public static void PlayCardWithSelects(GameLogic logic, Card card, Slot castSlot, params Card[] selectTargets)
        {
            logic.PlayCard(card, castSlot, skip_cost: true);
            FlushResolve(logic);

            if (selectTargets != null)
            {
                foreach (Card target in selectTargets)
                {
                    if (logic.GameData.selector == SelectorType.None)
                        break;
                    logic.SelectCard(target);
                    FlushResolve(logic);
                }
            }

            if (logic.GameData.selector != SelectorType.None)
            {
                throw new InvalidOperationException(
                    $"Selector still active ({logic.GameData.selector}) after PlayCardWithSelects for {card.card_id}");
            }
        }

        public static Vc5GameSnapshot CaptureSnapshot(Game game)
        {
            var snapshot = new Vc5GameSnapshot
            {
                turn_count = game.turn_count,
                current_player = game.current_player,
                phase = game.phase.ToString(),
                selector = game.selector.ToString(),
                players = new List<Vc5PlayerSnapshot>(),
                board = new List<Vc5CardSnapshot>()
            };

            foreach (Player player in game.players)
            {
                snapshot.players.Add(new Vc5PlayerSnapshot
                {
                    player_id = player.player_id,
                    hp = player.hp,
                    mana = player.mana,
                    hand_count = player.cards_hand.Count,
                    board_count = player.cards_board.Count,
                    deck_count = player.cards_deck.Count
                });

                foreach (Card card in player.cards_board)
                    snapshot.board.Add(CreateCardSnapshot(card));
            }

            return snapshot;
        }

        private static Vc5CardSnapshot CreateCardSnapshot(Card card)
        {
            var cardSnapshot = new Vc5CardSnapshot
            {
                uid = card.uid,
                card_id = card.card_id,
                player_id = card.player_id,
                slot_x = card.slot.x,
                slot_y = card.slot.y,
                slot_p = card.slot.p,
                hp = card.GetHP(),
                attack = card.GetAttack(),
                statuses = new List<Vc5StatusSnapshot>()
            };

            foreach (CardStatus status in card.status)
            {
                cardSnapshot.statuses.Add(new Vc5StatusSnapshot
                {
                    type = status.type.ToString(),
                    value = status.value,
                    duration = status.duration
                });
            }

            return cardSnapshot;
        }
    }
}
