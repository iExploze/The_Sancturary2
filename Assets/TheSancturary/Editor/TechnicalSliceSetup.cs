using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheSancturary.Monsters;
using TheSancturary.Multiplayer;
using TheSancturary.Player;
using TheSancturary.UI;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheSancturary.Editor
{
    public static class TechnicalSliceSetup
    {
        private const string Root = "Assets/TheSancturary";
        private const string ScenesFolder = Root + "/Scenes";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string ScriptableObjectsFolder = Root + "/ScriptableObjects";
        private const string MaterialsFolder = Root + "/Materials";
        private const string NavigationFolder = Root + "/Navigation";
        private const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
        private const string GrayboxScenePath = ScenesFolder + "/GrayboxPrototype.unity";
        private const string PlayerPrefabPath = PrefabsFolder + "/NetworkPlayer.prefab";
        private const string MonsterPrefabPath = PrefabsFolder + "/NetworkMonster.prefab";
        private const string MonsterDefinitionPath = ScriptableObjectsFolder + "/PrototypeMonster.asset";
        private const string NavMeshDataPath = NavigationFolder + "/GrayboxNavMesh.asset";
        private const string NetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";

        private static readonly Color EnvironmentColor = new(0.24f, 0.27f, 0.29f);
        private static readonly Color WallColor = new(0.18f, 0.2f, 0.22f);
        private static readonly Color AccentColor = new(0.16f, 0.58f, 0.62f);
        private static readonly Color MonsterColor = new(0.55f, 0.08f, 0.08f);

        [MenuItem("The Sancturary/Build Technical Vertical Slice")]
        public static void BuildTechnicalSlice()
        {
            BuildTechnicalSliceBatch();
        }

        public static void BuildTechnicalSliceBatch()
        {
            try
            {
                EnsureFolders();
                var materials = CreateMaterials();
                var definition = CreateMonsterDefinition();
                var playerPrefab = CreatePlayerPrefab(materials.Player);
                var monsterPrefab = CreateMonsterPrefab(materials.Monster, definition);
                var networkPrefabs = ConfigureNetworkPrefabs(playerPrefab, monsterPrefab);

                CreateMainMenuScene(networkPrefabs);
                CreateGrayboxScene(materials, playerPrefab, monsterPrefab);
                ConfigureBuildSettings();
                PlayerSettings.runInBackground = true;
                PlayerSettings.productName = "The Sancturary";

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidateGeneratedSlice();
                RenderPreviews();
                Debug.Log("VERTICAL_SLICE_SETUP_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        public static void ValidateTechnicalSliceBatch()
        {
            ValidateGeneratedSlice();
            Debug.Log("VERTICAL_SLICE_VALIDATION_SUCCESS");
        }

        public static void BuildWindowsDevelopmentBatch()
        {
            ValidateGeneratedSlice();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var outputPath = Path.Combine(projectRoot, "Builds", "Windows", "TheSancturary.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? projectRoot);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainMenuScenePath, GrayboxScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows development build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }

            Debug.Log($"VERTICAL_SLICE_BUILD_SUCCESS path={outputPath} size={report.summary.totalSize}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "TheSancturary");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "ScriptableObjects");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Navigation");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static SliceMaterials CreateMaterials()
        {
            return new SliceMaterials
            {
                Floor = CreateMaterial(MaterialsFolder + "/Floor.mat", EnvironmentColor),
                Wall = CreateMaterial(MaterialsFolder + "/Wall.mat", WallColor),
                Accent = CreateMaterial(MaterialsFolder + "/Accent.mat", AccentColor),
                Player = CreateMaterial(MaterialsFolder + "/Player.mat", new Color(0.12f, 0.34f, 0.7f)),
                Monster = CreateMaterial(MaterialsFolder + "/Monster.mat", MonsterColor)
            };
        }

        private static Material CreateMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static MonsterDefinition CreateMonsterDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(MonsterDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MonsterDefinition>();
                AssetDatabase.CreateAsset(definition, MonsterDefinitionPath);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("moveSpeed").floatValue = 3.5f;
            serialized.FindProperty("acceleration").floatValue = 14f;
            serialized.FindProperty("detectionRange").floatValue = 24f;
            serialized.FindProperty("stoppingDistance").floatValue = 1.25f;
            serialized.FindProperty("targetRefreshInterval").floatValue = 0.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static GameObject CreatePlayerPrefab(Material material)
        {
            var root = new GameObject("NetworkPlayer");
            try
            {
                var networkObject = root.AddComponent<NetworkObject>();
                var networkTransform = root.AddComponent<NetworkTransform>();
                networkTransform.Interpolate = true;

                var characterController = root.AddComponent<CharacterController>();
                characterController.center = new Vector3(0f, 0.9f, 0f);
                characterController.height = 1.8f;
                characterController.radius = 0.4f;
                characterController.stepOffset = 0.3f;

                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 1f, 0f);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                var bodyRenderer = body.GetComponent<Renderer>();
                bodyRenderer.sharedMaterial = material;

                var cameraPivot = new GameObject("CameraPivot").transform;
                cameraPivot.SetParent(root.transform, false);
                cameraPivot.localPosition = new Vector3(0f, 1.62f, 0f);

                var cameraObject = new GameObject("PlayerCamera");
                cameraObject.transform.SetParent(cameraPivot, false);
                var playerCamera = cameraObject.AddComponent<Camera>();
                playerCamera.nearClipPlane = 0.05f;
                playerCamera.fieldOfView = 70f;
                playerCamera.enabled = false;
                var audioListener = cameraObject.AddComponent<AudioListener>();
                audioListener.enabled = false;

                var controller = root.AddComponent<NetworkPlayerController>();
                SetReference(controller, "characterController", characterController);
                SetReference(controller, "cameraPivot", cameraPivot);
                SetReference(controller, "playerCamera", playerCamera);
                SetReference(controller, "playerAudioListener", audioListener);
                SetReference(controller, "bodyRenderer", bodyRenderer);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                if (prefab == null || networkObject == null)
                {
                    throw new InvalidOperationException("Failed to create the network player prefab.");
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateMonsterPrefab(Material material, MonsterDefinition definition)
        {
            var root = new GameObject("NetworkMonster");
            try
            {
                root.AddComponent<NetworkObject>();
                var networkTransform = root.AddComponent<NetworkTransform>();
                networkTransform.Interpolate = true;

                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.45f;
                agent.height = 2f;
                agent.baseOffset = 0f;
                agent.angularSpeed = 480f;

                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 1f, 0f);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.GetComponent<Renderer>().sharedMaterial = material;

                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                eye.transform.SetParent(root.transform, false);
                eye.transform.localPosition = new Vector3(0f, 1.45f, 0.43f);
                eye.transform.localScale = Vector3.one * 0.16f;
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                    MaterialsFolder + "/MonsterEye.mat",
                    new Color(0.95f, 0.25f, 0.06f));

                var controller = root.AddComponent<MonsterController>();
                SetReference(controller, "definition", definition);
                SetReference(controller, "agent", agent);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, MonsterPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Failed to create the network monster prefab.");
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static NetworkPrefabsList ConfigureNetworkPrefabs(GameObject playerPrefab, GameObject monsterPrefab)
        {
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
            if (list == null)
            {
                list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
            }

            foreach (var existing in list.PrefabList.ToArray())
            {
                list.Remove(existing);
            }

            list.Add(new NetworkPrefab { Override = NetworkPrefabOverride.None, Prefab = playerPrefab });
            list.Add(new NetworkPrefab { Override = NetworkPrefabOverride.None, Prefab = monsterPrefab });
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
            return list;
        }

        private static void CreateMainMenuScene(NetworkPrefabsList networkPrefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = NetworkConstants.MainMenuSceneName;

            var cameraObject = new GameObject("MainMenuCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.045f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var networkRoot = new GameObject("NetworkRoot");
            var networkManager = networkRoot.AddComponent<NetworkManager>();
            var transport = networkRoot.AddComponent<UnityTransport>();
            networkRoot.AddComponent<PersistentNetworkManager>();
            networkRoot.AddComponent<NetworkSessionController>();
            networkRoot.AddComponent<TechnicalSliceSmokeHarness>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.PlayerPrefab = null;
            networkManager.NetworkConfig.ConnectionApproval = false;
            networkManager.NetworkConfig.EnableSceneManagement = true;
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabs);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            var inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = CreateUiObject("Panel", canvasObject.transform, new Vector2(620f, 610f), Vector2.zero);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.055f, 0.07f, 0.085f, 0.97f);

            CreateText(panel.transform, "Title", "THE SANCTURARY", 38, FontStyle.Bold,
                new Vector2(540f, 70f), new Vector2(0f, 245f), AccentColor);
            CreateText(panel.transform, "Subtitle", "CO-OP HORROR — TECHNICAL SLICE", 18, FontStyle.Normal,
                new Vector2(540f, 36f), new Vector2(0f, 200f), new Color(0.7f, 0.76f, 0.8f));

            CreateText(panel.transform, "AddressLabel", "HOST ADDRESS", 16, FontStyle.Bold,
                new Vector2(500f, 28f), new Vector2(0f, 142f), Color.white, TextAnchor.MiddleLeft);
            var address = CreateInputField(panel.transform, "AddressInput", "127.0.0.1",
                new Vector2(500f, 48f), new Vector2(0f, 102f));

            CreateText(panel.transform, "PortLabel", "PORT", 16, FontStyle.Bold,
                new Vector2(500f, 28f), new Vector2(0f, 48f), Color.white, TextAnchor.MiddleLeft);
            var port = CreateInputField(panel.transform, "PortInput", NetworkConstants.DefaultPort.ToString(),
                new Vector2(500f, 48f), new Vector2(0f, 8f));
            port.contentType = InputField.ContentType.IntegerNumber;

            var hostButton = CreateButton(panel.transform, "HostButton", "HOST",
                new Vector2(240f, 52f), new Vector2(-130f, -72f));
            var joinButton = CreateButton(panel.transform, "JoinButton", "JOIN",
                new Vector2(240f, 52f), new Vector2(130f, -72f));
            var disconnectButton = CreateButton(panel.transform, "DisconnectButton", "DISCONNECT",
                new Vector2(500f, 44f), new Vector2(0f, -132f));
            var quitButton = CreateButton(panel.transform, "QuitButton", "QUIT",
                new Vector2(500f, 44f), new Vector2(0f, -184f));

            var status = CreateText(panel.transform, "StatusText", "Ready.", 15, FontStyle.Normal,
                new Vector2(500f, 50f), new Vector2(0f, -245f), new Color(0.72f, 0.82f, 0.84f));

            var menu = canvasObject.AddComponent<MultiplayerMenuController>();
            SetReference(menu, "addressInput", address);
            SetReference(menu, "portInput", port);
            SetReference(menu, "statusText", status);
            SetReference(menu, "hostButton", hostButton);
            SetReference(menu, "joinButton", joinButton);
            SetReference(menu, "disconnectButton", disconnectButton);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(quitButton.onClick, menu.Quit);

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateGrayboxScene(
            SliceMaterials materials,
            GameObject playerPrefab,
            GameObject monsterPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = NetworkConstants.GrayboxSceneName;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.17f, 0.19f);

            var lightObject = new GameObject("DirectionalLight");
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(0.75f, 0.82f, 0.9f);

            var overviewObject = new GameObject("EditorOverviewCamera");
            overviewObject.transform.position = new Vector3(-24f, 29f, -28f);
            overviewObject.transform.rotation = Quaternion.Euler(35f, 40f, 0f);
            var overviewCamera = overviewObject.AddComponent<Camera>();
            overviewCamera.clearFlags = CameraClearFlags.SolidColor;
            overviewCamera.backgroundColor = new Color(0.025f, 0.035f, 0.045f);
            overviewCamera.fieldOfView = 55f;
            overviewCamera.enabled = false;

            var environment = new GameObject("GrayboxEnvironment").transform;
            CreateBlock(environment, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 30f), materials.Floor);
            CreateBlock(environment, "NorthWall", new Vector3(0f, 1.5f, 15f), new Vector3(40f, 3f, 0.5f), materials.Wall);
            CreateBlock(environment, "SouthWall", new Vector3(0f, 1.5f, -15f), new Vector3(40f, 3f, 0.5f), materials.Wall);
            CreateBlock(environment, "WestWall", new Vector3(-20f, 1.5f, 0f), new Vector3(0.5f, 3f, 30f), materials.Wall);
            CreateBlock(environment, "EastWall", new Vector3(20f, 1.5f, 0f), new Vector3(0.5f, 3f, 30f), materials.Wall);

            CreateBlock(environment, "CenterWallSouth", new Vector3(0f, 1.5f, -9f), new Vector3(0.5f, 3f, 12f), materials.Wall);
            CreateBlock(environment, "CenterWallNorth", new Vector3(0f, 1.5f, 8f), new Vector3(0.5f, 3f, 14f), materials.Wall);
            CreateBlock(environment, "WestRoomWall", new Vector3(-10f, 1.5f, 4f), new Vector3(20f, 3f, 0.5f), materials.Wall);
            CreateBlock(environment, "EastRoomWallA", new Vector3(6f, 1.5f, -4f), new Vector3(12f, 3f, 0.5f), materials.Wall);
            CreateBlock(environment, "EastRoomWallB", new Vector3(17f, 1.5f, -4f), new Vector3(6f, 3f, 0.5f), materials.Wall);

            CreateBlock(environment, "ObstacleA", new Vector3(-13f, 0.8f, 9f), new Vector3(4f, 1.6f, 2.5f), materials.Accent);
            CreateBlock(environment, "ObstacleB", new Vector3(10f, 1f, 8f), new Vector3(3f, 2f, 4f), materials.Accent);
            CreateBlock(environment, "ObstacleC", new Vector3(12f, 0.65f, -10f), new Vector3(5f, 1.3f, 2f), materials.Accent);

            CreatePlayerSpawnPoint("PlayerSpawn_01", new Vector3(-16f, 0f, -11f), materials.Player);
            CreatePlayerSpawnPoint("PlayerSpawn_02", new Vector3(-12f, 0f, -11f), materials.Player);
            CreatePlayerSpawnPoint("PlayerSpawn_03", new Vector3(-16f, 0f, -7f), materials.Player);
            CreatePlayerSpawnPoint("PlayerSpawn_04", new Vector3(-12f, 0f, -7f), materials.Player);

            var monsterSpawn = new GameObject("MonsterSpawnPoint").transform;
            monsterSpawn.position = new Vector3(14f, 0f, 11f);
            CreateMarker(monsterSpawn, "Marker", materials.Monster);

            var playerSpawnerObject = new GameObject("PlayerSpawner");
            playerSpawnerObject.AddComponent<NetworkObject>();
            var playerSpawner = playerSpawnerObject.AddComponent<PlayerSpawner>();
            SetReference(playerSpawner, "playerPrefab", playerPrefab.GetComponent<NetworkObject>());

            var monsterSpawnerObject = new GameObject("MonsterSpawner");
            monsterSpawnerObject.AddComponent<NetworkObject>();
            var monsterSpawner = monsterSpawnerObject.AddComponent<MonsterSpawner>();
            SetReference(monsterSpawner, "monsterPrefab", monsterPrefab.GetComponent<NetworkObject>());
            SetReference(monsterSpawner, "spawnPoint", monsterSpawn);

            CreateGameplayHud();

            var navigationObject = new GameObject("NavigationSurface");
            var surface = navigationObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;

            EditorSceneManager.SaveScene(scene, GrayboxScenePath);
            BakeNavMesh(surface);
            EditorSceneManager.SaveScene(scene, GrayboxScenePath);
        }

        private static void BakeNavMesh(NavMeshSurface surface)
        {
            var existingData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshDataPath);
            if (existingData != null)
            {
                surface.navMeshData = existingData;
                surface.UpdateNavMesh(existingData).completed += _ => { };
                EditorUtility.SetDirty(existingData);
                return;
            }

            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException("NavMesh baking produced no NavMeshData.");
            }

            surface.navMeshData.name = "GrayboxNavMesh";
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshDataPath);
            EditorUtility.SetDirty(surface);
        }

        private static void CreatePlayerSpawnPoint(string name, Vector3 position, Material material)
        {
            var spawnPoint = new GameObject(name);
            spawnPoint.transform.position = position;
            spawnPoint.AddComponent<PlayerSpawnPoint>();
            CreateMarker(spawnPoint.transform, "Marker", material);
        }

        private static void CreateMarker(Transform parent, string name, Material material)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            marker.transform.localScale = new Vector3(0.7f, 0.04f, 0.7f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateGameplayHud()
        {
            var canvasObject = new GameObject("GameplayHud", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            var text = CreateText(
                canvasObject.transform,
                "Instructions",
                "WASD Move   •   Mouse Look   •   Space Jump   •   Esc Cursor   •   F10 Disconnect",
                18,
                FontStyle.Bold,
                new Vector2(1040f, 44f),
                new Vector2(0f, -320f),
                Color.white);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreateUiObject(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return gameObject;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            FontStyle style,
            Vector2 size,
            Vector2 position,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var gameObject = CreateUiObject(name, parent, size, position);
            var text = gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private static InputField CreateInputField(
            Transform parent,
            string name,
            string value,
            Vector2 size,
            Vector2 position)
        {
            var gameObject = CreateUiObject(name, parent, size, position);
            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.15f, 0.17f);
            var input = gameObject.AddComponent<InputField>();

            var text = CreateText(gameObject.transform, "Text", value, 18, FontStyle.Normal,
                size - new Vector2(28f, 8f), Vector2.zero, Color.white, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            input.textComponent = text;
            input.text = value;

            var placeholder = CreateText(gameObject.transform, "Placeholder", string.Empty, 18, FontStyle.Italic,
                size - new Vector2(28f, 8f), Vector2.zero, new Color(1f, 1f, 1f, 0.35f), TextAnchor.MiddleLeft);
            input.placeholder = placeholder;
            return input;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 size,
            Vector2 position)
        {
            var gameObject = CreateUiObject(name, parent, size, position);
            var image = gameObject.AddComponent<Image>();
            image.color = AccentColor;
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(gameObject.transform, "Label", label, 18, FontStyle.Bold, size, Vector2.zero, Color.white);
            return button;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GrayboxScenePath, true)
            };
        }

        private static void ValidateGeneratedSlice()
        {
            var playerPrefab = RequireAsset<GameObject>(PlayerPrefabPath);
            var monsterPrefab = RequireAsset<GameObject>(MonsterPrefabPath);
            var definition = RequireAsset<MonsterDefinition>(MonsterDefinitionPath);
            var networkPrefabs = RequireAsset<NetworkPrefabsList>(NetworkPrefabsPath);
            var navMeshData = RequireAsset<NavMeshData>(NavMeshDataPath);

            RequireComponent<NetworkObject>(playerPrefab, "player prefab");
            RequireComponent<NetworkTransform>(playerPrefab, "player prefab");
            RequireComponent<NetworkPlayerController>(playerPrefab, "player prefab");
            RequireComponent<NetworkObject>(monsterPrefab, "monster prefab");
            RequireComponent<NetworkTransform>(monsterPrefab, "monster prefab");
            RequireComponent<NavMeshAgent>(monsterPrefab, "monster prefab");
            RequireComponent<MonsterController>(monsterPrefab, "monster prefab");

            if (definition.DetectionRange <= 0f || navMeshData.sourceBounds.size.sqrMagnitude <= 0f)
            {
                throw new InvalidOperationException("Monster definition or baked NavMesh data is invalid.");
            }

            if (!networkPrefabs.Contains(playerPrefab) || !networkPrefabs.Contains(monsterPrefab) ||
                networkPrefabs.PrefabList.Count != 2)
            {
                throw new InvalidOperationException("Network prefab registration is inconsistent.");
            }

            ValidateMainMenuScene(networkPrefabs);
            ValidateGrayboxScene();

            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (!enabledScenes.SequenceEqual(new[] { MainMenuScenePath, GrayboxScenePath }))
            {
                throw new InvalidOperationException("Build Settings scenes are not configured in the expected order.");
            }
        }

        private static void ValidateMainMenuScene(NetworkPrefabsList networkPrefabs)
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var networkManager = Object.FindFirstObjectByType<NetworkManager>();
            var menu = Object.FindFirstObjectByType<MultiplayerMenuController>();
            var transport = Object.FindFirstObjectByType<UnityTransport>();
            if (!scene.IsValid() || networkManager == null || menu == null || transport == null)
            {
                throw new InvalidOperationException("Main menu scene is missing required network or UI components.");
            }

            if (networkManager.NetworkConfig.NetworkTransport != transport ||
                networkManager.NetworkConfig.PlayerPrefab != null ||
                !networkManager.NetworkConfig.EnableSceneManagement ||
                networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Count != 1 ||
                networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists[0] != networkPrefabs)
            {
                throw new InvalidOperationException("NetworkManager configuration is inconsistent.");
            }
        }

        private static void ValidateGrayboxScene()
        {
            var scene = EditorSceneManager.OpenScene(GrayboxScenePath, OpenSceneMode.Single);
            var surface = Object.FindFirstObjectByType<NavMeshSurface>();
            var playerSpawnPoints = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            var monsterSpawnPoint = GameObject.Find("MonsterSpawnPoint");
            if (!scene.IsValid() ||
                Object.FindFirstObjectByType<PlayerSpawner>() == null ||
                Object.FindFirstObjectByType<MonsterSpawner>() == null ||
                playerSpawnPoints.Length != 4 ||
                monsterSpawnPoint == null ||
                surface == null ||
                surface.navMeshData == null ||
                AssetDatabase.GetAssetPath(surface.navMeshData) != NavMeshDataPath)
            {
                throw new InvalidOperationException("Graybox scene is missing spawners, spawn points, or baked NavMesh data.");
            }

            foreach (var spawnPoint in playerSpawnPoints)
            {
                RequireNavMeshPosition(spawnPoint.transform.position, spawnPoint.name);
            }

            RequireNavMeshPosition(monsterSpawnPoint.transform.position, monsterSpawnPoint.name);
        }

        private static void RequireNavMeshPosition(Vector3 position, string context)
        {
            if (!NavMesh.SamplePosition(position, out _, 1f, NavMesh.AllAreas))
            {
                throw new InvalidOperationException($"{context} is not on the baked NavMesh.");
            }
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required asset is missing: {path}");
            }

            return asset;
        }

        private static void RequireComponent<T>(GameObject gameObject, string context) where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
            {
                throw new InvalidOperationException($"{context} is missing {typeof(T).Name}.");
            }
        }

        private static void RenderPreviews()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var previewFolder = Path.Combine(projectRoot, "Logs", "Previews");
            Directory.CreateDirectory(previewFolder);

            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            RenderCameraPreview(
                Object.FindFirstObjectByType<Camera>(),
                Path.Combine(previewFolder, "MainMenu.png"));

            EditorSceneManager.OpenScene(GrayboxScenePath, OpenSceneMode.Single);
            var overview = GameObject.Find("EditorOverviewCamera")?.GetComponent<Camera>();
            RenderCameraPreview(overview, Path.Combine(previewFolder, "Graybox.png"));
        }

        private static void RenderCameraPreview(Camera camera, string outputPath)
        {
            if (camera == null)
            {
                throw new InvalidOperationException($"Preview camera is missing for {outputPath}.");
            }

            const int width = 1280;
            const int height = 720;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                Debug.Log($"VERTICAL_SLICE_PREVIEW path={outputPath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class SliceMaterials
        {
            public Material Floor;
            public Material Wall;
            public Material Accent;
            public Material Player;
            public Material Monster;
        }
    }
}
