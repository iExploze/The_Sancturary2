using UnityEngine;

namespace TheSancturary.Monsters
{
    [CreateAssetMenu(menuName = "The Sancturary/Monster Definition", fileName = "MonsterDefinition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0f)] private float acceleration = 12f;
        [SerializeField, Min(0f)] private float detectionRange = 20f;
        [SerializeField, Min(0f)] private float stoppingDistance = 1.3f;
        [SerializeField, Min(0.02f)] private float targetRefreshInterval = 0.2f;

        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float DetectionRange => detectionRange;
        public float StoppingDistance => stoppingDistance;
        public float TargetRefreshInterval => targetRefreshInterval;
    }
}
