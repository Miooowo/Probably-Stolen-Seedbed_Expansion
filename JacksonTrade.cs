using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MidnightPawn.Game;
using UnityEngine;

namespace GuestMaidenCorrupt
{
    // 杰克逊博士苗床：与他买卖、或在他店里消费，每 1000 信用点 1 级，最高 99。
    // 每级：从他手里买 -1%，卖给他 +1%。
    static class JacksonTrade
    {
        public const string Marker = "JACKSON_MAIDEN";
        const string Traded = "JACKSON_TRADED";
        const string BuyId = "jacksonMaidenBuying";
        const string SellId = "jacksonMaidenSelling";
        public const int Step = 1000;
        public const int MaxLevel = 99;

        public static bool IsJackson(StoreClient client)
        {
            if (client == null)
            {
                return false;
            }

            string id = client.identifier ?? "";
            if (id == "inventor" || id == "inventorStorage" || id == "inventorIntro" || id == "inventorModuleIntro")
            {
                return true;
            }

            string sprite = client.spriteName ?? "";
            if (sprite.Equals("inventor", StringComparison.OrdinalIgnoreCase)
                || sprite.IndexOf("jackson", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string called = client.displayName ?? "";
            return called.IndexOf("杰克逊", StringComparison.Ordinal) >= 0;
        }

        public static bool IsMaiden(GameItem item)
        {
            if (item == null || !item.IsTag("MAIDEN_FEMALE"))
            {
                return false;
            }

            if (item.IsTag(Marker))
            {
                return true;
            }

            string name = ItemFactory.GetStr(item, "MAIDEN_NAME");
            string key = ItemFactory.GetStr(item, "MAIDEN_IDENTITY_KEY");
            string job = ItemFactory.GetStr(item, "MAIDEN_JOB");
            bool match = (!string.IsNullOrEmpty(name) && name.IndexOf("杰克逊", StringComparison.Ordinal) >= 0)
                || (!string.IsNullOrEmpty(key) && key.StartsWith("special:inventor", StringComparison.Ordinal))
                || (!string.IsNullOrEmpty(job) && job.StartsWith("inventor", StringComparison.Ordinal));
            if (!match)
            {
                return false;
            }

            item.EnableTag(Marker);
            return true;
        }

        public static void Keep(GameItem maiden, GameItem source)
        {
            if (maiden == null || source == null || !source.IsTag(Marker))
            {
                return;
            }

            maiden.EnableTag(Marker);
            ItemFactory.SetInt(maiden, Traded, ItemFactory.GetInt(source, Traded));
        }

        public static void WriteNote(RichTextBuilder builder, GameItem item)
        {
            if (builder == null || item == null || !IsMaiden(item))
            {
                return;
            }

            int traded = Math.Max(0, ItemFactory.GetInt(item, Traded));
            int level = LevelOf(traded);
            string line = "杰克逊交易 Lv." + level + "/" + MaxLevel;
            if (level < MaxLevel)
            {
                line += "，再 " + (Step - traded % Step) + " 点升级";
            }

            builder.AddLine(
                line,
                true,
                RenderHandler.ColorPalette.LightOrange,
                false,
                false,
                false,
                false,
                (RenderHandler.ColorPalette)(-1),
                (RenderHandler.ColorPalette)(-1),
                (RenderHandler.ColorPalette)(-1));
        }

        public static void Touch(GameItem item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                item.RemoveFeatureByCategory(BuyId);
                item.RemoveFeatureByCategory(SellId);
                int level = ActiveLevel();
                if (level <= 0)
                {
                    return;
                }

                bool owned = GeneralHelper.IsItemOwned(item);
                item.AddItemFeature(Make(owned ? SellId : BuyId, owned ? level : -level));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 杰克逊交易价失败: " + ex.Message);
            }
        }

        public static void Clear(GameItem item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                item.RemoveFeatureByCategory(BuyId);
                item.RemoveFeatureByCategory(SellId);
            }
            catch
            {
            }
        }

        public static int Credits(GameItem item, bool selling)
        {
            if (item == null)
            {
                return 0;
            }

            int value;
            try
            {
                value = (int)item.GetNegociatedValue();
            }
            catch
            {
                return 0;
            }

            if (value <= 0)
            {
                return 0;
            }

            if (!selling)
            {
                return value;
            }

            try
            {
                PlayerStore store = PlayerStore.Instance;
                StoreClientInstance visit = store == null ? null : store.currentClientInstance;
                StoreClient client = visit == null ? null : visit.GetClientBlueprint();
                if (client != null && client.useClientBudget && client.GetBudget() < value)
                {
                    return Math.Max(0, client.GetBudget());
                }
            }
            catch
            {
            }

            return value;
        }

        public static void ApplyCounters()
        {
            try
            {
                Each(EmporiumEntry.Instance == null ? null : EmporiumEntry.Instance.backInvinvElement);
                Each(EmporiumEntry.Instance == null ? null : EmporiumEntry.Instance.frontInvinvElement);
                VisitGrid(Touch);
                if (PlayerStore.Instance != null)
                {
                    PlayerStore.Instance.RefreshCounterItem();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 刷新杰克逊交易价失败: " + ex.Message);
            }
        }

        public static void AddTrade(int credits)
        {
            if (credits <= 0 || !DealingWithJackson())
            {
                return;
            }

            GameItem maiden = FindMaiden();
            if (maiden == null)
            {
                return;
            }

            int before = LevelOf(ItemFactory.GetInt(maiden, Traded));
            int traded = ItemFactory.GetInt(maiden, Traded) + credits;
            if (traded < 0)
            {
                traded = int.MaxValue;
            }

            ItemFactory.SetInt(maiden, Traded, traded);
            int after = LevelOf(traded);
            Debug.Log("[苗床顾客侵蚀] 杰克逊交易 +" + credits + "，Lv." + after);
            if (after != before)
            {
                ApplyCounters();
            }
        }

        static int ActiveLevel()
        {
            if (!DealingWithJackson())
            {
                return 0;
            }

            GameItem maiden = FindMaiden();
            return maiden == null ? 0 : LevelOf(ItemFactory.GetInt(maiden, Traded));
        }

        static bool DealingWithJackson()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                StoreClientInstance visit = store == null ? null : store.currentClientInstance;
                StoreClient client = visit == null ? null : visit.GetClientBlueprint();
                return IsJackson(client);
            }
            catch
            {
                return false;
            }
        }

        static GameItem FindMaiden()
        {
            try
            {
                List<GameItem> owned = ItemLife.AllOwned();
                if (owned == null)
                {
                    return null;
                }

                GameItem best = null;
                int bestTraded = -1;
                for (int i = 0; i < owned.Count; i++)
                {
                    GameItem item = owned[i];
                    if (!IsMaiden(item))
                    {
                        continue;
                    }

                    int traded = ItemFactory.GetInt(item, Traded);
                    if (traded > bestTraded)
                    {
                        best = item;
                        bestTraded = traded;
                    }
                }

                return best;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 寻找杰克逊苗床失败: " + ex.Message);
                return null;
            }
        }

        static int LevelOf(int traded)
        {
            if (traded <= 0)
            {
                return 0;
            }

            int level = traded / Step;
            return level > MaxLevel ? MaxLevel : level;
        }

        static ItemFeature Make(string id, int percent)
        {
            ItemFeature feature = new ItemFeature();
            feature.identifier = id;
            feature.category = id;
            feature.valueStage = (ItemFeature.ValueStage)1;
            feature.SetPublicDisplay("熟客");
            feature.SetActualDisplay("熟客");
            feature.isFeatureMatch = true;
            feature.featureType = percent < 0
                ? ItemFeature.FeatureType.TemporaryBuying
                : ItemFeature.FeatureType.TemporarySelling;
            feature.SetPreExposedValueMod(percent, true);
            return feature;
        }

        static void Each(GameInventory inventory)
        {
            if (inventory == null || inventory.childItems == null)
            {
                return;
            }

            var copy = new List<GameItem>();
            foreach (object entry in (IEnumerable)inventory.childItems)
            {
                if (entry is GameItem item)
                {
                    copy.Add(item);
                }
            }

            for (int i = 0; i < copy.Count; i++)
            {
                Touch(copy[i]);
            }
        }

        static void VisitGrid(Action<GameItem> act)
        {
            PlayerStore store = PlayerStore.Instance;
            StoreClientInstance visit = store == null ? null : store.currentClientInstance;
            if (visit == null)
            {
                return;
            }

            object grid = ModReflect.Get(visit, "gridInv");
            if (grid is GameInventory inventory)
            {
                Each(inventory);
            }
        }
    }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.OnAddedToClientArea))]
    static class JacksonClientPricePatch
    {
        static void Postfix(GameItem __instance)
        {
            JacksonTrade.Touch(__instance);
        }
    }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.OnAddedToWeightedArea))]
    static class JacksonWeightedPricePatch
    {
        static void Postfix(GameItem __instance)
        {
            JacksonTrade.Touch(__instance);
        }
    }

    [HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.ActivateClientOutsideStore))]
    static class JacksonShopOpenPatch
    {
        static void Postfix()
        {
            JacksonTrade.ApplyCounters();
        }
    }

    [HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.DeactivateClientOutsideStore))]
    static class JacksonShopClosePatch
    {
        static void Postfix()
        {
            JacksonTrade.ApplyCounters();
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.DismissCurrentClient))]
    static class JacksonDismissPricePatch
    {
        static void Postfix()
        {
            JacksonTrade.ApplyCounters();
        }
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.CurrentClientLeave))]
    static class JacksonLeavePricePatch
    {
        static void Postfix()
        {
            JacksonTrade.ApplyCounters();
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.BuyItem))]
    static class JacksonBuyXpPatch
    {
        static void Prefix(GameItem item)
        {
            JacksonTrade.AddTrade(JacksonTrade.Credits(item, false));
        }

        static void Postfix(GameItem item)
        {
            JacksonTrade.Clear(item);
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.SellItem))]
    static class JacksonSellXpPatch
    {
        static void Prefix(GameItem item)
        {
            JacksonTrade.AddTrade(JacksonTrade.Credits(item, true));
        }

        static void Postfix(GameItem item)
        {
            JacksonTrade.Clear(item);
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.BuyAllItemFromTable))]
    static class JacksonBuyAllXpPatch
    {
        static void Prefix()
        {
            JacksonTrade.AddTrade(CounterTotal());
        }

        static int CounterTotal()
        {
            if (PlayerStore.Instance == null || !PlayerStore.Instance.CanBuyItemFromTable())
            {
                return 0;
            }

            return SumBackCounter();
        }

        static int SumBackCounter()
        {
            if (EmporiumEntry.Instance == null || EmporiumEntry.Instance.backInvinvElement == null)
            {
                return 0;
            }

            int sum = 0;
            foreach (object entry in (IEnumerable)EmporiumEntry.Instance.backInvinvElement.childItems)
            {
                if (entry is GameItem item)
                {
                    sum += (int)item.GetNegociatedValue();
                }
            }

            return sum;
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.SellAllItemFromTable))]
    static class JacksonSellAllXpPatch
    {
        static void Prefix()
        {
            if (PlayerStore.Instance == null || !PlayerStore.Instance.CanSellItemFromTable())
            {
                return;
            }

            if (EmporiumEntry.Instance == null || EmporiumEntry.Instance.backInvinvElement == null)
            {
                return;
            }

            int sum = 0;
            foreach (object entry in (IEnumerable)EmporiumEntry.Instance.backInvinvElement.childItems)
            {
                if (entry is GameItem item)
                {
                    sum += (int)item.GetNegociatedValue();
                }
            }

            JacksonTrade.AddTrade(sum);
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.RemovedItemToClientArea))]
    static class JacksonLeaveClientPatch
    {
        static void Postfix(GameItem gameItem)
        {
            JacksonTrade.Clear(gameItem);
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.RemovedItemToWeightedArea))]
    static class JacksonLeaveWeightedPatch
    {
        static void Postfix(GameItem gameItem)
        {
            JacksonTrade.Clear(gameItem);
        }
    }
}
