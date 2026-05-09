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
