using HarmonyLib;
using RW;
using RWMM.Dto;
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static RWMM.ResourceDump;

namespace RWMM
{
	internal class Resources_IO
	{
		public static int dump_data = 1;
		private static List<string> _loaded_files = new List<string>();
		/*private static string _rwmm_plugin_dir;
		private static string _plugins_dir;
		public static void init(string rwmm_plugin_dir, string plugins_dir)
		{
			_rwmm_plugin_dir = rwmm_plugin_dir;
			_plugins_dir = plugins_dir;
			Main.log("Resource_json initialized.");
		}*/

		[HarmonyPatch(typeof(ItemDB), "LoadDatabaseForce")]
		static class ItemDB_LoadDatabaseForce
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix(ref List<Item> ___items)
			{
				if(HasRun("Item"))
					return;
				Apply<Item, _Item>(ref ___items);

			//	Item it = ListUtils.GetByRef(___items,"Ancient Relic");
			//	Main.log_obj(it);
			//	it = ListUtils.GetByRef(___items, "Ancient Relic 6969");
			//	Main.log_obj(it);
			}
		}

		[HarmonyPatch(typeof(EquipmentDB), "LoadDatabaseForce")]
		static class EquipmentDB_LoadDatabaseForce
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix(ref List<Equipment> ___equipments)
			{
				if (HasRun("Equipment"))
					return;
				Apply<Equipment, _Equipment>(ref ___equipments);
			}
		}
		[HarmonyPatch(typeof(ShipDB), "LoadDatabaseForce")]
		static class ShipDB_LoadDatabaseForce
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix(ref List<ShipModelData> ___shipModels)
			{
				if (HasRun("ShipModelData"))
					return;
				//convert to _ShipModelData because the json formatter has issues with ShipModelData
				List<_ShipModelData> dumpList = new List<_ShipModelData>();
				for (int i = 0; i < ___shipModels.Count; i++)
				{
					ObjUtils.SetRef(___shipModels[i], ___shipModels[i].shipModelName);
					_ShipModelData newObj = new _ShipModelData();
					ObjectApply.Apply(___shipModels[i], newObj);
					dumpList.Add(newObj);
				}

				ResourceDump.DumpListToJson(dumpList);
			//	ResourceDump.DumpListToJson(___shipModels);
				ResourceImport.ImportType<ShipModelData, _ShipModelData>(ref ___shipModels);
				Main.log("Done with ship models");
			}
		}
		[HarmonyPatch(typeof(QuestDB), "Validate")]
		static class QuestDB_Validate
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix(ref List<Quest> ___questReference)
			{
				if (HasRun("Quest"))
					return;
				Apply<Quest, _Quest>(ref ___questReference);
			}
		}
/*		[HarmonyPatch(typeof(PerkDB), "LoadPerks")]
		static class PerkDB_LoadPerks
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix(ref List<Perk> ___perks)
			{
				if (HasRun("Perks"))
					return;
				Apply<Perk, _Perk>(ref ___perks);
			}
		}*/
		[HarmonyPatch(typeof(GameManager), "GetBasePredefinitions")]
		static class GameManager_GetBasePredefinitions
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix(ref Predefinitions ___predefinitions)
			{
				if (HasRun("Predefinitions"))
					return;
				for (int i = 0; i < ___predefinitions.weapons.Length; i++)
				{
					ObjUtils.SetRef(___predefinitions.weapons[i], ___predefinitions.weapons[i].name);
				}
				Apply<TWeapon, _TWeapon>(ref ___predefinitions.weapons);

				for (int i = 0; i < ___predefinitions.crewMembers.Length; i++)
				{
					ObjUtils.SetRef(___predefinitions.crewMembers[i], ___predefinitions.crewMembers[i].aiChar.name);
				}
				Apply<CrewMember, _CrewMember>(ref ___predefinitions.crewMembers);


				//Apply<Faction, _Faction>(ref ___predefinitions.factions, true, false);
				//public ShipClassDefinition[] shipClassDefinitions = new ShipClassDefinition[7];
				//public ShipRoleDefinition[] shipRoleDefinitions;
				//public AICharacter[] Characters = new AICharacter[1];
			}
		}
		public static void Apply<T, TData>(ref T[] array, string comments = "")
		{
			if (array == null)
			{
				array = Array.Empty<T>();
			}

			var list = new List<T>(array);

			Apply<T, TData>(ref list, comments);

			array = list.ToArray();
		}
		public static void Apply<T, TData>(ref List<T> list, string comments = "")
		{

			ResourceDump.DumpListToJson(list, comments);
			ResourceImport.ImportType<T, TData>(ref list);

		}

		private static bool HasRun(string type)
		{
			if (_loaded_files.Contains(type))
				return true;
			_loaded_files.Add(type);
			return false;
		}

	}
}

