#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5DataRegistrationTests : Vc5LogicTestBase
    {
        [Test]
        public void LoadGameData_RemovesDestroyedStaticRegistryEntries()
        {
            CardData deadCard = ScriptableObject.CreateInstance<CardData>();
            deadCard.id = "vc5_test_destroyed_card";
            CardData.card_list.Add(deadCard);
            CardData.card_dict[deadCard.id] = deadCard;

            AbilityData deadAbility = ScriptableObject.CreateInstance<AbilityData>();
            deadAbility.id = "vc5_test_destroyed_ability";
            AbilityData.ability_list.Add(deadAbility);
            AbilityData.ability_dict[deadAbility.id] = deadAbility;

            DeckData deadDeck = ScriptableObject.CreateInstance<DeckData>();
            deadDeck.id = "vc5_test_destroyed_deck";
            DeckData.deck_list.Add(deadDeck);

            Object.DestroyImmediate(deadCard);
            Object.DestroyImmediate(deadAbility);
            Object.DestroyImmediate(deadDeck);

            Vc5LogicTestHarness.LoadGameData();

            Assert.IsFalse(CardData.card_list.Exists(item => item == null),
                "CardData registry must not retain destroyed Unity objects.");
            Assert.IsFalse(AbilityData.ability_list.Exists(item => item == null),
                "AbilityData registry must not retain destroyed Unity objects.");
            Assert.IsFalse(DeckData.deck_list.Exists(item => item == null),
                "DeckData registry must not retain destroyed Unity objects.");
            Assert.IsFalse(CardData.card_dict.ContainsKey("vc5_test_destroyed_card"),
                "Destroyed card dictionary entries must be removed.");
            Assert.IsFalse(AbilityData.ability_dict.ContainsKey("vc5_test_destroyed_ability"),
                "Destroyed ability dictionary entries must be removed.");
        }
    }
}
#endif
