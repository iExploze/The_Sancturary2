using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// Replicates one authoritative on/off state. Each peer applies that logical state
    /// to its directly assigned lights while preserving their configured intensities.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkLightSwitch : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        [Header("Interaction")]
        [SerializeField] private InteractionTarget interactionTarget;

        [Header("Lights")]
        [SerializeField] private Light[] controlledLights;
        [SerializeField] private bool startsOn = true;

        [Header("Local replicated-state audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip switchClickSound;

        [Networked] public NetworkBool IsOn { get; private set; }
        [Networked] private TickTimer InteractionCooldown { get; set; }

        private float[] _normalIntensities;
        private bool _lastPresentedState;
        private bool _spawned;

        public InteractionTarget PromptTarget => interactionTarget;

        private void Awake()
        {
            ResolveReferences();
            CaptureNormalIntensities();
        }

        public override void Spawned()
        {
            ResolveReferences();
            CaptureNormalIntensities();

            if (HasStateAuthority)
                IsOn = startsOn;

            _lastPresentedState = IsOn;
            ApplyLightState(_lastPresentedState);
            _spawned = true;
        }

        private void Update()
        {
            if (!_spawned)
                return;

            bool isOn = IsOn;
            if (isOn == _lastPresentedState)
                return;

            _lastPresentedState = isOn;
            ApplyLightState(isOn);
            PlaySwitchSound();
        }

        public bool TryGetActionText(
            NetworkPlayerInventory viewerInventory,
            string interactionVerb,
            out string actionText)
        {
            if (!_spawned)
            {
                actionText = null;
                return false;
            }

            actionText = IsOn ? "F \u2014 Turn Off" : "F \u2014 Turn On";
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
            if (!HasStateAuthority || requestingPlayer == null || requestingPlayer.IsDeadOrPending ||
                IsInteractionCoolingDown())
                return false;

            IsOn = !IsOn;
            InteractionCooldown = TickTimer.CreateFromSeconds(Runner, 0.1f);
            return true;
        }

        public void Configure(
            InteractionTarget configuredInteractionTarget,
            Light[] configuredLights,
            AudioSource configuredAudioSource,
            AudioClip configuredSwitchClickSound,
            bool configuredStartsOn = true)
        {
            interactionTarget = configuredInteractionTarget;
            controlledLights = configuredLights;
            audioSource = configuredAudioSource;
            switchClickSound = configuredSwitchClickSound;
            startsOn = configuredStartsOn;
            CaptureNormalIntensities();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            interactionTarget ??= GetComponent<InteractionTarget>();
            audioSource ??= GetComponent<AudioSource>();
        }

        private void CaptureNormalIntensities()
        {
            int count = controlledLights?.Length ?? 0;
            _normalIntensities = new float[count];
            for (int i = 0; i < count; i++)
            {
                Light controlledLight = controlledLights[i];
                if (controlledLight != null)
                    _normalIntensities[i] = controlledLight.intensity;
            }
        }

        private void ApplyLightState(bool isOn)
        {
            if (controlledLights == null)
                return;

            if (_normalIntensities == null || _normalIntensities.Length != controlledLights.Length)
                CaptureNormalIntensities();

            for (int i = 0; i < controlledLights.Length; i++)
            {
                Light controlledLight = controlledLights[i];
                if (controlledLight != null)
                    controlledLight.intensity = isOn ? _normalIntensities[i] : 0f;
            }
        }

        private void PlaySwitchSound()
        {
            if (audioSource != null && switchClickSound != null)
                audioSource.PlayOneShot(switchClickSound);
        }

        private bool IsInteractionCoolingDown()
        {
            return InteractionCooldown.IsRunning && Runner != null &&
                   !InteractionCooldown.ExpiredOrNotRunning(Runner);
        }
    }
}
