namespace IdleTafang.Gameplay.Combat
{
    public readonly struct SectorFocusSnapshot
    {
        public SectorFocusSnapshot(int focusedSector, int sectorCount, bool isReady, float warmupRemaining, bool hasTarget)
        {
            FocusedSector = focusedSector;
            SectorCount = sectorCount;
            IsReady = isReady;
            WarmupRemaining = warmupRemaining;
            HasTarget = hasTarget;
        }

        public int FocusedSector { get; }
        public int SectorCount { get; }
        public bool IsReady { get; }
        public float WarmupRemaining { get; }
        public bool HasTarget { get; }

        public static SectorFocusSnapshot Idle(int sectorCount = 3)
        {
            return new SectorFocusSnapshot(0, sectorCount, true, 0f, false);
        }
    }
}
