using UnityEngine;

namespace TheSancturary.Player
{
    public static class PickupPhysicsRules
    {
        public static PickupPhysicsSettings Resolve(
            PickupPhysicsSettings authoredSettings,
            bool isHeld,
            bool isServer)
        {
            if (isHeld)
            {
                return new PickupPhysicsSettings(
                    false,
                    true,
                    RigidbodyInterpolation.None,
                    CollisionDetectionMode.ContinuousSpeculative);
            }

            return new PickupPhysicsSettings(
                authoredSettings.UseGravity,
                !isServer || authoredSettings.IsKinematic,
                authoredSettings.Interpolation,
                isServer && !authoredSettings.IsKinematic
                    ? authoredSettings.CollisionDetection
                    : CollisionDetectionMode.ContinuousSpeculative);
        }
    }
}
