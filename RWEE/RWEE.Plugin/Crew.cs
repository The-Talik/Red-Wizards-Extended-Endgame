using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RW;
using static RWEE.Logging;
namespace RWEE
{
	internal class Crew
	{
		/**
* Make special crew more common
*/
		[HarmonyPatch(typeof(CrewDB), "CreateCrewMember", new Type[] {
						typeof(int),               // level
            typeof(int),               // minRarity
            typeof(int),               // maxRarity
            typeof(int),               // factionIndex
            typeof(bool),              // allowSpecial
            typeof(SM_Academy),        // academy
            typeof(System.Random)      // rand
        })]
		static class CrewDB_CreateCrewMember
		{
			static bool Prefix(ref int level, ref int minRarity, ref int maxRarity, int factionIndex, bool allowSpecial, SM_Academy academy, System.Random rand, ref bool ___loaded, ref CrewMember __result)
			{
				
				logr.Log($"Crew Orig: Level: {level} minRarity: {minRarity} maxRarity: {maxRarity} academy: {academy}");
				if (academy != null)
				{
					if (level > 50)
						level = 50;
					return true;
				}
				if (level > 30)
					minRarity++;
				if (level > 60)
					maxRarity++;
				level = 1;
				logr.Log($"Crew Orig: Level: {level} minRarity: {minRarity} maxRarity: {maxRarity} academy: {academy}");
				if (!allowSpecial)
					return true;
				if (!___loaded)
					return true;

				CrewMember crewMember = null;

				List<CrewMember> availableSpecialCrewMember = CrewDB.GetAvailableSpecialCrewMember();


				float num = 30 - GameData.data.specialCrewUsed.Count;

				//logr.Log($"Crew: {CrewDB.GetAvailableSpecialCrewMember().Count} {num}");

				if (num < 5)
				{
					num = 5;
				}
				if (availableSpecialCrewMember.Count != 0 && UnityEngine.Random.Range(1, 101) <= num)
				{

					crewMember = availableSpecialCrewMember[UnityEngine.Random.Range(0, availableSpecialCrewMember.Count)];
					GameData.data.specialCrewUsed.Add(crewMember.id);
				}

				if (crewMember == null)
					return true;
				__result = crewMember;
				return false;
			}

		}
		/**
		* Makes Sam Holo Spawnable in an escape pod after you steal his ship.
		*/
		[HarmonyPatch(typeof(PerkDB), "AcquirePerk")]
		static class PerkDB_AcquirePerk
		{
			static void Prefix(int id)
			{
				logr.Log($"Getting perk: {id}");
				//if (id == 8)
				{
					logr.Log("Unlocking Sam");
					CrewMember cm = CrewDB.GetPredefinedCrewMember(11);
					logr.Log($"cm: {cm.aiChar.name}");
					cm.hidden = false;
					cm = CrewDB.GetPredefinedCrewMember(11);
				}
			}
		}
		/**
		* Makes Sam Holo Spawnable in an escape pod after you steal his ship.
		*/
		[HarmonyPatch(typeof(CrewDB), "LoadCrewMembers")]
		static class CrewDB_LoadCrewMembers
		{
			static void Prefix(ref CrewMember[] ___crewList)
			{
				if (PChar.HasPerk(8))
				{
					logr.Log("has Scoundrel perk");
					for (int i = 0; i < GameManager.predefinitions.crewMembers.Length; i++)
					{
						if (GameManager.predefinitions.crewMembers[i].id == 11)
						{
							GameManager.predefinitions.crewMembers[i].hidden = false;
							logr.Log("Unlocking Sam");
						}
						logr.Log($"Crew: {GameManager.predefinitions.crewMembers[i].id} {GameManager.predefinitions.crewMembers[i].aiChar.name}");
					}
				}
			}
			/*static void Postfix(ref CrewMember[] ___crewList)
			{
					if (PChar.HasPerk(8))
					{
							logr.Log("has Scoundrel perk -- Unlocking Sam");
							CrewMember cm = CrewDB.GetPredefinedCrewMember(11);
							cm.hidden = false;

							/*for (int i = 0; i < ___crewList.Length; i++)
							{
									//logr.Log($"Crew: {___crewList[i].id}");
									/*if (CrewDB.predefCrew[i].id == 11)
									{
											CrewDB.predefCrew[i].hidden = false;
									}*
							}*
					}
			}*/
		}
	}
}