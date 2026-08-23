#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5DemoTutorialTests : Vc5LogicTestBase
    {
        private const string TutorialTypeName = "TcgEngine.UI.Vc5DemoTutorialOverlay, Assembly-CSharp";

        [Test]
        public void TutorialPolicy_ShowsOnlyForVc5DemoSoloDecks()
        {
            Type type = Type.GetType(TutorialTypeName);
            Assert.NotNull(type, "Vc5DemoTutorialOverlay should exist.");

            MethodInfo shouldShow = type.GetMethod("ShouldShowFor", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(shouldShow, "ShouldShowFor should expose the Demo-only policy.");

            Assert.IsTrue(InvokeShouldShow(shouldShow, GameType.Solo,
                Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId));
            Assert.IsTrue(InvokeShouldShow(shouldShow, GameType.Solo,
                Vc5DemoBootstrap.RangedPressureDeckId, Vc5DemoBootstrap.MobileAssaultDeckId));
            Assert.IsFalse(InvokeShouldShow(shouldShow, GameType.Multiplayer,
                Vc5DemoBootstrap.MobileAssaultDeckId, Vc5DemoBootstrap.RangedPressureDeckId));
            Assert.IsFalse(InvokeShouldShow(shouldShow, GameType.Solo,
                "old_deck", Vc5DemoBootstrap.RangedPressureDeckId));
        }

[Test]
        public void TutorialOverlay_BuildsTwoPageFigmaLayoutAndNavigatesBeforeDismiss()
        {
            Type type = Type.GetType(TutorialTypeName);
            Assert.NotNull(type, "Vc5DemoTutorialOverlay should exist.");

            MethodInfo show = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Transform) }, null);
            Assert.NotNull(show, "Show(Transform) should build the runtime overlay.");

            GameObject canvasObject = new GameObject("Tutorial Test Canvas");
            try
            {
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                Component overlay = show.Invoke(null, new object[] { canvas.transform }) as Component;
                Assert.NotNull(overlay);
                Assert.AreEqual("VC5 Demo Tutorial Overlay", overlay.gameObject.name);
                Assert.AreEqual(canvas.transform, overlay.transform.parent);
                Assert.AreEqual(canvas.transform.childCount - 1, overlay.transform.GetSiblingIndex());

                Image dim = FindComponent<Image>(overlay.transform, "DimMask");
                Assert.NotNull(dim);
                Assert.IsTrue(dim.raycastTarget, "The dim layer must block clicks on both tutorial pages.");

                Transform page1 = FindTransform(overlay.transform, "TutorialPage_1");
                Transform page2 = FindTransform(overlay.transform, "TutorialPage_2");
                Assert.NotNull(page1);
                Assert.NotNull(page2);
                Assert.IsTrue(page1.gameObject.activeSelf);
                Assert.IsFalse(page2.gameObject.activeSelf);

                string[] highlights =
                {
                    "Highlight_Hand", "Highlight_Board", "Highlight_Score", "Highlight_PhaseButton",
                    "Highlight_PlayerResources", "Highlight_PlayerUnits"
                };
                string[] borders = { "Border_Top", "Border_Bottom", "Border_Left", "Border_Right" };
                foreach (string highlight in highlights)
                {
                    Image image = FindComponent<Image>(overlay.transform, highlight);
                    Assert.NotNull(image, "Missing Figma highlight: " + highlight);
                    Assert.IsFalse(image.raycastTarget, highlight + " should leave the dim layer as raycast target.");
                    Assert.Less(image.color.a, 0.2f, highlight + " fill must remain translucent.");
                    foreach (string border in borders)
                        Assert.NotNull(FindComponent<Image>(image.transform, border), highlight + " is missing " + border);
                }

                string[] requiredText =
                {
                    "你只需要打牌",
                    "下一页：资源与技能",
                    "资源、得分与棋子技能",
                    "法力值与胜利分数",
                    "蓝色法力点用于打牌和主动技能；0/9 显示当前得分，先达到 9 分获胜。",
                    "两种得分方式",
                    "击杀敌方棋子",
                    "敌方棋子死亡时，你获得 3 分。",
                    "得分阶段",
                    "得分区人数更多的一方 +2 分；人数相同，双方各 +1 分。",
                    "查看与使用棋子技能",
                    "把鼠标移到棋子上，可以查看主动技能和被动技能。\n\n点击己方棋子，会显示可用主动技能；再点击技能按钮即可释放。被动技能会自动生效。",
                    "知道了，开始对战"
                };
                string[] actualText = overlay.GetComponentsInChildren<Text>(true).Select(text => text.text).ToArray();
                foreach (string expected in requiredText)
                    CollectionAssert.Contains(actualText, expected);

                Button next = FindComponent<Button>(overlay.transform, "NextButton");
                Button back = FindComponent<Button>(overlay.transform, "BackButton");
                Button confirm = FindComponent<Button>(overlay.transform, "ConfirmButton_Page2");
                Assert.NotNull(next);
                Assert.NotNull(back);
                Assert.NotNull(confirm);

                next.onClick.Invoke();
                Assert.IsFalse(page1.gameObject.activeSelf);
                Assert.IsTrue(page2.gameObject.activeSelf);
                Assert.IsFalse(overlay == null, "Next must not dismiss the overlay.");

                back.onClick.Invoke();
                Assert.IsTrue(page1.gameObject.activeSelf);
                Assert.IsFalse(page2.gameObject.activeSelf);

                next.onClick.Invoke();
                confirm.onClick.Invoke();
                Assert.IsTrue(overlay == null, "Only the final page confirm should dismiss the overlay.");
            }
            finally
            {
                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static bool InvokeShouldShow(MethodInfo method, GameType type, string playerDeck, string aiDeck)
        {
            return (bool)method.Invoke(null, new object[] { type, playerDeck, aiDeck });
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            Transform target = transforms.FirstOrDefault(item => item.name == name);
            return target != null ? target.GetComponent<T>() : null;
        }
    

private static Transform FindTransform(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
        }
}
}
#endif
