using System;
using System.Collections.Generic;
using HarmonyLib;
using MidnightPawn.Game;
using UnityEngine;
using UnityEngine.UI;

namespace GuestMaidenCorrupt
{
    // 立绘模组只把大窗口（StoreClientMono）换成当前那张图。
    // 左上角是小框。直接把高清大图塞进去会按九宫格/紧网格切开，
    // 手机上大贴图还会撕成横条，底部伸出面板。这里先缩成一张
    // 完整小图，再放进原来的框。
    static class HeaderPortrait
    {
        const int FitSize = 128;

        static bool logged;
        static Sprite noted;
        static Sprite fitted;
        static Texture2D fittedTex;
        static int fittedId;
        static readonly Dictionary<int, Vector3> rendererScale = new Dictionary<int, Vector3>();

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
                PaintItem(GuestBox.CurrentItem as GameItemElement, client, big, key);
                PaintItem(SpecialNpc.Current as GameItemElement, client, big, key);
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

        static void PaintItem(GameItemElement card, StoreClient client, Sprite big, string key)
        {
            if (card == null)
            {
                return;
            }

            Image face = card.background == null ? null : card.background.image;
            Image drag = card.fakeBackground == null ? null : card.fakeBackground.image;
            Assign(face, big);
            Assign(drag, big);
            ClearStacked(card, face, drag, client, big, key);
        }

        static void ClearStacked(GameItemElement card, Image face, Image drag, StoreClient client, Sprite big, string key)
        {
            Transform scope = CardScope(card);
            if (scope == null)
            {
                return;
            }

            Image[] images = scope.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image == face || image == drag || image.sprite == null || IsFrame(image.sprite))
                {
                    continue;
                }

                bool ours = image.sprite.name == "guest-corner-fit";
                bool oldPortrait = SamePerson(image.sprite, client, big, key) || SamePerson(image.overrideSprite, client, big, key);
                if (!ours && !oldPortrait)
                {
                    continue;
                }

                image.sprite = null;
                image.overrideSprite = null;
                image.enabled = false;
            }
        }

        static Transform CardScope(GameItemElement card)
        {
            Transform node = card.handler != null ? card.handler.transform : (card.background == null ? null : card.background.transform);
            if (node == null)
            {
                return null;
            }

            Transform scope = node;
            for (int depth = 0; depth < 12 && node.parent != null; depth++)
            {
                node = node.parent;
                if (node.childCount > 30)
                {
                    break;
                }

                RectTransform rect = node as RectTransform;
                if (rect != null && (rect.rect.width > 720f || rect.rect.height > 820f))
                {
                    break;
                }

                scope = node;
            }

            return scope;
        }

        static void Assign(Image image, Sprite big)
        {
            if (image == null || big == null)
            {
                return;
            }

            Sprite sprite = Fit(big);
            if (sprite == null)
            {
                return;
            }

            if (image.sprite == sprite && image.overrideSprite == null && image.type == Image.Type.Simple && !image.useSpriteMesh)
            {
                return;
            }

            Vector2 size = image.rectTransform.sizeDelta;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            image.preserveAspect = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.sprite = sprite;
            image.overrideSprite = null;
            image.rectTransform.sizeDelta = size;
        }

        static void Assign(SpriteRenderer renderer, Sprite big)
        {
            if (renderer == null || big == null || renderer.sprite == null)
            {
                return;
            }

            Sprite sprite = Fit(big);
            if (sprite == null || renderer.sprite == sprite)
            {
                return;
            }

            int id = renderer.GetInstanceID();
            Vector3 scale;
            if (!rendererScale.TryGetValue(id, out scale))
            {
                scale = renderer.transform.localScale;
                rendererScale[id] = scale;
            }

            float oldHeight = renderer.sprite.bounds.size.y;
            renderer.sprite = sprite;
            float newHeight = sprite.bounds.size.y;
            if (oldHeight > 0.01f && newHeight > 0.01f)
            {
                float factor = oldHeight / newHeight;
                renderer.transform.localScale = new Vector3(scale.x * factor, scale.y * factor, scale.z);
            }
        }

        static Sprite Fit(Sprite big)
        {
            if (big == null || big.texture == null)
            {
                return null;
            }

            int id = big.GetInstanceID();
            if (fitted != null && fittedId == id)
            {
                return fitted;
            }

            ClearFit();
            Texture2D texture = Raster(big);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "guest-corner-fit";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            texture.hideFlags = HideFlags.HideAndDontSave;
            fitted = sprite;
            fittedTex = texture;
            fittedId = id;
            return fitted;
        }

        static Texture2D Raster(Sprite big)
        {
            Texture source = big.texture;
            float texW = source.width;
            float texH = source.height;
            if (texW < 1f || texH < 1f)
            {
                return null;
            }

            Rect region = big.textureRect;
            if (region.width < 1f || region.height < 1f)
            {
                region = new Rect(0f, 0f, texW, texH);
            }

            region.x = Mathf.Clamp(region.x, 0f, texW - 1f);
            region.y = Mathf.Clamp(region.y, 0f, texH - 1f);
            region.width = Mathf.Clamp(region.width, 1f, texW - region.x);
            region.height = Mathf.Clamp(region.height, 1f, texH - region.y);

            int storedW = Block(region.width, region.height);
            int storedH = Block(region.height, region.width);
            Color32[] stored = ReadRegion(source, region, storedW, storedH);
            if (stored == null)
            {
                return null;
            }

            if (!HasInk(stored))
            {
                stored = ReadRegion(source, new Rect(0f, 0f, texW, texH), storedW, storedH);
                if (stored == null || !HasInk(stored))
                {
                    return null;
                }
            }

            int visW;
            int visH;
            Color32[] visual = Orient(stored, storedW, storedH, big.packingRotation, out visW, out visH);
            int drawW = Block(visW, visH);
            int drawH = Block(visH, visW);
            if (drawW != visW || drawH != visH)
            {
                visual = Scale(visual, visW, visH, drawW, drawH);
            }

            Color32[] canvas = new Color32[FitSize * FitSize];
            int ox = (FitSize - drawW) / 2;
            int oy = (FitSize - drawH) / 2;
            for (int y = 0; y < drawH; y++)
            {
                int from = y * drawW;
                int to = (oy + y) * FitSize + ox;
                Array.Copy(visual, from, canvas, to, drawW);
            }

            Texture2D texture = new Texture2D(FitSize, FitSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 0;
            texture.SetPixels32(canvas);
            texture.Apply(false, true);
            return texture;
        }

        static int Block(float along, float other)
        {
            float aspect = other < 1f ? 1f : along / other;
            int size = aspect >= 1f ? FitSize : Mathf.RoundToInt(FitSize * aspect);
            size = Mathf.Clamp(size, 4, FitSize);
            int pot = 4;
            while (pot * 2 <= size)
            {
                pot *= 2;
            }

            return pot;
        }

        static Color32[] ReadRegion(Texture source, Rect region, int width, int height)
        {
            float texW = source.width;
            float texH = source.height;
            RenderTexture previous = RenderTexture.active;
            RenderTexture buffer = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            buffer.filterMode = FilterMode.Bilinear;
            buffer.wrapMode = TextureWrapMode.Clamp;
            Texture2D read = null;
            try
            {
                Vector2 scale = new Vector2(region.width / texW, region.height / texH);
                Vector2 offset = new Vector2(region.x / texW, region.y / texH);
                Graphics.Blit(source, buffer, scale, offset);
                RenderTexture.active = buffer;
                read = new Texture2D(width, height, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                read.Apply(false, false);
                return read.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(buffer);
                if (read != null)
                {
                    UnityEngine.Object.Destroy(read);
                }
            }
        }

        static bool HasInk(Color32[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 16)
            {
                if (pixels[i].a > 8)
                {
                    return true;
                }
            }

            return false;
        }

        static Color32[] Orient(Color32[] src, int width, int height, SpritePackingRotation rotation, out int outW, out int outH)
        {
            if (rotation == SpritePackingRotation.FlipHorizontal)
            {
                outW = width;
                outH = height;
                return FlipX(src, width, height);
            }

            if (rotation == SpritePackingRotation.FlipVertical)
            {
                outW = width;
                outH = height;
                return FlipY(src, width, height);
            }

            if (rotation == SpritePackingRotation.Rotate180)
            {
                outW = width;
                outH = height;
                return FlipY(FlipX(src, width, height), width, height);
            }

            outW = width;
            outH = height;
            return src;
        }

        static Color32[] FlipX(Color32[] src, int width, int height)
        {
            Color32[] dst = new Color32[src.Length];
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    dst[row + (width - 1 - x)] = src[row + x];
                }
            }

            return dst;
        }

        static Color32[] FlipY(Color32[] src, int width, int height)
        {
            Color32[] dst = new Color32[src.Length];
            for (int y = 0; y < height; y++)
            {
                Array.Copy(src, y * width, dst, (height - 1 - y) * width, width);
            }

            return dst;
        }

        static Color32[] Scale(Color32[] src, int sw, int sh, int dw, int dh)
        {
            Color32[] dst = new Color32[dw * dh];
            for (int y = 0; y < dh; y++)
            {
                int sy = sh < 2 ? 0 : y * (sh - 1) / (dh - 1);
                for (int x = 0; x < dw; x++)
                {
                    int sx = sw < 2 ? 0 : x * (sw - 1) / (dw - 1);
                    dst[y * dw + x] = src[sy * sw + sx];
                }
            }

            return dst;
        }

        static void ClearFit()
        {
            if (fitted != null)
            {
                UnityEngine.Object.Destroy(fitted);
            }

            if (fittedTex != null)
            {
                UnityEngine.Object.Destroy(fittedTex);
            }

            fitted = null;
            fittedTex = null;
            fittedId = 0;
        }

        public static void Release()
        {
            ClearFit();
            rendererScale.Clear();
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

        void OnDestroy()
        {
            HeaderPortrait.Release();
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
