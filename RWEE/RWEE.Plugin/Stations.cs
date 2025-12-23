using HarmonyLib;
using RW;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RWEE.Logging;
using static System.Collections.Specialized.BitVector32;
namespace RWEE
{
	internal class Stations
	{
		/**
		* Level up stations when you complete a quest.
		*/
		[HarmonyPatch(typeof(QuestControl), "CompleteQuest")]
		static class QuestControl_CompleteQuest
		{
			static void Postfix(int rewardChosen, ref Quest ___quest, ref Inventory ___inventory, ref PlayerControl ___pc)
			{
				if (QuestDB.IsQuestCompleted(___quest, ___pc.transform))
				{
					logr.Log($"Quest Complete postfix {___quest.refCode} {ObjUtils.GetRef(GameData.data.spaceShipData.ShipModel)}");
				}
			}
			static void Prefix(int rewardChosen, ref Quest ___quest, ref Inventory ___inventory, ref PlayerControl ___pc)
			{
				if (QuestDB.IsQuestCompleted(___quest, ___pc.transform))
				{
					//logr.Log($"Quest Complete prefix {___quest.refCode} {ObjUtils.GetRef(GameData.data.spaceShipData.ShipModel)}");
					if (PChar.Char.resetSkillsPoints == 0)
						PChar.Char.resetSkillsPoints++;
					//int currStationID = -1;
					if (___inventory.inStation)
					{
						AttemptToLevelStation(___inventory.currStation);

						int hostileFaction = FactionDB.GetHostileFactionForQuest(___inventory.currStation.factionIndex);
						Station hostileStation = GameData.GetRandomStation(hostileFaction, ___inventory.currStation.level, 0, 10, 0, ___inventory.currStation.Sector, false, 1, false);
						AttemptToLevelStation(hostileStation, true);
					}
					else
					{

						logr.Log("Not in station");
					}
				}
				else
				{
					logr.Log("Quest Incomplete");
				}
			}
			static void AttemptToLevelStation(Station station, bool hostile = false)
			{
				if (hostile)
					logr.Log($"also considering leveling up a random hostile station {station.stationName(true)} and sector {station.Sector.level}. Shhh...  Nothing to see here.");
				else
					logr.Log($"considering leveling up station {station.stationName(true)} and sector {station.Sector.level}.");
				//currStationID = this.inventory.currStation.id;
				//logr.Log($"Char Level: {PChar.Char.level} Station Level:{station.level} Sector Level: {station.Sector.level}");
				if (PChar.Char.level > station.level)
				{
					float amtOverStation = PChar.Char.level * 2 - station.level;
					if (amtOverStation < 1)
						amtOverStation = 1;
					if (UnityEngine.Random.Range(0, 20) < amtOverStation || (Main.DEBUG && 20 < amtOverStation))
					{
						int origLevel = station.level;
						station.level++;//not using LevelUp, because I want it to be silent.
						logr.Warn($"Leveling up station: Char:{PChar.Char.level} Station Level:{origLevel}->{station.level} Sector Level: {station.Sector.level}");
					}
				}

				if (station.level > station.Sector.level)
				{
					if (station.Sector.level < UnityEngine.Random.Range(station.level - 10, station.level) || (Main.DEBUG && station.Sector.level < station.level - 10))
					{
						int origLevel = station.Sector.level;
						station.Sector.level++;
						logr.Warn($"Leveling up sector. {origLevel}->{station.Sector.level}");
						//Sectors.AdjustNeighboringSectors(station.Sector);
						station.Sector.UpdateSectorLevels(false);
					}
				}

				//logr.Log($"New>Char Level: {PChar.Char.level} Station Level:{station.level} Sector Level: {station.Sector.level}");
			}
		}

		/**
		* Stations above L50 have more gold star quests
		*/
		[HarmonyPatch(typeof(QuestDB), "GetQuestForStation")]
		static class QuestDB_GetQuestForStation
		{
			static void Prefix(int refCode, ref int level, ref float dificulty, ref int rankOverride)
			{
				logr.Log($"Generating Quest {refCode} Level: {level} Diff: {dificulty} {rankOverride}");
				if (refCode == 11)
				{
					if (PChar.Char.level > level)
					{
						level += (PChar.Char.level - level) / 4;
					}
				}
			}
			/**
		* Stations above L50 have more gold star quests
		*/
			static void Postfix(int refCode, int level, ref float dificulty, ref int rankOverride, ref Quest __result)
			{
				logr.Log($"Quest generated: {refCode} {__result.kind} L{__result.level} diff: {dificulty} rank: {__result.rank} xprm: {__result.xpRewardMod}");
				logr.Log($"{QuestDB.Name(__result, true)}");
			}
		}
		/**
		* Stations above L50 have more gold star quests
		*/
		[HarmonyPatch(typeof(SM_MissionBoard), "GenerateQuests")]
		static class SM_MissionBoard_GenerateQuests
		{
			static void Prefix(ref int qntToAdd, ref Station ___station, ref List<StationQuest> ___quests, ref QuestChances __state)
			{
				int num = ___station.factionIndex;
				logr.Log($"GenerateQuests {qntToAdd} {GameManager.predefinitions.factions[num].questChances.eliminateEasy} {GameManager.predefinitions.factions[num].questChances.eliminateMedium} {GameManager.predefinitions.factions[num].questChances.eliminateHard}");
				__state = GameManager.predefinitions.factions[num].questChances;
				float mod = 0;
				if (___station.level > 50)
					mod += ___station.level - 50;
				if (PChar.Char.level > 50)
					mod += ___station.level - 50;
				if (mod > 0)
				{
					if (qntToAdd == -1)
					{
						System.Random genRand = ___station.GenRand;
						___quests = new List<StationQuest>();
						qntToAdd = genRand.Next(3, 5);
					}
					qntToAdd += 1 + (int)mod / 50;
					var qc = GameManager.predefinitions.factions[num].questChances;

					qc.eliminateEasy -= Mathf.RoundToInt(qc.eliminateEasy * mod);
					qc.eliminateMedium += Mathf.RoundToInt((50f - qc.eliminateMedium) * mod);
					qc.eliminateHard += Mathf.RoundToInt((100f - qc.eliminateHard) * mod);
					//GameManager.predefinitions.factions[num].questChances = qc;
				}
				logr.Log($"GenerateQuests {qntToAdd} {GameManager.predefinitions.factions[num].questChances.eliminateEasy} {GameManager.predefinitions.factions[num].questChances.eliminateMedium} {GameManager.predefinitions.factions[num].questChances.eliminateHard}");

			}
			static void Postfix(ref Station ___station, ref QuestChances __state)
			{
				GameManager.predefinitions.factions[___station.factionIndex].questChances = __state;
			}
		}

		/**
		 * Used in quest generation.  Increased range for higher level sectors
		 */

		[HarmonyPatch(typeof(GameDataInfo), "GetRandomSector", new Type[] {
		typeof(int), typeof(int), typeof(int), typeof(bool), typeof(TSector), typeof(int), typeof(System.Random)
	})]
		static class GameDataInfo_GetRandomSector
		{
			static void Prefix(int faction, int levelPlus, ref int distance, bool allowCurrentSector, TSector currSector, int sectorType)
			{
				if (currSector.level > 50)
					distance += currSector.level - 50;
			}
		}
	}

}
