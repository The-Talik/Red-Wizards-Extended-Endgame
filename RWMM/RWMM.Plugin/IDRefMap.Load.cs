using HarmonyLib;
using RW;
using RW.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RWMM.Dto._CrewMember._AICharacter;
using static RWMM.Logging;


namespace RWMM
{
	internal static partial class IdRefMap
	{
		internal static class Load
		{
			/**
 * Rebuild the in-memory dictionary after GameDataInfo is installed
 * todo: floating items/equipment?
 * mercenary captain
 * loot from stations
 * loot from enemies
 * towed object
 */
			[HarmonyPatch(typeof(GameData), "SetGameData")]
			static class Patch_GameData_SetGameData_refmap
			{
				[HarmonyPriority(Priority.Last)]
				static void Postfix()
				{
					logr.Open("IDRefMap Fixing game data");
					int i;
					LoadFromGameData();

					logr.Log($"Checking current ship [{GameData.data.spaceShipData.ShipModel.modelName}] cargo/equipment");

					FixShip(GameData.data.spaceShipData);

					logr.Open("Fixing Containers");
					if (GameData.data.containers != null)
						for (i = 0; i < GameData.data.containers.Count; i++)
							if (GameData.data.containers[i].items != null)
								fixItems(ref GameData.data.containers[i].items);
					logr.Close();

					logr.Open("Docked Ships");
					// docked ships
					if (GameData.data.shipLoadouts != null)
						for (i = 0; i < GameData.data.shipLoadouts.Count; i++)
						{
							logr.Warn($"Ship Loadout {i} {GameData.data.shipLoadouts[i].data.ShipModel.modelName}");
							FixShip(GameData.data.shipLoadouts[i].data);
						}
					logr.Close("Docked Ships");
					logr.Open("Fleet Ships");
					//fleet ships
					if (GameData.data.character.mercenaries != null)
						for (i = 0; i < GameData.data.character.mercenaries.Count; i++)
						{
							logr.Warn($"mercenary {i}");
							FixShip(GameData.data.character.mercenaries[i].shipData);
						}
					logr.Close("Fleet Ships");

					if (GameData.data.stationList != null)
						for (i = 0; i < GameData.data.stationList.Count; i++)
						{
							//IdRefMap.fixItems(ref GameData.data.stationList[i].itemStock.items);
							//IdRefMap.fixItems(ref GameData.data.stationList[i].sm_Market.persistentItems);
						}
					if (GameData.data.sectors != null)
						for (i = 0; i < GameData.data.sectors.Count; i++)
						{

						}
					if (GameData.data.weaponList != null)
						fixItems(ref GameData.data.weaponList);
					//if (GameData.data.crew != null)
					//	fixItems(ref GameData.data.crew);
					/*if (GameData.data.crew != null)
					{
						logr.Log("CREW");
						logr.LogLineList(GameData.data.crew);
					}*/

					//GameData.data.towedObjects
					//GameData.data.crew (does this include all crew)
					//GameData.data.NPCs (Does this include all npc ships?)
					//galacticMarket?
					logr.Close();
				}
			}
			private static void FixShip(SpaceShipData spaceShipData)
			{
				logr.Open($"FixShip {spaceShipData.ShipModel.modelName} ({spaceShipData.ShipModel.id})");
				fixItem(spaceShipData.ShipModel);
				if (spaceShipData.cargo != null)
					fixItems(ref spaceShipData.cargo);
				if (spaceShipData.equipments != null)
					fixItems(ref spaceShipData.equipments);
				if (spaceShipData.builtInData != null)
					fixItems(ref spaceShipData.builtInData);
				if (spaceShipData.weapons != null)
					fixItems(ref spaceShipData.weapons);
				//if (spaceShipData.members != null)
				//	fixItems(ref spaceShipData.members);
				logr.Close();
			}
			internal static void LoadFromGameData()
			{
				logr.Open("RefMap LoadFromGameData");
				try
				{
					// 0) Always start clean
					for (int i = 0; i < ManagedTypes.Count; i++)
					{
						var type = ManagedTypes[i];
						var map_list = GetPairList(Map, type);
						if (map_list != null)
							map_list.Clear();
					}

					// 1) Try saved JSON
					var json = GetSaveField();
					logr.LogTruncate("Saved JSON: " + (string.IsNullOrEmpty(json) ? "<empty>" : json));
					IdRefMapJson data = null;

					if (!string.IsNullOrEmpty(json))
					{
						try
						{
							data = JsonUtils.FromJson<IdRefMapJson>(json);
							for (int i = 0; i < ManagedTypes.Count; i++)
							{
								var list= GetPairList(data, ManagedTypes[i]);
								logr.Log($" Loaded {ManagedTypes[i].Name} count={list?.Count.ToString() ?? "<null>"}");
							}
						}
						catch (Exception ex)
						{
							logr.Warn("Bad id/ref JSON in save, will use defaults: " + ex.Message);
						}
					}
					else
					{
						logr.Warn("No id/ref JSON found in save.");
					}
					
						// 2) Fallback defaults if missing/corrupt
						if (data == null || data.Item == null || data.Item.Count == 0)
					{
						logr.Warn("Using Defaults");
						data = new IdRefMapJson
						{
							Item = new List<IdRefMapJson.Pair>
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
							new IdRefMapJson.Pair { Id = 198, Name = "Pirate Capital Booster" }

						}
						};

						logr.Warn("No saved id/ref map found; using defaults.");
					}

					// 3) Build remap lists (ONLY changed ids)
					for (int i = 0; i < ManagedTypes.Count; i++)
					{
						var type = ManagedTypes[i];

						var saved_list = GetPairList(data, type);
						if (saved_list == null || saved_list.Count == 0)
							continue;

						logr.Open(type.Name);


						var map_list = GetPairList(Map, type);



						if (map_list == null)
						{
							logr.Warn("No Map list found for type: " + type.Name);
							continue;
						}

						IList db_list = null;
						try
						{
							db_list = GetList(type);
						}
						catch (Exception ex)
						{
							logr.Error("GetList failed for " + type.Name + ": " + ex.Message);
							continue;
						}
						MakeRemapList(type, saved_list, db_list, map_list);
						//LogMap(saved_list);
						//logr.Log("Changes");
						LogMap(map_list);
						logr.Close(type.Name + " (" + map_list.Count + " / " + saved_list.Count + ")");
					}
				}
				catch (Exception ex)
				{
					logr.Error("LoadFromGameData failed: " + ex);
				}
				logr.Close();
			}

			private static string GetSaveField()
			{
				var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
				return fi?.GetValue(GameData.data) as string;
			}
			static private void MakeRemapList(Type type, List<IdRefMapJson.Pair> saved_list, IList db_list, List<IdRefMapJson.Pair> map_list)
			{
				if (type == null || saved_list == null || db_list == null || map_list == null)
					return;

				map_list.Clear();
				for (int i = 0; i < saved_list.Count; i++)
				{
					var saved = saved_list[i];

					object current = ListUtils.GetById(db_list, type, saved.Id);
					string current_ref = null;

					if (current != null)
						current_ref = ObjUtils.GetRef(current, true);


					// unchanged -> skip
					if (current != null && string.Equals(saved.Name, current_ref, StringComparison.Ordinal))
					{
						saved.NewId = saved.Id;
						continue;
					}

					// find by refName in current DB
					object match = ListUtils.GetByRef(db_list, type, saved.Name);

					// backwards compatibility: try adding/removing rwee_ prefix
					if (match == null && !string.IsNullOrEmpty(saved.Name))
					{
						if (saved.Name.StartsWith("rwee_", StringComparison.Ordinal))
							match = ListUtils.GetByRef(db_list, type, saved.Name.Substring(5));
						else
							match = ListUtils.GetByRef(db_list, type, "rwee_" + saved.Name);
					}

					if (match != null)
					{
						saved.NewId = ObjUtils.GetId(match);
						map_list.Add(saved);
						logr.Warn($"ID change detected [{type.Name}]: {saved.Name} {saved.Id}->{saved.NewId}");
					}
					else
					{
						logr.Error($"Remap not found [{type.Name}]: '{saved.Name}' old id {saved.Id} (db items={db_list.Count}).");
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
				logr.Log($"fixItems<{typeof(T).Name}> count={(items == null ? "<null>" : items.Count.ToString())}");
				if (items == null)
					return;

				for (int i = 0; i < items.Count; i++)
				{
					fixItem(items[i]);
				}
			}
			internal static void fixItem<T>(T item)
			{
				int oldID = ObjUtils.GetIdReference(item);
				if (TryGetNewID(item, oldID, out var newID) && newID != oldID)
				{
					logr.Warn($" Updating {item.GetType()} ID: {oldID}->{newID}");
					ObjUtils.SetIdReference(item, newID);
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
		}
	}
}
