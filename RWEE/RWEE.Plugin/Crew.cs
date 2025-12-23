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
			static void Postfix(int id)
			{
				logr.Log($"Acquiring perk: {id}");
				if (id == 8)  //scoundrel
				{
					UnlockSam();
					UnlockTinkerSteve();
				}
				if (id == 4) //lone wolf
				{
				}
			}
		}
		private static void UnlockSam()
		{
			logr.Log("Unlocking Sam Holo");
			CrewMember cm = CrewDB.GetPredefinedCrewMember(11);
			cm.hidden = false;
		}
		private static void UnlockTinkerSteve()
		{
			logr.Log("Unlocking High Tinker Steve");
			CrewMember cm = CrewDB.GetPredefinedCrewMember(14);
			cm.hidden = false;
		}
		/**
		* Makes Sam Holo Spawnable in an escape pod after you steal his ship.
		*/
		[HarmonyPatch(typeof(CrewDB), "LoadCrewMembers")]
		static class CrewDB_LoadCrewMembers
		{
			static void Prefix(ref CrewMember[] ___crewList)
			{
				if (PChar.HasPerk(8) || PChar.HasPerk(4))  //scoundrel or Lone Wolf
				{
					UnlockSam();
					UnlockTinkerSteve();
				}
				//if (PChar.HasPerk(8) || )
				//{

				for (int i = 0; i < GameManager.predefinitions.crewMembers.Length; i++)
				{
					logr.Log($"Crew: {GameManager.predefinitions.crewMembers[i].id} {GameManager.predefinitions.crewMembers[i].aiChar.name} Hidden: {GameManager.predefinitions.crewMembers[i].hidden}");
				}
				//List<Quest> quests = QuestDB.;
				//for (int i = 0; i < 
				//}
			}

/**
 * Unlocking a special crew member also adds them to findable crew list  (High Tinker Steve)
 */
			[HarmonyPatch(typeof(GenData), "UnlockCrewMember")]
			static class GenData_UnlockCrewMember
			{
				static void Postfix(int crewMemberID)
				{
					logr.Log($"Unlocking Crew Member ID: {crewMemberID}");
					CrewMember cm = CrewDB.GetPredefinedCrewMember(crewMemberID);
					cm.hidden = false;
				}
			}
			[HarmonyPatch(typeof(CrewMember), "GainXP")]
			static class CrewMember_GainXP
			{
				static void Postfix(ref CrewMember __instance, ref int ___rarity, ref int ___nextRarityCount)
				{
					//logr.Log($"Crew GainXP Rarity: {___rarity} NextRarityCount: {___nextRarityCount}");
					int mult = ___rarity - 3;
					if (___nextRarityCount >= 1000 * Math.Pow(mult, 1.5f) && ___rarity >= 5 && ___rarity < Main.MAX_RARITY)
					{
						__instance.LevelUpRarity();
						___nextRarityCount = 0;
					}
				}
			}

			[HarmonyPatch(typeof(CrewMember), "GetNameModified", new Type[] { typeof(int), typeof(bool), typeof(bool) })]
			static class CrewMember_GetNameModified
			{
				static void Postfix(AICharacter ___aiChar, List<CrewSkill> ___skills, ref string __result)
				{
					if (___aiChar == null)
						return;
					__result += $" ({___aiChar.level})";
					if (___skills == null || ___skills.Count == 0)
						return;

					var abbrev_list = new List<string>(___skills.Count);

					for (int i = 0; i < ___skills.Count; i++)
					{
						var skill = ___skills[i];
						if (skill == null)
							continue;

						var skill_name = Lang.Get(23, 10 + ((int)skill.ID * (int)CrewPosition.Navigator));
						if (string.IsNullOrEmpty(skill_name))
							continue;

						skill_name = skill_name.Trim();
						var len = skill_name.Length < 3 ? skill_name.Length : 3;

						abbrev_list.Add(skill_name.Substring(0, len));
					}

					if (abbrev_list.Count == 0)
						return;

					__result = (__result ?? "") + " [" + string.Join(", ", abbrev_list) + "]";
				}
			}
		}
	}
}