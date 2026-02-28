
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWUI
{
	internal class Items
	{
		[HarmonyPatch(typeof(TWeapon), "GetNameModified", new Type[] { typeof(int), typeof(int), typeof(bool) })]
		static class TWeapon_GetNameModified
		{
			static void Postfix(ref TWeapon __instance, ref string __result)
			{
				__result = $"{__result} [{__instance.space}]";
			}
		}
	}
}
