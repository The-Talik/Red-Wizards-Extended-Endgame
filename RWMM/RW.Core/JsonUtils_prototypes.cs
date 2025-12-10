using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RW
{
	// NOTE: change your existing JsonUtil declaration to:
	// public static partial class JsonUtil
	public static partial class JsonUtils
	{
		// Object -> formatted JSON (shallow: scalars + arrays of scalars only)
		public static string ToSimpleJson(object obj, bool omit_nulls = true)
		{
			var sb = new StringBuilder(32 * 1024);
			write_value(sb, obj, 0, omit_nulls);
			sb.Append("\n");
			return sb.ToString();
		}

		// List -> formatted JSON (each element dumped shallow)
		public static string ToSimpleJsonList<T>(IEnumerable<T> list, bool omit_nulls = true)
		{
			var sb = new StringBuilder(64 * 1024);
			write_array(sb, (IEnumerable)list, 0, omit_nulls);
			sb.Append("\n");
			return sb.ToString();
		}

		private static void write_value(StringBuilder sb, object value, int depth, bool omit_nulls)
		{
			if (value == null)
			{
				sb.Append("null");
				return;
			}

			var t = value.GetType();

			if (t.IsEnum)
			{
				write_string(sb, value.ToString());
				return;
			}

			if (value is string)
			{
				write_string(sb, (string)value);
				return;
			}

			if (value is bool)
			{
				sb.Append(((bool)value) ? "true" : "false");
				return;
			}

			if (is_number_type(t))
			{
				sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
				return;
			}

			if (value is DateTime)
			{
				write_string(sb, ((DateTime)value).ToString("o", CultureInfo.InvariantCulture));
				return;
			}

			// Dictionary<string, scalar|array-of-scalar>
			if (value is IDictionary dict)
			{
				write_dictionary(sb, dict, depth, omit_nulls);
				return;
			}

			// IEnumerable of scalars
			if (value is IEnumerable en && !(value is string))
			{
				if (is_scalar_enumerable(value))
				{
					write_array(sb, en, depth, omit_nulls);
					return;
				}

				// not scalar list -> don't recurse
				sb.Append("null");
				return;
			}

			// object -> only simple members (no sub-objects)
			write_object_shallow(sb, value, depth, omit_nulls);
		}

		private static void write_object_shallow(StringBuilder sb, object obj, int depth, bool omit_nulls)
		{
			var t = obj.GetType();

			sb.Append("{");

			var members = new List<MemberInfo>();

			foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				if (f.IsNotSerialized) continue;
				if (!is_simple_member_type(f.FieldType)) continue;
				members.Add(f);
			}

			foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (!p.CanRead) continue;
				if (p.GetIndexParameters().Length != 0) continue;
				if (!is_simple_member_type(p.PropertyType)) continue;
				members.Add(p);
			}

			members = members.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();

			bool first = true;

			for (int i = 0; i < members.Count; i++)
			{
				var m = members[i];

				object v = null;
				try { v = get_member_value(m, obj); }
				catch { continue; }

				if (omit_nulls && v == null)
					continue;

				if (first)
				{
					sb.Append("\n");
					first = false;
				}
				else
				{
					sb.Append(",\n");
				}

				write_indent(sb, depth + 1);
				write_string(sb, m.Name);
				sb.Append(": ");
				write_value(sb, v, depth + 1, omit_nulls);
			}

			if (!first)
			{
				sb.Append("\n");
				write_indent(sb, depth);
			}

			sb.Append("}");
		}

		private static void write_dictionary(StringBuilder sb, IDictionary dict, int depth, bool omit_nulls)
		{
			sb.Append("{");

			// only string keys
			var keys = new List<string>();
			var map = new Dictionary<string, object>(StringComparer.Ordinal);

			foreach (DictionaryEntry kv in dict)
			{
				if (!(kv.Key is string)) continue;

				var k = (string)kv.Key;
				if (string.IsNullOrWhiteSpace(k)) continue;

				if (!map.ContainsKey(k))
				{
					map[k] = kv.Value;
					keys.Add(k);
				}
			}

			keys.Sort(StringComparer.Ordinal);

			bool first = true;

			for (int i = 0; i < keys.Count; i++)
			{
				var k = keys[i];
				var v = map[k];

				// only scalar or scalar-enumerable
				if (v != null)
				{
					var vt = v.GetType();
					if (!is_simple_scalar_type(vt) && !(v is IEnumerable && !(v is string) && is_scalar_enumerable(v)))
						continue;
				}
				else if (omit_nulls)
				{
					continue;
				}

				if (first)
				{
					sb.Append("\n");
					first = false;
				}
				else
				{
					sb.Append(",\n");
				}

				write_indent(sb, depth + 1);
				write_string(sb, k);
				sb.Append(": ");
				write_value(sb, v, depth + 1, omit_nulls);
			}

			if (!first)
			{
				sb.Append("\n");
				write_indent(sb, depth);
			}

			sb.Append("}");
		}

		private static void write_array(StringBuilder sb, IEnumerable en, int depth, bool omit_nulls)
		{
			sb.Append("[");

			bool first = true;

			foreach (var it in en)
			{
				if (omit_nulls && it == null)
					continue;

				if (first)
				{
					sb.Append("\n");
					first = false;
				}
				else
				{
					sb.Append(",\n");
				}

				write_indent(sb, depth + 1);
				write_value(sb, it, depth + 1, omit_nulls);
			}

			if (!first)
			{
				sb.Append("\n");
				write_indent(sb, depth);
			}

			sb.Append("]");
		}

		private static object get_member_value(MemberInfo m, object obj)
		{
			var f = m as FieldInfo;
			if (f != null) return f.GetValue(obj);

			var p = m as PropertyInfo;
			if (p != null) return p.GetValue(obj, null);

			return null;
		}

		private static void write_indent(StringBuilder sb, int depth)
		{
			for (int i = 0; i < depth; i++)
				sb.Append("\t");
		}

		private static void write_string(StringBuilder sb, string s)
		{
			if (s == null)
			{
				sb.Append("null");
				return;
			}

			sb.Append('\"');

			for (int i = 0; i < s.Length; i++)
			{
				var c = s[i];

				switch (c)
				{
					case '\\': sb.Append("\\\\"); break;
					case '\"': sb.Append("\\\""); break;
					case '\b': sb.Append("\\b"); break;
					case '\f': sb.Append("\\f"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < 32)
						{
							sb.Append("\\u");
							sb.Append(((int)c).ToString("x4"));
						}
						else
						{
							sb.Append(c);
						}
						break;
				}
			}

			sb.Append('\"');
		}

		private static bool is_number_type(Type t)
		{
			return t == typeof(byte) || t == typeof(sbyte) ||
				t == typeof(short) || t == typeof(ushort) ||
				t == typeof(int) || t == typeof(uint) ||
				t == typeof(long) || t == typeof(ulong) ||
				t == typeof(float) || t == typeof(double) ||
				t == typeof(decimal);
		}

		private static bool is_simple_scalar_type(Type t)
		{
			if (t == null) return false;
			if (t.IsEnum) return true;

			return t == typeof(string) ||
				t == typeof(bool) ||
				is_number_type(t) ||
				t == typeof(DateTime);
		}

		private static bool is_simple_member_type(Type t)
		{
			if (is_simple_scalar_type(t)) return true;

			var elem = try_get_enumerable_element_type(t);
			if (elem != null && is_simple_scalar_type(elem)) return true;

			return false;
		}

		private static bool is_scalar_enumerable(object value)
		{
			if (value == null) return false;

			// If we can infer element type and it’s scalar, accept.
			var elem = try_get_enumerable_element_type(value.GetType());
			if (elem != null)
				return is_simple_scalar_type(elem);

			// Otherwise, do a light runtime check (first few items).
			var en = value as IEnumerable;
			if (en == null) return false;

			int checked_count = 0;

			foreach (var it in en)
			{
				if (it == null) continue;

				if (!is_simple_scalar_type(it.GetType()))
					return false;

				checked_count++;
				if (checked_count >= 8)
					break;
			}

			return true;
		}

		private static Type try_get_enumerable_element_type(Type t)
		{
			if (t == null) return null;

			if (t.IsArray)
				return t.GetElementType();

			if (t.IsGenericType)
			{
				var def = t.GetGenericTypeDefinition();

				if (def == typeof(List<>) || def == typeof(IList<>) || def == typeof(IEnumerable<>))
					return t.GetGenericArguments()[0];
			}

			var ifaces = t.GetInterfaces();

			for (int i = 0; i < ifaces.Length; i++)
			{
				var it = ifaces[i];
				if (!it.IsGenericType) continue;

				if (it.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					return it.GetGenericArguments()[0];
			}

			return null;
		}
	}
}
