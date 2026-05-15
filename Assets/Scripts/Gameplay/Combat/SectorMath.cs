using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public static class SectorMath
    {
        public static int GetSectorIndex(float baseX, float baseZ, float worldX, float worldZ, int sectorCount)
        {
            int count = Mathf.Max(1, sectorCount);
            float dx = worldX - baseX;
            float dz = worldZ - baseZ;
            float angle = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            float sectorWidth = 360f / count;
            int index = Mathf.FloorToInt(angle / sectorWidth);
            if (index >= count)
            {
                index = count - 1;
            }

            return index;
        }

        public static float GetDistanceSqToBase(float baseX, float baseZ, float worldX, float worldZ)
        {
            float dx = worldX - baseX;
            float dz = worldZ - baseZ;
            return dx * dx + dz * dz;
        }
    }
}
