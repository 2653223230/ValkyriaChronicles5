using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Shared setup logic for the VC5 demo solo match entry.
    /// Keeps menu UI and tests using the same GameClient settings.
    /// </summary>
    public static class Vc5DemoMatchSetup
    {
        private const string DemoUsername = "VC5_Demo_Player";

        public static string GetDefaultPlayerDeckId()
        {
            return Vc5DemoBootstrap.MobileAssaultDeckId;
        }

        public static string GetDefaultAIDeckId()
        {
            return Vc5DemoBootstrap.RangedPressureDeckId;
        }

        public static bool ApplySoloAIMatch(string playerDeckId, string aiDeckId)
        {
            DeckData playerDeck = GetDeck(playerDeckId);
            DeckData aiDeck = GetDeck(aiDeckId);
            if (playerDeck == null || aiDeck == null)
                return false;

            UserDeckData playerUserDeck = new UserDeckData(playerDeck);
            UserDeckData aiUserDeck = new UserDeckData(aiDeck);
            if (!playerUserDeck.IsValid() || !aiUserDeck.IsValid())
                return false;

            GameplayData gdata = GameplayData.Get();

            GameClient.game_settings = GameSettings.Default;
            GameClient.game_settings.game_type = GameType.Solo;
            GameClient.game_settings.game_mode = GameMode.Casual;
            GameClient.game_settings.server_url = "";
            GameClient.game_settings.scene = gdata != null ? gdata.GetRandomArena() : "Game";

            GameClient.player_settings = PlayerSettings.Default;
            GameClient.player_settings.username = GetCurrentUsername();
            GameClient.player_settings.deck = playerUserDeck;
            GameClient.player_settings.deck.tid = playerDeck.id;

            GameClient.ai_settings = PlayerSettings.DefaultAI;
            GameClient.ai_settings.deck = aiUserDeck;
            GameClient.ai_settings.deck.tid = aiDeck.id;
            GameClient.ai_settings.ai_level = gdata != null ? gdata.ai_level : GameClient.ai_settings.ai_level;

            return true;
        }

        public static bool TryStartSoloAIMatch(MainMenu menu, string playerDeckId, string aiDeckId, out string error)
        {
            error = "";
            if (menu == null)
            {
                error = "未找到主菜单。";
                return false;
            }

            EnsureDemoLogin();

            if (!ApplySoloAIMatch(playerDeckId, aiDeckId))
            {
                error = "卡组无效：请确认玩家和 AI 都选择了已实现的试玩卡组。";
                return false;
            }

            PlayerPrefs.SetString("tcg_vc5_demo_player_deck", playerDeckId);
            PlayerPrefs.SetString("tcg_vc5_demo_ai_deck", aiDeckId);
            menu.StartGame(GameType.Solo, GameMode.Casual);
            return true;
        }

        public static DeckData[] GetPlayableDemoDecks()
        {
            EnsureDemoDecksRegistered();

            List<DeckData> decks = new List<DeckData>();
            AddDeck(decks, Vc5DemoBootstrap.MobileAssaultDeckId);
            AddDeck(decks, Vc5DemoBootstrap.RangedPressureDeckId);
            AddDeck(decks, Vc5DemoBootstrap.RangedPressureC3DeckId);
            return decks.ToArray();
        }

        public static string GetSavedPlayerDeckId()
        {
            return PlayerPrefs.GetString("tcg_vc5_demo_player_deck", GetDefaultPlayerDeckId());
        }

        public static string GetSavedAIDeckId()
        {
            return PlayerPrefs.GetString("tcg_vc5_demo_ai_deck", GetDefaultAIDeckId());
        }

        public static void EnsureDemoLogin()
        {
            Authenticator auth = GetAuthenticatorOrNull();
            if (auth == null || auth.IsConnected())
                return;

            auth.LoginTest(DemoUsername);
        }

        private static DeckData GetDeck(string deckId)
        {
            EnsureDemoDecksRegistered();
            if (string.IsNullOrEmpty(deckId))
                return null;

            return DeckData.Get(deckId);
        }

        private static void EnsureDemoDecksRegistered()
        {
            if (DeckData.Get(Vc5DemoBootstrap.MobileAssaultDeckId) == null
                || DeckData.Get(Vc5DemoBootstrap.RangedPressureDeckId) == null
                || DeckData.Get(Vc5DemoBootstrap.RangedPressureC3DeckId) == null)
            {
                Vc5DemoBootstrap.Register();
            }
        }

        private static void AddDeck(List<DeckData> decks, string deckId)
        {
            DeckData deck = DeckData.Get(deckId);
            if (deck != null)
                decks.Add(deck);
        }

        private static string GetCurrentUsername()
        {
            Authenticator auth = GetAuthenticatorOrNull();
            if (auth != null && !string.IsNullOrEmpty(auth.Username))
                return auth.Username;
            return DemoUsername;
        }

        private static Authenticator GetAuthenticatorOrNull()
        {
            try
            {
                return Authenticator.Get();
            }
            catch (System.NullReferenceException)
            {
                return null;
            }
        }
    }
}
