using System;
using System.Collections.Generic;
using IdleTafang.Config;
using IdleTafang.Gameplay.Resources;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    [DefaultExecutionOrder(50)]
    public sealed class CombatWaveManager : MonoBehaviour
    {
        [SerializeField] private CombatArena arena;
        [SerializeField] private CombatPath path;
        [SerializeField] private CombatEnemy enemyPrefab;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int enemiesPerWave = 12;
        [SerializeField] private int baseHealth = 5;
        [SerializeField] private int enemyDamageOnReach = 1;
        [SerializeField] private float enemyHpMultiplier = 1f;

        private CombatWaveManagerLogic logic;
        private readonly List<CombatEnemy> activeEnemies = new List<CombatEnemy>();
        private int spawnedCount;
        private int escapedCount;
        private int currentBaseHealth;
        private int maxBaseHealth;
        private bool waveCompleteLogged;
        private bool runFailedLogged;
        private bool combatActive;
        private int leakDamageReductionStacks;

        public event Action WaveCompleted;
        public event Action RunFailed;

        public int CurrentBaseHealth => currentBaseHealth;
        public int MaxBaseHealth => Mathf.Max(1, maxBaseHealth);
        public int SpawnedCount => spawnedCount;
        public int EscapedCount => escapedCount;
        public int EnemiesPerWave => Mathf.Max(1, enemiesPerWave);
        public int RemainingToSpawn => Mathf.Max(0, EnemiesPerWave - spawnedCount);
        public int ActiveEnemyCount => activeEnemies.Count;
        public bool IsRunFailed => currentBaseHealth <= 0;
        public bool IsWaveComplete => spawnedCount >= EnemiesPerWave && activeEnemies.Count == 0;

        public bool IsCombatActive => combatActive;

        /// <summary>漏怪伤害每层减少量（整局叠加，由波间强化写入）。</summary>
        public void SetLeakDamageReductionStacks(int stacks)
        {
            leakDamageReductionStacks = Mathf.Max(0, stacks);
        }

        /// <summary>E3：消耗 Energy 回复基地生命（不超过上限）。</summary>
        public bool TryRepairBase(ResourceWallet wallet, int energyCost, int healAmount)
        {
            if (wallet == null || healAmount <= 0 || energyCost <= 0)
            {
                return false;
            }

            if (currentBaseHealth >= maxBaseHealth)
            {
                return false;
            }

            if (!wallet.TrySpendEnergy(energyCost))
            {
                return false;
            }

            currentBaseHealth = Mathf.Min(maxBaseHealth, currentBaseHealth + healAmount);
            return true;
        }

        /// <summary>F1/F2/F5：由 Run 开局从 <see cref="RunConfig"/> 注入，须在 Awake 前执行（配合 <see cref="RunHudController"/> 更早执行序）。</summary>
        public void ApplyRunConfig(RunConfig cfg)
        {
            if (cfg == null)
            {
                return;
            }

            spawnInterval = cfg.EnemySpawnIntervalSeconds;
            enemiesPerWave = cfg.EnemiesPerWave;
            baseHealth = cfg.StartingBaseHealth;
            enemyDamageOnReach = cfg.EnemyDamageOnReach;
            enemyHpMultiplier = cfg.EnemyHpMultiplier;
            logic = new CombatWaveManagerLogic(spawnInterval);
            maxBaseHealth = Mathf.Max(1, baseHealth);
            currentBaseHealth = maxBaseHealth;
        }

        public void CopyActiveEnemies(List<CombatEnemy> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                CombatEnemy enemy = activeEnemies[i];
                if (enemy != null)
                {
                    buffer.Add(enemy);
                }
            }
        }

        public bool TryGetBasePosition(out Vector3 position)
        {
            if (arena != null && arena.CenterPoint != null)
            {
                position = arena.CenterPoint.position;
                return true;
            }

            position = default;
            return false;
        }

        public void SetCombatActive(bool active)
        {
            combatActive = active;

            if (!combatActive)
            {
                ClearActiveEnemies();
            }
        }

        public void ApplyMaxBaseHealth(int newMaxBaseHealth, bool addDeltaToCurrent)
        {
            int nextMax = Mathf.Max(1, newMaxBaseHealth);
            int previousMax = Mathf.Max(1, maxBaseHealth);
            maxBaseHealth = nextMax;

            if (addDeltaToCurrent)
            {
                int delta = nextMax - previousMax;
                currentBaseHealth = Mathf.Clamp(currentBaseHealth + delta, 0, maxBaseHealth);
            }
            else
            {
                currentBaseHealth = Mathf.Clamp(currentBaseHealth, 0, maxBaseHealth);
            }
        }

        public void StartNewWave()
        {
            if (logic == null)
            {
                logic = new CombatWaveManagerLogic(spawnInterval);
            }

            // In case StartNewWave is called early (e.g., debug hotkeys),
            // make sure no leftovers from previous wave remain.
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                CombatEnemy enemy = activeEnemies[i];
                if (enemy != null)
                {
                    enemy.ReachedTarget -= OnEnemyReachedTarget;
                    Destroy(enemy.gameObject);
                }
            }
            activeEnemies.Clear();

            spawnedCount = 0;
            waveCompleteLogged = false;
            runFailedLogged = false;
            logic.Reset();
        }

        private void Awake()
        {
            logic = new CombatWaveManagerLogic(spawnInterval);
            maxBaseHealth = Mathf.Max(1, baseHealth);
            currentBaseHealth = maxBaseHealth;
        }

        private void Update()
        {
            if (!combatActive)
            {
                return;
            }

            if (arena == null || path == null || enemyPrefab == null || path.SpawnPoints == null || path.SpawnPoints.Length == 0 || logic == null)
            {
                return;
            }

            if (!IsRunFailed && spawnedCount < EnemiesPerWave && logic.Tick(Time.deltaTime, path.SpawnPoints.Length, out int spawnPointIndex))
            {
                CombatSpawnPoint spawnPoint = path.SpawnPoints[spawnPointIndex];
                if (spawnPoint == null)
                {
                    Debug.LogWarning("CombatWaveManager found a null spawn point reference.");
                    return;
                }

                if (arena.CenterPoint == null)
                {
                    Debug.LogWarning("CombatWaveManager requires CombatArena.CenterPoint.");
                    return;
                }

                CombatEnemy enemy = Instantiate(enemyPrefab, spawnPoint.Position, Quaternion.identity);
                enemy.ReachedTarget += OnEnemyReachedTarget;
                enemy.Died += OnEnemyDied;
                enemy.SetTarget(arena.CenterPoint);
                enemy.ApplySpawnDifficultyMultiplier(enemyHpMultiplier);
                activeEnemies.Add(enemy);
                spawnedCount += 1;
            }

            if (IsRunFailed)
            {
                return;
            }

            if (!waveCompleteLogged && IsWaveComplete)
            {
                waveCompleteLogged = true;
                Debug.Log($"Wave complete. Spawned={spawnedCount}, Escaped={escapedCount}, BaseHP={currentBaseHealth}");
                WaveCompleted?.Invoke();
            }
        }

        private void OnEnemyReachedTarget(CombatEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.ReachedTarget -= OnEnemyReachedTarget;
            enemy.Died -= OnEnemyDied;
            activeEnemies.Remove(enemy);

            escapedCount += 1;
            int leak = Mathf.Max(0, enemyDamageOnReach - leakDamageReductionStacks);
            currentBaseHealth = Mathf.Max(0, currentBaseHealth - leak);

            Destroy(enemy.gameObject);

            if (currentBaseHealth <= 0)
            {
                if (!runFailedLogged)
                {
                    runFailedLogged = true;
                    Debug.Log($"Run failed. Base HP reached 0 after {escapedCount} enemies escaped.");
                    RunFailed?.Invoke();
                }
            }
        }

        private void OnEnemyDied(CombatEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.ReachedTarget -= OnEnemyReachedTarget;
            enemy.Died -= OnEnemyDied;
            activeEnemies.Remove(enemy);
        }

        private void ClearActiveEnemies()
        {
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                CombatEnemy enemy = activeEnemies[i];
                if (enemy != null)
                {
                    enemy.ReachedTarget -= OnEnemyReachedTarget;
                    enemy.Died -= OnEnemyDied;
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
        }

        private void OnDestroy()
        {
            ClearActiveEnemies();
        }
    }
}
