using System;
using System.Collections.Generic;
using HarmonyLib;
using MidnightPawn.Api;
using MidnightPawn.Game;
using UnityEngine;

namespace GuestMaidenCorrupt
{
    [HarmonyPatch(typeof(CorruptTentacle), "IsCorruptible")]
    [HarmonyPriority(Priority.First)]
    static class AllowGuestCorruptPatch
    {
        static bool Prefix(GameItem it, ref bool __result)
        {
            if (!GuestCorrupt.IsCurrentGuest(it))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(CorruptTentacle), "TryCorrupt")]
    static class FleeAfterSuccessPatch
    {
        static void Prefix(GameItem tentacle, GameItem target)
        {
            GuestCorrupt.NoteTentacle(tentacle);
            GuestCorrupt.LiftGuestCharge(tentacle, target);
        }

        static void Postfix(GameItem tentacle, bool __result)
        {
            GuestCorrupt.RestoreGuestCharge(tentacle, __result);
            GuestCorrupt.FinishDelivery(tentacle);
        }
    }

    [HarmonyPatch(typeof(SecData), "GetCrimeString")]
    static class CrimeNamePatch
    {
        static void Postfix(string crimeID, ref string __result)
        {
            if (crimeID == GuestCrime.Id)
            {
                __result = "猥亵消费者";
            }
        }
    }

    [HarmonyPatch(typeof(Toast), "Show")]
    static class RewriteNoticePatch
    {
        static void Prefix(ref string text)
        {
            GuestCorrupt.RewriteNotice(ref text);
        }
    }

    [HarmonyPatch(typeof(ItemDelivery), "TryDeliver")]
    static class BroodDeliveryPatch
    {
        static bool Prefix(GameItem it, ref string __result)
        {
            if (!GuestCorrupt.TryClaimBrood(it, out string where))
            {
                return true;
            }

            __result = where;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.OnAddedToClientArea))]
    static class CommonSenseClientPatch
    {
        static void Postfix(GameItem __instance)
        {
            CommonSense.OnClient(__instance);
        }
    }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.OnAddedToWeightedArea))]
    static class CommonSenseWeightedPatch
    {
        static void Postfix(GameItem __instance)
        {
            CommonSense.OnWeighted(__instance);
        }
    }

    [HarmonyPatch(typeof(ModSourceHelper), nameof(ModSourceHelper.CreateTooltip))]
    static class ChanceTooltipPatch
    {
        static void Postfix(RichTextBuilder builder, GameItem item)
        {
            GuestCorrupt.WriteChance(builder, item);
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.DismissCurrentClient))]
    static class CommonSenseDismissPatch
    {
        static void Postfix()
        {
            CommonSense.End();
        }
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.CurrentClientLeave))]
    static class CommonSenseLeavePatch
    {
        static void Postfix()
        {
            CommonSense.End();
        }
    }

    [HarmonyPatch(typeof(DialoguePhoneUIManager), nameof(DialoguePhoneUIManager.CurrentClientLeave))]
    static class CommonSensePhoneLeavePatch
    {
        static void Postfix()
        {
            CommonSense.End();
        }
    }

    [HarmonyPatch(typeof(StoreStation), nameof(StoreStation.StartDay))]
    static class ElectronicMaidenDayPatch
    {
        static void Postfix()
        {
            ElectronicMaiden.OnDay();
        }
    }

    [HarmonyPatch(typeof(MachineHelper), nameof(MachineHelper.OnLoad))]
    static class ElectronicMaidenLoadPatch
    {
        static void Postfix(GameItem machine)
        {
            ElectronicMaiden.OnLoad(machine);
        }
    }

    [HarmonyPatch(typeof(RichTextGenerator), nameof(RichTextGenerator.UseItemOnOtherItem))]
    static class DragChancePatch
    {
        static bool Prefix(GameItem sourceItem, GameItem targetItem, bool canInsert, RichTextBuilder customBuilder, ref RichText __result)
        {
            if (customBuilder == null || !GuestCorrupt.TryDragGuest(sourceItem, targetItem, out GameItem guest))
            {
                return true;
            }

            customBuilder.Clear();
            string dragged = sourceItem.GetDisplayName(true);
            string onto = targetItem.GetDisplayName(true);
            if (canInsert)
            {
                if (TooltipUseHelper.HasUseTooltip(sourceItem, targetItem))
                {
                    TooltipUseHelper.HandleUseTooltip(sourceItem, targetItem, customBuilder);
                }
                else
                {
                    customBuilder.Add(
                        LocHelper.GetLocalizedMechanic("mech_drag_use_on", dragged, "<u>" + onto + "</u>"),
                        RenderHandler.ColorPalette.LightGreen);
                }
            }
            else
            {
                customBuilder.Add(
                    LocHelper.GetLocalizedMechanic("mech_drag_cant_use_on", dragged, onto),
                    RenderHandler.ColorPalette.Red);
            }

            GameItem tentacle = targetItem != null && targetItem.identifier == "corrupt_tentacle" ? targetItem : sourceItem;
            GuestCorrupt.WriteChance(customBuilder, guest, tentacle);
            __result = customBuilder.Build();
            return false;
        }
    }

    [HarmonyPatch(typeof(ModuleHelper), "CanElectronicPartTarget")]
    static class ElectronicFeedPatch
    {
        static bool Prefix(GameItem electronicPart, GameItem target, ref bool __result)
        {
            if (target == null || !target.IsTag(ElectronicMaiden.Marker))
            {
                return true;
            }

            try
            {
                __result = GeneralHelper.IsItemOwned(electronicPart) && GeneralHelper.IsItemOwned(target);
            }
            catch
            {
                __result = false;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(CorruptTentacle), nameof(CorruptTentacle.GetCharge))]
    static class ChargeReadPatch
    {
        static bool Prefix(GameItem it, ref int __result)
        {
            if (it == null)
            {
                __result = 0;
                return false;
            }

            int raw = ItemFactory.GetInt(it, "TENTACLE_CHARGE");
            __result = raw < 0 ? 0 : raw;
            return false;
        }
    }

    [HarmonyPatch(typeof(CorruptTentacle), nameof(CorruptTentacle.SetCharge))]
    static class ChargeWritePatch
    {
        static bool Prefix(GameItem it, int v)
        {
            if (it != null)
            {
                if (v < 0)
                {
                    v = 0;
                }

                ItemFactory.SetInt(it, "TENTACLE_CHARGE", v);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(TentacleApi), nameof(TentacleApi.AddCharge))]
    static class ChargeAddPatch
    {
        static bool Prefix(GameItem tentacle, int v)
        {
            if (tentacle == null)
            {
                return false;
            }

            int next = CorruptTentacle.GetCharge(tentacle) + v;
            if (next < 0)
            {
                next = 0;
            }

            CorruptTentacle.SetCharge(tentacle, next);
            return false;
        }
    }

    [HarmonyPatch(typeof(CorruptTentacle), nameof(CorruptTentacle.DailyRecharge))]
    static class ChargeDailyPatch
    {
        static bool Prefix()
        {
            try
            {
                ItemLife.Prune(CorruptTentacle.Instances);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 清理触手失败: " + ex.Message);
            }

            GameItem[] list;
            try
            {
                list = CorruptTentacle.Instances.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 读取触手失败: " + ex.Message);
                return false;
            }

            foreach (GameItem tentacle in list)
            {
                if (tentacle == null)
                {
                    continue;
                }

                try
                {
                    CorruptTentacle.SetCharge(tentacle, CorruptTentacle.GetCharge(tentacle) + 10);
                    CorruptTentacle.RefreshTooltip(tentacle);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[苗床顾客侵蚀] 每日淫力失败: " + ex.Message);
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(CorruptTentacle), nameof(CorruptTentacle.OnSemenUsed))]
    static class ChargeSemenPatch
    {
        static int frame = -1;
        static readonly HashSet<GameItem> used = new HashSet<GameItem>();

        static bool Prefix(GameItem tentacle, GameItem semen, ref bool __result)
        {
            if (tentacle == null)
            {
                __result = false;
                return false;
            }

            int now = Time.frameCount;
            if (now != frame)
            {
                frame = now;
                used.Clear();
            }

            if (semen != null && !used.Add(semen))
            {
                __result = false;
                return false;
            }

            try
            {
                CorruptTentacle.SetCharge(tentacle, CorruptTentacle.GetCharge(tentacle) + 10);
                CorruptTentacle.RefreshTooltip(tentacle);
                int charge = CorruptTentacle.GetCharge(tentacle);
                Toast.Show("在苗床的子宫中呆了一晚上，并且混合了淫水的触手精液充满了淫力，被侵蚀性触手重新吸收，淫力 +10（" + charge + "）");
                __result = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 精液充能失败: " + ex.Message);
                __result = false;
            }

            return false;
        }
    }
}
