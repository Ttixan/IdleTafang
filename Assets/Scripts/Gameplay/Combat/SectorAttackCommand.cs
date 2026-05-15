namespace IdleTafang.Gameplay.Combat
{
    public readonly struct SectorAttackCommand
    {
        public static SectorAttackCommand None => new SectorAttackCommand(0, 0, false);

        public SectorAttackCommand(int targetId, int damage)
            : this(targetId, damage, damage > 0)
        {
        }

        private SectorAttackCommand(int targetId, int damage, bool hasAttack)
        {
            TargetId = targetId;
            Damage = damage;
            HasAttack = hasAttack;
        }

        public int TargetId { get; }
        public int Damage { get; }
        public bool HasAttack { get; }
    }
}
