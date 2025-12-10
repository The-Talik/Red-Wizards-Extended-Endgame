using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RWMM
{
	internal class Test
	{
		class TestShip
		{
			public string name;
			public int rarity;
			public int id;
		}

		public static void TestNewtonsoft()
		{
			try
			{
				var ship = new TestShip
				{
					name = "Hephaestus",
					rarity = 1,
					id = 42
				};

				var patch_json = "{ \"rarity\": 4 }";

				// This should only change rarity, leaving name/id untouched
				JsonConvert.PopulateObject(patch_json, ship);

				Main.log($"Newtonsoft test: name={ship.name}, rarity={ship.rarity}, id={ship.id}");

				// Expected log: name=Hephaestus, rarity=4, id=42
			}
			catch (Exception ex)
			{
				Main.log($"Newtonsoft test FAILED: {ex}", 0);
			}
		}
	}
}
