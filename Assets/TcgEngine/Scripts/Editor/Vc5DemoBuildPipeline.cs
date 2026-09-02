#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TcgEngine.Editor
{
    public static class Vc5DemoBuildPipeline
    {
        private const string AndroidOutputRelativePath = "Builds/Direct/Android/ValkyriaChronicles5.apk";
        private const string WindowsOutputRelativePath = "Builds/Direct/Windows/ValkyriaChronicles5/ValkyriaChronicles5.exe";
        private const string ProductName = "ValkyriaChronicles5";
        private const string AppIconPath = "Assets/TcgEngine/Images/VC5/AppIcon.png";

        public static void BuildWindows()
        {
            BuildWindowsInternal(false);
        }

        public static void BuildWindowsClean()
        {
            BuildWindowsInternal(true);
        }

        public static void BuildAndroid()
        {
            BuildAndroidInternal(false);
        }

        public static void BuildAndroidClean()
        {
            BuildAndroidInternal(true);
        }

        public static void ApplyAndroidBranding()
        {
            ApplyDemoBranding();
        }

        public static void ApplyDemoBranding()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
                throw new InvalidOperationException($"Demo app icon is missing: {AppIconPath}");

            UnityEditor.PlayerSettings.productName = ProductName;
            UnityEditor.PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon });
            int standaloneIconCount = UnityEditor.PlayerSettings
                .GetIconSizesForTargetGroup(BuildTargetGroup.Standalone).Length;
            UnityEditor.PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,
                Enumerable.Repeat(icon, standaloneIconCount).ToArray());
            UnityEditor.PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            UnityEditor.PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            UnityEditor.PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            UnityEditor.PlayerSettings.allowedAutorotateToPortrait = false;
            UnityEditor.PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            UnityEditor.PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            UnityEditor.PlayerSettings.allowedAutorotateToLandscapeRight = true;
            UnityEditor.PlayerSettings.Android.resizableWindow = false;
            AssetDatabase.SaveAssets();
            Debug.Log($"[VC5 Build] Demo branding applied: name={ProductName}, icon={AppIconPath}, minSdk=24, architectures=ARMv7|ARM64");
        }

        private static void BuildWindowsInternal(bool cleanBuild)
        {
            string[] scenes = GetDemoScenesOrExit("Windows");
            if (scenes == null)
                return;

            string projectRoot = GetProjectRootOrExit();
            if (projectRoot == null)
                return;

            ConfigureBuildStorage(projectRoot, "Windows", false);
            ApplyDemoBranding();

            string outputPath = Path.Combine(projectRoot, WindowsOutputRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.DetailedBuildReport |
                    (cleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None)
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"Windows build failed: {summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}");
                return;
            }

            Debug.Log($"[VC5 Build] Windows build succeeded: {outputPath}, bytes={summary.totalSize}");
            EditorApplication.Exit(0);
        }

        private static void BuildAndroidInternal(bool cleanBuild)
        {
            string[] scenes = GetDemoScenesOrExit("Android");
            if (scenes == null)
                return;

            string projectRoot = GetProjectRootOrExit();
            if (projectRoot == null)
                return;

            ConfigureBuildStorage(projectRoot, "Android", true);
            ApplyDemoBranding();

            string outputPath = Path.Combine(projectRoot, AndroidOutputRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            EditorUserBuildSettings.buildAppBundle = false;
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.DetailedBuildReport |
                    (cleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None)
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"Android build failed: {summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}");
                return;
            }

            Debug.Log($"[VC5 Build] Android APK succeeded: {outputPath}, bytes={summary.totalSize}");
            EditorApplication.Exit(0);
        }

        private static string[] GetDemoScenesOrExit(string platform)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 2 &&
                scenes[0] == "Assets/TcgEngine/Scenes/Menu/Menu.unity" &&
                scenes[1] == "Assets/TcgEngine/Scenes/Game/Game.unity")
                return scenes;

            Fail($"{platform} Demo build requires exactly Menu.unity then Game.unity.");
            return null;
        }

        private static string GetProjectRootOrExit()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
                return projectRoot;

            Fail("Unable to resolve the Unity project root.");
            return null;
        }

        private static void ConfigureBuildStorage(string projectRoot, string platform, bool configureGradle)
        {
            string tempPath = Path.Combine(projectRoot, "Builds", "Temp", platform);
            Directory.CreateDirectory(tempPath);
            Environment.SetEnvironmentVariable("TEMP", tempPath);
            Environment.SetEnvironmentVariable("TMP", tempPath);
            Debug.Log($"[VC5 Build] TEMP/TMP: {tempPath}");

            if (configureGradle)
            {
                string gradleHomePath = Path.Combine(projectRoot, "Builds", "GradleHome");
                Directory.CreateDirectory(gradleHomePath);
                Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleHomePath);
                Debug.Log($"[VC5 Build] GRADLE_USER_HOME: {gradleHomePath}");
            }
        }

        private static void Fail(string message)
        {
            Debug.LogError("[VC5 Build] " + message);
            EditorApplication.Exit(1);
        }
    }
}
#endif
