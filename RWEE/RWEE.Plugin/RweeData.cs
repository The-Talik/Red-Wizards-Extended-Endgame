using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System; 
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RWEE
{
	[HarmonyPatch(typeof(GameData), "SetGameData")]
	static class Patch_GameData_SetGameData
	{
		static void Postfix()
		{
			RweeData.Init(true);
			String saveVersion = RweeData.GetString("RWEESaveVersion");
			Main.log($"Save version: {saveVersion} Local Version: {Main.pluginVersion}");
			//if (VersionControl.IsNewer(Main.pluginVersion, saveVersion))
			if (saveVersion == null)
			{
				Main.log("Updating Universe.");
				int sectorsUpdated = 0;
				for (int i = 0; i < GameData.data.sectors.Count; i++)
				{
					int cX = GameData.data.sectors[i].x;
					int cY = GameData.data.sectors[i].y;
					int staticLevel = (int)Vector2.Distance(new Vector2(25f, 14f), new Vector2((float)cX, (float)cY));
					float minLevel = Sectors.calculateMinLevel(cX, cY, staticLevel);
					float maxLevel = Sectors.calculateMaxLevel(cX, cY, staticLevel);
					if (GameData.data.sectors[i].level > 40 && GameData.data.sectors[i].level < minLevel)
					{
						sectorsUpdated++;
						int newLevel = Sectors.calculateLevel(cX, cY, staticLevel);
						Main.warn($"Sector level updated from {GameData.data.sectors[i].level} to {newLevel}");
						GameData.data.sectors[i].AdjustLevel(newLevel, false, false, false);

					}
					Main.log($"Sector level min/act/max: {staticLevel} {minLevel} {GameData.data.sectors[i].level} {maxLevel}");
				}
				if (sectorsUpdated > 0)
				{
					RW.SimplePopup.Show($"{sectorsUpdated} sectors have been leveled beyond the normal cap of 55. If you might want to uninstall this mod, it is recommended that you create a copy of your save file before your next save.");
				}
			}
			RweeData.SetString("RWEESaveVersion", Main.pluginVersion);
			if(Items.debugUpgrades)
			{
				var items = ItemDB.GetItems(false);
				//CargoSystem cs = PlayerControl.inst.GetCargoSystem;
				for (int i=0; i< items.Count; i++)
				{
					if (items[i].refName.StartsWith("rwee"))
					{
						GameData.data.AddCargoItemForced(3, items[i].id, items[i].rarity, 1, -1);
						SideInfo.AddMsg(Lang.Get(6, 18, ItemDB.GetItemNameModified(items[i].id, 2)));
					}
				}
				var equipments = EquipmentDB.GetList(99999);
				//CargoSystem cs = PlayerControl.inst.GetCargoSystem;
				for (int i = 0; i < equipments.Count; i++)
				{
					if (equipments[i].refName.StartsWith("rwee"))
					{
						GameData.data.AddCargoItemForced(2, equipments[i].id, 1, 1, -1);
						SideInfo.AddMsg(Lang.Get(6, 18, ItemDB.GetItemNameModified(equipments[i].id, 2)));
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(GameData), "SaveGame")]
	static class Patch_GameData_SaveGame
	{
		static void Prefix()
		{
			RweeData.Flush();
		}
	}
	public static class RweeData
	{
		// in-memory cache
		static Dictionary<string, string> _kv;
		static FieldInfo _fld; // GameDataInfo.rweeJson

		public static void Init(bool force = false)
		{
			//Main.log("RweeData.Init()");
			if (_fld == null)
				_fld = typeof(GameDataInfo).GetField("rweeJson", BindingFlags.Public | BindingFlags.Instance);
			//Main.log("RweeData.Init: initiated _fld");
			if (force)
				_kv = null;
			else
				if (_kv != null) return;

			string raw = null;
			if (GameData.data != null && _fld != null)
				raw = _fld.GetValue(GameData.data) as string;
			else
				Main.log("RweeData.Init: error getting data");

			_kv = Parse(raw);
			Main.log("[RWEE] RweeData.Init: v=" + (_kv.ContainsKey("v") ? _kv["v"] : "?") + " keys=" + _kv.Count + " bytes=" + (raw != null ? raw.Length : 0));

			if (!_kv.ContainsKey("v")) _kv["v"] = "1"; // version tag
		}

		public static void Flush() // write back to GameData.data before saving
		{
			//Main.log("WreeData.Flush()");
			Init();
			if (GameData.data == null || _fld == null || _kv == null) return;
			_fld.SetValue(GameData.data, Serialize(_kv));
		}

		// ---- typed helpers ----
		public static int GetInt(string key, int defVal = 0)
		{
			Init();
			if (_kv.TryGetValue(key, out var s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
				return v;
			return defVal;
		}
		public static void SetInt(string key, int value)
		{
			Init();
			_kv[key] = value.ToString(CultureInfo.InvariantCulture);
		}
		public static int IncInt(string key, int amt = 1)
		{
			int value = GetInt(key);
			SetInt(key, value + amt);
			return value + amt;
		}

		public static string GetString(string key, string defVal = null)
		{
			Init();
			return _kv.TryGetValue(key, out var s) ? s : defVal;
		}
		public static void SetString(string key, string value)
		{
			Init();
			_kv[key] = value ?? "";
		}

		// ---- minimal line-based format: key=escapedValue per line ----
		static Dictionary<string, string> Parse(string raw)
		{
			var dict = new Dictionary<string, string>(StringComparer.Ordinal);
			if (string.IsNullOrEmpty(raw)) return dict;

			var lines = raw.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i].TrimEnd('\r');
				if (line.Length == 0 || line[0] == '#') continue;
				int eq = line.IndexOf('=');
				if (eq <= 0) continue;
				var k = line.Substring(0, eq);
				var v = Unescape(line.Substring(eq + 1));
				dict[k] = v;
			}
			return dict;
		}

		static string Serialize(Dictionary<string, string> kv)
		{
			var sb = new StringBuilder();
			foreach (var p in kv)
			{
				sb.Append(p.Key).Append('=').Append(Escape(p.Value)).Append('\n');
			}
			return sb.ToString();
		}

		static string Escape(string s) =>
			(s ?? "").Replace("\\", "\\\\").Replace("\n", "\\n").Replace("=", "\\e");

		static string Unescape(string s)
		{
			if (string.IsNullOrEmpty(s)) return "";
			var sb = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c == '\\' && i + 1 < s.Length)
				{
					char n = s[++i];
					if (n == 'n') sb.Append('\n');
					else if (n == '\\') sb.Append('\\');
					else if (n == 'e') sb.Append('=');
					else { sb.Append('\\').Append(n); }
				}
				else sb.Append(c);
			}
			return sb.ToString();
		}
	}
/*	[HarmonyPatch(typeof(GameData), "LoadGameAsync")]
	static class GameData_LoadGameAsync
	{
		static void Postfix()
		{
			Main.log($"GameData loaded");
		}
	*/
}
