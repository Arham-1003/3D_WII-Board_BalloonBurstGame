using System;
using System.IO;
using BalloonBalance.Gameplay;
using BalloonBalance.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonBalance.Editor
{
    internal static class PointerMilestoneSetup
    {
        private const string GameplayPath = "Assets/Scenes/Gameplay.unity";
        private const string PointerPrefabPath = "Assets/Prefabs/Pointer.prefab";
        private const string PointerMaterialPath = "Assets/Materials/Pointer Glow.mat";
        private const string StageMaterialPath = "Assets/Materials/Stage Dark.mat";
        private const string FrameMaterialPath = "Assets/Materials/Stage Frame.mat";

        [MenuItem("Balloon Balance/Setup/Create Visible Pointer Milestone")]
        private static void ConfigureFromMenu()
        {
            ConfigureCommandLine();
        }

        public static void ConfigureCommandLine()
        {
            if (!File.Exists(GameplayPath))
            {
                throw new FileNotFoundException("Gameplay scene is missing.", GameplayPath);
            }

            Directory.CreateDirectory("Assets/Materials");
            Material pointerMaterial = EnsureMaterial(PointerMaterialPath, new Color(0.05f, 0.95f, 1f), true);
            Material stageMaterial = EnsureMaterial(StageMaterialPath, new Color(0.025f, 0.045f, 0.08f), false);
            Material frameMaterial = EnsureMaterial(FrameMaterialPath, new Color(0.1f, 0.45f, 0.95f), true);
            GameObject pointerPrefab = EnsurePointerPrefab(pointerMaterial);

            Scene scene = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            RemoveExistingStage(scene);

            GameObject stage = new GameObject("Balloon Balance Gameplay Stage");
            GameObject floor = CreateCube("Floor", stage.transform, new Vector3(0f, -0.15f, 3.5f), new Vector3(7f, 0.3f, 8f), stageMaterial);
            floor.isStatic = true;

            GameObject interactionPlane = new GameObject("Interaction Plane Center");
            interactionPlane.transform.SetParent(stage.transform, false);
            interactionPlane.transform.position = new Vector3(0f, 1.65f, 3f);

            CreateCube("Frame Top", stage.transform, new Vector3(0f, 2.8f, 3.08f), new Vector3(3.8f, 0.08f, 0.08f), frameMaterial);
            CreateCube("Frame Bottom", stage.transform, new Vector3(0f, 0.5f, 3.08f), new Vector3(3.8f, 0.08f, 0.08f), frameMaterial);
            CreateCube("Frame Left", stage.transform, new Vector3(-1.9f, 1.65f, 3.08f), new Vector3(0.08f, 2.38f, 0.08f), frameMaterial);
            CreateCube("Frame Right", stage.transform, new Vector3(1.9f, 1.65f, 3.08f), new Vector3(0.08f, 2.38f, 0.08f), frameMaterial);

            TextMesh title = new GameObject("Title").AddComponent<TextMesh>();
            title.transform.SetParent(stage.transform, false);
            title.transform.position = new Vector3(0f, 3.05f, 3.12f);
            title.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            title.anchor = TextAnchor.MiddleCenter;
            title.alignment = TextAlignment.Center;
            title.text = "BALLOON BALANCE VR";
            title.fontSize = 64;
            title.characterSize = 0.05f;
            title.color = new Color(0.45f, 0.95f, 1f);

            TextMesh instructions = new GameObject("Keyboard Instructions").AddComponent<TextMesh>();
            instructions.transform.SetParent(stage.transform, false);
            instructions.transform.position = new Vector3(0f, 0.25f, 3.12f);
            instructions.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            instructions.anchor = TextAnchor.MiddleCenter;
            instructions.alignment = TextAlignment.Center;
            instructions.text = "WASD / ARROW KEYS - MOVE POINTER";
            instructions.fontSize = 48;
            instructions.characterSize = 0.035f;
            instructions.color = Color.white;

            GameObject input = new GameObject("Keyboard CoP Input");
            input.transform.SetParent(stage.transform, false);
            KeyboardCopInputSource keyboardSource = input.AddComponent<KeyboardCopInputSource>();
            CopInputManager inputManager = input.AddComponent<CopInputManager>();
            SerializedObject inputManagerSerialized = new SerializedObject(inputManager);
            inputManagerSerialized.FindProperty("inputSourceBehaviour").objectReferenceValue = keyboardSource;
            inputManagerSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject pointer = (GameObject)PrefabUtility.InstantiatePrefab(pointerPrefab, scene);
            pointer.name = "Pointer";
            pointer.transform.SetParent(stage.transform, true);
            PointerController pointerController = pointer.GetComponent<PointerController>();
            SerializedObject pointerSerialized = new SerializedObject(pointerController);
            pointerSerialized.FindProperty("inputManager").objectReferenceValue = inputManager;
            pointerSerialized.FindProperty("interactionPlaneCenter").objectReferenceValue = interactionPlane.transform;
            pointerSerialized.ApplyModifiedPropertiesWithoutUndo();
            pointer.transform.position = interactionPlane.transform.position;

            GameObject lightObject = new GameObject("Gameplay Directional Light");
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate(scene);
            Debug.Log("[Balloon Balance] Visible pointer milestone created in Gameplay. Use WASD or arrow keys in Play Mode.");
        }

        private static Material EnsureMaterial(string path, Color color, bool emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject EnsurePointerPrefab(Material material)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PointerPrefabPath);
            if (existing != null) return existing;

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointer.name = "Pointer";
                SceneManager.MoveGameObjectToScene(pointer, preview);
                pointer.transform.localScale = Vector3.one * 0.22f;
                pointer.GetComponent<MeshRenderer>().sharedMaterial = material;
                pointer.GetComponent<SphereCollider>().isTrigger = true;
                Rigidbody body = pointer.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                pointer.AddComponent<PointerController>();
                return PrefabUtility.SaveAsPrefabAsset(pointer, PointerPrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static void RemoveExistingStage(Scene scene)
        {
            GameObject existing = Array.Find(scene.GetRootGameObjects(), root => root.name == "Balloon Balance Gameplay Stage");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        }

        private static void Validate(Scene scene)
        {
            GameObject stage = Array.Find(scene.GetRootGameObjects(), root => root.name == "Balloon Balance Gameplay Stage");
            if (stage == null || stage.GetComponentInChildren<PointerController>(true) == null
                || stage.GetComponentInChildren<KeyboardCopInputSource>(true) == null
                || stage.GetComponentInChildren<CopInputManager>(true) == null)
            {
                throw new InvalidOperationException("Visible pointer milestone validation failed.");
            }
        }
    }
}
