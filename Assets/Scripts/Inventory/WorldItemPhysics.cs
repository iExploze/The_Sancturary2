using Fusion;
using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>
    /// Keeps a shared world item's physical state aligned with its availability.
    /// NetworkTransform synchronizes the authoritative Rigidbody pose while remote
    /// peers keep their Rigidbody kinematic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(Rigidbody))]
    public sealed class WorldItemPhysics : MonoBehaviour
    {
        [SerializeField] private NetworkObject networkObject;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Collider[] physicalColliders;
        [SerializeField] private Vector3 localBoundsCenter;
        [SerializeField] private Vector3 localBoundsSize = Vector3.one * 0.1f;

        public Rigidbody Body => body;
        public Collider[] PhysicalColliders => physicalColliders;
        public Bounds LocalBounds => new(localBoundsCenter, localBoundsSize);
        public bool HasValidLocalBounds =>
            localBoundsSize.x > 0f &&
            localBoundsSize.y > 0f &&
            localBoundsSize.z > 0f;

        private void Awake()
        {
            ResolveReferences();
            StopMotion();
            if (body == null)
                return;

            body.useGravity = false;
            body.isKinematic = true;
        }

        public void ApplyAvailableState(bool available)
        {
            ResolveReferences();
            SetCollidersEnabled(available);

            if (body == null)
                return;

            bool simulate = available &&
                            networkObject != null &&
                            networkObject.IsValid &&
                            networkObject.HasStateAuthority;

            if (!simulate)
            {
                StopMotion();
                body.useGravity = false;
                body.isKinematic = true;
                body.Sleep();
                return;
            }

            body.isKinematic = false;
            body.useGravity = true;
            body.WakeUp();
        }

        public void PrepareForDrop()
        {
            ResolveReferences();
            StopMotion();
            ApplyAvailableState(true);
        }

        public void Configure(
            NetworkObject configuredNetworkObject,
            Rigidbody configuredBody,
            Collider[] configuredColliders,
            Bounds configuredLocalBounds)
        {
            networkObject = configuredNetworkObject;
            body = configuredBody;
            physicalColliders = configuredColliders;
            localBoundsCenter = configuredLocalBounds.center;
            localBoundsSize = configuredLocalBounds.size;
        }

        private void StopMotion()
        {
            if (body == null)
                return;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (physicalColliders == null)
                return;

            for (int index = 0; index < physicalColliders.Length; index++)
            {
                if (physicalColliders[index] != null)
                    physicalColliders[index].enabled = enabled;
            }
        }

        private void ResolveReferences()
        {
            networkObject ??= GetComponent<NetworkObject>();
            body ??= GetComponent<Rigidbody>();
            if (physicalColliders == null || physicalColliders.Length == 0)
                physicalColliders = GetComponentsInChildren<Collider>(true);
        }

        private void OnValidate()
        {
            ResolveReferences();
            localBoundsSize = new Vector3(
                Mathf.Max(0f, localBoundsSize.x),
                Mathf.Max(0f, localBoundsSize.y),
                Mathf.Max(0f, localBoundsSize.z));
        }
    }
}
