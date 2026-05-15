namespace IdleTafang.Gameplay.Combat
{
    public sealed class SectorFocusLogic
    {
        private readonly int sectorCount;
        private readonly float warmupDuration;

        public SectorFocusLogic(int sectorCount, float warmupDuration, int initialSector = 0)
        {
            this.sectorCount = sectorCount < 1 ? 1 : sectorCount;
            this.warmupDuration = warmupDuration < 0f ? 0f : warmupDuration;
            Reset(initialSector);
        }

        public int FocusedSector { get; private set; }

        public float WarmupRemaining { get; private set; }

        public int SectorCount => sectorCount;

        public bool IsReady => WarmupRemaining <= 0f;

        public void Reset(int initialSector = 0)
        {
            FocusedSector = Wrap(initialSector);
            WarmupRemaining = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (WarmupRemaining <= 0f)
            {
                return;
            }

            WarmupRemaining -= deltaTime;
            if (WarmupRemaining < 0f)
            {
                WarmupRemaining = 0f;
            }
        }

        public void FocusPrevious()
        {
            SwitchTo(Wrap(FocusedSector - 1));
        }

        public void FocusNext()
        {
            SwitchTo(Wrap(FocusedSector + 1));
        }

        private void SwitchTo(int sector)
        {
            int wrapped = Wrap(sector);
            if (wrapped == FocusedSector)
            {
                return;
            }

            FocusedSector = wrapped;
            WarmupRemaining = warmupDuration;
        }

        private int Wrap(int sector)
        {
            int mod = sector % sectorCount;
            return mod < 0 ? mod + sectorCount : mod;
        }
    }
}
