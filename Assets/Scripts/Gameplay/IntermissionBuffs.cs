using System;
using UnityEngine;

namespace IdleTafang.Gameplay
{
    public enum IntermissionBuffCategory
    {
        Sector,
        Turret,
        Defense
    }

    public enum IntermissionBuffRarity
    {
        Common,
        Rare,
        Epic
    }

    public enum IntermissionBuffEffectKind
    {
        /// <summary>扇区自动炮伤害每层 +10%（乘法：(1 + 0.1×层数)×基础伤害）。</summary>
        SectorDamageTenPercentStack,

        /// <summary>手炮特攻 Energy 消耗每层 -1（不低于 0）。</summary>
        SpecialEnergyMinusOneStack,

        /// <summary>漏怪每层少扣 1 点基地血（不低于 0）。</summary>
        LeakDamageMinusOneStack
    }

    /// <summary>波间商店单条强化报价（E4）。</summary>
    public readonly struct IntermissionBuffOffer : IEquatable<IntermissionBuffOffer>
    {
        public IntermissionBuffOffer(
            string id,
            string displayName,
            IntermissionBuffCategory category,
            IntermissionBuffRarity rarity,
            int energyCost,
            IntermissionBuffEffectKind effectKind)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Category = category;
            Rarity = rarity;
            EnergyCost = Mathf.Max(0, energyCost);
            EffectKind = effectKind;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IntermissionBuffCategory Category { get; }
        public IntermissionBuffRarity Rarity { get; }
        public int EnergyCost { get; }
        public IntermissionBuffEffectKind EffectKind { get; }

        public bool Equals(IntermissionBuffOffer other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is IntermissionBuffOffer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id != null ? Id.GetHashCode() : 0;
        }
    }

    /// <summary>默认强化条目与抽取权重（E5/E6 最小池）。</summary>
    public static class IntermissionBuffCatalog
    {
        /// <summary>第一版三种示例强化（各类型一条）。</summary>
        public static readonly IntermissionBuffOffer[] All =
        {
            new IntermissionBuffOffer(
                "sector_sharp",
                "Sharpened sectors (+10% sector dmg)",
                IntermissionBuffCategory.Sector,
                IntermissionBuffRarity.Common,
                5,
                IntermissionBuffEffectKind.SectorDamageTenPercentStack),
            new IntermissionBuffOffer(
                "turret_efficiency",
                "Efficient specials (-1 special Energy)",
                IntermissionBuffCategory.Turret,
                IntermissionBuffRarity.Rare,
                10,
                IntermissionBuffEffectKind.SpecialEnergyMinusOneStack),
            new IntermissionBuffOffer(
                "base_padding",
                "Reinforced plating (-1 leak damage)",
                IntermissionBuffCategory.Defense,
                IntermissionBuffRarity.Epic,
                15,
                IntermissionBuffEffectKind.LeakDamageMinusOneStack)
        };

        public static int RarityPickWeight(IntermissionBuffRarity rarity)
        {
            return rarity switch
            {
                IntermissionBuffRarity.Common => 60,
                IntermissionBuffRarity.Rare => 30,
                IntermissionBuffRarity.Epic => 10,
                _ => 1
            };
        }

        /// <summary>
        /// 将 <see cref="All"/> 拷贝到 <paramref name="dst"/> 并随机打乱展示顺序（三条互不重复）。
        /// 后续可改为带放回权重抽取（E5）。
        /// </summary>
        public static void FillThreeShuffledOffers(System.Random rng, IntermissionBuffOffer[] dst)
        {
            if (rng == null || dst == null || All.Length == 0)
            {
                return;
            }

            int n = Mathf.Min(dst.Length, All.Length);
            for (int i = 0; i < n; i++)
            {
                dst[i] = All[i];
            }

            ShuffleOfferOrder(rng, dst, n);
        }

        /// <summary>洗牌展示顺序（仅打乱前 <paramref name="count"/> 项）。</summary>
        public static void ShuffleOfferOrder(System.Random rng, IntermissionBuffOffer[] offers, int count)
        {
            if (rng == null || offers == null || count <= 1)
            {
                return;
            }

            int limit = Mathf.Min(count, offers.Length);
            for (int i = limit - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (offers[i], offers[j]) = (offers[j], offers[i]);
            }
        }
    }

    /// <summary>整局叠加的强化层数（E5）。</summary>
    public sealed class RunBuffState
    {
        public int SectorDamageTenPercentStacks { get; private set; }
        public int SpecialEnergyMinusStacks { get; private set; }
        public int LeakDamageMinusStacks { get; private set; }

        /// <summary>扇区投射伤害倍率。</summary>
        public float SectorProjectileDamageMultiplier => 1f + 0.1f * SectorDamageTenPercentStacks;

        public void Reset()
        {
            SectorDamageTenPercentStacks = 0;
            SpecialEnergyMinusStacks = 0;
            LeakDamageMinusStacks = 0;
        }

        public void ApplyOffer(in IntermissionBuffOffer offer)
        {
            switch (offer.EffectKind)
            {
                case IntermissionBuffEffectKind.SectorDamageTenPercentStack:
                    SectorDamageTenPercentStacks += 1;
                    break;
                case IntermissionBuffEffectKind.SpecialEnergyMinusOneStack:
                    SpecialEnergyMinusStacks += 1;
                    break;
                case IntermissionBuffEffectKind.LeakDamageMinusOneStack:
                    LeakDamageMinusStacks += 1;
                    break;
            }
        }

        public int DiscountSpecialEnergyCost(int baseCost)
        {
            return Mathf.Max(0, baseCost - SpecialEnergyMinusStacks);
        }

        public string BuildSummaryLine()
        {
            return
                $"Buffs: Sector×{SectorProjectileDamageMultiplier:0.##} | SpecE-{SpecialEnergyMinusStacks} | Leak-{LeakDamageMinusStacks}";
        }
    }
}
