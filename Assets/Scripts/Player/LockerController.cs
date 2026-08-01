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
            ClosedFree,
            EnteringOpening,
            EnteringClosing,
            Occupied,
            ExitingOpening,
            ExitingClosing,
            MonsterOpening,
            MonsterClosing
        }

        [Header("Required References")]
        [SerializeField] private InteractionTarget externalInteractionTarget;
        [SerializeField] private Transform door;
        [SerializeField] private Transform playerViewAnchor;
        [SerializeField] private Transform playerHiddenStorageAnchor;
        [SerializeField] private Transform playerExitPoint;
        [SerializeField] private Transform monsterInteractionPoint;

        [Header("Door")]
        [SerializeField] private Vector3 openDoorLocalEulerAngles;
        [SerializeField] private Vector3 closedDoorLocalEulerAngles = Vector3.zero;
        [SerializeField, Min(0.05f)] private float openingDuration = 0.25f;
        [SerializeField, Min(0.05f)] private float closingDuration = 0.25f;

        [Header("Door Audio")]
        [SerializeField] private AudioSource doorAudioSource;
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip doorCloseSound;
        [SerializeField, Range(0f, 1f)] private float doorAudioVolume = 0.85f;

        [Header("Occupied Feedback")]
        [SerializeField] private AudioClip occupiedInteractionSound;
        [SerializeField] private string occupiedInteractionMessage = "This locker is occupied.";

        [Networked] public LockerState CurrentState { get; private set; }
        [Networked] public NetworkBool DoorTargetOpen { get; private set; }
        [Networked] public NetworkBool DoorIsAnimating { get; private set; }
        [Networked] public PlayerRef OccupantPlayer { get; private set; }
        [Networked] private TickTimer DoorMotionTimer { get; set; }
        [Networked] private PlayerRef FeedbackPlayer { get; set; }
        [Networked] private ushort FeedbackSequence { get; set; }

        private Quaternion _openRotation;
        private Quaternion _closedRotation;
        private ushort _lastPresentedFeedbackSequence;
        private bool _lastPresentedDoorTargetOpen;
        private bool _spawned;

        public InteractionTarget PromptTarget => externalInteractionTarget;
        public Transform PlayerViewAnchor => playerViewAnchor;
        public Transform MonsterInteractionPoint => monsterInteractionPoint;
        public PlayerRef CurrentOccupant => OccupantPlayer;
        public bool IsTransitioning => CurrentState != LockerState.ClosedFree && CurrentState != LockerState.Occupied;

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
                CurrentState = LockerState.ClosedFree;
                DoorTargetOpen = false;
                DoorIsAnimating = false;
                OccupantPlayer = PlayerRef.None;
                DoorMotionTimer = TickTimer.None;
                FeedbackPlayer = PlayerRef.None;
            }

            _spawned = true;
            _lastPresentedFeedbackSequence = FeedbackSequence;
            _lastPresentedDoorTargetOpen = DoorTargetOpen;
            ApplyDoorRotation(DoorTargetOpen);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (hasState && HasStateAuthority && TryResolveOccupant(out FusionNetworkPlayer player))
                player.ClearLockerStateAuthoritative(this);
            _spawned = false;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (OccupantPlayer != PlayerRef.None &&
                (!TryResolveOccupant(out FusionNetworkPlayer occupant) ||
                 occupant.IsDeadOrPending ||
                 !occupant.IsUsingLocker(this)))
            {
                ReleaseInvalidOccupantAuthoritative(occupant);
            }

            if (!DoorIsAnimating || !DoorMotionTimer.ExpiredOrNotRunning(Runner))
                return;

            DoorIsAnimating = false;
            DoorMotionTimer = TickTimer.None;
            AdvanceDoorTransitionAuthoritative();
        }

        private void Update()
        {
            if (!_spawned || door == null)
                return;

            Quaternion target = DoorTargetOpen ? _openRotation : _closedRotation;
            float duration = DoorTargetOpen ? openingDuration : closingDuration;
            float degreesPerSecond = Quaternion.Angle(_closedRotation, _openRotation) / Mathf.Max(0.05f, duration);
            door.localRotation = Quaternion.RotateTowards(door.localRotation, target, degreesPerSecond * Time.deltaTime);
        }

        public override void Render()
        {
            if (DoorTargetOpen != _lastPresentedDoorTargetOpen)
            {
                _lastPresentedDoorTargetOpen = DoorTargetOpen;
                PlayDoorSound(DoorTargetOpen);
            }

            if (FeedbackSequence == _lastPresentedFeedbackSequence)
                return;

            _lastPresentedFeedbackSequence = FeedbackSequence;
            if (TryResolvePlayer(FeedbackPlayer, out FusionNetworkPlayer player))
                player.PresentLocalInteractionFeedback(occupiedInteractionMessage, occupiedInteractionSound);
        }

        public bool TryGetActionText(NetworkPlayerInventory viewerInventory, string interactionVerb, out string actionText)
        {
            FusionNetworkPlayer viewer = viewerInventory != null ? viewerInventory.GetComponent<FusionNetworkPlayer>() : null;
            if (viewer != null && viewer.IsUsingLocker(this))
            {
                actionText = CurrentState == LockerState.Occupied ? "F — Leave" : "Occupied";
                return true;
            }

            actionText = CurrentState == LockerState.ClosedFree ? "F — Hide" : "Occupied";
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
            if (!HasStateAuthority || requestingPlayer == null || requestingPlayer.IsDeadOrPending ||
                requestingPlayer.Object.InputAuthority == PlayerRef.None)
                return false;

            PlayerRef requester = requestingPlayer.Object.InputAuthority;
            if (OccupantPlayer == requester)
            {
                if (CurrentState != LockerState.Occupied || !requestingPlayer.IsHiddenInLocker)
                    return false;

                CurrentState = LockerState.ExitingOpening;
                SetDoorTargetAuthoritative(true, openingDuration);
                return true;
            }

            if (CurrentState != LockerState.ClosedFree || OccupantPlayer != PlayerRef.None ||
                !requestingPlayer.TryReserveLockerAuthoritative(this))
            {
                PresentOccupiedFeedbackAuthoritative(requester);
                return false;
            }

            // This happens while the player is still visible at the external interaction point.
            OccupantPlayer = requester;
            CurrentState = LockerState.EnteringOpening;
            SetDoorTargetAuthoritative(true, openingDuration);
            NotifyMonstersEntryAccepted(requestingPlayer);
            return true;
        }

        public bool IsCurrentOccupant(PlayerRef playerRef)
        {
            return playerRef != PlayerRef.None && OccupantPlayer == playerRef;
        }

        public bool IsOccupying(FusionNetworkPlayer player)
        {
            return player != null && IsCurrentOccupant(player.Object.InputAuthority) && player.IsUsingLocker(this);
        }

        public bool BeginMonsterEjectAuthoritative(FusionNetworkPlayer player)
        {
            if (!HasStateAuthority || player == null || CurrentState != LockerState.Occupied || !IsOccupying(player))
                return false;

            CurrentState = LockerState.MonsterOpening;
            SetDoorTargetAuthoritative(true, openingDuration);
            return true;
        }

        public void ReleaseInvalidOccupantAuthoritative(FusionNetworkPlayer player)
        {
            if (!HasStateAuthority)
                return;

            PlayerRef released = OccupantPlayer;
            if (player != null)
            {
                if (playerExitPoint != null)
                    player.FinishLeavingLockerAuthoritative(this, playerExitPoint.position, playerExitPoint.rotation);
                else
                    player.ClearLockerStateAuthoritative(this);
            }
            OccupantPlayer = PlayerRef.None;
            NotifyMonstersOccupantCleared(released);
            CurrentState = LockerState.ExitingClosing;
            SetDoorTargetAuthoritative(false, closingDuration);
        }

        private void AdvanceDoorTransitionAuthoritative()
        {
            switch (CurrentState)
            {
                case LockerState.EnteringOpening:
                    if (!TryResolveOccupant(out FusionNetworkPlayer entering) || playerHiddenStorageAnchor == null)
                    {
                        ReleaseInvalidOccupantAuthoritative(entering);
                        return;
                    }

                    entering.FinishEnteringLockerAuthoritative(this, playerHiddenStorageAnchor.position);
                    CurrentState = LockerState.EnteringClosing;
                    SetDoorTargetAuthoritative(false, closingDuration);
                    break;
                case LockerState.EnteringClosing:
                    CurrentState = LockerState.Occupied;
                    break;
                case LockerState.ExitingOpening:
                    if (TryResolveOccupant(out FusionNetworkPlayer exiting) && playerExitPoint != null)
                        exiting.FinishLeavingLockerAuthoritative(this, playerExitPoint.position, playerExitPoint.rotation);
                    ClearOccupantAndCloseAuthoritative(LockerState.ExitingClosing);
                    break;
                case LockerState.ExitingClosing:
                case LockerState.MonsterClosing:
                    CurrentState = LockerState.ClosedFree;
                    break;
                case LockerState.MonsterOpening:
                    if (TryResolveOccupant(out FusionNetworkPlayer ejected) && playerExitPoint != null)
                    {
                        ejected.ForceEjectFromLockerAuthoritative(this, playerExitPoint.position, playerExitPoint.rotation);
                        NotifyMonstersLockerEjectionKilled(ejected);
                    }
                    ClearOccupantAndCloseAuthoritative(LockerState.MonsterClosing);
                    break;
            }
        }

        private void ClearOccupantAndCloseAuthoritative(LockerState closingState)
        {
            PlayerRef released = OccupantPlayer;
            OccupantPlayer = PlayerRef.None;
            NotifyMonstersOccupantCleared(released);
            CurrentState = closingState;
            SetDoorTargetAuthoritative(false, closingDuration);
        }

        private void SetDoorTargetAuthoritative(bool open, float duration)
        {
            DoorTargetOpen = open;
            DoorIsAnimating = true;
            DoorMotionTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.05f, duration));
        }

        private void PresentOccupiedFeedbackAuthoritative(PlayerRef player)
        {
            FeedbackPlayer = player;
            FeedbackSequence++;
            if (FeedbackSequence == 0)
                FeedbackSequence = 1;
        }

        private void NotifyMonstersEntryAccepted(FusionNetworkPlayer player)
        {
            foreach (GeoMonsterController monster in FindObjectsByType<GeoMonsterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                monster.TryWitnessLockerEntryAuthoritative(this, player);
        }

        private void NotifyMonstersOccupantCleared(PlayerRef player)
        {
            if (player == PlayerRef.None)
                return;

            foreach (GeoMonsterController monster in FindObjectsByType<GeoMonsterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                monster.CancelWitnessedLockerAuthoritative(this, player);
        }

        private void NotifyMonstersLockerEjectionKilled(FusionNetworkPlayer player)
        {
            foreach (GeoMonsterController monster in FindObjectsByType<GeoMonsterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                monster.TriggerLockerEjectionJumpscareAuthoritative(this, player);
        }

        private bool TryResolveOccupant(out FusionNetworkPlayer player)
        {
            return TryResolvePlayer(OccupantPlayer, out player);
        }

        private bool TryResolvePlayer(PlayerRef playerRef, out FusionNetworkPlayer player)
        {
            player = null;
            if (playerRef == PlayerRef.None || Runner == null || !Runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject))
                return false;

            player = playerObject.GetComponent<FusionNetworkPlayer>();
            return player != null;
        }

        private void ResolveReferences()
        {
            externalInteractionTarget ??= GetComponent<InteractionTarget>();
            doorAudioSource ??= GetComponent<AudioSource>();
        }

        private void PlayDoorSound(bool opening)
        {
            AudioClip sound = opening ? doorOpenSound : doorCloseSound;
            if (doorAudioSource != null && sound != null)
                doorAudioSource.PlayOneShot(sound, doorAudioVolume);
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
            DrawAnchorGizmo(monsterInteractionPoint, new Color(0.9f, 0.15f, 0.1f, 0.9f));
            DrawAnchorGizmo(playerExitPoint, new Color(0.1f, 0.75f, 1f, 0.9f));
            DrawAnchorGizmo(playerHiddenStorageAnchor, new Color(0.8f, 0.35f, 1f, 0.9f));
        }

        private static void DrawAnchorGizmo(Transform anchor, Color color)
        {
            if (anchor == null)
                return;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(anchor.position, 0.18f);
            Gizmos.DrawLine(anchor.position, anchor.position + anchor.forward * 0.35f);
        }
    }
}
