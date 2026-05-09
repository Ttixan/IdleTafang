namespace IdleTafang.Gameplay.Typing
{
    public sealed class TypingRewardCalculator
    {
        public int CalculateEnergyReward(TypingStats stats)
        {
            if (stats == null)
            {
                return 0;
            }

            int baseReward = stats.CorrectTyped;
            int comboBonus = stats.Combo / 2;
            return baseReward + comboBonus;
        }
    }
}
