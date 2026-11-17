using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VC5PvE.SharedAP.Editor
{
    public static class SharedSceneBuilder
    {
        public const string SourceScenePath = "Assets/VC5PvE/Scenes/ForestRuins.unity";
        public const string ScenePath = "Assets/VC5PvE/SharedAP/Scenes/SharedAP_Qinglan.unity";

        [MenuItem("VC5PvE/Shared AP/Open Qinglan (independent version)")]
        public static void Open()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildFromForestRuins();
                return;
            }

            EnsureSafeToOpen();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("VC5PvE/Shared AP/Rebuild Qinglan from ForestRuins")]
        public static void BuildFromForestRuins()
        {
            EnsureSafeToOpen();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
                throw new InvalidOperationException("Source scene was not found: " + SourceScenePath);

            EnsureSceneFolder();
            // Save a scene copy to keep the existing shared-scene GUID and all source render settings.
            // The original scene is opened read-only and never modified or saved over.
            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, ScenePath, true))
                throw new InvalidOperationException("Could not copy ForestRuins to: " + ScenePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var flow = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PrototypeFlow>(true))
                .FirstOrDefault();
            if (flow == null)
                throw new InvalidOperationException("The copied scene does not contain a PrototypeFlow resource source.");

            Camera camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"));
            if (camera == null)
                throw new InvalidOperationException("The copied scene does not contain its tagged main camera.");

            GameObject controllerObject = flow.gameObject;
            controllerObject.name = "Shared AP Controller";
            var controller = controllerObject.AddComponent<SharedBattleController>();
            controller.ViewCamera = camera;
            controller.UiFont = flow.UiFont;
            controller.UnitSprites = flow.UnitSprites;
            controller.ScenerySprites = flow.ScenerySprites;
            controller.SoundClips = flow.SoundClips;
            controller.TreePrefabs = flow.TreePrefabs;
            controller.RockPrefabs = flow.RockPrefabs;
            controller.ShrubPrefabs = flow.ShrubPrefabs;
            controller.RuinPrefabs = flow.RuinPrefabs;
            controller.GroundAtlas = flow.GroundAtlas;
            UnityEngine.Object.DestroyImmediate(flow);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save the rebuilt shared AP scene: " + ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureSafeToOpen()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play mode before opening or rebuilding the shared AP scene.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    throw new InvalidOperationException("An open scene is unsaved. Save it before opening or rebuilding Qinglan.");
            }
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/VC5PvE/SharedAP"))
                AssetDatabase.CreateFolder("Assets/VC5PvE", "SharedAP");
            if (!AssetDatabase.IsValidFolder("Assets/VC5PvE/SharedAP/Scenes"))
                AssetDatabase.CreateFolder("Assets/VC5PvE/SharedAP", "Scenes");
        }
    }
}
