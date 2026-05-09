using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatPath : MonoBehaviour
    {
        [SerializeField] private CombatSpawnPoint[] spawnPoints;

        public CombatSpawnPoint[] SpawnPoints => spawnPoints;

        private void OnValidate()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = GetComponentsInChildren<CombatSpawnPoint>();
            }
        }
    }
}
