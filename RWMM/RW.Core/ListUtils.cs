using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using static RW.Core.Logging;
using System.Collections;
namespace RW
{
	public static class ListUtils
	{
		public static T GetByRef<T>(List<T> list, string value)
		{
			string field = ObjUtils.RefField(typeof(T).Name);
			if (field == null)
				return default;
			return GetBy<T, string>(list, field, value);
		}
		public static T GetById<T>(List<T> list, int value)
		{
			string field = ObjUtils.IdField(typeof(T).Name);
			if (field == null)
				return default;
			return GetBy<T, int>(list, field, value);
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
			logr.Log($"[ListUtils.GetBy] searching {typeof(T).Name} for {field}='{searchStr}' (items={list.Count}).", 3);

			for (int idx = 0; idx < list.Count; idx++)
			{
				var item = list[idx];
				if (item == null)
				{
					logr.Log($"[ListUtils.GetBy] item[{idx}] is null, skipping.", 3);
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
						logr.Log($"[ListUtils.GetBy] item[{idx}] read property {type.FullName}.{field} -> {(memberValue == null ? "null" : memberValue.ToString())} (propType={prop.PropertyType.Name}).", 3);
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
							logr.Log($"[ListUtils.GetBy] item[{idx}] read field {type.FullName}.{field} -> {(memberValue == null ? "null" : memberValue.ToString())} (fieldType={fi.FieldType.Name}).", 3);
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
					logr.Log($"[ListUtils.GetBy] item[{idx}] {type.FullName}.{field} is null, skipping.", 3);
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
					logr.Log($"[ListUtils.GetBy] match in item[{idx}] {type.FullName}.{field} = '{memberStr}'.", 3);
					return item;
				}
				else
				{
					logr.Log($"[ListUtils.GetBy] item[{idx}] {type.FullName}.{field} ('{memberStr}') != search '{searchStr}'.", 3);
				}
			}

			logr.Log($"[ListUtils.GetBy] no match for {field}='{searchStr}' in list of {typeof(T).Name} (checked {list.Count} items).", 3);
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
					id = ObjUtils.GetId(item);
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
		public static List<T> Clone<T>(List<T> list)
		{
			if (list == null)
				return null;
			var clone = new List<T>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				clone.Add(ObjUtils.Clone(list[i]));
			}
			return clone;
		}
		public static List<TOut> Clone<TIn, TOut>(IEnumerable<TIn> source)
			where TOut : new()
		{
			logr.Log($"[ListUtils.Clone<{typeof(TIn).Name},{typeof(TOut).Name}>] Cloning from {typeof(TIn)} to {typeof(TOut)}.", 2);
			var result = new List<TOut>();
			if (source == null)
				return result;

			foreach (var item in source)
			{
				var dto = new TOut();
				ObjectApply.Apply(item, dto); // whatever you’re already doing
				result.Add(dto);
			}

			return result;
		}
		/*public static List<String> ToStrings<T>(List<T> list)
		{
			if (list == null)
				return null;
			var strList = new List<String>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				strList.Add(ObjUtils.GetRef(list[i]));
			}
			return strList;
		}*/
		public static List<string> ToStrings<T>(IEnumerable<T> items)
		{
			if (items == null)
				return null;

			var result = new List<string>();
			foreach (var item in items)
			{
				if (item == null)
					continue;

				// Use your existing ref helper, not ToString()
				var r = ObjUtils.GetRef(item, true);
				if (!string.IsNullOrEmpty(r))
					result.Add(r);
			}
			return result;
		}
		public static object GetByRef(IList list, Type type, string value)
		{
			if (type == null)
				return null;

			string field = ObjUtils.RefField(type.Name);
			if (field == null)
				return null;

			return GetBy(list, field, value);
		}

		public static object GetById(IList list, Type type, int value)
		{
			if (type == null)
				return null;

			string field = ObjUtils.IdField(type.Name);
			if (field == null)
				return null;

			return GetBy(list, field, value);
		}

		public static object GetBy(IList list, string field, object value)
		{
			if (list == null)
				return null;

			string search_str = value != null ? value.ToString() : string.Empty;

			for (int idx = 0; idx < list.Count; idx++)
			{
				var item = list[idx];
				if (item == null)
					continue;

				var item_type = item.GetType();

				object member_value = null;

				var prop = item_type.GetProperty(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (prop != null)
				{
					try { member_value = prop.GetValue(item, null); }
					catch { continue; }
				}
				else
				{
					var fi = item_type.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (fi == null)
						continue;

					try { member_value = fi.GetValue(item); }
					catch { continue; }
				}

				if (member_value == null)
					continue;

				string member_str;
				try { member_str = member_value.ToString(); }
				catch { continue; }

				if (string.Equals(member_str, search_str, StringComparison.Ordinal))
					return item;
			}

			return null;
		}

	}

}
