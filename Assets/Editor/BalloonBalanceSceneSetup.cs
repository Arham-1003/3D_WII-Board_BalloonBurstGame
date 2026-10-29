using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonBalance.Editor
{
    [InitializeOnLoad]
    internal static class BalloonBalanceSceneSetup
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string SessionKey = "BalloonBalance.SceneSetup.Attempted";

        private static readonly string[] SceneNames =
        {
            "Bootstrap",
            "CalibrationLobby",
            "Gameplay",
            "Results",
        };

        static BalloonBalanceSceneSetup()
        {
            EditorApplication.delayCall += EnsureProjectScenesOnce;
        }

        [MenuItem("Balloon Balance/Setup/Ensure Project Scenes")]
        private static void EnsureProjectScenesFromMenu()
        {
            EnsureProjectScenes();
        }

        public static void EnsureProjectScenesCommandLine()
        {
            EnsureProjectScenes();
        }

        private static void EnsureProjectScenesOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (IsSetupComplete())
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureProjectScenesOnce;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EnsureProjectScenes();
        }

        private static void EnsureProjectScenes()
        {
            Directory.CreateDirectory(SceneFolder);

            string originalScenePath = SceneManager.GetActiveScene().path;
            foreach (string sceneName in SceneNames)
            {
                string scenePath = GetScenePath(sceneName);
                if (File.Exists(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject(sceneName);
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log($"[Balloon Balance] Created scene: {scenePath}");
            }

            EditorBuildSettings.scenes = SceneNames
                .Select(sceneName => new EditorBuildSettingsScene(GetScenePath(sceneName), true))
                .ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string bootstrapPath = GetScenePath(SceneNames[0]);
            if (File.Exists(bootstrapPath))
            {
                EditorSceneManager.OpenScene(bootstrapPath, OpenSceneMode.Single);
            }
            else if (!string.IsNullOrWhiteSpace(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            Debug.Log("[Balloon Balance] Four-scene project structure and build order are ready.");
        }

        private static string GetScenePath(string sceneName)
        {
            return $"{SceneFolder}/{sceneName}.unity";
        }

        private static bool IsSetupComplete()
        {
            if (SceneNames.Any(sceneName => !File.Exists(GetScenePath(sceneName))))
            {
                return false;
            }

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            return buildScenes.Length == SceneNames.Length
                && SceneNames.Select(GetScenePath)
                    .SequenceEqual(buildScenes.Select(scene => scene.path))
                && buildScenes.All(scene => scene.enabled);
        }
    }
}
