using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheSancturary.Editor
{
    public static class AssetLayoutMigration
    {
        private const string OldRoot = "Assets/TheSancturary";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GrayboxScenePath = "Assets/Scenes/GrayboxPrototype.unity";

        private static readonly FolderMove[] FolderMoves =
        {
            new(OldRoot + "/Editor", "Assets/Editor/TheSancturary"),
            new(OldRoot + "/Materials", "Assets/Materials"),
            new(OldRoot + "/Navigation", "Assets/Navigation"),
            new(OldRoot + "/Prefabs", "Assets/Prefabs"),
            new(OldRoot + "/Scenes", "Assets/Scenes"),
            new(OldRoot + "/ScriptableObjects", "Assets/ScriptableObjects"),
            new(OldRoot + "/Scripts", "Assets/Scripts"),
            new(OldRoot + "/Tests", "Assets/Tests")
        };

        [MenuItem("The Sancturary/Migrate/Flatten Asset Layout")]
        public static void RunFromMenu()
        {
            RunBatch();
        }

        public static void RunBatch()
        {
            try
            {
                Migrate();
                Debug.Log("ASSET_LAYOUT_MIGRATION_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogError($"ASSET_LAYOUT_MIGRATION_FAILURE: {exception.Message}");
                Debug.LogException(exception);
                throw;
            }
        }

        private static void Migrate()
        {
            if (!AssetDatabase.IsValidFolder(OldRoot))
            {
                ValidateCompletedLayout();
                ConfigureBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("Asset layout was already flattened; no assets were moved.");
                return;
            }

            var expectedAssets = CollectExpectedAssetMoves();
            PreflightDestinations(expectedAssets);
            EnsureFolderPath("Assets/Editor");

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var folderMove in FolderMoves)
                {
                    MoveOrMergeFolder(folderMove.Source, folderMove.Destination);
                }

                RequireEmptyFolder(OldRoot);
                if (!AssetDatabase.DeleteAsset(OldRoot))
                {
                    throw new InvalidOperationException($"Unity could not delete the empty folder '{OldRoot}'.");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            VerifyMovedAssetGuids(expectedAssets);
            ValidateCompletedLayout();
        }

        private static List<AssetMove> CollectExpectedAssetMoves()
        {
            var expectedAssets = new List<AssetMove>();
            foreach (var folderMove in FolderMoves)
            {
                if (!AssetDatabase.IsValidFolder(folderMove.Source))
                {
                    continue;
                }

                foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { folderMove.Source }))
                {
                    var sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(sourcePath) || AssetDatabase.IsValidFolder(sourcePath))
                    {
                        continue;
                    }

                    var suffix = sourcePath.Substring(folderMove.Source.Length);
                    expectedAssets.Add(new AssetMove(sourcePath, folderMove.Destination + suffix, guid));
                }
            }

            return expectedAssets;
        }

        private static void PreflightDestinations(IEnumerable<AssetMove> expectedAssets)
        {
            var collisions = new List<string>();
            foreach (var expectedAsset in expectedAssets)
            {
                var destinationGuid = AssetDatabase.AssetPathToGUID(expectedAsset.Destination);
                if (!string.IsNullOrEmpty(destinationGuid))
                {
                    collisions.Add($"{expectedAsset.Source} -> {expectedAsset.Destination}");
                }
            }

            foreach (var folderMove in FolderMoves)
            {
                var destinationGuid = AssetDatabase.AssetPathToGUID(folderMove.Destination);
                if (!string.IsNullOrEmpty(destinationGuid) &&
                    !AssetDatabase.IsValidFolder(folderMove.Destination))
                {
                    collisions.Add($"Folder destination is occupied by an asset: {folderMove.Destination}");
                }
            }

            if (collisions.Count > 0)
            {
                throw new InvalidOperationException(
                    "Asset migration found destination collisions and made no moves:\n" +
                    string.Join("\n", collisions));
            }
        }

        private static void MoveOrMergeFolder(string source, string destination)
        {
            if (!AssetDatabase.IsValidFolder(source))
            {
                return;
            }

            EnsureFolderPath(GetParentPath(destination));
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(destination)))
            {
                MoveAsset(source, destination);
                return;
            }

            if (!AssetDatabase.IsValidFolder(destination))
            {
                throw new InvalidOperationException($"Destination is not a folder: {destination}");
            }

            var directAssets = AssetDatabase.FindAssets(string.Empty, new[] { source })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path =>
                    !string.IsNullOrEmpty(path) &&
                    !AssetDatabase.IsValidFolder(path) &&
                    string.Equals(GetParentPath(path), source, StringComparison.Ordinal))
                .Distinct()
                .ToArray();

            foreach (var sourceAsset in directAssets)
            {
                MoveAsset(sourceAsset, destination + "/" + Path.GetFileName(sourceAsset));
            }

            foreach (var sourceSubfolder in AssetDatabase.GetSubFolders(source).ToArray())
            {
                MoveOrMergeFolder(
                    sourceSubfolder,
                    destination + "/" + Path.GetFileName(sourceSubfolder));
            }

            RequireEmptyFolder(source);
            if (!AssetDatabase.DeleteAsset(source))
            {
                throw new InvalidOperationException($"Unity could not delete the empty folder '{source}'.");
            }
        }

        private static void MoveAsset(string source, string destination)
        {
            var error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"Could not move '{source}' to '{destination}': {error}");
            }

            Debug.Log($"ASSET_LAYOUT_MIGRATION_MOVE {source} -> {destination}");
        }

        private static void VerifyMovedAssetGuids(IEnumerable<AssetMove> expectedAssets)
        {
            foreach (var expectedAsset in expectedAssets)
            {
                var actualGuid = AssetDatabase.AssetPathToGUID(expectedAsset.Destination);
                if (!string.Equals(actualGuid, expectedAsset.Guid, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"GUID changed while moving '{expectedAsset.Source}' to '{expectedAsset.Destination}'. " +
                        $"Expected {expectedAsset.Guid}, found {actualGuid}.");
                }
            }
        }

        private static void RequireEmptyFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            var remainingEntries = Directory.EnumerateFileSystemEntries(folder).ToArray();
            if (remainingEntries.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Unity asset moves did not leave '{folder}' empty. Remaining entries: " +
                    string.Join(", ", remainingEntries));
            }
        }

        private static void ValidateCompletedLayout()
        {
            if (AssetDatabase.IsValidFolder(OldRoot))
            {
                throw new InvalidOperationException($"Old asset root still exists: {OldRoot}");
            }

            foreach (var folderMove in FolderMoves)
            {
                if (!AssetDatabase.IsValidFolder(folderMove.Destination))
                {
                    throw new InvalidOperationException($"Expected destination folder is missing: {folderMove.Destination}");
                }
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) == null ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(GrayboxScenePath) == null)
            {
                throw new InvalidOperationException("One or both migrated gameplay scenes are missing.");
            }
        }

        private static void ConfigureBuildSettings()
        {
            var expectedScenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GrayboxScenePath, true)
            };

            // Clearing first prevents Unity from retaining stale paths when the scene GUIDs
            // are unchanged by an AssetDatabase move.
            EditorBuildSettings.scenes = Array.Empty<EditorBuildSettingsScene>();
            EditorBuildSettings.scenes = expectedScenes;

            var configuredPaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            if (!configuredPaths.SequenceEqual(new[] { MainMenuScenePath, GrayboxScenePath }))
            {
                throw new InvalidOperationException(
                    "Unity did not update Build Settings to the migrated scene paths. Current paths: " +
                    string.Join(", ", configuredPaths));
            }
        }

        private static void EnsureFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = GetParentPath(folderPath);
            EnsureFolderPath(parent);
            var guid = AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException($"Unity could not create folder '{folderPath}'.");
            }
        }

        private static string GetParentPath(string assetPath)
        {
            return Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        }

        private sealed class FolderMove
        {
            public FolderMove(string source, string destination)
            {
                Source = source;
                Destination = destination;
            }

            public string Source { get; }
            public string Destination { get; }
        }

        private sealed class AssetMove
        {
            public AssetMove(string source, string destination, string guid)
            {
                Source = source;
                Destination = destination;
                Guid = guid;
            }

            public string Source { get; }
            public string Destination { get; }
            public string Guid { get; }
        }
    }
}
