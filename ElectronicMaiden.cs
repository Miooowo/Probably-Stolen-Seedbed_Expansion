using System;
using System.Collections;
using System.Collections.Generic;
using MidnightPawn.Game;
using UnityEngine;

namespace GuestMaidenCorrupt
{
    static class ElectronicMaiden
    {
        public const string Marker = "ELECTRONIC_MAIDEN";
        const string Parts = "ELECTRONIC_MAIDEN_PARTS";
        const string Cores = "ELECTRONIC_MAIDEN_CORES";
        const string ElectronicId = "common_electronic";

        public static void Attach(GameItem maiden)
        {
            if (maiden == null)
            {
                return;
            }

            GameGridInventory grid = new GameGridInventory(1, 1);
            PixelWindow window = MachineHelper.CreateMachineryWindow(
                true,
                MachineHelper.Panel("输出", grid, "output"));
            maiden.SetContentWindow(window);
            maiden.EnableTag("CONTAINER_TAG");
            maiden.EnableTag(Marker);
            grid.mayInventoryAddItemFunc = (_, __) => false;
            Bind(maiden);
        }

        public static void OnDay()
        {
            foreach (GameItem maiden in Find())
            {
                try
                {
                    Produce(maiden);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[苗床顾客侵蚀] 电子苗床产出失败: " + ex.Message);
                }
            }
        }

        public static void OnLoad(GameItem item)
        {
            if (item != null && item.IsTag(Marker))
            {
                Bind(item);
            }
        }

        static void Produce(GameItem maiden)
        {
            Bind(maiden);
            GameInventory grid = Output(maiden);
            if (grid == null || Occupied(grid))
            {
                return;
            }

            if (UnityEngine.Random.Range(0, 100) >= MaidenOutput.DayPercent)
            {
                return;
            }

            MaidenOutput.Shares shares = MaidenOutput.Weight(Read(maiden, Parts), Read(maiden, Cores));
            string id = MaidenOutput.Pick(
                shares,
                UnityEngine.Random.Range(0, 100),
                UnityEngine.Random.Range(0, MaidenOutput.NormalIds.Length));
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            GameItem made = DirectoryMaster.Item(id, true);
            if (made == null)
            {
                return;
            }

            Deposit(grid, made);
            Debug.Log("[苗床顾客侵蚀] 电子苗床产出 " + id);
        }

        static void Deposit(GameInventory grid, GameItem made)
        {
            grid.mayInventoryAddItemFunc = (item, _) => ReferenceEquals(item, made);
            try
            {
                if (!GraphUtils.CanAccept(grid, made))
                {
                    made.Destroy();
                    return;
                }

                GraphUtils.EmporiumTryAdd(grid, made);
            }
            finally
            {
                grid.mayInventoryAddItemFunc = (_, __) => false;
            }
        }

        static void Bind(GameItem maiden)
        {
            GameInventory grid = Output(maiden);
            if (grid != null)
            {
                grid.mayInventoryAddItemFunc = (_, __) => false;
            }

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

            if (dragged.identifier == ElectronicId)
            {
                Add(maiden, Parts);
            }
            else if (dragged.identifier == MaidenOutput.CappedId)
            {
                Add(maiden, Cores);
            }

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

        static GameInventory Output(GameItem maiden)
        {
            try
            {
                return MachineHelper.GetSlot(maiden, 0);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 读取电子苗床输出格失败: " + ex.Message);
                return null;
            }
        }

        static bool Occupied(GameInventory grid)
        {
            try
            {
                IEnumerable items = grid.childItems;
                if (items == null)
                {
                    return false;
                }

                foreach (object item in items)
                {
                    if (item != null)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 检查输出格失败: " + ex.Message);
                return true;
            }

            return false;
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

        static List<GameItem> Find()
        {
            var found = new List<GameItem>();
            var seen = new HashSet<GameItem>();
            try
            {
                if (EmporiumEntry.Instance != null)
                {
                    Collect(EmporiumEntry.Instance.GetAllItems(), found, seen);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 查找店铺里的电子苗床失败: " + ex.Message);
            }

            try
            {
                List<GameItem> bodies = TentacleBody.InstancesSnapshot();
                if (bodies == null)
                {
                    return found;
                }

                foreach (GameItem body in bodies)
                {
                    List<GameGridInventory> grids = body == null ? null : TentacleBody.GridsOf(body);
                    if (grids == null)
                    {
                        continue;
                    }

                    foreach (GameGridInventory grid in grids)
                    {
                        if (grid == null)
                        {
                            continue;
                        }

                        Collect(GraphUtils.FindAllChildrenType<GameItem>(grid), found, seen);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 查找触手本体里的电子苗床失败: " + ex.Message);
            }

            return found;
        }

        static void Collect(IEnumerable<GameItem> items, List<GameItem> found, HashSet<GameItem> seen)
        {
            if (items == null)
            {
                return;
            }

            foreach (GameItem item in items)
            {
                if (item != null && item.IsTag(Marker) && seen.Add(item))
                {
                    found.Add(item);
                }
            }
        }
    }
}
