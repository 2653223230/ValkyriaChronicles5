#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using TcgEngine.UI;
using UnityEditor;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoArt")]
    public class Vc5DemoArtRenderTests : Vc5LogicTestBase
    {
        [Test]
        public void CardDetails_RenderAllFourteenIllustrations()
        {
            Capture("c3-art-cards", new[] { "c3_tactical_move", "c3_forced_march", "c3_temp_calibration", "c3_scope_upgrade",
                "c3_fire_coverage", "c3_heavy_break", "c3_weakpoint_snipe", "c3_mobile_shot" });
            Capture("demo-art-heroes", new[] { "c3_sniper", "c3_fire_guard", "c3_mobile_ranger", "cavalry", "assassin", "scout" });
        }

        static void Capture(string name, string[] ids)
        {
            GameObject root = new GameObject("Art render test");
            RenderTexture target = RenderTexture.GetTemporary(1920, 1080, 24);
            RenderTexture previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                GameObject cameraObject = new GameObject("Art camera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.10f, 0.12f, 0.13f);
                camera.targetTexture = target;
                camera.aspect = 1920f / 1080f;
                GameObject canvasObject = new GameObject("Art canvas", typeof(Canvas));
                canvasObject.transform.SetParent(root.transform);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                int columns = ids.Length == 8 ? 4 : 3;
                for (int i = 0; i < ids.Length; i++)
                {
                    GameObject item = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TcgEngine/Prefabs/UI/CardUI.prefab"), canvas.transform);
                    RectTransform rect = item.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2((i % columns - (columns - 1) / 2f) * 450f, i / columns == 0 ? 260f : -260f);
                    rect.localScale = Vector3.one * 0.72f;
                    CardUI ui = item.GetComponent<CardUI>();
                    ui.SetCard(CardData.Get("vc5_demo_" + ids[i]), VariantData.GetDefault());
                    Assert.NotNull(ui.card_image.sprite);
                }
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                pixels.Apply();
                Assert.Greater(pixels.GetPixels32().Count(c => c.r > 150 || c.g > 150 || c.b > 150), 100000);
                Directory.CreateDirectory("TestResults");
                File.WriteAllBytes("TestResults/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(root);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (pixels != null) Object.DestroyImmediate(pixels);
            }
        }
    }
}
#endif
