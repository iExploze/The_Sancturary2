using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    public sealed class FusionSpawnPoint : MonoBehaviour
    {
        [Min(0)] public int index;

        private void OnDrawGizmos()
        {
            Gizmos.color = index == 0 ? new Color(0.1f, 1f, 0.8f, 0.9f) : new Color(0.1f, 0.65f, 1f, 0.75f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.32f, 0.32f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.48f, 0.32f);
            Gizmos.DrawLine(transform.position + new Vector3(0.32f, 0.32f, 0f), transform.position + new Vector3(0.32f, 1.48f, 0f));
            Gizmos.DrawLine(transform.position + new Vector3(-0.32f, 0.32f, 0f), transform.position + new Vector3(-0.32f, 1.48f, 0f));
            Gizmos.DrawRay(transform.position + Vector3.up * 1.55f, transform.forward * 0.8f);
        }
    }
}
