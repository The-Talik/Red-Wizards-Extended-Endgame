using System;
using System.Collections.Generic;
using System.Reflection;
using static RW.Core.Logging;
namespace RW
{
	public static class ListUtils
	{
		public static T GetByRef<T>(List<T> list, string value)
		{
			string field = ObjUtils.RefField(typeof(T).Name);
			if (field == null)
				return default(T);
			return GetBy<T, string>(list, field, value);
		}

		public static T GetBy<T>(List<T> list, string field, int value)
		{
			return GetBy<T, int>(list, field, value);
		}

		public static T GetBy<T>(List<T> list, string field, string value)
		{
			return GetBy<T, string>(list, field, value);
		}

		public static T GetBy<T, TField>(List<T> list, string field, TField value)
		{
			if (list == null)
			{
				logr.Warn($"[ListUtils.GetBy] called with null list for type {typeof(T).Name}.");
				return default(T);
			}

			string searchStr = value?.ToString() ?? string.Empty;
			logr.Log($"[ListUtils.GetBy] searching {typeof(T).Name} for {field}='{searchStr}' (items={list.Count}).",3);

			for (int idx = 0; idx < list.Count; idx++)
			{
				var item = list[idx];
				if (item == null)
				{
					logr.Log($"[ListUtils.GetBy] item[{idx}] is null, skipping.",3);
					continue;
				}

				var type = item.GetType();

				// Try property first (include non-public to be resilient)
				PropertyInfo prop = type.GetProperty(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				FieldInfo fi = null;
				object memberValue = null;

				if (prop != null)
				{
					try
					{
						memberValue = prop.GetValue(item, null);
						logr.Log($"[ListUtils.GetBy] item[{idx}] read property {type.FullName}.{field} -> {(memberValue == null ? "null" : memberValue.ToString())} (propType={prop.PropertyType.Name}).",3);
					}
					catch (Exception ex)
					{
						logr.Error($"[ListUtils.GetBy] exception reading property {type.FullName}.{field} on item[{idx}]: {ex}");
						continue;
					}
				}
				else
				{
					// Fallback to field
					fi = type.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (fi != null)
					{
						try
						{
							memberValue = fi.GetValue(item);
							logr.Log($"[ListUtils.GetBy] item[{idx}] read field {type.FullName}.{field} -> {(memberValue == null ? "null" : memberValue.ToString())} (fieldType={fi.FieldType.Name}).",3);
						}
						catch (Exception ex)
						{
							logr.Error($"[ListUtils.GetBy] exception reading field {type.FullName}.{field} on item[{idx}]: {ex}");
							continue;
						}
					}
					else
					{
						// Neither property nor field found; warn and skip this item
						logr.Warn($"[ListUtils.GetBy] type {type.FullName} has no property/field '{field}'; skipping item[{idx}].");
						continue;
					}
				}

				if (memberValue == null)
				{
					logr.Log($"[ListUtils.GetBy] item[{idx}] {type.FullName}.{field} is null, skipping.",3);
					continue;
				}

				string memberStr;
				try
				{
					memberStr = memberValue.ToString();
				}
				catch (Exception ex)
				{
					logr.Warn($"[ListUtils.GetBy] ToString() failed for {type.FullName}.{field} on item[{idx}]: {ex.Message}; skipping.");
					continue;
				}

				if (string.Equals(memberStr, searchStr, StringComparison.Ordinal))
				{
					logr.Log($"[ListUtils.GetBy] match in item[{idx}] {type.FullName}.{field} = '{memberStr}'.",3);
					return item;
				}
				else
				{
					logr.Log($"[ListUtils.GetBy] item[{idx}] {type.FullName}.{field} ('{memberStr}') != search '{searchStr}'.",3);
				}
			}

			logr.Log($"[ListUtils.GetBy] no match for {field}='{searchStr}' in list of {typeof(T).Name} (checked {list.Count} items).",3);
			return default(T);
		}

		public static int NextFreeId<T>(List<T> list)
		{
			if (list == null || list.Count == 0)
				return 0;

			var used_ids = new HashSet<int>();

			for (int i = 0; i < list.Count; i++)
			{
				var item = list[i];
				if (item == null)
				{
					logr.Log($"[ListUtils.NextFreeId] item[{i}] is null, skipping.", 3);
					continue;
				}

				int id;
				try
				{
					id = ObjUtils.GetField<int>(item, "id");
				}
				catch (Exception ex)
				{
					logr.Warn($"[ListUtils.NextFreeId] failed to read id on item[{i}] (type={item.GetType().FullName}): {ex.Message}");
					continue;
				}

				if (id >= 0)
					used_ids.Add(id);
				else
					logr.Log($"[ListUtils.NextFreeId] item[{i}] has invalid id '{id}' (type={item.GetType().FullName}).", 3);
			}

			int candidate = 1000;
			while (used_ids.Contains(candidate))
				candidate++;

			logr.Log($"[ListUtils.NextFreeId] returning {candidate} after scanning {used_ids.Count} used ids.", 3);
			return candidate;
		}
	}
}
