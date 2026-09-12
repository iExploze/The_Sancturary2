using Fusion;
using TheSancturary.Inventory;
using UnityEngine;
using UnityEngine.AI;

namespace TheSancturary.Monsters
{
    public struct MonsterContactEvent : INetworkStruct
    {
        public ushort Sequence;
        public Vector3 Point;
        public Vector3 Normal;
        public byte Kind;
    }

    /// <summary>Persistent health/incapacitation; accepted hits are resolved only by state authority.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class MonsterCombatState : NetworkBehaviour
    {
        [SerializeField] private ItemCombatSettings settings;
        [SerializeField] private bool isHenry;
        [Networked] public int Health { get; private set; }
        [Networked] public NetworkBool IsDead { get; private set; }
        [Networked] public TickTimer SleepTimer { get; private set; }
        [Networked] private TickTimer RemovalTimer { get; set; }
        private const int ContactCapacity = 16;
        [Networked, Capacity(ContactCapacity)] private NetworkArray<MonsterContactEvent> Contacts => default;
        [Networked] private ushort ContactSequence { get; set; }
        private ushort _presentedContactSequence;
        private bool _presentedDeathEffect;
        public int MaximumHealth => settings == null ? 1 : isHenry ? settings.HenryHealth : settings.GeoHealth;
        public bool IsSleeping => !IsDead && Runner != null && !SleepTimer.ExpiredOrNotRunning(Runner);
        public bool IsIncapacitated => IsDead || IsSleeping;
        public float SleepRemainingSeconds => Runner != null ? SleepTimer.RemainingTime(Runner) ?? 0f : 0f;
        private Animator _animator;
        private Quaternion _livingRotation;
        private Vector3 _livingPosition;
        private bool _presentedSuppression;
        private ParticleSystem _sleepParticles;

        public override void Spawned()
        {
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _livingRotation = _animator.transform.localRotation;
                _livingPosition = _animator.transform.localPosition;
            }
            if (HasStateAuthority) Health = MaximumHealth;
            _presentedContactSequence = ContactSequence;
            _presentedDeathEffect = IsDead;
            ApplyPresentation();
        }

        public bool TryReceiveHitAuthoritative(NetworkItemUseController source,
            InventoryItemDefinition item, Vector3 point, Vector3 normal)
        {
            if (!HasStateAuthority || IsDead || settings == null || source == null ||
                source.Runner != Runner || !source.HasStateAuthority || item == null ||
                item.CombatSettings != settings) return false;
            bool dart = item.ItemId == "tranq_gun";
            if (dart)
                SleepTimer = TickTimer.CreateFromSeconds(Runner, settings.sleepSeconds);
            else
            {
                bool shotgun = item.ItemId == "sawed_off_shotgun";
                Health = ItemCombatRules.DamageResult(Health,
                    shotgun ? settings.shotgunDamage : settings.normalHitDamage,
                    shotgun && settings.shotgunInstantKill);
                if (Health == 0)
                {
                    IsDead = true;
                    SleepTimer = TickTimer.None;
                    RemovalTimer = TickTimer.CreateFromSeconds(Runner, settings.deathVisibleSeconds);
                    foreach (Collider body in GetComponentsInChildren<Collider>(true)) body.enabled = false;
                }
            }
            if (IsIncapacitated) StopNavigation();
            ushort next = unchecked((ushort)(ContactSequence + 1));
            Contacts.Set(next % ContactCapacity, new MonsterContactEvent
            {
                Sequence = next, Point = point, Normal = normal, Kind = (byte)(IsDead ? 2 : dart ? 1 : 0)
            });
            ContactSequence = next;
            return true;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (IsDead && RemovalTimer.Expired(Runner)) Runner.Despawn(Object);
            else if (SleepTimer.Expired(Runner)) SleepTimer = TickTimer.None;
        }

        public override void Render()
        {
            int count = Mathf.Min((ushort)(ContactSequence - _presentedContactSequence), ContactCapacity);
            for (int remaining = count - 1; remaining >= 0; remaining--)
            {
                ushort sequence = unchecked((ushort)(ContactSequence - remaining));
                MonsterContactEvent contact = Contacts.Get(sequence % ContactCapacity);
                if (contact.Sequence == sequence) PresentContact(contact.Point, contact.Normal, contact.Kind);
            }
            _presentedContactSequence = ContactSequence;
            // A severely delayed peer still sees one death burst when it receives the terminal state.
            if (IsDead && !_presentedDeathEffect) PresentContact(transform.position + Vector3.up, Vector3.up, 2);
            ApplyPresentation();
        }

        private void StopNavigation()
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        private void ApplyPresentation()
        {
            bool suppressed = IsIncapacitated;
            if (suppressed && !_presentedSuppression)
                foreach (AudioSource source in GetComponentsInChildren<AudioSource>(true)) source.Stop();
            if (_animator != null)
            {
                _animator.enabled = !suppressed;
                float blend = 1f - Mathf.Exp(-Time.deltaTime * 8f);
                _animator.transform.localRotation = Quaternion.Slerp(_animator.transform.localRotation,
                    _livingRotation * (IsSleeping ? Quaternion.Euler(0, 0, 78) : Quaternion.identity), blend);
                _animator.transform.localPosition = Vector3.Lerp(_animator.transform.localPosition,
                    _livingPosition + (IsSleeping ? new Vector3(0, -0.45f, 0) : Vector3.zero), blend);
            }
            if (IsDead)
            {
                foreach (Renderer body in GetComponentsInChildren<Renderer>(true)) body.enabled = false;
                foreach (Collider body in GetComponentsInChildren<Collider>(true)) body.enabled = false;
            }
            if (IsSleeping && _sleepParticles == null && settings != null && settings.sleepEffect != null)
            {
                _sleepParticles = Instantiate(settings.sleepEffect, transform.position + Vector3.up, Quaternion.identity, transform);
                _sleepParticles.Play();
            }
            if (!IsSleeping && _sleepParticles != null) Destroy(_sleepParticles.gameObject);
            _presentedSuppression = suppressed;
        }

        private void PresentContact(Vector3 point, Vector3 normal, byte kind)
        {
            if (settings == null) return;
            if (kind == 2)
            {
                if (_presentedDeathEffect) return;
                _presentedDeathEffect = true;
            }
            ParticleSystem effect = kind == 2 ? settings.deathEffect : kind == 0 ? settings.hitEffect : null;
            if (effect != null)
            {
                ParticleSystem burst = Instantiate(effect, point,
                    Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up));
                burst.Play();
                Destroy(burst.gameObject, 6f);
            }
            AudioClip clip = kind == 2 ? settings.deathBurst : kind == 1 ? settings.dartImpact :
                settings.monsterImpacts != null && settings.monsterImpacts.Length > 0
                    ? settings.monsterImpacts[Random.Range(0, settings.monsterImpacts.Length)] : null;
            if (clip != null)
            {
                GameObject voice = new GameObject("Monster contact audio");
                voice.transform.position = point;
                AudioSource audio = voice.AddComponent<AudioSource>();
                audio.outputAudioMixerGroup = settings.effectsMixer;
                audio.spatialBlend = 1;
                audio.minDistance = 2;
                audio.maxDistance = 28;
                audio.rolloffMode = AudioRolloffMode.Linear;
                audio.PlayOneShot(clip);
                Destroy(voice, clip.length + 0.2f);
            }
        }
    }
}
