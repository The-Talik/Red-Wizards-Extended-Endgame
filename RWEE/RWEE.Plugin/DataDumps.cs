using HarmonyLib;
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace RWEE
{
	internal class DataDumps
	{
		const bool show = false;

		// CSV filenames
		static readonly string ItemsCsv = "Items.csv";
		static readonly string EquipCsv = "Equipment.csv";
		static readonly string WeaponsCsv = "Weapons.csv";

		static string GetOutputFolder()
		{
			try
			{
				var asm = Assembly.GetExecutingAssembly();
				var dir = Path.GetDirectoryName(asm.Location) ?? Environment.CurrentDirectory;
				var outDir = Path.Combine(dir, "../../RWEE_DataDumps");
				if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
				return outDir;
			}
			catch
			{
				return Environment.CurrentDirectory;
			}
		}

		static void EnsureCsvHeader(string fileName, string headerLine)
		{
			try
			{
				var path = Path.Combine(GetOutputFolder(), fileName);
				if (!File.Exists(path))
				{
					File.WriteAllText(path, headerLine + Environment.NewLine, Encoding.UTF8);
				}
			}
			catch (Exception ex)
			{
				Main.error($"[CSV] Failed to ensure header for {fileName}: {ex}");
			}
		}

		static void AppendCsvRow(string fileName, params string[] columns)
		{
			try
			{
				var path = Path.Combine(GetOutputFolder(), fileName);
				var line = string.Join(",", columns.Select(CsvEscape));
				File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
			}
			catch (Exception ex)
			{
				Main.error($"[CSV] Failed to append row to {fileName}: {ex}");
			}
		}

		static string CsvEscape(string s)
		{
			if (s == null) return "";
			// Escape quotes by doubling them and wrap whole field in quotes if it contains comma, quote or newline
			var needsQuotes = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
			var escaped = s.Replace("\"", "\"\"");
			return needsQuotes ? $"\"{escaped}\"" : escaped;
		}

		// =========================
		// Item dump
		// =========================
		[HarmonyPatch(typeof(ItemDB), "LoadDatabase")]
		static class ItemDB_LoadDatabase_dump
		{
			// lowest so other LoadDatabase patches finish first
			[HarmonyPriority(Priority.Last)]
			static void Postfix(List<Item> ___items)
			{
				if (!show)
					return;
				try
				{
					var items = ___items;
					if (items == null || items.Count == 0)
					{
						Main.log("[Items] ItemDB.items is null/empty.");
						return;
					}

					Main.log($"[Items] Dumping {items.Count} items…");

					// ensure CSV header
					EnsureCsvHeader(ItemsCsv, "id,name,refName,type,rarity,itemLevel,levelPlus,basePrice,priceVariation,weight,tradeQty,prodLvlReq,flags,tradeChance,craftMats,prodMats");

					for (int i = 0; i < items.Count; i++)
					{
						var it = items[i];
						if (it == null) continue;

						Main.log(FormatItem(it));

						// write CSV row
						try
						{
							var name =
								!string.IsNullOrEmpty(it.itemName) ? it.itemName :
								(!string.IsNullOrEmpty(it.refName) ? it.refName :
								it.GetNameModified(0));

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

							string tradeChance = it.tradeChance != null && it.tradeChance.Length > 0
								? string.Join("|", it.tradeChance)
								: "-";

							string craftMats = (it.craftingMaterials != null && it.craftingMaterials.Count > 0)
								? string.Join("|", it.craftingMaterials.Select(cm => $"{cm.itemID}x{cm.quantity}"))
								: "-";

							string prodMats = (it.productionMaterials != null && it.productionMaterials.Count > 0)
								? string.Join("|", it.productionMaterials.Select(pm => $"{pm.itemID}x{pm.quantity}"))
								: "-";

							AppendCsvRow(ItemsCsv,
								it.id.ToString(),
								name,
								it.refName ?? "",
								it.type.ToString(),
								it.rarity.ToString(),
								it.itemLevel.ToString(),
								it.levelPlus.ToString(),
								it.basePrice.ToString("0.##"),
								it.priceVariation.ToString("0.##"),
								it.weight.ToString("0.###"),
								it.TradeQuantity.ToString(),
								it.ProductionLevelRequired.ToString(),
								string.Join("|", flags),
								tradeChance,
								craftMats,
								prodMats
							);
						}
						catch (Exception ex)
						{
							Main.error("[CSV Items] Failed to write item row: " + ex);
						}
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
					$"[#{it.id}] {name} [{it.refName}]  " +
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




		// =========================
		// Equipment dump
		// =========================
		[HarmonyPatch(typeof(EquipmentDB), "LoadDatabase")]
		public static class EquipmentDB_LoadDatabase_Dump_Patch
		{
			[HarmonyPriority(Priority.Last)]
			static void Postfix()
			{
				if (!show)
					return;
				try
				{
					var list = GetEquipmentList();
					if (list == null || list.Count == 0)
					{
						Main.log("[Equip] Equipment list is null/empty.");
						return;
					}

					Main.log($"[Equip] Dumping {list.Count} equipment…");

					// ensure CSV header
					EnsureCsvHeader(EquipCsv, "id,name,refName,type,primaryEffect,minShipClass,itemLevel,techLevel,sortPower,space,massFlat,massChange,energyCost,energyCostPerShipClass,rarityMod,dropLevel,lootChance,sellChance,flags,reqItem,effects");

					for (int i = 0; i < list.Count; i++)
					{
						var eq = list[i];
						if (eq == null) continue;
						Main.log(FormatEquipment(eq));

						// write CSV row
						try
						{
							string effects = (eq.effects != null && eq.effects.Count > 0)
								? string.Join("|", eq.effects.Select(x =>
								{
									string s = $"{x.type}:{x.value:0.##}";
									try { s += (x.mod != 0f ? $";m={x.mod:0.##}" : ""); } catch { }
									return s;
								}))
								: "-";

							string reqItem = (eq.requiredItemID >= 0 ? $"{eq.requiredItemID}x{eq.requiredQnt}" : "-");

							AppendCsvRow(EquipCsv,
								eq.id.ToString(),
								!string.IsNullOrEmpty(eq.equipName) ? eq.equipName : (eq.refName ?? ""),
								eq.refName ?? "",
								eq.type.ToString(),
								(eq.effects != null && eq.effects.Count > 0) ? eq.effects[0].type.ToString() : "-",
								eq.minShipClass.ToString(),
								eq.itemLevel.ToString(),
								eq.techLevel.ToString(),
								eq.sortPower.ToString(),
								eq.space.ToString("0.##"),
								eq.massFlat.ToString("0.##"),
								eq.massChange.ToString("0.##"),
								eq.energyCost.ToString("0.##"),
								(eq.energyCostPerShipClass ? "1" : "0"),
								eq.rarityMod.ToString("0.##"),
								eq.dropLevel.ToString(),
								eq.lootChance.ToString("0.###"),
								eq.sellChance.ToString("0.###"),
								FlagStr(eq),
								reqItem,
								effects
							);
						}
						catch (Exception ex)
						{
							Main.error("[CSV Equip] Failed to write equipment row: " + ex);
						}
					}
					Main.log("[Equip] Done.");
				}
				catch (Exception ex)
				{
					Main.log("[Equip] Dump failed: " + ex);
				}
			}

			public static List<Equipment> GetEquipmentList()
			{
				// Try common field names on EquipmentDB
				var fi =
					AccessTools.Field(typeof(EquipmentDB), "equipments");

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
					$"[#{e.id}] {name} " +
					$"[{e.refName}] " +
					$"type={e.type} {e.effects[0].type} class>={e.minShipClass} " +
					$"lvl={e.itemLevel} (tech={e.techLevel},sort={e.sortPower}) " +
					$"space={e.space:0.##} massFlat={e.massFlat:0.##} massMod={e.massChange:0.##} " +
					$"energy={e.energyCost:0.##}{(e.energyCostPerShipClass ? "*2^(class-1)" : "")} " +
					$"rarityMod={e.rarityMod:0.##} drop={e.dropLevel} loot%={e.lootChance} sell%={e.sellChance} " +
					$"flags=[{FlagStr(e)}] reqItem={(e.requiredItemID >= 0 ? $"{e.requiredItemID}x{e.requiredQnt}" : "-")} " +
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
		// =========================
		// Weapons dump (after predefs)
		// =========================
		[HarmonyPatch(typeof(GameData), "GetPredefinedData")]
		static class GameData_GetPredefinedData_DumpWeapons
		{
			[HarmonyPriority(Priority.Last)]
			static void Postfix()
			{
				if (!show) return;
				try
				{
					var (list, src) = ResolveWeapons();
					if (list == null || list.Count == 0)
					{
						Main.log("[Weapons] List is null/empty.");
						return;
					}

					Main.log($"[Weapons] Dumping {list.Count} weapons (source='{src}')…");

					// ensure CSV header
					EnsureCsvHeader(WeaponsCsv, "index,name,type,compType,damageType,space,damage,rateOfFire,burst,range,speed,aoe,proximityDmgMod,fluxDamageMod,heatDamageMod,techLevel,dropLevel,itemLevel,energyShot,energyPerSec,heatShotRaw,heatPerSec,power,flags");

					for (int i = 0; i < list.Count; i++)
					{
						var w = list[i];
						if (w == null) continue;
						Main.log(FormatWeapon(w));

						// write CSV row
						try
						{
							// name fallback
							string name = string.IsNullOrEmpty(w.name) ? $"Weapon#{w.index}" : w.name;

							// energy/heat per-shot (raw; no player skills)
							float energyShot = w.energyCost;
							float heatShotRaw = w.heatGenMod * (w.damage * 0.75f + 1f);

							// per-second considering burst mechanics
							float rof = Math.Max(w.rateOfFire, 0.0001f);
							float burstDenom = Math.Max(w.rateOfFire + w.shortCooldown * w.burst, 0.0001f);
							float energyPerSec = (w.burst == 0) ? (energyShot / rof) : (energyShot * (w.burst + 1) / burstDenom);
							float heatPerSec = (w.burst == 0) ? (heatShotRaw / rof) : (heatShotRaw * (w.burst + 1) / burstDenom);

							AppendCsvRow(WeaponsCsv,
								w.index.ToString(),
								name,
								w.type.ToString(),
								w.compType.ToString(),
								w.damageType.ToString(),
								w.space.ToString("0.##"),
								w.damage.ToString("0.##"),
								w.rateOfFire.ToString("0.###"),
								w.burst.ToString(),
								w.range.ToString(),
								w.speed.ToString(),
								w.aoe.ToString("0.##"),
								w.proximityDmgMod.ToString("0.###"),
								w.fluxDamageMod.ToString("0.###"),
								w.heatDamageMod.ToString("0.###"),
								w.techLevel.ToString(),
								w.dropLevel.ToString(),
								w.itemLevel.ToString(),
								energyShot.ToString("0.##"),
								energyPerSec.ToString("0.##"),
								heatShotRaw.ToString("0.##"),
								heatPerSec.ToString("0.##"),
								w.power().ToString("0.##"),
								(w.tradable ? "Trade" : "NoTrade") + (w.massKiller ? "|MassKiller" : "") + (w.autoTargeting ? "|AutoTarget" : "")
							);
						}
						catch (Exception ex)
						{
							Main.error("[CSV Weapons] Failed to write weapon row: " + ex);
						}
					}
					Main.log("[Weapons] Done.");
				}
				catch (Exception ex)
				{
					Main.error("[Weapons] Dump failed: " + ex);
				}
			}

			static (List<TWeapon> list, string source) ResolveWeapons()
			{
				var gi = GameData.data;
				if (gi == null) return (null, "GameData.data=null");

				// Try common names first
				var f =
					AccessTools.Field(gi.GetType(), "weapons") ??
					AccessTools.Field(gi.GetType(), "weaponList") ??
					AccessTools.Field(gi.GetType(), "allWeapons");

				if (f != null)
				{
					var l = AsList(f.GetValue(gi));
					if (l != null) return (l, f.Name);
				}

				// Fallback: find first field that looks like List<TWeapon> / TWeapon[] 
				foreach (var fld in gi.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
				{
					var trial = AsList(fld.GetValue(gi));
					if (trial != null && trial.Count > 0) return (trial, fld.Name);
				}

				return (new List<TWeapon>(), "notfound");
			}

			static List<TWeapon> AsList(object obj)
			{
				if (obj is List<TWeapon> l) return l;
				if (obj is TWeapon[] arr) return arr.ToList();
				if (obj is System.Collections.IEnumerable en)
				{
					var outList = new List<TWeapon>();
					foreach (var o in en)
					{
						if (o is TWeapon w) outList.Add(w);
						else return null; // mixed → bail
					}
					return outList;
				}
				return null;
			}

			static string FormatWeapon(TWeapon w)
			{
				// name fallback
				string name = string.IsNullOrEmpty(w.name) ? $"Weapon#{w.index}" : w.name;

				// energy/heat per-shot (raw; no player skills)
				float energyShot = w.energyCost; // property: energyCostMod * (damage * 0.12f)
				float heatShotRaw = w.heatGenMod * (w.damage * 0.75f + 1f);

				// per-second considering burst mechanics
				float rof = Math.Max(w.rateOfFire, 0.0001f);
				float burstDenom = Math.Max(w.rateOfFire + w.shortCooldown * w.burst, 0.0001f);
				float energyPerSec = (w.burst == 0) ? (energyShot / rof) : (energyShot * (w.burst + 1) / burstDenom);
				float heatPerSec = (w.burst == 0) ? (heatShotRaw / rof) : (heatShotRaw * (w.burst + 1) / burstDenom);

				return
					$"[#{w.index}] {name}  " +
					$"type={w.type}/{w.compType} dmgType={w.damageType} " +
					$"space={w.space:0.##} dmg={w.damage:0.#} ROF={w.rateOfFire:0.##} burst={w.burst} " +
					$"range={w.range} speed={w.speed} aoe={w.aoe:0.#} prox={w.proximityDmgMod:0.###} " +
					$"fluxMod={w.fluxDamageMod:0.###} heatDmgMod={w.heatDamageMod:0.###} " +
					$"tech={w.techLevel} drop={w.dropLevel} ilvl={w.itemLevel} " +
					$"E/shot={energyShot:0.##} E/s={energyPerSec:0.##} Heat/shot={heatShotRaw:0.#} Heat/s={heatPerSec:0.#} " +
					$"power={w.power():0.#} " +
					$"flags=[{(w.tradable ? "Trade" : "NoTrade")}{(w.massKiller ? "|MassKiller" : "")}{(w.autoTargeting ? "|AutoTarget" : "")}]";
			}
		}
	}
}
