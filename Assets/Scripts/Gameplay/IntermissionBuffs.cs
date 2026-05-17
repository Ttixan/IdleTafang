using System;
using System.Collections.Generic;
using System.Text;
using IdleTafang.Config;
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
            return BuffPoolWeights.Default.WeightFor(rarity);
        }

        /// <summary>
        /// 按稀有度权重从无放回池中抽满 <paramref name="dst"/> 前半段，再打乱展示顺序（F3）。
        /// </summary>
        public static void FillThreeOffers(
            System.Random rng,
            IntermissionBuffOffer[] dst,
            BuffPoolWeights weights,
            IntermissionBuffOffer[] pool = null)
        {
            pool ??= All;
            if (rng == null || dst == null || pool.Length == 0)
            {
                return;
            }

            int pickCount = Mathf.Min(dst.Length, pool.Length);
            var available = new List<(IntermissionBuffOffer offer, int w)>(pool.Length);
            for (int i = 0; i < pool.Length; i++)
            {
                IntermissionBuffOffer o = pool[i];
                available.Add((o, weights.WeightFor(o.Rarity)));
            }

            for (int slot = 0; slot < pickCount; slot++)
            {
                int sum = 0;
                for (int i = 0; i < available.Count; i++)
                {
                    sum += available[i].w;
                }

                if (sum <= 0)
                {
                    sum = available.Count;
                    for (int i = 0; i < available.Count; i++)
                    {
                        available[i] = (available[i].offer, 1);
                    }
                }

                int roll = rng.Next(sum);
                int acc = 0;
                int chosen = 0;
                for (int i = 0; i < available.Count; i++)
                {
                    acc += available[i].w;
                    if (roll < acc)
                    {
                        chosen = i;
                        break;
                    }
                }

                dst[slot] = available[chosen].offer;
                available.RemoveAt(chosen);
            }

            ShuffleOfferOrder(rng, dst, pickCount);
        }

        /// <summary>等价于 <see cref="FillThreeOffers"/> + 默认 60/30/10 权重。</summary>
        public static void FillThreeShuffledOffers(System.Random rng, IntermissionBuffOffer[] dst)
        {
            FillThreeOffers(rng, dst, BuffPoolWeights.Default);
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

    /// <summary>整局叠加的强化层数（E5）；并记录每次波间选购便于结算/HUD（E7）。</summary>
    public sealed class RunBuffState
    {
        private readonly List<string> purchaseHistory = new List<string>();

        public int SectorDamageTenPercentStacks { get; private set; }
        public int SpecialEnergyMinusStacks { get; private set; }
        public int LeakDamageMinusStacks { get; private set; }

        public IReadOnlyList<string> PurchaseHistory => purchaseHistory;

        public int PurchaseCount => purchaseHistory.Count;

        /// <summary>是否已有任意叠层（数值层面）。</summary>
        public bool HasAnyStacks =>
            SectorDamageTenPercentStacks > 0 || SpecialEnergyMinusStacks > 0 || LeakDamageMinusStacks > 0;

        /// <summary>扇区投射伤害倍率。</summary>
        public float SectorProjectileDamageMultiplier => 1f + 0.1f * SectorDamageTenPercentStacks;

        public void Reset()
        {
            SectorDamageTenPercentStacks = 0;
            SpecialEnergyMinusStacks = 0;
            LeakDamageMinusStacks = 0;
            purchaseHistory.Clear();
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

            purchaseHistory.Add($"{offer.DisplayName} [{offer.Rarity}, {offer.EnergyCost}E]");
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

        /// <summary>左侧 HUD 多行展示（E7）。</summary>
        public string BuildHudMultiline()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Intermission buffs (run)");
            sb.AppendLine(BuildStackLines());
            if (purchaseHistory.Count == 0)
            {
                sb.Append("Purchases: none");
            }
            else
            {
                sb.AppendLine("Purchases:");
                for (int i = 0; i < purchaseHistory.Count; i++)
                {
                    sb.Append("  ").Append(i + 1).Append(". ").AppendLine(purchaseHistory[i]);
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>结算面板用的强化摘要（E7）。</summary>
        public string BuildSettlementSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- Intermission build ---");
            sb.AppendLine(BuildStackLines());
            if (purchaseHistory.Count == 0)
            {
                sb.Append("Purchases: none");
            }
            else
            {
                sb.AppendLine("Purchases:");
                for (int i = 0; i < purchaseHistory.Count; i++)
                {
                    sb.Append("  ").Append(i + 1).Append(". ").AppendLine(purchaseHistory[i]);
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>波间面板底部携带状态一行。</summary>
        public string BuildCarriedFootnote()
        {
            string stacks = HasAnyStacks ? BuildSummaryLine() : "Stacks: none";
            string picks = PurchaseCount == 0 ? "Purchases: 0" : $"Purchases: {PurchaseCount}";
            return stacks + "\n" + picks;
        }

        private string BuildStackLines()
        {
            if (!HasAnyStacks)
            {
                return "Stacks: none";
            }

            var sb = new StringBuilder();
            sb.Append("Stacks:");
            bool first = true;

            void AppendSegment(string segment)
            {
                if (first)
                {
                    sb.Append(' ').Append(segment);
                    first = false;
                }
                else
                {
                    sb.Append(" | ").Append(segment);
                }
            }

            if (SectorDamageTenPercentStacks > 0)
            {
                AppendSegment($"Sector +10%x{SectorDamageTenPercentStacks} (×{SectorProjectileDamageMultiplier:0.##})");
            }

            if (SpecialEnergyMinusStacks > 0)
            {
                AppendSegment($"Spec {-SpecialEnergyMinusStacks}E");
            }

            if (LeakDamageMinusStacks > 0)
            {
                AppendSegment($"Leak -{LeakDamageMinusStacks}");
            }

            return sb.ToString();
        }
    }
}
