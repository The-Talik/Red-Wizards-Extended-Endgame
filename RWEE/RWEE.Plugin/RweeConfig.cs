using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWEE
{
	internal class RweeConfig
	{
		public static ConfigEntry<bool> increaseSectorCap;
		public static ConfigEntry<bool> sectorsLevelUp;
		public static ConfigEntry<bool> disableEquipment;

		public static void Init(ConfigFile config)
		{
			increaseSectorCap = config.Bind(
				"Sectors",
				"Increase Sector Cap",
				true,
				"Increase Sector Cap to 200."
			);
			sectorsLevelUp = config.Bind(
				"Sectors",
				"Sectors Level Up",
				true,
				"Sectors level up when stations level up, and when nearby sectors level up."
			);
						disableEquipment = config.Bind(
				"Ships",
				"Disable Equipment",
				true,
				"Disables instead of unloads equipment when over limit from having a fleet."
			);

		}
	}
}
