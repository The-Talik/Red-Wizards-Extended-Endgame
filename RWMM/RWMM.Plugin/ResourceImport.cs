using Newtonsoft.Json;
using RW;
using RWMM.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static RWMM.ResourceDump;
using static RWMM.Resources_IO;

namespace RWMM
{
	internal class ResourceImport
	{
		public static void ImportType<T, TData>(ref List<T> list)
		{
			string type_name = typeof(T).Name;
			type_name = type_name.TrimStart('_');
			var folders = GetPrototypeFolders();
			foreach (var folder in folders)
			{
				Main.log($"Scanning prototype folder for {type_name}s: {folder}");
				var json_files = DirUtils.FindJsonFiles(folder);
				foreach (var json_file in json_files)
				{
					//Main.log($"Importing prototype file: {json_file}");
					var json_text = File.ReadAllText(json_file);
					var type = GetFromJson("type", json_text);

					if (type == null)
					{
						Main.warn($"    Could not determine type of prototype file: {json_file}");
						continue;
					}

					if (type == type_name)
					{
						//Main.log($"  Found {type} file: {json_file} ");
						ImportObject<T, TData>(json_text, ref list, json_file, Directory.GetParent(folder).FullName);
					}
				}
			}
			Main.log($"----Dumped and imported {list.Count} objects of type {typeof(T).Name}----");
			if (Resources_IO.dump_data > 0)
				Main.log_line_list<T>(list);

		}
		public static void ImportObject<T, TData>(string json, ref List<T> list, string json_file, string base_folder)
		{
			Main.log("importing object json "+base_folder);
			var wrap = JsonUtils.FromJson<Wrap<TData>>(json);
			wrap.base_folder = base_folder;
			Main.log_obj(wrap);


			Main.log($"    Importing {typeof(T).Name} ref: \"{wrap.refName}\" file: {json_file}");


			//First we look for an existing item to update.
			if (TryUpdateExisting<T, TData>(list, wrap, json))
				return;

			//Lets see if we have a clone option.
			if (TryCloneFrom<T, TData>(list, wrap, json))
				return;

			//Otherwise create a new one.
			Main.error($"    Creating new prototypes not yet supported.  Clone an existing one instead for: {wrap.refName}");
			return;

			//obj = new T();
			//Main.log($"    Creating new {typeof(T).Name} ref: {refName}");
			//addNewProto<T, TData>(obj,ref list, wrap, refName);
			//return;
		}
		private static void TryUpdateImages<T, TData>(T obj, Wrap<TData> wrap)
		{
			if (!string.IsNullOrEmpty(wrap.image))
			{
				string sprite_field = ObjUtils.SpriteField(typeof(T).Name);
				if (sprite_field != null)
				{
					Sprite sprite = IconUtils.MakeSprite(Path.Combine(wrap.base_folder, wrap.image));
					ObjUtils.SetField<Sprite>(obj, sprite_field, sprite);
				}
			}
		}
		private static bool TryUpdateExisting<T, TData>(List<T> list, Wrap<TData> wrap, string json)
		{
			//Let's grab the original game object.
			var gameObj = ListUtils.GetByRef(list, wrap.refName);
			if (gameObj == null)
				return false;
			Main.log($"    Found existing {typeof(T).Name} ref: {wrap.refName}, updating.");

			var clonedObj = ObjUtils.Clone<T>(gameObj);


			Wrap<T> gameObjWrap = new Wrap<T>(gameObj.GetType().Name, gameObj);

			JsonConvert.PopulateObject(json, gameObjWrap);
			//restore stuff from original
			if (ObjUtils.GetField<int>(gameObj, "expansion") == 0 && ObjUtils.GetField<int>(clonedObj, "expansion") != 0)
			{
				ObjUtils.SetField<int>(gameObj, "expansion", ObjUtils.GetField<int>(clonedObj, "expansion"));
				Main.error($"Disabling expansion is not allowed");
			}
			TryUpdateImages(gameObj, wrap);
			showChangedFields<T>(clonedObj, gameObj);
			return true;

		}
		private static bool TryCloneFrom<T, TData>(List<T> list, Wrap<TData> wrap, string json)
		{
			if (wrap.cloneFrom == null)
				return false;

			var gameObj = ListUtils.GetByRef(list, wrap.cloneFrom);
			if (gameObj == null)
				return false;
			Main.log($"    Found {typeof(T).Name} to clone from: {wrap.cloneFrom} for new object {wrap.refName} ");

			var clonedObj = ObjUtils.Clone<T>(gameObj);

			Wrap<T> clonedbjWrap = new Wrap<T>(clonedObj.GetType().Name, clonedObj);


			JsonConvert.PopulateObject(json, clonedbjWrap);
			//Fix stuff from original
			if (ObjUtils.GetField<int>(gameObj, "id") == ObjUtils.GetField<int>(clonedObj, "id"))
			{
				int newID = ListUtils.NextFreeId<T>(list);
				ObjUtils.SetField<int>(clonedObj, "id", newID);
			}
			if (ListUtils.GetBy<T>(list, "id", ObjUtils.GetField<int>(clonedObj, "id")) != null)
			{
				Main.error($"    ID conflict with {typeof(T).Name} ref: {wrap.refName} ID {ObjUtils.GetField<int>(clonedObj, "id")} is already in use. Skipping");
				return true;
			}
			ObjUtils.SetRef(clonedObj,wrap.refName);
			TryUpdateImages(clonedObj, wrap);
			showChangedFields<T>(gameObj, clonedObj);
			list .Add(clonedObj);
			return true;
		}
		private static void showChangedFields<T>(T original, T updated)
		{
			var type = typeof(T);
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var field in fields)
			{
				var originalValue = field.GetValue(original);
				var updatedValue = field.GetValue(updated);
				if (!object.Equals(originalValue, updatedValue))
				{
					Main.log($"{field.Name}: '{originalValue}' -> '{updatedValue}'");
				}
}
		}
		public static List<string> GetPrototypeFolders()
		{
			var dir = RW.DirUtils.FindPluginDir();
			if (dir == null || !dir.Exists)
				return new List<string>();

			// Find all "resources" subfolders under plugins/
			return Directory
				.EnumerateDirectories(dir.FullName, "prototypes", SearchOption.AllDirectories)
				.ToList();
		}


		public static string GetFromJson(string type, string json)
		{
			if (string.IsNullOrEmpty(json))
				return null;

			var m = Regex.Match(json, $"\"{type}\"\\s*:\\s*\"([^\"]*)\"");
			return m.Success ? m.Groups[1].Value : null;
		}
	}
}
