using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RW
{
	public static class ErrorPopupMonitor
	{
		private const int MaxMessageChars = 6000;
		private static readonly HashSet<string> shown = new HashSet<string>(StringComparer.Ordinal);
		private static bool installed;
		private static ManualLogSource log;

		public static void Install(ManualLogSource logger, string sourceName = null)
		{
			if (!installed)
			{
				log = logger;
				Application.logMessageReceived += HandleLogMessageReceived;
				installed = true;
			}

			ShowStartupErrors();
		}

		public static void ShowStartupErrors()
		{
			string text;
			if (StartupErrorLog.TryReadAndClear(out text))
			{
				Show("Red Wizard startup errors",
					"Errors were recorded before the in-game UI was available:" + Environment.NewLine + Environment.NewLine + text);
			}
		}

		public static void Report(string title, Exception ex)
		{
			Report(title, ex == null ? "<no exception>" : ex.ToString());
		}

		public static void Report(string title, string message)
		{
			Show(title, message);
		}

		private static void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
		{
			if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
				return;

			if (string.IsNullOrEmpty(condition))
				return;

			var key = type + "|" + condition + "|" + FirstLine(stackTrace);
			if (!shown.Add(key))
				return;

			var sb = new StringBuilder();
			sb.AppendLine("A Unity error was logged:");
			sb.AppendLine();
			sb.AppendLine(type + ": " + condition);
			if (!string.IsNullOrEmpty(stackTrace))
			{
				sb.AppendLine();
				sb.AppendLine(stackTrace);
			}

			Show("Red Wizard runtime error", sb.ToString());
		}

		private static void Show(string title, string message)
		{
			try
			{
				SimplePopup.Show(title, Trim(message));
			}
			catch (Exception ex)
			{
				try
				{
					log?.LogWarning("Failed to show Red Wizard error popup: " + ex);
				}
				catch
				{
				}
			}
		}

		private static string FirstLine(string text)
		{
			if (string.IsNullOrEmpty(text))
				return "";

			var idx = text.IndexOfAny(new[] { '\r', '\n' });
			return idx < 0 ? text : text.Substring(0, idx);
		}

		private static string Trim(string text)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= MaxMessageChars)
				return text;

			return text.Substring(0, MaxMessageChars) + Environment.NewLine + "...(truncated)";
		}
	}
}
