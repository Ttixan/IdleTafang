using System;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatPathLogic
    {
        public CombatPoint[] SpawnPoints { get; }

        public CombatPathLogic(CombatPoint[] spawnPoints)
        {
            SpawnPoints = spawnPoints ?? Array.Empty<CombatPoint>();
        }
    }
}