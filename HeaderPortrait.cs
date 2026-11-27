using System;
using HarmonyLib;
using MidnightPawn.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuestMaidenCorrupt
{
    // 立绘模组只把大窗口（StoreClientMono）换成当前那张图。
    // 左上角是另一张：客人格进店时烤好并缓存的像素图，或仍握着原图的小立绘。
    // 高清立绘管理器不再改这张缓存。这里在晚更新里抄大窗口当前的立绘。
    static class HeaderPortrait
    {
        static bool logged;
        static Sprite noted;

        public static void Ensure()
        {
            StoreUIManager ui = StoreUIManager.Instance;
            if (ui == null || ui.GetComponent<HeaderPortraitLate>() != null)
            {
                return;
            }

            ui.gameObject.AddComponent<HeaderPortraitLate>();
        }

        public static void Tick()
        {
            try
            {
                Sprite big = Big();
                StoreClient client = Current();
                if (big == null || client == null)
                {
                    noted = null;
                    return;
                }

                string key = SpriteKey(client);
                PaintItem(GuestBox.CurrentItem as GameItemElement, big);
                PaintItem(SpecialNpc.Current as GameItemElement, big);
                PaintLoose(client, big, key);
                PaintNearName(client.displayName, big, key);
                if (noted != big)
                {
                    noted = big;
                    Debug.Log("[苗床顾客侵蚀] 左上角已跟上大窗口当前立绘");
                }
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

        static void PaintItem(GameItemElement card, Sprite big)
        {
            if (card == null)
            {
                return;
            }

            Assign(card.background == null ? null : card.background.image, big);
            Assign(card.fakeBackground == null ? null : card.fakeBackground.image, big);
        }

        static void PaintLoose(StoreClient client, Sprite big, string key)
        {
            Image[] images = UnityEngine.Object.FindObjectsOfType<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !image.gameObject.activeInHierarchy || IsChrome(image))
                {
                    continue;
                }

                if (SamePerson(image.sprite, client, big, key) || SamePerson(image.overrideSprite, client, big, key))
                {
                    Assign(image, big);
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

                if (SamePerson(renderer.sprite, client, big, key))
                {
                    Assign(renderer, big);
                }
            }
        }

        static void PaintNearName(string name, Sprite big, string key)
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

                if (UnderDialogue(label.transform))
                {
                    continue;
                }

                Transform node = label.transform;
                for (int depth = 0; depth < 8 && node != null; depth++)
                {
                    RectTransform rect = node as RectTransform;
                    if (rect != null && (rect.rect.width > 520f || rect.rect.height > 640f))
                    {
                        break;
                    }

                    if (node.childCount > 24)
                    {
                        break;
                    }

                    PaintBranch(node, big);
                    node = node.parent;
                }
            }
        }

        static void PaintBranch(Transform root, Sprite big)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !image.gameObject.activeInHierarchy || IsChrome(image) || IsFrame(image.sprite))
                {
                    continue;
                }

                if (image.sprite != null && image.sprite != big && PortraitSized(image.rectTransform))
                {
                    Assign(image, big);
                }
            }

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || Protected(renderer) || renderer.sprite == null || renderer.sprite == big || IsFrame(renderer.sprite))
                {
                    continue;
                }

                Assign(renderer, big);
            }
        }

        static void Assign(Image image, Sprite big)
        {
            if (image == null || big == null)
            {
                return;
            }

            if (image.sprite == big && (image.overrideSprite == null || image.overrideSprite == big))
            {
                return;
            }

            Vector2 size = image.rectTransform.sizeDelta;
            image.preserveAspect = true;
            image.sprite = big;
            if (image.overrideSprite != null)
            {
                image.overrideSprite = null;
            }

            image.rectTransform.sizeDelta = size;
        }

        static void Assign(SpriteRenderer renderer, Sprite big)
        {
            if (renderer == null || big == null || renderer.sprite == null || renderer.sprite == big)
            {
                return;
            }

            float oldHeight = renderer.sprite.bounds.size.y;
            Vector3 scale = renderer.transform.localScale;
            renderer.sprite = big;
            float newHeight = big.bounds.size.y;
            if (oldHeight > 0.01f && newHeight > 0.01f)
            {
                renderer.transform.localScale = scale * (oldHeight / newHeight);
            }
        }

        static bool SamePerson(Sprite sprite, StoreClient client, Sprite big, string key)
        {
            if (sprite == null || sprite == big || IsFrame(sprite))
            {
                return false;
            }

            string spriteName = sprite.name;
            if (string.IsNullOrEmpty(spriteName))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(big.name) && string.Equals(spriteName, big.name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(key) && spriteName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (JacksonTrade.IsJackson(client))
            {
                if (spriteName.IndexOf("jackson", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (spriteName.IndexOf("inventor", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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
            return ratio >= 0.4f && ratio <= 1.8f;
        }

        static bool IsFrame(Sprite sprite)
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
            {
                return false;
            }

            string spriteName = sprite.name;
            return spriteName.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("border", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("button", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("window", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("elements_", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsChrome(Image image)
        {
            return image != null && UnderDialogue(image.transform);
        }

        static bool UnderDialogue(Transform transform)
        {
            DialogUIManager dialog = DialogUIManager.Instance;
            if (dialog == null || transform == null)
            {
                return false;
            }

            if (dialog.clientNameBubble != null && (transform == dialog.clientNameBubble.transform || transform.IsChildOf(dialog.clientNameBubble.transform)))
            {
                return true;
            }

            if (dialog.clientBubble != null && transform.IsChildOf(dialog.clientBubble.transform))
            {
                return true;
            }

            return false;
        }

        static string SpriteKey(StoreClient client)
        {
            try
            {
                return client.GetSprite();
            }
            catch
            {
                return null;
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

            bool instanceHasSprite = mono != null && mono.image != null && mono.image.sprite != null;
            if (!instanceHasSprite && ui.clientGameObject != null && ui.clientGameObject.image == renderer)
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
    }

    [DefaultExecutionOrder(32000)]
    public class HeaderPortraitLate : MonoBehaviour
    {
        void LateUpdate()
        {
            HeaderPortrait.Tick();
        }
    }

    [HarmonyPatch(typeof(StoreUIManager), "Update")]
    static class HeaderPortraitBoot
    {
        static void Postfix()
        {
            HeaderPortrait.Ensure();
        }
    }
}
