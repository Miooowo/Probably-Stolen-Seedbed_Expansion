using System;
using HarmonyLib;
using MidnightPawn.Game;
using UnityEngine;

namespace GuestMaidenCorrupt
{
    // 主模组不给特殊客人做客人格。这里补一张可以拖上侵蚀性触手的卡。
    static class SpecialNpc
    {
        public const string Marker = "SPECIAL_NPC";

        static GameItem card;
        static PixelWindow window;
        static string shownId;
        static int refreshDepth;

        public static GameItem Current => card;

        public static void BeginRefresh()
        {
            refreshDepth++;
        }

        public static void EndRefresh()
        {
            if (refreshDepth > 0)
            {
                refreshDepth--;
            }
        }

        public static bool Refreshing => refreshDepth > 0;

        public static bool IsSpecial(StoreClient client)
        {
            if (client == null)
            {
                return false;
            }

            try
            {
                if (SpecialClients.IsAny(client))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 识别特殊客人失败: " + ex.Message);
            }

            return JacksonTrade.IsJackson(client);
        }

        public static void Show(StoreClient client)
        {
            if (client == null || !IsSpecial(client))
            {
                Dismiss();
                return;
            }

            string id = (client.identifier ?? "") + "|" + (client.displayName ?? "");
            if (card != null && window != null && shownId == id)
            {
                try
                {
                    if (ItemLife.IsAlive(card))
                    {
                        return;
                    }
                }
                catch
                {
                }
            }

            Dismiss();
            shownId = id;

            try
            {
                GameItem made = DirectoryMaster.Item("maiden_lower", true);
                if (made == null)
                {
                    return;
                }

                string name = string.IsNullOrEmpty(client.displayName) ? client.identifier : client.displayName;
                made.name = name;
                made.shortDescription = "特殊客人";
                ItemFactory.SetStr(made, "MAIDEN_NAME", name);
                ItemFactory.SetStr(made, "MAIDEN_IDENTITY", name);
                ItemFactory.SetStr(made, "MAIDEN_IDENTITY_KEY", IdentityKey(client, name));
                ItemFactory.SetStr(made, "MAIDEN_FACTION", FactionOf(client));
                ItemFactory.SetStr(made, "MAIDEN_JOB", JobOf(client));
                made.EnableTag(Marker);
                made.EnableTag("LOCKED_TAG");
                if (JacksonTrade.IsJackson(client))
                {
                    made.EnableTag(JacksonTrade.Marker);
                }

                if (BroodSync.IsHeld(name))
                {
                    made.EnableTag("ALREADY_CAPTURED");
                }

                GameGridInventory grid = new GameGridInventory(4, 4);
                GraphUtils.TryAcceptAll((GraphNodeStorage)grid, made, -1);
                window = MachineHelper.CreateMachineryWindow(false, MachineHelper.Panel(name, grid, "guest", true));
                window.SetTitle(name);
                window.Center();
                window.Show();
                card = made;
                Debug.Log("[苗床顾客侵蚀] 特殊客人可侵蚀: " + name);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 特殊客人卡片失败: " + ex.Message);
                Dismiss();
            }
        }

        public static void ReleaseIfGone()
        {
            if (card == null)
            {
                return;
            }

            try
            {
                if (ItemLife.IsAlive(card))
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            card = null;
            DestroyWindow();
        }

        public static void Dismiss()
        {
            GameItem held = card;
            card = null;
            shownId = null;
            DestroyWindow();
            if (held == null)
            {
                return;
            }

            try
            {
                if (ItemLife.IsAlive(held))
                {
                    held.Destroy();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 收起特殊客人失败: " + ex.Message);
            }
        }

        static void DestroyWindow()
        {
            PixelWindow open = window;
            window = null;
            if (open == null)
            {
                return;
            }

            try
            {
                open.Destroy();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 关闭特殊客人窗口失败: " + ex.Message);
            }
        }

        static string IdentityKey(StoreClient client, string name)
        {
            string id = client.identifier;
            if (id == "inventor" || id == "inventorStorage")
            {
                return "special:inventor";
            }

            if (string.IsNullOrEmpty(id))
            {
                id = name;
            }

            return "special:" + id;
        }

        static string FactionOf(StoreClient client)
        {
            try
            {
                if (!string.IsNullOrEmpty(client.clientFaction))
                {
                    return MaidenIdentity.FactionCn(client.clientFaction);
                }

                return MaidenIdentity.FactionFromSprite(client.spriteName);
            }
            catch
            {
                return "下层区";
            }
        }

        static string JobOf(StoreClient client)
        {
            if (JacksonTrade.IsJackson(client))
            {
                return "博士";
            }

            return string.IsNullOrEmpty(client.identifier) ? "客人" : client.identifier;
        }
    }

    [HarmonyPatch(typeof(GuestBox), "Show")]
    static class SpecialNpcShowPatch
    {
    static void Prefix()
    {
        SpecialNpc.BeginRefresh();
    }

    static void Postfix()
    {
        try
        {
            ShowCurrent();
        }
        finally
        {
            SpecialNpc.EndRefresh();
        }
    }

    static void ShowCurrent()
    {
        StoreClient client = null;
            try
            {
                PlayerStore store = PlayerStore.Instance;
                StoreClientInstance visit = store == null ? null : store.currentClientInstance;
                client = visit == null ? null : visit.GetClientBlueprint();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 读取特殊客人失败: " + ex.Message);
            }

            if (client == null || GuestBox.CurrentItem != null || !SpecialNpc.IsSpecial(client))
            {
                SpecialNpc.Dismiss();
                JacksonTrade.ApplyCounters();
                return;
            }

            SpecialNpc.Show(client);
            JacksonTrade.ApplyCounters();
        }
    }

    [HarmonyPatch(typeof(GuestBox), "Hide")]
    static class SpecialNpcHidePatch
    {
        static void Postfix()
        {
            if (SpecialNpc.Refreshing)
            {
                return;
            }

            SpecialNpc.Dismiss();
        }
    }
}
