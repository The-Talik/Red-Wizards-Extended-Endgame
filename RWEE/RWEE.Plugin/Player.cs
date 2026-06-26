using HarmonyLib;
using RW.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RWEE.Logging;
using UnityEngine.UI;
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
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var code = new List<CodeInstruction>(instructions);
				var charField = AccessTools.Field(typeof(PChar), "Char");
				var currXpField = AccessTools.Field(typeof(PlayerCharacter), "currXP");
				var throttleMethod = AccessTools.Method(typeof(PChar_EarnXP), nameof(ThrottleCharacterLevelXp));
				int patched = 0;

				for (int i = 0; i <= code.Count - 6; i++)
				{
					if (code[i].opcode == OpCodes.Ldsfld && Equals(code[i].operand, charField) &&
						code[i + 1].opcode == OpCodes.Dup &&
						code[i + 2].opcode == OpCodes.Ldfld && Equals(code[i + 2].operand, currXpField) &&
						LoadsAmountArgument(code[i + 3]) &&
						code[i + 4].opcode == OpCodes.Add &&
						code[i + 5].opcode == OpCodes.Stfld && Equals(code[i + 5].operand, currXpField))
					{
						code.Insert(i + 4, new CodeInstruction(OpCodes.Call, throttleMethod));
						patched++;
						i += 5;
					}
				}

				if (patched != 1)
					logr.Warn($"RWEE PChar.EarnXP transpiler expected 1 currXP patch, found {patched}. Character XP throttling may be inactive.");

				return code;
			}

			static bool LoadsAmountArgument(CodeInstruction instruction)
			{
				return instruction.opcode == OpCodes.Ldarg_0 ||
					(instruction.opcode == OpCodes.Ldarg_S && IsArgumentZero(instruction.operand)) ||
					(instruction.opcode == OpCodes.Ldarg && IsArgumentZero(instruction.operand));
			}

			static bool IsArgumentZero(object operand)
			{
				if (operand is int index)
					return index == 0;
				if (operand is short shortIndex)
					return shortIndex == 0;
				if (operand is byte byteIndex)
					return byteIndex == 0;
				return false;
			}

			static float ThrottleCharacterLevelXp(float amount)
			{
				if (PChar.Char == null || PChar.Char.level <= Main.OLD_PCHAR_MAXLEVEL)
					return amount;

				float levelRange = Main.NEW_PCHAR_MAXLEVEL - Main.OLD_PCHAR_MAXLEVEL;
				if (levelRange <= 0f)
					return amount;

				float multiplier = (Main.NEW_PCHAR_MAXLEVEL - PChar.Char.level) / levelRange;
				return amount * Mathf.Clamp01(multiplier);
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

				static bool Prefix(List<AIMercenaryCharacter> ___mercenaries, ref int __result)
				{
					//logr.Log("GetFleetSize Prefix");
					if (!fleet_override)
						return true;

					//logr.Log($"GetFleetSize original: {___mercenaries.Count}");
					__result = 0;
					for (int i = 0; i < ___mercenaries.Count; i++)
					{
						if (___mercenaries[i].IsActive())
						{
							__result++;
						}
					}
					//logr.Log($"GetFleetSize active: {__result}");
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
