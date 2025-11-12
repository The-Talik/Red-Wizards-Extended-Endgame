using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWEE
{
	internal class DataDumps
	{
		/**
* Dump items
*/
		//[HarmonyPatch(typeof(ItemDB), "LoadDatabase")]
		static class ItemDB_LoadDatabase
		{
			// lowest so other LoadDatabase patches finish first
			[HarmonyPriority(Priority.Last)]
			static void Postfix(List<Item> ___items)
			{
				try
				{
					var items = ___items;
					if (items == null || items.Count == 0)
					{
						Main.log("[Items] ItemDB.items is null/empty.");
						return;
					}

					Main.log($"[Items] Dumping {items.Count} items…");

					for (int i = 0; i < items.Count; i++)
					{
						var it = items[i];
						if (it == null) continue;

						Main.log(FormatItem(it));
					}

					Main.log("[Items] Done.");
				}
				catch (Exception ex)
				{
					Main.log("[Items] Dump failed: " + ex);
				}
			}

			static string FormatItem(Item it)
			{
				// name fallbacks (some builds leave itemName empty until later)
				string name =
					!string.IsNullOrEmpty(it.itemName) ? it.itemName :
					(!string.IsNullOrEmpty(it.refName) ? it.refName :
					it.GetNameModified(0));

				// flags summary
				var flags = new List<string>();
				if (it.IsAmmo) flags.Add("Ammo");
				if (it.IsFabricated) flags.Add($"Fab(ID={it.defaultFabricatorID},Y={it.productionYield})");
				if (it.CanBeMined) flags.Add($"Mine(geo={it.geologyRequired})");
				if (it.IsBlueprint) flags.Add("Blueprint");
				if (it.IsContainer) flags.Add("Container");
				if (it.IsBasicOre) flags.Add("BasicOre");
				if (!it.canBeTraded) flags.Add("NoTrade");
				if (!it.randomDrop) flags.Add("NoRandomDrop");
				if (it.craftable) flags.Add($"Craft(yield={it.craftingYield})");

				// lists
				string tradeChance = it.tradeChance != null && it.tradeChance.Length > 0
					? string.Join(",", it.tradeChance)
					: "-";

				string craftMats = (it.craftingMaterials != null && it.craftingMaterials.Count > 0)
					? string.Join(", ", it.craftingMaterials.Select(cm => $"{cm.itemID}x{cm.quantity}"))
					: "-";

				string prodMats = (it.productionMaterials != null && it.productionMaterials.Count > 0)
					? string.Join(", ", it.productionMaterials.Select(pm => $"{pm.itemID}x{pm.quantity}"))
					: "-";

				// line
				return
					$"[#{it.id}] {name}  " +
					$"type={it.type} rare={it.rarity} " +
					$"lvl={it.itemLevel} (lvlPlus={it.levelPlus}) " +
					$"price={it.basePrice:0}±{it.priceVariation:0.##} " +
					$"weight={it.weight:0.###} " +
					$"tradeQty={it.TradeQuantity} " +
					$"prodLvlReq={it.ProductionLevelRequired} " +
					$"flags=[{string.Join("|", flags)}] " +
					$"tradeChance=[{tradeChance}] " +
					$"craftMats=[{craftMats}] " +
					$"prodMats=[{prodMats}]";
			}
		}
		/**
		 * Equipment list dump
		 */
		//[HarmonyPatch(typeof(EquipmentDB), "LoadDatabase")]
		static class EquipmentDB_LoadDatabase_Dump_Patch
		{
			[HarmonyPriority(Priority.Last)]
			static void Postfix()
			{
				try
				{
					var list = GetEquipmentList();
					if (list == null || list.Count == 0)
					{
						Main.log("[Equip] Equipment list is null/empty.");
						return;
					}

					Main.log($"[Equip] Dumping {list.Count} equipment…");
					for (int i = 0; i < list.Count; i++)
					{
						var eq = list[i];
						if (eq == null) continue;
						Main.log(FormatEquipment(eq));
					}
					Main.log("[Equip] Done.");
				}
				catch (Exception ex)
				{
					Main.log("[Equip] Dump failed: " + ex);
				}
			}

			static List<Equipment> GetEquipmentList()
			{
				// Try common field names on EquipmentDB
				var fi =
					AccessTools.Field(typeof(EquipmentDB), "equipments") ??
					AccessTools.Field(typeof(EquipmentDB), "equipment") ??
					AccessTools.Field(typeof(EquipmentDB), "list") ??
					AccessTools.Field(typeof(EquipmentDB), "items");

				return fi?.GetValue(null) as List<Equipment>;
			}

			static string FormatEquipment(Equipment e)
			{
				// name fallback chain
				string name =
					!string.IsNullOrEmpty(e.equipName) ? e.equipName :
					(!string.IsNullOrEmpty(e.refName) ? e.refName :
					e.GetNameModified(1, 0)); // rarity=1, plain text

				// effects summary (type:value[;mod])
				string effects = (e.effects != null && e.effects.Count > 0)
					? string.Join(", ", e.effects.Select(x =>
					{
						// Effect has fields: type, value, mod (seen in game code)
						string s = $"{x.type}:{x.value:0.##}";
						try { s += (x.mod != 0f ? $";m={x.mod:0.##}" : ""); } catch { }
						return s;
					}))
					: "-";

				// rep requirement
				string rep = (e.repReq.factionIndex != 0 || e.repReq.repNeeded != 0)
					? $"rep(f={e.repReq.factionIndex},need={e.repReq.repNeeded})"
					: "-";

				// price preview (rarity 1)
				float price = e.Price(1);

				return
					$"[#{e.id}] {name}  " +
					$"type={e.type} class>={e.minShipClass} " +
					$"lvl={e.itemLevel} (tech={e.techLevel},sort={e.sortPower}) " +
					$"space={e.space:0.##} massFlat={e.massFlat:0.##} massMod={e.massChange:0.##} " +
					$"energy={e.energyCost:0.##}{(e.energyCostPerShipClass ? "*2^(class-1)" : "")} " +
					$"rarityMod={e.rarityMod:0.##} drop={e.dropLevel} loot%={e.lootChance} sell%={e.sellChance} " +
					$"flags=[{FlagStr(e)}] reqItem={(e.requiredItemID >= 0 ? $"{e.requiredItemID}x{e.requiredQnt}" : "-")} " +
					$"price~={price:0} " +
					$"effects=[{effects}]";
			}

			static string FlagStr(Equipment e)
			{
				var bits = new List<string>();
				if (e.activated) bits.Add("Active");
				if (e.uniqueReplacement) bits.Add("UniqueReplace");
				if (e.permanent) bits.Add("Perm");
				if (e.spawnInArena) bits.Add("Arena");
				if (!e.enableChangeKey) bits.Add("NoRebind");
				return bits.Count > 0 ? string.Join("|", bits) : "-";
			}
		}
	}
}
