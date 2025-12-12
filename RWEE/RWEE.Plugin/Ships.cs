using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Random;
using static RWEE.Logging;
using RW;
namespace RWEE
{
	internal class Ships
	{
		static readonly FieldInfo disabled_fi = AccessTools.Field(typeof(InstalledEquipment), "disabled");

		[HarmonyPatch(typeof(SpaceShipData), "CheckEquipmentSpaceOcupied")]
		static class SpaceShipData_CheckEquipmentSpaceOcupied
		{
			static bool Prefix(bool ___isPlayerCharacter, ShipModelData modelData, int ___equipmentSpace, ref List<InstalledEquipment> ___equipments, ref float __result)
			{
				if (___equipments == null)
					return true;
				logr.Log("Checking Equipment Space Occupied {___isPlayerCharacter}");
				float totalEquipmentSpace = 0f;
				float totalDroneSpace = 0f;
				float hangarDroneSpace = (float)modelData.hangarDroneSpace;
				foreach (InstalledEquipment installedEquipment in ___equipments)
				{
					Equipment equipment = EquipmentDB.GetEquipment(installedEquipment.equipmentID);

					int disabledCount = 0;
					for (int i = 1; i <= installedEquipment.qnt; i++)
					{
						if (totalEquipmentSpace + equipment.space > ___equipmentSpace)
						{
							disabledCount++;
						}
						else
						{
							totalEquipmentSpace += equipment.space;
							totalDroneSpace += equipment.IsDrone ? equipment.space : 0f;
						}
					}
					disabled_fi.SetValue(installedEquipment, disabledCount);
					//logr.Log($"Equipment: {equipment.name} x{installedEquipment.qnt}. Setting disabled to: {disabled_fi.GetValue(installedEquipment)}");
				}

				if (hangarDroneSpace > 0f && totalDroneSpace > 0f)
				{
					float spaceCoveredByHangar = hangarDroneSpace;
					if (totalDroneSpace < spaceCoveredByHangar)
					{
						spaceCoveredByHangar = totalDroneSpace;
					}

					totalEquipmentSpace -= spaceCoveredByHangar;
				}

				__result = totalEquipmentSpace;
				logr.Log($"Done.  Equipment Space: {totalEquipmentSpace}/{___equipmentSpace}");
				if (___isPlayerCharacter)
					PlayerControl.inst.calculateShipASAP = true;
				return false;
			}

		}
		/**
		 * Disables effects for disabled equipment
		 */
		/*[HarmonyPatch]
		static class EquipmentDB_GetEffect
		{
			[HarmonyPrefix]
			[HarmonyPatch(typeof(EquipmentDB), "GetEffect")]
			[HarmonyPatch(typeof(EquipmentDB), "GetEnergyExpend")]
			static void Prefix(ref List<InstalledEquipment> equipments)
			{
				equipments = filterInstalledEquipment(equipments);
			}
		}
		[HarmonyPatch]
		static class EquipmentDB_GetEffect2
		{
			[HarmonyPrefix]
			[HarmonyPatch(typeof(SpaceShip), "ApplyPreShipBonus")]
			static void Prefix(ref List<InstalledEquipment> equips)
			{
				equips = filterInstalledEquipment(equips);
			}
		}*/
		static List<InstalledEquipment> filterInstalledEquipment(List<InstalledEquipment> equipments)
		{
			if (equipments == null || equipments.Count == 0 || disabled_fi == null)
				return equipments;
			var filtered = new List<InstalledEquipment>(equipments.Count);

			for (int i = 0; i < equipments.Count; i++)
			{
				var inst = equipments[i];
				if (inst == null)
					continue;
				if ((int)disabled_fi.GetValue(inst) < inst.qnt)
				{
					equipments[i].qnt = inst.qnt - (int)disabled_fi.GetValue(inst);
					filtered.Add(inst);
				}
			}
			return filtered;
		}
		[HarmonyPatch(typeof(ShipInfo), "LoadData")]
		static class ShipInfo_LoadData
		{
			static void Postfix(ShipInfo __instance, SpaceShip ___ss, UnityEngine.Transform ___itemPanel)
			{
				if (__instance == null || ___ss == null || ___ss.shipData == null)
					return;

				var panel = ___itemPanel;
				if (panel == null)
					return;

				// adjust this to whatever the installed list is actually called
				var installedList = ___ss.shipData.equipments; // List<InstalledEquipment>
				if (installedList == null)
					return;

				for (int i = 0; i < panel.childCount; i++)
				{
					var child = panel.GetChild(i);
					if (child == null || !child.gameObject.activeInHierarchy)
						continue;

					var slot = child.GetComponent<EquipmentSlot>();
					if (slot == null)
						continue;

					// skip non-installed slots (built-in, headers, etc.)
					if (slot.isBuiltInEquipment)
						continue;

					int idx = slot.itemIndex;
					if (idx < 0 || idx >= installedList.Count)
						continue;

					var inst = installedList[idx];
					int disabledCount = (int)disabled_fi.GetValue(inst);
					if (disabledCount == 0)
						continue;

					var text = child.GetComponentInChildren<UnityEngine.UI.Text>();
					if (text == null)
						continue;

					// avoid double-tagging if LoadData gets called multiple times
					if (text.text.Contains("disabled]"))
						continue;
					if (inst.qnt > 1)
						text.text += $" <color=#888888>[{disabledCount} disabled]</color>";
					else
						text.text += " <color=#888888>[disabled]</color>";
				}
			}
		}

		[HarmonyPatch(typeof(SpaceShip), "CalculateShipStats")]
		static class SpaceShip_CalculateShipStats
		{
			static void Prefix(SpaceShip __instance, ref ShipStats ___stats, ref SpaceShipData ___shipData, ref List<InstalledEquipment> __state)
			{
				logr.Open("SpaceShip_CalculateShipStats");
				//logr.Log($"Calculating ship stats - temporarily removing disabled equipment is player:{__instance.IsPlayer}");
				if (!__instance.IsPlayer)
					return;
				//___shipData.CheckEquipmentSpaceOcupied(___stats.modelData);

				__state = ListUtils.Clone<InstalledEquipment>(___shipData.equipments);

				___shipData.equipments = filterInstalledEquipment(___shipData.equipments);

			}
			static void Postfix(SpaceShip __instance, ref SpaceShipData ___shipData, ref List<InstalledEquipment> __state)
			{
				logr.Close("SpaceShip_CalculateShipStats");
				//logr.Log($"Restoring full equipment list after ship stats calculation  is player:{__instance.IsPlayer}");
				if (!__instance.IsPlayer)
					return;

				if (__state == null || __state.Count == 0)
					return;
				___shipData.equipments = __state;

			}
		}

	}
}
