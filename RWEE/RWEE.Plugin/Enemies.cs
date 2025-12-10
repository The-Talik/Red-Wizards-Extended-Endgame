using HarmonyLib;
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RW;
using static RWEE.Logging;
/**
 * AIType 0: Normal, 1: Marauder, 2: Mercenary, 3: Sentinel, 4: Boss, 5: Guardian, 6: NPC, 7: Special Char
 * /* 0: this.Flee(); 1: this.TacticStrafeFire(); 2: this.TacticHitAndRun(); 3: this.TacticMelee();
					*/
namespace RWEE
{
	internal class Enemies
	{

		/**
		 * Give AI ships a bonus after level 50
		 */
		[HarmonyPatch(typeof(ShipStats), "ApplyAIBonus")]
		static class ShipStats_ApplyAIBonus
		{
			static void Postfix(AICharacter aiChar, ref float ___baseHP, ref float ___baseShield, ref float ___baseEnergy,
					ref float ___hpRegen, ref float ___shieldRecharge, ref float ___maxSpeed, ref float ___acceleration, ref SpaceShip ___ss)
			{
				if (aiChar.level > 50)
				{
					float origBaseHP = ___baseHP;
					float origBaseShield = ___baseShield;
					float origBaseEnergy = ___baseEnergy;
					float origHpRegen = ___hpRegen;
					float origShieldRecharge = ___shieldRecharge;
					float origMaxSpeed = ___maxSpeed;
					float origAcceleration = ___acceleration;
					float origDamageBonus = ___ss.dmgBonus;

					//logr.Log($"Applying AI bonus. lev: {aiChar.level} hp: {___baseHP} shield: {___baseShield} energy: {___baseEnergy} regen: {___hpRegen} regen shield-recharge: {___shieldRecharge} dam bonus: {___ss.dmgBonus}");
					//float mod = aiChar.level / 50;  //1x at L50, 2x at L100, 3x at L150, etc

					float mod = levelToMod(aiChar.level * (aiChar.AIType == 4 || aiChar.AIType == 3 ? 1.3f : 1f)); //bosses are harder, but give better loot.
					if (mod <= 0)
						mod = 0;
					___baseHP *= (1 + mod);
					___baseShield *= (1 + mod);
					___baseEnergy *= (1 + mod);
					___hpRegen += 0.0001f;
					___hpRegen *= (1 + mod);
					___shieldRecharge *= (1 + mod);
					___maxSpeed *= (1 + mod / 10);
					___acceleration *= (1 + mod / 10);

					___ss.dmgBonus += mod;
					logr.Log($"Boosting AI {aiChar.Name()} {(1 + mod)}x. lev: {aiChar.level} hp: {origBaseHP}→{___baseHP} shield: {origBaseShield}→{___baseShield}" +
						$"energy: {origBaseEnergy}→{___baseEnergy} regen: {origHpRegen}→{___hpRegen} shield-recharge: {origShieldRecharge}→{___shieldRecharge} dam bonus: origDamageBonus→{___ss.dmgBonus}");
				}
			}
		}
		static public float levelToMod(float level)
		{
			float mod = (float)(Math.Pow(level - 49, 1.5) - 1) / 100;
			//logr.Log($"level: {level} mod: {mod}");
			return Mathf.Max(mod, 0);
		}
		/**
 * Maurauders are more likely to be gold star at higher levels.
 */
		[HarmonyPatch(typeof(HideoutStation), "GenerateShips")]
		static class HideoutStation_GenerateShips
		{
			static void Postfix(HideoutType ___type, ref List<AICharacter> ___aiChars, int ___level)
			{
				if (___type != HideoutType.Marauder)
					return;
				for (int i = 0; i < ___aiChars.Count; i++)
				{
					if (UnityEngine.Random.Range(10, 50) < ___level)
						___aiChars[i].rank = 1;
					if (UnityEngine.Random.Range(50, 100) < ___level)
						___aiChars[i].rank++;
					if (___aiChars[i].rank > 2)
						___aiChars[i].rank = 2;
					___aiChars[i].shipData = null;
					logr.Log($"Rank: {___aiChars[i].rank}");
					___aiChars[i].DefineShipModel(new ShipType());
				}
			}
		}

		/**
		 * Adjust AI to use strafe if they are faster than their target.
		 */

		[HarmonyPatch(typeof(AIControl), "SetNewTarget")]
		static class AIControl_SetNewTarget
		{
			static void Postfix(ref AIControl __instance, SpaceShip ___ss)
			{
				//logr.Log($"AIControl_SetNewTarget: {__instance.Char.name}");
				if (__instance.Char.level > 50)
				{
					float newReaction = 2f / (__instance.Char.level / 25);
					__instance.reactionTime = Mathf.Min(__instance.reactionTime, Mathf.Clamp(newReaction, .25f, 2f));
				}
				SetTactic(ref __instance, ___ss);
			}
		}

		[HarmonyPatch(typeof(AIControl), "VerifyChangeTactic")]
		static class AIControl_VerifyChangeTactic
		{
			static void Postfix(ref AIControl __instance, SpaceShip ___ss)
			{
				SetTactic(ref __instance, ___ss);
			}
		}
		[HarmonyPatch(typeof(AIControl), "SetActions")]
		static class AIControl_SetActions
		{
			static void Postfix(ref AIControl __instance, SpaceShip ___ss)
			{
				SetTactic(ref __instance, ___ss);
			}
		}
		[HarmonyPatch(typeof(AIMarauder), "SetActions")]
		static class AIMarauder_SetActions
		{
			static void Prefix(ref AIControl __instance, SpaceShip ___ss, AICharacter ___Char, ref float ___attackDistance, ref float ___returnDistance)
			{
				___attackDistance = 250f * (1 + levelToMod(___Char.level));
				___returnDistance = 350f * (1 + levelToMod(___Char.level));

				if (__instance.targetEntity == null)
					return;
				SpaceShip targetShip = __instance.targetEntity as SpaceShip;
				if (!targetShip.IsPlayer)
				{
					___attackDistance = 250f;
					___returnDistance = 350f;
				}
			}
		}
		static void SetTactic(ref AIControl __instance, SpaceShip ___ss)// ref AICharacter ___Char, Entity ___targetEntity, SpaceShip ___ss)
		{
			if (__instance.Char.level < 50)
				return;
			float hpPerc = 1f * ___ss.currHP / ___ss.stats.baseHP;
			//logr.Log($"hp: {hpPerc}");
			if (hpPerc < .2)
			{
				__instance.Char.currTactic = 0; //flee
				__instance.target = null;
				__instance.targetEntity = null;
				//logr.Log("Fleeing");
				return;
			}
			if (__instance.targetEntity == null)
				return;
			if (__instance.Char.behavior.role == 0)
			{
				SpaceShip targetShip = __instance.targetEntity as SpaceShip;
				//Tactic: 0 == Alternate, 1 == Strafe and Fire, 2 == Hit and Run, 3 == Melee
				//logr.Log($"Ship: [{__instance.Char.Name()} L{__instance.Char.level}] maxSpd:{getMaxSpeed(___ss)} acc:{getAcceleration(___ss)} mass:{___ss.stats.mass} turn:{getTurnSpeed(___ss)} tactic:curr:{__instance.Char.currTactic} fav:{__instance.Char.behavior.favTactic} max:{__instance.Char.maxTactic} warp{__instance.Char.behavior.emergencyWarpHPThreshold}");
				//logr.Log($"Target: [{targetShip.name}] maxSpd:{getMaxSpeed(targetShip)} acc:{getAcceleration(targetShip)} mass:{targetShip.stats.mass} turn:{getTurnSpeed(targetShip)}");

				if (getAcceleration(___ss) > getAcceleration(targetShip) * 2 && getTurnSpeed(___ss) > getTurnSpeed(targetShip) * 2)
				{

					//we are more maneuverable than our target.
					//logr.Log("We qualify for tactic upgrade");
					__instance.Char.currTactic = 2;
					__instance.Char.behavior.favTactic = 2;
					__instance.Char.maxTactic = 2;
				}

			}
		}
		static float getMaxSpeed(SpaceShip ss)
		{
			return ss.stats.maxSpeed * ss.energyMmt.valueMod(2);
		}
		static float getAcceleration(SpaceShip ss)
		{
			return ss.stats.acceleration * ss.energyMmt.valueMod(2) / (ss.stats.mass / 100f);
		}
		static float getTurnSpeed(SpaceShip ss)
		{
			return ss.stats.turnSpeed;
		}
		[HarmonyPatch(typeof(ScanSystem), MethodType.Constructor, new Type[] { typeof(Transform), typeof(AIControl), typeof(int) })]
		static class ScanSystem_ScanSystem
		{
			static void Postfix(ref float ___scanDistance, AIControl aic)
			{
				if (aic == null)
					return;
				switch (aic.Char.AIType)
				{
					case 1:
					case 4:
					case 5:
						float orig = ___scanDistance;
						if (aic.Char.level > 50)
						{
							___scanDistance = orig * (1 + levelToMod(aic.Char.level));
						}
						//logr.Log($"Updating Scan Distance [{aic.Char.Name()} {aic.Char.level} {aic.Char.AIType}] {orig}->{___scanDistance}");
						break;
				}
			}
		}
		/**
		 * High levels drop high tier
		 */

		[HarmonyPatch(typeof(AIControl), "ConfigureAI")]
		static class AIControl_ConfigureAI
		{
			static void Postfix(AICharacter ___Char, SpaceShip ___ss)
			{
				
				if (___Char.rank < 1)
					return;
				for (int i = 0; i < ___ss.loots.Count; i++)
				{
					if (___ss.loots[i].itemType > 2)
						continue;
					int tmpLev = ___Char.level;
					if (___Char.rank == 1)
						tmpLev -= 50;
					int oldRarity = ___ss.loots[i].rarity;
					string itemLog = "";
					while (tmpLev > 50)
					{
						if (UnityEngine.Random.Range(1, 101) < 6 || Items.debugUpgrades)
						{
							___ss.loots[i].rarity++;
							itemLog += "+";
						}
						else
							itemLog += ".";

						if (___Char.AIType == 4)
							tmpLev -= 3;
						else
							tmpLev -= 5;
					}
					if (___ss.loots[i].rarity > Main.MAX_RARITY || Items.debugUpgrades)
						___ss.loots[i].rarity = Main.MAX_RARITY;
					if (___Char.level < 100 && !Items.debugUpgrades)
						if (___ss.loots[i].rarity > 6)
							___ss.loots[i].rarity = 6;

					logr.Log($"AIControl Improving Loot: Char  [{___Char.Name()} L{___Char.level}] {___Char.AIType} itemType:{___ss.loots[i].itemType} itemID:{___ss.loots[i].itemID} rarity:{oldRarity}->{itemLog}->{___ss.loots[i].rarity} rarityEnabled:{___ss.loots[i].rarityEnabled}");
				}
			}
		}
		[HarmonyPatch(typeof(EquipmentDB), "GetRandomEquipment")]
		static class EquipmentDB_GetRandomEquipment
		{
			static bool Prefix(List<Equipment> ___equipments,
	float minSpace, float maxSpace, int minPower, int maxPower, ref
	int effectType, ShipClassLevel maxShipClass, int faction,
	bool enableNoRarity, DropLevel maxDropLevel, int factionExtraChance, System.Random rand, ref Equipment __result)
			{
				//logr.Log(
				//	$"GetRandomEquipment(" +
				//	$"minSpace={minSpace:0.##}, maxSpace={maxSpace:0.##}, " +
				//	$"minPower={minPower}, maxPower={maxPower}, " +
				//	$"effectType={effectType}, maxShipClass={maxShipClass}({(int)maxShipClass}), " +
				//	$"faction={faction}, enableNoRarity={enableNoRarity}, " +
				//	$"maxDropLevel={maxDropLevel}({(int)maxDropLevel}), factionExtraChance={factionExtraChance}, " +
				//	$"rand={(rand == null ? "null" : rand.GetHashCode().ToString())}" +
				//	$")");
				if (effectType >= 0)
					return true;
				if (minPower < 50 +UnityEngine.Random.Range(0,100))  //L50 = always return;  L 150 = never
					return true;
				bool flag = GameData.data.gameMode == 1;
				//int type = rand.Next(0, EquipmentType.GetNames(typeof(EquipmentType)).Count());
				//logr.Log($"Looking for type {(EquipmentType)type}");
				List<int> list;
				//Cherry pick end-game gear
				//switch ((EquipmentType)type)
				//{
				//	case EquipmentType.Armor:
				list = new List<int> { 57, 8, 75, 100, 99, 76, 52, 179, 101, 180, 136, 129,
				//		};
				//		break;
				//	case EquipmentType.Battery:
				//		list = new List<int> { 
							110, 111, 112,
				//		};
				//		break;
				//	case EquipmentType.Booster:
				//		list = new List<int> {
							12, 24, 85, 86, 150, 151, 132, 135 ,198,
						//};
				//		break;
				//	case EquipmentType.Computer:
				//		list = new List<int> {
							156, 183, 184, 185, 27,63, 90, 93, 60, 171, 105,
						//};
				//		break;
				//	case EquipmentType.Device:
				//		list = new List<int> {
						181, 159, 158, 160, 114, 98, 165, 113, 162, 104,145,
						//};
				//		break;
				//	case EquipmentType.Engine:
				//		list = new List<int> {
							49, 41, 123, 147, 43, 149, 66, 97, 106, 146, 148, 107,
						//};
				//		break;
				//	case EquipmentType.Generator:
				//		list = new List<int> {
							51, 157, 67,108,68,109,
						//};
				//		break;
				//	case EquipmentType.Maneuverability:
				//		list = new List<int>{
						125, 128,
						//};
				//		break;
				//	case EquipmentType.Sensor:
				//		list = new List<int> {
							50, 38, 140,
						//};
				//		break;
				//	case EquipmentType.Shield:
				//		list = new List<int> {
							53, 54, 48, 56, 103, 69, 155,
						//};
				//		break;
				//	case EquipmentType.Utility:
				//		list = new List<int> {
							30, 193, 36, 37, 47, 154, 117, 122, 143, 119, 186 };
				//		break;
				//}
				//var fi = AccessTools.Field(typeof(EquipmentDB), "equipments");
				//var equipments = fi?.GetValue(null) as List<Equipment>;
				int num = 0;
				
				while (num < 100)
				{
					logr.Log($"loop {num}");
					int id = list[UnityEngine.Random.Range(0, list.Count)];
					Equipment equipment = ___equipments.FirstOrDefault(i => i != null && i.id == id);
					if (equipment == null)
					{
						logr.Warn($"equipment null id: {id}");
					}
					else {
						int lootChance = equipment.lootChance;
						if (factionExtraChance > 0 && equipment.repReq.factionIndex > 0)
							lootChance *= factionExtraChance;
						logr.Log("looking");
						if (equipment != null
							&& equipment.space >= minSpace
												&& equipment.space <= maxSpace
												&& UnityEngine.Random.Range(1, 101) <= lootChance
												&& equipment.dropLevel <= maxDropLevel
												&& (equipment.rarityMod > 0f || enableNoRarity)
												&& (!flag || equipment.spawnInArena)
												&& equipment.minShipClass <= maxShipClass
												&& (equipment.repReq.factionIndex == 0 || equipment.repReq.factionIndex == faction))
						{
							logr.Log($"found {equipment.equipName}");
							__result = equipment;
							return false;
						}
					}
					minSpace -= 1f;
					maxSpace += 1f;
					if (num > 5 && maxShipClass < ShipClassLevel.Kraken)
					{
						maxShipClass++;
						num = 0;
					}
					num++;
				}
				logr.Warn("Could not find item.  Returning to default method.");
				return true;
			}
			/*
			static bool Prefix(
			float minSpace, float maxSpace, int minPower, int maxPower, ref
			int effectType, ShipClassLevel maxShipClass, int faction,
			bool enableNoRarity, DropLevel maxDropLevel, int factionExtraChance, System.Random rand, ref Equipment __result)
		{
			/*logr.Log(
				$"GetRandomEquipment(" +
				$"minSpace={minSpace:0.##}, maxSpace={maxSpace:0.##}, " +
				$"minPower={minPower}, maxPower={maxPower}, " +
				$"effectType={effectType}, maxShipClass={maxShipClass}({(int)maxShipClass}), " +
				$"faction={faction}, enableNoRarity={enableNoRarity}, " +
				$"maxDropLevel={maxDropLevel}({(int)maxDropLevel}), factionExtraChance={factionExtraChance}, " +
				$"rand={(rand == null ? "null" : rand.GetHashCode().ToString())}" +
				$")");*
			if (effectType >= 0)
				return true;
			if(minPower < 50)
				return true;
			//EquipmentDB.ValidateDatabase();
			bool flag = GameData.data.gameMode == 1;
			int num = 0;
			if (minPower >= maxPower)
			{
				minPower = maxPower - 1;
			}
			if (maxSpace < 1f)
			{
				maxSpace = 1f;
			}
			List<Equipment> list = new List<Equipment>();
			var fi = AccessTools.Field(typeof(EquipmentDB), "equipments");
			var equipments = fi?.GetValue(null) as List<Equipment>;
			int type = rand.Next(0, EquipmentType.GetNames(typeof(EquipmentType)).Count());
			logr.Log($"Looking for type {(EquipmentType)type}");
							int count = 5;
							switch((EquipmentType)type)
							{
								case EquipmentType.Armor:
									count = 10;
									break;
								case EquipmentType.Battery:
									count = 3;
									break;
								case EquipmentType.Booster:
									count = 5;
									break;
								case EquipmentType.Computer:
									count = 6;
									break;
								case EquipmentType.Device:
									count = 10;
									break;
								case EquipmentType.Engine:
									count = 10;
									break;
								case EquipmentType.Generator:
									count = 10;
									break;
								case EquipmentType.Maneuverability:
									count = 10;
									break;
								case EquipmentType.Sensor:
									count = 10;
									break;
								case EquipmentType.Shield:
									count = 10;
									break;
								case EquipmentType.Utility:
									count = 10;
									break;
							}
							while (list.Count < 5)
							{
								logr.Log($"loop {num} Power: {minPower}-{maxPower} Space: {minSpace}-{maxSpace}");
								for (int i = 0; i < equipments.Count; i++)
								{
									Equipment equipment = equipments[i];
									if (equipment.space >= minSpace
										&& equipment.space <= maxSpace
										&& equipment.itemLevel + UnityEngine.Random.Range(-5,5) >= minPower
										&& equipment.itemLevel + UnityEngine.Random.Range(-5, 5) <= maxPower
										&& rand.Next(1, 101) <= equipment.lootChance
										&& equipment.dropLevel <= maxDropLevel
										&& (equipment.rarityMod > 0f || enableNoRarity)
										&& (int)equipment.type == type
										&& (!flag || equipment.spawnInArena)
										&& equipment.minShipClass <= maxShipClass
										&& (equipment.repReq.factionIndex == 0 || equipment.repReq.factionIndex == faction))
									{
										logr.Log($"Adding loot contender: {equipment.equipName}");
										list.Add(equipment);
										if (factionExtraChance > 0 && equipment.repReq.factionIndex > 0)
										{
											for (int j = 0; j < factionExtraChance; j++)
											{
												list.Add(equipment);
											}
										}
									}
								}
								num++;
								if (list.Count < 5)
								{
									minPower--;
									maxPower = (int)(maxPower + (1 + (int)maxDropLevel * (int)DropLevel.Boss));
									minSpace -= 1f;
									maxSpace += 1f;
									if (num > 5 && maxShipClass < ShipClassLevel.Kraken)
									{
										maxShipClass++;
										num = 0;
									}
									list.Clear();
								}
							}
							logr.Log($"Contenders: {list.Count}");	
							__result = list[rand.Next(0, list.Count)];
							return false;
						}*/
			static void Postfix(ref Equipment __result)
			{
				//logr.Log($"Found {__result.name}");
			}
		}
	}
}
