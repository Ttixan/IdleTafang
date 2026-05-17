using System;
using UnityEngine;

namespace IdleTafang.Config
{
    /// <summary>F1/F2/F5：单局可调参数（波次、扇区、修基地、刷怪）；挂在 GameBootstrap 或 RunHud 上引用同一资产即可。</summary>
    [CreateAssetMenu(menuName = "IdleTafang/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        [Serializable]
        public struct KillEnergyByEnemyType
        {
            public int enemyTypeId;
            public int energy;
        }

        [Header("Run")]
        [SerializeField] [Min(1)] private int maxWaves = 5;

        [Header("Energy")]
        [SerializeField] [Min(0)] private int startingEnergy = 20;
        [Tooltip("表中未出现的 enemyTypeId 使用本默认值。")]
        [SerializeField] [Min(0)] private int defaultKillEnergy = 1;
        [SerializeField] private KillEnergyByEnemyType[] killEnergyByEnemyType = new KillEnergyByEnemyType[0];
        [Tooltip("索引 0 = 第 1 波；长度不足末项沿用；为空则倍率为 1。")]
        [SerializeField] private float[] waveKillEnergyMultipliers = new[] { 1f };

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

        public int StartingEnergy => startingEnergy;

        /// <param name="enemyTypeId">敌人预制体上的类型 ID。</param>
        /// <param name="waveNumber">当前战斗波次，从 1 开始。</param>
        public int GetKillEnergyReward(int enemyTypeId, int waveNumber)
        {
            int baseEnergy = Mathf.Max(0, defaultKillEnergy);
            if (killEnergyByEnemyType != null)
            {
                for (int i = 0; i < killEnergyByEnemyType.Length; i++)
                {
                    if (killEnergyByEnemyType[i].enemyTypeId == enemyTypeId)
                    {
                        baseEnergy = Mathf.Max(0, killEnergyByEnemyType[i].energy);
                        break;
                    }
                }
            }

            float mult = 1f;
            if (waveKillEnergyMultipliers != null && waveKillEnergyMultipliers.Length > 0)
            {
                int idx = Mathf.Max(0, waveNumber - 1);
                if (idx >= waveKillEnergyMultipliers.Length)
                {
                    idx = waveKillEnergyMultipliers.Length - 1;
                }

                mult = waveKillEnergyMultipliers[idx];
            }

            return Mathf.Max(0, Mathf.RoundToInt(baseEnergy * Mathf.Max(0f, mult)));
        }
    }
}
