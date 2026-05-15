using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatArena : MonoBehaviour
    {
        [SerializeField] private Transform centerPoint;
        [SerializeField] private Transform projectileSpawnPoint;

        public Transform CenterPoint => centerPoint;

        /// <summary>World position where sector-auto bullets spawn (tower vs ground).</summary>
        public Vector3 GetProjectileSpawnWorldPosition()
        {
            if (projectileSpawnPoint != null)
            {
                return projectileSpawnPoint.position;
            }

            Transform tower = transform.Find("TowerVisual");
            if (tower != null)
            {
                return tower.position;
            }

            if (centerPoint != null)
            {
                return centerPoint.position + Vector3.up * 1.5f;
            }

            return transform.position + Vector3.up * 1.5f;
        }

        public CombatArenaLogic Logic => new CombatArenaLogic(new CombatPoint(centerPoint != null ? centerPoint.position.x : transform.position.x, centerPoint != null ? centerPoint.position.z : transform.position.z));
    }
}
