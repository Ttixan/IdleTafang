using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatWaveManager : MonoBehaviour
    {
        [SerializeField] private CombatArena arena;
        [SerializeField] private CombatPath path;
        [SerializeField] private CombatEnemy enemyPrefab;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int enemiesPerWave = 12;
        [SerializeField] private int baseHealth = 5;
        [SerializeField] private int enemyDamageOnReach = 1;

        private CombatWaveManagerLogic logic;
        private readonly List<CombatEnemy> activeEnemies = new List<CombatEnemy>();
        private int spawnedCount;
        private int escapedCount;
        private int currentBaseHealth;
        private int maxBaseHealth;
        private bool waveCompleteLogged;
        private bool runFailedLogged;

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
            currentBaseHealth = Mathf.Max(0, currentBaseHealth - Mathf.Max(1, enemyDamageOnReach));

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

        private void OnDestroy()
        {
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                CombatEnemy enemy = activeEnemies[i];
                if (enemy != null)
                {
                    enemy.ReachedTarget -= OnEnemyReachedTarget;
                    enemy.Died -= OnEnemyDied;
                }
            }

            activeEnemies.Clear();
        }
    }
}
