using IdleTafang.Gameplay;
using UnityEngine;

namespace IdleTafang.Config
{
    /// <summary>F3：波间强化稀有度权重（用于从无放回池中抽样）。</summary>
    public readonly struct BuffPoolWeights
    {
        public static BuffPoolWeights Default => new BuffPoolWeights(60, 30, 10);

        public int Common { get; }
        public int Rare { get; }
        public int Epic { get; }

        public BuffPoolWeights(int common, int rare, int epic)
        {
            Common = Mathf.Max(0, common);
            Rare = Mathf.Max(0, rare);
            Epic = Mathf.Max(0, epic);
        }

        public int WeightFor(IntermissionBuffRarity rarity)
        {
            int w = rarity switch
            {
                IntermissionBuffRarity.Common => Common,
                IntermissionBuffRarity.Rare => Rare,
                IntermissionBuffRarity.Epic => Epic,
                _ => 1
            };

            return w <= 0 ? 1 : w;
        }
    }
}
