namespace IdleTafang.Gameplay.Combat
{
    public readonly struct SectorCombatEnemyInfo
    {
        public SectorCombatEnemyInfo(int id, float x, float z)
        {
            Id = id;
            X = x;
            Z = z;
        }

        public int Id { get; }
        public float X { get; }
        public float Z { get; }
    }
}
