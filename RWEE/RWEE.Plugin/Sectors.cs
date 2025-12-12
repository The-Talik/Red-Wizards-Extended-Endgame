using HarmonyLib;
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RWEE.Logging;
namespace RWEE
{
	internal class Sectors
	{
		/**
		 * remove sector level cap entirely.  Changes scaling to scale more slowly after level 50.
		 */
		[HarmonyPatch(typeof(GameDataInfo), "GetSectorIndex")]
		static class GameDataInfo_GetSectorIndex_Patch
		{
			static bool Prefix(GameDataInfo __instance, int cX, int cY, int desiredFactionControl, ref int __result)
			{
				int num = -1;
				foreach (TSector tsector in __instance.sectors)
				{
					if (tsector.x == cX && tsector.y == cY)
					{
						num = __instance.sectors.IndexOf(tsector);
					}
				}
				if (num < 0)
				{
					int newLevel = Sectors.calculateLevel(cX, cY);

					TSector item = new TSector(2, cX, cY, newLevel, desiredFactionControl);
					__result = __instance.sectors.IndexOf(item);
					return false;
				}
				__result = num;
				return false;
			}
		}
		static public float calculateMinLevel(int cX, int cY, int? staticLevel = null)
		{
			if (staticLevel == null)
				staticLevel = (int)Vector2.Distance(new Vector2(25f, 14f), new Vector2((float)cX, (float)cY));
			return (float)staticLevel * 0.9f;
		}
		static public float calculateMaxLevel(int cX, int cY, int? staticLevel = null)
		{
			if (staticLevel == null)
				staticLevel = (int)Vector2.Distance(new Vector2(25f, 14f), new Vector2((float)cX, (float)cY));
			return (float)staticLevel * 1.5f;
		}
		static public int calculateLevel(int cX, int cY, int? staticLevel = null)
		{
			if (staticLevel == null)
				staticLevel = (int)Vector2.Distance(new Vector2(25f, 14f), new Vector2((float)cX, (float)cY));

			int randomLevel = (int)UnityEngine.Random.Range(calculateMinLevel(cX, cY, staticLevel), calculateMaxLevel(cX, cY, staticLevel));
			return randomLevel;
		}
		/**
		 * remove clamp for sector ship generation.
		 */
		[HarmonyPatch(typeof(GameManager), "RandomizeShipLevel")]
		static class GameManager_RandomizeShipLevel
		{
			static bool Prefix(float distanceFromCenter, int bonus, ref TSector ___currSector, ref int __result)
			{
				int num = (int)(distanceFromCenter / 1000f);
				int min = ___currSector.level - 1 + num / 2 + bonus;
				int max = ___currSector.level + 2 + num + bonus;
				int num2 = bonus / 2;
				if (num2 > 10)
				{
					num2 = 10;
				}
				// __result = Mathf.Clamp(UnityEngine.Random.Range(min, max), 1, 50 + num2);
				__result = UnityEngine.Random.Range(min, max);
				return false;
			}
		}
		/**
		 * Update nearby sectors on level up.
		 */
		/*[HarmonyPatch(typeof(TSector), "LevelUp")]
		static class TSector_LevelUp
		{
			static void Postfix(TSector __instance)
			{
				AdjustNeighboringSectors(__instance);  //This seems to get caught in a loop.  Might be ok, though.
			}
		}*/
		/*[HarmonyPatch(typeof(TSector), "UpdateSectorLevels")]
		static class TSector_UpdateSectorLevels
		{
			static void Postfix(List<Station> ___smallBases, int ___level, int ___x, int ___y, ref List<Hideout> ___hideouts)
			{
				logr.Log($"UpdateSectorLevels Postfix New Level: {___level}");
				for (int i = 0; i < GameData.data.sectors.Count; i++)
				{
					int cX = GameData.data.sectors[i].x;
					int cY = GameData.data.sectors[i].y;
					int staticLevel = (int)Vector2.Distance(new Vector2((float)___x, (float)___y), new Vector2((float)cX, (float)cY));
					if (___level - staticLevel > GameData.data.sectors[i].level)
						logr.Log($"Comparing to Sector Level: i:{i} curr: {___level} remote:{GameData.data.sectors[i].level} Distance: {staticLevel} Want: {___level - staticLevel}");

					if (___level - staticLevel*4 > GameData.data.sectors[i].level + UnityEngine.Random.Range(1,10))
					{
						logr.Warn("Leveling up sector");
						GameData.data.sectors[i].level++;
//						GameData.data.sectors[i].AdjustLevel(GameData.data.sectors[i].level+1, false, false, false);
					}
				}
//				List<Station> hideouts = __instance.GetHideouts(HideoutType.Marauder, false);
				for (int i = 0; i < ___smallBases.Count; i++)
				{
					HideoutStation hideoutStation;
					if ((hideoutStation = (___smallBases[i] as HideoutStation)) != null && hideoutStation.type== HideoutType.Marauder)
					{
						//logr.Log($"Hideout Station chars: {hideoutStation.aiChars} {hideoutStation.aiChars.Count}");
						for(int j=0;j< hideoutStation.aiChars.Count;j++)
						{
							//logr.Log($"char: {hideoutStation.aiChars[j]} {hideoutStation.aiChars[j].level}");
						}
						if (hideoutStation.aiChars.Count < 2 && UnityEngine.Random.Range(0, 100) < 10)
						{

							logr.Log("regenerating Mauraders");
							hideoutStation.GenerateShips();
						}
					}
				}
			}
		}*/
		public static void AdjustNeighboringSectors(TSector sector)
		{
			logr.Log($"AdjustNeighboringSectors Comparing to: {sector.level}");
			for (int i = 0; i < GameData.data.sectors.Count; i++)
			{
				int cX = GameData.data.sectors[i].x;
				int cY = GameData.data.sectors[i].y;
				int staticLevel = (int)Vector2.Distance(new Vector2((float)sector.x, (float)sector.y), new Vector2((float)cX, (float)cY));
				if (sector.level - staticLevel > GameData.data.sectors[i].level)
					logr.Log($"Comparing to Sector Level: i:{i} curr: {sector.level} remote:{GameData.data.sectors[i].level} Distance: {staticLevel} Want: {sector.level - staticLevel}");

				if (sector.level - staticLevel * 4 > GameData.data.sectors[i].level)
				{
					logr.Warn("Leveling up sector");
					GameData.data.sectors[i].level++;
					//						GameData.data.sectors[i].AdjustLevel(GameData.data.sectors[i].level+1, false, false, false);
				}
			}
			//				List<Station> hideouts = __instance.GetHideouts(HideoutType.Marauder, false);
			for (int i = 0; i < sector.smallBases.Count; i++)
			{
				HideoutStation hideoutStation;
				if ((hideoutStation = (sector.smallBases[i] as HideoutStation)) != null && hideoutStation.type == HideoutType.Marauder)
				{
					//logr.Log($"Hideout Station chars: {hideoutStation.aiChars} {hideoutStation.aiChars.Count}");
					for (int j = 0; j < hideoutStation.aiChars.Count; j++)
					{
						//logr.Log($"char: {hideoutStation.aiChars[j]} {hideoutStation.aiChars[j].level}");
					}
					if (hideoutStation.aiChars.Count < 2 && UnityEngine.Random.Range(0, 100) < 10)
					{

						logr.Log("regenerating Mauraders");
						hideoutStation.GenerateShips();
					}
				}
			}
		}
		/**
		 * Don't throw an error when trying to level up a non-generated sector
		 */
		[HarmonyPatch(typeof(TSector), "AdjustLevel")]
		static class TSector_AdjustLevel
		{
			static bool Prefix(int newLevel, bool ___generated, ref int ___level)
			{
				if (!___generated)
				{
					___level = newLevel;
					return false;
				}
				return true;
			}
		}
		/*
		 * This didn't seem to do anything????
		 * [HarmonyPatch(typeof(GalaxyMap), "GetSectorInfo")]
		static class GalaxyMap_GetSectorInfo
		{
			static void Postfix(TSector sector, ref string __result)
			{
				logr.Log("GalaxyMap_GetSectorInfo");
				__result += $"\nLarge Asteroids: {sector.bigAsteroids.Count}";
			}
		}*/
		[HarmonyPatch(typeof(TSector), "GetString")]
		static class TSector_GetString
		{
			static void Postfix(bool ___discovered, List<BigAsteroid> ___bigAsteroids, ref string __result)
			{
				if(___discovered)
					__result += $"\nLarge Asteroids: {___bigAsteroids.Count}";
			}
		}
	}
}
