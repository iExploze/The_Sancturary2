using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheSancturary.Monsters;
using TheSancturary.Multiplayer;
using TheSancturary.Player;
using TheSancturary.UI;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheSancturary.Tests
{
    public sealed class AssetedMapSceneValidationTests
    {
        private const string AssetedMapPath = "Assets/Scenes/AssetedMap.unity";

        private static readonly IReadOnlyDictionary<string, string> RequiredAssetGuids =
            new Dictionary<string, string>
            {
                ["Assets/Skyden_Games/FPS Horror Hospital Pack/Scenes/FPS Horror Hospital Environment Scene.unity"] = "4237f5d7377760541839b2c370686bd6",
                ["Assets/kΩsmaragd/Dark Buttons/dark-Buttons.png"] = "52d82ce7ca39a6b449f7d8ea37dd2f0b",
                ["Assets/Flashlight/Model/Flashlight.prefab"] = "b694b8ec5c41f6247843a2b67222fda6",
                ["Assets/Flashlight/Model/Spotlight.prefab"] = "7c4cfd35d7e78fb44a0f5522e37c35b1",
                ["Assets/Flashlight/Textures/Flashlight_Cookie.png"] = "29135c4c1148bc44883907ae1a731420",
                ["Assets/Skyden_Games/FPS Horror Hospital Pack/Art/Prefabs/Structures/Door_A_Frame.prefab"] = "504e35d7d5171ce4b8b15db466c2940d",
                ["Assets/Skyden_Games/FPS Horror Hospital Pack/Art/Prefabs/Props/Sign Exit.prefab"] = "89da6a0260d5e4c42b36af4176e9021d",
                ["Assets/Skyden_Games/FPS Horror Hospital Pack/Art/Prefabs/Props/Aid Kit Box.prefab"] = "3e8322fabb5bf8e43ab490a07ea50803",
                ["Assets/Skyden_Games/FPS Horror Hospital Pack/Art/Prefabs/Props/Hospital Bowl.prefab"] = "154740dc894a0274eb4418ac46f415ae",
                ["Assets/Skyden_Games/FPS Horror Hospital Pack/Art/Prefabs/Props/Serum.prefab"] = "ed8ecca7d0e405a459b4871db3b4c5c9"
            };

        [Test]
        public void RequiredAssetsRetainPinnedGuids()
        {
            foreach (var asset in RequiredAssetGuids)
            {
                Assert.That(AssetDatabase.LoadMainAssetAtPath(asset.Key), Is.Not.Null, asset.Key);
                Assert.That(AssetDatabase.AssetPathToGUID(asset.Key), Is.EqualTo(asset.Value), asset.Key);
            }
        }

        [Test]
        public void EnabledBuildScenesUsePlayableFlowOrder()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            Assert.That(enabledScenes, Is.EqualTo(new[]
            {
                "Assets/Scenes/MainMenu.unity",
                AssetedMapPath,
                "Assets/Scenes/EndScene.unity"
            }));
        }

        [Test]
        public void AssetedMapHasConfiguredSpawnsExitPickupsAndNavigation()
        {
            EditorSceneManager.OpenScene(AssetedMapPath, OpenSceneMode.Single);

            var playerSpawns = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(point => point.name, StringComparer.Ordinal)
                .ToArray();
            var monsterSpawn = GameObject.Find("MonsterSpawnPoint");
            var exit = Object.FindFirstObjectByType<NetworkExitDoor>(FindObjectsInactive.Include);
            var surface = Object.FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);
            var pickups = Object.FindObjectsByType<NetworkPickableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(playerSpawns, Has.Length.EqualTo(4));
            Assert.That(playerSpawns[0].name, Is.EqualTo("PlayerSpawn_01"));
            Assert.That(playerSpawns[0].transform.position, Is.EqualTo(new Vector3(18.293f, 0.5f, -18.065f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(monsterSpawn, Is.Not.Null);
            Assert.That(monsterSpawn.transform.position, Is.EqualTo(new Vector3(2.846f, 0.5f, 14.765f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(Object.FindFirstObjectByType<PlayerSpawner>(FindObjectsInactive.Include), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<MonsterSpawner>(FindObjectsInactive.Include), Is.Not.Null);
            Assert.That(exit, Is.Not.Null);
            Assert.That(exit.GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(exit.GetComponent<Collider>().isTrigger, Is.True);
            Assert.That(surface, Is.Not.Null);
            Assert.That(surface.navMeshData, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(surface.navMeshData), Is.EqualTo("Assets/Navigation/AssetedMapNavMesh.asset"));
            Assert.That(pickups, Has.Length.EqualTo(3));

            foreach (var pickup in pickups)
            {
                Assert.That(pickup.GetComponent<NetworkObject>(), Is.Not.Null, pickup.name);
                Assert.That(pickup.GetComponent<NetworkObject>().InScenePlaced, Is.True, pickup.name);
                Assert.That(pickup.GetComponent<NetworkTransform>(), Is.Not.Null, pickup.name);
                Assert.That(pickup.GetComponent<NetworkTransform>().AuthorityMode, Is.EqualTo(NetworkTransform.AuthorityModes.Server), pickup.name);
                Assert.That(pickup.GetComponent<NetworkRigidbody>(), Is.Not.Null, pickup.name);
                Assert.That(pickup.GetComponent<Rigidbody>(), Is.Not.Null, pickup.name);
                Assert.That(pickup.GetComponent<BoxCollider>(), Is.Not.Null, pickup.name);
                Assert.That(pickup.GetComponent<NavMeshModifier>()?.ignoreFromBuild, Is.True, pickup.name);
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pickup), Does.StartWith("Assets/Prefabs/Pickups/"), pickup.name);
            }
        }

        [Test]
        public void AssetedMapContainsNoTemporaryActorsOrOverviewCamera()
        {
            EditorSceneManager.OpenScene(AssetedMapPath, OpenSceneMode.Single);

            Assert.That(Object.FindObjectsByType<NetworkPlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
        }

        [Test]
        public void PlayerPrefabContainsNetworkFlashlightPickupAndOwnerHud()
        {
            var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/NetworkPlayer.prefab");
            try
            {
                var flashlight = player.GetComponent<NetworkPlayerFlashlight>();
                var pickup = player.GetComponent<NetworkPlayerPickup>();
                var flashlightSerialized = new SerializedObject(flashlight);
                var pickupSerialized = new SerializedObject(pickup);
                var light = flashlightSerialized.FindProperty("flashlightLight").objectReferenceValue as Light;

                Assert.That(flashlight, Is.Not.Null);
                Assert.That(pickup, Is.Not.Null);
                Assert.That(light, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(light.cookie), Is.EqualTo("Assets/Flashlight/Textures/Flashlight_Cookie.png"));
                Assert.That(flashlightSerialized.FindProperty("flashlightVisual").objectReferenceValue, Is.Not.Null);
                Assert.That(pickupSerialized.FindProperty("playerCamera").objectReferenceValue, Is.Not.Null);
                Assert.That(pickupSerialized.FindProperty("holdPoint").objectReferenceValue, Is.Not.Null);
                Assert.That(pickupSerialized.FindProperty("ownerHud").objectReferenceValue, Is.Not.Null);
                Assert.That(pickupSerialized.FindProperty("interactionPrompt").objectReferenceValue, Is.Not.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        [Test]
        public void MenuAndEndSceneRetainValidControllersAndCallbacks()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var menuController = Object.FindFirstObjectByType<MultiplayerMenuController>(FindObjectsInactive.Include);
            var menuSerialized = new SerializedObject(menuController);
            var menuPanelSprite = GameObject.Find("Panel").GetComponent<Image>().sprite;

            Assert.That(menuController, Is.Not.Null);
            Assert.That(menuSerialized.FindProperty("addressInput").objectReferenceValue, Is.Not.Null);
            Assert.That(menuSerialized.FindProperty("portInput").objectReferenceValue, Is.Not.Null);
            Assert.That(menuSerialized.FindProperty("statusText").objectReferenceValue, Is.Not.Null);
            Assert.That(menuSerialized.FindProperty("hostButton").objectReferenceValue, Is.Not.Null);
            Assert.That(menuSerialized.FindProperty("joinButton").objectReferenceValue, Is.Not.Null);
            Assert.That(menuSerialized.FindProperty("disconnectButton").objectReferenceValue, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(menuPanelSprite), Is.EqualTo("Assets/kΩsmaragd/Dark Buttons/dark-Buttons.png"));

            EditorSceneManager.OpenScene("Assets/Scenes/EndScene.unity", OpenSceneMode.Single);
            var endController = Object.FindFirstObjectByType<EndSceneController>(FindObjectsInactive.Include);
            var endSerialized = new SerializedObject(endController);

            Assert.That(endController, Is.Not.Null);
            Assert.That(endSerialized.FindProperty("returnToMenuButton").objectReferenceValue, Is.Not.Null);
            Assert.That(GameObject.Find("EscapeTitle").GetComponent<Text>().text, Does.Contain("ESCAPED"));
        }

        private sealed class Vector3ComparerWithEqualsOperator : IEqualityComparer<Vector3>
        {
            public static readonly Vector3ComparerWithEqualsOperator Instance = new();

            public bool Equals(Vector3 left, Vector3 right)
            {
                return left == right;
            }

            public int GetHashCode(Vector3 value)
            {
                return value.GetHashCode();
            }
        }
    }
}
