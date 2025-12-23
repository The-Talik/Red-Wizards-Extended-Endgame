using HarmonyLib;
using RW;
using RWMM.Dto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static RWMM.Logging;
namespace RWMM
{
	internal static partial class IdRefMap
	{
		public class IdRefMapJson
		{
			public List<Pair> Item = new List<Pair>();
			public List<Pair> Equipment = new List<Pair>();
			public List<Pair> ShipModelData = new List<Pair>();
			public List<Pair> Quest = new List<Pair>();
			public List<Pair> TWeapon = new List<Pair>();
			public List<Pair> CrewMember = new List<Pair>();
			public List<Pair> Perk = new List<Pair>();

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
						//("1 = weapons, 2 = equipment, 3 = item (goods), 4 == ship, 5 == crew member")]
						switch (type)
						{
							case 1:
								return TWeapon;
							case 2:
								return Equipment; // Equipment
							case 3:
								return Item;     // Item
							case 4:
								return ShipModelData;
							case 5:
								return CrewMember;
							default:
								logr.Error($"IdRefMapJson.GetByType: Unsupported type {obj.GetType().Name} {type}");
								return null;
						}
					case "ShipModelData":
						return ShipModelData;
					case "InstalledEquipment":
					case "BuiltInEquipmentData":
						return Equipment;
					case "TWeapon":
					case "EquipedWeapon":
										return TWeapon;
					case "AssignedCrewMember":
						return CrewMember;

					default:
						logr.Error($"IdRefMapJson.GetByType: Unsupported type {obj.GetType().Name}");
						return null;
				}
			}
		}

		internal static List<Type> ManagedTypes = new List<Type>
		{
			typeof(Item),
			typeof(Equipment),
			typeof(ShipModelData),
			typeof(Quest),
			typeof(TWeapon),
			typeof(CrewMember),
			typeof(Perk)
		};
		internal static readonly IdRefMapJson Map = new IdRefMapJson();

		[HarmonyPatch(typeof(GameData), "NewGame")]
		static class Patch_GameData_NewGame_refmap
		{
			static void Postfix()
			{
				var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
				fi?.SetValue(GameData.data, string.Empty);
				IdRefMap.Map.Item.Clear();
				IdRefMap.Map.Equipment.Clear();
			}
		}




		
		

		static private List<IdRefMapJson.Pair> GetPairList(IdRefMapJson map_obj, Type type)
		{
			if (map_obj == null || type == null)
				return null;

			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

			// prefer field
			var field = typeof(IdRefMapJson).GetField(type.Name, flags);
			if (field == null)
				return null;
			var val = field.GetValue(map_obj) as List<IdRefMapJson.Pair>;
			return val;

		}



		static private IList GetList(Type type)
		{
			if (type == typeof(Item))
				return ItemDB.GetItems(false);

			if (type == typeof(Equipment))
				return EquipmentDB.GetList(false);

			if (type == typeof(ShipModelData))
				return ShipDB.GetEntireList();

			if (type == typeof(Quest))
			{
				var field = typeof(QuestDB).GetField("questReference", BindingFlags.NonPublic | BindingFlags.Static);
				if (field == null)
					throw new InvalidOperationException("QuestDB.questReference field not found");

				var quest_reference = field.GetValue(null) as IList;
				if (quest_reference == null)
					throw new InvalidOperationException("QuestDB.questReference is not a list");

				return quest_reference;
			}

			if (type == typeof(TWeapon))
			{
				logr.Warn("Getting TWeapon list from predefinitions");
				logr.LogLineList(GameManager.predefinitions.weapons.ToList());
				return GameManager.predefinitions.weapons.ToList();
			}

			if (type == typeof(CrewMember))
				return GameManager.predefinitions.crewMembers.ToList();

			if (type == typeof(Perk))
				return PerkDB.GetAllPerks();

			logr.Error($"GetList does not support type {type.Name}");
			return null;
		}
		private static void LogMap(List<IdRefMapJson.Pair> map, bool hide_new = false)
		{
			for (int i = 0; i < map.Count; i++)
			{
				var pair = map[i];
				logr.Log($"{pair.Name}: {pair.Id}" + (!hide_new ? $" => {pair.NewId}" : ""));
			}
		}
	}
}
