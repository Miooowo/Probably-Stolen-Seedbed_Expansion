using System;

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

    public static class MaidenOutput
    {
        public const int DayPercent = 20;
        public const string UncappedId = "system_uncapped_neural_core";
        public const string CappedId = "system_capped_neural_core";
        public const string RuinedId = "system_module_ruined";
        public const string CorruptId = "system_module_corrupt";

        public static readonly string[] NormalIds =
        {
            "system_module_performance",
            "system_module_efficiancy",
            "system_module_quality",
            "system_module_overclock",
            "system_module_eco",
            "system_module_fineness",
            "system_module_streamlining"
        };

        public struct Shares
        {
            public int Uncapped;
            public int Capped;
            public int Ruined;
            public int Corrupt;
            public int Normal;
        }

        public static int UncappedPercent(int neuralFeeds)
        {
            if (neuralFeeds < 0)
            {
                neuralFeeds = 0;
            }

            int percent = neuralFeeds * 5;
            return percent > 50 ? 50 : percent;
        }

        public static int LowShare(int electronicFeeds)
        {
            if (electronicFeeds < 0)
            {
                electronicFeeds = 0;
            }

            int share = 80 - electronicFeeds * 10;
            return share < 0 ? 0 : share;
        }

        public static Shares Weight(int electronicFeeds, int neuralFeeds)
        {
            int uncapped = UncappedPercent(neuralFeeds);
            int rest = 100 - uncapped - 5;
            int low = rest * LowShare(electronicFeeds) / 100;
            int ruined = low / 2;
            return new Shares
            {
                Uncapped = uncapped,
                Capped = 5,
                Ruined = ruined,
                Corrupt = low - ruined,
                Normal = rest - low
            };
        }

        public static string Pick(Shares shares, int roll, int normalIndex)
        {
            if (roll < 0)
            {
                roll = 0;
            }

            if (roll > 99)
            {
                roll = 99;
            }

            int cursor = shares.Uncapped;
            if (roll < cursor)
            {
                return UncappedId;
            }

            cursor += shares.Capped;
            if (roll < cursor)
            {
                return CappedId;
            }

            cursor += shares.Ruined;
            if (roll < cursor)
            {
                return RuinedId;
            }

            cursor += shares.Corrupt;
            if (roll < cursor)
            {
                return CorruptId;
            }

            if (NormalIds.Length == 0)
            {
                return null;
            }

            if (normalIndex < 0)
            {
                normalIndex = 0;
            }

            return NormalIds[normalIndex % NormalIds.Length];
        }
    }
}
