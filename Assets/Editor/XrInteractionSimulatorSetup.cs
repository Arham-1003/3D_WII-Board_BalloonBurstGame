using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace BalloonBalance.Editor
{
    internal static class XrInteractionSimulatorSetup
    {
        private const string PackageName = "com.unity.xr.interaction.toolkit";
        private const string SampleName = "XR Interaction Simulator";
        private const string SettingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";

        [MenuItem("Balloon Balance/Setup/Configure XR Interaction Simulator")]
        private static void ConfigureFromMenu()
        {
            ConfigureCommandLine();
        }

        public static void ConfigureCommandLine()
        {
            Sample sample = Sample.FindByPackage(PackageName, string.Empty)
                .FirstOrDefault(candidate => candidate.displayName == SampleName);
            if (string.IsNullOrEmpty(sample.displayName))
            {
                throw new InvalidOperationException($"Could not find the {SampleName} sample in {PackageName}.");
            }

            if (!sample.isImported && !sample.Import(Sample.ImportOptions.OverridePreviousImports))
            {
                throw new InvalidOperationException($"Failed to import the {SampleName} sample.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            GameObject simulatorPrefab = AssetDatabase.FindAssets("\"XR Interaction Simulator\" t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .FirstOrDefault(prefab => prefab != null && prefab.TryGetComponent<XRInteractionSimulator>(out _));
            if (simulatorPrefab == null)
            {
                throw new InvalidOperationException("The imported XR Interaction Simulator prefab was not found.");
            }

            ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsPath);
            if (settings == null)
            {
                throw new InvalidOperationException($"XR simulator settings were not found at {SettingsPath}.");
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            SetBoolean(serializedSettings, "m_AutomaticallyInstantiateSimulatorPrefab", true);
            SetBoolean(serializedSettings, "m_AutomaticallyInstantiateInEditorOnly", true);
            SetBoolean(serializedSettings, "m_UseClassic", false);
            SerializedProperty prefabProperty = RequireProperty(serializedSettings, "m_SimulatorPrefab");
            prefabProperty.objectReferenceValue = simulatorPrefab;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Validate(settings, simulatorPrefab);
            Debug.Log($"[Balloon Balance] XR Interaction Simulator configured from {AssetDatabase.GetAssetPath(simulatorPrefab)} for Editor-only automatic instantiation.");
        }

        private static void SetBoolean(SerializedObject serializedObject, string propertyName, bool value)
        {
            RequireProperty(serializedObject, propertyName).boolValue = value;
        }

        private static SerializedProperty RequireProperty(SerializedObject serializedObject, string propertyName)
        {
            return serializedObject.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"XR simulator setting {propertyName} was not found.");
        }

        private static void Validate(ScriptableObject settings, GameObject simulatorPrefab)
        {
            SerializedObject serializedSettings = new SerializedObject(settings);
            if (!RequireProperty(serializedSettings, "m_AutomaticallyInstantiateSimulatorPrefab").boolValue
                || !RequireProperty(serializedSettings, "m_AutomaticallyInstantiateInEditorOnly").boolValue
                || RequireProperty(serializedSettings, "m_UseClassic").boolValue
                || RequireProperty(serializedSettings, "m_SimulatorPrefab").objectReferenceValue != simulatorPrefab
                || !simulatorPrefab.TryGetComponent<XRInteractionSimulator>(out _))
            {
                throw new InvalidOperationException("XR Interaction Simulator configuration validation failed.");
            }
        }
    }
}
