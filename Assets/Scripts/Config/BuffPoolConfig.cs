using UnityEngine;

namespace IdleTafang.Config
{
    /// <summary>F3：强化池稀有度权重（条目列表仍在 <see cref="Gameplay.IntermissionBuffCatalog"/>）。</summary>
    [CreateAssetMenu(menuName = "IdleTafang/Buff Pool Config", fileName = "BuffPoolConfig")]
    public sealed class BuffPoolConfig : ScriptableObject
    {
        [SerializeField] private int commonPickWeight = 60;
        [SerializeField] private int rarePickWeight = 30;
        [SerializeField] private int epicPickWeight = 10;

        public BuffPoolWeights Weights => new BuffPoolWeights(commonPickWeight, rarePickWeight, epicPickWeight);
    }
}
