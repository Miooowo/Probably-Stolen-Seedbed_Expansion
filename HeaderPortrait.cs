using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuestMaidenCorrupt
{
    // 大窗口的立绘会被别的模组换成另一张图。左上角那张小立绘仍拿着原来的像素图。
    // 顾客名字还在屏幕上时，把仍显示原像素图的小立绘换成大窗口正在用的那张。
    static class HeaderPortrait
    {
        static Sprite seen;
        static int arm;
        static int cooldown;
        static bool logged;

        public static void Arm()
        {
            arm = 120;
            cooldown = 0;
        }

        public static void Tick()
        {
            try
            {
                StoreClient client = Current();
                Sprite big = Big();
                if (client == null || big == null)
                {
                    seen = null;
                    arm = 0;
                    return;
                }

                if (big != seen)
                {
                    seen = big;
                    arm = 120;
                    cooldown = 0;
                }

                if (arm <= 0)
                {
                    return;
                }

                arm--;
                if (cooldown > 0)
                {
                    cooldown--;
                    return;
                }

                cooldown = 10;
                Apply(client, big);
            }
            catch (Exception ex)
            {
                if (!logged)
                {
                    logged = true;
                    Debug.LogWarning("[苗床顾客侵蚀] 同步左上角立绘失败: " + ex.Message);
                }
            }
        }

        static void Apply(StoreClient client, Sprite big)
        {
            string key = client.GetSprite();
            Sprite vanilla = Vanilla(key);
            if (vanilla == null || big == vanilla)
            {
                return;
            }

            string name = client.displayName;
            if (string.IsNullOrEmpty(name) || !NameVisible(name))
            {
                return;
            }

            Image[] images = UnityEngine.Object.FindObjectsOfType<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !image.isActiveAndEnabled)
                {
                    continue;
                }

                if (ShowsVanilla(image.overrideSprite, vanilla))
                {
                    image.overrideSprite = big;
                }

                if (ShowsVanilla(image.sprite, vanilla))
                {
                    image.sprite = big;
                }
            }

            SpriteRenderer[] renderers = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || Protected(renderer))
                {
                    continue;
                }

                if (ShowsVanilla(renderer.sprite, vanilla))
                {
                    renderer.sprite = big;
                }
            }
        }

        static StoreClient Current()
        {
            if (!PlayerStore.HasInstance)
            {
                return null;
            }

            PlayerStore store = PlayerStore.Instance;
            if (store == null || store.currentClientInstance == null)
            {
                return null;
            }

            return store.currentClientInstance.GetClientBlueprint();
        }

        static Sprite Big()
        {
            StoreClientMono mono = StoreClientMono.Instance;
            if (mono != null && mono.image != null && mono.image.sprite != null)
            {
                return mono.image.sprite;
            }

            StoreUIManager ui = StoreUIManager.Instance;
            if (ui != null && ui.clientGameObject != null && ui.clientGameObject.image != null)
            {
                return ui.clientGameObject.image.sprite;
            }

            return null;
        }

        static Sprite Vanilla(string key)
        {
            SpriteDict dict = SpriteDict.Instance;
            if (dict == null || dict.spriteDictionary == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            Sprite sprite;
            if (!dict.spriteDictionary.TryGetValue(key, out sprite))
            {
                return null;
            }

            return sprite;
        }

        static bool ShowsVanilla(Sprite sprite, Sprite vanilla)
        {
            if (sprite == null || vanilla == null)
            {
                return false;
            }

            if (sprite == vanilla)
            {
                return true;
            }

            return sprite.texture == vanilla.texture && sprite.rect == vanilla.rect;
        }

        static bool Protected(SpriteRenderer renderer)
        {
            StoreClientMono mono = StoreClientMono.Instance;
            if (mono != null && mono.image == renderer)
            {
                return true;
            }

            StoreUIManager ui = StoreUIManager.Instance;
            if (ui == null)
            {
                return false;
            }

            if (ui.clientGameObject != null && ui.clientGameObject.image == renderer)
            {
                return true;
            }

            if (ui.secGameObject != null && ui.secGameObject.image == renderer)
            {
                return true;
            }

            if (ui.secGameObject2 != null && ui.secGameObject2.image == renderer)
            {
                return true;
            }

            return false;
        }

        static bool NameVisible(string name)
        {
            TMP_Text[] labels = UnityEngine.Object.FindObjectsOfType<TMP_Text>();
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null || !label.isActiveAndEnabled || label.text == null)
                {
                    continue;
                }

                if (label.text.Trim() == name)
                {
                    return true;
                }
            }

            Text[] plain = UnityEngine.Object.FindObjectsOfType<Text>();
            for (int i = 0; i < plain.Length; i++)
            {
                Text label = plain[i];
                if (label == null || !label.isActiveAndEnabled || label.text == null)
                {
                    continue;
                }

                if (label.text.Trim() == name)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(StoreUIManager), "Update")]
    static class HeaderPortraitTick
    {
        static void Postfix()
        {
            HeaderPortrait.Tick();
        }
    }

    [HarmonyPatch(typeof(StoreUIManager), "DisplayClient")]
    static class HeaderPortraitOnDisplay
    {
        static void Postfix()
        {
            HeaderPortrait.Arm();
        }
    }

    [HarmonyPatch(typeof(StoreUIManager), "OnNextClientArrived")]
    static class HeaderPortraitOnArrived
    {
        static void Postfix()
        {
            HeaderPortrait.Arm();
        }
    }

    [HarmonyPatch(typeof(DialogUIManager), "DisplayClientText")]
    [HarmonyPriority(Priority.Last)]
    static class HeaderPortraitOnLine
    {
        static void Postfix()
        {
            HeaderPortrait.Arm();
            HeaderPortrait.Tick();
        }
    }

    [HarmonyPatch(typeof(NegociationUIManager), "RefreshClientHeader")]
    [HarmonyPriority(Priority.Last)]
    static class HeaderPortraitOnHeader
    {
        static void Postfix()
        {
            HeaderPortrait.Arm();
            HeaderPortrait.Tick();
        }
    }
}
