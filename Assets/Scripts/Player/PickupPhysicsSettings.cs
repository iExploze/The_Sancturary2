using UnityEngine;

namespace TheSancturary.Player
{
    public readonly struct PickupPhysicsSettings
    {
        public PickupPhysicsSettings(
            bool useGravity,
            bool isKinematic,
            RigidbodyInterpolation interpolation,
            CollisionDetectionMode collisionDetection)
        {
            UseGravity = useGravity;
            IsKinematic = isKinematic;
            Interpolation = interpolation;
            CollisionDetection = collisionDetection;
        }

        public bool UseGravity { get; }
        public bool IsKinematic { get; }
        public RigidbodyInterpolation Interpolation { get; }
        public CollisionDetectionMode CollisionDetection { get; }
    }
}
