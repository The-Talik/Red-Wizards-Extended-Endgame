using Newtonsoft.Json;
using RW;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static RWMM.Logging;
namespace RWMM
{
	public static class ResourceDump
	{
		private static List<string> _loaded_files = new List<string>();
		public static void DumpListToJson<T>(IEnumerable<T> objects, string comments = "")
		{
			string type_name = typeof(T).Name;
			type_name = type_name.TrimStart('_');
			logr.Log($"-----Dumping {type_name}s-----");


			comments += $"  Top level 'refName' uses and overwrites 'obj.{ObjUtils.RefField(type_name)}'.";
			if(ObjUtils.SpriteField(type_name) != null)
			{
				comments += $"  Top level 'image' overwrites 'obj.{ObjUtils.SpriteField(type_name)}'.";
			}
			comments = comments.Trim();

			var res_root = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "base_prototypes", type_name + "s");

			Directory.CreateDirectory(res_root);

			logr.Log($"  Found {objects.Count()} {type_name}s");
			int i = 0;
			int j = 0;
			foreach (var obj in objects)
			{
				if (obj == null)
					continue;
				var wrap = new Wrap<T>(type_name, obj);
				//				logr.Log($"Ref: {GetField(obj, "refName")} json: {JsonUtil.pretty(json)}");
				var ref_name = ObjUtils.GetRef(obj);
				wrap.refName = ref_name;
				if (string.IsNullOrWhiteSpace(ref_name))
				{
					logr.Warn($"  Skipping {type_name} with no refName/nameRef/id (index {i})");
					i++;
					continue;
				}
				wrap._comment = comments;
				if(ObjUtils.SpriteField(type_name) != null)
				{
					wrap.image = "";
				}
				var json = JsonUtils.ToJson(wrap);
				//string json = JsonConvert.SerializeObject(obj);
				ref_name = SanitizeFilename(ref_name);
				switch (obj.GetType().Name)
				{
					case "TWeapon":
						File.WriteAllText(Path.Combine(res_root, ref_name + ".json"), JsonUtils.Pretty(json));
						break;
					default:
						File.WriteAllText(Path.Combine(res_root, ObjUtils.GetField<int>(obj, "id") + "_" + ref_name + ".json"), JsonUtils.Pretty(json));
						break;
				}
				i++;
				j++;

			}
			logr.Log($"  Dumped {j}/{i} {type_name}s");
		}

		private static string SanitizeFilename(string name)
		{
			foreach (var c in Path.GetInvalidFileNameChars())
				name = name.Replace(c, '_');

			return name;
		}
	}
}

/*
 		private static bool _inited;
		private static string _rwmm_plugin_dir;
		private static string _plugins_dir;

		private static readonly HashSet<string> _dumped_types = new HashSet<string>(StringComparer.Ordinal);
private sealed class RefEq : IEqualityComparer<object>
{
	public new bool Equals(object x, object y) => ReferenceEquals(x, y);
	public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
[DataContract]
public sealed class JsonValue
{
	[DataMember(EmitDefaultValue = false)] public string s;
	[DataMember(EmitDefaultValue = false)] public double? n;
	[DataMember(EmitDefaultValue = false)] public bool? b;
	[DataMember(EmitDefaultValue = false)] public List<JsonValue> a;
	[DataMember(EmitDefaultValue = false)] public Dictionary<string, JsonValue> o;
}
private static JsonValue to_value(object obj, int depth, int max_depth, int max_enumerable_len, HashSet<object> seen)
{
	if (obj == null) return null;
	if (depth > max_depth) return null;

	var t = obj.GetType();

	if (t.IsEnum) return new JsonValue { s = obj.ToString() };

	if (t == typeof(string)) return new JsonValue { s = (string)obj };
	if (t == typeof(bool)) return new JsonValue { b = (bool)obj };

	if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) ||
		t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
		t == typeof(float) || t == typeof(double) || t == typeof(decimal))
		return new JsonValue { n = Convert.ToDouble(obj) };

	if (t == typeof(DateTime))
		return new JsonValue { s = ((DateTime)obj).ToString("o") };

	// skip UnityEngine.* without referencing UnityEngine from Core
	if ((t.FullName ?? "").StartsWith("UnityEngine.", StringComparison.Ordinal))
		return null;

	// skip delegates
	if (typeof(Delegate).IsAssignableFrom(t))
		return null;

	// prevent cycles
	if (!t.IsValueType)
	{
		if (seen.Contains(obj)) return null;
		seen.Add(obj);
	}

	// IDictionary -> object
	if (obj is IDictionary dict)
	{
		var o = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
		int i = 0;

		foreach (DictionaryEntry kv in dict)
		{
			if (i++ >= max_enumerable_len) break;
			var key = kv.Key?.ToString();
			if (string.IsNullOrWhiteSpace(key)) continue;
			o[key] = to_value(kv.Value, depth + 1, max_depth, max_enumerable_len, seen);
		}

		return new JsonValue { o = o };
	}

	// IEnumerable -> array
	if (obj is IEnumerable en && !(obj is string))
	{
		var a = new List<JsonValue>();
		int i = 0;

		foreach (var it in en)
		{
			if (i++ >= max_enumerable_len) break;
			a.Add(to_value(it, depth + 1, max_depth, max_enumerable_len, seen));
		}

		return new JsonValue { a = a };
	}

	// object -> public fields + public readable props
	{
		var o = new Dictionary<string, JsonValue>(StringComparer.Ordinal);

		foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
		{
			if (f.IsNotSerialized) continue;
			if (!is_dumpable_member_type(f.FieldType)) continue;

			object v = null;
			try { v = f.GetValue(obj); } catch { }
			o[f.Name] = to_value(v, depth + 1, max_depth, max_enumerable_len, seen);
		}

		foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!p.CanRead) continue;
			if (p.GetIndexParameters().Length != 0) continue;
			if (!is_dumpable_member_type(p.PropertyType)) continue;

			object v = null;
			try { v = p.GetValue(obj, null); } catch { }
			o[p.Name] = to_value(v, depth + 1, max_depth, max_enumerable_len, seen);
		}

		return new JsonValue { o = o };
	}
}




private static bool is_dumpable_member_type(Type t)
{
	var n = t.FullName ?? "";
	if (n.StartsWith("UnityEngine.", StringComparison.Ordinal)) return false;
	if (typeof(Delegate).IsAssignableFrom(t)) return false;
	return true;
}











public static void dump_vanilla_list<T>(string type_name, List<T> list)
{
	if (!_inited) return;
	if (list == null) return;

	// only dump once per session per type
	if (!_dumped_types.Add(type_name))
		return;

	logr.Log($"Dumping {type_name}s");
	//var base_root = Path.Combine(_rwmm_plugin_dir, "common", "base");
	var res_root = Path.Combine(_rwmm_plugin_dir, "game_resources", type_name+"s");

	Directory.CreateDirectory(res_root);
//			ensure_base_package_json(base_root);

	var path = Path.Combine(res_root, type_name + ".json");

	var ordered = list
		.OrderBy(x => get_ref_name(x) ?? "", StringComparer.Ordinal)
		.ToList();
	logr.Log($"ordered {type_name}s: {ordered.Count}");
	try
	{
		var json = JsonUtil.ToJson(ordered);
		logr.Log("RWMM dump: " + json);
		File.WriteAllText(path, json);
		logr.Log("RWMM dump: " + type_name + " -> " + path);
	}
	catch (Exception e)
	{
		logr.Warn("RWMM dump failed (" + type_name + "): " + e.Message);
	}
}

public static int import_list<T>(string type_name, List<T> target)
{
	if (!_inited) return 0;
	if (target == null) return 0;

	int upserts = 0;

	// build lookup once
	var existing = new Dictionary<string, T>(StringComparer.Ordinal);
	foreach (var e in target)
	{
		var rn = get_ref_name(e);
		if (!string.IsNullOrWhiteSpace(rn) && !existing.ContainsKey(rn))
			existing[rn] = e;
	}

	foreach (var file in enumerate_resource_files(type_name))
	{
		var json = safe_read_all_text(file);
		if (string.IsNullOrWhiteSpace(json))
			continue;

		List<T> incoming;

		try
		{
			incoming = JsonUtil.FromJson<List<T>>(json);
		}
		catch (Exception e)
		{
			logr.Log("RWMM import failed (" + type_name + "): " + file + " :: " + e.Message);
			continue;
		}

		if (incoming == null || incoming.Count == 0)
			continue;

		foreach (var inc in incoming)
		{
			var rn = get_ref_name(inc);
			if (string.IsNullOrWhiteSpace(rn))
				continue;

			if (existing.TryGetValue(rn, out var cur))
			{
				patch_in_place(cur, inc);
			}
			else
			{
				assign_id_if_needed(target, inc);
				target.Add(inc);
				existing[rn] = inc;
			}

			upserts++;
		}
	}

	if (upserts > 0)
		logr.Log("RWMM import: " + type_name + " upserts=" + upserts);

	return upserts;
}

// -----------------------------
// file discovery
// -----------------------------

private static IEnumerable<string> enumerate_resource_files(string type_name)
{
	if (string.IsNullOrWhiteSpace(_plugins_dir))
		yield break;

	// Deterministic:
	// - packages sorted by folder name
	// - files sorted by full path
	foreach (var pkg_dir in Directory.EnumerateDirectories(_plugins_dir).OrderBy(x => x, StringComparer.Ordinal))
	{
		var pkg_json = Path.Combine(pkg_dir, "package.json");
		var res_dir = Path.Combine(pkg_dir, "resources");

		if (!File.Exists(pkg_json) || !Directory.Exists(res_dir))
			continue;

		foreach (var file in Directory.EnumerateFiles(res_dir, type_name + ".json", SearchOption.AllDirectories)
			.OrderBy(x => x, StringComparer.Ordinal))
		{
			yield return file;
		}
	}
}

private static string safe_read_all_text(string path)
{
	try { return File.ReadAllText(path); }
	catch { return null; }
}

// -----------------------------
// patch / id helpers
// -----------------------------

private static void patch_in_place(object existing, object incoming)
{
	if (existing == null || incoming == null) return;

	var t = existing.GetType();

	foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
	{
		if (f.IsInitOnly) continue;

		if (string.Equals(f.Name, "refName", StringComparison.Ordinal)) continue;
		if (string.Equals(f.Name, "id", StringComparison.Ordinal)) continue;

		// avoid UnityEngine.* hard dependency in Core:
		if (f.FieldType.FullName != null && f.FieldType.FullName.StartsWith("UnityEngine.", StringComparison.Ordinal))
			continue;

		try { f.SetValue(existing, f.GetValue(incoming)); }
		catch { }
	}

	foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
	{
		if (!p.CanRead || !p.CanWrite) continue;
		if (p.GetIndexParameters().Length != 0) continue;

		if (string.Equals(p.Name, "refName", StringComparison.Ordinal)) continue;
		if (string.Equals(p.Name, "id", StringComparison.Ordinal)) continue;

		if (p.PropertyType.FullName != null && p.PropertyType.FullName.StartsWith("UnityEngine.", StringComparison.Ordinal))
			continue;

		try { p.SetValue(existing, p.GetValue(incoming, null), null); }
		catch { }
	}
}

private static void assign_id_if_needed<T>(List<T> target, T obj)
{
	if (obj == null) return;

	var cur = get_id(obj);
	if (cur.HasValue && cur.Value > 0)
		return;

	int max = 0;

	foreach (var e in target)
	{
		var id = get_id(e);
		if (id.HasValue && id.Value > max)
			max = id.Value;
	}

	set_id(obj, max + 1);
}

private static string get_ref_name(object obj)
{
	if (obj == null) return null;

	var t = obj.GetType();

	var f = t.GetField("refName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	if (f != null)
		return f.GetValue(obj) as string;

	var p = t.GetProperty("refName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	if (p != null && p.CanRead && p.GetIndexParameters().Length == 0)
		return p.GetValue(obj, null) as string;

	return null;
}

private static int? get_id(object obj)
{
	if (obj == null) return null;

	var t = obj.GetType();

	var f = t.GetField("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	if (f != null)
	{
		try
		{
			var v = f.GetValue(obj);
			if (v is int i) return i;
			if (v is long l) return (int)l;
		}
		catch { }
	}

	var p = t.GetProperty("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	if (p != null && p.CanRead && p.GetIndexParameters().Length == 0)
	{
		try
		{
			var v = p.GetValue(obj, null);
			if (v is int i) return i;
			if (v is long l) return (int)l;
		}
		catch { }
	}

	return null;
}

private static void set_id(object obj, int value)
{
	if (obj == null) return;

	var t = obj.GetType();

	var f = t.GetField("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	if (f != null && !f.IsInitOnly)
	{
		try
		{
			if (f.FieldType == typeof(int)) f.SetValue(obj, value);
			else if (f.FieldType == typeof(long)) f.SetValue(obj, (long)value);
		}
		catch { }
		return;
	}

	var p = t.GetProperty("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	if (p != null && p.CanWrite && p.GetIndexParameters().Length == 0)
	{
		try
		{
			if (p.PropertyType == typeof(int)) p.SetValue(obj, value, null);
			else if (p.PropertyType == typeof(long)) p.SetValue(obj, (long)value, null);
		}
		catch { }
	}
}

// -----------------------------
// package.json (base dump)
// -----------------------------

[DataContract]
private sealed class Package_json
{
	[DataMember(Name = "name")] public string name;
	[DataMember(Name = "title")] public string title;
	[DataMember(Name = "version")] public string version;
	[DataMember(Name = "author")] public string author;
	[DataMember(Name = "description")] public string description;
	[DataMember(Name = "generated_utc")] public string generated_utc;
}

private static void ensure_base_package_json(string base_root)
{
	var path = Path.Combine(base_root, "package.json");
	if (File.Exists(path))
		return;

	var pkg = new Package_json
	{
		name = "base",
		title = "Base Game (Dump)",
		version = "0.0.0",
		author = "RWMM",
		description = "Auto-generated dump of core game resources.",
		generated_utc = DateTime.UtcNow.ToString("o")
	};

	try
	{
		File.WriteAllText(path, JsonUtil.ToJson(pkg));
	}
	catch { }
}*/
