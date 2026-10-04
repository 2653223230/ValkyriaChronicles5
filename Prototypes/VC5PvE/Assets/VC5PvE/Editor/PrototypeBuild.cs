using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VC5PvE.Editor
{
    /// <summary>Standalone Windows x64 build entry point for the VC5PvE project.</summary>
    public static class PrototypeBuild
    {
        private static readonly string[] BuildScenes =
        {
            "Assets/VC5PvE/Scenes/Title.unity",
            "Assets/VC5PvE/Scenes/ForestRuins.unity"
        };

        [MenuItem("VC5PvE/Build Windows x64")]
        public static void BuildWindows()
        {
            PrototypeSceneBuilder.BuildScenes();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Unity could not switch this project to Standalone Windows x64.");

            ConfigureWindowsPlayer();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(projectRoot, "Builds", "Windows");
            string outputPath = Path.Combine(outputDirectory, "森钟战术.exe");
            Directory.CreateDirectory(outputDirectory);

            for (int i = 0; i < BuildScenes.Length; i++)
                if (!File.Exists(Path.Combine(projectRoot, BuildScenes[i].Replace('/', Path.DirectorySeparatorChar))))
                    throw new FileNotFoundException("Expected prototype build scene is missing.", BuildScenes[i]);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            WriteReport(projectRoot, report, outputPath);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("VC5PvE Windows build failed: " + report.summary.result + "; details: " +
                    Path.Combine(projectRoot, "Logs", "VC5PvE-Windows-build-report.txt"));

            Debug.Log("VC5PvE Windows x64 build succeeded: " + outputPath + " (" + report.summary.totalSize + " bytes)");
        }

        private static void ConfigureWindowsPlayer()
        {
            PlayerSettings.companyName = "VC5PvE Prototype";
            PlayerSettings.productName = "森钟战术";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        }

        private static void WriteReport(string projectRoot, BuildReport report, string outputPath)
        {
            string logDirectory = Path.Combine(projectRoot, "Logs");
            Directory.CreateDirectory(logDirectory);
            var text = new StringBuilder();
            text.AppendLine("VC5PvE Windows x64 build");
            text.AppendLine("Result: " + report.summary.result);
            text.AppendLine("Target: " + report.summary.platform);
            text.AppendLine("Output: " + outputPath);
            text.AppendLine("Size bytes: " + report.summary.totalSize);
            text.AppendLine("Duration: " + report.summary.totalTime);
            text.AppendLine("Errors: " + report.summary.totalErrors);
            text.AppendLine("Warnings: " + report.summary.totalWarnings);
            foreach (BuildStep step in report.steps)
            {
                text.AppendLine();
                text.AppendLine("Step: " + step.name + " — " + step.duration);
                foreach (BuildStepMessage message in step.messages)
                    text.AppendLine("[" + message.type + "] " + message.content);
            }
            File.WriteAllText(Path.Combine(logDirectory, "VC5PvE-Windows-build-report.txt"), text.ToString(), new UTF8Encoding(false));
        }
    }
}
