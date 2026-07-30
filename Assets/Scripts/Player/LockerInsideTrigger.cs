using System.Collections.Generic;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>
    /// Deduplicates the player's child colliders before forwarding authoritative
    /// trigger membership to the locker on the parent object.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class LockerInsideTrigger : MonoBehaviour
    {
        [SerializeField] private LockerController locker;

        private readonly Dictionary<FusionNetworkPlayer, HashSet<int>> _playerColliders = new();

        public void Configure(LockerController configuredLocker)
        {
            locker = configuredLocker;
        }

        private void Awake()
        {
            if (locker == null)
                locker = GetComponentInParent<LockerController>();
        }

        private void OnDisable()
        {
            if (locker != null && locker.HasStateAuthority)
            {
                foreach (FusionNetworkPlayer player in _playerColliders.Keys)
                    locker.NotifyPlayerExitedAuthoritative(player);
            }

            _playerColliders.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            FusionNetworkPlayer player = ResolvePlayer(other);
            if (player == null)
                return;

            if (!_playerColliders.TryGetValue(player, out HashSet<int> colliders))
            {
                colliders = new HashSet<int>();
                _playerColliders.Add(player, colliders);
            }

            if (!colliders.Add(other.GetInstanceID()) || colliders.Count != 1)
                return;

            locker?.NotifyPlayerEnteredAuthoritative(player);
        }

        private void OnTriggerExit(Collider other)
        {
            FusionNetworkPlayer player = ResolvePlayer(other);
            if (player == null ||
                !_playerColliders.TryGetValue(player, out HashSet<int> colliders) ||
                !colliders.Remove(other.GetInstanceID()))
            {
                return;
            }

            if (colliders.Count > 0)
                return;

            _playerColliders.Remove(player);
            locker?.NotifyPlayerExitedAuthoritative(player);
        }

        private static FusionNetworkPlayer ResolvePlayer(Collider other)
        {
            return other != null
                ? other.GetComponentInParent<FusionNetworkPlayer>()
                : null;
        }
    }
}
