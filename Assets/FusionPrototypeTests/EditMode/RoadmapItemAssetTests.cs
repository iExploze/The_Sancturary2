using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using NUnit.Framework;
using TheSancturary.Inventory;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class RoadmapItemAssetTests
    {
        private const string CatalogPath =
            "Assets/Inventory/Definitions/InventoryItemCatalog.asset";
        private const string PrefabFolder = "Assets/Inventory/Prefabs";
        private const string PlayerPrefabPath =
            "Assets/Prefabs/FusionNetworkPlayer.prefab";
        private const string AnimatorControllerPath =
            "Assets/Animations/Player/PlayerHumanoid.controller";
        private const string ValidationSetupPath =
            PrefabFolder + "/ItemValidationSetup.prefab";
        private const int ReplicatedItemIdCharacterLimit = 31;

        private sealed class ExpectedItem
        {
            public string AssetName;
            public string ItemId;
            public InventoryItemCategory Category;
            public int Width;
            public int Height;
            public bool CanRotate;
            public bool CanEquip;
            public InventoryItemUseKind UseKind;
            public InventoryHoldStyle HoldStyle;
            public string CompatibleAmmoItemId = string.Empty;
            public byte AmmunitionCapacity;
            public byte InitialLoadedAmmunition = 0;
            public string UseAudioName;
            public string DryFireAudioName;
            public string ImpactAudioName;
            public string ReloadAudioName;

            public string WorldPrefabPath =>
                PrefabFolder + "/World" + AssetName + ".prefab";
            public string OwnerHeldPrefabPath =>
                PrefabFolder + "/Held" + AssetName + ".prefab";
            public string RemoteHeldPrefabPath =>
                PrefabFolder + "/Held" + AssetName + "Remote.prefab";
        }

        private static readonly ExpectedItem[] RequiredRoster =
        {
            new ExpectedItem
            {
                AssetName = "Flashlight",
                ItemId = "flashlight",
                Category = InventoryItemCategory.Equipment,
                Width = 1,
                Height = 1,
                CanEquip = true,
                UseKind = InventoryItemUseKind.FlashlightToggle,
                HoldStyle = InventoryHoldStyle.OneHanded,
                UseAudioName = "item_flashlight_toggle"
            },
            new ExpectedItem
            {
                AssetName = "RevivalSyringe",
                ItemId = "revival_syringe",
                Category = InventoryItemCategory.Medicine,
                Width = 2,
                Height = 1,
                CanRotate = true,
                CanEquip = true,
                UseKind = InventoryItemUseKind.RevivalSyringe,
                HoldStyle = InventoryHoldStyle.OneHanded,
                UseAudioName = "item_revival_syringe_use"
            },
            new ExpectedItem
            {
                AssetName = "MedKit",
                ItemId = "med_kit",
                Category = InventoryItemCategory.Medicine,
                Width = 1,
                Height = 1,
                CanEquip = true,
                UseKind = InventoryItemUseKind.FullHeal,
                HoldStyle = InventoryHoldStyle.OneHanded,
                UseAudioName = "item_medkit_use"
            },
            new ExpectedItem
            {
                AssetName = "Adrenaline",
                ItemId = "adrenaline",
                Category = InventoryItemCategory.Medicine,
                Width = 1,
                Height = 1,
                CanEquip = true,
                UseKind = InventoryItemUseKind.Adrenaline,
                HoldStyle = InventoryHoldStyle.OneHanded,
                UseAudioName = "item_adrenaline_use"
            },
            new ExpectedItem
            {
                AssetName = "Crowbar",
                ItemId = "crowbar",
                Category = InventoryItemCategory.PuzzleTool,
                Width = 3,
                Height = 1,
                CanRotate = true,
                CanEquip = true,
                UseKind = InventoryItemUseKind.Crowbar,
                HoldStyle = InventoryHoldStyle.OneHanded,
                UseAudioName = "item_crowbar_swing",
                ImpactAudioName = "item_crowbar_pry"
            },
            new ExpectedItem
            {
                AssetName = "FireAxe",
                ItemId = "fire_axe",
                Category = InventoryItemCategory.PuzzleTool,
                Width = 2,
                Height = 3,
                CanRotate = true,
                CanEquip = true,
                UseKind = InventoryItemUseKind.FireAxe,
                HoldStyle = InventoryHoldStyle.TwoHanded,
                UseAudioName = "item_fireaxe_swing",
                ImpactAudioName = "item_fireaxe_wood_impact"
            },
            new ExpectedItem
            {
                AssetName = "OldRevolver",
                ItemId = "old_revolver",
                Category = InventoryItemCategory.Firearm,
                Width = 2,
                Height = 2,
                CanEquip = true,
                UseKind = InventoryItemUseKind.Firearm,
                HoldStyle = InventoryHoldStyle.OneHanded,
                CompatibleAmmoItemId = "revolver_ammo",
                AmmunitionCapacity = 6,
                UseAudioName = "item_revolver_fire",
                DryFireAudioName = "item_revolver_dry",
                ReloadAudioName = "item_reload_revolver"
            },
            new ExpectedItem
            {
                AssetName = "TranqGun",
                ItemId = "tranq_gun",
                Category = InventoryItemCategory.Firearm,
                Width = 3,
                Height = 2,
                CanEquip = true,
                UseKind = InventoryItemUseKind.Firearm,
                HoldStyle = InventoryHoldStyle.TwoHanded,
                CompatibleAmmoItemId = "tranq_dart",
                AmmunitionCapacity = 1,
                UseAudioName = "item_tranq_fire",
                DryFireAudioName = "item_tranq_dry",
                ReloadAudioName = "item_reload_single_round"
            },
            new ExpectedItem
            {
                AssetName = "SawedOffShotgun",
                ItemId = "sawed_off_shotgun",
                Category = InventoryItemCategory.Firearm,
                Width = 3,
                Height = 2,
                CanEquip = true,
                UseKind = InventoryItemUseKind.Firearm,
                HoldStyle = InventoryHoldStyle.TwoHanded,
                CompatibleAmmoItemId = "shotgun_shell",
                AmmunitionCapacity = 1,
                UseAudioName = "item_shotgun_fire",
                DryFireAudioName = "item_shotgun_dry",
                ReloadAudioName = "item_reload_single_round"
            },
            new ExpectedItem
            {
                AssetName = "RevolverAmmo",
                ItemId = "revolver_ammo",
                Category = InventoryItemCategory.Ammunition,
                Width = 1,
                Height = 1,
                UseKind = InventoryItemUseKind.None,
                HoldStyle = InventoryHoldStyle.None
            },
            new ExpectedItem
            {
                AssetName = "TranqDart",
                ItemId = "tranq_dart",
                Category = InventoryItemCategory.Ammunition,
                Width = 1,
                Height = 1,
                UseKind = InventoryItemUseKind.None,
                HoldStyle = InventoryHoldStyle.None
            },
            new ExpectedItem
            {
                AssetName = "ShotgunShell",
                ItemId = "shotgun_shell",
                Category = InventoryItemCategory.Ammunition,
                Width = 1,
                Height = 1,
                UseKind = InventoryItemUseKind.None,
                HoldStyle = InventoryHoldStyle.None
            }
        };

        [Test]
        public void CatalogContainsEachRequiredItemExactlyOnceAndAllIdsAreValid()
        {
            InventoryItemCatalog catalog = LoadRequiredAsset<InventoryItemCatalog>(
                CatalogPath);
            IReadOnlyList<InventoryItemDefinition> definitions =
                catalog.Definitions;

            Assert.That(definitions, Is.Not.Null);
            Assert.That(definitions.Count, Is.GreaterThanOrEqualTo(
                RequiredRoster.Length));

            var ids = new List<string>(definitions.Count);
            for (int index = 0; index < definitions.Count; index++)
            {
                InventoryItemDefinition definition = definitions[index];
                Assert.That(
                    definition,
                    Is.Not.Null,
                    $"Catalog definition {index} is missing.");
                Assert.That(
                    definition.ItemId,
                    Is.Not.Null.And.Not.Empty,
                    $"Catalog definition '{definition.name}' has no item ID.");
                Assert.That(
                    definition.ItemId,
                    Is.EqualTo(definition.ItemId.Trim()),
                    $"Item ID '{definition.ItemId}' contains surrounding whitespace.");
                Assert.That(
                    definition.ItemId.Length,
                    Is.LessThanOrEqualTo(ReplicatedItemIdCharacterLimit),
                    $"Item ID '{definition.ItemId}' exceeds Fusion's replicated " +
                    $"{ReplicatedItemIdCharacterLimit}-character limit.");
                ids.Add(definition.ItemId);
            }

            Assert.That(
                ids.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(ids.Count),
                "Every catalog item ID must be unique.");

            foreach (ExpectedItem expected in RequiredRoster)
            {
                InventoryItemDefinition[] matches = definitions
                    .Where(definition => definition.ItemId == expected.ItemId)
                    .ToArray();
                Assert.That(
                    matches.Length,
                    Is.EqualTo(1),
                    $"Catalog must contain '{expected.ItemId}' exactly once.");
                Assert.That(
                    catalog.TryGet(expected.ItemId, out InventoryItemDefinition found),
                    Is.True,
                    $"Catalog lookup failed for '{expected.ItemId}'.");
                Assert.That(found, Is.SameAs(matches[0]));
            }
        }

        [Test]
        public void RequiredFootprintsFitTheFixedTwoByFiveGrid()
        {
            foreach (ExpectedItem expected in RequiredRoster)
            {
                bool fitsNormally =
                    expected.Width <= PlayerInventory.Columns &&
                    expected.Height <= PlayerInventory.Rows;
                bool fitsRotated =
                    expected.CanRotate &&
                    expected.Height <= PlayerInventory.Columns &&
                    expected.Width <= PlayerInventory.Rows;
                Assert.That(
                    fitsNormally || fitsRotated,
                    Is.True,
                    $"'{expected.ItemId}' cannot fit the fixed " +
                    $"{PlayerInventory.Columns}x{PlayerInventory.Rows} grid.");
            }

            Assert.That(
                2 * RequiredRoster.Single(item =>
                    item.ItemId == "old_revolver").Width,
                Is.LessThanOrEqualTo(PlayerInventory.Columns),
                "Two firearms must be permitted whenever their footprints fit.");
        }

        [Test]
        public void RequiredDefinitionsMatchTheProvisionalRosterContract()
        {
            InventoryItemCatalog catalog = LoadRequiredAsset<InventoryItemCatalog>(
                CatalogPath);

            foreach (ExpectedItem expected in RequiredRoster)
            {
                InventoryItemDefinition definition = GetDefinition(
                    catalog,
                    expected.ItemId);
                string context = $"Definition '{expected.ItemId}'";

                Assert.That(definition.Category, Is.EqualTo(expected.Category), context);
                Assert.That(definition.Width, Is.EqualTo(expected.Width), context);
                Assert.That(definition.Height, Is.EqualTo(expected.Height), context);
                Assert.That(definition.CanRotate, Is.EqualTo(expected.CanRotate), context);
                Assert.That(definition.Stackable, Is.False, context);
                Assert.That(definition.CanEquip, Is.EqualTo(expected.CanEquip), context);
                Assert.That(definition.CanDrop, Is.True, context);
                Assert.That(definition.UseKind, Is.EqualTo(expected.UseKind), context);
                Assert.That(definition.HoldStyle, Is.EqualTo(expected.HoldStyle), context);
                Assert.That(
                    definition.CompatibleAmmoItemId ?? string.Empty,
                    Is.EqualTo(expected.CompatibleAmmoItemId),
                    context);
                Assert.That(
                    definition.AmmunitionCapacity,
                    Is.EqualTo(expected.AmmunitionCapacity),
                    context);
                Assert.That(
                    definition.InitialLoadedAmmunition,
                    Is.EqualTo(expected.InitialLoadedAmmunition),
                    context);

                Assert.That(definition.WorldPrefab, Is.Not.Null, context);
                Assert.That(
                    AssetDatabase.GetAssetPath(definition.WorldPrefab),
                    Is.EqualTo(expected.WorldPrefabPath),
                    context);

                if (expected.CanEquip)
                {
                    Assert.That(definition.EquippedPrefab, Is.Not.Null, context);
                    Assert.That(
                        definition.ThirdPersonEquippedPrefab,
                        Is.Not.Null.And.Not.SameAs(definition.EquippedPrefab),
                        context);
                    Assert.That(
                        AssetDatabase.GetAssetPath(definition.EquippedPrefab),
                        Is.EqualTo(expected.OwnerHeldPrefabPath),
                        context);
                    Assert.That(
                        AssetDatabase.GetAssetPath(
                            definition.ThirdPersonEquippedPrefab),
                        Is.EqualTo(expected.RemoteHeldPrefabPath),
                        context);
                }
                else
                {
                    Assert.That(definition.EquippedPrefab, Is.Null, context);
                    Assert.That(definition.ThirdPersonEquippedPrefab, Is.Null, context);
                }

                AssertClip(definition.UseAudioClip, expected.UseAudioName, context);
                AssertClip(
                    definition.DryFireAudioClip,
                    expected.DryFireAudioName,
                    context);
                AssertClip(
                    definition.ImpactAudioClip,
                    expected.ImpactAudioName,
                    context);
                AssertClip(
                    definition.ReloadAudioClip,
                    expected.ReloadAudioName,
                    context);
            }
        }

        [Test]
        public void RequiredWorldPrefabsUseTheAuthoritativeWorldItemPattern()
        {
            int worldItemLayer = LayerMask.NameToLayer("WorldItem");
            Assert.That(worldItemLayer, Is.GreaterThanOrEqualTo(0));
            InventoryItemCatalog catalog = LoadRequiredAsset<InventoryItemCatalog>(
                CatalogPath);

            foreach (ExpectedItem expected in RequiredRoster)
            {
                InventoryItemDefinition definition = GetDefinition(
                    catalog,
                    expected.ItemId);
                GameObject prefab = LoadRequiredAsset<GameObject>(
                    expected.WorldPrefabPath);
                string context = $"World prefab '{expected.WorldPrefabPath}'";

                Assert.That(prefab.layer, Is.EqualTo(worldItemLayer), context);
                Assert.That(
                    prefab.GetComponentsInChildren<Transform>(true)
                        .All(child => child.gameObject.layer == worldItemLayer),
                    Is.True,
                    context + " must keep every child on WorldItem.");
                Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null, context);
                Assert.That(prefab.GetComponent<Rigidbody>(), Is.Not.Null, context);
                Assert.That(prefab.GetComponent<WorldItemPhysics>(), Is.Not.Null, context);

                WorldInventoryItem worldItem =
                    prefab.GetComponent<WorldInventoryItem>();
                Assert.That(worldItem, Is.Not.Null, context);
                Assert.That(worldItem.Definition, Is.SameAs(definition), context);
                Assert.That(worldItem.PromptTarget, Is.Not.Null, context);
                Assert.That(
                    prefab.GetComponentsInChildren<Collider>(true).Length,
                    Is.GreaterThan(0),
                    context);
                Assert.That(
                    prefab.GetComponentsInChildren<Renderer>(true).Length,
                    Is.GreaterThan(0),
                    context);
            }
        }

        [Test]
        public void HeldPrefabsAreExactlyTheEighteenVisualOnlyRosterWrappers()
        {
            ExpectedItem[] equippable = RequiredRoster
                .Where(item => item.CanEquip)
                .ToArray();
            string[] expectedPaths = equippable
                .SelectMany(item => new[]
                {
                    item.OwnerHeldPrefabPath,
                    item.RemoteHeldPrefabPath
                })
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] actualPaths = AssetDatabase.FindAssets(
                    "t:Prefab",
                    new[] { PrefabFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path)
                    .StartsWith("Held", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(expectedPaths.Length, Is.EqualTo(18));
            CollectionAssert.AreEquivalent(expectedPaths, actualPaths);

            foreach (ExpectedItem expected in equippable)
            {
                AssertHeldPrefab(expected.OwnerHeldPrefabPath, expected);
                AssertHeldPrefab(expected.RemoteHeldPrefabPath, expected);
            }
        }

        [Test]
        public void ValidationObstaclesRequireOnlyTheirAuthoredTools()
        {
            AssertObstacle(
                PrefabFolder + "/CrowbarValidationObstacle.prefab",
                "crowbar",
                1);
            AssertObstacle(
                PrefabFolder + "/FireAxeValidationObstacle.prefab",
                "fire_axe",
                2);
        }

        [Test]
        public void PlayerPrefabHasItemUseEquipmentAndAnimatorIkWiring()
        {
            GameObject playerPrefab = LoadRequiredAsset<GameObject>(
                PlayerPrefabPath);
            FusionNetworkPlayer player =
                playerPrefab.GetComponentInChildren<FusionNetworkPlayer>(true);
            NetworkPlayerInventory inventory =
                playerPrefab.GetComponentInChildren<NetworkPlayerInventory>(true);
            PlayerEquipment equipment =
                playerPrefab.GetComponentInChildren<PlayerEquipment>(true);
            NetworkItemUseController useController =
                playerPrefab.GetComponent<NetworkItemUseController>();
            Animator animator =
                playerPrefab.GetComponentInChildren<Animator>(true);
            PlayerEquipmentAnimatorIK animatorIK = animator != null
                ? animator.GetComponent<PlayerEquipmentAnimatorIK>()
                : null;

            Assert.That(player, Is.Not.Null);
            Assert.That(inventory, Is.Not.Null);
            Assert.That(equipment, Is.Not.Null);
            Assert.That(useController, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animatorIK, Is.Not.Null);

            AssertSerializedReference(useController, "player", player);
            AssertSerializedReference(useController, "inventory", inventory);
            AssertSerializedReference(equipment, "player", player);
            AssertSerializedReferenceNotNull(equipment, "ownerFirstPersonAnchor");
            AssertSerializedReferenceNotNull(equipment, "thirdPersonAnchor");
            AssertSerializedReference(equipment, "animatorIK", animatorIK);
            AssertSerializedReference(animatorIK, "animator", animator);

            AnimatorController controller = LoadRequiredAsset<AnimatorController>(
                AnimatorControllerPath);
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));
            AnimatorControllerLayer[] itemLayers = controller.layers
                .Where(layer => layer.name == "ItemPresentation")
                .ToArray();
            Assert.That(itemLayers.Length, Is.EqualTo(1));
            Assert.That(itemLayers[0].iKPass, Is.True);
            Assert.That(itemLayers[0].avatarMask, Is.Not.Null);
            Assert.That(itemLayers[0].stateMachine, Is.Not.Null);
        }

        [Test]
        public void ValidationSetupContainsOnePrimaryTwoOfEachAmmoAndTwoObstacles()
        {
            GameObject setup = LoadRequiredAsset<GameObject>(
                ValidationSetupPath);
            WorldInventoryItem[] pickups =
                setup.GetComponentsInChildren<WorldInventoryItem>(true);
            NetworkToolObstacle[] obstacles =
                setup.GetComponentsInChildren<NetworkToolObstacle>(true);

            Assert.That(pickups.Length, Is.EqualTo(15));
            Assert.That(obstacles.Length, Is.EqualTo(2));

            Dictionary<string, int> counts = pickups
                .GroupBy(pickup => pickup.ItemId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(),
                    StringComparer.Ordinal);
            Assert.That(counts.Keys, Is.EquivalentTo(
                RequiredRoster.Select(item => item.ItemId)));

            foreach (ExpectedItem expected in RequiredRoster)
            {
                int expectedCount =
                    expected.Category == InventoryItemCategory.Ammunition
                        ? 2
                        : 1;
                Assert.That(
                    counts.TryGetValue(expected.ItemId, out int actualCount),
                    Is.True,
                    $"Validation setup is missing '{expected.ItemId}'.");
                Assert.That(
                    actualCount,
                    Is.EqualTo(expectedCount),
                    $"Validation setup has the wrong '{expected.ItemId}' count.");
            }

            Assert.That(
                obstacles.Single(obstacle =>
                    obstacle.RequiredItemId == "crowbar").RequiredHits,
                Is.EqualTo(1));
            Assert.That(
                obstacles.Single(obstacle =>
                    obstacle.RequiredItemId == "fire_axe").RequiredHits,
                Is.EqualTo(2));
        }

        private static void AssertHeldPrefab(
            string path,
            ExpectedItem expected)
        {
            GameObject prefab = LoadRequiredAsset<GameObject>(path);
            string context = $"Held prefab '{path}'";
            HeldItemVisual heldVisual = prefab.GetComponent<HeldItemVisual>();

            Assert.That(heldVisual, Is.Not.Null, context);
            Assert.That(
                prefab.GetComponentsInChildren<HeldItemVisual>(true).Length,
                Is.EqualTo(1),
                context);
            Assert.That(
                prefab.GetComponentsInChildren<Renderer>(true).Length,
                Is.GreaterThan(0),
                context);
            Assert.That(heldVisual.RightHandGrip, Is.Not.Null, context);
            Assert.That(heldVisual.EffectOrigin, Is.Not.Null, context);
            Assert.That(
                heldVisual.RightHandGrip.IsChildOf(prefab.transform),
                Is.True,
                context);

            if (expected.HoldStyle == InventoryHoldStyle.TwoHanded)
            {
                Assert.That(heldVisual.LeftHandGrip, Is.Not.Null, context);
                Assert.That(
                    heldVisual.LeftHandGrip.IsChildOf(prefab.transform),
                    Is.True,
                    context);
            }

            Assert.That(
                prefab.GetComponentInChildren<Collider>(true),
                Is.Null,
                context + " must remain visual-only.");
            Assert.That(
                prefab.GetComponentInChildren<Rigidbody>(true),
                Is.Null,
                context + " must remain visual-only.");
            Assert.That(
                prefab.GetComponentInChildren<NetworkObject>(true),
                Is.Null,
                context + " must remain local presentation.");
            Assert.That(
                prefab.GetComponentInChildren<WorldInventoryItem>(true),
                Is.Null,
                context + " must not contain pickup logic.");
        }

        private static void AssertObstacle(
            string path,
            string requiredItemId,
            int requiredHits)
        {
            GameObject prefab = LoadRequiredAsset<GameObject>(path);
            string context = $"Obstacle prefab '{path}'";
            NetworkToolObstacle obstacle =
                prefab.GetComponent<NetworkToolObstacle>();

            Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null, context);
            Assert.That(obstacle, Is.Not.Null, context);
            Assert.That(obstacle.RequiredItemId, Is.EqualTo(requiredItemId), context);
            Assert.That(obstacle.RequiredHits, Is.EqualTo(requiredHits), context);
            Assert.That(
                prefab.GetComponentsInChildren<Collider>(true).Length,
                Is.GreaterThan(0),
                context);
        }

        private static InventoryItemDefinition GetDefinition(
            InventoryItemCatalog catalog,
            string itemId)
        {
            InventoryItemDefinition[] matches = catalog.Definitions
                .Where(definition => definition != null &&
                                     definition.ItemId == itemId)
                .ToArray();
            Assert.That(
                matches.Length,
                Is.EqualTo(1),
                $"Catalog must contain '{itemId}' exactly once.");
            return matches[0];
        }

        private static void AssertClip(
            AudioClip clip,
            string expectedName,
            string context)
        {
            if (string.IsNullOrEmpty(expectedName))
            {
                Assert.That(clip, Is.Null, context);
                return;
            }

            Assert.That(clip, Is.Not.Null, context);
            Assert.That(clip.name, Is.EqualTo(expectedName), context);
        }

        private static T LoadRequiredAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"Required asset is missing: {path}");
            return asset;
        }

        private static void AssertSerializedReference(
            UnityEngine.Object owner,
            string propertyName,
            UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(owner)
                .FindProperty(propertyName);
            Assert.That(
                property,
                Is.Not.Null,
                $"'{owner.name}' has no serialized '{propertyName}' field.");
            Assert.That(
                property.objectReferenceValue,
                Is.SameAs(expected),
                $"'{owner.name}.{propertyName}' is not wired correctly.");
        }

        private static void AssertSerializedReferenceNotNull(
            UnityEngine.Object owner,
            string propertyName)
        {
            SerializedProperty property = new SerializedObject(owner)
                .FindProperty(propertyName);
            Assert.That(
                property,
                Is.Not.Null,
                $"'{owner.name}' has no serialized '{propertyName}' field.");
            Assert.That(
                property.objectReferenceValue,
                Is.Not.Null,
                $"'{owner.name}.{propertyName}' is not wired.");
        }
    }
}
