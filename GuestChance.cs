using System;
using System.Collections.Generic;

namespace GuestMaidenCorrupt
{
    public static class GuestChance
    {
        public const int FedHalf = 30;
        public const int Full = 200;

        public static int Of(int reputation, int affection, int luck, bool fed = false, int trust = 0)
        {
            int half = reputation * 2 + affection + luck + trust + (fed ? FedHalf : 0);
            if (half < 0)
            {
                return 0;
            }

            if (half > Full)
            {
                return Full;
            }

            return half;
        }

        public static bool Succeeds(int halfPercent, int roll)
        {
            return roll >= 0 && roll < halfPercent;
        }

        public static string Format(int halfPercent)
        {
            int whole = halfPercent / 2;
            if ((halfPercent & 1) == 0)
            {
                return whole + "%";
            }

            return whole + ".5%";
        }
    }

    public static class FailureCost
    {
        public const double RepFactor = 0.67;

        public static int Crime(bool semenFed)
        {
            if (!semenFed)
            {
                return 20;
            }

            return (int)Math.Round(20 * 0.85, MidpointRounding.AwayFromZero);
        }

        public static double ReputationLoss(bool semenFed, bool intimidating)
        {
            double loss = 1;
            if (semenFed)
            {
                loss *= RepFactor;
            }

            if (intimidating)
            {
                loss *= RepFactor;
            }

            return loss;
        }
    }

    public static class MaidenSellPrice
    {
        public static long Adjust(long written, long mainModPrice, int day)
        {
            long price = written > 0 ? written : mainModPrice;
            if (price <= 0 || day >= 14)
            {
                return price < 0 ? 0 : price;
            }

            price = (long)Math.Round(price * 0.9, MidpointRounding.AwayFromZero);
            return price < 1 ? 1 : price;
        }
    }

    // 电子苗床产出模型（梯度式，无概率）。
    //   核心目的：喂电子零件(common_electronic) → 解锁「限制AI芯片」(capped)；
    //             喂「限制AI芯片」(capped) → 额外产出「不限制AI芯片」(uncapped，更高级)。
    //   硬门槛：喂满对应物品才启动；每苗床各自记一个 tick，每 2 天结算一次。
    public static class MaidenOutput
    {
        public const string CappedId = "system_capped_neural_core";     // 限制AI芯片
        public const string UncappedId = "system_uncapped_neural_core"; // 不限制AI芯片

        public const int PartsThreshold = 5;   // 电子零件硬门槛（喂满 5 个）
        public const int CoresThreshold = 5;   // 限制AI芯片硬门槛（喂满 5 个）
        public const int Interval = 2;          // 每 2 天（每苗床自记 tick）

        // 第 tick 夜应产出的物品 id（硬门槛 + 周期，无随机）。
        public static List<string> Due(int parts, int cores, int tick)
        {
            var due = new List<string>();
            if (tick <= 0 || tick % Interval != 0)
            {
                return due;
            }

            if (parts >= PartsThreshold)
            {
                due.Add(CappedId);
            }

            if (cores >= CoresThreshold)
            {
                due.Add(UncappedId);
            }

            return due;
        }
    }
}
