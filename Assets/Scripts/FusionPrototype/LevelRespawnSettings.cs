using System.Linq;
using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// Scene-local opt-in for authoritative player respawning. Scenes without this
    /// component retain the normal permanent-death behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelRespawnSettings : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float respawnDelaySeconds = 5f;
        [SerializeField] private FusionSpawnPoint[] spawnPoints;

        public float RespawnDelaySeconds => respawnDelaySeconds;

        public bool TryGetSpawnPoint(PlayerRef player, out Vector3 position, out Quaternion rotation)
        {
            ResolveSpawnPoints();
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                position = default;
                rotation = default;
                return false;
            }

            int playerIndex = Mathf.Max(0, player.AsIndex);
            Transform spawn = spawnPoints[playerIndex % spawnPoints.Length].transform;
            position = spawn.position;
            rotation = spawn.rotation;
            return true;
        }

        public void Configure(float delaySeconds)
        {
            respawnDelaySeconds = Mathf.Max(0.1f, delaySeconds);
            ResolveSpawnPoints();
        }

        private void OnValidate()
        {
            respawnDelaySeconds = Mathf.Max(0.1f, respawnDelaySeconds);
            ResolveSpawnPoints();
        }

        private void ResolveSpawnPoints()
        {
            if (spawnPoints != null && spawnPoints.Length > 0 &&
                spawnPoints.All(point => point != null && point.gameObject.scene == gameObject.scene))
                return;

            spawnPoints = FindObjectsByType<FusionSpawnPoint>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(point => point.gameObject.scene == gameObject.scene)
                .OrderBy(point => point.index)
                .ThenBy(point => point.name)
                .ToArray();
        }
    }
}
