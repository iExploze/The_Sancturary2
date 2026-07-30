using System.Collections.Generic;
using Fusion;
using TheSancturary.Monsters;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(InteractionTarget))]
    public sealed class LockerController : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        public enum LockerState : byte
        {
            ClosedEmpty,
            OpenEmpty,
            ClosingOccupied,
            ClosedOccupied,
            OpeningOccupied,
            OpenOccupied
        }

        [Header("Required References")]
        [SerializeField] private InteractionTarget interactionTarget;
        [SerializeField] private Transform door;
        [SerializeField] private Collider insideTrigger;
        [SerializeField] private Transform monsterAttackPoint;
        [SerializeField] private Transform playerRejectPoint;

        [Header("Door")]
        [SerializeField] private Vector3 openDoorLocalEulerAngles;
        [SerializeField] private Vector3 closedDoorLocalEulerAngles = Vector3.zero;
        [SerializeField, Min(0.05f)] private float openingDuration = 0.65f;
        [SerializeField, Min(0.05f)] private float closingDuration = 0.65f;

        [Header("Occupied Feedback")]
        [SerializeField] private AudioClip occupiedInteractionSound;
        [SerializeField] private string occupiedInteractionMessage = "This locker is occupied.";

        [Header("Rejection")]
        [SerializeField, Min(0.05f)] private float rejectionCooldown = 0.5f;

        [Networked] public LockerState CurrentState { get; private set; }
        [Networked] public NetworkBool DoorTargetOpen { get; private set; }
        [Networked] public NetworkBool DoorIsAnimating { get; private set; }
        [Networked] public PlayerRef OccupantPlayer { get; private set; }
        [Networked] public PlayerRef SecondOccupantPlayer { get; private set; }
        [Networked] private TickTimer DoorMotionTimer { get; set; }
        [Networked] private PlayerRef FeedbackPlayer { get; set; }
        [Networked] private ushort FeedbackSequence { get; set; }

        private readonly List<PlayerRef> _playersInside = new();
        private readonly Dictionary<PlayerRef, float> _nextRejectionTime = new();
        private Quaternion _openRotation;
        private Quaternion _closedRotation;
        private ushort _lastPresentedFeedbackSequence;
        private bool _spawned;

        public InteractionTarget PromptTarget => interactionTarget;
        public Transform MonsterAttackPoint => monsterAttackPoint;
        public PlayerRef CurrentOccupant => OccupantPlayer;

        private void Awake()
        {
            ResolveReferences();
            CaptureDoorRotations();
            ApplyDoorRotation(false);
        }

        public override void Spawned()
        {
            ResolveReferences();
            CaptureDoorRotations();

            if (HasStateAuthority)
            {
                CurrentState = LockerState.ClosedEmpty;
                DoorTargetOpen = false;
                DoorIsAnimating = false;
                OccupantPlayer = PlayerRef.None;
                SecondOccupantPlayer = PlayerRef.None;
                DoorMotionTimer = TickTimer.None;
                FeedbackPlayer = PlayerRef.None;
            }

            _spawned = true;
            _lastPresentedFeedbackSequence = FeedbackSequence;
            ApplyDoorRotation(DoorTargetOpen);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
            _playersInside.Clear();
            _nextRejectionTime.Clear();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            for (int index = _playersInside.Count - 1; index >= 0; index--)
            {
                PlayerRef playerRef = _playersInside[index];
                if (TryResolvePlayer(playerRef, out FusionNetworkPlayer player) &&
                    !player.IsDeadOrPending &&
                    player.IsOccupyingLocker(this))
                {
                    continue;
                }

                _playersInside.RemoveAt(index);
                player?.ReleaseLockerAuthoritative(this);
                NotifyMonstersOccupantCleared(playerRef);
            }
            SynchronizeOccupantsAndState();

            if (!DoorIsAnimating || !DoorMotionTimer.ExpiredOrNotRunning(Runner))
                return;

            DoorIsAnimating = false;
            DoorMotionTimer = TickTimer.None;
            SynchronizeOccupantsAndState();
        }

        private void Update()
        {
            if (!_spawned || door == null)
                return;

            Quaternion target = DoorTargetOpen ? _openRotation : _closedRotation;
            float duration = DoorTargetOpen ? openingDuration : closingDuration;
            float angle = Quaternion.Angle(_closedRotation, _openRotation);
            float degreesPerSecond = angle / Mathf.Max(0.05f, duration);
            door.localRotation = Quaternion.RotateTowards(
                door.localRotation,
                target,
                degreesPerSecond * Time.deltaTime);
        }

        public override void Render()
        {
            if (FeedbackSequence == _lastPresentedFeedbackSequence)
                return;

            _lastPresentedFeedbackSequence = FeedbackSequence;
            if (TryResolvePlayer(FeedbackPlayer, out FusionNetworkPlayer player))
            {
                player.PresentLocalInteractionFeedback(
                    occupiedInteractionMessage,
                    occupiedInteractionSound);
            }
        }

        public bool TryGetActionText(
            NetworkPlayerInventory viewerInventory,
            string interactionVerb,
            out string actionText)
        {
            if (!_spawned || door == null)
            {
                actionText = null;
                return false;
            }

            FusionNetworkPlayer viewerPlayer = viewerInventory != null
                ? viewerInventory.GetComponent<FusionNetworkPlayer>()
                : null;
            bool occupiedByAnother =
                (OccupantPlayer != PlayerRef.None || SecondOccupantPlayer != PlayerRef.None) &&
                (viewerPlayer == null || !viewerPlayer.IsOccupyingLocker(this));
            string verb = occupiedByAnother
                ? "Check"
                : DoorTargetOpen
                    ? "Close"
                    : string.IsNullOrWhiteSpace(interactionVerb)
                        ? "Open"
                        : interactionVerb;
            actionText = $"F \u2014 {verb}";
            return true;
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (!_spawned || requestingPlayer == null)
                return false;

            requestingPlayer.RequestInteraction(this);
            return true;
        }

        public bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer)
        {
            if (!HasStateAuthority || requestingPlayer == null || requestingPlayer.IsDeadOrPending)
                return false;

            PlayerRef requestingPlayerRef = requestingPlayer.Object.InputAuthority;
            bool hasPlayersInside = _playersInside.Count > 0;
            bool requesterIsInside = _playersInside.Contains(requestingPlayerRef);
            if (hasPlayersInside && !requesterIsInside)
            {
                FeedbackPlayer = requestingPlayerRef;
                FeedbackSequence++;
                if (FeedbackSequence == 0)
                    FeedbackSequence = 1;
                return false;
            }

            bool open = !DoorTargetOpen;
            if (!open)
                RejectExcessPlayersAuthoritative();

            if (_playersInside.Count > 0)
                SetOccupiedDoorTargetAuthoritative(open);
            else
                SetEmptyDoorTargetAuthoritative(open);
            return true;
        }

        public void NotifyPlayerEnteredAuthoritative(FusionNetworkPlayer player)
        {
            if (!HasStateAuthority || player == null || player.IsDeadOrPending)
                return;

            PlayerRef playerRef = player.Object.InputAuthority;
            if (playerRef == PlayerRef.None || _playersInside.Contains(playerRef))
                return;

            if (!player.TryOccupyLockerAuthoritative(this))
            {
                RejectPlayerAuthoritative(player);
                return;
            }

            _playersInside.Add(playerRef);
            SynchronizeOccupantsAndState();

            GeoMonsterController[] monsters = FindObjectsByType<GeoMonsterController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < monsters.Length; i++)
                monsters[i].TryWitnessLockerEntryAuthoritative(this, player);

            if (!DoorTargetOpen && _playersInside.Count > 2)
                RejectExcessPlayersAuthoritative();
        }

        public void NotifyPlayerExitedAuthoritative(FusionNetworkPlayer player)
        {
            if (!HasStateAuthority || player == null)
                return;

            PlayerRef playerRef = player.Object.InputAuthority;
            if (!_playersInside.Contains(playerRef))
                return;

            ReleasePlayerAuthoritative(player);
        }

        public bool IsPlayerInside(FusionNetworkPlayer player)
        {
            return HasStateAuthority &&
                player != null &&
                _playersInside.Contains(player.Object.InputAuthority);
        }

        public bool IsCurrentOccupant(PlayerRef playerRef)
        {
            return playerRef != PlayerRef.None &&
                (OccupantPlayer == playerRef || SecondOccupantPlayer == playerRef);
        }

        public void ReleasePlayerAuthoritative(FusionNetworkPlayer player)
        {
            if (!HasStateAuthority || player == null)
                return;

            PlayerRef releasedPlayer = player.Object.InputAuthority;
            if (!_playersInside.Remove(releasedPlayer))
                return;

            player.ReleaseLockerAuthoritative(this);
            NotifyMonstersOccupantCleared(releasedPlayer);
            SynchronizeOccupantsAndState();
        }

        public void Configure(
            InteractionTarget configuredInteractionTarget,
            Transform configuredDoor,
            Collider configuredInsideTrigger,
            Transform configuredMonsterAttackPoint,
            Transform configuredPlayerRejectPoint,
            Vector3 configuredOpenDoorLocalEulerAngles,
            Vector3 configuredClosedDoorLocalEulerAngles)
        {
            interactionTarget = configuredInteractionTarget;
            door = configuredDoor;
            insideTrigger = configuredInsideTrigger;
            monsterAttackPoint = configuredMonsterAttackPoint;
            playerRejectPoint = configuredPlayerRejectPoint;
            openDoorLocalEulerAngles = configuredOpenDoorLocalEulerAngles;
            closedDoorLocalEulerAngles = configuredClosedDoorLocalEulerAngles;
            CaptureDoorRotations();
        }

        private void SetEmptyDoorTargetAuthoritative(bool open)
        {
            CurrentState = open ? LockerState.OpenEmpty : LockerState.ClosedEmpty;
            SetDoorTargetAuthoritative(open, open ? openingDuration : closingDuration);
        }

        private void SetOccupiedDoorTargetAuthoritative(bool open)
        {
            CurrentState = open
                ? LockerState.OpeningOccupied
                : LockerState.ClosingOccupied;
            SetDoorTargetAuthoritative(open, open ? openingDuration : closingDuration);
        }

        private void SetDoorTargetAuthoritative(bool open, float duration)
        {
            DoorTargetOpen = open;
            DoorIsAnimating = true;
            DoorMotionTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.05f, duration));
        }

        private void RejectPlayerAuthoritative(
            FusionNetworkPlayer player,
            bool ignoreCooldown = false)
        {
            if (playerRejectPoint == null || player == null)
                return;

            PlayerRef playerRef = player.Object.InputAuthority;
            float now = Time.time;
            if (!ignoreCooldown &&
                _nextRejectionTime.TryGetValue(playerRef, out float nextTime) &&
                now < nextTime)
                return;

            _nextRejectionTime[playerRef] = now + rejectionCooldown;
            player.TeleportAuthoritative(playerRejectPoint.position);
        }

        private void RejectExcessPlayersAuthoritative()
        {
            while (_playersInside.Count > 2)
            {
                int lastIndex = _playersInside.Count - 1;
                PlayerRef rejectedPlayerRef = _playersInside[lastIndex];
                _playersInside.RemoveAt(lastIndex);
                if (TryResolvePlayer(rejectedPlayerRef, out FusionNetworkPlayer rejectedPlayer))
                {
                    rejectedPlayer.ReleaseLockerAuthoritative(this);
                    RejectPlayerAuthoritative(rejectedPlayer, true);
                }

                NotifyMonstersOccupantCleared(rejectedPlayerRef);
            }

            SynchronizeOccupantsAndState();
        }

        private void SynchronizeOccupantsAndState()
        {
            OccupantPlayer = _playersInside.Count > 0
                ? _playersInside[0]
                : PlayerRef.None;
            SecondOccupantPlayer = _playersInside.Count > 1
                ? _playersInside[1]
                : PlayerRef.None;

            bool occupied = _playersInside.Count > 0;
            if (DoorIsAnimating && occupied)
            {
                CurrentState = DoorTargetOpen
                    ? LockerState.OpeningOccupied
                    : LockerState.ClosingOccupied;
                return;
            }

            CurrentState = DoorTargetOpen
                ? occupied ? LockerState.OpenOccupied : LockerState.OpenEmpty
                : occupied ? LockerState.ClosedOccupied : LockerState.ClosedEmpty;
        }

        private void NotifyMonstersOccupantCleared(PlayerRef releasedPlayer)
        {
            GeoMonsterController[] monsters = FindObjectsByType<GeoMonsterController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < monsters.Length; i++)
                monsters[i].CancelWitnessedLockerAuthoritative(this, releasedPlayer);
        }

        private bool TryResolvePlayer(PlayerRef playerRef, out FusionNetworkPlayer player)
        {
            player = null;
            if (playerRef == PlayerRef.None ||
                Runner == null ||
                !Runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject))
            {
                return false;
            }

            player = playerObject.GetComponent<FusionNetworkPlayer>();
            return player != null;
        }

        private void ResolveReferences()
        {
            interactionTarget ??= GetComponent<InteractionTarget>();
            if (insideTrigger == null)
            {
                LockerInsideTrigger relay = GetComponentInChildren<LockerInsideTrigger>(true);
                insideTrigger = relay != null ? relay.GetComponent<Collider>() : null;
            }
        }

        private void CaptureDoorRotations()
        {
            _openRotation = Quaternion.Euler(openDoorLocalEulerAngles);
            _closedRotation = Quaternion.Euler(closedDoorLocalEulerAngles);
        }

        private void ApplyDoorRotation(bool open)
        {
            if (door != null)
                door.localRotation = open ? _openRotation : _closedRotation;
        }

        private void OnDrawGizmosSelected()
        {
            if (monsterAttackPoint != null)
            {
                Gizmos.color = new Color(0.9f, 0.15f, 0.1f, 0.9f);
                Gizmos.DrawWireSphere(monsterAttackPoint.position, 0.18f);
            }

            if (playerRejectPoint != null)
            {
                Gizmos.color = new Color(0.1f, 0.75f, 1f, 0.9f);
                Gizmos.DrawWireSphere(playerRejectPoint.position, 0.18f);
            }
        }
    }
}
