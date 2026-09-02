#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TcgEngine.Client;
using TcgEngine.Server;
using TcgEngine.UI;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace TcgEngine.Testing.Editor
{
    public class Vc5C3RuntimeTests
    {
        [UnityTest, Category("VC5C3Runtime")]
        public IEnumerator MenuStartsC3Match_AndPreviewRendersAtTwoResolutions()
        {
            EditorSceneManager.OpenScene("Assets/TcgEngine/Scenes/Menu/Menu.unity");
            yield return new EnterPlayMode();
            yield return WaitUntil(() => Object.FindObjectOfType<MainMenu>() != null, 15);
            for (int i = 0; i < 30; i++) yield return null;
            MainMenu menu = Object.FindObjectOfType<MainMenu>();
            Assert.IsTrue(Vc5DemoMatchSetup.TryStartSoloAIMatch(menu, Vc5DemoBootstrap.RangedPressureC3DeckId,
                Vc5DemoBootstrap.MobileAssaultDeckId, out string error), error);
            yield return WaitUntil(() => GameClient.Get() != null && GameClient.Get().IsReady()
                && Object.FindObjectOfType<Vc5C3PreviewOverlay>() != null, 30);
            GameClient client = GameClient.Get();
            Assert.AreEqual(0, client.GetGameData().first_player);
            Assert.AreEqual(3, client.GetPlayer().cards_board.Count);
            var tutorial = Object.FindObjectOfType<Vc5DemoTutorialOverlay>();
            Assert.NotNull(tutorial);
            Assert.Greater(client.GetPlayer().cards_hand.Count, 0);
            Assert.AreEqual(Vc5DemoTutorialOverlay.MobileShotId, client.GetPlayer().cards_hand[0].card_id,
                "C3 tutorial opening card is not the first card in the runtime hand.");
            for (int i = 0; i < 20; i++) yield return null;
            CaptureTutorialStates(tutorial);
            yield return null;

            // Freeze only this test's server and build a deterministic visible preview fixture.
            foreach (ServerManagerLocal manager in Object.FindObjectsOfType<ServerManagerLocal>()) manager.enabled = false;
            for (int i = 0; i < 15; i++) yield return null;
            PrepareFixture();
            yield return WaitForAnimation();
            VerifyTargetLine();
            CaptureFixture();
            yield return new ExitPlayMode();
        }

        // Keep lambda-captured locals outside the iterator that crosses EnterPlayMode's domain reload.
        static void PrepareFixture()
        {
            Game state = GameClient.Get().GetGameData();
            Assert.NotNull(state, "Client game data");
            Assert.NotNull(state.players[0], "Client player zero");
            Assert.NotNull(state.players[0].cards_board, "Client board list");
            Card actor = null;
            for (int i = 0; i < state.players[0].cards_board.Count; i++)
            {
                Card candidate = state.players[0].cards_board[i];
                Assert.NotNull(candidate, "Null client board entry " + i);
                TestContext.WriteLine("Board entry: " + candidate.card_id);
                if (candidate.card_id == Vc5C3Rules.Ranger) actor = candidate;
            }
            Assert.NotNull(actor, "C3 ranger missing from runtime board.");
            actor.slot = new Slot(3, 3, Slot.GetP(0));
            Card[] others = state.players[0].cards_board.Where(c => c != actor).ToArray();
            others[0].slot = new Slot(3, 1, Slot.GetP(0));
            others[1].slot = new Slot(3, 5, Slot.GetP(0));
            int[,] cells = { { 7, 3 }, { 7, 2 }, { 7, 4 } };
            for (int i = 0; i < 3; i++)
                state.players[1].cards_board[i].slot = Vc5DemoGrid.ToPerspective(new Slot(cells[i, 0], cells[i, 1], Slot.GetP(0)), Slot.GetP(1));
            Vc5LogicTestHarness.GiveHandCard(state, 0, Vc5C3Rules.Prefix + "mobile_shot");
            state.players[0].mana = 9;
        }

        static void CaptureFixture()
        {
            Vc5C3PreviewOverlay overlay = Object.FindObjectOfType<Vc5C3PreviewOverlay>();
            overlay.enabled = false;
            Game live = GameClient.Get().GetGameData();
            Card actor = live.players[0].cards_board.First(c => c.card_id == Vc5C3Rules.Ranger);
            Card shot = live.players[0].cards_hand.First(c => c.card_id == Vc5C3Rules.Prefix + "mobile_shot");
            Directory.CreateDirectory("TestResults");
            Capture(overlay, live, live.GetCard(shot.uid), live.GetCard(actor.uid), 1920, 1080);
            Capture(overlay, live, live.GetCard(shot.uid), live.GetCard(actor.uid), 1280, 720);
            Assert.IsTrue(overlay.Preview.plan.valid);
            Assert.Greater(overlay.Preview.plan.path.Count, 1);
            Assert.Greater(overlay.Preview.damage.Count, 0);
            Assert.IsTrue(overlay.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g => !g.raycastTarget));
            VerifyCardDetail(overlay, live, shot, actor);
        }

        static void CaptureTutorialStates(Vc5DemoTutorialOverlay tutorial)
        {
            Directory.CreateDirectory("TestResults");
            Assert.AreEqual(Vc5DemoTutorialOverlay.TutorialStep.Intro, tutorial.CurrentStep);
            CaptureCurrentView("tutorial-v21-01-intro", 1920, 1080);

            FindButton(tutorial.transform, "StartTutorialButton").onClick.Invoke();
            Assert.AreEqual(Vc5DemoTutorialOverlay.TutorialStep.WaitCardDrag, tutorial.CurrentStep);
            CaptureCurrentView("tutorial-v21-02-wait", 1920, 1080);

            typeof(Vc5DemoTutorialOverlay).GetMethod("SetStep",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(tutorial, new object[] { Vc5DemoTutorialOverlay.TutorialStep.Preview });
            CaptureCurrentView("tutorial-v21-03-preview", 1920, 1080);

            tutorial.ShowResultAfterSuccessfulPlay();
            CaptureCurrentView("tutorial-v21-04-result", 1920, 1080);
            FindButton(tutorial.transform, "ResultContinueButton").onClick.Invoke();
            CaptureCurrentView("tutorial-v21-05-hud", 1920, 1080);
            FindButton(tutorial.transform, "HudContinueButton").onClick.Invoke();
            CaptureCurrentView("tutorial-v21-06-score-flow", 1920, 1080);
            FindButton(tutorial.transform, "FinishTutorialButton").onClick.Invoke();
        }

        static UnityEngine.UI.Button FindButton(Transform root, string name)
        {
            Transform target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            Assert.NotNull(target, "Missing tutorial button: " + name);
            UnityEngine.UI.Button button = target.GetComponent<UnityEngine.UI.Button>();
            Assert.NotNull(button, "Tutorial object is not a button: " + name);
            return button;
        }

        static void CaptureCurrentView(string name, int width, int height)
        {
            Camera[] cameras = Camera.allCameras.OrderBy(camera => camera.depth).ToArray();
            RenderTexture texture = RenderTexture.GetTemporary(width, height, 24);
            RenderTexture previous = RenderTexture.active;
            RenderTexture[] targets = cameras.Select(camera => camera.targetTexture).ToArray();
            float[] aspects = cameras.Select(camera => camera.aspect).ToArray();
            Texture2D pixels = null;
            try
            {
                foreach (Camera camera in cameras)
                {
                    camera.targetTexture = texture;
                    camera.aspect = (float)width / height;
                }
                Canvas.ForceUpdateCanvases();
                foreach (Camera camera in cameras)
                    camera.Render();
                RenderTexture.active = texture;
                pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes($"TestResults/{name}-{width}x{height}.png", pixels.EncodeToPNG());
                Assert.Greater(pixels.GetPixels32().Count(color => color.r > 160 || color.g > 160 || color.b > 160),
                    width * height / 100, name + " rendered blank.");
            }
            finally
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    cameras[i].targetTexture = targets[i];
                    cameras[i].aspect = aspects[i];
                }
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(texture);
                if (pixels != null)
                    Object.DestroyImmediate(pixels);
            }
        }

        static object InvokePrivate(object target, string method)
        {
            return target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        static void VerifyTargetLine()
        {
            Game game = GameClient.Get().GetGameData();
            Card actor = game.players[0].cards_board[0];
            var line = Object.FindObjectOfType<TcgEngine.FX.MouseLineFX>();
            Assert.NotNull(line);
            PlayerControls.Get().SelectCard(BoardCard.Get(actor.uid));
            Assert.NotNull(PlayerControls.Get().GetSelected());
            InvokePrivate(line, "RefreshLine");
            InvokePrivate(line, "RefreshRender");
            Assert.AreEqual(0, line.GetComponentsInChildren<Renderer>().Length, "Passive selection leaves a targeting line");
            game.selector = SelectorType.SelectTarget;
            game.selector_player_id = 0;
            game.selector_caster_uid = actor.uid;
            InvokePrivate(line, "RefreshLine");
            InvokePrivate(line, "RefreshRender");
            Assert.Greater(line.GetComponentsInChildren<Renderer>().Length, 0, "Actual target selection lost its line");
            line.enabled = false;
            Assert.AreEqual(0, line.GetComponentsInChildren<Renderer>().Length, "Disabled effect leaves dots");
            line.enabled = true;
            game.selector = SelectorType.None;
            InvokePrivate(line, "RefreshLine");
            InvokePrivate(line, "RefreshRender");
            Assert.AreEqual(0, line.GetComponentsInChildren<Renderer>().Length, "Canceled target selection leaves dots");
            PlayerControls.Get().UnselectAll();
        }

        static void VerifyCardDetail(Vc5C3PreviewOverlay overlay, Game game, Card spell, Card actor)
        {
            var preview = Object.FindObjectOfType<CardPreviewUI>();
            var feedback = Object.FindObjectOfType<Vc5DemoBattleFeedback>();
            Assert.NotNull(preview);
            Assert.NotNull(feedback);
            Transform score = feedback.transform.Find("ScorePanel");
            Transform log = feedback.transform.Find("ActionLog");
            preview.enabled = false;
            preview.card_ui.SetCard(spell);
            preview.desc.text = CardPreviewUI.BuildAdditionalDescription(spell.CardData);
            preview.ui_panel.Show(true);
            InvokePrivate(preview, "LateUpdate");
            Assert.IsFalse(score.gameObject.activeSelf, "Score summary covers card detail");
            Assert.IsFalse(log.gameObject.activeSelf, "Action log covers card detail");
            Capture(overlay, game, spell, actor, 1920, 1080, "card-detail");
            Capture(overlay, game, spell, actor, 1280, 720, "card-detail");
            preview.card_ui.SetCard(actor);
            preview.desc.text = CardPreviewUI.BuildAdditionalDescription(actor.CardData);
            Capture(overlay, game, spell, actor, 1280, 720, "hero-detail");
            preview.ui_panel.Hide(false);
            InvokePrivate(preview, "LateUpdate");
            Assert.IsFalse(score.gameObject.activeSelf, "Panels return before detail finishes fading");
            preview.ui_panel.Hide(true);
            InvokePrivate(preview, "LateUpdate");
            Assert.IsTrue(score.gameObject.activeSelf);
            Assert.IsTrue(log.gameObject.activeSelf);
            preview.enabled = true;
        }

        static IEnumerator WaitUntil(System.Func<bool> ready, double seconds)
        {
            double end = UnityEditor.EditorApplication.timeSinceStartup + seconds;
            while (!ready() && UnityEditor.EditorApplication.timeSinceStartup < end) yield return null;
            Assert.IsTrue(ready(), "Runtime startup timed out.");
        }

        static IEnumerator WaitForAnimation()
        {
            double end = UnityEditor.EditorApplication.timeSinceStartup + 3;
            while (UnityEditor.EditorApplication.timeSinceStartup < end) yield return null;
        }

        static void Capture(Vc5C3PreviewOverlay overlay, Game game, Card spell, Card actor, int width, int height, string name = "c3-preview")
        {
            Camera[] cameras = Camera.allCameras.OrderBy(c => c.depth).ToArray();
            RenderTexture texture = RenderTexture.GetTemporary(width, height, 24);
            RenderTexture previous = RenderTexture.active;
            var targets = cameras.Select(c => c.targetTexture).ToArray();
            var aspects = cameras.Select(c => c.aspect).ToArray();
            Texture2D pixels = null;
            try
            {
                foreach (Camera camera in cameras) { camera.targetTexture = texture; camera.aspect = (float)width / height; }
                Canvas.ForceUpdateCanvases();
                overlay.Refresh(game, spell, actor);
                Canvas.ForceUpdateCanvases();
                foreach (BoardCard card in Object.FindObjectsOfType<BoardCard>())
                {
                    Assert.IsTrue(card.GetComponentInChildren<Canvas>(true).gameObject.activeSelf, "Board card has not finished appearing.");
                    Assert.NotNull(card.card_sprite.sprite, "Missing hero illustration");
                    Assert.LessOrEqual(card.card_sprite.sprite.bounds.size.x, 8.03f, "Hero art exceeds the board frame");
                }
                var previewGraphic = overlay.GetComponentInChildren<Vc5C3PreviewGraphic>();
                TestContext.WriteLine($"Preview graphic: enabled={previewGraphic.enabled}, active={previewGraphic.IsActive()}, culled={previewGraphic.canvasRenderer.cull}, materials={previewGraphic.canvasRenderer.materialCount}, alpha={previewGraphic.canvasRenderer.GetAlpha()}, rect={previewGraphic.rectTransform.rect}, layer={previewGraphic.gameObject.layer}, position={previewGraphic.transform.position}, scale={previewGraphic.transform.lossyScale}");
                foreach (Camera camera in cameras) TestContext.WriteLine($"Camera {camera.name}: mask={camera.cullingMask}, depth={camera.depth}, clips={camera.nearClipPlane}/{camera.farClipPlane}");
                using (var mesh = new UnityEngine.UI.VertexHelper())
                {
                    typeof(Vc5C3PreviewGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                        .Invoke(previewGraphic, new object[] { mesh });
                    Assert.Greater(mesh.currentVertCount, 0, "Preview range/path mesh is empty.");
                }
                foreach (Camera camera in cameras) camera.Render();
                RenderTexture.active = texture;
                pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes($"TestResults/{name}-{width}x{height}.png", pixels.EncodeToPNG());
                Color32[] colors = pixels.GetPixels32();
                Assert.Greater(colors.Count(c => c.r > 180 || c.g > 180 || c.b > 180), width * height / 100, "Rendered view is blank.");
                int routePixels = 0;
                for (int y = height / 4; y < height * 3 / 4; y++)
                    for (int x = width / 3; x < width * 2 / 3; x++)
                    {
                        Color32 color = colors[y * width + x];
                        if (color.r > 170 && color.g > 140 && color.b < 90) routePixels++;
                    }
                Assert.Greater(routePixels, 20, "The yellow preview route is not visible on the board.");
            }
            finally
            {
                for (int i = 0; i < cameras.Length; i++) { cameras[i].targetTexture = targets[i]; cameras[i].aspect = aspects[i]; }
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(texture);
                if (pixels != null) Object.DestroyImmediate(pixels);
            }
        }
    }

    // Re-register after domain reload so runtime evidence survives MCP callback loss.
    [UnityEditor.InitializeOnLoad]
    internal static class Vc5C3RuntimeResultWriter
    {
        static readonly TestRunnerApi api;
        static Vc5C3RuntimeResultWriter()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultCallbacks());
        }

        sealed class ResultCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void RunFinished(ITestResultAdaptor result) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.FullName != "TcgEngine.Testing.Editor.Vc5C3RuntimeTests.MenuStartsC3Match_AndPreviewRendersAtTwoResolutions")
                    return;
                Directory.CreateDirectory("TestResults");
                File.WriteAllText("TestResults/demo-art-runtime.xml", result.ToXml().OuterXml);
            }
        }
    }
}
#endif
