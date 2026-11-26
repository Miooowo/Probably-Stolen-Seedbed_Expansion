using System;
using MidnightPawn.Game;
using UnityEngine;

namespace GuestMaidenCorrupt
{
    static class ElectronicMaiden
    {
        public const string Marker = "ELECTRONIC_MAIDEN";
        const string Parts = "ELECTRONIC_MAIDEN_PARTS";
        const string Cores = "ELECTRONIC_MAIDEN_CORES";
        const string Tick = "ELECTRONIC_MAIDEN_TICK";
        const string ElectronicId = "common_electronic";

        // 抓获改造体时挂上标记 + 喂食回调。不再造容器窗口（产出改到巢穴产出区）。
        public static void Setup(GameItem maiden)
        {
            if (maiden == null)
            {
                return;
            }

            maiden.EnableTag(Marker);
            Bind(maiden);
        }

        public static void OnLoad(GameItem item)
        {
            if (item != null && item.IsTag(Marker))
            {
                Bind(item);
            }
        }

        // 每夜结算（由 TentacleBody.NightlyTick 后置调用）：梯度式产出模组到巢穴产出区。
        public static void Settle(GameItem maiden, GameGridInventory output)
        {
            if (maiden == null || output == null || !maiden.IsTag(Marker))
            {
                return;
            }

            int tick = Read(maiden, Tick) + 1;
            maiden.ModifyTag(Tick, s =>
            {
                s.Enable();
                s.SetInt(tick);
            }, false);

            foreach (string id in MaidenOutput.Due(Read(maiden, Parts), Read(maiden, Cores), tick))
            {
                try
                {
                    GameItem made = DirectoryMaster.Item(id, true);
                    if (made == null)
                    {
                        continue;
                    }

                    if (TentacleBody.PlaceInto(output, made))
                    {
                        Debug.Log("[苗床顾客侵蚀] 电子苗床产出 " + id);
                    }
                    else
                    {
                        made.Destroy();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[苗床顾客侵蚀] 电子苗床产出失败: " + ex.Message);
                }
            }
        }

        public static int PartsOf(GameItem item)
        {
            return Read(item, Parts);
        }

        public static int CoresOf(GameItem item)
        {
            return Read(item, Cores);
        }

        static void Bind(GameItem maiden)
        {
            maiden.mayItemTargetThisFunc = (dragged, _) => IsFeed(dragged);
            maiden.canItemTargetThisFunc = (dragged, _) => IsFeed(dragged);
            maiden.onItemTargetThisFunc = Consume;
        }

        static void Consume(GameItem dragged, GameItem maiden)
        {
            if (!IsFeed(dragged) || maiden == null || !maiden.IsTag(Marker))
            {
                return;
            }

            string tag;
            int threshold;
            if (dragged.identifier == ElectronicId)
            {
                tag = Parts;
                threshold = MaidenOutput.PartsThreshold;
            }
            else
            {
                tag = Cores;
                threshold = MaidenOutput.CoresThreshold;
            }

            if (Read(maiden, tag) >= threshold)
            {
                return;
            }

            Add(maiden, tag);

            try
            {
                dragged.Destroy();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 喂养消耗失败: " + ex.Message);
            }
        }

        static bool IsFeed(GameItem item)
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                if (!GeneralHelper.IsItemOwned(item))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return item.identifier == ElectronicId || item.identifier == MaidenOutput.CappedId;
        }

        static int Read(GameItem item, string tag)
        {
            if (item == null || !item.IsTag(tag))
            {
                return 0;
            }

            TagState state = item.GetTagReadonly(tag);
            return state == null ? 0 : state.GetInt();
        }

        static void Add(GameItem item, string tag)
        {
            int next = Read(item, tag) + 1;
            item.ModifyTag(tag, state =>
            {
                state.Enable();
                state.SetInt(next);
            }, false);
            Debug.Log("[苗床顾客侵蚀] 电子苗床喂养 " + tag + " " + next);
        }
    }
}
