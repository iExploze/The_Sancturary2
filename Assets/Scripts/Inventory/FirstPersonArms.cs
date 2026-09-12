using UnityEngine;

namespace TheSancturary.Inventory
{
    /// <summary>Local-only derived player arms. Animated grip sockets drive articulated elbows, wrists and fingers.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class FirstPersonArms : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Arm
        {
            public Transform upper;
            public Transform lower;
            public Transform hand;
            public Vector3 elbowHint;
            public Vector3 gripRotation;
            public Vector3 palmOffset;
            public Quaternion handBindRotation;
            public Vector3 forearmAxis;
            public Transform[] fingers;
            public Vector3[] fingerCurlAxes;
            public float[] fingerCurlDegrees;
            public Quaternion[] fingerGripRotations;
            [System.NonSerialized] public Quaternion[] fingerRest;
        }
        public Arm right;
        public Arm left;
        private HeldItemVisual _item;
        public void Bind(HeldItemVisual item) => _item = item;

        private void Awake()
        {
            foreach (var arm in new[] { right, left })
            {
                if (arm == null || arm.fingers == null) continue;
                arm.fingerRest = new Quaternion[arm.fingers.Length];
                for (int i = 0; i < arm.fingers.Length; i++) arm.fingerRest[i] = arm.fingers[i].localRotation;
            }
        }

        private void LateUpdate()
        {
            if (_item == null) return;
            Solve(right, _item.RightHandGrip, _item.RightGripClosure);
            Solve(left, _item.LeftHandGrip, _item.LeftGripClosure);
        }

        private void Solve(Arm arm, Transform target, float closure)
        {
            if (arm == null || arm.upper == null || arm.lower == null || arm.hand == null || target == null) return;
            Vector3 root = arm.upper.position;
            float upperLength = Vector3.Distance(root, arm.lower.position);
            float lowerLength = Vector3.Distance(arm.lower.position, arm.hand.position);
            Quaternion wristRotation = target.rotation * Quaternion.Euler(arm.gripRotation);
            Vector3 wristPosition = target.position - wristRotation * arm.palmOffset;
            Vector3 delta = wristPosition - root;
            float distance = Mathf.Clamp(delta.magnitude, 0.01f, upperLength + lowerLength - 0.001f);
            Vector3 direction = delta.normalized;
            Vector3 side = Vector3.ProjectOnPlane(transform.TransformPoint(arm.elbowHint) - root, direction).normalized;
            if (side.sqrMagnitude < 0.01f) side = transform.up;
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
            float outwards = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            Vector3 elbow = root + direction * along + side * outwards;
            arm.upper.rotation = Quaternion.FromToRotation(arm.lower.position - root, elbow - root) * arm.upper.rotation;
            Quaternion forearmRotation = wristRotation * Quaternion.Inverse(arm.handBindRotation);
            arm.lower.rotation = Quaternion.FromToRotation(forearmRotation * arm.forearmAxis,
                wristPosition - arm.lower.position) * forearmRotation;
            arm.hand.rotation = wristRotation;
            if (arm.fingers == null || arm.fingerRest == null) return;
            for (int i = 0; i < arm.fingers.Length; i++)
            {
                if (arm.fingerGripRotations != null && i < arm.fingerGripRotations.Length)
                {
                    arm.fingers[i].localRotation = Quaternion.Slerp(arm.fingerRest[i], arm.fingerGripRotations[i], Mathf.Clamp01(closure));
                    continue;
                }
                // A distinct thumb curl keeps the palm open around the grip instead of making a fist.
                float curl = arm.fingerCurlDegrees != null && i < arm.fingerCurlDegrees.Length
                    ? arm.fingerCurlDegrees[i] : 50f;
                Vector3 axis = arm.fingerCurlAxes != null && i < arm.fingerCurlAxes.Length
                    ? arm.fingerCurlAxes[i] : Vector3.up;
                arm.fingers[i].localRotation = arm.fingerRest[i] * Quaternion.AngleAxis(-curl * Mathf.Clamp01(closure), axis);
            }
        }
    }
}
