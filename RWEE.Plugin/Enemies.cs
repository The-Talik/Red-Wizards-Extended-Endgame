using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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

					//Main.log($"Applying AI bonus. lev: {aiChar.level} hp: {___baseHP} shield: {___baseShield} energy: {___baseEnergy} regen: {___hpRegen} regen shield-recharge: {___shieldRecharge} dam bonus: {___ss.dmgBonus}");
					//float mod = aiChar.level / 50;  //1x at L50, 2x at L100, 3x at L150, etc

					float mod = levelToMod(aiChar.level * (aiChar.AIType==4 || aiChar.AIType==3 ? 1.2f : 1f)); //bosses are harder, but give better loot.
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
					Main.log($"Applied AI Bonus {(1+mod)}x. lev: {aiChar.level} hp: {origBaseHP}→{___baseHP} shield: {origBaseShield}→{___baseShield}" +
						$"energy: {origBaseEnergy}→{___baseEnergy} regen: {origHpRegen}→{___hpRegen} shield-recharge: {origShieldRecharge}→{___shieldRecharge} dam bonus: origDamageBonus→{___ss.dmgBonus}");
					//this.baseHP;
					//this.ss.armor;
					//this.ss.armorMod
					//this.ss.dmgBonus
				}
			}
		}
		static public float levelToMod(float level)
		{
			float mod = (float)(Math.Pow(level - 49, 1.5) - 1) / 100;
			//Main.log($"level: {level} mod: {mod}");
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
					Main.log($"Rank: {___aiChars[i].rank}");
					___aiChars[i].DefineShipModel(new ShipType());
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
				//				Main.log($"Done generating AIControl level:{___Char.level} rank:{___Char.rank}");
				if (___Char.rank < 1)
					return;
				//___ss.loots
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
						if (UnityEngine.Random.Range(1, 101) < 10 || Items.debugUpgrades)
						{
							___ss.loots[i].rarity++;
							itemLog += "+";
						}
						else
							itemLog += ".";

						if(___Char.AIType == 4)
							tmpLev -= 5;
						else
							tmpLev -= 10;
					}
					if (___ss.loots[i].rarity > Main.MAX_RARITY || Items.debugUpgrades)
						___ss.loots[i].rarity = Main.MAX_RARITY;
					if (___Char.level < 100 && !Items.debugUpgrades)
						if (___ss.loots[i].rarity > 6)
							___ss.loots[i].rarity = 6;

					Main.log($"Loot: Char  [{___Char.Name()} L{___Char.level}] {___Char.AIType} itemType:{___ss.loots[i].itemType} itemID:{___ss.loots[i].itemID} rarity:{oldRarity}->{itemLog}->{___ss.loots[i].rarity} rarityEnabled:{___ss.loots[i].rarityEnabled}");
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
				//Main.log($"AIControl_SetNewTarget: {__instance.Char.name}");
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

				if(__instance.targetEntity == null)
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
			//Main.log($"hp: {hpPerc}");
			if (hpPerc < .2)
			{
				__instance.Char.currTactic = 0; //flee
				__instance.target = null;
				__instance.targetEntity = null;
				//Main.log("Fleeing");
				return;
			}
			if (__instance.targetEntity == null)
				return;
			if (__instance.Char.behavior.role == 0)
			{
				SpaceShip targetShip = __instance.targetEntity as SpaceShip;
				//Tactic: 0 == Alternate, 1 == Strafe and Fire, 2 == Hit and Run, 3 == Melee
				//Main.log($"Ship: [{__instance.Char.Name()} L{__instance.Char.level}] maxSpd:{getMaxSpeed(___ss)} acc:{getAcceleration(___ss)} mass:{___ss.stats.mass} turn:{getTurnSpeed(___ss)} tactic:curr:{__instance.Char.currTactic} fav:{__instance.Char.behavior.favTactic} max:{__instance.Char.maxTactic} warp{__instance.Char.behavior.emergencyWarpHPThreshold}");
				//Main.log($"Target: [{targetShip.name}] maxSpd:{getMaxSpeed(targetShip)} acc:{getAcceleration(targetShip)} mass:{targetShip.stats.mass} turn:{getTurnSpeed(targetShip)}");

				if (getAcceleration(___ss) > getAcceleration(targetShip) * 2 && getTurnSpeed(___ss) > getTurnSpeed(targetShip) * 2)
				{
					
					//we are more maneuverable than our target.
					//Main.log("We qualify for tactic upgrade");
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
			return ss.stats.turnSpeed * ss.crew.efficiency[1];
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
						//Main.log($"Updating Scan Distance [{aic.Char.Name()} {aic.Char.level} {aic.Char.AIType}] {orig}->{___scanDistance}");
						break;
				}
			}
		}
		[HarmonyPatch(typeof(AIMarauder), "SearchForEnemies")]
		static class AIMarauder_SearchForEnemies
		{
			static void Postfix(AICharacter ___Char, ref float ___attackDistance, ref float ___returnDistance)
			{

			}
		}
		/*		[HarmonyPatch(typeof(AIMercenary), "SearchForEnemies")]
				static class AIMercenary_SearchForEnemies
				{
					static void Postfix(AICharacter ___Char, ref float ___attackDistance, ref float ___returnDistance)
					{
						Main.error($"AIMercenary_SearchForEnemies {___Char.Name()} {___attackDistance} {___returnDistance}");
						___attackDistance = 250f * (1 + levelToMod(___Char.level));
						___returnDistance = 350f * (1 + levelToMod(___Char.level));
					}
				}*/
	}
}
