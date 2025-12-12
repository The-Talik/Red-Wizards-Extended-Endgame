using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RW;
using System; 
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static RWMM.Logging;

namespace RWMM
{
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class Main : BaseUnityPlugin
	{
		public const string pluginGuid = "mc.starvalor.RWMM";
		public const string pluginName = "RWMM";//"Red Wizard's Mod Manager";
		public const string pluginVersion = Versions.RWMM;
		internal static int errorCount = 0;



		public const bool DEBUG = false;

		private Harmony _harmony;

		private void Awake()
		{
			_harmony = new Harmony(pluginGuid);
			_harmony.PatchAll(Assembly.GetExecutingAssembly());
			Logging.Init(Logger, 1);
			RW.Core.Logging.Init(Logging.logr);
//			RW.Core.Logging.Init(Logger, 1);
			//Logger.ForegroundColor = ConsoleColor.Cyan;
			//	Logger.WriteLine("This is cyan text");



			logr.Log("[RWMM] Loaded");

		}
		private void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}
	}
	public class Logging : BaseUnityPlugin
	{
		internal static RW.Logging.Logr logr;

		public static void Init(ManualLogSource log, int verbosity)
		{
			logr = new RW.Logging.Logr(log, verbosity);
			logr.Log("[RWMM] Logging initialized.");
		}
	}
}