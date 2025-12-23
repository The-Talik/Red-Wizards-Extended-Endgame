using HarmonyLib;
using RW;
using RW.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
	using static RWMM.Logging;
namespace RWMM
{
	internal static partial class IdRefMap
	{
		internal static class Save
		{
			// Write the map right before the game serializes GameDataInfo
			[HarmonyPatch(typeof(GameData), "SaveGame")]
			static class Patch_GameData_SaveGame_refmap
			{
				[HarmonyPriority(Priority.Last)]
				static void Prefix()
				{
					logr.Open("IDRefMap saving to game data");
					SaveToGameData();
					logr.Close();
				}
			}
			internal static void SaveToGameData()
			{
				try
				{
					for (int i = 0; i < ManagedTypes.Count; i++)
					{
						var type = ManagedTypes[i];
						logr.Open("Making map list: " + type.Name);
						var map_list = GetMapList(type);
						MakeMapType(map_list, GetList(type));
						logr.Close($"Done making map list: {type.Name} ({map_list.Count})");
					}

					var json = JsonUtils.ToJson(Map);
					if (json == "{}")
						logr.Error($"EMPTY JSON: {json}");
					logr.Log("Saving JSON: " + (string.IsNullOrEmpty(json) ? "<empty>" : json.Substring(0, Math.Min(128, json.Length)) + (json.Length > 128 ? "..." : "")));

					SetSaveField(json);
				}
				catch (Exception ex)
				{
					logr.Warn("SaveToGameData failed: " + ex);
				}
			}

		}
		private static void SetSaveField(string json)
		{
			var fi = AccessTools.Field(typeof(GameDataInfo), "rweeItemMapJson");
			if (fi != null)
				fi.SetValue(GameData.data, json);
			else
				logr.Error("Save field 'rweeItemMapJson' not found (prepatcher missing?).");
		}
		static private void MakeMapType(List<IdRefMapJson.Pair> map_list, IList objects)
		{
			if (map_list == null || objects == null)
				return;

			map_list.Clear();

			for (int i = 0; i < objects.Count; i++)
			{
				var it = objects[i];
				if (it == null) continue;

				int id;
				string ref_name;

				try
				{
					id = ObjUtils.GetId(it);
					ref_name = ObjUtils.GetRef(it);
				}
				catch (Exception ex)
				{
					logr.Warn("MakeMapType: failed reading id/ref: " + ex.Message);
					continue;
				}

				if (string.IsNullOrEmpty(ref_name))
					continue;

				map_list.Add(new IdRefMapJson.Pair { Id = id, Name = ref_name });
			}
			LogMap(map_list,true);
		}
		static private List<IdRefMapJson.Pair> GetMapList(Type type)
		{
			var map_field = typeof(IdRefMapJson).GetField(type.Name, BindingFlags.Public | BindingFlags.Instance);
			if (map_field == null)
				throw new InvalidOperationException("No map list found for type: " + type.Name);

			return (List<IdRefMapJson.Pair>)map_field.GetValue(Map);
		}
	}
}
