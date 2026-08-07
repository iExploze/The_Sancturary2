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
using UnityEngine.Animations.Rigging;
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
            public InventoryHoldPose HoldPose;
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
                HoldPose = InventoryHoldPose.OneHandedCarry,
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
                HoldPose = InventoryHoldPose.OneHandedCarry,
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
                HoldPose = InventoryHoldPose.OneHandedCarry,
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
                HoldPose = InventoryHoldPose.OneHandedCarry,
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
                HoldPose = InventoryHoldPose.OneHandedCarry,
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
                HoldPose = InventoryHoldPose.TwoHandedTool,
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
                HoldPose = InventoryHoldPose.Pistol,
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
                HoldPose = InventoryHoldPose.LongGun,
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
                HoldPose = InventoryHoldPose.LongGun,
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
                Assert.That(definition.HoldPose, Is.EqualTo(expected.HoldPose), context);
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

                if (expected.CanEquip)
                {
                    MeshCollider[] meshColliders =
                        prefab.GetComponentsInChildren<MeshCollider>(true);
                    Assert.That(
                        meshColliders.Length,
                        Is.GreaterThan(0),
                        context + " must use mesh-shaped collision.");
                    Assert.That(
                        prefab.GetComponentsInChildren<BoxCollider>(true),
                        Is.Empty,
                        context + " must not retain the generic box collider.");
                    Assert.That(
                        meshColliders.All(collider =>
                            collider.convex && collider.sharedMesh != null),
                        Is.True,
                        context + " requires cooked convex collision meshes.");

                    WorldItemPhysics physics =
                        prefab.GetComponent<WorldItemPhysics>();
                    Assert.That(physics.HasValidLocalBounds, Is.True, context);
                    CollectionAssert.AreEquivalent(
                        meshColliders,
                        physics.PhysicalColliders,
                        context + " must reference every physical collider.");

                    SerializedObject serializedWorldItem =
                        new(worldItem);
                    SerializedProperty pickupColliders =
                        serializedWorldItem.FindProperty("pickupColliders");
                    Assert.That(
                        pickupColliders.arraySize,
                        Is.EqualTo(meshColliders.Length),
                        context + " must reference every pickup collider.");
                    for (int index = 0;
                         index < pickupColliders.arraySize;
                         index++)
                    {
                        Assert.That(
                            pickupColliders.GetArrayElementAtIndex(index)
                                .objectReferenceValue,
                            Is.Not.Null,
                            context + " contains a missing pickup collider.");
                    }
                }
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

        [TestCase("Adrenaline", "Assets/Props/Medical Glass_8.prefab")]
        [TestCase("MedKit", "Assets/Props/Medical Glass_10.prefab")]
        public void BottleWrappersUseTheRequestedVisualSource(
            string itemName,
            string expectedSourcePath)
        {
            string[] wrapperPaths =
            {
                PrefabFolder + "/World" + itemName + ".prefab",
                PrefabFolder + "/Held" + itemName + ".prefab",
                PrefabFolder + "/Held" + itemName + "Remote.prefab"
            };

            foreach (string wrapperPath in wrapperPaths)
            {
                GameObject wrapper = LoadRequiredAsset<GameObject>(wrapperPath);
                Transform model = wrapper.transform.Find("VisualRoot/Model");
                Assert.That(model, Is.Not.Null, wrapperPath);
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(
                    model.gameObject);
                Assert.That(source, Is.Not.Null, wrapperPath);
                Assert.That(
                    AssetDatabase.GetAssetPath(source),
                    Is.EqualTo(expectedSourcePath),
                    wrapperPath);
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
        public void PlayerPrefabHasItemUseEquipmentAndAnimationRiggingWiring()
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
            PlayerEquipmentRigController equipmentRig = animator != null
                ? animator.GetComponent<PlayerEquipmentRigController>()
                : null;
            PlayerThirdPersonLookPresentation lookPresentation =
                animator != null
                    ? animator.GetComponent<
                        PlayerThirdPersonLookPresentation>()
                    : null;
            RigBuilder rigBuilder = animator != null
                ? animator.GetComponent<RigBuilder>()
                : null;
            Transform itemRig = animator != null
                ? animator.transform.Find("ItemRig")
                : null;
            Rig rightArmRig = itemRig != null
                ? itemRig.Find("RightArmRig")?.GetComponent<Rig>()
                : null;
            Rig leftArmRig = itemRig != null
                ? itemRig.Find("LeftArmRig")?.GetComponent<Rig>()
                : null;
            Rig chestAimRig = itemRig != null
                ? itemRig.Find("ChestAimRig")?.GetComponent<Rig>()
                : null;
            Rig headLookRig = itemRig != null
                ? itemRig.Find("HeadLookRig")?.GetComponent<Rig>()
                : null;
            TwoBoneIKConstraint rightArmConstraint = rightArmRig != null
                ? rightArmRig.transform.Find("RightArmIK")
                    ?.GetComponent<TwoBoneIKConstraint>()
                : null;
            TwoBoneIKConstraint leftArmConstraint = leftArmRig != null
                ? leftArmRig.transform.Find("LeftArmIK")
                    ?.GetComponent<TwoBoneIKConstraint>()
                : null;
            MultiAimConstraint headLookConstraint = headLookRig != null
                ? headLookRig.transform.Find("HeadLookAim")
                    ?.GetComponent<MultiAimConstraint>()
                : null;
            MultiAimConstraint chestAimConstraint = chestAimRig != null
                ? chestAimRig.transform.Find("UpperChestAim")
                    ?.GetComponent<MultiAimConstraint>()
                : null;

            Assert.That(player, Is.Not.Null);
            Assert.That(inventory, Is.Not.Null);
            Assert.That(equipment, Is.Not.Null);
            Assert.That(useController, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(equipmentRig, Is.Not.Null);
            Assert.That(lookPresentation, Is.Not.Null);
            Assert.That(rigBuilder, Is.Not.Null);
            Assert.That(itemRig, Is.Not.Null);
            Assert.That(rightArmRig, Is.Not.Null);
            Assert.That(leftArmRig, Is.Not.Null);
            Assert.That(chestAimRig, Is.Not.Null);
            Assert.That(headLookRig, Is.Not.Null);
            Assert.That(rightArmConstraint, Is.Not.Null);
            Assert.That(leftArmConstraint, Is.Not.Null);
            Assert.That(headLookConstraint, Is.Not.Null);
            Assert.That(rightArmConstraint.IsValid(), Is.True);
            Assert.That(leftArmConstraint.IsValid(), Is.True);
            Assert.That(chestAimConstraint, Is.Not.Null);
            Assert.That(chestAimConstraint.IsValid(), Is.True);
            Assert.That(headLookConstraint.IsValid(), Is.True);

            AssertSerializedReference(useController, "player", player);
            AssertSerializedReference(useController, "inventory", inventory);
            AssertSerializedReference(equipment, "player", player);
            AssertSerializedReferenceNotNull(equipment, "ownerFirstPersonAnchor");
            AssertSerializedReference(
                equipment,
                "thirdPersonAnchor",
                lookPresentation.ItemPitchPivot);
            AssertSerializedReference(equipment, "equipmentRig", equipmentRig);
            AssertSerializedReference(equipmentRig, "animator", animator);
            AssertSerializedReference(equipmentRig, "rigBuilder", rigBuilder);
            AssertSerializedReference(
                equipmentRig,
                "rightArmRig",
                rightArmRig);
            AssertSerializedReference(
                equipmentRig,
                "leftArmRig",
                leftArmRig);
            AssertSerializedReference(
                equipmentRig,
                "rightArmConstraint",
                rightArmConstraint);
            AssertSerializedReference(
                equipmentRig,
                "leftArmConstraint",
                leftArmConstraint);
            Assert.That(rigBuilder.layers.Count, Is.EqualTo(4));
            Assert.That(
                rigBuilder.layers.Select(layer => layer.rig),
                Is.EqualTo(new[]
                {
                    rightArmRig,
                    leftArmRig,
                    chestAimRig,
                    headLookRig
                }));

            SerializedObject serializedRig = new(equipmentRig);
            Transform rightTarget = (Transform)serializedRig
                .FindProperty("rightHandTarget").objectReferenceValue;
            Transform leftTarget = (Transform)serializedRig
                .FindProperty("leftHandTarget").objectReferenceValue;
            Transform rightHint = (Transform)serializedRig
                .FindProperty("rightElbowHint").objectReferenceValue;
            Transform leftHint = (Transform)serializedRig
                .FindProperty("leftElbowHint").objectReferenceValue;
            Assert.That(rightTarget, Is.Not.Null);
            Assert.That(leftTarget, Is.Not.Null);
            Assert.That(rightHint, Is.Not.Null);
            Assert.That(leftHint, Is.Not.Null);
            Assert.That(rightTarget.IsChildOf(animator.avatarRoot), Is.False);
            Assert.That(leftTarget.IsChildOf(animator.avatarRoot), Is.False);
            Assert.That(rightArmConstraint.data.target, Is.SameAs(rightTarget));
            Assert.That(leftArmConstraint.data.target, Is.SameAs(leftTarget));
            Assert.That(rightArmConstraint.data.hint, Is.SameAs(rightHint));
            Assert.That(leftArmConstraint.data.hint, Is.SameAs(leftHint));
            Assert.That(
                rightArmConstraint.data.root,
                Is.SameAs(animator.GetBoneTransform(
                    HumanBodyBones.RightUpperArm)));
            Assert.That(
                leftArmConstraint.data.root,
                Is.SameAs(animator.GetBoneTransform(
                    HumanBodyBones.LeftUpperArm)));

            Transform upperChest = animator.GetBoneTransform(
                HumanBodyBones.UpperChest);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform expectedTorso = upperChest != null ? upperChest : chest;
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Assert.That(expectedTorso, Is.Not.Null);
            Assert.That(head, Is.Not.Null);
            Assert.That(lookPresentation.TorsoBone, Is.SameAs(expectedTorso));
            Assert.That(lookPresentation.HeadBone, Is.SameAs(head));
            Assert.That(
                lookPresentation.ItemBodyAnchor.parent,
                Is.SameAs(expectedTorso));
            Assert.That(
                lookPresentation.ItemPitchPivot.parent,
                Is.SameAs(lookPresentation.ItemBodyAnchor));
            Assert.That(lookPresentation.HeadLookTarget, Is.Not.Null);
            Assert.That(
                lookPresentation.HeadLookTarget.IsChildOf(animator.avatarRoot),
                Is.False);
            Assert.That(
                lookPresentation.HeadLookRig,
                Is.SameAs(headLookRig));
            Assert.That(
                lookPresentation.HeadLookConstraint,
                Is.SameAs(headLookConstraint));
            Assert.That(lookPresentation.ChestAimRig, Is.SameAs(chestAimRig));
            Assert.That(
                lookPresentation.ChestAimConstraint,
                Is.SameAs(chestAimConstraint));
            Assert.That(lookPresentation.ChestAimTarget, Is.Not.Null);
            Assert.That(
                lookPresentation.ChestAimTarget.IsChildOf(animator.avatarRoot),
                Is.False);
            Assert.That(
                chestAimConstraint.data.constrainedObject,
                Is.SameAs(expectedTorso));
            Assert.That(chestAimConstraint.data.sourceObjects.Count, Is.EqualTo(1));
            Assert.That(
                chestAimConstraint.data.sourceObjects[0].transform,
                Is.SameAs(lookPresentation.ChestAimTarget));
            Assert.That(chestAimConstraint.data.constrainedXAxis, Is.True);
            Assert.That(chestAimConstraint.data.constrainedYAxis, Is.False);
            Assert.That(chestAimConstraint.data.constrainedZAxis, Is.False);
            Assert.That(
                headLookConstraint.data.constrainedObject,
                Is.SameAs(head));
            Assert.That(headLookConstraint.data.sourceObjects.Count, Is.EqualTo(1));
            Assert.That(
                headLookConstraint.data.sourceObjects[0].transform,
                Is.SameAs(lookPresentation.HeadLookTarget));
            Assert.That(
                headLookConstraint.data.limits,
                Is.EqualTo(new Vector2(-45f, 45f)));
            Assert.That(headLookConstraint.data.constrainedXAxis, Is.True);
            Assert.That(headLookConstraint.data.constrainedYAxis, Is.False);
            Assert.That(headLookConstraint.data.constrainedZAxis, Is.False);
            Assert.That(
                lookPresentation,
                Is.Not.InstanceOf<NetworkBehaviour>());

            AnimatorController controller = LoadRequiredAsset<AnimatorController>(
                AnimatorControllerPath);
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));
            Assert.That(
                controller.parameters.Any(parameter =>
                    parameter.name == PlayerEquipmentRigController.HoldPoseParameterName &&
                    parameter.type == AnimatorControllerParameterType.Int),
                Is.True);
            AnimatorControllerLayer[] itemLayers = controller.layers
                .Where(layer => layer.name ==
                                    PlayerEquipmentRigController.OneHandPoseLayerName ||
                                layer.name ==
                                    PlayerEquipmentRigController.TwoHandPoseLayerName)
                .ToArray();
            Assert.That(itemLayers.Length, Is.EqualTo(2));
            Assert.That(itemLayers.All(layer => !layer.iKPass), Is.True);
            Assert.That(itemLayers.All(layer => layer.avatarMask != null), Is.True);
            Assert.That(itemLayers.All(layer => layer.stateMachine != null), Is.True);
            Assert.That(itemLayers.All(layer => layer.defaultWeight == 0f), Is.True);
            Assert.That(
                itemLayers.Select(layer => AssetDatabase.GetAssetPath(layer.avatarMask)),
                Is.EquivalentTo(new[]
                {
                    "Assets/Animations/Player/PlayerRightArmHold.mask",
                    "Assets/Animations/Player/PlayerUpperBody.mask"
                }));
            AvatarMask oneHandMask = itemLayers.Single(layer =>
                layer.name == PlayerEquipmentRigController.OneHandPoseLayerName)
                .avatarMask;
            Assert.That(
                oneHandMask.GetHumanoidBodyPartActive(
                    AvatarMaskBodyPart.RightArm),
                Is.True);
            Assert.That(
                oneHandMask.GetHumanoidBodyPartActive(
                    AvatarMaskBodyPart.LeftArm),
                Is.False);
            Assert.That(
                oneHandMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body),
                Is.False);
            AvatarMask twoHandMask = itemLayers.Single(layer =>
                layer.name == PlayerEquipmentRigController.TwoHandPoseLayerName)
                .avatarMask;
            Assert.That(
                twoHandMask.GetHumanoidBodyPartActive(
                    AvatarMaskBodyPart.RightArm),
                Is.True);
            Assert.That(
                twoHandMask.GetHumanoidBodyPartActive(
                    AvatarMaskBodyPart.LeftArm),
                Is.True);
            Assert.That(
                twoHandMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body),
                Is.True);
            Assert.That(
                twoHandMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root),
                Is.False);
            Assert.That(
                twoHandMask.GetHumanoidBodyPartActive(
                    AvatarMaskBodyPart.LeftLeg),
                Is.False);
            Assert.That(
                twoHandMask.GetHumanoidBodyPartActive(
                    AvatarMaskBodyPart.RightLeg),
                Is.False);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/Animations/Player/ItemPoses/OneHandedCarryPose.anim"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/Animations/Player/ItemPoses/LongGunHoldPose.anim"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/Animations/Player/ItemPoses/TwoHandedToolPose.anim"),
                Is.Not.Null);
        }

        [Test]
        public void ThirdPersonLookPresentationClampsWithoutChangingRawLookPitch()
        {
            float[] rawPitches = { 0f, 20f, 45f, 80f, -20f, -45f, -80f };
            float[] expected = { 0f, 20f, 45f, 45f, -20f, -45f, -45f };

            for (int index = 0; index < rawPitches.Length; index++)
            {
                float rawPitch = rawPitches[index];
                float presentationPitch =
                    PlayerThirdPersonLookPresentation
                        .ClampPresentationPitch(rawPitch);

                Assert.That(presentationPitch, Is.EqualTo(expected[index]));
                Assert.That(rawPitch, Is.EqualTo(rawPitches[index]));
            }
        }

        [Test]
        public void EquipmentRigUsesRightHandOnlyForOneHandedHoldStyle()
        {
            GameObject playerInstance = UnityEngine.Object.Instantiate(
                LoadRequiredAsset<GameObject>(PlayerPrefabPath));
            GameObject oneHandedInstance = UnityEngine.Object.Instantiate(
                LoadRequiredAsset<GameObject>(
                    PrefabFolder + "/HeldCrowbarRemote.prefab"));
            GameObject twoHandedInstance = UnityEngine.Object.Instantiate(
                LoadRequiredAsset<GameObject>(
                    PrefabFolder + "/HeldFireAxeRemote.prefab"));
            try
            {
                PlayerEquipment equipment =
                    playerInstance.GetComponent<PlayerEquipment>();
                PlayerEquipmentRigController equipmentRig =
                    playerInstance.GetComponentInChildren<
                        PlayerEquipmentRigController>(true);
                HeldItemVisual oneHandedVisual =
                    oneHandedInstance.GetComponent<HeldItemVisual>();
                HeldItemVisual twoHandedVisual =
                    twoHandedInstance.GetComponent<HeldItemVisual>();

                equipmentRig.Configure(
                    equipment,
                    oneHandedVisual.RightHandGrip,
                    oneHandedVisual.LeftHandGrip,
                    InventoryHoldStyle.OneHanded,
                    InventoryHoldPose.OneHandedCarry);
                Assert.That(equipmentRig.DesiredRightWeight, Is.EqualTo(1f));
                Assert.That(equipmentRig.DesiredLeftWeight, Is.EqualTo(0f));

                equipmentRig.Configure(
                    equipment,
                    twoHandedVisual.RightHandGrip,
                    twoHandedVisual.LeftHandGrip,
                    InventoryHoldStyle.TwoHanded,
                    InventoryHoldPose.TwoHandedTool);
                Assert.That(equipmentRig.DesiredRightWeight, Is.EqualTo(1f));
                Assert.That(equipmentRig.DesiredLeftWeight, Is.EqualTo(1f));

                equipmentRig.Clear(equipment);
                Assert.That(equipmentRig.DesiredRightWeight, Is.EqualTo(0f));
                Assert.That(equipmentRig.DesiredLeftWeight, Is.EqualTo(0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(twoHandedInstance);
                UnityEngine.Object.DestroyImmediate(oneHandedInstance);
                UnityEngine.Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void DynamicDropPoseStagingKeepsTransformAndRigidbodyAlignedForFiftyFarPoses()
        {
            GameObject instance = new("Dynamic Drop Pose Test");
            try
            {
                Rigidbody body = instance.AddComponent<Rigidbody>();
                WorldItemPhysics physics =
                    instance.AddComponent<WorldItemPhysics>();
                physics.Configure(
                    instance.GetComponent<NetworkObject>(),
                    body,
                    Array.Empty<Collider>(),
                    new Bounds(Vector3.zero, Vector3.one));

                for (int index = 0; index < 50; index++)
                {
                    Vector3 position = new(
                        40f + index * 1.7f,
                        3f + index * 0.05f,
                        -35f - index * 1.3f);
                    Quaternion rotation = Quaternion.Euler(
                        11f + index,
                        70f + index * 3f,
                        -7f);

                    Assert.That(
                        physics.StageDropPoseBeforeSpawn(position, rotation),
                        Is.True,
                        $"cycle {index}");
                    Assert.That(
                        Vector3.Distance(instance.transform.position, position),
                        Is.LessThan(0.001f),
                        $"cycle {index}");
                    Assert.That(
                        Vector3.Distance(physics.Body.position, position),
                        Is.LessThan(0.001f),
                        $"cycle {index}");
                    Assert.That(
                        Quaternion.Angle(instance.transform.rotation, rotation),
                        Is.LessThan(0.01f),
                        $"cycle {index}");
                    Assert.That(
                        Quaternion.Angle(physics.Body.rotation, rotation),
                        Is.LessThan(0.01f),
                        $"cycle {index}");
                    Assert.That(
                        physics.Body.isKinematic,
                        Is.True,
                        $"cycle {index}");
                    Assert.That(
                        physics.Body.useGravity,
                        Is.False,
                        $"cycle {index}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
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
