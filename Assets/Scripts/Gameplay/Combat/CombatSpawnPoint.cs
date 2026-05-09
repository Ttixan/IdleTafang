using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatSpawnPoint : MonoBehaviour
    {
        [SerializeField] private Transform target;

        public Vector3 Position => transform.position;
        public Transform Target => target;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
