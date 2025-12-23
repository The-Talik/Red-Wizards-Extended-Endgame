using Newtonsoft.Json;
using RW;
using RWMM.Dto;
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
		public static void DumpListToJson<T, TData>(IEnumerable<T> objects, string comments = "")
			where TData : new()
		{
			List<TData> dumpList = ListUtils.Clone<T, TData>(objects);

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

			logr.Log($"  Found {dumpList.Count()} {type_name}s");
			int i = 0;
			int j = 0;
			foreach (var obj in dumpList)
			{
				if (obj == null)
					continue;
				//logr.Log($"  Dumping {type_name} index {i}");
				var wrap = new Wrap<TData>(type_name, obj);
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

				File.WriteAllText(Path.Combine(res_root, ObjUtils.GetId(obj) + "_" + ref_name + ".json"), JsonUtils.Pretty(json));

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
