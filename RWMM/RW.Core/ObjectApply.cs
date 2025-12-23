using RW;
using RW.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using static RW.Core.Logging;
namespace RW
{

	public static class ObjectApply
	{
		public static void Apply<TData, TObject>(TData data, TObject obj, string field = null) // from, to
		{
			logr.Log($"[ObjectApply.Apply] Applying object of type {data.GetType()} to {obj.GetType()}", 2);
			if (data == null || obj == null)
			{
				logr.Warn($"Data or obj was null");
				return;
			}

			ApplyInternal((object)data, (object)obj, field);
			logr.Log($"[ObjectApply.Apply] Done", 2);
		}

		private static void ApplyInternal(object data, object obj, string field = null)
		{
			if (data == null || obj == null)
				return;

			var data_type = data.GetType();
			var obj_type = obj.GetType();

			// index source members by name
			var source = new Dictionary<string, Func<object>>(StringComparer.Ordinal);
			logr.Log($"[ObjectApply.ApplyInternal] Indexing source", 2);
			foreach (var f in data_type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				var f1 = f;
				source[f1.Name] = () => f1.GetValue(data);
			}

			// We intentionally ignore properties on the DTO side for now.

			logr.Log($"[ObjectApply.ApplyInternal] Setting target fields", 2);
			// set target fields
			foreach (var tf in obj_type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if(field != null && tf.Name != field)
					continue;
				if (tf.IsInitOnly) continue;
				if (tf.Name == "replacePerksRef")
				{
					//logr.Log("replacePerksRef");
					if (!source.TryGetValue("replacePerks", out var get_value))
						continue;
					var value = get_value();
					if (value is IEnumerable<Perk> perks)
					{
						var stringValues = ListUtils.ToStrings(perks); // infers <Perk>
						ApplyToField(obj, tf, stringValues);
					}
					continue;
				}
				if (tf.Name == "replacePerks")
				{
					logr.Log("replacePerks");
					if (!source.TryGetValue("replacePerksRef", out var get_value))
						continue;
					var value = get_value();
					if (value is IEnumerable<String> strings)
					{
						List<Perk> perks = new List<Perk>();
						foreach (var s in strings)
						{
							logr.Log($"Perk ref: {s}");
							var all_perk = PerkDB.GetAllPerks();
							Perk perk = ListUtils.GetByRef<Perk>(all_perk, s);
							if (perk != null)
							{
								perks.Add(perk);
							}
						}
						ApplyToField(obj, tf, perks);
					}
				}
				else
				{
					if (!source.TryGetValue(tf.Name, out var get_value))
						continue;


					var value = get_value();
					ApplyToField(obj, tf, value);
				}
			}

			// Properties on the target are currently unused, but keeping this for symmetry if we ever turn it back on.
				/*
				foreach (var tp in obj_type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				{
					if (!tp.CanWrite) continue;
					if (tp.GetIndexParameters().Length != 0) continue;

					if (!source.TryGetValue(tp.Name, out var get_value))
						continue;

					var value = get_value();
					ApplyToProperty(obj, tp, value);
				}
				*/
		}

		private static void ApplyToField(object target, FieldInfo tf, object value)
		{
			var target_type = tf.FieldType;

			// collections (arrays / lists) are full replace
			if (IsCollectionType(target_type))
			{
				logr.Log($"[ObjectApply.ApplyToField] Collection field {tf.Name} of type {target_type.Name}", 2);

				if (value == null)
				{
					logr.Log($"[ObjectApply.ApplyToField] Collection {tf.Name}: setting to null", 2);
					tf.SetValue(target, null);
					return;
				}

				if (TryConvertCollection(value, target_type, out var converted))
				{
					logr.Log($"[ObjectApply.ApplyToField] Collection {tf.Name}: converted and replaced", 2);
					tf.SetValue(target, converted);
				}
				else
				{
					logr.Error($"[ObjectApply.ApplyToField] Did not import collection {tf.Name} of type {target_type.Name}");
				}
				return;
			}

			bool is_simple = IsSimpleType(target_type);
			bool is_unity = IsUnityObject(target_type);

			// complex nested object (non-simple, non-Unity)
			if (!is_simple && !is_unity)
			{
				logr.Log($"[ObjectApply.ApplyToField] Complex object {tf.Name} of type {target_type.Name}", 2);
				var current = tf.GetValue(target);

				if (current == null)
				{
					// first: let TryConvert handle direct-assignable cases (DTO -> DTO, etc.)
					if (TryConvert(value, target_type, out var converted))
					{
						tf.SetValue(target, converted);
					}
					else if (value != null)
					{
						// otherwise, create a new instance and Apply into it
						object instance;
						if (target_type.IsValueType)
						{
							instance = Activator.CreateInstance(target_type);
						}
						else
						{
							var ctor = target_type.GetConstructor(Type.EmptyTypes);
							if (ctor != null)
							{
								instance = ctor.Invoke(null);
							}
							else
							{
								try
								{
									instance = FormatterServices.GetUninitializedObject(target_type);
								}
								catch (Exception ex)
								{
									logr.Error($"ObjectApply: cannot create instance of {target_type.Name} for field {tf.Name}: {ex.Message}");
									return;
								}
							}
						}

						ApplyNestedInternal(value, instance, target_type);
						tf.SetValue(target, instance);
					}
				}
				else
				{
					// mutate existing instance
					ApplyNestedInternal(value, current, target_type);

					// structs need to be written back after modification
					if (target_type.IsValueType && !target_type.IsPrimitive && !target_type.IsEnum)
					{
						tf.SetValue(target, current);
					}
				}
				return;
			}

			// scalar / Unity object path
			if (TryConvert(value, target_type, out var scalar_converted))
			{
				if (is_simple)
					logr.Log($"[ObjectApply.ApplyToField] Simple field {tf.Name} of type {target_type.Name}", 2);
				if (is_unity)
					logr.Log($"[ObjectApply.ApplyToField] Unity field {tf.Name} of type {target_type.Name}", 2);
				tf.SetValue(target, scalar_converted);
				return;
			}

			logr.Error($"Did not import {tf.Name} of type {target_type.Name}");
		}

		private static void ApplyToProperty(object target, PropertyInfo tp, object value)
		{
			var target_type = tp.PropertyType;

			// collections (arrays / lists) are full replace
			if (IsCollectionType(target_type))
			{
				if (value == null)
				{
					tp.SetValue(target, null, null);
					return;
				}

				if (TryConvertCollection(value, target_type, out var converted))
				{
					tp.SetValue(target, converted, null);
				}
				else
				{
					logr.Error($"Did not import collection property {tp.Name} of type {target_type.Name}");
				}
				return;
			}

			bool is_simple = IsSimpleType(target_type);
			bool is_unity = IsUnityObject(target_type);

			// complex nested object (non-simple, non-Unity)
			if (!is_simple && !is_unity)
			{
				var current = tp.GetValue(target, null);

				if (current == null)
				{
					if (TryConvert(value, target_type, out var converted))
					{
						tp.SetValue(target, converted, null);
					}
				}
				else
				{
					ApplyNestedInternal(value, current, target_type);

					// structs need to be written back after modification
					if (target_type.IsValueType && !target_type.IsPrimitive && !target_type.IsEnum)
					{
						tp.SetValue(target, current, null);
					}
				}
				return;
			}

			// scalar / Unity object path
			if (TryConvert(value, target_type, out var scalar_converted))
			{
				tp.SetValue(target, scalar_converted, null);
			}
		}

		private static void ApplyNestedInternal(object data, object target, Type target_type)
		{
			if (data == null || target == null)
				return;

			// Use runtime types; ApplyInternal will reflect over them.
			ApplyInternal(data, target);
		}

		private static bool IsSimpleType(Type type)
		{
			if (type.IsPrimitive || type.IsEnum)
				return true;

			if (type == typeof(string) || type == typeof(decimal))
				return true;

			return false;
		}

		private static bool IsUnityObject(Type type)
		{
			return typeof(UnityEngine.Object).IsAssignableFrom(type);
		}

		private static bool IsCollectionType(Type type)
		{
			if (type.IsArray)
				return true;

			if (typeof(System.Collections.IList).IsAssignableFrom(type))
				return true;

			return false;
		}

		/// <summary>
		/// Convert a DTO collection (arrays/lists) into the target collection type,
		/// mapping each element (including complex nested objects) as needed.
		/// </summary>
		private static bool TryConvertCollection(object value, Type target_type, out object result)
		{
			result = null;

			if (value == null)
			{
				// caller handles explicit null
				return true;
			}

			if (!(value is System.Collections.IEnumerable enumerable))
			{
				return false;
			}

			// Array target
			if (target_type.IsArray)
			{
				var element_type = target_type.GetElementType();
				if (element_type == null)
					return false;

				var temp = new List<object>();
				foreach (var item in enumerable)
				{
					if (!ConvertElement(item, element_type, out var converted_item))
						continue;

					temp.Add(converted_item);
				}

				var array = Array.CreateInstance(element_type, temp.Count);
				for (int i = 0; i < temp.Count; i++)
				{
					array.SetValue(temp[i], i);
				}

				result = array;
				return true;
			}

			// IList / List<T> target
			if (typeof(System.Collections.IList).IsAssignableFrom(target_type))
			{
				var list = (System.Collections.IList)Activator.CreateInstance(target_type);
				var element_type = typeof(object);

				if (target_type.IsGenericType)
				{
					var args = target_type.GetGenericArguments();
					if (args.Length == 1)
						element_type = args[0];
				}

				foreach (var item in enumerable)
				{
					if (!ConvertElement(item, element_type, out var converted_item))
						continue;

					list.Add(converted_item);
				}

				result = list;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Convert a single element from DTO-side to target element type,
		/// including deep-merge for complex nested objects.
		/// </summary>
		private static bool ConvertElement(object value, Type element_type, out object out_value)
		{
			out_value = null;

			if (value == null)
			{
				// null OK for reference types and Nullable<T>
				if (!element_type.IsValueType || Nullable.GetUnderlyingType(element_type) != null)
				{
					out_value = null;
					return true;
				}
				return false;
			}

			var value_type = value.GetType();

			// Already assignable
			if (element_type.IsAssignableFrom(value_type))
			{
				out_value = value;
				return true;
			}

			bool is_simple = IsSimpleType(element_type);
			bool is_unity = IsUnityObject(element_type);

			// Complex nested object in a collection: create new instance and Apply into it
			if (!is_simple && !is_unity)
			{
				object instance;

				if (element_type.IsValueType)
				{
					// structs always have a default "ctor"
					instance = Activator.CreateInstance(element_type);
				}
				else
				{
					// try normal parameterless ctor first
					var ctor = element_type.GetConstructor(Type.EmptyTypes);
					if (ctor != null)
					{
						instance = ctor.Invoke(null);
					}
					else
					{
						// fallback: create without running any ctor (Unity-style)
						try
						{
							instance = FormatterServices.GetUninitializedObject(element_type);
						}
						catch (Exception ex)
						{
							logr.Error($"ConvertElement: cannot create instance of {element_type.Name}: {ex.Message}");
							return false;
						}
					}
				}

				ApplyNestedInternal(value, instance, element_type);
				out_value = instance;
				return true;
			}

			// Simple / Unity types: fall back to scalar conversion
			return TryConvert(value, element_type, out out_value);
		}

		private static bool TryConvert(object value, Type target_type, out object out_value)
		{
			out_value = null;

			if (value == null)
			{
				// null is OK for reference types and Nullable<T>
				if (!target_type.IsValueType || (Nullable.GetUnderlyingType(target_type) != null))
					return true;

				return false;
			}

			var value_type = value.GetType();

			// already assignable
			if (target_type.IsAssignableFrom(value_type))
			{
				out_value = value;
				return true;
			}

			// Nullable<T>
			var underlying = Nullable.GetUnderlyingType(target_type);
			if (underlying != null)
			{
				if (!TryConvert(value, underlying, out var tmp))
					return false;

				out_value = tmp;
				return true;
			}

			// Enums: support both string names and numeric values
			if (target_type.IsEnum)
			{
				try
				{
					// string → enum (e.g. "Legendary")
					if (value is string s)
					{
						out_value = Enum.Parse(target_type, s, ignoreCase: true);
						return true;
					}

					// numeric → enum (DTOGen emits enums as int)
					var underlying_enum_type = Enum.GetUnderlyingType(target_type);
					var numeric = Convert.ChangeType(value, underlying_enum_type, CultureInfo.InvariantCulture);
					out_value = Enum.ToObject(target_type, numeric);
					return true;
				}
				catch
				{
					return false;
				}
			}

			// numeric conversions / other IConvertible
			try
			{
				out_value = Convert.ChangeType(value, target_type, CultureInfo.InvariantCulture);
				return true;
			}
			catch
			{
			}

			return false;
		}

	}

}
