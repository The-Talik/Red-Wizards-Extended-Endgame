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

namespace RW.Logging
{ 
	/**
	 * Verbosity 3: Most RW.Core log items
	 * Verbosity 2: shows extra detailed info from the app.  RW.Core does not use this
	 * Verbosity 1: shows standard logs, warnings, and errors
	 * Verbosity 0 (default): shows warnings and errors
	 * Verbosity -1: shows errors
	 * Verbosity -2: show nothing
	 */
	public class Logr
	{
		public static ManualLogSource log;
		public static int verbosity = 0;
		public static int errorCount = 0;
		public static List<string> errors = new List<string>();
		public static int indent = 0;
		public Logr(ManualLogSource _log, int _verbosity = 0)
		{
			log = _log;
			verbosity = _verbosity;
		}
		public void Log(string msg, int level = 1)
		{
			if(indent > 0)
				msg = new string(' ', indent) + msg;
			if (verbosity >= level)
				log?.LogInfo(msg);
		}
		public void Log(object obj, int level = 1)
		{
			Log(JsonUtils.ToPrettyJson(obj), level);
		}
		public void LogLineList<T>(List<T> obj, int level = 1)
		{
			for (int i = 0; i < obj.Count; i++)
			{
				LogLineObj(obj[i], level);
			}
		}
		public void LogLineObj(object obj, int level = 1)
		{
			Log($"{obj.GetType()} {ObjUtils.GetId(obj,true)} {ObjUtils.GetRef(obj)}", level);
		}
		public void LogTruncate(string msg, int maxLength = 128, int level = 1)
		{
			if (msg.Length > maxLength)
				msg = msg.Substring(0, maxLength) + "...(truncated)";
			Log(msg, level);
		}
		public void Warn(string msg, int level = 0)
		{
			if (indent > 0)
				msg = new string(' ', indent) + msg;
			if (verbosity >= level)
				log?.LogWarning(msg);
		}
		public void Error(string msg, bool stash = true, int level = -1)
		{
			if (indent > 0)
				msg = new string(' ', indent) + msg;
			if (verbosity >= level)
			{
				log?.LogError(msg);
				if (stash)
				{
					errorCount++;
					errors.Add(msg);
				}
				//if (showPopup)
				//	SimplePopup.Show("Error",msg);
			}
		}
		public void PopupErrors(string title = null, string text =null)
		{
			if (errorCount > 0)
			{
				if (title == null)
					title = "Red Wizard's Mod Manager";
				StringBuilder sb = new StringBuilder();
				if (text == null)
					text = $"There were {errorCount} errors:";
				sb.AppendLine(text);
				foreach (var err in errors)
				{
					sb.AppendLine(err);
				}
				SimplePopup.Show(title, sb.ToString());
				errorCount = 0;
				errors.Clear();
			}
		}
		public void Open(string text = "")
		{
			Log(text);
			Log("{");
			indent++;
		}
		public void Close(string text = "")
		{
			indent--;
			text = "} " + text;
			Log(text);
			
		}
	}
}