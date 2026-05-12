namespace IdleTafang.Gameplay.Builds
{
    public sealed class BuildPrototype
    {
        public string Name { get; }
        public int EnergyCost { get; }
        public int Level { get; private set; }

        public int BaseHealthBonusPerLevel { get; } = 1;

        public int TurretBaseDamage { get; } = 2;
        public int TurretDamagePerLevel { get; } = 1;
        public float TurretBaseCooldownSeconds { get; } = 0.25f;
        public float TurretCooldownReducePerLevel { get; } = 0.02f;
        public float TurretMinCooldownSeconds { get; } = 0.08f;

        public BuildPrototype(string name, int energyCost)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Build" : name;
            EnergyCost = energyCost < 0 ? 0 : energyCost;
            Level = 1;
        }

        public void Upgrade()
        {
            Level += 1;
        }

        public int GetBaseHealthBonus()
        {
            int levelBonus = Level - 1;
            return levelBonus <= 0 ? 0 : levelBonus * BaseHealthBonusPerLevel;
        }

        public int GetTurretDamage()
        {
            int bonusLevels = Level - 1;
            return System.Math.Max(1, TurretBaseDamage + (bonusLevels <= 0 ? 0 : bonusLevels * TurretDamagePerLevel));
        }

        public float GetTurretFireCooldownSeconds()
        {
            int bonusLevels = Level - 1;
            float reduce = bonusLevels <= 0 ? 0f : bonusLevels * TurretCooldownReducePerLevel;
            return System.Math.Max(TurretMinCooldownSeconds, TurretBaseCooldownSeconds - reduce);
        }
    }
}