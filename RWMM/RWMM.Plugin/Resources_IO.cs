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
using static RWMM.Logging;
namespace RWMM
{
	internal class Resources_IO
	{
		public static int done_count = 0;
		public static int dump_data = 1;
		private static List<string> _loaded_files = new List<string>();
		/*private static string _rwmm_plugin_dir;
		private static string _plugins_dir;
		public static void init(string rwmm_plugin_dir, string plugins_dir)
		{
			_rwmm_plugin_dir = rwmm_plugin_dir;
			_plugins_dir = plugins_dir;
			logr.Log("Resource_json initialized.");
		}*/

		
		[HarmonyPatch(typeof(ItemDB), "LoadDatabaseForce")]
		static class ItemDB_LoadDatabaseForce
		{
			[HarmonyPostfix]
			[HarmonyPriority(Priority.First)]
			static void Dump_Postfix(ref List<Item> ___items)
			{
				if (HasRun("Item1"))
					return;

				ResourceDump.DumpListToJson<Item,_Item>(___items);
			}

			[HarmonyPostfix]
			[HarmonyPriority(Priority.Last)]
			static void Import_Postfix(ref List<Item> ___items)
			{
				if (HasRun("Item2"))
					return;

				ResourceImport.ImportType<Item, _Item>(ref ___items);
				done();
			}
		}

		[HarmonyPatch(typeof(EquipmentDB), "LoadDatabaseForce")]
		static class EquipmentDB_LoadDatabaseForce
		{
			[HarmonyPostfix]
			[HarmonyPriority(Priority.First)]
			static void Dump_Postfix(ref List<Equipment> ___equipments)
			{
				if (HasRun("Equipment"))
					return;
				ResourceDump.DumpListToJson<Equipment,_Equipment>(___equipments);
			}
			[HarmonyPostfix]
			[HarmonyPriority(Priority.Last)]
			static void Import_Postfix(ref List<Equipment> ___equipments)
			{
				if (HasRun("Equipment2"))
					return;
				ResourceImport.ImportType<Equipment, _Equipment>(ref ___equipments);
				done();
			}
		}
		[HarmonyPatch(typeof(ShipDB), "LoadDatabaseForce")]
		static class ShipDB_LoadDatabaseForce
		{
			[HarmonyPostfix]
			[HarmonyPriority(Priority.First)]
			static void Dump_Postfix(ref List<ShipModelData> ___shipModels)
			{
				for (int i = 0; i < ___shipModels.Count; i++)
				{
					ObjUtils.SetRef(___shipModels[i], ___shipModels[i].shipModelName);
				}
				if (HasRun("ShipModelData"))
					return;
				ResourceDump.DumpListToJson<ShipModelData, _ShipModelData>(___shipModels);
				//	ResourceDump.DumpListToJson(___shipModels);
				done();
			}
			[HarmonyPostfix]
			[HarmonyPriority(Priority.Last)]
			static void Import_Postfix(ref List<ShipModelData> ___shipModels)
			{
				if (HasRun("ShipModelData2"))
					return;
				ResourceImport.ImportType<ShipModelData, _ShipModelData>(ref ___shipModels);
				done();
			}
		}
		[HarmonyPatch(typeof(QuestDB), "Validate")]
		static class QuestDB_Validate
		{
			[HarmonyPostfix]
			[HarmonyPriority(Priority.First)]
			static void Dump_Postfix(ref List<Quest> ___questReference)
			{
				if (HasRun("Quest"))
					return;
				ResourceDump.DumpListToJson< Quest,_Quest>(___questReference);
			}
			[HarmonyPostfix]
			[HarmonyPriority(Priority.Last)]
			static void Import_Postfix(ref List<Quest> ___questReference)
			{
				if (HasRun("Quest2"))
					return;
				ResourceImport.ImportType<Quest, _Quest>(ref ___questReference);
				done();
			}
		}
		
		[HarmonyPatch(typeof(GameManager), "GetBasePredefinitions")]
		static class GameManager_GetBasePredefinitions
		{
			[HarmonyPostfix]
			[HarmonyPriority(Priority.First)]
			static void Dump_Postfix(ref Predefinitions ___predefinitions)
			{
				for (int i = 0; i < ___predefinitions.weapons.Length; i++)
				{
					ObjUtils.SetRef(___predefinitions.weapons[i], ___predefinitions.weapons[i].name);
				}

				for (int i = 0; i < ___predefinitions.crewMembers.Length; i++)
				{
					ObjUtils.SetRef(___predefinitions.crewMembers[i], ___predefinitions.crewMembers[i].aiChar.name);
				}

				//logr.Log("Dumping Predefinitions");
				//logr.LogLineList<CrewMember>(___predefinitions.crewMembers.ToList());
				if (HasRun("Predefinitions"))
					return;
				ResourceDump.DumpListToJson<TWeapon, _TWeapon>(___predefinitions.weapons);
				ResourceDump.DumpListToJson<CrewMember, _CrewMember>(___predefinitions.crewMembers);


				//Apply<Faction, _Faction>(ref ___predefinitions.factions, true, false);
				//public ShipClassDefinition[] shipClassDefinitions = new ShipClassDefinition[7];
				//public ShipRoleDefinition[] shipRoleDefinitions;
				//public AICharacter[] Characters = new AICharacter[1];

			}
			[HarmonyPostfix]
			[HarmonyPriority(Priority.Last)]
			static void Import_Postfix(ref Predefinitions ___predefinitions)
			{
				//we need to run these again if they are reloaded
				//if (HasRun("Predefinitions2"))
				//	return;

				ResourceImport.ImportType<TWeapon, _TWeapon>(ref ___predefinitions.weapons);

				ResourceImport.ImportType<CrewMember, _CrewMember>(ref ___predefinitions.crewMembers);


				//logr.LogLineList<TWeapon>(GameManager.predefinitions.weapons.ToList());

				//Apply<Faction, _Faction>(ref ___predefinitions.factions, true, false);
				//public ShipClassDefinition[] shipClassDefinitions = new ShipClassDefinition[7];
				//public ShipRoleDefinition[] shipRoleDefinitions;
				//public AICharacter[] Characters = new AICharacter[1];
				done();
			}
		}
		[HarmonyPatch(typeof(PerkDB), "LoadPerks")]
		static class PerkDB_LoadPerks
		{
			[HarmonyPostfix]
			[HarmonyPriority(Priority.First)]
			static void Dump_Postfix(ref List<Perk> ___perks)
			{
				if (HasRun("Perk"))
					return;
				ResourceDump.DumpListToJson<Perk, _Perk>(___perks);
			}
			[HarmonyPostfix]
			[HarmonyPriority(Priority.Last)]
			static void Import_Postfix(ref List<Perk> ___perks)
			{
				if (HasRun("Perk2"))
					return;
				ResourceImport.ImportType<Perk, _Perk>(ref ___perks);

				//Perk perk = ListUtils.GetByRef<Perk>(___perks, "Miner");
				//logr.LogObj(perk);
				done();
			}
		}
		[HarmonyPatch(typeof(CrewDB), "LoadCrewMembers")]
		static class CrewDB_LoadCrewMembers
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix()
			{
				logr.Log("Fixing CrewMember refs");
				CrewMember[] crewMembers = GameManager.predefinitions.crewMembers;
				for (int i = 0; i < crewMembers.Count(); i++)
				{
					//ObjUtils.SetRef(crewMembers[i], crewMembers[i].aiChar.name);
					CrewMember crewMember = CrewDB.GetCrewMember(crewMembers[i].id);
					if(ObjUtils.GetRef(crewMember,true) == null)
						ObjUtils.SetRef(crewMember, ObjUtils.GetRef(crewMembers[i]));
					logr.Log($"{i} {crewMember.id} {ObjUtils.GetRef(crewMember)}");
				}
			}
		}
/*		[HarmonyPatch(typeof(SkillDB), "LoadDatabase")]
		static class SkillDB_LoadDatabase
		{
			[HarmonyPriority(Priority.First)]
			static void Postfix(List<Skill> ___skills)
			{
				logr.Log("Loading bonus list");
				for(int i = 0; i < ___skills.Count; i++)
				{
					logr.Log($"{i} {___skills[i].id} {___skills[i].refSkillName}");

				}
			}
		}*/
		private static bool HasRun(string type)
		{
			if (_loaded_files.Contains(type))
				return true;
			_loaded_files.Add(type);
			return false;
		}
		private static void done()
		{
			done_count++;
			logr.Log($"Done with {done_count}");
			if (done_count == 7)
			{
				logr.PopupErrors("Red Wizard's Mod Manager","There were errors during load:");
			}
		}
	}
}

