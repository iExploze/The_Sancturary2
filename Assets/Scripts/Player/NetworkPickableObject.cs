using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace TheSancturary.Player
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(NetworkRigidbody))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NetworkPickableObject : NetworkBehaviour
    {
        [SerializeField] private Rigidbody physicsBody;
        [SerializeField] private Collider[] managedColliders;
        // NetworkRigidbody makes the body kinematic in Awake, so restoration must use authored values.
        [SerializeField] private bool releasedUseGravity = true;
        [SerializeField] private bool releasedIsKinematic;
        [SerializeField] private RigidbodyInterpolation releasedInterpolation = RigidbodyInterpolation.Interpolate;
        [SerializeField] private CollisionDetectionMode releasedCollisionDetection = CollisionDetectionMode.ContinuousDynamic;

        private readonly NetworkVariable<PickupNetworkState> pickupState = new(
            PickupNetworkState.Released,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private PickupPhysicsSettings AuthoredPhysicsSettings => new(
            releasedUseGravity,
            releasedIsKinematic,
            releasedInterpolation,
            releasedCollisionDetection);

        public PickupNetworkState State => pickupState.Value;
        public bool IsHeld => pickupState.Value.IsHeld;

        private void Awake()
        {
            if (physicsBody == null)
            {
                physicsBody = GetComponent<Rigidbody>();
            }

            if (managedColliders == null || managedColliders.Length == 0)
            {
                managedColliders = GetComponentsInChildren<Collider>(true);
            }

        }

        public override void OnNetworkSpawn()
        {
            pickupState.OnValueChanged += HandlePickupStateChanged;
            ApplyReplicatedState(pickupState.Value);

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            pickupState.OnValueChanged -= HandlePickupStateChanged;
            if (NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public bool TryHold(NetworkObject holder)
        {
            if (!IsServer || holder == null || !holder.IsSpawned || IsHeld)
            {
                return false;
            }

            pickupState.Value = PickupNetworkState.HeldBy(holder);
            ApplyReplicatedState(pickupState.Value);
            return true;
        }

        public void MoveHeld(Vector3 position, Quaternion rotation)
        {
            if (!IsServer || !IsHeld)
            {
                return;
            }

            physicsBody.MovePosition(position);
            physicsBody.MoveRotation(rotation);
        }

        public void Release(Vector3 position, Quaternion rotation)
        {
            if (!IsServer || !IsHeld)
            {
                return;
            }

            physicsBody.position = position;
            physicsBody.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            pickupState.Value = PickupNetworkState.Released;
            ApplyReplicatedState(pickupState.Value);
        }

        private void HandlePickupStateChanged(PickupNetworkState previous, PickupNetworkState current)
        {
            ApplyReplicatedState(current);
        }

        private void ApplyReplicatedState(PickupNetworkState state)
        {
            foreach (var managedCollider in managedColliders)
            {
                if (managedCollider != null)
                {
                    managedCollider.enabled = !state.IsHeld;
                }
            }

            var resolvedSettings = PickupPhysicsRules.Resolve(
                AuthoredPhysicsSettings,
                state.IsHeld,
                IsServer);

            if (state.IsHeld && !physicsBody.isKinematic)
            {
                physicsBody.linearVelocity = Vector3.zero;
                physicsBody.angularVelocity = Vector3.zero;
            }

            physicsBody.useGravity = resolvedSettings.UseGravity;
            physicsBody.isKinematic = resolvedSettings.IsKinematic;
            physicsBody.collisionDetectionMode = resolvedSettings.CollisionDetection;
            physicsBody.interpolation = resolvedSettings.Interpolation;
            if (!state.IsHeld && IsServer && !physicsBody.isKinematic)
            {
                physicsBody.linearVelocity = Vector3.zero;
                physicsBody.angularVelocity = Vector3.zero;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            var holderResolved = pickupState.Value.Holder.TryGet(out var holder, NetworkManager);
            if (!IsServer || !PickupReleaseRules.ShouldReleaseAfterDisconnect(
                    pickupState.Value.IsHeld,
                    holderResolved,
                    holderResolved ? holder.OwnerClientId : ulong.MaxValue,
                    clientId))
            {
                return;
            }

            Release(physicsBody.position, physicsBody.rotation);
        }
    }
}
