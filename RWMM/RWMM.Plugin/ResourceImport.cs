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
using System.Xml.Linq;
using UnityEngine;
using static RWMM.Logging;
using static RWMM.ResourceDump;
using static RWMM.Resources_IO;
namespace RWMM
{
	internal class ResourceImport
	{
		public static void ImportType<T, TData>(ref T[] array)
		{
			if (array == null)
			{
				array = Array.Empty<T>();
			}

			var list = new List<T>(array);

			ImportType<T, TData>(ref list);

			array = list.ToArray();
		}
		public static void ImportType<T, TData>(ref List<T> list)
		{
			string type_name = typeof(T).Name;
			type_name = type_name.TrimStart('_');
			var folders = GetPrototypeFolders();
			foreach (var folder in folders)
			{
				logr.Log($"Scanning prototype folder for {type_name}s: {folder}");
				var json_files = DirUtils.FindJsonFiles(folder);
				foreach (var json_file in json_files)
				{
					//logr.Log($"Importing prototype file: {json_file}");
					var json_text = File.ReadAllText(json_file);
					var type = GetFromJson("type", json_text);

					if (type == null)
					{
						logr.Warn($"    Could not determine type of prototype file: {json_file}");
						continue;
					}

					if (type == type_name)
					{
						//logr.Log($"  Found {type} file: {json_file} ");
						ImportObject<T, TData>(json_text, ref list, json_file, Directory.GetParent(folder).FullName);
					}
				}
			}
			logr.Log($"----Dumped and imported {list.Count} objects of type {typeof(T).Name}----");
			if (Resources_IO.dump_data > 0)
				logr.LogLineList<T>(list);

		}
		public static void ImportObject<T, TData>(string json, ref List<T> list, string json_file, string base_folder)
		{
			logr.Log("importing object json "+base_folder);
			var wrap = JsonUtils.FromJson<Wrap<TData>>(json);
			wrap.base_folder = base_folder;
			logr.Log(json);


			logr.Log($"    Importing {typeof(T).Name} ref: \"{wrap.refName}\" file: {json_file}");


			//First we look for an existing item to update.
			if (TryUpdateExisting<T, TData>(list, wrap, json))
				return;

			//Lets see if we have a clone option.
			if (TryCloneFrom<T, TData>(list, wrap, json))
				return;

			//Otherwise create a new one.
			logr.Error($"    Creating new prototypes not yet supported.  Clone an existing one instead for: {wrap.refName}");
			return;

			/*
			if (TryCreateNew<T, TData>(list, wrap, json))
				return;
			return;*/
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
			logr.Log($"Found existing {typeof(T).Name} ref: {wrap.refName}, updating.");

			var clonedObj = ObjUtils.Clone<T>(gameObj);


			Wrap<T> gameObjWrap = new Wrap<T>(gameObj.GetType().Name, gameObj);

			PopulateObject<T, TData>(json, gameObjWrap, wrap);
			//restore stuff from original
			if (ObjUtils.GetField<int>(gameObj, "expansion", true) == 0 && ObjUtils.GetField<int>(clonedObj, "expansion",true) != 0)
			{
				ObjUtils.SetField<int>(gameObj, "expansion", ObjUtils.GetField<int>(clonedObj, "expansion"));
				logr.Error($"Disabling expansion is not allowed");
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
			logr.Log($"Found {typeof(T).Name} to clone from: {wrap.cloneFrom} for new object {wrap.refName} ");

			var clonedObj = ObjUtils.Clone<T>(gameObj);

			Wrap<T> clonedbjWrap = new Wrap<T>(clonedObj.GetType().Name, clonedObj);


			PopulateObject<T, TData>(json, clonedbjWrap, wrap);
			//Fix stuff from original
			if (ObjUtils.GetId(gameObj) == ObjUtils.GetId(clonedObj))
			{
				int newID = ListUtils.NextFreeId<T>(list);
				ObjUtils.SetId(clonedObj, newID);
			}
			if (ListUtils.GetById<T>(list, ObjUtils.GetId(clonedObj)) != null)
			{
				logr.Error($"    ID conflict with {typeof(T).Name} ref: {wrap.refName} ID {ObjUtils.GetId(clonedObj)} is already in use. Skipping");
				return true;
			}
			ObjUtils.SetRef(clonedObj,wrap.refName);
			TryUpdateImages(clonedObj, wrap);
			showChangedFields<T>(gameObj, clonedObj);
			list .Add(clonedObj);
			return true;
		}
		private static bool TryCreateNew<T, TData>(List<T> list, Wrap<TData> wrap, string json)
		{
			logr.Log($"Creating a new object {typeof(T).Name}");


			T newObj = ObjUtils.CreateInstance<T>();

			Wrap<T> newObjWrap = new Wrap<T>(newObj.GetType().Name, newObj);
			PopulateObject<T, TData>(json, newObjWrap, wrap);
			if (ObjUtils.GetId(newObj) == 0)
			{
				int newID = ListUtils.NextFreeId<T>(list);
				ObjUtils.SetId(newObj, newID);
			}
			if (ListUtils.GetById<T>(list, ObjUtils.GetId(newObj)) != null)
			{
				logr.Error($"    ID conflict with {typeof(T).Name} ref: {wrap.refName} ID {ObjUtils.GetId(newObj)} is already in use. Skipping");
				return true;
			}
			ObjUtils.SetRef(newObj, wrap.refName);
			TryUpdateImages(newObj, wrap);
			list.Add(newObj);
			return true;
		}
		private static void PopulateObject<T, TData>(string json, Wrap<T> obj, Wrap<TData> wrap)
		{
			JsonConvert.PopulateObject(json, obj);
			switch (wrap.type)
			{
				case "Perk":
					ObjectApply.Apply<TData, T>(wrap.obj, obj.obj, "replacePerks");
					break;
			}
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
					logr.Log($"Updated {field.Name}: '{originalValue}' -> '{updatedValue}'");
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
