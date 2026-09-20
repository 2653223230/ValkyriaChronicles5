#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.Gameplay;
using TcgEngine.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5R4Tests : Vc5LogicTestBase
    {
        [Test]
        public void DeployCards_UseDedicatedSpellArtworkInsteadOfCharacterPortraits()
        {
            CardData cover = CardData.Get(Vc5R4Rules.Prefix + "cover_deploy");
            CardData watch = CardData.Get(Vc5R4Rules.Prefix + "watch_deploy");

            Assert.AreEqual("Assets/TcgEngine/Resources/VC5/DemoArt/vc5_demo_r4_cover_deploy.png",
                AssetDatabase.GetAssetPath(cover.art_full));
            Assert.AreEqual("Assets/TcgEngine/Resources/VC5/DemoArt/vc5_demo_r4_watch_deploy.png",
                AssetDatabase.GetAssetPath(watch.art_full));
        }

        [Test]
        public void Commander_DisplayTextListsAllTemporaryOrdersAndLimits()
        {
            CardData commander = CardData.Get(Vc5R4Rules.Commander);
            string text = commander.GetDisplayText();
            StringAssert.Contains("推进（1费）：其他友军移动最多 2 格", text);
            StringAssert.Contains("开火（1费）：其他友军攻击，基础伤害为自身攻击力", text);
            StringAssert.DoesNotContain("攻击力−1", text);
            StringAssert.Contains("掩护（0费）：其他友军获得 2 护盾", text);
            StringAssert.Contains("*指令仅本回合可用，只能用于射程内其他友军", text);

            string detail = CardPreviewUI.BuildAdditionalDescription(commander);
            StringAssert.Contains("推进（1费）：其他友军移动最多 2 格", detail);
            StringAssert.Contains("开火（1费）：其他友军攻击，基础伤害为自身攻击力", detail);
            StringAssert.Contains("掩护（0费）：其他友军获得 2 护盾", detail);

            CardData fireOrder = CardData.Get("vc5_demo_r4_fire_order");
            StringAssert.Contains("指挥范围内另一名友军攻击其自身射程内最近敌人", fireOrder.GetDisplayText());
            StringAssert.Contains("基础伤害为自身攻击力", fireOrder.GetDisplayText());
            StringAssert.DoesNotContain("攻击力 -1", fireOrder.GetDisplayText());

            AbilityData chooseOrder = AbilityData.Get(Vc5R4Rules.ChooseOrder);
            StringAssert.Contains("基础伤害为自身攻击力", chooseOrder.chain_abilities[1].desc);
        }

        [Test]
        public void Commander_DescriptionLayoutUsesApprovedReadableSizeWithoutBestFit()
        {
            MethodInfo apply = typeof(CardPreviewUI).GetMethod("ApplyDescriptionLayout",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(apply, "Commander detail needs a dedicated readable layout instead of shrinking the text.");

            GameObject go = new GameObject("CommanderDescriptionLayoutTest");
            try
            {
                Text text = go.AddComponent<Text>();
                text.fontSize = 24;
                text.resizeTextForBestFit = true;
                text.rectTransform.sizeDelta = new Vector2(329.2196f, 336.6f);
                text.rectTransform.anchoredPosition = new Vector2(8.7998f, -279.94f);

                apply.Invoke(null, new object[]
                {
                    text,
                    CardData.Get(Vc5R4Rules.Commander),
                    24,
                    new Vector2(329.2196f, 336.6f),
                    new Vector2(8.7998f, -279.94f),
                });

                Assert.IsFalse(text.resizeTextForBestFit);
                Assert.AreEqual(24, text.fontSize);
                Assert.AreEqual(1f, text.lineSpacing, 0.001f);
                Assert.AreEqual(476f, text.rectTransform.sizeDelta.x, 1f);
                Assert.AreEqual(312f, text.rectTransform.sizeDelta.y, 1f);
                Assert.AreEqual(66f, text.rectTransform.anchoredPosition.x, 1f);

                apply.Invoke(null, new object[]
                {
                    text,
                    CardData.Get(Vc5R4Rules.Ranger),
                    24,
                    new Vector2(329.2196f, 336.6f),
                    new Vector2(8.7998f, -279.94f),
                });

                Assert.AreEqual(1f, text.lineSpacing);
                Assert.AreEqual(329.2196f, text.rectTransform.sizeDelta.x, 0.01f);
                Assert.AreEqual(8.7998f, text.rectTransform.anchoredPosition.x, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void LegacyCharge_KilledByWatch_DoesNotExecuteFollowingDamageEffect()
        {
            GameLogic logic = Setup(out Game game);
            Card watcher = Place(game, 0, "sniper", 3, 3);
            watcher.r4_watch = true;
            Card mover = Vc5LogicTestHarness.PlaceOnBoard(game, 1, "vc5_demo_cavalry",
                Vc5DemoGrid.ToPerspective(new Slot(8, 3, Slot.GetP(0)), Slot.GetP(1)));
            mover.damage = 4;
            game.current_player = 1;
            game.ability_triggerer = mover.uid;
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 1, "vc5_demo_decisive_charge");
            AbilityData ability = AbilityData.Get("vc5_demo_decisive_charge_target");
            foreach (EffectData effect in ability.effects) effect.DoEffect(logic, ability, spell, watcher);
            Assert.IsFalse(game.IsOnBoard(mover));
            Assert.AreEqual(0, watcher.damage);
        }

        [Test]
        public void R4State_RoundTripsThroughExistingNetworkSerializer()
        {
            Setup(out Game game);
            Card commander = Place(game, 0, "commander", 3, 3);
            commander.r4_watch = true;
            Vc5R4Rules.Shield(commander, 2);
            Card order = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_r4_cover_order");
            order.r4_commander_uid = commander.uid;
            Game copy = NetworkTool.Deserialize<Game>(NetworkTool.Serialize(game));
            Assert.IsTrue(copy.GetCard(commander.uid).r4_watch);
            Assert.AreEqual(2, copy.GetCard(commander.uid).r4_shield);
            Assert.AreEqual(commander.uid, copy.GetCard(order.uid).r4_commander_uid);
        }

        [Test]
        public void Advance_MovesTwoBeyondCommandRange_AndCancelRefunds()
        {
            GameLogic logic = Setup(out Game game);
            Card commander = Place(game, 0, "commander", 3, 3);
            Card sniper = Place(game, 0, "sniper", 5, 3);
            Card order = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_r4_advance_order");
            order.r4_commander_uid = commander.uid;
            int mana = game.players[0].mana;
            logic.PlayCard(order, sniper.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.CancelSelection();
            Assert.AreEqual(mana, game.players[0].mana);
            Assert.Contains(order, game.players[0].cards_hand);
            logic.PlayCard(order, sniper.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            logic.SelectSlot(new Slot(7, 3, sniper.slot.p));
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(7, sniper.slot.x);
            Assert.AreEqual(mana - 1, game.players[0].mana);
            Assert.IsTrue(sniper.HasStatus(StatusType.Vc5C3MovedThisTurn));
            Assert.AreEqual(0, game.current_player);
        }

        [Test]
        public void WatchSurvivesOwnerPriority_ExpiresAtRoundEnd_TemporaryOrdersExpireBeforeEndDiscard()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "sniper", 3, 3);
            actor.r4_watch = true;
            game.current_player = 1;
            logic.StartMainPhase();
            Assert.IsTrue(actor.r4_watch);
            game.current_player = 0;
            logic.StartMainPhase();
            Assert.IsTrue(actor.r4_watch);
            Card order = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_r4_cover_order");
            Assert.IsFalse(game.players[0].GetDiscardCostCards().Contains(order));
            game.players[1].EndTurn = true;
            logic.EndTurn();
            Assert.IsFalse(game.players[0].cards_hand.Contains(order));
            Assert.IsFalse(game.players[0].cards_discard.Contains(order));
            Assert.IsTrue(actor.r4_watch);
            Vc5R4Rules.EndRound(game);
            Assert.IsFalse(actor.r4_watch);
        }

        [Test]
        public void WatchDeployment_ImmediateNearestHit_PreviewMatches_ConsumesAgainstShield()
        {
            GameLogic logic = Setup(out Game game);
            Card watcher = Place(game, 0, "mobile_ranger", 3, 3);
            Card far = Place(game, 1, "commander", 6, 3);
            Card near = Place(game, 1, "commander", 5, 3);
            Vc5R4Rules.Shield(near, 2);
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_r4_watch_deploy");
            Slot destination = new Slot(4, 3, watcher.slot.p);
            Vc5C3Preview preview = Vc5C3Preview.Build(game, spell, watcher, destination);
            Assert.IsTrue(preview.damage.ContainsKey(near.uid), "Landing must preview the immediate shot.");
            Assert.AreEqual(0, preview.damage[near.uid]);
            Assert.AreEqual(2, near.r4_shield, "Preview cannot mutate live state.");
            Vc5C3Rules.Execute(logic, spell, watcher, destination);
            Assert.IsFalse(watcher.r4_watch);
            Assert.AreEqual(1, near.r4_shield);
            Assert.AreEqual(0, far.damage);
            Assert.IsTrue(watcher.HasStatus(StatusType.Vc5C3MobileFire), "Watch cannot consume damage-card bonus.");
        }

        [Test]
        public void Watch_BuffsKeepIt_AttackClearsIt_EvenWhenShieldAbsorbs()
        {
            GameLogic logic = Setup(out Game game);
            Card watcher = Place(game, 0, "sniper", 3, 3);
            Card commander = Place(game, 0, "commander", 3, 2);
            Card enemy = Place(game, 1, "commander", 5, 3);
            watcher.r4_watch = true;
            foreach (string name in new[] { "temp_calibration", "scope_upgrade", "cover_order" })
            {
                Card buff = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5R4Rules.Prefix + name);
                buff.r4_commander_uid = commander.uid;
                Vc5C3Rules.Execute(logic, buff, watcher);
                Assert.IsTrue(watcher.r4_watch, name);
            }
            Vc5R4Rules.Shield(enemy, 9);
            Vc5C3Rules.Execute(logic, Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5R4Rules.Prefix + "heavy_break"), watcher);
            Assert.AreEqual(0, enemy.damage);
            Assert.IsFalse(watcher.r4_watch);
        }

        [Test]
        public void Watch_CommanderCancelKeepsIt_CompletedSkillClearsIt()
        {
            GameLogic logic = Setup(out Game game);
            Card commander = Place(game, 0, "commander", 3, 3);
            commander.r4_watch = true;
            Card discard = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5R4Rules.Prefix + "scope_upgrade");
            AbilityData skill = AbilityData.Get(Vc5R4Rules.PrepareSkill);
            logic.CastAbility(commander, skill);
            logic.SelectCard(discard);
            logic.CancelSelection();
            Assert.IsTrue(commander.r4_watch);
            logic.CastAbility(commander, skill);
            logic.SelectCard(discard);
            logic.SelectChoice(0);
            Assert.IsFalse(commander.r4_watch);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Watch_EntryWorksForBothPlayers_OnlyOnce(int owner)
        {
            GameLogic logic = Setup(out Game game);
            Card watcher = Place(game, owner, "sniper", 3, 3);
            Card mover = Place(game, 1 - owner, "commander", 8, 3);
            watcher.r4_watch = true;
            game.current_player = 1 - owner;
            Slot destination = Vc5DemoGrid.ToPerspective(new Slot(6, 3, Slot.GetP(0)), mover.slot.p);
            logic.MoveCard(mover, destination, true, true);
            Assert.AreEqual(1, mover.damage);
            Assert.IsFalse(watcher.r4_watch);
            logic.MoveCard(mover, Vc5DemoGrid.ToPerspective(new Slot(5, 3, Slot.GetP(0)), mover.slot.p), true, true);
            Assert.AreEqual(1, mover.damage);
        }

        [Test]
        public void Watch_MovementEndingInRangeTriggers_EvenWhenMoverStartedInside()
        {
            GameLogic logic = Setup(out Game game);
            Card watcher = Place(game, 0, "mobile_ranger", 3, 3);
            Slot moverSlot = Vc5DemoGrid.ToPerspective(new Slot(5, 3, Slot.GetP(0)), Slot.GetP(1));
            Card mover = Vc5LogicTestHarness.PlaceOnBoard(game, 1, Vc5BAI1Rules.Rifleman, moverSlot);
            watcher.r4_watch = true;
            game.current_player = 1;
            logic.MoveCard(mover, Vc5DemoGrid.ToPerspective(new Slot(4, 3, Slot.GetP(0)), mover.slot.p), true, true);
            Assert.AreEqual(1, mover.damage);
            Assert.IsFalse(watcher.r4_watch);
        }

        [Test]
        public void RuntimeStatusText_IncludesR4ShieldWatchAndOrdinaryBuffs()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "mobile_ranger", 3, 3);
            actor.r4_shield = 2;
            actor.r4_shield_rounds = 2;
            actor.r4_watch = true;
            actor.AddStatus(StatusType.Vc5C3PermanentRange, 1, 0);

            var method = typeof(TcgEngine.Client.Vc5StatusDisplay).GetMethod("FormatAll",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method, "The single left detail panel needs one runtime status formatter.");
            string text = (string)method.Invoke(null, new object[] { actor });
            StringAssert.Contains("护盾 2", text);
            StringAssert.Contains("警戒", text);
            StringAssert.Contains("改装：射程 +1", text);
        }

        [Test]
        public void Watch_MultipleWatchers_StopAfterLethalEntry_SecondRemains()
        {
            GameLogic logic = Setup(out Game game);
            Card first = Place(game, 0, "sniper", 3, 3);
            Card second = Place(game, 0, "sniper", 3, 2);
            second.AddStatus(StatusType.Vc5C3PermanentRange, 1, 0);
            first.r4_watch = second.r4_watch = true;
            Card mover = Place(game, 1, "mobile_ranger", 8, 3);
            mover.damage = 4;
            game.current_player = 1;
            logic.MoveCard(mover, Vc5DemoGrid.ToPerspective(new Slot(5, 3, Slot.GetP(0)), mover.slot.p), true, true);
            Assert.IsFalse(game.IsOnBoard(mover));
            Assert.AreEqual(1, new[] { first, second }.Count(c => c.r4_watch), "Only the first lethal reaction is consumed.");
        }

        [Test]
        public void Commander_CancelsFreely_CommitsChosenDiscardAndOneOrder()
        {
            GameLogic logic = Setup(out Game game);
            Card commander = Place(game, 0, "commander", 3, 3);
            Card discard = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_r4_scope_upgrade");
            AbilityData skill = AbilityData.Get("vc5_demo_r4_prepare");
            int mana = game.players[0].mana;
            logic.CastAbility(commander, skill);
            logic.SelectCard(discard);
            Assert.AreEqual(SelectorType.SelectorChoice, game.selector);
            logic.CancelSelection();
            Assert.Contains(discard, game.players[0].cards_hand);
            Assert.IsFalse(commander.IsAbilityOnCooldown(skill));
            logic.CastAbility(commander, skill);
            logic.SelectCard(discard);
            logic.SelectChoice(1);
            Card order = game.players[0].cards_hand.Single(c => c.card_id == "vc5_demo_r4_fire_order");
            Assert.AreEqual(commander.uid, order.r4_commander_uid);
            Assert.Contains(discard, game.players[0].cards_discard);
            Assert.IsTrue(commander.IsAbilityOnCooldown(skill));
            Assert.AreEqual(mana, game.players[0].mana);
            Assert.AreEqual(0, game.current_player);
        }

        [Test]
        public void Ranger_CancelIsFree_CommitCostsOneAndSetsPassive()
        {
            GameLogic logic = Setup(out Game game);
            Card ranger = Place(game, 0, "mobile_ranger", 3, 3);
            AbilityData skill = AbilityData.Get("vc5_demo_r4_short_move");
            int mana = game.players[0].mana;
            logic.CastAbility(ranger, skill);
            logic.CancelSelection();
            Assert.AreEqual(mana, game.players[0].mana);
            Assert.IsFalse(ranger.IsAbilityOnCooldown(skill));
            logic.CastAbility(ranger, skill);
            logic.SelectSlot(new Slot(4, 3, ranger.slot.p));
            Assert.AreEqual(4, ranger.slot.x);
            Assert.AreEqual(mana - 1, game.players[0].mana);
            Assert.IsTrue(ranger.HasStatus(StatusType.Vc5C3MobileFire));
            Assert.IsTrue(ranger.IsAbilityOnCooldown(skill));
        }

        [Test]
        public void Orders_UseCurrentCommandRange_AndFireUsesSniperPassive()
        {
            GameLogic logic = Setup(out Game game);
            Card commander = Place(game, 0, "commander", 3, 3);
            Card sniper = Place(game, 0, "sniper", 6, 3);
            Card enemy = Place(game, 1, "commander", 7, 3);
            Card order = Vc5LogicTestHarness.GiveHandCard(game, 0, "vc5_demo_r4_fire_order");
            order.r4_commander_uid = commander.uid;
            Assert.IsFalse(game.CanPlayCard(order, sniper.slot));
            commander.AddStatus(StatusType.Vc5C3TemporaryRange, 1, 1);
            Assert.IsTrue(game.CanPlayCard(order, sniper.slot));
            Vc5C3Preview preview = Vc5C3Preview.Build(game, order, sniper);
            logic.PlayCard(order, sniper.slot);
            Vc5LogicTestHarness.FlushResolve(logic);
            Assert.AreEqual(4, enemy.damage);
            Assert.AreEqual(preview.damage[enemy.uid], enemy.damage);
            Assert.IsFalse(game.players[0].cards_discard.Contains(order));
            Assert.AreEqual(0, game.current_player);
        }

        [Test]
        public void Watch_PathEntryResolvesBeforeAttack_AndCloneDoesNotMutateRealState()
        {
            GameLogic logic = Setup(out Game game);
            Card watcher = Place(game, 0, "sniper", 3, 3);
            Card mover = Place(game, 1, "mobile_ranger", 8, 3);
            watcher.r4_watch = true;
            mover.damage = 4;
            game.current_player = 1;
            Card spell = Vc5LogicTestHarness.GiveHandCard(game, 1, "vc5_demo_r4_mobile_shot");
            Vc5C3Preview preview = Vc5C3Preview.Build(game, spell, mover);
            Assert.IsTrue(watcher.r4_watch);
            Assert.AreEqual(4, mover.damage);
            Assert.AreEqual(1, preview.damage[mover.uid]);
            Vc5C3Rules.Execute(logic, spell, mover);
            Assert.IsFalse(game.IsOnBoard(mover));
            Assert.AreEqual(0, watcher.damage, "Dead moving shooter must not finish its attack.");
            Assert.IsFalse(watcher.r4_watch);
        }

        [Test]
        public void Shield_ConsumesDoesNotStack_ExpiresAfterFollowingRound()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "sniper", 3, 3);
            Card enemy = Place(game, 1, "commander", 4, 3);
            Vc5R4Rules.Shield(actor, 2);
            logic.DamageCard(enemy, actor, 1);
            Assert.AreEqual(1, actor.r4_shield);
            Assert.AreEqual(0, actor.damage);
            Vc5R4Rules.Shield(actor, 2);
            Vc5R4Rules.Shield(actor, 1);
            Assert.AreEqual(2, actor.r4_shield);
            Vc5R4Rules.EndRound(game);
            Assert.AreEqual(2, actor.r4_shield);
            Vc5R4Rules.EndRound(game);
            Assert.AreEqual(0, actor.r4_shield);
        }

        [Test]
        public void R4CoverSources_StackWithoutLimit_RefreshAndExpire()
        {
            GameLogic logic = Setup(out Game game);
            Card actor = Place(game, 0, "sniper", 3, 3);
            Card commander = Place(game, 0, "commander", 3, 2);
            Vc5R4Rules.Shield(actor, 3);

            Card deploy = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5R4Rules.Prefix + "cover_deploy");
            Vc5C3Rules.Execute(logic, deploy, actor, new Slot(4, 3, actor.slot.p));
            for (int i = 0; i < 2; i++)
            {
                Card order = Vc5LogicTestHarness.GiveHandCard(game, 0, Vc5R4Rules.Prefix + "cover_order");
                order.r4_commander_uid = commander.uid;
                Vc5C3Rules.Execute(logic, order, actor);
            }

            Assert.AreEqual(8, actor.r4_shield, "R4 cover shields should add 3 + 1 + 2 + 2 without a cap.");
            Assert.AreEqual(2, actor.r4_shield_rounds);
            Vc5R4Rules.EndRound(game);
            Assert.AreEqual(8, actor.r4_shield);
            Vc5R4Rules.EndRound(game);
            Assert.AreEqual(0, actor.r4_shield);
        }

        static GameLogic Setup(out Game game)
        {
            GameLogic logic = Vc5LogicTestHarness.CreateLogic(out game, "deck_vc5_demo_command_r4", vc5TestMode: false);
            foreach (Player player in game.players) { player.cards_board.Clear(); player.cards_hand.Clear(); player.mana = 9; }
            game.current_player = 0;
            return logic;
        }
        static Card Place(Game game, int player, string suffix, int x, int y)
        {
            Slot slot = Vc5DemoGrid.ToPerspective(new Slot(x, y, Slot.GetP(0)), Slot.GetP(player));
            return Vc5LogicTestHarness.PlaceOnBoard(game, player, "vc5_demo_r4_" + suffix, slot);
        }

        [Test]
        public void IndependentDeck_MatchesApprovedCounts_AndLeavesOriginalC3Intact()
        {
            Vc5LogicTestHarness.LoadGameData();
            DeckData deck = DeckData.Get("deck_vc5_demo_command_r4");
            Assert.NotNull(deck, "Approved R4 must be a separately registered deck.");
            Assert.AreEqual(20, deck.cards.Length);
            Assert.AreEqual(3, deck.heroes.Length);
            string[] names = { "tactical_move", "forced_march", "temp_calibration", "scope_upgrade", "cover_deploy", "watch_deploy", "fire_coverage", "heavy_break", "weakpoint_snipe", "mobile_shot" };
            int[] counts = { 2, 2, 2, 2, 1, 2, 2, 2, 2, 3 };
            for (int i = 0; i < names.Length; i++)
                Assert.AreEqual(counts[i], deck.cards.Count(c => c.id == "vc5_demo_r4_" + names[i]), names[i]);
            DeckData old = DeckData.Get(Vc5DemoBootstrap.RangedPressureC3DeckId);
            Assert.AreEqual(20, old.cards.Length);
            Assert.AreEqual(4, old.cards.Count(c => c.id == Vc5C3Rules.Prefix + "mobile_shot"));
            Assert.IsFalse(deck.heroes.Any(h => old.heroes.Contains(h)));
        }

        [TestCase("advance_order", 1)]
        [TestCase("fire_order", 1)]
        [TestCase("cover_order", 0)]
        public void TemporaryOrders_UseLatestCosts_AndCannotBeDeckBuilt(string suffix, int mana)
        {
            CardData card = CardData.Get("vc5_demo_r4_" + suffix);
            Assert.NotNull(card);
            Assert.AreEqual(mana, card.mana);
            Assert.IsTrue(card.fast_action);
            Assert.IsFalse(card.deckbuilding);
        }
    }
}
#endif
