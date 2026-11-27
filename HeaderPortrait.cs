using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuestMaidenCorrupt
{
    // 别的模组会把 SpriteDict 里的立绘换成新图。大窗口之后取到的是新图，
    // 左上角还握着换库之前的那张旧图。不能再拿「现在的图库」去认旧图。
    static class HeaderPortrait
    {
        static Sprite seen;
        static int cooldown;
        static bool logged;

        public static void Arm()
        {
            cooldown = 0;
            Tick();
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
                    return;
                }

                if (big != seen)
                {
                    seen = big;
                    cooldown = 0;
                }

                if (cooldown > 0)
                {
                    cooldown--;
                    return;
                }

                cooldown = 8;
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
            string key = null;
            try
            {
                key = client.GetSprite();
            }
            catch
            {
            }

            PaintNamed(big, key);
            PaintBesideName(client.displayName, big);
        }

        static void PaintNamed(Sprite big, string key)
        {
            Image[] images = UnityEngine.Object.FindObjectsOfType<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !image.gameObject.activeInHierarchy || IsChrome(image))
                {
                    continue;
                }

                if (IsStale(image.overrideSprite, big, key))
                {
                    image.overrideSprite = big;
                }

                if (IsStale(image.sprite, big, key))
                {
                    image.sprite = big;
                }
            }

            SpriteRenderer[] renderers = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || Protected(renderer))
                {
                    continue;
                }

                if (IsStale(renderer.sprite, big, key))
                {
                    renderer.sprite = big;
                }
            }
        }

        static void PaintBesideName(string name, Sprite big)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            TMP_Text[] labels = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null || !label.isActiveAndEnabled || label.text == null || label.text.Trim() != name)
                {
                    continue;
                }

                Transform panel = label.transform.parent;
                if (panel == null)
                {
                    continue;
                }

                PaintPanel(panel, big);
            }
        }

        static void PaintPanel(Transform panel, Sprite big)
        {
            for (int i = 0; i < panel.childCount; i++)
            {
                PaintNode(panel.GetChild(i), big);
            }
        }

        static void PaintNode(Transform node, Sprite big)
        {
            if (node == null)
            {
                return;
            }

            Image image = node.GetComponent<Image>();
            if (image != null && !IsChrome(image) && image.sprite != null && image.sprite != big && PortraitSized(image.rectTransform))
            {
                image.sprite = big;
                if (image.overrideSprite != null && image.overrideSprite != big)
                {
                    image.overrideSprite = big;
                }
            }

            SpriteRenderer renderer = node.GetComponent<SpriteRenderer>();
            if (renderer != null && !Protected(renderer) && renderer.sprite != null && renderer.sprite != big)
            {
                renderer.sprite = big;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                Image nested = child.GetComponent<Image>();
                if (nested != null && !IsChrome(nested) && nested.sprite != null && nested.sprite != big && PortraitSized(nested.rectTransform))
                {
                    nested.sprite = big;
                    if (nested.overrideSprite != null && nested.overrideSprite != big)
                    {
                        nested.overrideSprite = big;
                    }
                }
            }
        }

        static bool PortraitSized(RectTransform rect)
        {
            if (rect == null)
            {
                return false;
            }

            float width = rect.rect.width;
            float height = rect.rect.height;
            if (width < 24f || height < 24f || width > 420f || height > 520f)
            {
                return false;
            }

            float ratio = width / height;
            return ratio >= 0.45f && ratio <= 1.8f;
        }

        static bool IsStale(Sprite sprite, Sprite big, string key)
        {
            if (sprite == null || sprite == big)
            {
                return false;
            }

            string spriteName = sprite.name;
            if (string.IsNullOrEmpty(spriteName))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(key) && string.Equals(spriteName, key, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string bigName = big == null ? null : big.name;
            return !string.IsNullOrEmpty(bigName)
                && string.Equals(spriteName, bigName, System.StringComparison.OrdinalIgnoreCase);
        }

        static bool IsChrome(Image image)
        {
            DialogUIManager dialog = DialogUIManager.Instance;
            if (dialog == null)
            {
                return false;
            }

            if (dialog.clientNameBubbleImage != null && image == dialog.clientNameBubbleImage)
            {
                return true;
            }

            if (dialog.clientNameBubble != null && image.transform.IsChildOf(dialog.clientNameBubble.transform))
            {
                return true;
            }

            return false;
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
            StoreUIManager ui = StoreUIManager.Instance;
            if (ui != null && ui.clientGameObject != null && ui.clientGameObject.image != null && ui.clientGameObject.image.sprite != null)
            {
                return ui.clientGameObject.image.sprite;
            }

            StoreClientMono mono = StoreClientMono.Instance;
            if (mono != null && mono.image != null)
            {
                return mono.image.sprite;
            }

            return null;
        }

        static bool Protected(SpriteRenderer renderer)
        {
            StoreUIManager ui = StoreUIManager.Instance;
            if (ui != null)
            {
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
            }

            StoreClientMono mono = StoreClientMono.Instance;
            return mono != null && mono.image == renderer;
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
        }
    }
}
