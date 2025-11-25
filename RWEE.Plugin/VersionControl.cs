using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;               // ServicePointManager for TLS
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;   // UnityWebRequest
namespace RWEE
{
		[Serializable]
		public class RemoteVersion
		{
			// initialize to silence "never assigned" warnings
			public string version = "";
			public string url = "";
			public string message = "";
			public ChangelogEntry[] changelog = new ChangelogEntry[0];

			public string FormatMessage(string local) =>
				(message ?? "").Replace("{remote}", version ?? "").Replace("{local}", local ?? "");
		}
		[Serializable]
		public class ChangelogEntry
		{
			public string v = "";
			public string[] ch = new string[0];
		}
	public static class VersionControl
	{

		/// <summary>
		/// Start a version check. Provide your own UI callback; if null, logs only.
		/// </summary>
		public static void Check(
			MonoBehaviour host,
			ManualLogSource log,
			string versionUrl,
			string localVersion,
			Action<string, string> onUpdateAvailable = null
		)
		{
			try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
			host.StartCoroutine(CheckRoutine(log, versionUrl, localVersion, onUpdateAvailable));
		}

		private static IEnumerator CheckRoutine(
			ManualLogSource log,
			string url,
			string localVer,
			Action<string, string> onUpdate
		)
		{
			using (var req = UnityWebRequest.Get(url))
			{
				req.timeout = 10;
				yield return req.SendWebRequest();

				// Unity 2019 API
				if (req.isNetworkError || req.isHttpError)
				{
					log?.LogWarning("Version check failed: " + req.error);
					yield break;
				}

				var json = req.downloadHandler.text;
				RemoteVersion rv = null;
				Main.warn(json);
				try { rv = JsonUtil.FromJson<RemoteVersion>(json); }
				catch (Exception ex)
				{
					log?.LogWarning("Version JSON parse error: " + ex.Message);
					yield break;
				}
				if (rv == null || string.IsNullOrEmpty(rv.version))
					yield break;

				if (IsNewer(rv.version, localVer))
				{
					var msg = !string.IsNullOrEmpty(rv.message)
						? rv.message.Replace("{local}", localVer).Replace("{remote}", rv.version)
						: ("A new version " + rv.version + " is available (you have " + localVer + ").");
					Main.error($"isNewer rv.changelog.Length: {rv.changelog.Length}");
					for (int i = 0; i < (rv.changelog?.Length ?? 0); i++)
					{
						var e = rv.changelog[i];
						if (e == null) continue;
						if (VersionControl.IsNewer(e.v, localVer) && e.ch != null)
						{
							msg += "\n<b>" + e.v + "</b>";
							for (int j = 0; j < e.ch.Length; j++)
							{
								msg += "\n - " + e.ch[j];
							}
						}
					}
					if (onUpdate != null) onUpdate(msg, rv.url);
					else log?.LogInfo(msg + (string.IsNullOrEmpty(rv.url) ? "" : " → " + rv.url));
				}
			}
		}

		public static bool IsNewer(string remote, string local)
		{
			try
			{
				var r = new Version(Normalize(remote));
				var l = new Version(Normalize(local));
				return r.CompareTo(l) > 0;
			}
			catch
			{
				return string.Compare(remote, local, StringComparison.Ordinal) > 0;
			}
		}

		private static string Normalize(string v)
		{
			if (string.IsNullOrEmpty(v)) return "0.0.0";
			var s = v.Trim();
			var dash = s.IndexOf('-');
			if (dash >= 0) s = s.Substring(0, dash); // drop -beta
			var parts = s.Split('.');
			if (parts.Length == 1) s += ".0.0";
			else if (parts.Length == 2) s += ".0";
			return s;
		}
	}

}