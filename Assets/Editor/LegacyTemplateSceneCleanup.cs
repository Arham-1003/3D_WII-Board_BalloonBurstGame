using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BalloonBalance.Editor
{
    [InitializeOnLoad]
    internal static class LegacyTemplateSceneCleanup
    {
        private const string GameplayPath = "Assets/Scenes/Gameplay.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string BasicScenePath = "Assets/Scenes/BasicScene.unity";
        private const string SessionKey = "BalloonBalance.LegacyTemplateScenesCleaned";

        static LegacyTemplateSceneCleanup()
        {
            EditorApplication.delayCall += CleanOnce;
        }

        [MenuItem("Balloon Balance/Setup/Remove Legacy Template Scenes and Open Gameplay")]
        private static void CleanFromMenu()
        {
            CleanCommandLine();
        }

        public static void CleanCommandLine()
        {
            if (!File.Exists(GameplayPath))
            {
                throw new FileNotFoundException("The Balloon Balance Gameplay scene does not exist.", GameplayPath);
            }

            EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            DeleteIfPresent(SampleScenePath);
            DeleteIfPresent(BasicScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (File.Exists(SampleScenePath) || File.Exists(BasicScenePath)
                || EditorSceneManager.GetActiveScene().path != GameplayPath)
            {
                throw new InvalidOperationException("Legacy template scene cleanup validation failed.");
            }

            Debug.Log("[Balloon Balance] Removed legacy template scenes and opened Gameplay.");
        }

        private static void CleanOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += CleanOnce;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            CleanCommandLine();
        }

        private static void DeleteIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null && !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException($"Unity could not delete {path}.");
            }
        }
    }
}
