using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using RW.Logging;
using UnityEngine;


namespace RW.Core
{
	public static class Main
	{

	}
	public class Logging : BaseUnityPlugin
	{
		internal static RW.Logging.Logr logr;

		public static void Init(Logr _logr)
		{
			logr = _logr;
			logr.Log("RW.Core Loaded");
			//SettingsUtils.Init(Settings.Type,"RWMM_Settings.cfg");
		}
		public static void Init(ManualLogSource log, int verbosity)
		{
			logr = new RW.Logging.Logr(BepInEx.Logging.Logger.CreateLogSource(log.SourceName+".RW"), verbosity);
			logr.Log("RW.Core Loaded");
		}
	}
}
