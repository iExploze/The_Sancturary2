using Unity.Netcode;
using UnityEngine;

namespace TheSancturary.Monsters
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class MonsterSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject monsterPrefab;
        [SerializeField] private Transform spawnPoint;

        private NetworkObject spawnedMonster;

        public override void OnNetworkSpawn()
        {
            if (!IsServer || spawnedMonster != null)
            {
                return;
            }

            spawnedMonster = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedMonster.Spawn(true);
        }
    }
}
