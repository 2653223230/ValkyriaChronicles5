#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TcgEngine.Editor;
using UnityEditor;
using UnityEngine;

namespace TcgEngine.Testing.Editor
{
    [Category("VC5DemoGate")]
    public class Vc5DemoBuildPipelineTests
    {
        [Test]
        public void DemoBranding_ConfiguresBothPlatformsAndAndroidTabletBaseline()
        {
            MethodInfo applyBranding = typeof(Vc5DemoBuildPipeline).GetMethod("ApplyDemoBranding",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(applyBranding, "The shared Windows/Android branding entry point is missing.");

            applyBranding.Invoke(null, null);

            Assert.AreEqual("ValkyriaChronicles5", UnityEditor.PlayerSettings.productName);
            Assert.AreEqual(AndroidSdkVersions.AndroidApiLevel24, UnityEditor.PlayerSettings.Android.minSdkVersion);
            Assert.AreEqual(UIOrientation.AutoRotation, UnityEditor.PlayerSettings.defaultInterfaceOrientation);
            Assert.IsFalse(UnityEditor.PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(UnityEditor.PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsTrue(UnityEditor.PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(UnityEditor.PlayerSettings.allowedAutorotateToLandscapeRight);
            Assert.IsFalse(UnityEditor.PlayerSettings.Android.resizableWindow,
                "The current full-screen UI has not been validated for arbitrary multi-window resizing.");

            AssertIcon(BuildTargetGroup.Android);
            AssertIcon(BuildTargetGroup.Standalone);
        }

        [Test]
        public void DemoBuildScenes_ContainOnlyMenuThenGame()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "Assets/TcgEngine/Scenes/Menu/Menu.unity",
                "Assets/TcgEngine/Scenes/Game/Game.unity"
            }, scenes);
        }

        private static void AssertIcon(BuildTargetGroup group)
        {
            Texture2D[] icons = UnityEditor.PlayerSettings.GetIconsForTargetGroup(group);
            Assert.IsTrue(icons.Any(icon => icon != null &&
                AssetDatabase.GetAssetPath(icon) == "Assets/TcgEngine/Images/VC5/AppIcon.png"),
                group + " must use the approved VC5 icon.");
        }
    }
}
#endif
