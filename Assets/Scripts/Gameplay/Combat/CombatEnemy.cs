using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatEnemy : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;

        private Transform target;

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
    }
}
