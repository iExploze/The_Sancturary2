using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(InteractionTarget))]
    public sealed class NetworkVentEntrance : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        [Header("Interaction")]
        [SerializeField] private InteractionTarget interactionTarget;
        [SerializeField] private NetworkVentExit correspondingExit;
        [SerializeField] private Transform[] interiorArrivalAnchors;
        [SerializeField] private LayerMask destinationCollisionMask = ~0;

        [Header("Open State")]
        [SerializeField] private bool startsOpen = true;
        [SerializeField] private GameObject openVisual;
        [SerializeField] private GameObject closedCover;
        [SerializeField] private Collider closedBlocker;

        [Header("Audio and Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip enterSound;
        [SerializeField] private AudioClip stateChangeSound;
        [SerializeField] private string blockedMessage = "Vent entrance is blocked.";

        [Networked] public NetworkBool IsOpen { get; private set; }
        [Networked] private PlayerRef FeedbackPlayer { get; set; }
        [Networked] private ushort FeedbackSequence { get; set; }
        [Networked] private ushort TransitionAudioSequence { get; set; }

        private bool _spawned;
        private bool _lastPresentedOpen;
        private ushort _lastPresentedFeedbackSequence;
        private ushort _lastPresentedTransitionAudioSequence;

        public InteractionTarget PromptTarget => interactionTarget;
        public NetworkVentExit CorrespondingExit => correspondingExit;

        private void Awake()
        {
            ResolveReferences();
            ApplyPresentation(startsOpen);
        }

        public override void Spawned()
        {
            ResolveReferences();
            if (HasStateAuthority)
            {
                IsOpen = startsOpen;
                FeedbackPlayer = PlayerRef.None;
            }

            _spawned = true;
            _lastPresentedOpen = IsOpen;
            _lastPresentedFeedbackSequence = FeedbackSequence;
            _lastPresentedTransitionAudioSequence = TransitionAudioSequence;
            ApplyPresentation(IsOpen);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
        }

        public override void Render()
        {
            bool open = IsOpen;
            if (open != _lastPresentedOpen)
            {
                _lastPresentedOpen = open;
                ApplyPresentation(open);
                if (audioSource != null && stateChangeSound != null)
                    audioSource.PlayOneShot(stateChangeSound);
            }

            if (TransitionAudioSequence != _lastPresentedTransitionAudioSequence)
            {
                _lastPresentedTransitionAudioSequence = TransitionAudioSequence;
                if (audioSource != null && enterSound != null)
                    audioSource.PlayOneShot(enterSound);
            }

            if (FeedbackSequence == _lastPresentedFeedbackSequence)
                return;

            _lastPresentedFeedbackSequence = FeedbackSequence;
            if (TryResolveFeedbackPlayer(out FusionNetworkPlayer player))
                player.PresentLocalInteractionFeedback(blockedMessage, null);
        }

        public bool TryGetActionText(NetworkPlayerInventory viewerInventory, string interactionVerb, out string actionText)
        {
            if (!_spawned)
            {
                actionText = null;
                return false;
            }

            actionText = IsOpen ? "F \u2014 Enter" : "Closed";
            return true;
        }

        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (!_spawned || requestingPlayer == null || !requestingPlayer.HasInputAuthority)
                return false;

            requestingPlayer.RequestInteraction(this);
            return true;
        }

        public bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer)
        {
            if (!HasStateAuthority || !IsOpen || requestingPlayer == null ||
                !requestingPlayer.CanEnterVentAuthoritative())
                return false;

            if (!VentDestinationSelector.TrySelectFirstClear(
                    interiorArrivalAnchors,
                    anchor => requestingPlayer.CanOccupyCrouchedPositionAuthoritative(
                        anchor.position,
                        destinationCollisionMask),
                    out Transform destination))
            {
                PresentBlockedFeedbackAuthoritative(requestingPlayer.Object.InputAuthority);
                return false;
            }

            if (!requestingPlayer.TryEnterVentAuthoritative(destination.position, destination.rotation))
                return false;

            TransitionAudioSequence++;
            return true;
        }

        public bool SetOpenAuthoritative(bool open)
        {
            if (!HasStateAuthority)
                return false;

            IsOpen = open;
            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Set Open (State Authority Only)")]
        private void SetOpenFromInspector()
        {
            SetOpenAuthoritative(true);
        }

        [ContextMenu("Set Closed (State Authority Only)")]
        private void SetClosedFromInspector()
        {
            SetOpenAuthoritative(false);
        }
#endif

        public void Configure(
            InteractionTarget configuredInteractionTarget,
            NetworkVentExit configuredCorrespondingExit,
            Transform[] configuredInteriorArrivalAnchors,
            GameObject configuredOpenVisual,
            GameObject configuredClosedCover,
            Collider configuredClosedBlocker,
            AudioSource configuredAudioSource,
            AudioClip configuredEnterSound,
            AudioClip configuredStateChangeSound,
            bool configuredStartsOpen = true)
        {
            interactionTarget = configuredInteractionTarget;
            correspondingExit = configuredCorrespondingExit;
            interiorArrivalAnchors = configuredInteriorArrivalAnchors;
            openVisual = configuredOpenVisual;
            closedCover = configuredClosedCover;
            closedBlocker = configuredClosedBlocker;
            audioSource = configuredAudioSource;
            enterSound = configuredEnterSound;
            stateChangeSound = configuredStateChangeSound;
            startsOpen = configuredStartsOpen;
            ApplyPresentation(startsOpen);
        }

        private void ResolveReferences()
        {
            interactionTarget ??= GetComponent<InteractionTarget>();
            audioSource ??= GetComponent<AudioSource>();
        }

        private void ApplyPresentation(bool open)
        {
            if (openVisual != null)
                openVisual.SetActive(open);
            if (closedCover != null)
                closedCover.SetActive(!open);
            if (closedBlocker != null)
                closedBlocker.enabled = !open;
            correspondingExit?.ApplyLinkedPresentation(open);
        }

        private void PresentBlockedFeedbackAuthoritative(PlayerRef player)
        {
            FeedbackPlayer = player;
            FeedbackSequence++;
            if (FeedbackSequence == 0)
                FeedbackSequence = 1;
        }

        private bool TryResolveFeedbackPlayer(out FusionNetworkPlayer player)
        {
            player = null;
            return FeedbackPlayer != PlayerRef.None && Runner != null &&
                   Runner.TryGetPlayerObject(FeedbackPlayer, out NetworkObject playerObject) &&
                   (player = playerObject.GetComponent<FusionNetworkPlayer>()) != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            if (!Application.isPlaying)
                ApplyPresentation(startsOpen);
        }
#endif
    }
}
