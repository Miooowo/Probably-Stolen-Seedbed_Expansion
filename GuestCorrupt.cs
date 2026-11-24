using System;
using System.Collections.Generic;
using MidnightPawn.Api;
using MidnightPawn.Game;
using UnityEngine;

namespace GuestMaidenCorrupt
{
    static class GuestCorrupt
    {
        static readonly string[] Tags =
        {
            "MAIDEN_FEMALE", "MAIDEN_LOWER", "MAIDEN_VIRGIN", "MAIDEN_ANAL_VIRGIN",
            "MAIDEN_VIRGIN_LOST", "MAIDEN_ANAL_VIRGIN_LOST", "MAIDEN_BIG_BREAST", "MAIDEN_YOUNG",
            "MAIDEN_DAUGHTER", "MAIDEN_OVULATING", "MAIDEN_PREGNANT", "MAIDEN_PREGNANT_DAYS",
            "MAIDEN_FORCE", "MAIDEN_INTEL", "MAIDEN_CHARM", "MAIDEN_NAME", "MAIDEN_SPRITE_SRC",
            "MAIDEN_FACTION", "MAIDEN_JOB", "MAIDEN_BODY", "MAIDEN_BREAST",
            "MAIDEN_AFFECTION", "MAIDEN_TRUST", "MAIDEN_HYPNOSIS", "MAIDEN_BRAND",
            "MAIDEN_INTERACT_CUR", "MAIDEN_INTERACT_MAX", "TRAIN_VALUE",
            "ORAL_COUNT", "SEX_COUNT", "ANAL_COUNT", "LACTATION_LV",
            "FACTION_LOWER_LEVEL", "FACTION_UPPER_LEVEL", "FACTION_SECURITY",
            "FACTION_BLACK_MARKET", "FACTION_REVOLUTION", "FACTION_CARTEL", "FACTION_CHURCH", "FACTION_TOURIST",
            "CONTRABAND_ITEM_TAG", "CONTRABAND_LEVEL", "STOLEN_TAG", "NOT_FOR_RESALE", "IMPORTANT_TAG"
        };

        static bool registered;
        static bool pendingSuccess;
        static string pendingText;
        static string pendingFailText;
        static bool noticeRewritten;
        static string pendingFaction;
        static GameItem pendingMaiden;
        static GameItem pendingTentacle;

        public static void Register()
        {
            if (registered)
            {
                return;
            }

            TentacleApi.RegisterCorruptible(ModEntry.ModId, IsCurrentGuest, Convert);
            registered = true;
        }

        public static void WriteChance(RichTextBuilder builder, GameItem item)
        {
            if (builder == null || !IsCurrentGuest(item))
            {
                return;
            }

            try
            {
                int half = GuestChance.Of(
                    FactionReputation.Get(TentacleApi.GetFaction(item)),
                    TentacleApi.GetAffection(item),
                    HealthData.GetLuck(),
                    SemenFed(),
                    TentacleApi.GetTrust(item));
                builder.AddLine(
                    "侵蚀成功率 " + GuestChance.Format(half),
                    true,
                    RenderHandler.ColorPalette.DarkCyan,
                    true,
                    false,
                    false,
                    false,
                    (RenderHandler.ColorPalette)(-1),
                    (RenderHandler.ColorPalette)(-1),
                    (RenderHandler.ColorPalette)(-1));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 显示成功率失败: " + ex.Message);
            }
        }

        public static bool TryDragGuest(GameItem source, GameItem target, out GameItem guest)
        {
            guest = null;
            GameItem maiden = null;
            if (IsCorruptTentacle(target) && source != null && TentacleApi.IsMaiden(source))
            {
                maiden = source;
            }
            else if (IsCorruptTentacle(source) && target != null && TentacleApi.IsMaiden(target))
            {
                maiden = target;
            }

            if (maiden == null)
            {
                return false;
            }

            if (IsCurrentGuest(maiden))
            {
                guest = maiden;
                return true;
            }

            GameItem current = CurrentGuest();
            if (current == null || !TentacleApi.IsMaiden(current) || !SameGuest(maiden, current))
            {
                return false;
            }

            guest = current;
            return true;
        }

        static bool IsCorruptTentacle(GameItem item)
        {
            return item != null && item.identifier == "corrupt_tentacle";
        }

        static bool SameGuest(GameItem item, GameItem current)
        {
            if (ReferenceEquals(item, current))
            {
                return true;
            }

            if (item.uniqueId != 0 && item.uniqueId == current.uniqueId)
            {
                return true;
            }

            string name = TentacleApi.GetName(item);
            return !string.IsNullOrEmpty(name) && name == TentacleApi.GetName(current);
        }

        public static bool IsCurrentGuest(GameItem item)
        {
            if (item == null || !TentacleApi.IsMaiden(item))
            {
                return false;
            }

            GameItem guest = CurrentGuest();
            if (guest == null)
            {
                return false;
            }

            if (ReferenceEquals(item, guest))
            {
                return true;
            }

            return item.uniqueId != 0 && item.uniqueId == guest.uniqueId;
        }

        public static void FinishDelivery(GameItem tentacle)
        {
            if (!pendingSuccess)
            {
                if (!noticeRewritten && !string.IsNullOrEmpty(pendingFailText))
                {
                    Toast.Show(pendingFailText, "触手");
                }

                ClearDelivery();
                return;
            }

            pendingSuccess = false;
            if (GuestRemains())
            {
                ClearDelivery();
                return;
            }

            if (tentacle != null)
            {
                TentacleApi.AddCharge(tentacle, 10);
            }

            if (!string.IsNullOrEmpty(pendingFaction))
            {
                FactionReputation.Add(pendingFaction, 1);
            }

            pendingFaction = null;
            if (!noticeRewritten && !string.IsNullOrEmpty(pendingText))
            {
                Toast.Show(pendingText, "触手");
            }

            ClearDelivery();
            CommonSense.Grant();
        }

        public static void RewriteNotice(ref string text)
        {
            if (pendingSuccess && text == "触手把那个东西整个吞了下去，重新吐出一件新的东西。" && !string.IsNullOrEmpty(pendingText))
            {
                text = pendingText;
                noticeRewritten = true;
                return;
            }

            if (!string.IsNullOrEmpty(pendingFailText) && text == "这根触手试了试，没能把那个东西变成什么有用的东西。")
            {
                text = pendingFailText;
                pendingFailText = null;
                noticeRewritten = true;
            }
        }

        public static void NoteTentacle(GameItem tentacle)
        {
            pendingTentacle = tentacle;
        }

        public static bool TryClaimBrood(GameItem it, out string where)
        {
            where = null;
            if (!pendingSuccess || pendingMaiden == null || !ReferenceEquals(it, pendingMaiden))
            {
                return false;
            }

            foreach (GameGridInventory brood in BroodTargets(pendingTentacle))
            {
                try
                {
                    ModHelper.SetItemOwnerShip(it, true);
                    if (TentacleBody.PlaceInto(brood, it))
                    {
                        where = "苗床区";
                        Debug.Log("[苗床顾客侵蚀] 已放进苗床区");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[苗床顾客侵蚀] 放进苗床区失败: " + ex.Message);
                }
            }

            return false;
        }

        static IEnumerable<GameGridInventory> BroodTargets(GameItem tentacle)
        {
            List<GameItem> bodies = TentacleBody.InstancesSnapshot();
            if (bodies == null)
            {
                yield break;
            }

            GameGridInventory open = null;
            var rest = new List<GameGridInventory>();
            var seen = new HashSet<GameGridInventory>();
            foreach (GameItem body in bodies)
            {
                if (body == null || !Owned(body))
                {
                    continue;
                }

                GameGridInventory brood = BroodOf(body);
                if (brood == null || !seen.Add(brood))
                {
                    continue;
                }

                if (tentacle != null && Holds(body, tentacle))
                {
                    yield return brood;
                    continue;
                }

                if (open == null && IsOpen(body))
                {
                    open = brood;
                }
                else
                {
                    rest.Add(brood);
                }
            }

            if (open != null)
            {
                yield return open;
            }

            foreach (GameGridInventory brood in rest)
            {
                yield return brood;
            }
        }

        static GameGridInventory BroodOf(GameItem body)
        {
            List<GameGridInventory> grids = TentacleBody.GridsOf(body);
            return grids == null ? null : TentacleBody.BroodOf(grids);
        }

        static bool Holds(GameItem body, GameItem item)
        {
            if (item == null || item.parentInventory == null)
            {
                return false;
            }

            List<GameGridInventory> grids = TentacleBody.GridsOf(body);
            if (grids == null)
            {
                return false;
            }

            foreach (GameGridInventory grid in grids)
            {
                if (grid != null && ReferenceEquals(item.parentInventory, grid))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsOpen(GameItem body)
        {
            PixelWindow window = body.contentWindow;
            return window != null && window.IsVisible();
        }

        static bool Owned(GameItem item)
        {
            try
            {
                return GeneralHelper.IsItemOwned(item);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 读取本体归属失败: " + ex.Message);
                return true;
            }
        }

        static void ClearDelivery()
        {
            pendingText = null;
            pendingFailText = null;
            noticeRewritten = false;
            pendingFaction = null;
            pendingMaiden = null;
            pendingTentacle = null;
        }

        static void Flee()
        {
            try
            {
                if (DialogUIManager.Instance != null)
                {
                    DialogUIManager.Instance.CurrentClientLeave(0f);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 顾客离开失败: " + ex.Message);
            }
        }

        static GameItem Convert(GameItem guest)
        {
            if (!IsCurrentGuest(guest))
            {
                return null;
            }

            string faction = TentacleApi.GetFaction(guest);
            string name = TentacleApi.GetName(guest);
            bool fed = SemenFed();
            bool augmented = guest.IsTag("MAIDEN_AUGMENTED");
            int affection = TentacleApi.GetAffection(guest);
            int luck = HealthData.GetLuck();
            int trust = TentacleApi.GetTrust(guest);
            int chance = GuestChance.Of(FactionReputation.Get(faction), affection, luck, fed, trust);
            int roll = UnityEngine.Random.Range(0, GuestChance.Full);
            if (!GuestChance.Succeeds(chance, roll))
            {
                if (augmented)
                {
                    pendingFailText = "侵蚀失败，改造体被击杀。";
                    KillAugmented();
                    return null;
                }

                bool intimidating = StartingPerk.IsIntimidating();
                FactionReputation.Add(faction, -FailureCost.ReputationLoss(fed, intimidating));
                GuestCrime.OnFailure(FailureCost.Crime(fed));
                pendingFailText = "侵蚀失败，顾客被你吓跑了。";
                Flee();
                return null;
            }

            GameItem maiden = CopyUnlocked(guest, faction);
            if (maiden == null)
            {
                Toast.Show(name + "被触手缠住了，但没能拖走。", "触手");
                return null;
            }

            if (augmented)
            {
                ElectronicMaiden.Attach(maiden);
                pendingSuccess = true;
                pendingFaction = null;
                pendingMaiden = maiden;
                pendingText = "侵蚀成功，沦为电子苗床。";
                return maiden;
            }

            pendingSuccess = true;
            pendingFaction = faction;
            pendingMaiden = maiden;
            pendingText = "侵蚀成功，顾客沦为了你的苗床。";
            return maiden;
        }

        static void KillAugmented()
        {
            try
            {
                AugHelper.OnGoodKill();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 改造体击杀结算失败: " + ex.Message);
            }

            StoreUIManager ui = null;
            bool shot = false;
            try
            {
                ui = StoreUIManager.Instance;
                shot = ui != null && ui.isShot;
                if (ui != null)
                {
                    ui.isShot = true;
                }

                if (PlayerStore.Instance != null)
                {
                    PlayerStore.Instance.DismissCurrentClient(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 清掉改造体失败: " + ex.Message);
            }
            finally
            {
                if (ui != null)
                {
                    ui.isShot = shot;
                }
            }

            try
            {
                GuestBox.Hide();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 收起改造体卡片失败: " + ex.Message);
            }
        }

        static bool SemenFed()
        {
            try
            {
                StoreClientInstance visit = PlayerStore.Instance == null ? null : PlayerStore.Instance.currentClientInstance;
                StoreClient client = visit == null ? null : visit.GetClientBlueprint();
                return client != null && TentacleEffects.IsAffected(client);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 确认精液是否作用于顾客失败: " + ex.Message);
                return false;
            }
        }

        static GameItem CopyUnlocked(GameItem source, string faction)
        {
            GameItem copy = DirectoryMaster.Item("maiden_lower", true);
            if (copy == null)
            {
                return null;
            }

            copy.name = source.name;
            copy.shortDescription = source.shortDescription;
            copy.flavorText = source.flavorText;
            foreach (string tag in Tags)
            {
                if (!source.IsTag(tag))
                {
                    continue;
                }

                TagState current = source.GetTagReadonly(tag);
                int number = current == null ? 0 : current.GetInt();
                string text = current == null ? null : current.GetString();
                copy.ModifyTag(tag, state =>
                {
                    state.Enable();
                    state.SetInt(number);
                    if (!string.IsNullOrEmpty(text))
                    {
                        state.SetString(text);
                    }
                }, false);
            }

            GeneralHelper.UnlockItem(copy);
            try
            {
                TentacleApi.RefreshDescription(copy);
            }
            catch
            {
            }

            copy.name = source.name;
            ApplySellPrice(copy, source, faction);
            ApplyPortrait(copy, source);
            return copy;
        }

        static void ApplySellPrice(GameItem copy, GameItem source, string faction)
        {
            try
            {
                long written = source.unitValue;
                long baseline = written > 0 ? 0 : MainPrice(copy, faction);
                int day = StoreStation.GetDayCounter();
                copy.unitValue = MaidenSellPrice.Adjust(written, baseline, day);
                Debug.Log("[苗床顾客侵蚀] 第 " + day + " 天，苗床售价 " + copy.unitValue);
            }
            catch (Exception ex)
            {
                copy.unitValue = source.unitValue;
                Debug.LogWarning("[苗床顾客侵蚀] 苗床售价失败: " + ex.Message);
            }
        }

        static long MainPrice(GameItem item, string faction)
        {
            return ItemFactory.FactionBase(faction)
                + ItemFactory.AttrValue(TagInt(item, "MAIDEN_FORCE"))
                + ItemFactory.AttrValue(TagInt(item, "MAIDEN_INTEL"))
                + ItemFactory.AttrValue(TagInt(item, "MAIDEN_CHARM"));
        }

        static int TagInt(GameItem item, string tag)
        {
            if (item == null || !item.IsTag(tag))
            {
                return 0;
            }

            TagState state = item.GetTagReadonly(tag);
            return state == null ? 0 : state.GetInt();
        }

        static void ApplyPortrait(GameItem copy, GameItem source)
        {
            string face = ClientSprite();
            if (string.IsNullOrEmpty(face))
            {
                face = KeyFromMaidenPath(source.spritePath);
            }

            if (!string.IsNullOrEmpty(face))
            {
                string injected = InjectFace(face);
                if (!string.IsNullOrEmpty(injected))
                {
                    copy.SetSpriteAndShapeFromMod("lewd_meme", injected);
                }
                else
                {
                    CopySprite(copy, source);
                }

                copy.ModifyTag("MAIDEN_SPRITE_SRC", state =>
                {
                    state.Enable();
                    state.SetString(face);
                }, false);
            }
            else
            {
                CopySprite(copy, source);
                DropRandomSource(copy);
            }

            if (copy.IsTag("IPH_SOURCE"))
            {
                copy.DisableTag("IPH_SOURCE", true);
            }

            if (source.shape != null)
            {
                copy.SetShape(source.shape.Clone());
            }

            RefreshShownSprite(copy);
            Debug.Log("[苗床顾客侵蚀] 立绘沿用 " + (string.IsNullOrEmpty(face) ? source.spritePath : face));
        }

        static void CopySprite(GameItem copy, GameItem source)
        {
            if (string.IsNullOrEmpty(source.spriteAtlasPath) || string.IsNullOrEmpty(source.spritePath))
            {
                return;
            }

            if (source.useSpriteFromMod)
            {
                copy.SetSpriteAndShapeFromMod(source.spriteAtlasPath, source.spritePath);
            }
            else
            {
                copy.SetSprite(source.spriteAtlasPath, source.spritePath);
            }
        }

        static void DropRandomSource(GameItem copy)
        {
            if (!copy.IsTag("MAIDEN_SPRITE_SRC"))
            {
                return;
            }

            string saved = copy.GetTagReadonly("MAIDEN_SPRITE_SRC")?.GetString();
            if (!string.IsNullOrEmpty(saved) && saved.StartsWith("iph|"))
            {
                copy.DisableTag("MAIDEN_SPRITE_SRC", true);
            }
        }

        static string ClientSprite()
        {
            try
            {
                StoreClientInstance client = PlayerStore.Instance == null ? null : PlayerStore.Instance.currentClientInstance;
                StoreClient blueprint = client == null ? null : client.GetClientBlueprint();
                return blueprint == null ? null : blueprint.spriteName;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 读取顾客立绘失败: " + ex.Message);
                return null;
            }
        }

        static string KeyFromMaidenPath(string path)
        {
            const string prefix = "maiden_";
            if (string.IsNullOrEmpty(path) || !path.StartsWith(prefix))
            {
                return null;
            }

            string key = path.Substring(prefix.Length);
            if (key.Length == 0 || key.StartsWith("iph"))
            {
                return null;
            }

            return key;
        }

        static string InjectFace(string face)
        {
            try
            {
                string key = face.IndexOf(':') >= 0
                    ? SpriteInjector.InjectPortraitTrimmed(face, 64, 64)
                    : SpriteInjector.InjectPortraitFromName(face, 64, 64);
                if (string.IsNullOrEmpty(key) && face.IndexOf(':') < 0)
                {
                    key = SpriteInjector.InjectPortraitTrimmed(face, 64, 64);
                }

                return key;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 注入顾客立绘失败: " + ex.Message);
                return null;
            }
        }

        static void RefreshShownSprite(GameItem item)
        {
            try
            {
                if (item is GameItemElement element)
                {
                    element.ValidatePartialSprite();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 立绘刷新失败: " + ex.Message);
            }
        }

        static GameItem CurrentGuest()
        {
            try
            {
                return GuestBox.CurrentItem;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 读取当前顾客失败: " + ex.Message);
                return null;
            }
        }

        static bool GuestRemains()
        {
            GameItem guest = CurrentGuest();
            if (guest == null)
            {
                return false;
            }

            try
            {
                return ItemLife.IsAlive(guest);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 确认顾客是否还在失败: " + ex.Message);
                return true;
            }
        }

    }

    static class GuestCrime
    {
        public const string Id = "INDECENT_CONSUMER";

        public static void OnFailure(int amount)
        {
            try
            {
                StoreClient client = Blueprint();
                if (client != null && (client.isNoContributeToEvidence || client.identifier == "cartelWoundedSoldier"))
                {
                    Debug.Log("[苗床顾客侵蚀] 该顾客不计入犯罪值");
                    return;
                }

                SecData record = PlayerStore.Instance == null ? null : PlayerStore.Instance.secData;
                if (record == null)
                {
                    return;
                }

                record.CommitCrime(Id, amount);
                Debug.Log("[苗床顾客侵蚀] 犯罪: 猥亵消费者 " + amount);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 记录犯罪失败: " + ex.Message);
            }
        }

        static StoreClient Blueprint()
        {
            StoreClientInstance visit = PlayerStore.Instance == null ? null : PlayerStore.Instance.currentClientInstance;
            return visit == null ? null : visit.GetClientBlueprint();
        }
    }

    static class FactionReputation
    {
        public static int Get(string faction)
        {
            switch (faction)
            {
                case "下层区":
                    return StoreReputation.GetLLReputation();
                case "上层区":
                    return StoreReputation.GetULReputation();
                case "治安部":
                    return StoreReputation.GetSecReputation();
                case "黑市":
                    return StoreReputation.GetBMReputation();
                case "革命军":
                    return StoreReputation.GetRevReputation();
                default:
                    return 0;
            }
        }

        public static void Add(string faction, int delta)
        {
            switch (faction)
            {
                case "下层区":
                    StoreReputation.ModLLReputation(delta);
                    break;
                case "上层区":
                    StoreReputation.ModULReputation(delta);
                    break;
                case "治安部":
                    StoreReputation.ModSecReputation(delta);
                    break;
                case "黑市":
                    StoreReputation.ModBMReputation(delta);
                    break;
                case "革命军":
                    StoreReputation.ModRevReputation(delta);
                    break;
                default:
                    StoreReputation.ModCurrentClientReputation(delta, true);
                    break;
            }
        }

        public static void Add(string faction, double delta)
        {
            try
            {
                string id = IdOf(faction);
                StoreReputation rep = string.IsNullOrEmpty(id) ? null : StoreReputation.GetStoreReputation(id);
                if (rep == null)
                {
                    Debug.LogWarning("[苗床顾客侵蚀] 没有找到阵营声望");
                    return;
                }

                rep.ModReputation(delta);
                if (ReputationUIManager.Instance != null)
                {
                    ReputationUIManager.Instance.NotifyReputation(id, delta < 0 ? -1 : 1);
                }

                Debug.Log("[苗床顾客侵蚀] 声望 " + delta);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[苗床顾客侵蚀] 调整声望失败: " + ex.Message);
            }
        }

        static string IdOf(string faction)
        {
            switch (faction)
            {
                case "下层区":
                    return "FACTION_LOWER_LEVEL";
                case "上层区":
                    return "FACTION_UPPER_LEVEL";
                case "治安部":
                    return "FACTION_SECURITY";
                case "黑市":
                    return "FACTION_BLACK_MARKET";
                case "革命军":
                    return "FACTION_REVOLUTION";
                default:
                    StoreClientInstance visit = PlayerStore.Instance == null ? null : PlayerStore.Instance.currentClientInstance;
                    StoreClient client = visit == null ? null : visit.GetClientBlueprint();
                    return client == null ? null : client.clientFaction;
            }
        }
    }
}
