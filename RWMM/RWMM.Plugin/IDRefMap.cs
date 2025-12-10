using HarmonyLib;
using RW;
using RWMM.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace RWMM
{
	public class IdRefMapJson
	{
		public List<Pair> Items = new List<Pair>();
		public List<Pair> Equipment = new List<Pair>();

		public class Pair
		{
			public int Id;
			public string Name;
			[IgnoreDataMember]
			public int NewId;
		}

		public List<Pair> GetByType(object obj)
		{
			int type = 0;
			switch (obj.GetType().Name)
			{
				//case "Item":
				case "CargoItem":
				case "ItemStockData":
				case "MarketItem":
					type = ObjUtils.GetField<int>(obj, "itemType");
					break;

				case "InstalledEquipment":
					return Equipment;
			}

			switch (type)
			{
				case 2:
					return Equipment; // Equipment
				case 3:
					return Items;     // Item
				default:
					return null;
			}
		}
	}

	internal static class IdRefMap
	{
		internal static readonly IdRefMapJson Map = new IdRefMapJson();
		// internal static readonly Dictionary<int, string> Map = new Dictionary<int, string>(1024);

		private static string GetSaveField()
		{
			var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
			return fi?.GetValue(GameData.data) as string;
		}

		private static void SetSaveField(string json)
		{
			var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
			if (fi != null)
				fi.SetValue(GameData.data, json);
			else
				Main.error("Save field 'rweeItemMapJson' not found (prepatcher missing?).");
		}

		internal static void SaveToGameData()
		{
			try
			{
				var items = ItemDB.GetItems(false);
				var equipment = EquipmentDB.GetList(false);

				MakeMapType(Map.Items, items);
				MakeMapType(Map.Equipment, equipment);

				string firstItemName = Map.Items.Count > 0 ? Map.Items[0].Name : "<none>";
				Main.log($"Map before JSON: items={Map.Items.Count} equip={Map.Equipment.Count} firstItem='{firstItemName}'");

				var json = JsonUtils.ToJson(Map);
				if (json == "{}")
					Main.error($"EMPTY JSON: {json}");
				else
					Main.log($"JSON: {json}");

				SetSaveField(json);
				Main.log($"Saved id/ref map (items={Map.Items.Count} equip={Map.Equipment.Count}).");
			}
			catch (Exception ex)
			{
				Main.warn("SaveToGameData failed: " + ex);
			}
		}

		internal static void MakeMapType<T>(List<IdRefMapJson.Pair> mapList, List<T> objects)
		{
			if (objects == null)
				return;

			mapList.Clear();

			for (int i = 0; i < objects.Count; i++)
			{
				var it = objects[i];
				if (it == null) continue;

				int id;
				string refName;

				try
				{
					id = ObjUtils.GetField<int>(it, "id");
					refName = ObjUtils.GetRef(it);
				}
				catch (Exception ex)
				{
					Main.error($"Could not get id/ref for object of type {it.GetType()}: {ex.Message}");
					continue; // skip bad entry, don't bail the whole map
				}

				if (string.IsNullOrEmpty(refName))
				{
					Main.error($"Could not get refName for object of type {it.GetType()}.");
					continue;
				}

				mapList.Add(new IdRefMapJson.Pair { Id = id, Name = refName });
			}
		}

		internal static void LoadFromGameData()
		{
			Main.log("RefMap LoadFromGameData");
			try
			{
				// 0) Always start clean
				Map.Items.Clear();
				Map.Equipment.Clear();

				// 1) Try saved JSON
				var json = GetSaveField();
				IdRefMapJson data = null;

				if (!string.IsNullOrEmpty(json))
				{
					Main.log($"Found json: {json}");
					try
					{
						data = JsonUtils.FromJson<IdRefMapJson>(json);
					}
					catch (Exception ex)
					{
						Main.warn("Bad id/ref JSON in save, will use defaults: " + ex.Message);
					}

					if (data != null)
					{
						int itemsCount = data.Items != null ? data.Items.Count : 0;
						int equipCount = data.Equipment != null ? data.Equipment.Count : 0;
						Main.log($"Loaded items: {itemsCount} equip: {equipCount}");
					}
					else
					{
						Main.warn("Parsed id/ref JSON is null; will use defaults.");
					}
				}
				else
				{
					Main.warn("No id/ref JSON found in save.");
				}

				// 2) Fallback defaults if missing/corrupt
				if (data == null || data.Items == null || data.Items.Count == 0)
				{
					Main.warn("Using Defaults");
					data = new IdRefMapJson
					{
						Items = new List<IdRefMapJson.Pair>
						{
							new IdRefMapJson.Pair { Id = 71, Name = "rwee_mystic_relic_generator" },
							new IdRefMapJson.Pair { Id = 72, Name = "rwee_arcane_orb_generator" },
							new IdRefMapJson.Pair { Id = 73, Name = "rwee_mystic_relic_battery" },
							new IdRefMapJson.Pair { Id = 74, Name = "rwee_arcane_orb_battery" },
							new IdRefMapJson.Pair { Id = 75, Name = "rwee_mystic_relic_engine" },
							new IdRefMapJson.Pair { Id = 76, Name = "rwee_arcane_orb_engine" },
							new IdRefMapJson.Pair { Id = 77, Name = "rwee_mystic_relic_booster" },
							new IdRefMapJson.Pair { Id = 78, Name = "rwee_arcane_orb_booster" },
							new IdRefMapJson.Pair { Id = 79, Name = "rwee_mystic_relic_maneuverability" },
							new IdRefMapJson.Pair { Id = 80, Name = "rwee_arcane_orb_maneuverability" },
							new IdRefMapJson.Pair { Id = 81, Name = "rwee_mystic_relic_armor" },
							new IdRefMapJson.Pair { Id = 82, Name = "rwee_arcane_orb_armor" },
							new IdRefMapJson.Pair { Id = 83, Name = "rwee_mystic_relic_shield" },
							new IdRefMapJson.Pair { Id = 84, Name = "rwee_arcane_orb_shield" },
							new IdRefMapJson.Pair { Id = 85, Name = "rwee_mystic_relic_sensor" },
							new IdRefMapJson.Pair { Id = 86, Name = "rwee_arcane_orb_sensor" },
							new IdRefMapJson.Pair { Id = 87, Name = "rwee_mystic_relic_computer" },
							new IdRefMapJson.Pair { Id = 88, Name = "rwee_arcane_orb_computer" },
							new IdRefMapJson.Pair { Id = 89, Name = "rwee_mystic_relic_device" },
							new IdRefMapJson.Pair { Id = 90, Name = "rwee_arcane_orb_device" },
							new IdRefMapJson.Pair { Id = 91, Name = "rwee_mystic_relic_utility" },
							new IdRefMapJson.Pair { Id = 92, Name = "rwee_arcane_orb_utility" }
						},
						Equipment = new List<IdRefMapJson.Pair>
						{
							new IdRefMapJson.Pair { Id = 198, Name = "rwee_Pirate Capital Booster" }
						}
					};
					Main.warn("No saved id/ref map found; using defaults.");
				}

				// 3) Build remap lists only for CHANGED ids (name wins)
				RestoreType<Item>(data.Items, ItemDB.GetItems(false), Map.Items);
				RestoreType<Equipment>(data.Equipment, EquipmentDB.GetList(false), Map.Equipment);

				int dataItemsCount2 = data.Items != null ? data.Items.Count : 0;
				int dataEquipCount2 = data.Equipment != null ? data.Equipment.Count : 0;
				Main.log($"Loaded id/ref map ID changes: (items={Map.Items.Count}/{dataItemsCount2} equip={Map.Equipment.Count}/{dataEquipCount2}).");
			}
			catch (Exception ex)
			{
				Main.error("LoadFromGameData failed: " + ex);
			}
		}

		internal static void RestoreType<T>(List<IdRefMapJson.Pair> savedList, List<T> dbItems, List<IdRefMapJson.Pair> targetMap)
			where T : class
		{
			if (savedList == null || dbItems == null || targetMap == null)
				return;

			for (int i = 0; i < savedList.Count; i++)
			{
				var saved = savedList[i];

				T current = null;

				// find by old ID in current DB
				for (int j = 0; j < dbItems.Count; j++)
				{
					var candidate = dbItems[j];
					if (candidate == null) continue;

					int candidateId;
					try
					{
						candidateId = ObjUtils.GetField<int>(candidate, "id");
					}
					catch
					{
						continue;
					}

					if (candidateId == saved.Id)
					{
						current = candidate;
						break;
					}
				}

				string current_refName = current != null ? ObjUtils.GetRef(current) : null;

				if (current == null || !string.Equals(saved.Name, current_refName, StringComparison.Ordinal))
				{
					Main.log($"Remapping ID for '{saved.Name}' (old id {saved.Id})");

					// Find by refName in current DB
					var match = ListUtils.GetByRef(dbItems, saved.Name);
					if (match == null)
						match = ListUtils.GetByRef(dbItems, "rwee_" + saved.Name); // backwards compatibility

					if (match != null)
					{
						saved.NewId = ObjUtils.GetField<int>(match, "id");
						targetMap.Add(saved);
						Main.warn($"ID item change detected: {saved.Name} {saved.Id}->{saved.NewId}");
					}
					else
					{
						Main.error($"Item remap '{saved.Name}' (old id {saved.Id}) not found in current DB.\n");
					}
				}
			}
		}

		internal static void fixItems(ref List<CargoItem> items)
		{
			fixItems<CargoItem>(ref items);
		}

		internal static void fixItems(ref List<InstalledEquipment> items)
		{
			fixItems<InstalledEquipment>(ref items);
		}

		internal static void fixItems(ref List<ItemStockData> items)
		{
			fixItems<ItemStockData>(ref items);
		}

		internal static void fixItems(ref List<MarketItem> items)
		{
			fixItems<MarketItem>(ref items);
		}

		internal static void fixItems<T>(ref List<T> items)
		{
			if (items == null)
				return;

			for (int i = 0; i < items.Count; i++)
			{
				int oldID = ObjUtils.GetIdReference(items[i]);
				if (TryGetNewID(items[i], oldID, out var newID) && newID != oldID)
				{
					Main.warn($" Updating {items[i].GetType()} ID: {oldID}->{newID}");
					ObjUtils.SetIdReference(items[i], newID);
				}
			}
		}

		internal static bool TryGetNewID(object obj, int id, out int newId)
		{
			var mapList = Map.GetByType(obj);
			if (mapList == null || mapList.Count == 0)
			{
				newId = default;
				return false;
			}

			int idx = mapList.FindIndex(kv => kv.Id == id);
			if (idx >= 0)
			{
				newId = mapList[idx].NewId;
				return true;
			}

			newId = default;
			return false;
		}

		// Write the map right before the game serializes GameDataInfo
		[HarmonyPatch(typeof(GameData), "SaveGame")]
		static class Patch_GameData_SaveGame_refmap
		{
			[HarmonyPriority(Priority.Last)]
			static void Prefix()
			{
				IdRefMap.SaveToGameData();
			}
		}

		// Rebuild the in-memory dictionary after GameDataInfo is installed
		[HarmonyPatch(typeof(GameData), "SetGameData")]
		static class Patch_GameData_SetGameData_refmap
		{
			[HarmonyPriority(Priority.Last)]
			static void Postfix()
			{
				Main.log("Fixing item ID Refmap");
				int i;
				IdRefMap.LoadFromGameData();

				if (GameData.data.spaceShipData.cargo != null)
					IdRefMap.fixItems(ref GameData.data.spaceShipData.cargo);

				if (GameData.data.containers != null)
					for (i = 0; i < GameData.data.containers.Count; i++)
						if (GameData.data.containers[i].items != null)
							IdRefMap.fixItems(ref GameData.data.containers[i].items);

				if (GameData.data.spaceShipData.equipments != null)
					IdRefMap.fixItems(ref GameData.data.spaceShipData.equipments);
				//IdRefMap.fixItems(ref GameData.data.spaceShipData.weapons);

				// docked ships
				if (GameData.data.shipLoadouts != null)
					for (i = 0; i < GameData.data.shipLoadouts.Count; i++)
					{
						Main.warn($"Ship Loadout {i}");
						if (GameData.data.shipLoadouts[i].data.cargo != null)
							IdRefMap.fixItems(ref GameData.data.shipLoadouts[i].data.cargo);
						if (GameData.data.shipLoadouts[i].data.equipments != null)
							IdRefMap.fixItems(ref GameData.data.shipLoadouts[i].data.equipments);
						//IdRefMap.fixItems(ref GameData.data.shipLoadouts[i].data.weapons);
					}

				if (GameData.data.character.mercenaries != null)
					for (i = 0; i < GameData.data.character.mercenaries.Count; i++)
					{
						Main.warn($"mercenary {i}");
						if (GameData.data.character.mercenaries[i].shipData.cargo != null)
							IdRefMap.fixItems(ref GameData.data.character.mercenaries[i].shipData.cargo);
						if (GameData.data.character.mercenaries[i].shipData.equipments != null)
							IdRefMap.fixItems(ref GameData.data.character.mercenaries[i].shipData.equipments);
					}

				if (GameData.data.stationList != null)
					for (i = 0; i < GameData.data.stationList.Count; i++)
					{
						//IdRefMap.fixItems(ref GameData.data.stationList[i].itemStock.items);
						//IdRefMap.fixItems(ref GameData.data.stationList[i].sm_Market.persistentItems);
					}
			}
		}

		// (Optional) Clear/init on new games so the field exists even before first save
		[HarmonyPatch(typeof(GameData), "NewGame")]
		static class Patch_GameData_NewGame_refmap
		{
			static void Postfix()
			{
				var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
				fi?.SetValue(GameData.data, string.Empty);
				IdRefMap.Map.Items.Clear();
				IdRefMap.Map.Equipment.Clear();
			}
		}
	}
}
