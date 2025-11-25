using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;




namespace RWEE
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
	}
	internal static class IdRefMap
	{

		internal static readonly IdRefMapJson Map = new IdRefMapJson();
//		internal static readonly Dictionary<int, string> Map = new Dictionary<int, string>(1024);
		private static string GetSaveField()
		{
			var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
			return fi?.GetValue(GameData.data) as string;
		}

		private static void SetSaveField(string json)
		{
			var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
			if (fi != null) fi.SetValue(GameData.data, json);
			else Main.error("Save field 'rweeItemMapJson' not found (prepatcher missing?).");
		}

		internal static void SaveToGameData()
		{
			try
			{
				var items = ItemDB.GetItems(false);
				var equipment = EquipmentDB.GetList(false);
				if (items == null || items.Count == 0) return;
				if (equipment == null || equipment.Count == 0) return;

				Map.Items.Clear();
				Map.Equipment.Clear();

				for (int i = 0; i < items.Count; i++)
				{
					var it = items[i];
					if (it == null) continue;
					Map.Items.Add(new IdRefMapJson.Pair { Id = it.id, Name = it.refName ?? string.Empty });
				}
				for (int i = 0; i < equipment.Count; i++)
				{
					var eq = equipment[i];
					if (eq == null) continue;
					Map.Equipment.Add(new IdRefMapJson.Pair { Id = eq.id, Name = eq.refName ?? string.Empty });
				}

				Main.log($"Map before JSON: items={Map.Items.Count} equip={Map.Equipment.Count} firstItem='{Map.Items[0].Name}'");
				//var json = UnityEngine.JsonUtility.ToJson(Map, false);
				//var json = JsonConvert.SerializeObject(Map, Formatting.None);
				//string json;
/*				var serializer = new DataContractJsonSerializer(typeof(IdRefMapJson));
				using (var ms = new MemoryStream())
				{
					serializer.WriteObject(ms, Map);
					json = Encoding.UTF8.GetString(ms.ToArray());
				}*/
				var json = JsonUtil.ToJson(Map);
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
					try {
						data = JsonUtil.FromJson<IdRefMapJson>(json);
					}
					catch (Exception ex) { Main.warn("Bad id/ref JSON in save, will use defaults: " + ex.Message); }
					Main.log($"Loaded items: {data.Items.Count} equip: {data.Equipment.Count}");
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
							new IdRefMapJson.Pair { Id = 198, Name = "rwee Pirate Capital Booster" }
						}
					};
					Main.warn("No saved id/ref map found; using defaults.");
				}
				// 3) Build remap lists only for CHANGED ids (name wins)
				var dbItems = ItemDB.GetItems(false);
				for (int i = 0; i < data.Items.Count; i++)
				{
					var saved = data.Items[i];
					var current = ItemDB.GetItem(saved.Id); // may be null/old

					if (current == null || !string.Equals(saved.Name, current.refName, StringComparison.Ordinal))
					{
						// Find by refName in current DB
						var match = dbItems.FirstOrDefault(it => it != null && it.refName == saved.Name);
						if (match != null)
						{
							saved.NewId = match.id;
							Map.Items.Add(saved);
							Main.warn($"ID item change detected: {saved.Name} {saved.Id}->{saved.NewId}");
						}
					}
				}

				var dbEquip = EquipmentDB.GetList(false);
				for (int i = 0; i < data.Equipment.Count; i++)
				{
					var saved = data.Equipment[i];
					var current = EquipmentDB.GetEquipment(saved.Id); // may be null/old

					if (current == null || !string.Equals(saved.Name, current.refName, StringComparison.Ordinal))
					{
						var match = dbEquip.FirstOrDefault(eq => eq != null && eq.refName == saved.Name);
						if (match != null)
						{
							saved.NewId = match.id;
							Map.Equipment.Add(saved);
							Main.warn($"ID equip change detected: {saved.Name} {saved.Id}->{saved.NewId}");
						}
					}
				}

				Main.log($"Loaded id/ref map ID changes: (items={Map.Items.Count}/{data.Items.Count} equip={Map.Equipment.Count}/{data.Equipment.Count}).");
			}
			catch (Exception ex)
			{
				Main.error("LoadFromGameData failed: " + ex);
			}
		}
		internal static void fixItems(ref List<CargoItem> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (TryGetNewID(items[i].itemType,items[i].itemID, out var newID))
				{
					Main.warn($" Updating Item ID: {items[i].itemID}->{newID}");
					items[i].itemID = newID;
				}
			}
		}
		internal static void fixItems(ref List<InstalledEquipment> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (TryGetNewID(2, items[i].equipmentID, out var newID))
				{
					Main.warn($" Updating Item ID: {items[i].equipmentID}->{newID}");
					items[i].equipmentID = newID;
				}
			}
		}
		internal static void fixItems(ref List<ItemStockData> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (TryGetNewID(items[i].itemType, items[i].itemID, out var newID))
				{
					Main.warn($" Updating Item ID: {items[i].itemID}->{newID}");
					items[i].itemID = newID;
				}
			}
		}
		internal static void fixItems(ref List<MarketItem> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (TryGetNewID(items[i].itemType, items[i].itemID, out var newID))
				{
					Main.warn($" Updating Item ID: {items[i].itemID}->{newID}");
					items[i].itemID = newID;
				}
			}
		}
		internal static bool TryGetNewID(int type, int id, out int newId)
		{
			int idx;
			switch(type)
			{
				case 2:
					idx = Map.Equipment.FindIndex(kv => kv.Id == id);
					if (idx >= 0) {
						newId = Map.Equipment[idx].NewId;
						return true;
					}
					break;
				case 3:
					idx = Map.Items.FindIndex(kv => kv.Id == id);
					if (idx >= 0) {
						newId = Map.Items[idx].NewId;
						return true;
					}
					break;
			}
			newId = default;
			return false;
		}
		// Write the map right before the game serializes GameDataInfo
		[HarmonyPatch(typeof(GameData), "SaveGame")]
		static class Patch_GameData_SaveGame_refmap
		{
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
				IdRefMap.fixItems(ref GameData.data.spaceShipData.cargo);
				for (i = 0; i < GameData.data.containers.Count; i++)
					IdRefMap.fixItems(ref GameData.data.containers[i].items);
				IdRefMap.fixItems(ref GameData.data.spaceShipData.equipments);
				//IdRefMap.fixItems(ref GameData.data.spaceShipData.weapons);
				//docked ships
				for (i = 0; i < GameData.data.shipLoadouts.Count; i++)
				{
					Main.warn($"Ship Loadout {i}");
					IdRefMap.fixItems(ref GameData.data.shipLoadouts[i].data.cargo);
					IdRefMap.fixItems(ref GameData.data.shipLoadouts[i].data.equipments);
					//IdRefMap.fixItems(ref GameData.data.shipLoadouts[i].data.weapons);
				}
				for (i = 0; i < GameData.data.character.mercenaries.Count; i++)
				{
					Main.warn($"mercenary {i}");
					IdRefMap.fixItems(ref GameData.data.character.mercenaries[i].shipData.cargo);
					IdRefMap.fixItems(ref GameData.data.character.mercenaries[i].shipData.equipments);
				}
					for (i = 0; i < GameData.data.stationList.Count; i++)
				{
				//	IdRefMap.fixItems(ref GameData.data.stationList[i].itemStock.items);
				//	IdRefMap.fixItems(ref GameData.data.stationList[i].sm_Market.persistentItems);
				}
				/*for (i = 0; i < GameData.data.sectors.Count; i++)
				{
					for (int j = 0; j < GameData.data.sectors[i].aiChars.Count; i++)
					{
						
					}
				}*/
					//public List<InstalledEquipment> equipments;
					//public List<EquipedWeapon> weapons;
					//public List<ContainerData> containers;
					//public List<ShipLoadout> shipLoadouts;
					//public GalacticMarket galacticMarket;
					//public List<Station> stationList;
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