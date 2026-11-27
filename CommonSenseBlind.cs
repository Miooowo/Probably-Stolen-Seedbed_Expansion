using HarmonyLib;

namespace GuestMaidenCorrupt
{
    // 常识改变只让当前这位顾客自己的指认失效。玩家的扫描仪、试纸和放大镜不改。
    [HarmonyPatch(typeof(StoreClient), nameof(StoreClient.CanClientExposeThisFeature))]
    static class CommonSenseExposeFeaturePatch
    {
        static bool Prefix(ref bool __result)
        {
            if (!CommonSense.Active())
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(StoreClient), nameof(StoreClient.CanClientExposeAnyFeature))]
    static class CommonSenseExposeAnyPatch
    {
        static bool Prefix(ref bool __result)
        {
            if (!CommonSense.Active())
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(NegociationHelper), nameof(NegociationHelper.CheckBasic))]
    static class CommonSenseNegotiatePatch
    {
        static bool Prefix(ref bool __result)
        {
            if (!CommonSense.Active())
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(AmmoBoxHelper), nameof(AmmoBoxHelper.HandleAmmoBoxCheck))]
    static class CommonSenseAmmoPatch
    {
        static bool Prefix()
        {
            return !CommonSense.Active();
        }
    }
}
