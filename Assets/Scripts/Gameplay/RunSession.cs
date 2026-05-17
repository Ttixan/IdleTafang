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
        private int configuredMaxWaves = 5;
        private readonly RunPhaseController phaseController = new RunPhaseController();

        public int WaveIndex { get; private set; }
        public int CompletedWaves { get; private set; }
        public int MaxWaves => configuredMaxWaves;
        public RunResult Result { get; private set; } = RunResult.InProgress;
        public RunPhaseController Phase => phaseController;

        public bool IsFinished => Result != RunResult.InProgress;

        /// <summary>F5：与 <c>RunConfig</c> 对齐总波次数（持久；<see cref="Reset"/> 不清除此值）。</summary>
        public void ConfigureMaxWaves(int maxWaves)
        {
            configuredMaxWaves = maxWaves < 1 ? 1 : maxWaves;
        }

        public void Reset()
        {
            WaveIndex = 0;
            CompletedWaves = 0;
            Result = RunResult.InProgress;
            phaseController.Reset();
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
