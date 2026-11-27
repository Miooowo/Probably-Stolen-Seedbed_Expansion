using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuestMaidenCorrupt
{
    static class CommonSense
    {
        public const string Name = "常识改变";
        public const string SpokenLine = "嗯……啊……，请问主人……，有什么需要呢？";
        const string BuyId = "commonSenseBuying";
        const string SellId = "commonSenseSelling";
        const int Percent = 35;

        static StoreClient marked;
        static bool busy;

        public static void Grant()
        {
            try
            {
                StoreClient client = Current();
                if (client == null)
                {
                    Debug.LogWarning("[苗床顾客侵蚀] 没有当前顾客，常识改变没有加上");
                    return;
                }

                bool fresh = !ReferenceEquals(marked, client);
                marked = client;
                Refresh();
                VisitGrid(OnClient);
                if (fresh)
                {
                    Say(client);
                }

                Debug.Log("[苗床顾客侵蚀] 常识改变：买入价 -" + Percent + "%，卖出价 +" + Percent + "%，可与香水叠加");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变失败: " + ex.Message);
            }
        }

        public static void End()
        {
            if (marked == null || busy)
            {
                return;
            }

            marked = null;
            busy = true;
            try
            {
                Refresh();
                VisitGrid(OnClient);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变结束失败: " + ex.Message);
            }
            finally
            {
                busy = false;
            }
        }

        public static void OnClient(GameItem item)
        {
            try
            {
                if (item == null)
                {
                    return;
                }

                if (!Active() || GeneralHelper.IsItemOwned(item))
                {
                    if (!Active())
                    {
                        Strip(item);
                    }

                    return;
                }

                item.RemoveFeatureByCategory(SellId);
                item.AddItemFeature(Make(BuyId, -Percent));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变（收购）失败: " + ex.Message);
            }
        }

        public static void OnWeighted(GameItem item)
        {
            try
            {
                if (item == null)
                {
                    return;
                }

                if (!Active())
                {
                    Strip(item);
                    return;
                }

                if (GeneralHelper.IsItemOwned(item))
                {
                    item.RemoveFeatureByCategory(BuyId);
                    item.AddItemFeature(Make(SellId, Percent));
                    return;
                }

                item.RemoveFeatureByCategory(SellId);
                item.AddItemFeature(Make(BuyId, -Percent));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变（出售）失败: " + ex.Message);
            }
        }

        public static bool Active()
        {
            StoreClient client = Current();
            return client != null && ReferenceEquals(client, marked);
        }

        public static bool TryLabeledWater(GameItem item, out int purity, out string display)
        {
            purity = 0;
            display = null;
            if (item == null || !item.IsTag("LIQUID_CONTAINER_TAG"))
            {
                return false;
            }

            ItemFeature feature = WaterFeatureHelper.GetWaterFeature(item);
            if (feature == null)
            {
                return false;
            }

            purity = feature.IsExposed() ? WaterHelper.GetWaterPurity(item) : feature.customIntValue1;
            ItemCondition condition = feature.GetCurrentItemCondition();
            if (!feature.IsExposed() && purity <= 0 && condition != null && condition.customValue > 0)
            {
                purity = condition.customValue;
            }

            display = WaterFeatureHelper.GetWaterDisplayName(purity);
            if (condition != null && !string.IsNullOrEmpty(condition.display))
            {
                display = condition.display;
            }

            return true;
        }

        static void Say(StoreClient client)
        {
            try
            {
                DialogUIManager ui = DialogUIManager.Instance;
                if (ui == null)
                {
                    return;
                }

                string speaker = string.IsNullOrEmpty(client.displayName) ? "CLIENT" : client.displayName;
                Dialogue dialogue = new Dialogue();
                dialogue.SetText(speaker, SpokenLine);
                dialogue.priority = Dialogue.IMPORTANT_PRIORITY;
                ui.StartDialogue(dialogue, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变台词失败: " + ex.Message);
            }
        }

        static StoreClient Current()
        {
            PlayerStore store = PlayerStore.Instance;
            StoreClientInstance visit = store == null ? null : store.currentClientInstance;
            return visit == null ? null : visit.GetClientBlueprint();
        }

        static ItemFeature Make(string id, int percent)
        {
            ItemFeature feature = new ItemFeature();
            feature.identifier = id;
            feature.category = id;
            feature.valueStage = (ItemFeature.ValueStage)1;
            feature.SetPublicDisplay(Name);
            feature.SetActualDisplay(Name);
            feature.isFeatureMatch = true;
            feature.SetPreExposedValueMod(percent, true);
            return feature;
        }

        static void Strip(GameItem item)
        {
            item.RemoveFeatureByCategory(BuyId);
            item.RemoveFeatureByCategory(SellId);
        }

        static void Refresh()
        {
            PlayerStore store = PlayerStore.Instance;
            if (store != null)
            {
                store.RefreshCounterItem();
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
            if (!(grid is GameInventory inventory))
            {
                return;
            }

            IEnumerable items = inventory.childItems;

            if (items == null)
            {
                return;
            }

            var copy = new List<GameItem>();
            foreach (object entry in items)
            {
                if (entry is GameItem item)
                {
                    copy.Add(item);
                }
            }

            for (int i = 0; i < copy.Count; i++)
            {
                act(copy[i]);
            }
        }
    }
}
