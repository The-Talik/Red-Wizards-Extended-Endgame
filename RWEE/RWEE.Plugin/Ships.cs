using HarmonyLib;
using RW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RWEE.Logging;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Random;

namespace RWEE
{
	internal class Ships
	{
		/**
		 * Disables equipment that exceeds the ship's equipment space.
		 */
		static readonly FieldInfo eq_disabled_fi = AccessTools.Field(typeof(InstalledEquipment), "disabled");
		static readonly FieldInfo we_disabled_fi = AccessTools.Field(typeof(EquipedWeapon), "disabled");

		[HarmonyPatch(typeof(SpaceShipData), "CheckEquipmentSpaceOcupied")]
		static class SpaceShipData_CheckEquipmentSpaceOcupied
		{
			static bool Prefix(bool ___isPlayerCharacter, ShipModelData modelData, int ___equipmentSpace, ref List<InstalledEquipment> ___equipments, ref float __result)
			{
				if (!RweeConfig.disableEquipment.Value)
					return true;
				if (___equipments == null)
					return true;
				logr.Log("Checking Equipment Space Occupied {___isPlayerCharacter}");
				float totalEquipmentSpace = 0f;
				float totalDroneSpace = 0f;
				float hangarDroneSpaceLeft = (float)modelData.hangarDroneSpace;
				logr.Log("Drone hangar space: " + hangarDroneSpaceLeft);
				foreach (InstalledEquipment installedEquipment in ___equipments)
				{
					Equipment equipment = EquipmentDB.GetEquipment(installedEquipment.equipmentID);
					if (equipment.space == 0)
						continue;
					int disabledCount = 0;
					for (int i = 1; i <= installedEquipment.qnt; i++)
					{
						float spaceUsed = equipment.space;

						if (equipment.IsDrone && hangarDroneSpaceLeft > 0)
						{
							if (spaceUsed <= hangarDroneSpaceLeft)
							{
								hangarDroneSpaceLeft -= equipment.space;
								spaceUsed = 0;
							}
							else
							{
								spaceUsed -= hangarDroneSpaceLeft;
								hangarDroneSpaceLeft = 0f;
							}
						}

						if (totalEquipmentSpace + spaceUsed > ___equipmentSpace)
						{
							disabledCount++;
						}
						else
						{
							totalEquipmentSpace += spaceUsed;
						}
					}
					eq_disabled_fi.SetValue(installedEquipment, disabledCount);
					//logr.Log($"Equipment: {equipment.name} x{installedEquipment.qnt}. Setting disabled to: {eq_disabled_fi.GetValue(installedEquipment)}");
				}

				__result = totalEquipmentSpace;
				logr.Log($"Done.  Equipment Space: {totalEquipmentSpace}/{___equipmentSpace} (drone space free: {hangarDroneSpaceLeft}/{modelData.hangarDroneSpace}");
				if (___isPlayerCharacter)
					PlayerControl.inst.calculateShipASAP = true;
				return false;
			}
		}
		[HarmonyPatch(typeof(SpaceShipData), "CheckWeaponSpaceOcupied")]
		static class SpaceShipData_CheckWeaponSpaceOcupied
		{
			static bool Prefix(bool ___isPlayerCharacter, float ___weaponSpace, ref List<EquipedWeapon> ___weapons, ref float __result)
			{

				if (!RweeConfig.disableEquipment.Value)
					return true;
				if (___weapons == null)
					return true;

				float num = 0f;
				foreach (EquipedWeapon equipedWeapon in ___weapons)
				{
					if (num + GameData.data.weaponList[equipedWeapon.weaponIndex].space > ___weaponSpace)
					{
						we_disabled_fi.SetValue(equipedWeapon, 1);
					}
					else
					{
						we_disabled_fi.SetValue(equipedWeapon, 0);
						num += GameData.data.weaponList[equipedWeapon.weaponIndex].space;
					}
				}
				__result = num;

				if (___isPlayerCharacter)
					PlayerControl.inst.calculateShipASAP = true;
				return false;
			}
		}


		/**
		 * 
		 * Visually indicate disabled equipment in the ship info panel.
		 */
		[HarmonyPatch(typeof(ShipInfo), "LoadData")]
		static class ShipInfo_LoadData
		{
			static void Postfix(ShipInfo __instance, SpaceShip ___ss, UnityEngine.Transform ___itemPanel)
			{
				if (!RweeConfig.disableEquipment.Value)
					return;
				if (__instance == null || ___ss == null || ___ss.shipData == null)
					return;

				var panel = ___itemPanel;
				if (panel == null)
					return;


				var installedEquipment = ___ss.shipData.equipments; // List<InstalledEquipment>
				var installedWeapons = ___ss.shipData.weapons; // List<InstalledEquipment>

				for (int i = 0; i < panel.childCount; i++)
				{
					var child = panel.GetChild(i);
					if (child == null || !child.gameObject.activeInHierarchy)
						continue;

					var slot = child.GetComponent<EquipmentSlot>();
					bool plural = false;
					int disabledCount = 0;
					if (slot == null)
						return;

					// skip non-installed slots (built-in, headers, etc.)
					if (slot.isBuiltInEquipment)
						continue;

					int idx = slot.itemIndex;
					if (idx < 0)
						continue;
					switch (slot.itemType)
					{
						case 1: //weapons
							if (idx >= installedWeapons.Count)
								continue;
							var we_inst = installedWeapons[idx];
							disabledCount = (int)we_disabled_fi.GetValue(we_inst);
							break;
						case 2: //equipment
							if (idx >= installedEquipment.Count)
								continue;
							var eq_inst = installedEquipment[idx];
							disabledCount = (int)eq_disabled_fi.GetValue(eq_inst);
							break;
						default:
							continue;
					}



					if (disabledCount == 0)
						continue;


					var text = child.GetComponentInChildren<UnityEngine.UI.Text>();
					if (text == null)
						continue;

					// avoid double-tagging if LoadData gets called multiple times
					if (text.text.Contains("disabled]"))
						continue;
					if (plural)
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
					if (!RweeConfig.disableEquipment.Value)
						return;
					//logr.Open("SpaceShip_CalculateShipStats");
					//logr.Log($"Calculating ship stats - temporarily removing disabled equipment is player:{__instance.IsPlayer}");
					if (!__instance.IsPlayer)
						return;
					//___shipData.CheckEquipmentSpaceOcupied(___stats.modelData);

					__state = ListUtils.Clone<InstalledEquipment>(___shipData.equipments);

					___shipData.equipments = filterInstalledEquipment(___shipData.equipments);

				}
				static void Postfix(SpaceShip __instance, ref SpaceShipData ___shipData, ref List<InstalledEquipment> __state)
				{
					if (!RweeConfig.disableEquipment.Value)
						return;
					//logr.Close("SpaceShip_CalculateShipStats");
					//logr.Log($"Restoring full equipment list after ship stats calculation  is player:{__instance.IsPlayer}");
					if (!__instance.IsPlayer)
						return;

					if (__state == null || __state.Count == 0)
						return;
					___shipData.equipments = __state;

				}
			}
			static List<InstalledEquipment> filterInstalledEquipment(List<InstalledEquipment> equipments)
			{
				if (equipments == null || equipments.Count == 0 || eq_disabled_fi == null)
					return equipments;
				var filtered = new List<InstalledEquipment>(equipments.Count);

				for (int i = 0; i < equipments.Count; i++)
				{
					var inst = equipments[i];
					if (inst == null)
						continue;
					if ((int)eq_disabled_fi.GetValue(inst) < inst.qnt)
					{
						equipments[i].qnt = inst.qnt - (int)eq_disabled_fi.GetValue(inst);
						filtered.Add(inst);
					}
				}
				return filtered;
			}
			/*static List<EquipedWeapon> filterInstalledWeapons(List<EquipedWeapon> weapons)
			{
				if (weapons == null || weapons.Count == 0 || eq_disabled_fi == null)
					return weapons;
				var filtered = new List<EquipedWeapon>(weapons.Count);

				for (int i = 0; i < weapons.Count; i++)
				{
					var inst = weapons[i];
					if (inst == null)
						continue;
					if ((int)eq_disabled_fi.GetValue(inst) < 1)
					{
						weapons[i].qnt = inst.qnt - (int)eq_disabled_fi.GetValue(inst);
						filtered.Add(inst);
					}
				}
				return filtered;
			}*/
			[HarmonyPatch(typeof(Weapon), "Fire")]
			static class Weapon_Fire
			{
				static bool Prefix(Weapon __instance, SpaceShip ___ss)
				{
					if (___ss == null)
						return true;
					if (!___ss.IsPlayer && ___ss.IsPlayerFleet)
						return true;
					if (___ss.shipData == null
					|| ___ss.shipData.weapons == null
					|| __instance.equipedWeaponDataIndex >= ___ss.shipData.weapons.Count
					|| __instance.equipedWeaponDataIndex < 0
					|| ___ss.shipData.weapons[__instance.equipedWeaponDataIndex] == null)
						return true;
					var equipedWeapon = ___ss.shipData.weapons[__instance.equipedWeaponDataIndex];
					if ((int)we_disabled_fi.GetValue(equipedWeapon) > 0)
					{
logr.Log("weapon disabled");
						return false;
						
					}
					return true;
				}
			}
			/**
			 * Adds a chance to find the Geraki in high level debris fields if you have the Lone Wolf or Scoundrel perk, and the Lacewing if you have the Battleship Raid perk.
			 */
			[HarmonyPatch(typeof(DebrisFieldControl), "FinishScavenging")]
			static class DebrisFieldControl_FinishScavenging
			{
				static void Postfix(DebrisFieldControl __instance)
				{
					if (__instance.debrisField.level < 50)
						return;
					if (!__instance.debrisField.special && __instance.debrisField.level >= PChar.TechLevel() + 7)
						return;
					logr.Log("scanning debris field");
					var pos = __instance.transform.position;
					LootSystem ls = GameManager.instance.GetComponent<LootSystem>();
					if (PChar.HasPerk(4) || PChar.HasPerk(8))  //Lone Wolf or scoundrel
					{
						if (GameData.data.GetDeedCount("Found-Geraki") == 0)
						{
							int GerekiLoadout = GetGerekiLoadout();
							int chance = 1;
							if (GerekiLoadout >= 0)
								chance = 5;
							logr.Log($"Geraki chance: {chance}%");
							if (UnityEngine.Random.Range(1, 101) <= chance)
							{
								Vector3 normalized = UnityEngine.Random.rotation.eulerAngles.normalized;
								normalized.y = 0f;
								pos = pos + normalized * 5f;
								ls.InstantiateDrop(4, 56, 1, pos, 1, 0f, 40f, GetGerekiLoadout());
								GameData.data.AddDeed("Found-Geraki");
								return;
							}
						}
					}
					if (PChar.HasPerk(209))  //battleship raid
					{
						if (GameData.data.GetDeedCount("Found-Lacewing") == 0)
						{
							logr.Log($"Lacewing chance: 1%");
							if (UnityEngine.Random.Range(1, 101) <= 1)
							{
								Vector3 normalized = UnityEngine.Random.rotation.eulerAngles.normalized;
								normalized.y = 0f;
								pos = pos + normalized * 5f;
								ls.InstantiateDrop(4, 68, 1, pos, 1, 0f, 40f, -1);
								GameData.data.AddDeed("Found-Lacewing");
								return;
							}
						}
					}
				}
				private static int GetGerekiLoadout()
				{
					for (int i = 0; i < GameData.data.shipLoadouts.Count; i++)
					{
						var loadout = GameData.data.shipLoadouts[i];
						logr.Log($"Checking loadout {i}: {ObjUtils.GetRef(loadout.data.ShipModel)}");
						if (ObjUtils.GetRef(loadout.data.ShipModel) == "Geraki")
							return loadout.id;

					}
					return -1;
				}
				[HarmonyPatch(typeof(GameData), "SetGameData")]
				static class Patch_GameData_SetGameData
				{
					static void Postfix()
					{
						if (GameData.data.GetDeedCount("Found-Lacewing") > 0)
							return;
						if (ObjUtils.GetRef(GameData.data.spaceShipData.ShipModel) == "Lacewing")
							GameData.data.AddDeed("Found-Lacewing");
						if (GameData.data.shipLoadouts != null)
							for (int i = 0; i < GameData.data.shipLoadouts.Count; i++)
							{
								if (ObjUtils.GetRef(GameData.data.shipLoadouts[i].data.ShipModel) == "Lacewing")
									GameData.data.AddDeed("Found-Lacewing");
							}
					}
				}
				[HarmonyPatch(typeof(QuestControl), "CompleteQuest")]
				static class QuestControl_CompleteQuest
				{
					static void Prefix(int rewardChosen, ref Quest ___quest, ref Inventory ___inventory, ref PlayerControl ___pc, ref SpaceShipData __state)
					{
						if (___quest.refCode != 191)
							return;
						if (!QuestDB.IsQuestCompleted(___quest, ___pc.transform))
							return;

						logr.Log($"Quest Complete prefix {___quest.refCode} {rewardChosen} {ObjUtils.GetRef(GameData.data.spaceShipData.ShipModel)}");
						if (ObjUtils.GetRef(GameData.data.spaceShipData.ShipModel) != "Geraki")
							return;

						__state = ObjUtils.Clone<SpaceShipData>(GameData.data.spaceShipData);
						__state.equipments = ListUtils.Clone<InstalledEquipment>(GameData.data.spaceShipData.equipments);
						__state.weapons = ListUtils.Clone<EquipedWeapon>(GameData.data.spaceShipData.weapons);

						__state.members = ListUtils.Clone<AssignedCrewMember>(GameData.data.spaceShipData.members);
						__state.builtInData = ListUtils.Clone<BuiltInEquipmentData>(GameData.data.spaceShipData.builtInData);
						__state.enhancements = ListUtils.Clone<int>(GameData.data.spaceShipData.enhancements);
						if (rewardChosen == -1)
						{
							logr.Log("Stashing Gereki -- unloading items");
							GameData.data.spaceShipData.equipments.Clear();
							GameData.data.spaceShipData.weapons.Clear();
							GameData.data.spaceShipData.members.Clear();
						}
					}
					static void Postfix(int rewardChosen, ref Quest ___quest, ref Inventory ___inventory, ref PlayerControl ___pc, ref SpaceShipData __state)
					{
						if (___quest.refCode != 191)
							return;
						logr.Log($"Quest Complete postfix {___quest.refCode} {rewardChosen} {ObjUtils.GetRef(GameData.data.spaceShipData.ShipModel)}");
						if (__state == null)
							return;
						if (ObjUtils.GetRef(GameData.data.spaceShipData.ShipModel) == "Geraki")
							return;
						logr.Log($"Stashing {ObjUtils.GetRef(__state.ShipModel)}");
						int geraki = GameData.data.NewShipLoadout(null);
						GameData.data.SetShipLoadout(__state, geraki);
					}
				}
			}
		
	}
}
