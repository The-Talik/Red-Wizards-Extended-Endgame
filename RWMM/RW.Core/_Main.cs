using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;


namespace RW
{
	/**
	 * Verbosity 3: Most RW.Core log items
	 * Verbosity 2: shows extra detailed info from the app.  RW.Core does not use this
	 * Verbosity 1: shows standard logs, warnings, and errors
	 * Verbosity 0 (default): shows warnings and errors
	 * Verbosity -1: shows errors
	 * Verbosity -2: show nothing
	 */
	public static class Main
	{
		public static ManualLogSource Log;
		public static int verbosity = 0;
		public static void Init(ManualLogSource log, int verbosity = 0)
		{
			Main.Log = log;
			Main.verbosity = verbosity;
		}
		public static void log(string msg, int level = 1, ManualLogSource log = null)
		{
			if (log == null)
				log = Main.Log;
			if (Main.verbosity >= level)
				log?.LogInfo(msg);
		}
		public static void log_obj(object obj, int level = 1)
		{
			log(JsonUtils.ToPrettyJson(obj), level);
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
		public static void log_line_obj(object obj, int level = 1)
		{
			log($"{obj.GetType()} {ObjUtils.GetField<int>(obj,"id")} {ObjUtils.GetField<string>(obj, "refName")}", level);
		}
		public static void log_line_list(List<object> obj, int level = 1)
		{
			for (int i = 0; i < obj.Count; i++)
			{
				log_line_obj(obj[i], level);
			}
		}
	}

}
