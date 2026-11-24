using HarmonyLib;
using MidnightPawn.Game;

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
        static void Prefix(GameItem tentacle)
        {
            GuestCorrupt.NoteTentacle(tentacle);
        }

        static void Postfix(GameItem tentacle)
        {
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

            GuestCorrupt.WriteChance(customBuilder, guest);
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
}
