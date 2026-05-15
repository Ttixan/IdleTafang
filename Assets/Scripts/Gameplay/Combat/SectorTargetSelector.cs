using System.Collections.Generic;

namespace IdleTafang.Gameplay.Combat
{
    public static class SectorTargetSelector
    {
        public static bool TrySelectNearestInSector(
            IReadOnlyList<SectorCombatEnemyInfo> enemies,
            float baseX,
            float baseZ,
            int focusedSector,
            int sectorCount,
            out int selectedId)
        {
            selectedId = 0;
            if (enemies == null || enemies.Count == 0)
            {
                return false;
            }

            bool found = false;
            float bestDistanceSq = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                SectorCombatEnemyInfo enemy = enemies[i];
                int sector = SectorMath.GetSectorIndex(baseX, baseZ, enemy.X, enemy.Z, sectorCount);
                if (sector != focusedSector)
                {
                    continue;
                }

                float distanceSq = SectorMath.GetDistanceSqToBase(baseX, baseZ, enemy.X, enemy.Z);
                if (found && distanceSq >= bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                selectedId = enemy.Id;
                found = true;
            }

            return found;
        }
    }
}
