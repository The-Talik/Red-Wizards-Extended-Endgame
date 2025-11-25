using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace RWEE
{
	internal class Ships
	{
		static readonly FieldInfo disabled_fi = AccessTools.Field(typeof(InstalledEquipment), "disabled");

		[HarmonyPatch(typeof(SpaceShipData), "CheckEquipmentSpaceOcupied")]
		static class SpaceShipData_CheckEquipmentSpaceOcupied
		{
			/*static void Prefix(ShipModelData modelData, int ___equipmentSpace, ref List<InstalledEquipment> ___equipments)
			{
				float totalEquipmentSpace = 0f;
				float hangarDroneSpace = (float)modelData.hangarDroneSpace;
				List<InstalledEquipment> fixedEquipments = new List<InstalledEquipment>();
				foreach (InstalledEquipment installedEquipment in ___equipments)
				{
					Equipment equipment = EquipmentDB.GetEquipment(installedEquipment.equipmentID);
					float usedSpace = installedEquipment.qnt * equipment.space;

					FieldInfo disabled_fi = AccessTools.Field(typeof(InstalledEquipment), "disabled");
					if (totalEquipmentSpace + usedSpace > ___equipmentSpace)
					{
						disabled_fi.SetValue(installedEquipment, true);
					}
					else
					{
						totalEquipmentSpace += usedSpace;
						fixedEquipments.Add(installedEquipment);
					}

					Main.log($"Equipment: {equipment.name} x{installedEquipment.qnt} uses {usedSpace} space. {disabled_fi.GetValue(installedEquipment)}");
					___equipments = fixedEquipments;
				}
			}*/

			static bool Prefix(ShipModelData modelData, int ___equipmentSpace, ref List<InstalledEquipment> ___equipments, ref float __result)
			{
				float totalEquipmentSpace = 0f;
				float totalDroneSpace = 0f;
				float hangarDroneSpace = (float)modelData.hangarDroneSpace;

				foreach (InstalledEquipment installedEquipment in ___equipments)
				{
					Equipment equipment = EquipmentDB.GetEquipment(installedEquipment.equipmentID);
					float usedSpace = installedEquipment.qnt * equipment.space;

					FieldInfo disabled_fi = AccessTools.Field(typeof(InstalledEquipment), "disabled");
					if (totalEquipmentSpace + usedSpace > ___equipmentSpace)
					{
						disabled_fi.SetValue(installedEquipment, true);
					}
					else
					{
						totalEquipmentSpace += usedSpace;
						if (equipment.IsDrone)
						{
							totalDroneSpace += usedSpace;
						}
					}

					Main.log($"Equipment: {equipment.name} x{installedEquipment.qnt} uses {usedSpace} space. {disabled_fi.GetValue(installedEquipment)}");
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
				Main.log($"Equipment Space: {totalEquipmentSpace}/{___equipmentSpace}");
				return false;
			}

		}
		/**
		 * Disables effects for disabled equipment
		 */
		[HarmonyPatch(typeof(EquipmentDB), "GetEffect")]
		static class EquipmentDB_GetEffect
		{
			static void Prefix(List<InstalledEquipment> equipments)
			{
				if (equipments == null || equipments.Count == 0)
					return;

				// if we can't see the field, do nothing
				if (disabled_fi == null)
					return;
				var filtered = new List<InstalledEquipment>(equipments.Count);

				for (int i = 0; i < equipments.Count; i++)
				{
					var inst = equipments[i];
					if (inst == null)
						continue;

					if (!(bool)disabled_fi.GetValue(inst))
						filtered.Add(inst);
				}

				// use filtered list for this call only; caller's list is unchanged
				equipments = filtered;
			}
			
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
					if (!installed_equipment_is_disabled(inst))
						continue;

					var text = child.GetComponentInChildren<UnityEngine.UI.Text>();
					if (text == null)
						continue;

					// avoid double-tagging if LoadData gets called multiple times
					if (text.text.Contains("[disabled]"))
						continue;

					text.text += " <color=#888888>[disabled]</color>";
				}
			}

			static bool installed_equipment_is_disabled(InstalledEquipment inst)
			{
				if (disabled_fi == null || inst == null)
					return false;

				var val = disabled_fi.GetValue(inst);
				return val is bool b && b;
			}
		}
	}
}
