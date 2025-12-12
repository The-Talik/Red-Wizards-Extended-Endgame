using HarmonyLib;
using RW.Logging;
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RWEE.Logging;
namespace RWEE
{
	internal static class Player
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
				//logr.Log($"EarnXP {amount}");

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
				//logr.Error("TechLevelUp");
				//if (PChar.Char.techLevel < 101)
				//	PChar.Char.techLevel = 101;

				if (PChar.Char.techLevel >= Main.NEW_SECT_CAP)
					return false;
				//logr.Error("true");
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
		public static class SpacePilotBonusOverride
		{
			public static bool fleet_override = false;
			
			[HarmonyPatch]
			static class PChar_ApplySoloFlyingBonuses
			{
				[HarmonyPrefix]
				[HarmonyPatch(typeof(PChar), "ApplySoloFlyingBonuses")]
				[HarmonyPatch(typeof(PChar), "GetSpacePilotBonus")]
				static void Prefix()
				{
					logr.Open("ApplySoloFlyingBonuses");
					fleet_override = true;
				}
				[HarmonyPostfix]
				[HarmonyPatch(typeof(PChar), "ApplySoloFlyingBonuses")]
				[HarmonyPatch(typeof(PChar), "GetSpacePilotBonus")]
				static void Postfix()
				{
					logr.Close("ApplySoloFlyingBonuses");
					fleet_override = false;
				}
			}
			[HarmonyPatch(typeof(PlayerCharacter), "get_GetFleetSize")]
			static class PlayerCharacter_get_GetFleetSize
			{
				
				static bool Prefix(List<AIMercenaryCharacter> ___mercenaries,ref int __result)
				{
					logr.Log("GetFleetSize Prefix");
					if (!fleet_override)
						return true ;
						
					logr.Log($"GetFleetSize original: {___mercenaries.Count}");
					__result = 0;
					for(int i = 0; i < ___mercenaries.Count; i++)
					{
						if (___mercenaries[i].IsActive())
						{
							__result++;
						}
					}
					logr.Log($"GetFleetSize active: {__result}");
					return false;
				}
			}
			[HarmonyPatch]
			static class AIMercenary_recalculateShipASAP
			{
				[HarmonyPostfix]
				[HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.Die))]
				[HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.DockAtCarrier))]
				[HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.EmergencyWarp))]
				[HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.Vanish))]
//				[HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.StationDockingReached))]
				[HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.DockAtStation))]
				[HarmonyPatch(typeof(GameManager), nameof(GameManager.LaunchPlayerFleetMember))]
				static void Postfix(System.Reflection.MethodBase __originalMethod)
				{
					logr.Warn($"{__originalMethod} Recalculating ship ASAP due to mercenary change.");
					if (PlayerControl.inst != null)
					{
logr.Log($"Is Player.");
						
						PlayerControl.inst.CalculateShip(false);
						PlayerControl.inst.GetSpaceShip.VerifyShipCargoAndEquipment();

						Inventory.instance.RefreshIfOpen(null, true, true);
						//						PlayerControl.inst.calculateShipASAP = true;
					}
					else
						logr.Log($"No Player Found.");
				}
			}
		}
		[HarmonyPatch(typeof(PlayerControl), "CalculateShip")]
		static class PlayerControl_CalculateShip
		{
			static void Prefix()
			{
				logr.Open($"CalculateShip");
			}
			static void Postfix()
			{
				logr.Close($"CalculateShip");
			}
		}
	}
}
