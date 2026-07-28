using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TheSancturary.Inventory
{
    [DisallowMultipleComponent]
    public sealed class FlashlightController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string toggleActionName = "Attack";

        [Header("Spotlight")]
        [SerializeField] private Light flashlightLight;
        [SerializeField, Min(0.1f)] private float range = 20f;
        [SerializeField, Range(20f, 90f)] private float spotAngle = 55f;
        [SerializeField, Min(0f)] private float intensity = 8f;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private LightShadows shadows = LightShadows.Soft;

        private InputAction _toggleAction;
        private PlayerInventory _ownerInventory;

        public bool IsLightEnabled => flashlightLight != null && flashlightLight.enabled;

        private void Awake()
        {
            ResolveLight();
            ApplySettings();
            SetLightEnabled(false);
        }

        public void Initialize(PlayerInput playerInput, PlayerInventory ownerInventory)
        {
            ResolveLight();
            ApplySettings();
            _ownerInventory = ownerInventory;
            _toggleAction = playerInput != null
                ? playerInput.actions.FindActionMap(actionMapName, false)?.FindAction(toggleActionName, false)
                : null;
            SetLightEnabled(false);
        }

        private void Update()
        {
            if (_toggleAction == null ||
                _ownerInventory == null ||
                _ownerInventory.IsMenuOpen ||
                Cursor.lockState != CursorLockMode.Locked ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (_toggleAction.WasPressedThisFrame())
                SetLightEnabled(!IsLightEnabled);
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
