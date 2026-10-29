using System;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using BalloonBalance.XR;

namespace BalloonBalance.Editor
{
    [InitializeOnLoad]
    internal static class StationaryXrOriginSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Stationary XR Origin.prefab";
        private const string LobbyPath = "Assets/Scenes/CalibrationLobby.unity";
        private const string GameplayPath = "Assets/Scenes/Gameplay.unity";
        private const string SessionKey = "BalloonBalance.StationaryXrOrigin.Attempted";

        static StationaryXrOriginSetup()
        {
            EditorApplication.delayCall += EnsureOnce;
        }

        [MenuItem("Balloon Balance/Setup/Ensure Stationary XR Origins")]
        private static void EnsureFromMenu()
        {
            Ensure();
        }

        public static void EnsureCommandLine()
        {
            Ensure();
        }

        private static void EnsureOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += EnsureOnce;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            Ensure();
        }

        private static void Ensure()
        {
            if (!File.Exists(LobbyPath) || !File.Exists(GameplayPath))
            {
                throw new FileNotFoundException("Create the four project scenes before adding XR Origins.");
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = CreatePrefab();
            }

            prefab = EnsureEditorCameraComponents(prefab);

            EnsureScene(LobbyPath, prefab);
            EnsureScene(GameplayPath, prefab);
            ValidateScene(LobbyPath);
            ValidateScene(GameplayPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Balloon Balance] Stationary XR Origins are ready in lobby and gameplay scenes.");
        }

        private static GameObject CreatePrefab()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = new GameObject("Stationary XR Origin");
                SceneManager.MoveGameObjectToScene(root, previewScene);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                GameObject cameraOffset = new GameObject("Camera Offset");
                cameraOffset.transform.SetParent(root.transform, false);

                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(cameraOffset.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.055f, 0.095f);
                cameraObject.AddComponent<AudioListener>();

                TrackedPoseDriver poseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
                poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
                poseDriver.positionInput = new InputActionProperty(
                    new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
                poseDriver.rotationInput = new InputActionProperty(
                    new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
                poseDriver.trackingStateInput = new InputActionProperty(
                    new InputAction("Head Tracking State", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer"));

                XROrigin xrOrigin = root.AddComponent<XROrigin>();
                xrOrigin.Origin = root;
                xrOrigin.Camera = camera;
                xrOrigin.CameraYOffset = 0f;
                xrOrigin.CameraFloorOffsetObject = cameraOffset;
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException($"Failed to save {PrefabPath}.");
                }

                return saved;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static GameObject EnsureEditorCameraComponents(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Camera camera = contents.GetComponentInChildren<Camera>(true)
                    ?? throw new InvalidOperationException("Stationary XR Origin prefab has no camera.");
                if (camera.GetComponent<EditorHeadHeightFallback>() == null)
                {
                    camera.gameObject.AddComponent<EditorHeadHeightFallback>();
                }
                if (camera.GetComponent<EditorKeyboardCameraIsolation>() == null)
                {
                    camera.gameObject.AddComponent<EditorKeyboardCameraIsolation>();
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void EnsureScene(string path, GameObject prefab)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try
            {
                XROrigin[] origins = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<XROrigin>(true))
                    .ToArray();
                if (origins.Length == 0)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    EditorSceneManager.SaveScene(scene);
                }
                else if (origins.Length != 1)
                {
                    throw new InvalidOperationException($"Expected one XR Origin in {path}; found {origins.Length}.");
                }
            }
            finally
            {
                if (!wasLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateScene(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try
            {
                XROrigin[] origins = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<XROrigin>(true))
                    .ToArray();
                if (origins.Length != 1)
                {
                    throw new InvalidOperationException($"Expected one XR Origin in {path}; found {origins.Length}.");
                }

                XROrigin origin = origins[0];
                if (origin.transform.position != Vector3.zero || origin.transform.rotation != Quaternion.identity
                    || origin.Origin != origin.gameObject
                    || origin.RequestedTrackingOriginMode != XROrigin.TrackingOriginMode.Floor
                    || origin.CameraFloorOffsetObject == null
                    || origin.Camera == null || !origin.Camera.transform.IsChildOf(origin.transform)
                    || origin.Camera.GetComponent<TrackedPoseDriver>() == null
                    || origin.Camera.GetComponent<EditorHeadHeightFallback>() == null
                    || origin.Camera.GetComponent<EditorKeyboardCameraIsolation>() == null
                    || origin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(true).Length != 0)
                {
                    throw new InvalidOperationException($"Stationary XR Origin validation failed for {path}.");
                }
            }
            finally
            {
                if (!wasLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
