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
				//PChar.Char.techLevel = 101;
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
		[HarmonyPatch(typeof(PChar), "TechLevelUp")]
		static class PChar_TechLevelUp
		{
			static bool Prefix()
			{
				//Main.error("TechLevelUp");
				//if (PChar.Char.techLevel < 101)
				//	PChar.Char.techLevel = 101;

				if (PChar.Char.techLevel >= Main.NEW_SECT_CAP)
					return false;
				//Main.error("true");
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
		
		[HarmonyPatch(typeof(PChar), "UpdateChar")]
		static class PChar_UpdateChar
		{
			static void Postfix()
			{
				if (PChar.Char.level >= Main.NEW_PCHAR_MAXLEVEL)
				{
					PChar.Char.currXP = (float)PChar.GetlevelEXP(Main.NEW_PCHAR_MAXLEVEL);
				}
				if (PChar.Char.fighterPilot > Main.OLD_PCHAR_MAXLEVEL)
				{
					PChar.Char.fighterPilot = Main.OLD_PCHAR_MAXLEVEL;
				}
				if (PChar.Char.fleetCommander > Main.OLD_PCHAR_MAXLEVEL)
				{
					PChar.Char.fleetCommander = Main.OLD_PCHAR_MAXLEVEL;
				}
				if (PChar.Char.geology > Main.OLD_PCHAR_MAXLEVEL)
				{
					PChar.Char.geology = Main.OLD_PCHAR_MAXLEVEL;
				}
				if (PChar.Char.explorer > Main.OLD_PCHAR_MAXLEVEL)
				{
					PChar.Char.explorer = Main.OLD_PCHAR_MAXLEVEL;
				}
			}
		}
	}
}
