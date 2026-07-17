using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TheSancturary.Player
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerPickup : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private GameObject ownerHud;
        [SerializeField] private Text interactionPrompt;
        [SerializeField, Min(0.5f)] private float maximumPickupDistance = 2.5f;
        [SerializeField, Min(0.05f)] private float heldObjectClearanceRadius = 0.18f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private readonly NetworkVariable<bool> isHoldingObject = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<NetworkObjectReference> heldObjectReference = new(
            new NetworkObjectReference((NetworkObject)null),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkPickableObject serverHeldObject;

        public bool IsHoldingObject => isHoldingObject.Value;

        public override void OnNetworkSpawn()
        {
            if (ownerHud != null)
            {
                ownerHud.SetActive(IsOwner);
            }

            if (IsOwner)
            {
                UpdateInteractionPrompt();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                ReleaseServerHeldObject();
            }

            if (ownerHud != null)
            {
                ownerHud.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            UpdateInteractionPrompt();
            if (Keyboard.current?.eKey.wasPressedThisFrame != true)
            {
                return;
            }

            if (isHoldingObject.Value)
            {
                DropHeldObjectRpc();
                return;
            }

            if (TryGetLookedAtPickable(out var pickable))
            {
                TryPickupObjectRpc(new NetworkObjectReference(pickable.NetworkObject));
            }
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || serverHeldObject == null)
            {
                return;
            }

            if (!serverHeldObject.IsSpawned || !serverHeldObject.IsHeld)
            {
                ClearServerHeldReference();
                return;
            }

            serverHeldObject.MoveHeld(GetSafeHoldPosition(), holdPoint.rotation);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void TryPickupObjectRpc(NetworkObjectReference targetReference)
        {
            if (!targetReference.TryGet(out var targetObject, NetworkManager))
            {
                return;
            }

            var pickable = targetObject.GetComponent<NetworkPickableObject>();
            var targetCollider = targetObject.GetComponentInChildren<Collider>();
            var targetPosition = targetCollider != null
                ? targetCollider.ClosestPoint(playerCamera.transform.position)
                : targetObject.transform.position;
            var distance = Vector3.Distance(playerCamera.transform.position, targetPosition);
            var hasLineOfSight = HasServerLineOfSight(pickable, targetPosition);

            if (!PickupInteractionRules.CanPickUp(
                    serverHeldObject != null,
                    pickable != null && pickable.IsHeld,
                    pickable != null,
                    hasLineOfSight,
                    distance,
                    maximumPickupDistance + 0.25f) ||
                !pickable.TryHold(NetworkObject))
            {
                return;
            }

            serverHeldObject = pickable;
            isHoldingObject.Value = true;
            heldObjectReference.Value = targetReference;
            pickable.MoveHeld(GetSafeHoldPosition(), holdPoint.rotation);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void DropHeldObjectRpc()
        {
            ReleaseServerHeldObject();
        }

        private bool TryGetLookedAtPickable(out NetworkPickableObject pickable)
        {
            pickable = null;
            var hits = Physics.RaycastAll(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                maximumPickupDistance,
                interactionMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                pickable = hit.transform.GetComponentInParent<NetworkPickableObject>();
                return pickable != null && !pickable.IsHeld;
            }

            return false;
        }

        private bool HasServerLineOfSight(NetworkPickableObject target, Vector3 targetPosition)
        {
            if (target == null)
            {
                return false;
            }

            var origin = playerCamera.transform.position;
            var offset = targetPosition - origin;
            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return true;
            }

            var hits = Physics.RaycastAll(
                origin,
                offset.normalized,
                offset.magnitude + 0.1f,
                interactionMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                return hit.transform.GetComponentInParent<NetworkPickableObject>() == target;
            }

            return false;
        }

        private Vector3 GetSafeHoldPosition()
        {
            var origin = playerCamera.transform.position;
            var offset = holdPoint.position - origin;
            var distance = offset.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return holdPoint.position;
            }

            var direction = offset / distance;
            var safeDistance = distance;
            var hits = Physics.SphereCastAll(
                origin,
                heldObjectClearanceRadius,
                direction,
                distance,
                interactionMask,
                QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) ||
                    (serverHeldObject != null && hit.transform.IsChildOf(serverHeldObject.transform)))
                {
                    continue;
                }

                safeDistance = Mathf.Min(safeDistance, Mathf.Max(0.35f, hit.distance - 0.08f));
            }

            return origin + direction * safeDistance;
        }

        private void ReleaseServerHeldObject()
        {
            if (!IsServer || serverHeldObject == null)
            {
                ClearServerHeldReference();
                return;
            }

            var position = serverHeldObject.transform.position;
            var rotation = serverHeldObject.transform.rotation;
            serverHeldObject.Release(position, rotation);
            ClearServerHeldReference();
        }

        private void ClearServerHeldReference()
        {
            serverHeldObject = null;
            if (IsServer)
            {
                isHoldingObject.Value = false;
                heldObjectReference.Value = new NetworkObjectReference((NetworkObject)null);
            }
        }

        private void UpdateInteractionPrompt()
        {
            if (interactionPrompt == null)
            {
                return;
            }

            if (isHoldingObject.Value)
            {
                interactionPrompt.text = "E  DROP";
                return;
            }

            interactionPrompt.text = TryGetLookedAtPickable(out _) ? "E  PICK UP" : string.Empty;
        }
    }
}
