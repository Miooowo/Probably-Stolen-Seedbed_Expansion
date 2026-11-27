using System;
using HarmonyLib;
using MidnightPawn.Game;
using UnityEngine;
using UnityEngine.UI;

namespace GuestMaidenCorrupt
{
    // 左上角是客人格里那张图，名字写在它下面。进店时烤的是像素副本，
    // 不是大窗口上的 SpriteRenderer，也不是图库里的 inventor。
    // 大窗口之后被别的模组换掉时，再把这张图改成大窗口当前的立绘。
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
                if (big == null || Current() == null)
                {
                    noted = null;
                    return;
                }

                GameItemElement card = GuestBox.CurrentItem as GameItemElement;
                if (card == null)
                {
                    return;
                }

                Paint(card.background, big);
                Paint(card.fakeBackground, big);
                if (noted != big && card.background != null && card.background.image != null && card.background.image.sprite == big)
                {
                    noted = big;
                    Debug.Log("[苗床顾客侵蚀] 左上角客人格已跟上大窗口当前立绘");
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

        static void Paint(TreeNodeRender node, Sprite big)
        {
            if (node == null || node.image == null || big == null)
            {
                return;
            }

            Image image = node.image;
            if (image.sprite != big)
            {
                image.sprite = big;
            }

            if (image.overrideSprite != null && image.overrideSprite != big)
            {
                image.overrideSprite = null;
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
