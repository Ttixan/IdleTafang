namespace IdleTafang.Gameplay
{
    public enum RunResult
    {
        InProgress,
        Victory,
        Defeat
    }

    public sealed class RunSession
    {
        private const int TotalWaves = 5;

        public int WaveIndex { get; private set; }
        public int CompletedWaves { get; private set; }
        public int MaxWaves => TotalWaves;
        public RunResult Result { get; private set; } = RunResult.InProgress;

        public bool IsFinished => Result != RunResult.InProgress;

        public void Reset()
        {
            WaveIndex = 0;
            CompletedWaves = 0;
            Result = RunResult.InProgress;
        }

        public void AdvanceWave()
        {
            if (IsFinished)
            {
                return;
            }

            WaveIndex += 1;
            CompletedWaves += 1;
        }

        public void CompleteRun()
        {
            if (!IsFinished)
            {
                Result = RunResult.Victory;
            }
        }

        public void FailRun()
        {
            if (!IsFinished)
            {
                Result = RunResult.Defeat;
            }
        }
    }
}
