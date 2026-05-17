using System;

namespace IdleTafang.Gameplay.Combat
{
    /// <summary>
    /// 控制刷怪节奏；刷怪槽位为「打乱的无放回序列」，用尽后再洗牌，避免顺序轮询过于可预测。
    /// </summary>
    public sealed class CombatWaveManagerLogic
    {
        private readonly Random rng;
        private float spawnTimer;
        private int[] spawnPermutation;
        private int permutationCursor;
        private int lastSpawnPointCount;

        public float SpawnInterval { get; }

        public CombatWaveManagerLogic(float spawnInterval)
            : this(spawnInterval, null)
        {
        }

        /// <param name="rng">可传入固定种子以便测试；为 null 时使用与时间相关的默认随机源。</param>
        public CombatWaveManagerLogic(float spawnInterval, Random rng)
        {
            SpawnInterval = spawnInterval <= 0f ? 1f : spawnInterval;
            this.rng = rng ?? new Random();
        }

        public void Reset()
        {
            spawnTimer = 0f;
            spawnPermutation = null;
            permutationCursor = 0;
            lastSpawnPointCount = 0;
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

            if (spawnPointCount == 1)
            {
                spawnPointIndex = 0;
                return true;
            }

            if (spawnPermutation == null || lastSpawnPointCount != spawnPointCount || permutationCursor >= spawnPointCount)
            {
                ReshuffleSpawnOrder(spawnPointCount);
            }

            spawnPointIndex = spawnPermutation[permutationCursor];
            permutationCursor += 1;
            return true;
        }

        private void ReshuffleSpawnOrder(int spawnPointCount)
        {
            lastSpawnPointCount = spawnPointCount;

            if (spawnPermutation == null || spawnPermutation.Length != spawnPointCount)
            {
                spawnPermutation = new int[spawnPointCount];
                for (int i = 0; i < spawnPointCount; i++)
                {
                    spawnPermutation[i] = i;
                }
            }
            else
            {
                for (int i = 0; i < spawnPointCount; i++)
                {
                    spawnPermutation[i] = i;
                }
            }

            for (int i = spawnPointCount - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = spawnPermutation[i];
                spawnPermutation[i] = spawnPermutation[j];
                spawnPermutation[j] = tmp;
            }

            permutationCursor = 0;
        }
    }
}
