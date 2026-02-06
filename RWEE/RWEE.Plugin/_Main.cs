using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RW;
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
using static RWEE.Logging;


namespace RWEE
{


	[BepInPlugin(pluginGuid, pluginName,pluginVersion)] 
	public class Main : BaseUnityPlugin
	{
		public const string pluginGuid = "mc.starvalor.extendedendgame";
		public const string pluginName = "RWEE";//"Red Wizard's Extended Endgame";
		public const string pluginVersion = Versions.RWEE;

		public const int OLD_PCHAR_MAXLEVEL = 50;
		public const int NEW_PCHAR_MAXLEVEL = 100;
		public const int NEW_SECT_CAP = 205;
		public const int MAX_RARITY = 7;
		public const bool DEBUG = false;

		private Harmony _harmony;

		private ConfigEntry<bool> enemy_scaling_enabled;
		private ConfigEntry<bool> sector_leveling_enabled;
		private ConfigEntry<bool> loot_tuning_enabled;

		private void Awake()
		{
			RweeConfig.Init(Config);

			_harmony = new Harmony(pluginGuid);
			_harmony.PatchAll(Assembly.GetExecutingAssembly());
			Logging.Init(Logger, 1);

			logr.Log("Red Wizard's Extended Endgame Loaded");
			const string VERSION_URL = "https://mezr.com/star_valor.json.php";
			var fi = typeof(GameData).GetField("rweePatcherVersion", BindingFlags.Public | BindingFlags.Static);
			//logr.Log("GameDataInfo fields: " + string.Join(", ", fi.Select(f => f.Name + (f.IsStatic ? "[static]" : "[inst]"))));
			var patcherVersion = fi.GetValue(null) as string;
			if(patcherVersion != pluginVersion)
			{
				logr.Error($"Patcher version does not match plugin version.  Ensure both are up to date.  Patcher={patcherVersion} Plugin={pluginVersion}");
			}

			if (typeof(GameDataInfo).GetField("rweeJson", BindingFlags.Public | BindingFlags.Instance) == null)
			{
				logr.Error("Could not find rweeJson.  Did the prepatcher load?");
			}
			else
			{
				logr.Log("Found rweeJson.");
			}

			logr.Log("Has GameDataInfo.rweeJson? " + (typeof(GameDataInfo).GetField("rweeJson", BindingFlags.Public | BindingFlags.Instance) != null));
			
			VersionControl.Check(this, Logger, VERSION_URL, pluginVersion, (msg, link) =>
			{

				logr.Error(msg + (string.IsNullOrEmpty(link) ? "" : " → " + link),false);

				RW.SimplePopup.Show("Red Wizard's Extended Endgame", msg, link);
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
				//PlayerControl.inst.ReleaseControls(true);
			}
		}
	}
	public class Logging : BaseUnityPlugin
	{
		internal static RW.Logging.Logr logr;

		public static void Init(ManualLogSource log,int verbosity)
		{
			logr = new RW.Logging.Logr(log, verbosity);
			logr.Log("[RWEE] Logging initialized.");
		}
	}
}
