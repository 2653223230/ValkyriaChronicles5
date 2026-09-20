#if UNITY_EDITOR
using NUnit.Framework;
using TcgEngine.Client;
using TcgEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5DiscardSelectionTests
    {
        [Test]
        public void EndDiscard_StagesMultipleUidsAndSecondClickCancelsWithoutChangingHand()
        {
            var obj = new GameObject("Discard selection test");
            try
            {
                var area = obj.AddComponent<HandCardArea>();
                var player = new Player(0);
                var first = new Card("", "first", 0);
                var second = new Card("", "second", 0);
                player.cards_hand.Add(first);
                player.cards_hand.Add(second);
                area.ToggleDiscardSelection(first.uid);
                area.ToggleDiscardSelection(second.uid);
                Assert.AreEqual(2, area.DiscardSelectionCount);
                area.ToggleDiscardSelection(first.uid);
                Assert.IsFalse(area.IsDiscardSelected(first.uid));
                Assert.IsTrue(area.IsDiscardSelected(second.uid));
                Assert.AreEqual(2, player.cards_hand.Count);
                area.ClearDiscardSelection();
                Assert.AreEqual(0, area.DiscardSelectionCount);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void EndDiscard_LastClickedCardDrivesLeftDetailUntilPhaseEnds()
        {
            var first = new Card("", "first", 0);
            var second = new Card("", "second", 0);
            var previewType = typeof(CardPreviewUI);
            var select = previewType.GetMethod("SelectEndDiscardCard", BindingFlags.Public | BindingFlags.Static);
            var resolve = previewType.GetMethod("ResolvePreviewCard", BindingFlags.Public | BindingFlags.Static);
            var shouldShow = previewType.GetMethod("ShouldShowPreview", BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(select, "EndDiscard clicks need an explicit left-detail selection entry point.");
            Assert.IsNotNull(resolve);
            Assert.IsNotNull(shouldShow);

            select.Invoke(null, new object[] { first });
            Assert.AreSame(first, resolve.Invoke(null, new object[] { GamePhase.EndDiscard, second }));
            Assert.AreEqual(true, shouldShow.Invoke(null,
                new object[] { GamePhase.EndDiscard, first, true, true }),
                "The clicked card must remain visible even while the discard UI is open.");

            select.Invoke(null, new object[] { second });
            Assert.AreSame(second, resolve.Invoke(null, new object[] { GamePhase.EndDiscard, first }));
            Assert.AreSame(first, resolve.Invoke(null, new object[] { GamePhase.Main, first }),
                "Leaving EndDiscard must clear the pinned card and resume normal focus preview.");
            Assert.IsNull(resolve.Invoke(null, new object[] { GamePhase.EndDiscard, null }));
        }

        [Test]
        public void CommanderDiscard_ClickCanDeselectAndSwitchWithoutCommitting()
        {
            var obj = new GameObject("Commander selection test");
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            try
            {
                var selector = obj.AddComponent<CardSelector>();
                ability.id = Vc5R4Rules.PrepareSkill;
                typeof(CardSelector).GetField("iability", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(selector, ability);
                selector.OnClickCard(0);
                Assert.AreEqual(0, selector.SelectionIndex);
                selector.OnClickCard(0);
                Assert.AreEqual(-1, selector.SelectionIndex);
                selector.OnClickCard(1);
                selector.OnClickCard(2);
                Assert.AreEqual(2, selector.SelectionIndex);
            }
            finally { Object.DestroyImmediate(obj); Object.DestroyImmediate(ability); }
        }

        [Test]
        public void CommanderDiscard_ClickingVisibleCardUIReachesSelector()
        {
            var root = new GameObject("Commander selector root");
            var cardObject = new GameObject("Selector card");
            var uiObject = new GameObject("Visible card UI");
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            try
            {
                var selector = root.AddComponent<CardSelector>();
                ability.id = Vc5R4Rules.PrepareSkill;
                typeof(CardSelector).GetField("iability", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(selector, ability);

                cardObject.transform.SetParent(root.transform, false);
                var selectorCard = cardObject.AddComponent<CardSelectorCard>();
                selectorCard.SetIndex(0);
                uiObject.transform.SetParent(cardObject.transform, false);
                var cardUi = uiObject.AddComponent<CardUI>();
                selectorCard.card_ui = cardUi;
                typeof(CardSelectorCard).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(selectorCard, null);

                cardUi.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                Assert.AreEqual(0, selector.SelectionIndex,
                    "The visible inner CardUI must forward the real click to the commander discard selector.");
                cardUi.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                Assert.AreEqual(-1, selector.SelectionIndex);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(ability);
            }
        }
    }
}
#endif
