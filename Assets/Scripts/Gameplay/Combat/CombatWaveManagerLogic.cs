namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatWaveManagerLogic
    {
        private float spawnTimer;
        private int spawnIndex;

        public float SpawnInterval { get; }

        public CombatWaveManagerLogic(float spawnInterval)
        {
            SpawnInterval = spawnInterval <= 0f ? 1f : spawnInterval;
        }

        public bool Tick(float deltaTime, int spawnPointCount, out int spawnPointIndex)
        {
            spawnPointIndex = -1;

            if (deltaTime <= 0f || spawnPointCount <= 0)
            {
                return false;
            }

            spawnTimer += deltaTime;
            if (spawnTimer < SpawnInterval)
            {
                return false;
            }

            spawnTimer = 0f;
            spawnPointIndex = spawnIndex % spawnPointCount;
            spawnIndex += 1;
            return true;
        }
    }
}