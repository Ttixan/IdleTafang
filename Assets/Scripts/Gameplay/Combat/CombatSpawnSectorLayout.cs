using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    /// <summary>
    /// 与 <see cref="SectorMath"/> 一致：角度按 Atan2(dx,dz)（度），0° 指向世界 +Z。
    /// </summary>
    public static class CombatSpawnSectorLayout
    {
        public static int TotalSpawnCount(int sectorCount, int spawnsPerSector)
        {
            return Mathf.Max(1, sectorCount) * Mathf.Max(1, spawnsPerSector);
        }

        /// <summary>
        /// 第 sectorIndex 扇区内第 slotInSector 个刷怪点的朝向角（度），落在该扇区的圆弧内部（非整条圆环）。
        /// </summary>
        public static float AngleDegreesForSlot(
            int sectorIndex,
            int slotInSector,
            int sectorCount,
            int spawnsPerSector,
            float insetDegrees)
        {
            int sectors = Mathf.Max(1, sectorCount);
            int per = Mathf.Max(1, spawnsPerSector);
            float sectorWidth = 360f / sectors;
            float theta0 = sectorIndex * sectorWidth + insetDegrees;
            float theta1 = (sectorIndex + 1) * sectorWidth - insetDegrees;
            if (theta1 <= theta0)
            {
                theta1 = theta0 + Mathf.Min(1f, sectorWidth * 0.25f);
            }

            float t = per <= 1 ? 0.5f : (slotInSector + 0.5f) / per;
            return Mathf.Lerp(theta0, theta1, t);
        }

        public static Vector3 WorldPositionFromArena(
            Vector3 arenaCenter,
            float heightOffset,
            float radius,
            float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float dx = radius * Mathf.Sin(rad);
            float dz = radius * Mathf.Cos(rad);
            return new Vector3(arenaCenter.x + dx, arenaCenter.y + heightOffset, arenaCenter.z + dz);
        }
    }
}
