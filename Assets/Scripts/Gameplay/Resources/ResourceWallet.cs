namespace IdleTafang.Gameplay.Resources
{
    public sealed class ResourceWallet
    {
        public int Energy { get; private set; }
        public int Gold { get; private set; }

        public void AddEnergy(int amount)
        {
            if (amount > 0)
            {
                Energy += amount;
            }
        }

        public void AddGold(int amount)
        {
            if (amount > 0)
            {
                Gold += amount;
            }
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Energy < amount)
            {
                return false;
            }

            Energy -= amount;
            return true;
        }

        /// <summary>扣除能量；不足时清零（遗留行为，新逻辑请用 <see cref="TrySpendEnergy"/>）。</summary>
        public void SpendEnergy(int amount)
        {
            if (amount > 0)
            {
                Energy = Energy - amount < 0 ? 0 : Energy - amount;
            }
        }

        public void Reset()
        {
            Energy = 0;
            Gold = 0;
        }
    }
}
