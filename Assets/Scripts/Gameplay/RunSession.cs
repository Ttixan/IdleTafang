namespace IdleTafang.Gameplay
{
    public sealed class RunSession
    {
        public int WaveIndex { get; private set; }

        public void Reset()
        {
            WaveIndex = 0;
        }

        public void AdvanceWave()
        {
            WaveIndex += 1;
        }
    }
}
