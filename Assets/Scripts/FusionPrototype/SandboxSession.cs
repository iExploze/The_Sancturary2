using System.Collections.Generic;
using System.Linq;
using Fusion;
using TheSancturary.Inventory;
using TheSancturary.Monsters;
using UnityEngine;
using UnityEngine.AI;

namespace TheSancturary.FusionPrototype
{
    /// <summary>Opt-in, scene-owned sandbox controls. No instance exists in campaign scenes.</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class SandboxSession : NetworkBehaviour
    {
        [SerializeField] private Bounds protectedRegion = new(new Vector3(-10, 0, -5.5f), new Vector3(23, 100, 19));
        [SerializeField] private NetworkObject[] monsterPrefabs;
        [SerializeField] private Transform[] monsterSpawns;
        [SerializeField] private Transform[] patrolWaypoints;
        [SerializeField] private WorldInventoryItem[] equipment;
        [SerializeField] private NetworkKeyPickup[] keys;
        [SerializeField] private NetworkToolObstacle[] toolTargets;
        [SerializeField] private TextMesh statusDisplay;
        [SerializeField] private Shader labelShader;
        [Networked] public byte SelectedMonster { get; private set; }
        [Networked] public NetworkId ActiveMonster { get; private set; }
        [Networked] public ushort RestockRevision { get; private set; }
        [Networked] private TickTimer ControlCooldown { get; set; }
        [Networked] private NetworkString<_128> Status { get; set; }
        private static readonly List<SandboxSession> Sessions = new();
        private readonly Dictionary<NetworkBehaviour, Pose> _supplyPoses = new();
        private Material _labelMaterial;
        private readonly List<TextMesh> _facingLabels = new();

        private void Awake()
        {
            foreach (NetworkBehaviour item in equipment.Cast<NetworkBehaviour>().Concat(keys))
                if (item != null) _supplyPoses[item] = new Pose(item.transform.position, item.transform.rotation);
        }

        public override void Spawned()
        {
            Sessions.Add(this);
            if (statusDisplay == null || labelShader == null) return;
            Font font = statusDisplay.font;
            _labelMaterial = new Material(labelShader);
            foreach (TextMesh text in FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.gameObject.scene == gameObject.scene && text.font == font)
                {
                    text.GetComponent<MeshRenderer>().sharedMaterial = _labelMaterial;
                    if (text.name.EndsWith(" label") || text.name.EndsWith(" group sign")) _facingLabels.Add(text);
                }
        }
        public override void Despawned(NetworkRunner runner, bool hasState) => Sessions.Remove(this);
        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && ActiveMonster.IsValid && !Runner.TryFindObject(ActiveMonster, out _))
            {
                ActiveMonster = default;
                Status = "Monster defeated. Reset encounter for another test.";
            }
        }
        private void OnDestroy()
        {
            Sessions.Remove(this);
            if (_labelMaterial != null) Destroy(_labelMaterial);
        }

        public override void Render()
        {
            // Dynamic font atlases can be rebuilt after new status text is requested.
            if (_labelMaterial != null && statusDisplay != null)
                _labelMaterial.mainTexture = statusDisplay.font.material.mainTexture;
            Camera camera = Camera.main;
            if (camera != null)
                foreach (TextMesh label in _facingLabels)
                {
                    Vector3 direction = Vector3.ProjectOnPlane(label.transform.position - camera.transform.position, Vector3.up);
                    if (direction.sqrMagnitude > 0.001f) label.transform.rotation = Quaternion.LookRotation(direction);
                }
            if (statusDisplay != null)
            {
                string encounter = "NONE";
                if (ActiveMonster.IsValid && Runner.TryFindObject(ActiveMonster, out NetworkObject monster))
                {
                    MonsterCombatState combat = monster.GetComponent<MonsterCombatState>();
                    encounter = combat != null
                        ? $"{combat.Health}/{combat.MaximumHealth} HP" + (combat.IsSleeping ? $" | SLEEP {combat.SleepRemainingSeconds:0}s" : "")
                        : "ACTIVE";
                }
                statusDisplay.text = $"MONSTER: {MonsterName} | {encounter}\n{Status}";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LaunchRequestedSandbox()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sandbox") >= 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/SandboxLaunch.unity");
        }

        public static SandboxSession For(NetworkBehaviour actor) => actor != null && actor.Object != null
            ? Sessions.FirstOrDefault(s => s != null && s.Object != null && s.Object.IsValid && s.Runner == actor.Runner)
            : null;
        public static bool IsActiveFor(NetworkBehaviour actor) => For(actor) != null;
        public static bool IsProtected(FusionNetworkPlayer player) =>
            player != null && For(player) is SandboxSession session && session.Contains(player.transform.position);
        public bool Contains(Vector3 position) => protectedRegion.Contains(position);

        // Movement is also checked explicitly because the existing controllers integrate
        // their desired velocity themselves; NavMesh exclusion alone is insufficient.
        public static Vector3 ConstrainMonsterStep(NetworkBehaviour monster, Vector3 from, Vector3 to, float radius)
        {
            SandboxSession session = For(monster);
            if (session == null) return to;
            Bounds bounds = session.protectedRegion;
            bounds.Expand(new Vector3(radius * 2, 0, radius * 2));
            Vector3 delta = to - from;
            if (bounds.Contains(to) || (delta.sqrMagnitude > 0 &&
                bounds.IntersectRay(new Ray(from, delta.normalized), out float distance) && distance <= delta.magnitude))
                return from;
            return to;
        }

        public string MonsterName => monsterPrefabs != null && SelectedMonster < monsterPrefabs.Length
            ? monsterPrefabs[SelectedMonster].name : "Unavailable";

        public bool Execute(SandboxControl.Action action, FusionNetworkPlayer actor)
        {
            if (!HasStateAuthority || actor == null || actor.Runner != Runner || !actor.HasStateAuthority ||
                actor.IsDeadOrPending || actor.IsHiddenInLocker || !Contains(actor.transform.position)) return false;
            // Damage controls affect only the authenticated interacting player.
            if (action == SandboxControl.Action.HurtSelf) { actor.TakeDamage(50); return true; }
            if (action == SandboxControl.Action.DownSelf) { actor.TakeDamage(10000); return true; }
            if (!ControlCooldown.ExpiredOrNotRunning(Runner)) return false;
            ControlCooldown = TickTimer.CreateFromSeconds(Runner, 0.5f);
            switch (action)
            {
                case SandboxControl.Action.SelectMonster:
                    if (monsterPrefabs == null || monsterPrefabs.Length == 0) return false;
                    SelectedMonster = (byte)((SelectedMonster + 1) % monsterPrefabs.Length); return true;
                case SandboxControl.Action.SpawnMonster: return SpawnMonster();
                case SandboxControl.Action.DespawnMonster: DespawnMonster(); Status = "Encounter cleared."; return true;
                case SandboxControl.Action.ResetEncounter:
                    DespawnMonster(); return SpawnMonster();
                case SandboxControl.Action.Restock: Restock(); return true;
                default: return false;
            }
        }

        private bool SpawnMonster()
        {
            if (ActiveMonster.IsValid && Runner.TryFindObject(ActiveMonster, out _)) { Status = "Despawn or reset the active monster first."; return false; }
            if (monsterPrefabs == null || SelectedMonster >= monsterPrefabs.Length || monsterSpawns == null) return false;
            foreach (Transform point in monsterSpawns)
            {
                if (point == null || Contains(point.position) ||
                    !NavMesh.SamplePosition(point.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas) || Contains(hit.position)) continue;
                bool occupied = Runner.ActivePlayers.Any(p => Runner.TryGetPlayerObject(p, out NetworkObject player) &&
                    Vector3.Distance(player.transform.position, hit.position) < 6f);
                if (occupied) continue;
                NetworkObject spawned = Runner.Spawn(monsterPrefabs[SelectedMonster], hit.position, point.rotation);
                if (spawned == null) return false;
                ActiveMonster = spawned.Id;
                spawned.GetComponent<GeoMonsterController>()?.ConfigureSandboxPatrol(patrolWaypoints);
                spawned.GetComponent<HenryMonsterController>()?.ConfigureSandboxPatrol(patrolWaypoints);
                Status = "Monster spawned in danger zone.";
                return true;
            }
            Status = "Spawn blocked: move away from danger reset points.";
            return false;
        }

        private void DespawnMonster()
        {
            if (ActiveMonster.IsValid && Runner.TryFindObject(ActiveMonster, out NetworkObject monster)) Runner.Despawn(monster);
            ActiveMonster = default;
        }

        private void Restock()
        {
            // Recover all dropped sandbox equipment through Fusion. Held items stay owned.
            foreach (WorldInventoryItem item in FindObjectsByType<WorldInventoryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (item.Object != null && item.Object.IsValid && item.Runner == Runner && !item.Object.NetworkTypeId.IsSceneObject)
                    Runner.Despawn(item.Object);
            foreach (NetworkKeyPickup key in FindObjectsByType<NetworkKeyPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (key.Object != null && key.Object.IsValid && key.Runner == Runner && !key.Object.NetworkTypeId.IsSceneObject)
                    Runner.Despawn(key.Object);
            Dictionary<string, int> held = new();
            foreach (PlayerRef reference in Runner.ActivePlayers)
            {
                if (!Runner.TryGetPlayerObject(reference, out NetworkObject player)) continue;
                NetworkPlayerInventory inventory = player.GetComponent<NetworkPlayerInventory>();
                if (inventory == null) continue;
                foreach (NetworkInventoryEntry entry in inventory.Entries)
                    if (entry.IsOccupied) { string id = entry.ItemId.ToString(); held[id] = held.GetValueOrDefault(id) + 1; }
            }
            foreach (var group in equipment.Where(i => i != null).GroupBy(i => i.ItemId))
            {
                int available = Mathf.Max(0, group.Count() - held.GetValueOrDefault(group.Key));
                foreach (WorldInventoryItem item in group) item.ResetSandboxSupply(available-- > 0, _supplyPoses[item]);
            }
            foreach (NetworkKeyPickup key in keys)
                if (key != null) key.ResetSandboxSupply(!held.ContainsKey(key.ItemId), _supplyPoses[key]);
            foreach (NetworkToolObstacle target in toolTargets) if (target != null) target.ResetSandboxTarget();
            foreach (NetworkLockGroup target in FindObjectsByType<NetworkLockGroup>(FindObjectsSortMode.None))
                if (target.Object != null && target.Object.IsValid && target.Runner == Runner) target.ResetSandboxLock();
            RestockRevision++;
            Status = "Supplies, tool targets and locks reset. Held equipment preserved.";
        }
    }
}
