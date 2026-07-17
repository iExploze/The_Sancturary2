using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheSancturary.Player
{
    [DisallowMultipleComponent]
    public sealed class LocalTirednessVignette : MonoBehaviour
    {
        [SerializeField] private PlayerMovementDefinition movementDefinition;
        [SerializeField] private Volume volume;
        [SerializeField] private VolumeProfile volumeProfile;

        private VolumeProfile runtimeProfile;
        private Vignette vignette;
        private float normalizedStamina = 1f;
        private bool exhausted;
        private bool controlsVignette;

        public bool ControlsVignette => controlsVignette;

        public void ConfigureForOwner(bool isOwner)
        {
            ReleaseRuntimeProfile();
            controlsVignette = TirednessVignetteRules.ShouldControlVignette(isOwner);

            if (volume == null)
            {
                Debug.LogError("Local tiredness vignette is missing its Volume reference.", this);
                controlsVignette = false;
                return;
            }

            volume.enabled = false;
            if (!controlsVignette)
            {
                return;
            }

            if (movementDefinition == null || volumeProfile == null)
            {
                Debug.LogError("Local tiredness vignette is missing its movement definition or Volume Profile.", this);
                controlsVignette = false;
                return;
            }

            volume.sharedProfile = volumeProfile;
            runtimeProfile = volume.profile;
            runtimeProfile.name = volumeProfile.name + " (Local Runtime)";
            if (!runtimeProfile.TryGet(out vignette) || vignette == null)
            {
                Debug.LogError("The local tiredness Volume Profile does not contain a Vignette override.", this);
                controlsVignette = false;
                ReleaseRuntimeProfile();
                return;
            }

            vignette.active = true;
            vignette.intensity.Override(0f);
            volume.enabled = true;
        }

        public void SetStaminaState(float synchronizedNormalizedStamina, bool synchronizedExhausted)
        {
            if (!controlsVignette)
            {
                return;
            }

            normalizedStamina = Mathf.Clamp01(synchronizedNormalizedStamina);
            exhausted = synchronizedExhausted;
        }

        private void Update()
        {
            if (!controlsVignette || vignette == null || movementDefinition == null)
            {
                return;
            }

            var targetIntensity = TirednessVignetteRules.CalculateIntensity(
                normalizedStamina,
                movementDefinition.VignetteFadeInThreshold,
                movementDefinition.MaximumVignetteIntensity);

            if (exhausted && movementDefinition.ExhaustedPulseStrength > 0f)
            {
                var pulse = 0.5f + 0.5f * Mathf.Sin(
                    Time.unscaledTime * movementDefinition.ExhaustedPulseSpeed * Mathf.PI * 2f);
                targetIntensity -= pulse * movementDefinition.ExhaustedPulseStrength;
                targetIntensity = Mathf.Clamp(
                    targetIntensity,
                    0f,
                    movementDefinition.MaximumVignetteIntensity);
            }

            var response = 1f - Mathf.Exp(
                -movementDefinition.VignetteResponseSpeed * Time.unscaledDeltaTime);
            var intensity = Mathf.Lerp(
                vignette.intensity.value,
                targetIntensity,
                response);
            if (targetIntensity <= 0f && intensity < 0.0001f)
            {
                intensity = 0f;
            }

            vignette.intensity.Override(intensity);
        }

        private void OnDestroy()
        {
            ReleaseRuntimeProfile();
        }

        private void ReleaseRuntimeProfile()
        {
            vignette = null;
            if (volume != null)
            {
                volume.enabled = false;
                volume.profile = null;
                volume.sharedProfile = volumeProfile;
            }

            if (runtimeProfile == null)
            {
                return;
            }

            foreach (var component in runtimeProfile.components)
            {
                DestroyRuntimeObject(component);
            }

            DestroyRuntimeObject(runtimeProfile);
            runtimeProfile = null;
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }
    }
}
