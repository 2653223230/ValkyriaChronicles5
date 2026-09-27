#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
    public class Vc5StartupCacheTests
    {
        [Test]
        public void RunStartup_ReplacesPartialCacheBeforeBuildingDemoDecks()
        {
            var savedTraits = new List<TraitData>(TraitData.trait_list);
            try
            {
                TraitData.trait_list.Clear();
                TraitData.trait_list.Add(Resources.LoadAll<TraitData>("")[0]);
                var reset = typeof(DataLoader).GetMethod("ResetRuntimeRegistries", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(reset, "Run startup must discard stale static registries before loading Resources.");
                reset.Invoke(null, null);
                TeamData.Load(); RarityData.Load(); TraitData.Load(); StatusData.Load();
                CardData.Load(); AbilityData.Load(); DeckData.Load();
                Vc5DemoBootstrap.Register();
                foreach (var source in Resources.LoadAll<TraitData>(""))
                    Assert.NotNull(TraitData.Get(source.id), "Resource dependency missing from cache: " + source.id);
                foreach (string id in new[] {"deck_vc5_demo_command_r4", "deck_vc5_demo_steady_assault", "deck_vc5_demo_ranged_pressure_c3"})
                {
                    DeckData deck = DeckData.Get(id);
                    Assert.NotNull(deck, id);
                    Assert.AreEqual(3, deck.heroes.Length, id);
                    Assert.Greater(deck.cards.Length, 0, id);
                    foreach (CardData card in deck.cards)
                    {
                        Assert.NotNull(card, id);
                        foreach (AbilityData ability in card.abilities)
                        {
                            Assert.NotNull(ability, card.id);
                            foreach (AbilityData chain in ability.chain_abilities)
                                Assert.NotNull(chain, ability.id);
                        }
                    }
                }
            }
            finally
            {
                TraitData.trait_list.Clear();
                TraitData.trait_list.AddRange(savedTraits);
            }
        }
    }
}
#endif
