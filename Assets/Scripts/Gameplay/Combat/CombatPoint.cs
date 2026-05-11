namespace IdleTafang.Gameplay.Combat
{
    public readonly struct CombatPoint
    {
        public float X { get; }
        public float Z { get; }

        public CombatPoint(float x, float z)
        {
            X = x;
            Z = z;
        }
    }
}