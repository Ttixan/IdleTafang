using System;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatPath : MonoBehaviour
    {
        [SerializeField] private CombatSpawnPoint[] spawnPoints = Array.Empty<CombatSpawnPoint>();

        public CombatSpawnPoint[] SpawnPoints => spawnPoints;

        public CombatPathLogic Logic
        {
            get
            {
                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    return new CombatPathLogic(null);
                }

                CombatPoint[] points = new CombatPoint[spawnPoints.Length];
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    points[i] = spawnPoints[i].LogicPosition;
                }

                return new CombatPathLogic(points);
            }
        }

        private void OnValidate()
        {
            RebuildSpawnPoints();
        }

        private void Awake()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                RebuildSpawnPoints();
            }
        }

        public void RebuildSpawnPoints()
        {
            spawnPoints = GetComponentsInChildren<CombatSpawnPoint>(true);
        }
    }
}
