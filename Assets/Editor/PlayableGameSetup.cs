using System;
using System.IO;
using BalloonBalance.Gameplay;
using BalloonBalance.Input;
using BalloonBalance.Logging;
using BalloonBalance.XR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonBalance.Editor
{
    internal static class PlayableGameSetup
    {
        private const string GameplayPath = "Assets/Scenes/Gameplay.unity";
        private const string BalloonPrefabPath = "Assets/Prefabs/Balloon.prefab";
        private const string BurstPrefabPath = "Assets/Prefabs/Burst.prefab";
        private const string PointerPrefabPath = "Assets/Prefabs/Pointer.prefab";
        private const string MountainMeshPath = "Assets/Generated/Faceted Mountain.asset";

        [MenuItem("Balloon Balance/Setup/Build Playable Game")]
        private static void BuildFromMenu() => BuildCommandLine();

        public static void BuildCommandLine()
        {
            if (!File.Exists(GameplayPath)) throw new FileNotFoundException("Gameplay scene is missing.");
            Directory.CreateDirectory("Assets/Materials");

            Material balloonMaterial = Material("Assets/Materials/Balloon.mat", new Color(1f, 0.25f, 0.35f), true);
            Material stringMaterial = Material("Assets/Materials/Balloon String.mat", new Color(0.95f, 0.95f, 1f), false);
            Material burstMaterial = Material("Assets/Materials/Burst.mat", new Color(1f, 0.9f, 0.2f), true);
            Material grass = Material("Assets/Materials/Grass.mat", new Color(0.22f, 0.62f, 0.3f), false);
            Material path = Material("Assets/Materials/Path.mat", new Color(0.92f, 0.72f, 0.42f), false);
            Material frame = Material("Assets/Materials/Frame.mat", new Color(0.05f, 0.65f, 1f), true);
            Material cloud = Material("Assets/Materials/Cloud.mat", new Color(0.95f, 0.98f, 1f), false);
            Material mountain = Material("Assets/Materials/Mountain.mat", new Color(0.31f, 0.53f, 0.72f), false);
            Material farMountain = Material("Assets/Materials/Far Mountain.mat", new Color(0.55f, 0.72f, 0.84f), false);
            Material foothill = Material("Assets/Materials/Foothill.mat", new Color(0.3f, 0.62f, 0.34f), false);
            Material snow = Material("Assets/Materials/Mountain Snow.mat", new Color(0.94f, 0.97f, 1f), false);
            Material trunk = Material("Assets/Materials/Trunk.mat", new Color(0.38f, 0.2f, 0.08f), false);
            Material leaves = Material("Assets/Materials/Leaves.mat", new Color(0.12f, 0.55f, 0.22f), false);
            Material flowers = Material("Assets/Materials/Flowers.mat", new Color(1f, 0.55f, 0.16f), true);
            Material sunDisc = Material("Assets/Materials/Sun Disc.mat", new Color(1f, 0.78f, 0.18f), true);
            Mesh mountainMesh = BuildMountainMesh();

            GameObject balloonPrefab = BuildBalloonPrefab(balloonMaterial, stringMaterial);
            GameObject burstPrefab = BuildBurstPrefab(burstMaterial);
            GameObject pointerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PointerPrefabPath);
            if (pointerPrefab == null) throw new InvalidOperationException("Pointer prefab is missing.");

            Scene scene = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            RemoveRoot(scene, "Balloon Balance Gameplay Stage");
            RemoveRoot(scene, "Balloon Balance Game");
            RemoveRoot(scene, "EventSystem");

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.35f, 0.72f, 0.95f);
                camera.farClipPlane = 120f;
            }

            GameObject gameRoot = new GameObject("Balloon Balance Game");
            BuildScenery(gameRoot.transform, grass, path, frame, cloud, mountain, farMountain, foothill,
                snow, trunk, leaves, flowers, sunDisc, mountainMesh);

            GameObject interactionCenter = new GameObject("Interaction Plane Center");
            interactionCenter.transform.SetParent(gameRoot.transform, false);
            interactionCenter.transform.position = new Vector3(0f, 1.65f, 3f);

            GameObject inputObject = new GameObject("Keyboard CoP Input");
            inputObject.transform.SetParent(gameRoot.transform, false);
            KeyboardCopInputSource keyboard = inputObject.AddComponent<KeyboardCopInputSource>();
            CopInputManager inputManager = inputObject.AddComponent<CopInputManager>();
            SetReference(inputManager, "inputSourceBehaviour", keyboard);

            GameObject pointer = (GameObject)PrefabUtility.InstantiatePrefab(pointerPrefab, scene);
            pointer.name = "Balance Pointer";
            pointer.transform.SetParent(gameRoot.transform, true);
            pointer.transform.position = interactionCenter.transform.position;
            PointerController pointerController = pointer.GetComponent<PointerController>();
            SetReference(pointerController, "inputManager", inputManager);
            SetReference(pointerController, "interactionPlaneCenter", interactionCenter.transform);

            Transform poolRoot = new GameObject("Runtime Pools").transform;
            poolRoot.SetParent(gameRoot.transform, false);

            Canvas canvas = CreateCanvas(gameRoot.transform);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text titleText = CreateText(canvas.transform, "Title", font, 40, TextAnchor.MiddleCenter, new Color(0.02f, 0.16f, 0.3f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 60f));
            Text statusText = CreateText(canvas.transform, "Status", font, 23, TextAnchor.MiddleCenter, new Color(0.02f, 0.16f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(680f, 90f));
            Text scoreText = CreateText(canvas.transform, "Score", font, 22, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -58f), new Vector2(420f, 44f));
            Text timerText = CreateText(canvas.transform, "Timer", font, 30, TextAnchor.MiddleRight, Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -58f), new Vector2(200f, 48f));
            Button startButton = CreateButton(canvas.transform, "Start Button", font, "START GAME", new Vector2(0f, -15f));
            Button restartButton = CreateButton(canvas.transform, "Restart Button", font, "PLAY AGAIN", new Vector2(0f, -15f));
            restartButton.gameObject.SetActive(false);
            GameObject settingsPrompt = CreatePanel(canvas.transform, "Input Settings Prompt",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 18f), new Vector2(360f, 52f),
                new Color(0.02f, 0.16f, 0.3f, 0.94f));
            Text promptText = CreateText(settingsPrompt.transform, "Prompt", font, 20, TextAnchor.MiddleCenter,
                Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(promptText.rectTransform, new Vector2(10f, 5f), new Vector2(-10f, -5f));
            promptText.text = "INPUT & VIEW SETTINGS   [ Y ]";

            GameObject settingsPanel = CreatePanel(canvas.transform, "Input & View Settings",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 520f),
                new Color(0.015f, 0.07f, 0.13f, 0.97f));
            Text settingsTitle = CreateText(settingsPanel.transform, "Settings Title", font, 40,
                TextAnchor.MiddleCenter, new Color(0.15f, 0.85f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(780f, 62f));
            settingsTitle.text = "INPUT & VIEW SETTINGS";
            Text settingsHelp = CreateText(settingsPanel.transform, "Settings Help", font, 23,
                TextAnchor.UpperLeft, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -142f), new Vector2(720f, 120f));
            settingsHelp.fontStyle = FontStyle.Normal;
            settingsHelp.text = "Pointer: WASD or arrow keys\nOpen / close this window: Y\nThe round is paused while this window is open.";
            Toggle headLookToggle = CreateToggle(settingsPanel.transform, "Simulator Head Look Toggle", font,
                "ENABLE DESKTOP HEAD LOOK (HOLD RIGHT MOUSE)", new Vector2(-70f, -290f));
            headLookToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 52f);
            Text toggleExplanation = CreateText(settingsPanel.transform, "Head Look Explanation", font, 18,
                TextAnchor.UpperLeft, new Color(0.75f, 0.86f, 0.94f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(720f, 70f));
            toggleExplanation.fontStyle = FontStyle.Normal;
            toggleExplanation.text = "OFF keeps the desktop camera centred. ON allows rotation only; it never moves the player or pointer.";
            Button closeSettingsButton = CreateButton(settingsPanel.transform, "Close Settings Button", font,
                "CLOSE  [ Y ]", new Vector2(0f, -215f));
            EditorKeyboardCameraIsolation cameraIsolation = camera != null
                ? camera.GetComponent<EditorKeyboardCameraIsolation>()
                : null;
            if (cameraIsolation == null) throw new InvalidOperationException("Gameplay camera isolation is missing.");
            DesktopSimulatorHeadLookToggle toggleController = canvas.gameObject.AddComponent<DesktopSimulatorHeadLookToggle>();
            SetReference(toggleController, "toggle", headLookToggle);
            SetReference(toggleController, "cameraIsolation", cameraIsolation);
            DesktopInputSettingsMenu settingsMenu = canvas.gameObject.AddComponent<DesktopInputSettingsMenu>();
            SetReference(settingsMenu, "settingsPanel", settingsPanel);
            SetReference(settingsMenu, "closedPrompt", settingsPrompt);
            SetReference(settingsMenu, "closeButton", closeSettingsButton);
            settingsPanel.SetActive(false);

            GameObject managerObject = new GameObject("Game Manager");
            managerObject.transform.SetParent(gameRoot.transform, false);
            TrialEventLogger eventLogger = managerObject.AddComponent<TrialEventLogger>();
            BalloonGameManager manager = managerObject.AddComponent<BalloonGameManager>();
            SerializedObject managerSerialized = new SerializedObject(manager);
            Set(managerSerialized, "balloonPrefab", balloonPrefab.GetComponent<BalloonTarget>());
            Set(managerSerialized, "burstPrefab", burstPrefab.GetComponent<BurstEffect>());
            Set(managerSerialized, "balloonPoolRoot", poolRoot);
            Set(managerSerialized, "titleText", titleText);
            Set(managerSerialized, "statusText", statusText);
            Set(managerSerialized, "scoreText", scoreText);
            Set(managerSerialized, "timerText", timerText);
            Set(managerSerialized, "startButton", startButton);
            Set(managerSerialized, "restartButton", restartButton);
            Set(managerSerialized, "eventLogger", eventLogger);
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate(scene);
            Debug.Log("[Balloon Balance] Playable 90-second balloon game created. Press Start Game or Space.");
        }

        private static GameObject BuildBalloonPrefab(Material balloonMaterial, Material stringMaterial)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject balloon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                balloon.name = "Balloon";
                SceneManager.MoveGameObjectToScene(balloon, preview);
                balloon.transform.localScale = new Vector3(0.48f, 0.62f, 0.48f);
                balloon.GetComponent<MeshRenderer>().sharedMaterial = balloonMaterial;
                balloon.GetComponent<SphereCollider>().isTrigger = true;
                Rigidbody body = balloon.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                balloon.AddComponent<BalloonTarget>();

                GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                knot.name = "Knot";
                knot.transform.SetParent(balloon.transform, false);
                knot.transform.localPosition = new Vector3(0f, -0.58f, 0f);
                knot.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
                knot.GetComponent<MeshRenderer>().sharedMaterial = balloonMaterial;
                UnityEngine.Object.DestroyImmediate(knot.GetComponent<Collider>());

                GameObject stringObject = new GameObject("String");
                stringObject.transform.SetParent(balloon.transform, false);
                LineRenderer line = stringObject.AddComponent<LineRenderer>();
                line.sharedMaterial = stringMaterial;
                line.useWorldSpace = false;
                line.startWidth = 0.025f;
                line.endWidth = 0.012f;
                line.positionCount = 3;
                line.SetPositions(new[] { new Vector3(0f, -0.6f, 0f), new Vector3(0.06f, -0.95f, 0f), new Vector3(-0.03f, -1.3f, 0f) });
                return PrefabUtility.SaveAsPrefabAsset(balloon, BalloonPrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static GameObject BuildBurstPrefab(Material material)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                burst.name = "Burst";
                SceneManager.MoveGameObjectToScene(burst, preview);
                burst.GetComponent<MeshRenderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(burst.GetComponent<Collider>());
                burst.AddComponent<BurstEffect>();
                return PrefabUtility.SaveAsPrefabAsset(burst, BurstPrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static void BuildScenery(Transform parent, Material grass, Material path, Material frame,
            Material cloud, Material mountain, Material farMountain, Material foothill, Material snow,
            Material trunk, Material leaves, Material flowers, Material sunDisc, Mesh mountainMesh)
        {
            Cube("Meadow", parent, new Vector3(0f, -0.7f, 22f), new Vector3(40f, 0.6f, 70f), grass);
            Cube("Path", parent, new Vector3(0f, -0.37f, 22f), new Vector3(3.4f, 0.06f, 68f), path);
            Cube("Frame Top", parent, new Vector3(0f, 2.85f, 3.08f), new Vector3(3.8f, 0.07f, 0.07f), frame);
            Cube("Frame Bottom", parent, new Vector3(0f, 0.45f, 3.08f), new Vector3(3.8f, 0.07f, 0.07f), frame);
            Cube("Frame Left", parent, new Vector3(-1.9f, 1.65f, 3.08f), new Vector3(0.07f, 2.45f, 0.07f), frame);
            Cube("Frame Right", parent, new Vector3(1.9f, 1.65f, 3.08f), new Vector3(0.07f, 2.45f, 0.07f), frame);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int index = 0; index < 5; index++)
                {
                    float z = 3.5f + index * 2.6f;
                    Cylinder($"Tree Trunk {side} {index}", parent, new Vector3(side * (3.5f + index * 0.25f), 0.75f, z), new Vector3(0.22f, 0.75f, 0.22f), trunk);
                    Sphere($"Tree Crown {side} {index}", parent, new Vector3(side * (3.5f + index * 0.25f), 1.85f, z), new Vector3(1.1f, 1.35f, 1.1f), leaves);
                }
            }

            for (int index = 0; index < 7; index++)
            {
                float x = -15f + index * 5f;
                float height = 5.2f + (index % 3) * 1.1f;
                Vector3 position = new Vector3(x, -0.38f, 31f + Mathf.Abs(x) * 0.08f);
                Mountain($"Distant Mountain {index}", parent, position,
                    new Vector3(4.8f, height, 3.7f), farMountain, mountainMesh);
                Mountain($"Distant Snow Cap {index}", parent,
                    position + Vector3.up * (height * 0.68f), new Vector3(1.55f, height * 0.32f, 1.25f),
                    snow, mountainMesh);
            }

            for (int index = 0; index < 6; index++)
            {
                int side = index < 3 ? -1 : 1;
                int row = index % 3;
                float x = side * (6.5f + row * 3.6f);
                float height = 3.2f + row * 0.45f;
                Vector3 position = new Vector3(x, -0.38f, 22f + row * 1.4f);
                Mountain($"Green Foothill {index}", parent, position,
                    new Vector3(4.2f, height, 3.2f), foothill, mountainMesh);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 5.2f;
                float height = 5.4f;
                Vector3 position = new Vector3(x, -0.38f, 25f);
                Mountain($"Blue Mountain {side}", parent, position,
                    new Vector3(4.4f, height, 3.4f), mountain, mountainMesh);
                Mountain($"Blue Snow Cap {side}", parent, position + Vector3.up * (height * 0.7f),
                    new Vector3(1.45f, height * 0.3f, 1.15f), snow, mountainMesh);
            }

            for (int index = 0; index < 5; index++)
            {
                float x = -6f + index * 3f;
                float y = 5.4f + index % 2 * 0.7f;
                float z = 13f + index * 2f;
                Sphere($"Cloud {index} A", parent, new Vector3(x, y, z), new Vector3(1.7f, 0.48f, 0.65f), cloud);
                Sphere($"Cloud {index} B", parent, new Vector3(x + 0.75f, y + 0.12f, z), new Vector3(1.15f, 0.42f, 0.58f), cloud);
            }

            Sphere("Warm Sun", parent, new Vector3(7.5f, 7.2f, 28f), new Vector3(1.4f, 1.4f, 0.35f), sunDisc);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int index = 0; index < 12; index++)
                {
                    float z = 4.5f + index * 1.65f;
                    float x = side * (2.15f + (index % 3) * 0.35f);
                    Sphere($"Flower {side} {index}", parent, new Vector3(x, 0.04f, z),
                        new Vector3(0.11f, 0.16f, 0.11f), flowers);
                }
            }

            GameObject lightObject = new GameObject("Sun Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.94f, 0.8f);
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Game HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            return canvas;
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, Font font, string label, Vector2 position)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(260f, 76f);
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.04f, 0.42f, 0.8f, 0.96f);
            Button button = obj.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.12f, 0.65f, 1f);
            colors.pressedColor = new Color(0.02f, 0.25f, 0.6f);
            button.colors = colors;
            Text text = CreateText(obj.transform, "Label", font, 30, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform textRect = text.rectTransform;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.text = label;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string name, Font font, string label, Vector2 position)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(410f, 42f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(obj.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0f, 0.5f);
            backgroundRect.pivot = new Vector2(0f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(34f, 34f);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.02f, 0.16f, 0.3f, 0.9f);

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(background.transform, false);
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(7f, 7f);
            checkmarkRect.offsetMax = new Vector2(-7f, -7f);
            Image checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.color = new Color(0.05f, 0.9f, 1f);

            Text text = CreateText(obj.transform, "Label", font, 18, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(46f, 0f), new Vector2(-46f, 0f));
            text.fontStyle = FontStyle.Bold;
            text.text = label;

            Toggle toggle = obj.GetComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = false;
            return toggle;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = minimum;
            rect.offsetMax = maximum;
        }

        private static Material Material(string path, Color color, bool emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh BuildMountainMesh()
        {
            Directory.CreateDirectory("Assets/Generated");
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MountainMeshPath);
            if (mesh != null) return mesh;

            const int sides = 8;
            Vector3[] vertices = new Vector3[sides + 2];
            int[] triangles = new int[sides * 6];
            vertices[0] = Vector3.zero;
            vertices[sides + 1] = Vector3.up;
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                vertices[side + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                int next = (side + 1) % sides + 1;
                int offset = side * 6;
                triangles[offset] = 0;
                triangles[offset + 1] = next;
                triangles[offset + 2] = side + 1;
                triangles[offset + 3] = side + 1;
                triangles[offset + 4] = next;
                triangles[offset + 5] = sides + 1;
            }
            mesh = new Mesh { name = "Faceted Mountain" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MountainMeshPath);
            return mesh;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material) => Primitive(PrimitiveType.Cube, name, parent, position, scale, material);
        private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material) => Primitive(PrimitiveType.Sphere, name, parent, position, scale, material);
        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material) => Primitive(PrimitiveType.Cylinder, name, parent, position, scale, material);

        private static GameObject Mountain(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Mesh mesh)
        {
            GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            return obj;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return obj;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject found = Array.Find(scene.GetRootGameObjects(), root => root.name == name);
            if (found != null) UnityEngine.Object.DestroyImmediate(found);
        }

        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            Set(serialized, property, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(SerializedObject serialized, string property, UnityEngine.Object value)
        {
            SerializedProperty field = serialized.FindProperty(property) ?? throw new InvalidOperationException($"Missing serialized property {property}.");
            field.objectReferenceValue = value;
        }

        private static void Validate(Scene scene)
        {
            if (Array.Find(scene.GetRootGameObjects(), root => root.name == "Balloon Balance Game") == null
                || UnityEngine.Object.FindAnyObjectByType<BalloonGameManager>() == null
                || UnityEngine.Object.FindAnyObjectByType<PointerController>() == null
                || UnityEngine.Object.FindAnyObjectByType<DesktopSimulatorHeadLookToggle>() == null
                || UnityEngine.Object.FindAnyObjectByType<DesktopInputSettingsMenu>() == null
                || UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                throw new InvalidOperationException("Playable game scene validation failed.");
            }
        }
    }
}
