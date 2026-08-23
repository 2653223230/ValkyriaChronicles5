#if UNITY_EDITOR
using System;
using TcgEngine.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
    public static class Vc5BatchVerifier
    {
        public static void Run()
        {
            try
            {
                VerifyMsgStringRoundTrip();
                VerifyOpeningMana();
                Debug.Log("[VC5 Verify] All checks passed.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[VC5 Verify] Failed: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static void VerifyMsgStringRoundTrip()
        {
            MsgString msg = new MsgString { text = "card-uid-123" };
            byte[] bytes = NetworkTool.NetSerialize(msg);
            MsgString result = NetworkTool.NetDeserialize<MsgString>(bytes);
            if (result == null || result.text != msg.text)
                throw new InvalidOperationException("MsgString did not round-trip through NetworkTool.");
        }

        private static void VerifyOpeningMana()
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out Game game, firstPlayer: 0, vc5TestMode: false);
            Vc5LogicTestHarness.FlushResolve(logic);

            int startMana = GameplayData.Get().mana_start;
            AssertEqual(startMana, game.GetPlayer(0).mana_max, "p0 opening mana_max");
            AssertEqual(startMana, game.GetPlayer(0).mana, "p0 opening mana");
            AssertEqual(startMana, game.GetPlayer(1).mana_max, "p1 opening mana_max");
            AssertEqual(startMana, game.GetPlayer(1).mana, "p1 opening mana");

            int expectedAfterGrowth = startMana + GameplayData.Get().mana_per_turn;
            game.current_player = 1;
            logic.StartNextTurn();
            Vc5LogicTestHarness.FlushResolve(logic);

            AssertEqual(0, game.current_player, "current player after next round");
            AssertEqual(2, game.turn_count, "turn count after next round");
            AssertEqual(expectedAfterGrowth, game.GetPlayer(0).mana_max, "p0 second round mana_max");
            AssertEqual(expectedAfterGrowth, game.GetPlayer(0).mana, "p0 second round mana");
            AssertEqual(expectedAfterGrowth, game.GetPlayer(1).mana_max, "p1 second round mana_max");
            AssertEqual(expectedAfterGrowth, game.GetPlayer(1).mana, "p1 second round mana");
        }

        private static void AssertEqual(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }
}
#endif
