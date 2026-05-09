using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatWaveManager : MonoBehaviour
    {
        [SerializeField] private CombatArena arena;
        [SerializeField] private CombatPath path;
        [SerializeField] private CombatEnemy enemyPrefab;
        [SerializeField] private float spawnInterval = 2f;

        private float spawnTimer;
        private int spawnIndex;

        private void Update()
        {
            if (arena == null || path == null || enemyPrefab == null || path.SpawnPoints == null || path.SpawnPoints.Length == 0)
            {
                return;
            }

            spawnTimer += Time.deltaTime;
            if (spawnTimer < spawnInterval)
            {
                return;
            }

            spawnTimer = 0f;
            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            CombatSpawnPoint spawnPoint = path.SpawnPoints[spawnIndex % path.SpawnPoints.Length];
            spawnIndex += 1;

            CombatEnemy enemy = Instantiate(enemyPrefab, spawnPoint.Position, Quaternion.identity);
            enemy.SetTarget(arena.CenterPoint);
        }
    }
}
