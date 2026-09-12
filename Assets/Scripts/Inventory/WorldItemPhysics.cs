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
        [SerializeField, Tooltip("Keep an authored testing supply on its tray. Dynamically dropped copies retain normal physics.")]
        private bool stationarySceneSupply;

        private bool _dropPoseStaged;
        private bool _dropPoseActivated;

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
                            networkObject.HasStateAuthority &&
                            !(stationarySceneSupply && networkObject.NetworkTypeId.IsSceneObject) &&
                            (networkObject.NetworkTypeId.IsSceneObject ||
                             _dropPoseActivated);

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

        public void ResetSandboxPose(Pose pose)
        {
            ResolveReferences();
            NetworkTransform networkTransform = GetComponent<NetworkTransform>();
            if (networkObject == null || !networkObject.IsValid || !networkObject.HasStateAuthority ||
                !TheSancturary.FusionPrototype.SandboxSession.IsActiveFor(networkTransform)) return;
            StopMotion();
            if (body != null)
            {
                body.isKinematic = true;
                body.position = pose.position;
                body.rotation = pose.rotation;
            }
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            networkTransform.Teleport(pose.position, pose.rotation);
        }

        /// <summary>
        /// Copies the intended drop pose into both Unity transform stores before
        /// Fusion invokes Spawned. Awake runs while a prefab instance is still at
        /// its authored pose, so moving only Transform leaves Rigidbody at that
        /// stale pose and the first physics step can snap the object back there.
        /// </summary>
        public bool StageDropPoseBeforeSpawn(
            Vector3 position,
            Quaternion rotation)
        {
            ResolveReferences();
            if (body == null)
                return false;

            _dropPoseStaged = true;
            _dropPoseActivated = false;
            StopMotion();
            body.useGravity = false;
            body.isKinematic = true;
            transform.SetPositionAndRotation(position, rotation);
            body.position = position;
            body.rotation = rotation;
            body.Sleep();
            return PoseMatches(position, rotation);
        }

        /// <summary>
        /// Establishes the authoritative Fusion pose once, confirms Transform and
        /// Rigidbody agree, and only then permits dynamic physics simulation.
        /// </summary>
        public bool TryActivatePreparedDrop(
            Vector3 position,
            Quaternion rotation)
        {
            ResolveReferences();
            if (!_dropPoseStaged ||
                body == null ||
                networkObject == null ||
                !networkObject.IsValid ||
                !networkObject.HasStateAuthority ||
                networkObject.NetworkTypeId.IsSceneObject)
                return false;

            body.useGravity = false;
            body.isKinematic = true;
            GetComponent<NetworkTransform>()?.Teleport(position, rotation);
            transform.SetPositionAndRotation(position, rotation);
            body.position = position;
            body.rotation = rotation;
            if (!PoseMatches(position, rotation))
                return false;

            _dropPoseActivated = true;
            ApplyAvailableState(true);
            return PoseMatches(position, rotation);
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
            if (body == null || body.isKinematic)
                return;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private bool PoseMatches(Vector3 position, Quaternion rotation)
        {
            const float positionTolerance = 0.01f;
            const float rotationTolerance = 1f;
            float positionToleranceSquared =
                positionTolerance * positionTolerance;
            return (transform.position - position).sqrMagnitude <=
                       positionToleranceSquared &&
                   (body.position - position).sqrMagnitude <=
                       positionToleranceSquared &&
                   Quaternion.Angle(transform.rotation, rotation) <=
                       rotationTolerance &&
                   Quaternion.Angle(body.rotation, rotation) <=
                       rotationTolerance;
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
