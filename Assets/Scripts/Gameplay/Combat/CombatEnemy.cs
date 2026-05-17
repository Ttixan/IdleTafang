using System;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatEnemy : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float reachThreshold = 0.15f;
        [SerializeField] private int maxHealth = 5;

        private Transform target;
        private CombatEnemyLogic logic;
        private bool hasReachedTarget;
        private int currentHealth;
        private bool isDead;

        public event Action<CombatEnemy> ReachedTarget;
        public event Action<CombatEnemy> Died;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => Mathf.Max(1, maxHealth);

        private void Awake()
        {
            logic = new CombatEnemyLogic(new CombatPoint(transform.position.x, transform.position.z), moveSpeed);
            currentHealth = MaxHealth;
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

            if (isDead)
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

        public void TakeDamage(int damage)
        {
            if (isDead)
            {
                return;
            }

            int applied = Mathf.Max(0, damage);
            if (applied <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - applied);
            if (currentHealth <= 0)
            {
                isDead = true;
                Died?.Invoke(this);
                Destroy(gameObject);
            }
        }

        /// <summary>F2：按难度倍率缩放血量（相对预制体上的设计血量）。</summary>
        public void ApplySpawnDifficultyMultiplier(float multiplier)
        {
            multiplier = Mathf.Max(0.01f, multiplier);
            int design = Mathf.Max(1, maxHealth);
            int scaled = Mathf.Max(1, Mathf.RoundToInt(design * multiplier));
            maxHealth = scaled;
            currentHealth = scaled;
        }
    }
}
