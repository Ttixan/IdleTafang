using System;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatEnemy : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float reachThreshold = 0.15f;

        private Transform target;
        private CombatEnemyLogic logic;
        private bool hasReachedTarget;

        public event Action<CombatEnemy> ReachedTarget;

        private void Awake()
        {
            logic = new CombatEnemyLogic(new CombatPoint(transform.position.x, transform.position.z), moveSpeed);
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
            hasReachedTarget = false;

            if (this.target != null)
            {
                logic?.SetTarget(new CombatPoint(this.target.position.x, this.target.position.z));
            }
        }

        private void Update()
        {
            if (logic == null)
            {
                return;
            }

            if (target != null)
            {
                logic.SetTarget(new CombatPoint(target.position.x, target.position.z));
            }

            logic.Tick(Time.deltaTime);
            CombatPoint position = logic.Position;
            transform.position = new Vector3(position.X, transform.position.y, position.Z);

            if (target == null || hasReachedTarget)
            {
                return;
            }

            float dx = target.position.x - transform.position.x;
            float dz = target.position.z - transform.position.z;
            float distanceSq = dx * dx + dz * dz;
            float thresholdSq = reachThreshold * reachThreshold;
            if (distanceSq <= thresholdSq)
            {
                hasReachedTarget = true;
                ReachedTarget?.Invoke(this);
            }
        }
    }
}
