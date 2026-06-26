using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using RW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static RWUI.Logging;


namespace RWUI
{


	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class Main : BaseUnityPlugin
	{
		public const string pluginGuid = "mc.starvalor.RWUI";
		public const string pluginName = "RWUI";//"Red Wizard's Extended Endgame";
		public const string pluginVersion = Versions.RWEE;

		private Harmony _harmony;


		private void Awake()
		{
			Logging.Init(Logger, 1);
			ErrorPopupMonitor.Install(Logger, pluginName);
			try
			{
				_harmony = new Harmony(pluginGuid);
				_harmony.PatchAll(Assembly.GetExecutingAssembly());

				logr.Log("Red Wizard's User Interface loaded");
			}
			catch (Exception ex)
			{
				Logger.LogError("[RWUI] Load failed: " + ex);
				ErrorPopupMonitor.Report("Red Wizard's User Interface load error", ex);
				throw;
			}
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
			logr.Log("[RWUI] Logging initialized.");
		}
	}
}
