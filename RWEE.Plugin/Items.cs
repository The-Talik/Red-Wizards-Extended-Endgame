using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Playables;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
/**
 * Item Types:
 * 1 = weapons, 2 = equipment, 3 = item (goods), 4 == ship, 5 == crew member
 * 
 */
namespace RWEE
{
	static class UpgradeCtx
	{
		[ThreadStatic] static bool _has;
		[ThreadStatic] static EquipmentType _type;

		public static void Begin(EquipmentType t) { _type = t; _has = true; }
		public static bool TryGet(out EquipmentType t) { t = _type; return _has; }
		public static void Clear() { _has = false; _type = default; }
	}
	internal class Items
	{
		public static bool debugUpgrades = false;  //always upgrade items from high level bosses.  Always return mythic relics when scrapping.
																							 //List<Item> relics;
		static int[][] typeMap;



		[HarmonyPatch(typeof(EquipmentDB), "LoadDatabase")]
		static class EquipmentDB_LoadDatabase_addItems
		{
			[HarmonyPriority(Priority.VeryLow)]
			static void Postfix(ref List<Equipment> ___equipments)
			{

//				Main.error("Tmp IdRefMapJson: " + JsonConvert.SerializeObject(tmp, Formatting.None));

				//adjust tech levels to ensure end game items are dropped by endgame bosses.
				var item = ___equipments.FirstOrDefault(i => i != null && i.id == 81); //Superior Hull Reinforcement
				item.techLevel = 17;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 101); //Superior Hull Reinforcement
				item.techLevel = 30;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 75); //Lithrium
				item.techLevel = 20;
				item.sortPower = 25;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 100); //Energy
				item.techLevel = 25;
				item.sortPower = 20;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 86); //Speed booster iii
				item.techLevel = 21;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 151); //Pirate Heavy Booster
				item.techLevel = 21;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 150); //Pirate Booster
				item.techLevel = 15;
				item.sortPower = 24;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 113); //Nodolo's Combat Mod
				item.techLevel = 22;
				item = ___equipments.FirstOrDefault(i => i != null && i.id == 162); //PMC Collectors
				item.techLevel = 18;

				var template = ___equipments.FirstOrDefault(i => i != null && i.id == 151);
				if (template == null) { Main.warn("Template id 151 not found"); return; }

				int nextId = ___equipments.Max(i => i?.id ?? -1) + 1;
				//int nextId = 12000;
				// Runtime-safe deep-ish clone for ScriptableObjects
				var clone = UnityEngine.Object.Instantiate(template);

				// (Optional) ensure no shared mutable lists
				if (template.effects != null)
				{
					clone.effects = new List<Effect>(template.effects.Count);
					for (int i = 0; i < template.effects.Count; i++)
					{
						var e = template.effects[i];
						clone.effects.Add(new Effect { type = e.type, value = e.value, mod = e.mod });
					}
				}

				clone.id = nextId;
				clone.equipName = "Pirate Capital Booster";
				clone.refName = "rwee Pirate Capital Booster";  // keep refName unique/stable
				clone.minShipClass = ShipClassLevel.Dreadnought;
				clone.techLevel = 58;
				clone.space = 6f;
				clone.rarityMod = 1f;
				clone.energyCost = 32f;
				if (clone.effects != null && clone.effects.Count > 0)
					clone.effects[0].value = 92f;

				___equipments.Add(clone);


				(AccessTools.Method(typeof(EquipmentDB), "RebuildDictionaries")
				 ?? AccessTools.Method(typeof(EquipmentDB), "BuildDictionaries")
				 ?? AccessTools.Method(typeof(EquipmentDB), "Refresh"))?.Invoke(null, null);
			}
		}

		/**
		 * Adds Mystic Relics
		 */
		[HarmonyPatch(typeof(ItemDB), "LoadDatabase")]
		static class ItemDB_LoadDatabase_addItems
		{
			// do adds late so other mods finish first
			[HarmonyPriority(Priority.VeryLow)]
			static void Postfix(ref List<Item> ___items) // if it's an array in your build, use: ref Item[] ___items
			{
				if (___items == null) return;


				// find Ancient Relic (id 24) as template
				var ancient = ___items.FirstOrDefault(i => i != null && i.id == 24);
				//ancient.canUpgradeToTier += 2;  //for testing
				ancient.rarity = 4; //purple
				if (ancient == null) { Main.log("[LegendaryCatalyst] Ancient Relic not found"); return; }

				int nextId = ___items.Max(i => i?.id ?? -1) + 1;
				//int nextId = 12000;
				foreach (EquipmentType et in Enum.GetValues(typeof(EquipmentType)))
				{
					// optional skips:
					// if (et == EquipmentType.Generator || et == EquipmentType.Battery) continue;

					string label = et.ToString();                       // e.g., "Armor"
					

					//Tier 5
					var it = MakeRelicVariant(ancient, nextId++, 5,
						$"Legendary {label} Catalyst",
						"rwee_mystic_relic_" + label.ToLowerInvariant(),
						label, "legendary_catalyst.png",
						$"A low, steady hum radiates from within. As you hold it, old scars seem lighter and familiar tools feel newly made, as if the Catalyst is reminding matter what it was always capable of becoming.");
					___items.Add(it);

					//Tier 6
					it = MakeRelicVariant(ancient, nextId++, 6,
						$"Mythic {label} Relic",
						"rwee_arcane_orb_" + label.ToLowerInvariant(),
						label, "mythic_relic.png",
						$"The Relic does not shine so much as it refuses darkness. Touching it is like reading a memory written in thunder — names you’ve never learned settle on your tongue as if they were yours.");
					___items.Add(it);
				}

				RebuildItemIdDictFromList(___items);

			}
			static Item MakeRelicVariant(Item template, int newId, int tier, string itemName, string refName, string targetTag, string imageName, string description)
			{
				var it = ScriptableObject.CreateInstance<Item>();
				// copy selected fields from template
				it.expansion = template.expansion;
				it.rarity = tier;
				it.levelPlus = template.levelPlus;
				it.weight = template.weight;
				it.basePrice = Mathf.Max(template.basePrice * 1.6f * (tier - 4), 100000f);
				it.priceVariation = template.priceVariation;
				it.tradeChance = template.tradeChance != null ? (int[])template.tradeChance.Clone() : new int[7];
				it.tradeQuantity = template.tradeQuantity;
				it.type = template.type;
				it.askedInQuests = false;
				it.canBeStashed = template.canBeStashed;
				it.canBeTraded = false;
				it.randomDrop = false;
				//it.canUpgradeToTier = template.canUpgradeToTier+1;  //Can't use standard upgarding since we require type matching as well, which the item search method does not know
				it.geologyRequired = -1;
				it.craftable = false;
				it.craftingLevelAffectsYield = false;
				it.craftingMaterials = null;
				it.craftingYield = 0;
				it.defaultFabricatorID = -1;
				it.productionMaterials = null;
				it.productionYield = 0;
				it.teachItemBlueprints = null;

				// identity/text
				it.id = newId;
				it.refName = refName;
				it.itemName = itemName;
				it.description = description;


				string png = Path.Combine(Paths.PluginPath, "RedWizardsExtendedEndgame.plugin", "Assets", imageName);
				float ppu = it.sprite != null ? it.sprite.pixelsPerUnit : 100f;

				var spr = IconLoader.LoadSpriteFromPng(png, ppu);
				if (spr != null)
				{
					it.sprite = spr;
					Main.log($"[Icons] Set {it.refName} -> {spr.name} ({spr.rect.width}x{spr.rect.height})");
				}
				else
				{
					it.sprite = template.sprite;
					Main.log($"[Icons] Missing icon PNG: {png}");
				}
				Main.log($"[Relics] Added #{it.id} {it.itemName} {it.refName}");
				return it;
			}

			// Rebuild any private static IDictionary<int,Item> the DB keeps
			static void RebuildItemIdDictFromList(IList<Item> items)
			{
				var t = typeof(ItemDB);
				foreach (var f in AccessTools.GetDeclaredFields(t))
				{
					if (!f.IsStatic) continue;
					var ft = f.FieldType;
					if (!typeof(IDictionary).IsAssignableFrom(ft)) continue;
					if (!ft.IsGenericType) continue;
					var ga = ft.GetGenericArguments();
					if (ga.Length != 2 || ga[0] != typeof(int) || !typeof(Item).IsAssignableFrom(ga[1])) continue;

					var dict = f.GetValue(null) as IDictionary;
					if (dict == null)
					{
						// allocate an instance of the exact dict type (e.g., Dictionary<int,Item>)
						dict = Activator.CreateInstance(ft) as IDictionary;
						if (dict == null) continue;
						f.SetValue(null, dict);
					}

					dict.Clear();
					for (int i = 0; i < items.Count; i++)
					{
						var it = items[i];
						if (it != null) dict[it.id] = it;
					}
					Main.log($"[MysticRelic] Rebuilt item id dictionary: {f.Name}");
				}

				// try a DB-provided rebuild method if it exists
				(AccessTools.Method(typeof(ItemDB), "RebuildDictionaries")
					?? AccessTools.Method(typeof(ItemDB), "BuildDictionaries")
					?? AccessTools.Method(typeof(ItemDB), "Refresh"))?.Invoke(null, null);
			}
		}
		/**
	 * Scrap legendary items
	 */
		[HarmonyPatch(typeof(Inventory), "ScrapItem")]
		static class Inventory_ScrapItem
		{
			[HarmonyPriority(Priority.VeryLow)]
			static void Prefix(int ___selectedItem)
			{
				CargoSystem cs = PlayerControl.inst.GetCargoSystem;

				if (___selectedItem == -1 || cs.cargo[___selectedItem].itemType > 2)  //only equipment for now
					return;

				int type = cs.cargo[___selectedItem].itemType;
				int rarity = cs.cargo[___selectedItem].rarity;
				int id = cs.cargo[___selectedItem].itemID;
				if (rarity >= 5)
				{
					switch (type)
					{
						case 1:
							return;
						case 2:
							Equipment equipment = EquipmentDB.GetEquipment(id);
							string label = equipment.type.ToString();

							int rand = RweeRand.Range(0, 100, label.ToLowerInvariant()+"_mystic_relic_attempts");
							Main.log("random number:" + rand);
							if (rand < 10 || Items.debugUpgrades)
							{
								
								Item item = null;
								switch (rarity)
								{
									case 5:
										item = GetItemByRefName("rwee_mystic_relic_" + label.ToLowerInvariant());
										break;
									case 6:
									case 7:
										item = GetItemByRefName("rwee_arcane_orb_" + label.ToLowerInvariant());
										break;
								}
								if (item != null)
								{
									cs.StoreItem(3, item.id, 5, 1, 0f, -1, -1);
									SoundSys.PlaySound(16, true);
									SideInfo.AddMsg(Lang.Get(6, 18, ItemDB.GetItemNameModified(item.id, 2)));
								}
							}
							break;
					}
				}
			}
		}
		public static Item GetItemByRefName(string refName)
		{
			List<Item> items = ItemDB.GetItems(false);
			Item item = items.Find((Item i) => i.refName == refName);
			if (item != null)
			{
				Main.log($"found item {item.itemName}");
				return item;
			}
			if (InfoPanelControl.inst != null)
			{
				InfoPanelControl.inst.AddWarningToBuffer("ERROR: No <b>item</b> found for refName <b>" + refName + "</b>. Returning a crystal instead.");
				Debug.Log("ERROR on GetItemByRefName(string refName) for refName " + refName);
			}
			return items[4];
		}

		/**
		* 
		*/
		/*[HarmonyPatch(typeof(Inventory), "TierUpgradePossible")]
		static class Inventory_TierUpgradePossible
		{
			static void Postfix(int ___selectedItem, ref Station ___currStation, ref int __result)
			{
				Main.log($"TierUpgradePossible postfix {__result}");
				if (__result > 0)
					return;
				CargoSystem cs = PlayerControl.inst.GetCargoSystem;


				if (___selectedItem == -1 || ___selectedItem >= cs.cargo.Count)
					return;
				if (cs.cargo[___selectedItem].itemType > 2)
					return;

				int desiredTier = cs.cargo[___selectedItem].rarity + 1;
				Main.log($"Desired Tier: {desiredTier}");
				if (desiredTier == 5)
				{
					Equipment item = EquipmentDB.GetEquipment(cs.cargo[___selectedItem].itemID);
					string label = item.type.ToString();
					__result = GetUpgradeItemForTier(desiredTier, label, (___currStation != null) ? ___currStation.id : -1, (___currStation != null));

					//Item upgradeItem = ItemDB.GetItem(__result);
					//Main.log($"upgrade item: {upgradeItem.refName} {"mystic_relic_" + label.ToLowerInvariant()}");
				}
				//return CargoSystem.GetUpgradeItemForTier(desiredTier, this.inStation ? this.currStation.id : -1, this.inStation);
			}
		}*/
		[HarmonyPatch(typeof(Inventory))]
		static class Inventory_Upgrade_Context
		{
			// Apply same Prefix/Postfix to both A and B
			static IEnumerable<MethodBase> TargetMethods()
			{
				yield return AccessTools.Method(typeof(Inventory), "TierUpgradePossible");
				yield return AccessTools.Method(typeof(Inventory), "UpgradeItem");
			}
			static void Prefix(int ___selectedItem)
			{
				CargoSystem cs = PlayerControl.inst.GetCargoSystem;
				if (___selectedItem == -1 || ___selectedItem >= cs.cargo.Count)
					return;
				if (cs.cargo[___selectedItem].itemType > 2)
					return;
				Equipment item = EquipmentDB.GetEquipment(cs.cargo[___selectedItem].itemID);
				if (item != null)
					UpgradeCtx.Begin(item.type);
			}
			static void Postfix()
			{
				UpgradeCtx.Clear();
			}
		}
		[HarmonyPatch(typeof(ShipInfo))]
		static class ShipInfo_Upgrade_Context
		{
			// Apply same Prefix/Postfix to both A and B
			static IEnumerable<MethodBase> TargetMethods()
			{
				yield return AccessTools.Method(typeof(ShipInfo), "TierUpgradePossible");
				yield return AccessTools.Method(typeof(ShipInfo), "UpgradeItem");
			}
			static void Prefix(int ___selSlotIndex, SpaceShip ___ss, bool ___selectedIsBuiltInEquipment, int ___selItemType, int ___selItemIndex)
			{
				if (___selSlotIndex < 0)
					return;
				GenericCargoItem ci = GetSelectedItemAsGenericCargoItem(___ss, ___selectedIsBuiltInEquipment, ___selItemType, ___selItemIndex);

				if (ci == null)
					return;
				if (ci.itemType == 2)
				{
					Equipment item = EquipmentDB.GetEquipment(ci.itemID);
					if (item != null)
						UpgradeCtx.Begin(item.type);
				}
			}
			private static GenericCargoItem GetSelectedItemAsGenericCargoItem(SpaceShip ___ss, bool ___selectedIsBuiltInEquipment, int ___selItemType, int ___selItemIndex)
			{
				if (___ss != null && ___ss.shipData != null)
				{
					if (___selectedIsBuiltInEquipment)
					{
						BuiltInEquipmentData builtInEquipmentData = ___ss.shipData.GetBuiltInEquipmentData(___selItemIndex);
						return new GenericCargoItem(2, builtInEquipmentData.equipmentID, builtInEquipmentData.rarity, null, null, null, null);
					}
					if (___selItemType == 1 && ___selItemIndex < ___ss.shipData.weapons.Count)
					{
						EquipedWeapon equipedWeapon = ___ss.shipData.weapons[___selItemIndex];
						return new GenericCargoItem(1, equipedWeapon.weaponIndex, equipedWeapon.rarity, null, null, null, null);
					}
					if (___selItemType == 2 && ___selItemIndex < ___ss.shipData.equipments.Count)
					{
						InstalledEquipment installedEquipment = ___ss.shipData.equipments[___selItemIndex];
						return new GenericCargoItem(2, installedEquipment.equipmentID, installedEquipment.rarity, null, null, null, null);
					}
				}
				return null;
			}
			static void Postfix()
			{
				UpgradeCtx.Clear();
			}
		}
		[HarmonyPatch(typeof(CargoSystem), "GetUpgradeItemForTier")]
		static class CargoSystem_GetUpgradeItemForTier
		{
			static void Postfix(int desiredTier, int stationID, bool inStation, ref int __result)
			{
				if (!UpgradeCtx.TryGet(out var eqType))
					return;
				if (__result > 0)
					return;
				string type = eqType.ToString();//read value
																				//Main.log($"getting upgrade item for tier {desiredTier}");
				CargoSystem cs = PlayerControl.inst.GetCargoSystem;
				for (int i = 0; i < cs.cargo.Count; i++)
				{
					CargoItem cargoItem = cs.cargo[i];
					if (cargoItem.itemType == 3 && ((inStation && (cargoItem.stockStationID == stationID || cargoItem.stockStationID <= -2)) || cargoItem.stockStationID == -1))
					{
						switch (desiredTier)
						{
							case 5:
								if (ItemDB.GetItem(cargoItem.itemID).refName == "rwee_mystic_relic_" + type.ToLowerInvariant())
								{
									//Main.log($"found item {ItemDB.GetItem(cargoItem.itemID).refName} {i}");
									__result = i;
									return;
								}
								break;
							case 6:
								if (ItemDB.GetItem(cargoItem.itemID).refName == "rwee_arcane_orb_" + type.ToLowerInvariant())
								{
									//Main.log($"found item {ItemDB.GetItem(cargoItem.itemID).refName} {i}");
									__result = i;
									return;
								}
								break;
						}
					}
				}
				//Main.log($"Did not find item.");
			}
		}


		/**
 * 
 */
		/*[HarmonyPatch(typeof(LootSystem), "GenerateLootItem", new System.Type[] {
		typeof(int), typeof(int), typeof(bool), typeof(int),
		typeof(DropLevel), typeof(int), typeof(System.Random)
})]
		static class LootSystem_GenerateLootItem
		{
			static void Postfix(int power, int initialRarityBooster, bool enableNoRarity, int itemFaction, DropLevel maxDropLevel, int factionExtraChance, System.Random rand)
			{
				Main.log(
	$"[RWEE] GenerateLootItem args: power={power}, initialRarityBooster={initialRarityBooster}, " +
	$"enableNoRarity={enableNoRarity}, itemFaction={itemFaction}, " +
	$"maxDropLevel={(int)maxDropLevel}({maxDropLevel}), factionExtraChance={factionExtraChance}, " +
	$"rand={(rand != null ? rand.GetHashCode().ToString("X8") : "null")}"
);
			}
		}*/
		[HarmonyPatch(typeof(ItemDB), "GetRarityMod", new System.Type[] { typeof(int), typeof(float), typeof(float) })]
		static class ItemDB_GetRarityMod
		{
			static bool Prefix(int rarity, float rarityMod, float effectMod, ref float __result)
			{
				switch (rarity)
				{
					case 6:
						__result = 1f + 2.3f * rarityMod * effectMod;
						return false;
					case 7:
						__result = 1f + 3.1f * rarityMod * effectMod;
						return false;
				}
				return true;
			}
		}
		[HarmonyPatch(typeof(TWeapon), "Dmg")]
		static class TWeapon_Dmg
		{
			static void Postfix(int rarity, ref float __result)
			{
				if (rarity >= 5)
					__result *= 1.5f;
			}
		}

		[HarmonyPatch(typeof(ItemDB), "GetRarityColor", new System.Type[] { typeof(int), typeof(bool) })]
		static class ItemDB_GetRarityColor
		{
			static bool Prefix(int rarity, bool allowColorblindMode, ref string __result)
			{

				if (rarity <= 5)
					return true;
				string text = ColorSys.rarity1;
				switch (rarity)
				{
					case 6:
						__result = "<color=#e6f542>";
						break;
					case 7:
						__result = "<color=#d11c13>";
						break;
				}
				if (allowColorblindMode)
				{
					text = GameOptions.GetColorblindTier(rarity) + text;
				}
				return false;
			}
		}
		[HarmonyPatch(typeof(GameDataInfo), "GetRandomWeapon")]
		static class GameDataInfo_GetRandomWeapon
		{
			static void Postfix(GameDataInfo __instance,
				float maxSpace, int minPower, int maxPower, WeaponType ignoreType, DropLevel maxDropLevel, int faction, int factionExtraChance, System.Random rand,
				ref TWeapon __result)
			{
				int origMinPower = minPower;
				//Main.log($"found {__result.name}");
				if (__result.index != 0)
					return;
				if (__result.index == 0 && __result.itemLevel >= minPower)
					return;
				minPower -= 10;
				Main.log($"↪ Fixing ERROR. Increasing power range to {origMinPower}->{minPower} to {maxPower} and searching again.");
				
				__result = GameData.data.GetRandomWeapon(maxSpace, minPower, maxPower, ignoreType, maxDropLevel, faction, factionExtraChance, rand);
			}
		}

		/*
		internal static class UniformEffectPicker
		{
			static bool _built;
			static int[][] _effectBuckets;          // index = (int)EquipmentType -> int[] of effectType ids
			static EquipmentType[] _nonEmptyTypes;  // only types that have at least one effect

			public static void BuildOnce()
			{
				if (_built) return;

				var fi =
					AccessTools.Field(typeof(EquipmentDB), "equipments") ??
					AccessTools.Field(typeof(EquipmentDB), "equipment") ??
					AccessTools.Field(typeof(EquipmentDB), "list") ??
					AccessTools.Field(typeof(EquipmentDB), "items");

				var list = fi?.GetValue(null) as List<Equipment>;
				if (list == null) { _built = true; return; }

				int typeCount = Enum.GetValues(typeof(EquipmentType)).Length;
				var tmp = new List<int>[typeCount];
				for (int i = 0; i < typeCount; i++) tmp[i] = new List<int>(8);

				foreach (var eq in list)
				{
					if (eq?.effects == null || eq.effects.Count == 0) continue;
					int t = (int)eq.type;
					var bucket = tmp[t];
					for (int i = 0; i < eq.effects.Count; i++)
					{
						int eff = eq.effects[i].type;
						// de-dupe without HashSet; lists are small so Contains() is fine
						if (!bucket.Contains(eff)) bucket.Add(eff);
					}
				}

				var typeList = new List<EquipmentType>();
				_effectBuckets = new int[typeCount][];
				for (int i = 0; i < typeCount; i++)
				{
					if (tmp[i].Count > 0)
					{
						_effectBuckets[i] = tmp[i].ToArray();
						typeList.Add((EquipmentType)i);
					}
					else
					{
						_effectBuckets[i] = Array.Empty<int>();
					}
				}
				_nonEmptyTypes = typeList.ToArray();
				_built = true;
			}

			public static bool TryPick(System.Random rand, out int effectType)
			{
				effectType = -1;
				if (!_built || _nonEmptyTypes == null || _nonEmptyTypes.Length == 0) return false;

				int tIdx = (rand != null) ? rand.Next(_nonEmptyTypes.Length) : UnityEngine.Random.Range(0, _nonEmptyTypes.Length);
				int typeIndex = (int)_nonEmptyTypes[tIdx];
				var bucket = _effectBuckets[typeIndex];
				if (bucket == null || bucket.Length == 0) return false;

				int eIdx = (rand != null) ? rand.Next(bucket.Length) : UnityEngine.Random.Range(0, bucket.Length);
				effectType = bucket[eIdx];
				return true;
			}
		}*/
	}

	[HarmonyPatch(typeof(CargoItem), MethodType.Constructor, new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) })]
	static class CargoItem_CargoItem
	{
		static void Postfix(int ___itemID)
		{
			Main.log($"Loaded CargoItem: {___itemID}");
		}
	}
}

