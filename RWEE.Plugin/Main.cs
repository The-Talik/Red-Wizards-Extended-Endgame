using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RWEE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;


namespace RWEE
{


	[BepInPlugin(pluginGuid, pluginName,pluginVersion)] 
	public class Main : BaseUnityPlugin
	{
		public const string pluginGuid = "mc.starvalor.extendedendgame";
		public const string pluginName = "RWEE";//"Red Wizard's Extended Endgame";
		public const string pluginVersion = "1.0.5";

		//public System.Reflection.Assembly asm = typeof(Main).Assembly;
		//public const string pluginVersion = asm.GetName().Version?.ToString()
		//?? "0.0.0";

		public const int OLD_PCHAR_MAXLEVEL = 50;
		public const int NEW_PCHAR_MAXLEVEL = 100;
		public const int NEW_SECT_CAP = 205;
		public const int MAX_RARITY = 7;
		public const bool DEBUG = false;

		internal static ManualLogSource Log;
		private Harmony _harmony;


		public static void log(string msg) => Log?.LogInfo(msg);
		public static void warn(string msg) => Log?.LogWarning(msg);
		public static void error(string msg) => Log?.LogError(msg);

		private void Awake()
		{
			Log = Logger;
			_harmony = new Harmony(pluginGuid);
			_harmony.PatchAll(Assembly.GetExecutingAssembly());
			Main.log($"sectorLevelCap={GameData.sectorLevelCap}  maxLevel={PChar.maxLevel}  ver={GameData.currVersion}{GameData.subVersion}");
			/*
			 * now in prepatcher
			 * Traverse.Create(typeof(PChar))
					.Field("maxLevel")
					.SetValue(100);
			Traverse.Create(typeof(GameData))
				 .Field("sectorLevelCap")
				 .SetValue(205);*/

			Log.LogInfo("Harder Endgame Loaded");
			const string VERSION_URL = "https://mezr.com/star_valor.json";
			if(typeof(GameDataInfo).GetField("rweeJson", BindingFlags.Public | BindingFlags.Instance) == null)
			{
				Main.error("Could not find rweeJson.  Did the prepatcher load?");
				SimplePopup.Show("Could not find rweeJson.  Did the prepatcher load?");
			}
			else
			{
				Main.log("Found rweeJson.");
			}
			Main.log("Has GameDataInfo.rweeJson? " + (typeof(GameDataInfo).GetField("rweeJson", BindingFlags.Public | BindingFlags.Instance) != null));
			VersionControl.Check(this, Logger, VERSION_URL, pluginVersion, (msg, link) =>
			{
				//Main.error(msg + (string.IsNullOrEmpty(link) ? "" : " → " + link));

				SimplePopup.Show(msg, link,2);
				// Or open a page:
				// if (!string.IsNullOrEmpty(link)) Application.OpenURL(link);
			});
		}
		private void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}
		/**
		 * attempt to get controls to work on galaxy map.  Doesn't work.
		 */
		[HarmonyPatch(typeof(GalaxyMap), "ShowHideGalaxyMap")]
		static class GalaxyMap_ShowHideGalaxyMap
		{
			static void Postfix()
			{
				PlayerControl.inst.ReleaseControls(true);
			}
		}
	}
}
