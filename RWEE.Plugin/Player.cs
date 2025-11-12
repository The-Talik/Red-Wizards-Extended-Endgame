using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RWEE
{
	internal class Player
	{
		/**
		 * give less experience the higher above CL50 you are.
		 */
		[HarmonyPatch(typeof(PChar), "EarnXP")]
		static class PChar_EarnXP
		{
			static void Prefix(float amount, int type, ref int ___maxLevel, int baseLevel)
			{
				//Main.log($"EarnXP {amount}");

				if (PChar.Char.level >= 50)
				{
					float mult = (Main.NEW_PCHAR_MAXLEVEL - PChar.Char.level) / (Main.NEW_PCHAR_MAXLEVEL - 50f);
					mult = -(1f - mult) * (100f / 3f);

					for (int i = 0; i < PChar.Char.passive.Length; i++)
					{
						PChar.Char.passive[i] = (int)Mathf.Min(PChar.Char.passive[i], mult);  //xp multiplier formula = 1 + passive*0.03
					}
				}
			}
			static void Postfix(ref int ___maxLevel)
			{
				//___maxLevel = Main.Old_PChar_MaxLevel;
			}
		}
		[HarmonyPatch(typeof(PChar), "UpdateChar")]
		static class PChar_UpdateChar
		{
			static void Prefix(ref int ___maxLevel)
			{
				___maxLevel = Main.NEW_PCHAR_MAXLEVEL;
			}
			static void Postfix(ref int ___maxLevel)
			{
				___maxLevel = Main.OLD_PCHAR_MAXLEVEL;
			}
		}
		[HarmonyPatch(typeof(PChar), "TechLevelUp")]
		static class PChar_TechLevelUp
		{
			static bool Prefix()
			{
				if (PChar.Char.fighterPilot >= Main.NEW_SECT_CAP)
					return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(PChar), "SpacePilotUp")]
		static class PChar_SpacePilotUp
		{
			static bool Prefix()
			{
				if (PChar.Char.fighterPilot >= Main.OLD_PCHAR_MAXLEVEL)
					return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(PChar), "FleetCommanderUp")]
		static class PChar_FleetCommanderUp
		{
			static bool Prefix()
			{
				if (PChar.Char.fleetCommander >= Main.OLD_PCHAR_MAXLEVEL)
					return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(PChar), "GeologyUp")]
		static class PChar_GeologyUp
		{
			static bool Prefix()
			{
				if (PChar.Char.geology >= Main.OLD_PCHAR_MAXLEVEL)
					return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(PChar), "ExplorerUp")]
		static class PChar_ExplorerUp
		{
			static bool Prefix()
			{
				if (PChar.Char.explorer >= Main.OLD_PCHAR_MAXLEVEL)
					return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(PChar), "ConstructionUp")]
		static class PChar_ConstructionUp
		{
			static bool Prefix()
			{
				if (PChar.Char.explorer >= Main.NEW_SECT_CAP)
					return false;
				return true;
			}
		}
		/*
		[HarmonyPatch(typeof(PChar), "GetRelevantLevelRank")]
		static class PChar_GetRelevantLevelRank
		{
			static bool Prefix(int statLevel, int compareLevel, int flatBonus, float finalMod, ref int __result)
			{
				int num = 0;
				if (statLevel >= PChar.maxLevel)
				{
					__result = 0;
					return false;
				}
				if (statLevel <= 20)
				{
					int num2 = statLevel - 5;
					if (num2 < 0)
					{
						num2 = 0;
					}
					num = compareLevel - num2 + 1;
					num = Mathf.Clamp(num, 0, 8);
				}
				else
				{
					float num3 = (float)statLevel * 0.75f;
					float num4 = (float)statLevel * 0.25f;
					float num5 = ((float)compareLevel - num3) / num4;
					if (num5 < 0f)
					{
						num = 0;
					}
					else
					{
						if (num5 <= 0.2f)
						{
							num = 1;
						}
						if (0.2f < num5 && num5 <= 0.4f)
						{
							num = 2;
						}
						if (0.4f < num5 && num5 <= 0.6f)
						{
							num = 3;
						}
						if (0.6f < num5 && num5 <= 0.8f)
						{
							num = 4;
						}
						if (0.8f < num5 && num5 <= 1f)
						{
							num = 5;
						}
						if (1f < num5 && num5 <= 1.2f)
						{
							num = 6;
						}
						if (1.2f < num5 && num5 <= 1.4f)
						{
							num = 7;
						}
						if (1.4f < num5)
						{
							num = 8;
						}
					}
				}
				__result=  Mathf.RoundToInt((float)(num + flatBonus) * (1f + finalMod));
				return false;
			}
		}
		*/
	}
}
