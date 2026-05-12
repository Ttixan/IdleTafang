using IdleTafang.Gameplay.Resources;

namespace IdleTafang.Gameplay.Builds
{
    public sealed class BuildService
    {
        public bool TryBuild(BuildPrototype prototype, ResourceWallet wallet)
        {
            if (prototype == null || wallet == null)
            {
                return false;
            }

            if (wallet.Energy < prototype.EnergyCost)
            {
                return false;
            }

            wallet.SpendEnergy(prototype.EnergyCost);
            prototype.Upgrade();
            return true;
        }
    }
}