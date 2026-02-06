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
		public static ConfigEntry<bool> increase_sector_cap;
		public static ConfigEntry<bool> sectors_level_up;

		public static void Init(ConfigFile config)
		{
			increase_sector_cap = config.Bind(
				"Sectors",
				"Increase Sector Cap",
				true,
				"Increase Sector Cap to 200."
			);
			sectors_level_up = config.Bind(
				"Sectors",
				"Sectors Level Up",
				true,
				"Sectors level up when stations level up, and when nearby sectors level up."
			);
		}
	}
}
