using UnityEngine;

namespace IdleTafang.Config
{
    /// <summary>F1/F2/F5：单局可调参数（波次、扇区、修基地、刷怪）；挂在 GameBootstrap 或 RunHud 上引用同一资产即可。</summary>
    [CreateAssetMenu(menuName = "IdleTafang/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        [Header("Run")]
        [SerializeField] [Min(1)] private int maxWaves = 5;

        [Header("Sector (须与 CombatPath 扇区数一致)")]
        [SerializeField] [Min(1)] private int sectorCount = 3;
        [SerializeField] private float sectorSwitchWarmupSeconds = 0.5f;

        [Header("Wave / combat (CombatWaveManager)")]
        [SerializeField] private float enemySpawnIntervalSeconds = 2f;
        [SerializeField] [Min(1)] private int enemiesPerWave = 12;
        [SerializeField] [Min(1)] private int startingBaseHealth = 5;
        [SerializeField] [Min(0)] private int enemyDamageOnReach = 1;
        [SerializeField] private float enemyHpMultiplier = 1f;

        [Header("Intermission repair")]
        [SerializeField] [Min(1)] private int repairBaseEnergyCost = 10;
        [SerializeField] [Min(1)] private int repairBaseHealAmount = 1;

        [Header("Misc")]
        [Tooltip("文档中的波间缓冲秒数；当前推进仍为手动 Continue，仅作文案/后续自动化参考。")]
        [SerializeField] private float intermissionBufferSeconds = 5f;

        public int MaxWaves => maxWaves;
        public int SectorCount => sectorCount;
        public float SectorSwitchWarmupSeconds => sectorSwitchWarmupSeconds;
        public float EnemySpawnIntervalSeconds => enemySpawnIntervalSeconds;
        public int EnemiesPerWave => enemiesPerWave;
        public int StartingBaseHealth => startingBaseHealth;
        public int EnemyDamageOnReach => enemyDamageOnReach;
        public float EnemyHpMultiplier => Mathf.Max(0.01f, enemyHpMultiplier);
        public int RepairBaseEnergyCost => repairBaseEnergyCost;
        public int RepairBaseHealAmount => repairBaseHealAmount;
        public float IntermissionBufferSeconds => intermissionBufferSeconds;
    }
}
