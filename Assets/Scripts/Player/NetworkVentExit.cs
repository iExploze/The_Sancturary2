using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(InteractionTarget))]
    public sealed class NetworkVentExit : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        [Header("Interaction")]
        [SerializeField] private InteractionTarget interactionTarget;
        [SerializeField] private NetworkVentEntrance linkedEntrance;
        [SerializeField] private Transform[] exteriorArrivalAnchors;
        [SerializeField] private LayerMask destinationCollisionMask = ~0;

        [Header("Audio and Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip exitSound;
        [SerializeField] private string blockedMessage = "Vent exit is blocked.";

        [Header("Open State Presentation")]
        [SerializeField] private GameObject openVisual;
        [SerializeField] private GameObject closedCover;

        [Header("Owner-Local Exit Indicator")]
        [SerializeField] private GameObject exitIndicatorRoot;
        [SerializeField] private TextMesh exitIndicatorLabel;
        [SerializeField] private Renderer[] exitIndicatorRenderers;
        [SerializeField] private Light exitIndicatorLight;
        [SerializeField] private string exitLabel = "EXIT";
        [SerializeField] private Color openIndicatorColor = new(0.55f, 1f, 0.72f, 1f);
        [SerializeField] private Color closedIndicatorColor = new(1f, 0.2f, 0.12f, 1f);
        [SerializeField, Min(0f)] private float openIndicatorIntensity = 0.55f;
        [SerializeField, Min(0f)] private float closedIndicatorIntensity = 0.16f;

        [Networked] private PlayerRef FeedbackPlayer { get; set; }
        [Networked] private ushort FeedbackSequence { get; set; }
        [Networked] private ushort TransitionAudioSequence { get; set; }

        private bool _spawned;
        private ushort _lastPresentedFeedbackSequence;
        private ushort _lastPresentedTransitionAudioSequence;
        private bool _lastPresentedOpen;
        private bool _lastIndicatorVisible;
        private MaterialPropertyBlock _indicatorPropertyBlock;

        public InteractionTarget PromptTarget => interactionTarget;
        public NetworkVentEntrance LinkedEntrance => linkedEntrance;

        private void Awake()
        {
            _indicatorPropertyBlock = new MaterialPropertyBlock();
            ResolveReferences();
        }

        public override void Spawned()
        {
            ResolveReferences();
            if (HasStateAuthority)
                FeedbackPlayer = PlayerRef.None;
            _spawned = true;
            _lastPresentedFeedbackSequence = FeedbackSequence;
            _lastPresentedTransitionAudioSequence = TransitionAudioSequence;
            _lastPresentedOpen = linkedEntrance != null && linkedEntrance.IsOpen;
            ApplyPresentation(_lastPresentedOpen);
            SetIndicatorVisible(false);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
            SetIndicatorVisible(false);
        }

        public override void Render()
        {
            bool open = linkedEntrance != null && linkedEntrance.IsOpen;
            if (open != _lastPresentedOpen)
            {
                _lastPresentedOpen = open;
                ApplyPresentation(open);
            }

            SetIndicatorVisible(ShouldShowExitIndicator());

            if (TransitionAudioSequence != _lastPresentedTransitionAudioSequence)
            {
                _lastPresentedTransitionAudioSequence = TransitionAudioSequence;
                if (audioSource != null && exitSound != null)
                    audioSource.PlayOneShot(exitSound);
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

            actionText = linkedEntrance != null && linkedEntrance.IsOpen
                ? "F \u2014 Exit"
                : "Closed";
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
            if (!HasStateAuthority || linkedEntrance == null || !linkedEntrance.IsOpen ||
                requestingPlayer == null || !requestingPlayer.CanExitVentAuthoritative())
                return false;

            if (!VentDestinationSelector.TrySelectFirstClear(
                    exteriorArrivalAnchors,
                    anchor => requestingPlayer.CanOccupyCrouchedPositionAuthoritative(
                        anchor.position,
                        destinationCollisionMask),
                    out Transform destination))
            {
                PresentBlockedFeedbackAuthoritative(requestingPlayer.Object.InputAuthority);
                return false;
            }

            if (!requestingPlayer.TryExitVentAuthoritative(destination.position, destination.rotation))
                return false;

            TransitionAudioSequence++;
            return true;
        }

        public void Configure(
            InteractionTarget configuredInteractionTarget,
            NetworkVentEntrance configuredLinkedEntrance,
            Transform[] configuredExteriorArrivalAnchors,
            AudioSource configuredAudioSource,
            AudioClip configuredExitSound,
            GameObject configuredOpenVisual = null,
            GameObject configuredClosedCover = null,
            GameObject configuredExitIndicatorRoot = null,
            TextMesh configuredExitIndicatorLabel = null,
            Renderer[] configuredExitIndicatorRenderers = null,
            Light configuredExitIndicatorLight = null,
            string configuredExitLabel = "EXIT")
        {
            interactionTarget = configuredInteractionTarget;
            linkedEntrance = configuredLinkedEntrance;
            exteriorArrivalAnchors = configuredExteriorArrivalAnchors;
            audioSource = configuredAudioSource;
            exitSound = configuredExitSound;
            openVisual = configuredOpenVisual;
            closedCover = configuredClosedCover;
            exitIndicatorRoot = configuredExitIndicatorRoot;
            exitIndicatorLabel = configuredExitIndicatorLabel;
            exitIndicatorRenderers = configuredExitIndicatorRenderers;
            exitIndicatorLight = configuredExitIndicatorLight;
            exitLabel = configuredExitLabel;
            ApplyPresentation(true);
            SetIndicatorVisible(false);
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
            ApplyIndicatorAppearance(open);
        }

        internal void ApplyLinkedPresentation(bool open)
        {
            ApplyPresentation(open);
        }

        private bool ShouldShowExitIndicator()
        {
            if (!_spawned || Runner == null || !Runner.LocalPlayer.IsValid ||
                !Runner.TryGetPlayerObject(Runner.LocalPlayer, out NetworkObject playerObject))
                return false;

            FusionNetworkPlayer localPlayer = playerObject.GetComponent<FusionNetworkPlayer>();
            return localPlayer != null && localPlayer.HasInputAuthority && localPlayer.IsInVent;
        }

        private void SetIndicatorVisible(bool visible)
        {
            if (_lastIndicatorVisible == visible &&
                (exitIndicatorRoot == null || exitIndicatorRoot.activeSelf == visible))
                return;

            _lastIndicatorVisible = visible;
            if (exitIndicatorRoot != null)
                exitIndicatorRoot.SetActive(visible);
        }

        private void ApplyIndicatorAppearance(bool open)
        {
            _indicatorPropertyBlock ??= new MaterialPropertyBlock();
            Color color = open ? openIndicatorColor : closedIndicatorColor;
            if (exitIndicatorLabel != null)
            {
                exitIndicatorLabel.text = open ? exitLabel : $"{exitLabel} \u2014 CLOSED";
                exitIndicatorLabel.color = color;
            }

            if (exitIndicatorRenderers != null)
            {
                for (int index = 0; index < exitIndicatorRenderers.Length; index++)
                {
                    Renderer indicatorRenderer = exitIndicatorRenderers[index];
                    if (indicatorRenderer == null)
                        continue;

                    indicatorRenderer.GetPropertyBlock(_indicatorPropertyBlock);
                    _indicatorPropertyBlock.SetColor("_BaseColor", color);
                    _indicatorPropertyBlock.SetColor("_Color", color);
                    _indicatorPropertyBlock.SetColor("_EmissionColor", color * (open ? 1.4f : 0.35f));
                    indicatorRenderer.SetPropertyBlock(_indicatorPropertyBlock);
                }
            }

            if (exitIndicatorLight != null)
            {
                exitIndicatorLight.color = color;
                exitIndicatorLight.intensity = open ? openIndicatorIntensity : closedIndicatorIntensity;
            }
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
            {
                ApplyIndicatorAppearance(true);
                SetIndicatorVisible(false);
            }
        }
#endif

        private void OnDisable()
        {
            SetIndicatorVisible(false);
        }
    }
}
