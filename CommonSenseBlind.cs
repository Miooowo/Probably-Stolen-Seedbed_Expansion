using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuestMaidenCorrupt
{
    static class CommonSenseBlind
    {
        public static string LabelId(GameItem item, string category)
        {
            if (item == null)
            {
                return null;
            }

            ItemFeature feature = item.FindItemFeatureByCategory(category);
            if (feature == null)
            {
                return null;
            }

            ItemCondition condition = feature.GetCurrentItemCondition();
            return condition == null ? null : condition.identifier;
        }

        public static void ShowOnly(IList<GameObject> list, int index)
        {
            if (list == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    list[i].SetActive(i == index);
                }
            }
        }

        static void Hide(Button button)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        static void Hide(ChemScannerEffect effect)
        {
            if (effect != null)
            {
                effect.gameObject.SetActive(false);
            }
        }

        static void WritePair(TextMeshProUGUI tested, TextMeshProUGUI label, string shown)
        {
            string testedLine = LocHelper.GetLocalizedMechanic("mech_tooltip_tested") + "\n" + shown;
            string labelLine = LocHelper.GetLocalizedMechanic("mech_tooltip_labeled") + "\n" + shown;
            if (tested != null)
            {
                tested.text = testedLine;
            }

            if (label != null)
            {
                label.text = labelLine;
            }
        }

        static string ShownLabel(GameItem item)
        {
            if (item.IsTag("CHEMICAL_PRODUCT_TAG"))
            {
                ItemFeature feature = item.FindItemFeatureByCategory("CATEGORY_CHEMICAL_PURITY");
                ItemCondition condition = feature == null ? null : feature.GetCurrentItemCondition();
                return condition == null ? null : condition.display;
            }

            if (CommonSense.TryLabeledWater(item, out _, out string display))
            {
                return display;
            }

            return null;
        }

        public static void MaskChem(ChemScannerUIManager ui)
        {
            if (!CommonSense.Active() || ui == null || ui.currentScannedItem == null)
            {
                return;
            }

            GameItem item = ui.currentScannedItem;
            Hide(ui.confrontButton);
            Hide(ui.matchButton);
            Hide(ui.euphoriaEffect);
            Hide(ui.toxicityEffect);
            Hide(ui.synaprestEffect);
            Hide(ui.birthControlEffect);
            if (ui.contaminantTMP != null)
            {
                ui.contaminantTMP.text = "";
            }

            if (item.IsTag("LIQUID_CONTAINER_TAG") && WaterHelper.IsEmpty(item))
            {
                return;
            }

            string shown = ShownLabel(item);
            if (!string.IsNullOrEmpty(shown))
            {
                WritePair(ui.testedTMP, ui.labelTMP, shown);
            }
        }

        public static void MaskWater(WaterScannerUIManager ui)
        {
            if (!CommonSense.Active() || ui == null || ui.currentWaterContainer == null)
            {
                return;
            }

            GameItem item = ui.currentWaterContainer;
            Hide(ui.confrontButton);
            Hide(ui.matchButton);
            if (ui.contaminantTMP != null)
            {
                ui.contaminantTMP.text = "";
            }

            if (WaterHelper.IsEmpty(item))
            {
                return;
            }

            string shown = ShownLabel(item);
            if (!string.IsNullOrEmpty(shown))
            {
                WritePair(ui.testedTMP, ui.labelTMP, shown);
            }
        }

        public static Dialogue DeniedGenuineTalk()
        {
            Dialogue dialogue = new Dialogue();
            dialogue.SetText("PLAYER", LocHelper.GetLocalizedMechanic("ins_cig_talk_genuine")).NextDialogue()
                .SetText("CLIENT", LocHelper.GetLocalizedMechanic("ins_cig_talk_genuine_deny"));
            return dialogue;
        }
    }

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

    [HarmonyPatch(typeof(ItemFeature), nameof(ItemFeature.ExposeFeature))]
    static class CommonSenseRevealPatch
    {
        static bool Prefix()
        {
            return !CommonSense.Active();
        }
    }

    [HarmonyPatch(typeof(ItemFeatureUI), nameof(ItemFeatureUI.OnFeatureUIClick))]
    static class CommonSenseToolPatch
    {
        static bool Prefix(ItemFeatureUI __instance)
        {
            if (!CommonSense.Active() || __instance == null || __instance.itemFeature == null)
            {
                return true;
            }

            if (InspectionUIManager.IsConfrontLocked())
            {
                return true;
            }

            if (RTSController.previousSelectedObjects == null || RTSController.previousSelectedObjects.Count == 0)
            {
                return true;
            }

            GameObject selected = RTSController.previousSelectedObjects[0];
            if (selected == null)
            {
                return true;
            }

            if (selected.GetComponent<NegociationElement>() == null && selected.GetComponent<NotebookElement>() == null)
            {
                return true;
            }

            __instance.OnExposeFeatureBadMatch();
            return false;
        }
    }

    [HarmonyPatch(typeof(ChemScannerUIManager), nameof(ChemScannerUIManager.CanConfrontChem))]
    static class CommonSenseChemConfrontPatch
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

    [HarmonyPatch(typeof(ChemScannerUIManager), nameof(ChemScannerUIManager.CanMatchChem))]
    static class CommonSenseChemMatchPatch
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

    [HarmonyPatch(typeof(ChemScannerUIManager), nameof(ChemScannerUIManager.RefreshUI))]
    static class CommonSenseChemReadoutPatch
    {
        static void Postfix(ChemScannerUIManager __instance)
        {
            try
            {
                CommonSenseBlind.MaskChem(__instance);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变（化学扫描仪）失败: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(WaterScannerUIManager), nameof(WaterScannerUIManager.CanConfront))]
    static class CommonSenseWaterConfrontPatch
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

    [HarmonyPatch(typeof(WaterScannerUIManager), nameof(WaterScannerUIManager.CanMatch))]
    static class CommonSenseWaterMatchPatch
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

    [HarmonyPatch(typeof(WaterScannerUIManager), nameof(WaterScannerUIManager.RefreshUI))]
    static class CommonSenseWaterReadoutPatch
    {
        static void Postfix(WaterScannerUIManager __instance)
        {
            try
            {
                CommonSenseBlind.MaskWater(__instance);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变（水质扫描仪）失败: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(WaterHelper), nameof(WaterHelper.SetLastScanResult))]
    static class CommonSenseLastScanPatch
    {
        static bool Prefix(GameItem gameItem)
        {
            if (!CommonSense.Active())
            {
                return true;
            }

            if (gameItem == null || !gameItem.IsTag("LIQUID_CONTAINER_TAG"))
            {
                return false;
            }

            if (!CommonSense.TryLabeledWater(gameItem, out int purity, out _))
            {
                return true;
            }

            string scanText = string.Format(
                "[{0}]: {1:F2}% ",
                LocHelper.GetLocalizedMechanic("mech_last_scan"),
                (double)purity / 100.0) + WaterFeatureHelper.GetWaterDisplayName(purity);
            TagState tag = gameItem.GetTagReadonly("LAST_SCAN_RESULT_TAG");
            if (!tag.IsEnabled() || !scanText.Equals(tag.GetString()))
            {
                gameItem.ModifyTag("LAST_SCAN_RESULT_TAG", state =>
                {
                    state.Enable().SetString(scanText);
                }, true);
                gameItem.SyncModifiedState();
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(WaterStripUIManager), nameof(WaterStripUIManager.CanConfront))]
    static class CommonSenseStripConfrontPatch
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

    [HarmonyPatch(typeof(WaterStripUIManager), nameof(WaterStripUIManager.CanMatch))]
    static class CommonSenseStripMatchPatch
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

    [HarmonyPatch(typeof(WaterStripUIManager), nameof(WaterStripUIManager.Open))]
    static class CommonSenseStripOpenPatch
    {
        static void Prefix(GameItem testStrip)
        {
            if (!CommonSense.Active() || testStrip == null)
            {
                return;
            }

            try
            {
                int id = testStrip.GetTagReadonly("ASSOCIATED_ITEM_ID").GetInt();
                GameItem water = EmporiumEntry.Instance.FindItemByUniqueID(id);
                if (!CommonSense.TryLabeledWater(water, out int purity, out _))
                {
                    return;
                }

                testStrip.ModifyTag("TEST_WATER_QUALITY", state =>
                {
                    state.Enable().SetInt(purity);
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变（试纸）失败: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(WaterTestHelper), nameof(WaterTestHelper.HandleWaterCheckTestingStrip))]
    static class CommonSenseStripRecordPatch
    {
        static void Postfix(GameItem targetGameItem, GameItem testingStrip)
        {
            if (!CommonSense.Active() || targetGameItem == null || testingStrip == null)
            {
                return;
            }

            if (!CommonSense.TryLabeledWater(targetGameItem, out int purity, out _))
            {
                return;
            }

            testingStrip.ModifyTag("TEST_WATER_QUALITY", state =>
            {
                state.Enable().SetInt(purity);
            });
        }
    }

    [HarmonyPatch(typeof(WaterFeatureHelper), nameof(WaterFeatureHelper.CreateWaterFeatureTooltip))]
    static class CommonSenseWaterTooltipPatch
    {
        static bool Prefix(RichTextBuilder builder, GameItem item)
        {
            if (!CommonSense.Active())
            {
                return true;
            }

            try
            {
                if (builder == null || item == null || item.IsTag("WINE_CONTAINER_TAG") || !item.IsTag("LIQUID_CONTAINER_TAG"))
                {
                    return false;
                }

                if (WaterHelper.GetTotalVolume(item) == 0)
                {
                    return false;
                }

                ItemFeature feature = WaterFeatureHelper.GetWaterFeature(item);
                if (feature != null && feature.IsFeatureDiscovered() && CommonSense.TryLabeledWater(item, out _, out string display))
                {
                    builder.Add(LocHelper.GetLocalizedMechanic("mech_tooltip_tested"), RenderHandler.ColorPalette.Blue);
                    builder.AddLine(display);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 常识改变（水质提示）失败: " + ex.Message);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(WaterFeatureHelper), nameof(WaterFeatureHelper.HandleWaterCheckTestingStrip))]
    static class CommonSenseStripExposePatch
    {
        static bool Prefix()
        {
            return !CommonSense.Active();
        }
    }

    [HarmonyPatch(typeof(InsCigaretteHelper), nameof(InsCigaretteHelper.IsConfrontCorrect))]
    static class CommonSenseCigaretteVerdictPatch
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

    [HarmonyPatch(typeof(InspectableHelper), nameof(InspectableHelper.IsConfrontCorrect))]
    static class CommonSenseInspectVerdictPatch
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

    [HarmonyPatch(typeof(InsCigaretteHelper), nameof(InsCigaretteHelper.CreateTalkDialogue))]
    static class CommonSenseCigaretteTalkPatch
    {
        static bool Prefix(ref Dialogue __result)
        {
            if (!CommonSense.Active())
            {
                return true;
            }

            __result = CommonSenseBlind.DeniedGenuineTalk();
            return false;
        }
    }

    [HarmonyPatch(typeof(InsStampHelper), nameof(InsStampHelper.CreateTalkDialogue))]
    static class CommonSenseStampTalkPatch
    {
        static bool Prefix(ref Dialogue __result)
        {
            if (!CommonSense.Active())
            {
                return true;
            }

            __result = CommonSenseBlind.DeniedGenuineTalk();
            return false;
        }
    }

    [HarmonyPatch(typeof(CigaretteUI), nameof(CigaretteUI.OnInit))]
    static class CommonSenseCigaretteViewPatch
    {
        static void Postfix(CigaretteUI __instance, GameItem gameItem)
        {
            if (!CommonSense.Active() || __instance == null)
            {
                return;
            }

            string id = CommonSenseBlind.LabelId(gameItem, "CATEGORY_GENUINE_CIGARETTE");
            if (id == null)
            {
                return;
            }

            __instance.DisableAll();
            bool genuine = id == "genuineCigarette";
            int seal = genuine || __instance.seals == null || __instance.seals.Count < 2 ? 0 : 1;
            CommonSenseBlind.ShowOnly(__instance.seals, seal);
            CommonSenseBlind.ShowOnly(__instance.letterings, 0);
            CommonSenseBlind.ShowOnly(__instance.logos, 0);
            CommonSenseBlind.ShowOnly(__instance.boxes, 0);
        }
    }

    [HarmonyPatch(typeof(StampUI), nameof(StampUI.OnInit))]
    static class CommonSenseStampViewPatch
    {
        static void Postfix(StampUI __instance, GameItem gameItem)
        {
            if (!CommonSense.Active() || __instance == null)
            {
                return;
            }

            string id = CommonSenseBlind.LabelId(gameItem, "CATEGORY_GENUINE_STAMP");
            if (id == null)
            {
                return;
            }

            __instance.DisableAll();
            if (id == "genuineStamp")
            {
                int amount = gameItem.GetTagReadonly("STAMP_AMOUNT").GetInt();
                CommonSenseBlind.ShowOnly(__instance.logos, 0);
                CommonSenseBlind.ShowOnly(__instance.chips, 0);
                CommonSenseBlind.ShowOnly(__instance.letterings, 0);
                if (amount >= 160)
                {
                    CommonSenseBlind.ShowOnly(__instance.seals, 0);
                }

                return;
            }

            int logo = __instance.logos != null && __instance.logos.Count > 1 ? 1 : 0;
            CommonSenseBlind.ShowOnly(__instance.logos, logo);
            CommonSenseBlind.ShowOnly(__instance.chips, 0);
            CommonSenseBlind.ShowOnly(__instance.letterings, 0);
            CommonSenseBlind.ShowOnly(__instance.seals, 0);
        }
    }

    [HarmonyPatch(typeof(InjectorUI), nameof(InjectorUI.OnInit))]
    static class CommonSenseInjectorViewPatch
    {
        static void Postfix(InjectorUI __instance, GameItem gameItem)
        {
            if (!CommonSense.Active() || __instance == null)
            {
                return;
            }

            string id = CommonSenseBlind.LabelId(gameItem, "CATEGORY_GENUINE_INJECTOR");
            if (id == null)
            {
                return;
            }

            __instance.DisableAll();
            if (id == "expiredInjector")
            {
                CommonSenseBlind.ShowOnly(__instance.display, 1);
            }
            else
            {
                CommonSenseBlind.ShowOnly(__instance.display, 0);
            }

            if (id == "unusableInjector")
            {
                CommonSenseBlind.ShowOnly(__instance.broken, 0);
            }
            else if (__instance.body != null)
            {
                __instance.body.SetActive(true);
            }

            if (__instance.serialTMP != null)
            {
                __instance.serialTMP.text = "2404821";
            }

            if (__instance.modelTMP != null)
            {
                __instance.modelTMP.text = InsInjectorHelper.CORRECT_MODEL;
            }

            if (__instance.companyTMP != null)
            {
                __instance.companyTMP.text = id == "genuineInjector" || id == "expiredInjector"
                    ? InsInjectorHelper.CORRECT_COMPANY
                    : "Carmicheal Pharm.";
            }
        }
    }
}
