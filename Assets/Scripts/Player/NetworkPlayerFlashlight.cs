using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Player
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerFlashlight : NetworkBehaviour
    {
        [SerializeField] private GameObject flashlightVisual;
        [SerializeField] private Light flashlightLight;
        [SerializeField] private bool startsEnabled;

        private readonly NetworkVariable<bool> flashlightEnabled = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public bool IsFlashlightEnabled => flashlightEnabled.Value;

        public override void OnNetworkSpawn()
        {
            flashlightEnabled.OnValueChanged += HandleFlashlightStateChanged;
            if (IsServer)
            {
                flashlightEnabled.Value = startsEnabled;
            }

            if (flashlightVisual != null)
            {
                flashlightVisual.SetActive(true);
            }

            ApplyFlashlightState(flashlightEnabled.Value);
        }

        public override void OnNetworkDespawn()
        {
            flashlightEnabled.OnValueChanged -= HandleFlashlightStateChanged;
            ApplyFlashlightState(false);
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || Keyboard.current?.fKey.wasPressedThisFrame != true)
            {
                return;
            }

            SetFlashlightRpc(!flashlightEnabled.Value);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetFlashlightRpc(bool enabledState)
        {
            flashlightEnabled.Value = enabledState;
        }

        private void HandleFlashlightStateChanged(bool previous, bool current)
        {
            ApplyFlashlightState(current);
        }

        private void ApplyFlashlightState(bool enabledState)
        {
            if (flashlightLight != null)
            {
                flashlightLight.enabled = enabledState;
            }
        }
    }
}
