#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.Client;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DemoMatchSetupTests : Vc5LogicTestBase
    {
        [Test]
        public void DemoMatchSetup_ProvidesDefaultPlayerAndAIDecks()
        {
            Type type = GetSetupType();
            Assert.AreEqual(Vc5DemoBootstrap.MobileAssaultDeckId, InvokeString(type, "GetDefaultPlayerDeckId"));
            Assert.AreEqual(Vc5DemoBootstrap.RangedPressureDeckId, InvokeString(type, "GetDefaultAIDeckId"));
        }

        [Test]
        public void DemoMatchSetup_AppliesSelectedDecksToSoloGameClientSettings()
        {
            Type type = GetSetupType();
            MethodInfo apply = type.GetMethod("ApplySoloAIMatch", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(apply, "Vc5DemoMatchSetup.ApplySoloAIMatch should exist.");

            bool applied = (bool)apply.Invoke(null, new object[]
            {
                Vc5DemoBootstrap.RangedPressureDeckId,
                Vc5DemoBootstrap.MobileAssaultDeckId
            });

            Assert.IsTrue(applied);
            Assert.AreEqual(GameType.Solo, GameClient.game_settings.game_type);
            Assert.AreEqual(GameMode.Casual, GameClient.game_settings.game_mode);
            Assert.AreEqual(Vc5DemoBootstrap.RangedPressureDeckId, GameClient.player_settings.deck.tid);
            Assert.AreEqual(Vc5DemoBootstrap.MobileAssaultDeckId, GameClient.ai_settings.deck.tid);
            Assert.AreEqual(GameplayData.Get().ai_level, GameClient.ai_settings.ai_level);
            Assert.IsTrue(GameClient.player_settings.deck.IsValid());
            Assert.IsTrue(GameClient.ai_settings.deck.IsValid());
        }

        [Test]
        public void DemoMatchSetup_RejectsMissingDeckWithoutChangingCurrentSelections()
        {
            Type type = GetSetupType();
            MethodInfo apply = type.GetMethod("ApplySoloAIMatch", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(apply, "Vc5DemoMatchSetup.ApplySoloAIMatch should exist.");

            apply.Invoke(null, new object[]
            {
                Vc5DemoBootstrap.MobileAssaultDeckId,
                Vc5DemoBootstrap.RangedPressureDeckId
            });

            bool applied = (bool)apply.Invoke(null, new object[]
            {
                "missing_player_deck",
                Vc5DemoBootstrap.MobileAssaultDeckId
            });

            Assert.IsFalse(applied);
            Assert.AreEqual(Vc5DemoBootstrap.MobileAssaultDeckId, GameClient.player_settings.deck.tid);
            Assert.AreEqual(Vc5DemoBootstrap.RangedPressureDeckId, GameClient.ai_settings.deck.tid);
        }

        private static Type GetSetupType()
        {
            Type type = Type.GetType("TcgEngine.UI.Vc5DemoMatchSetup, Assembly-CSharp");
            Assert.NotNull(type, "Vc5DemoMatchSetup type should exist.");
            return type;
        }

        private static string InvokeString(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Missing method: " + methodName);
            return method.Invoke(null, null) as string;
        }
    }
}
#endif
