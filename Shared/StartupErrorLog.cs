using BepInEx;
using System;
using System.IO;
using System.Text;

namespace RW
{
	public static class StartupErrorLog
	{
		private const string FileName = "red-wizards-startup-errors.log";

		public static void Append(string source, Exception ex)
		{
			Append(source, ex == null ? "<no exception>" : ex.ToString());
		}

		public static void Append(string source, string message)
		{
			try
			{
				var path = GetPath();
				var dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir))
					Directory.CreateDirectory(dir);

				File.AppendAllText(path, Format(source, message), Encoding.UTF8);
			}
			catch
			{
			}
		}

		public static bool TryReadAndClear(out string text)
		{
			text = null;
			try
			{
				var path = GetPath();
				if (!File.Exists(path))
					return false;

				text = File.ReadAllText(path, Encoding.UTF8);
				File.Delete(path);
				return !string.IsNullOrWhiteSpace(text);
			}
			catch (Exception ex)
			{
				text = "Failed to read Red Wizard startup error log: " + ex;
				return true;
			}
		}

		private static string Format(string source, string message)
		{
			var name = string.IsNullOrEmpty(source) ? "Red Wizard" : source;
			return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + name + Environment.NewLine
				+ (message ?? "<no message>") + Environment.NewLine + Environment.NewLine;
		}

		private static string GetPath()
		{
			string dir = null;
			try
			{
				dir = Paths.ConfigPath;
			}
			catch
			{
			}

			if (string.IsNullOrEmpty(dir))
				dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "config");

			return Path.Combine(dir, FileName);
		}
	}
}
