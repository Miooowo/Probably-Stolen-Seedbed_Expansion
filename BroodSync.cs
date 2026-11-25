using System;
using System.Collections.Generic;
using HarmonyLib;
using MidnightPawn.Game;
using UnityEngine;

namespace GuestMaidenCorrupt
{
	// 苗床 ↔ 顾客池双向同步：
	//   1) 捕获时给苗床补 MAIDEN_IDENTITY_KEY，并把姓名写进 captured_held 名单。
	//   2) 已捕获的顾客再到店时（BuildGuestItem）打 ALREADY_CAPTURED 标记，侵蚀入口拒收。
	//   3) 每夜（睡醒后）遍历手里带身份 key 的苗床，SaveProfile/SaveSocial 灌回池；
	//      captured_held 里找不到对应苗床的，RemoveByName 从池移除，池自然补新人。
	//   4) 客人格离场（GuestBox.Hide）时把当次变化 tag 拷回对应苗床。
	internal static class BroodSync
	{
		private const string OWNER = "guest_maiden_corrupt";
		private const string KEY_HELD = "captured_held";

		// 离场时只回写「来访期间会变化的社交值」（好感/信任/催眠）。
		// 注意：客人格每次都由 GuestBox.BuildGuestItem → MaidenIdentity.ResetSexState 重建，
		// 处/肛处被强制置回“处女”，孕/排卵被清掉，训练/口/性/肛/烙印被清零。
		// 所以这些状态值绝不能从客人格拷回苗床，否则会把已破处/已调教的苗床洗回处女或清零。
		private static readonly string[] SyncTags = new string[]
		{
			"MAIDEN_AFFECTION", "MAIDEN_TRUST", "MAIDEN_HYPNOSIS"
		};

		// ---------- captured_held 名单 ----------

		private static List<string[]> LoadHeld()
		{
			List<string[]> result = new List<string[]>();
			try
			{
				string data = ModHelper.GetModData(OWNER, KEY_HELD, "");
				if (!string.IsNullOrEmpty(data))
				{
					string[] entries = data.Split(',');
					for (int i = 0; i < entries.Length; i++)
					{
						string trimmed = entries[i].Trim();
						if (trimmed.Length == 0)
						{
							continue;
						}
						string[] pair = trimmed.Split('|');
						if (pair.Length >= 2 && !string.IsNullOrEmpty(pair[0]))
						{
							result.Add(new string[] { pair[0], pair[1] ?? "" });
						}
					}
				}
			}
			catch
			{
			}
			return result;
		}

		private static void SaveHeld(List<string[]> held)
		{
			try
			{
				List<string> parts = new List<string>();
				for (int i = 0; i < held.Count; i++)
				{
					parts.Add(held[i][0] + "|" + (held[i][1] ?? ""));
				}
				ModHelper.SetModData(OWNER, KEY_HELD, string.Join(",", parts.ToArray()));
			}
			catch
			{
			}
		}

		public static bool IsHeld(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			List<string[]> held = LoadHeld();
			for (int i = 0; i < held.Count; i++)
			{
				if (held[i][0] == name)
				{
					return true;
				}
			}
			return false;
		}

		public static void MarkCaptured(string name, string category)
		{
			if (string.IsNullOrEmpty(name))
			{
				return;
			}
			try
			{
				List<string[]> held = LoadHeld();
				for (int i = 0; i < held.Count; i++)
				{
					if (held[i][0] == name)
					{
						return;
					}
				}
				held.Add(new string[] { name, category ?? "" });
				SaveHeld(held);
				Debug.Log("[苗床顾客侵蚀] 已登记捕获: " + name + " (" + category + ")");
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[苗床顾客侵蚀] 登记捕获失败: " + ex.Message);
			}
		}

		private static void RemoveHeld(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return;
			}
			try
			{
				List<string[]> held = LoadHeld();
				int before = held.Count;
				held.RemoveAll((string[] h) => h[0] == name);
				if (held.Count != before)
				{
					SaveHeld(held);
				}
			}
			catch
			{
			}
		}

		// ---------- tag 拷贝 ----------

		private static void CopyTag(GameItem from, GameItem to, string tag)
		{
			if (from == null || to == null)
			{
				return;
			}
			try
			{
				if (!from.IsTag(tag))
				{
					return;
				}
				TagState src = from.GetTagReadonly(tag);
				int num = ((src == null) ? 0 : src.GetInt());
				string str = ((src == null) ? null : src.GetString());
				to.ModifyTag(tag, delegate(TagState s)
				{
					s.Enable();
					s.SetInt(num);
					if (!string.IsNullOrEmpty(str))
					{
						s.SetString(str);
					}
				}, false);
			}
			catch
			{
			}
		}

		// ---------- 查找被持有的对应苗床 ----------

		private static GameItem FindHeldMaiden(string name, string key)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			List<GameItem> owned = ItemLife.AllOwned();
			for (int i = 0; i < owned.Count; i++)
			{
				GameItem it = owned[i];
				if (it == null)
				{
					continue;
				}
				try
				{
					if (!it.IsTag("MAIDEN_FEMALE"))
					{
						continue;
					}
					string n = ItemFactory.GetStr(it, "MAIDEN_NAME");
					if (n != name)
					{
						continue;
					}
					if (!string.IsNullOrEmpty(key))
					{
						string k = ItemFactory.GetStr(it, "MAIDEN_IDENTITY_KEY");
						if (!string.IsNullOrEmpty(k) && k != key)
						{
							continue;
						}
					}
					return it;
				}
				catch
				{
				}
			}
			return null;
		}

		// ---------- 离场同步：客人格 → 苗床 ----------

		public static void SyncGuestToMaiden(GameItem guest)
		{
			if (guest == null)
			{
				return;
			}
			try
			{
				string name = ItemFactory.GetStr(guest, "MAIDEN_NAME");
				string key = ItemFactory.GetStr(guest, "MAIDEN_IDENTITY_KEY");
				if (string.IsNullOrEmpty(name))
				{
					return;
				}
				GameItem maiden = BroodSync.FindHeldMaiden(name, key);
				if (maiden == null)
				{
					return;
				}
				string[] syncTags = BroodSync.SyncTags;
				for (int i = 0; i < syncTags.Length; i++)
				{
					BroodSync.CopyTag(guest, maiden, syncTags[i]);
				}
				Debug.Log("[苗床顾客侵蚀] 离场同步: " + name + " → 苗床");
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[苗床顾客侵蚀] 离场同步失败: " + ex.Message);
			}
		}

		// ---------- 每夜同步：苗床 → 池 + 消失检测 ----------

		public static void OnMorningSync()
		{
			try
			{
				List<GameItem> owned = ItemLife.AllOwned();
				HashSet<string> alive = new HashSet<string>();
				for (int i = 0; i < owned.Count; i++)
				{
					GameItem it = owned[i];
					if (it == null)
					{
						continue;
					}
					try
					{
						if (!it.IsTag("MAIDEN_FEMALE"))
						{
							continue;
						}
						string key = ItemFactory.GetStr(it, "MAIDEN_IDENTITY_KEY");
						if (string.IsNullOrEmpty(key))
						{
							continue;
						}
						string name = ItemFactory.GetStr(it, "MAIDEN_NAME");
						if (string.IsNullOrEmpty(name))
						{
							continue;
						}
						ClientPool.SaveProfile(key, name, it);
						double affAcc;
						double trsAcc;
						ClientPool.LoadSocialAcc(key, name, out affAcc, out trsAcc);
						ClientPool.SaveSocial(key, name, ItemFactory.GetInt(it, "MAIDEN_AFFECTION"), ItemFactory.GetInt(it, "MAIDEN_TRUST"), ItemFactory.GetInt(it, "MAIDEN_HYPNOSIS"), affAcc, trsAcc);
						alive.Add(name);
					}
					catch (Exception ex)
					{
						Debug.LogWarning("[苗床顾客侵蚀] 每夜同步单件失败: " + ex.Message);
					}
				}
				// 消失检测：名单里有、但手里已没有对应苗床 → 从池移除，自然补新人。
				List<string[]> held = LoadHeld();
				List<string> gone = new List<string>();
				for (int j = 0; j < held.Count; j++)
				{
					string[] h = held[j];
					if (alive.Contains(h[0]))
					{
						continue;
					}
					gone.Add(h[0]);
					if (!string.IsNullOrEmpty(h[1]))
					{
						try
						{
							ClientPool.RemoveByName(h[1], h[0]);
							Debug.Log("[苗床顾客侵蚀] 苗床已消失，从池移除: " + h[0] + " (" + h[1] + ")");
						}
						catch
						{
						}
					}
				}
				for (int k = 0; k < gone.Count; k++)
				{
					BroodSync.RemoveHeld(gone[k]);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[苗床顾客侵蚀] 每夜同步异常: " + ex.Message);
			}
		}
	}

	// 已捕获顾客再到店：给客人格打 ALREADY_CAPTURED 标记。
	[HarmonyPatch(typeof(GuestBox), "BuildGuestItem")]
	internal static class MarkCapturedGuestPatch
	{
		private static void Postfix(GameItem __result)
		{
			if (__result == null)
			{
				return;
			}
			try
			{
				string name = ItemFactory.GetStr(__result, "MAIDEN_NAME");
				if (BroodSync.IsHeld(name))
				{
					__result.ModifyTag("ALREADY_CAPTURED", delegate(TagState s)
					{
						s.Enable();
					}, false);
					Debug.Log("[苗床顾客侵蚀] 已捕获顾客再来: " + name + "（标记禁止再抓）");
					CommonSense.Grant();
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[苗床顾客侵蚀] 标记禁止再抓失败: " + ex.Message);
			}
		}
	}

	// 客人格离场前，把当次变化拷回对应苗床。
	[HarmonyPatch(typeof(GuestBox), "Hide")]
	internal static class DepartureSyncPatch
	{
		private static void Prefix()
		{
			try
			{
				BroodSync.SyncGuestToMaiden(GuestBox.CurrentItem);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[苗床顾客侵蚀] 离场同步失败: " + ex.Message);
			}
		}
	}
}
