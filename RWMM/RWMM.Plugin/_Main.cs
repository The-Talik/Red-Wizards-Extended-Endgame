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


namespace RWMM
{
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class Main : BaseUnityPlugin
	{
		public const string pluginGuid = "mc.starvalor.RWMM";
		public const string pluginName = "RWMM";//"Red Wizard's Mod Manager";
		public const string pluginVersion = "0.0.1";
		internal static int errorCount = 0;


		//public System.Reflection.Assembly asm = typeof(Main).Assembly;
		//public const string pluginVersion = asm.GetName().Version?.ToString()
		//?? "0.0.0";

		public const bool DEBUG = false;

		private Harmony _harmony;
		public static ManualLogSource Log;
		public static int verbosity = 0;
		public static void InitLog(ManualLogSource log, int verbosity = 0)
		{
			Main.Log = log;
			Main.verbosity = verbosity;
		}
		public static void log(string msg, int level = 1)
		{
			if (Main.verbosity >= level)
				Main.Log?.LogInfo(msg);
		}
		public static void log_obj(object obj, int level = 1)
		{
			Main.log(JsonUtils.ToPrettyJson(obj), level);
		}
		public static void log_line_list<T>(List<T> obj, int level = 1)
		{
			for(int i=0;i<obj.Count;i++)
			{
				log_line_obj(obj[i], level);
			}
		}
		public static void log_line_obj(object obj, int level = 1)
		{
			log($"{obj.GetType()} {ObjUtils.GetField<int>(obj, "id",true)} {ObjUtils.GetRef(obj)}", level);
		}
		public static void warn(string msg, int level = 0)
		{
			if (Main.verbosity >= level)
				Main.Log?.LogWarning(msg);
		}

		public static void error(string msg, bool showPopup = false, int level = -1)
		{
			if (Main.verbosity >= level)
			{
				Main.Log?.LogError(msg);
				if (showPopup)
					SimplePopup.Show(msg);
			}
		}
		/*

		public static void Main.log(string msg) => Log?.LogInfo(msg);
		public static void Main.warn(string msg) => Log?.LogWarning(msg);
		public static void Main.error(string msg,bool showPopup = true)
		{
			Log?.LogMain.error(msg);
			errorCount++;
			//SimplePopup.Show(msg);
		}*/

		private void Awake()
		{
			RW.Main.Init(Logger, 1);
			InitLog(Logger, 1);
			_harmony = new Harmony(pluginGuid);
			_harmony.PatchAll(Assembly.GetExecutingAssembly());

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("This is cyan text");


			/*Resources_IO.init(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				BepInEx.Paths.PluginPath
			);*/

			Main.log("[RWMM] Loaded");
			//if(errorCount>0)
			//	Main.error($"[RWMM] There were {errorCount} errors during load. See log for details.",true);
		}
		private void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}
	}
}