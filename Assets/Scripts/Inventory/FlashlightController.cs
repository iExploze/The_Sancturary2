using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Applies replicated flashlight presentation only. Input and authority live
    /// on FusionNetworkPlayer and NetworkPlayerInventory.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlashlightController : MonoBehaviour
    {
        [Header("Spotlight")]
        [SerializeField] private Light flashlightLight;
        [SerializeField, Min(0.1f)] private float range = 20f;
        [SerializeField, Range(20f, 90f)] private float spotAngle = 55f;
        [SerializeField, Min(0f)] private float intensity = 8f;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private LightShadows shadows = LightShadows.Soft;

        public bool IsLightEnabled =>
            flashlightLight != null && flashlightLight.enabled;

        private void Awake()
        {
            ResolveLight();
            ApplySettings();
            SetLightEnabled(false);
        }

        public void SetLightEnabled(bool enabled)
        {
            ResolveLight();
            if (flashlightLight != null)
                flashlightLight.enabled = enabled;
        }

        private void ApplySettings()
        {
            if (flashlightLight == null)
                return;

            flashlightLight.type = LightType.Spot;
            flashlightLight.color = color;
            flashlightLight.range = range;
            flashlightLight.spotAngle = spotAngle;
            flashlightLight.intensity = intensity;
            flashlightLight.shadows = shadows;
        }

        private void ResolveLight()
        {
            flashlightLight ??= GetComponentInChildren<Light>(true);
        }

        private void OnDisable()
        {
            SetLightEnabled(false);
        }
    }
}
