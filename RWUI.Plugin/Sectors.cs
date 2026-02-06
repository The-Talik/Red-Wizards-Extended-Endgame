using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RWUI.Logging;
namespace RWUI
{
	internal class Sectors
	{
		[HarmonyPatch(typeof(TSector), "GetString")]
		static class TSector_GetString
		{
			static void Postfix(bool ___discovered, List<BigAsteroid> ___bigAsteroids, ref string __result)
			{
				if (___discovered)
					__result += $"\nLarge Asteroids: {___bigAsteroids.Count}";
			}
		}
	}
}
