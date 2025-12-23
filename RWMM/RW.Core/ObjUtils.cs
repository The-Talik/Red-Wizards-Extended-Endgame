using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RW.Core.Logging;
namespace RW
{
	public static class ObjUtils
	{
		/*
		 * usage: var myField = ObjUtils.GetField<string>(Item, "refName");
		 */
		public static string GetRef(object obj, bool silent = false)
		{
			if (obj == null)
			{
				if (!silent)
					logr.Warn("ObjUtils.GetRef called with null object.");
				return default;
			}
			string field = RefField(obj.GetType().Name);
			if (field == null)
				return default;

			return GetRefWithErrors(obj, field, silent);
		}
		public static string RefField(string typeName)
		{
			typeName = typeName.TrimStart('_');
			switch (typeName)
			{
				case "Item":
				case "Equipment":
				case "TWeapon":
				case "CrewMember":
				case "Perk":
				case "ShipModelData":
					return "refName";
				case "Quest":
					return "nameRef";
				case "Pair":
					return "Name";
				case "InstalledEquipment":
					return null;
				default:
					logr.Error($"ObjUtils.refField({typeName}) not implemented.");
					return null;
			}
		}
		public static int GetId(object obj, bool silent = false)
		{
			if (obj == null)
			{
				if (!silent)
					logr.Warn("ObjUtils.GetId called with null object.");
				return default;
			}
			string field = IdField(obj.GetType().Name);
			if (field == null)
				return default;

			return GetField<int>(obj, field);
		}
		public static string IdField(string typeName)
		{
			typeName = typeName.TrimStart('_');
			switch (typeName)
			{
				case "TWeapon":
					return "index";
				case "Quest":
					return "refCode";
				case "Item":
				case "Equipment":

				case "CrewMember":
				case "Perk":
				case "ShipModelData":
					return "id";
				case "Pair":
					return "Id";
				case "InstalledEquipment":
					return "equipmentID";
				default:
					logr.Error($"ObjUtils.IdField<{typeName}> not implemented.");
					return null;
			}
		}
		public static string SpriteField(string typeName)
		{
			typeName = typeName.TrimStart('_');
			switch (typeName)
			{
				case "Item":
				case "Equipment":
					return "sprite";


				/*case "TWeapon":
				case "CrewMember":
				case "Perk":
					return "refName";
				case "Quest":
					return "nameRef";
				case "ShipModelData":
					return "shipModelName";*/
				default:
					//logr.Error($"ObjUtils.refField<{typeName}> not implemented.");
					return null;
			}
		}

		private static string GetRefWithErrors(object obj, string field, bool silent = false)
		{
			var v = GetField<string>(obj, field);
			if (!silent)
			{
				if (string.IsNullOrEmpty(v))
					logr.Warn($"ObjUtils.GetRef returned null/empty for {obj.GetType().Name} (field '{field}') looking for {field}.");
				else
					logr.Log($"ObjUtils.GetRef: {obj.GetType().Name}.refName = '{v}'", 3);
			}
			return v;
		}
		public static void SetRef(object obj, string value)
		{
			if (obj == null)
			{
				logr.Warn("ObjUtils.SetRef called with null object.");
				return;
			}

			logr.Log($"ObjUtils.SetRef: targetType={obj.GetType().FullName} field={RefField(obj.GetType().Name)} value='{value}'", 3);
			string field = RefField(obj.GetType().Name);
			if (field == null)
				return;

			SetField<string>(obj, field, value);
		}
		public static void SetId(object obj, int value)
		{
			if (obj == null)
			{
				logr.Warn("ObjUtils.SetId called with null object.");
				return;
			}

			logr.Log($"ObjUtils.SetRef: targetType={obj.GetType().FullName} field={IdField(obj.GetType().Name)} value='{value}'", 3);
			string field = IdField(obj.GetType().Name);
			if (field == null)
				return;

			SetField<int>(obj, field, value);
		}
		private static string IdReferenceField(string typeName)
		{
			switch (typeName)
			{
				case "CargoItem":
				case "ItemStockData":
				case "MarketItem":
					return "itemID";

				case "InstalledEquipment":
				case "BuiltInEquipmentData":
					return "equipmentID";
				case "EquipedWeapon":
					return "weaponIndex";
				case "AssignedCrewMember":
					return "crewMemberID";
				case "TWeapon":
					return "index";
				case "ShipModelData":
					return "id";
				default:
					logr.Error($"[ObjUtils.IdReferenceField]<{typeName}> not implemented.");
					return null;
					break;
			}
		}
		public static int GetIdReference(object obj, bool silent = false)
		{
			if (obj == null)
			{
				if (!silent)
					logr.Warn("[ObjUtils.GetRef] called with null object.");
				return -1;
			}
			var field = IdReferenceField(obj.GetType().Name);
			if (field == null)
				return -1;
			return ObjUtils.GetField<int>(obj, field, silent);
		}
		public static void SetIdReference(object obj, int id)
		{
			if (obj == null)
			{
				logr.Warn("[ObjUtils.SetRef] called with null object.");
			}
			var field = IdReferenceField(obj.GetType().Name);
			if (field == null)
				return;

			ObjUtils.SetField<int>(obj, field, id);
			
		}
		public static T GetField<T>(object obj, string field, bool silent = false)
		{
			if (obj == null || string.IsNullOrEmpty(field))
			{
				if (!silent)
					logr.Warn($"[ObjUtils.GetField]<{typeof(T).Name}> called with null object or empty field name.");
				return default;
			}

			var type = obj.GetType();

			object value = null;

			// Try property first
			var prop = type.GetProperty(field);
			if (prop != null)
			{
				try
				{
					value = prop.GetValue(obj, null);
					if (!silent)
						logr.Log($"[ObjUtils.GetField] found property {type.FullName}.{field} (prop type={prop.PropertyType.Name}), value={(value == null ? "null" : value.ToString())}", 3);
				}
				catch (Exception ex)
				{
					logr.Error($"[ObjUtils.GetField] exception reading property {type.FullName}.{field}: {ex}");
					return default;
				}
			}
			else
			{
				// Fallback to field
				var fi = type.GetField(field);
				if (fi != null)
				{
					try
					{
						value = fi.GetValue(obj);
						if (!silent)
							logr.Log($"[ObjUtils.GetField] found field {type.FullName}.{field} (field type={fi.FieldType.Name}), value={(value == null ? "null" : value.ToString())}", 3);
					}
					catch (Exception ex)
					{
						logr.Error($"[ObjUtils.GetField] exception reading field {type.FullName}.{field}: {ex}");
						return default;
					}
				}
				else
				{
					if (!silent)
						logr.Warn($"[ObjUtils.GetField] neither property nor field '{field}' found on type {type.FullName}.");
					return default;
				}
			}

			if (value == null)
			{
				return default;
			}

			// If it's already the right type, just return it
			if (value is T t_value)
			{
				return t_value;
			}

			try
			{
				// Handle things like short → int, byte → int, etc.
				return (T)System.Convert.ChangeType(value, typeof(T));
			}
			catch (System.Exception ex) when (
				ex is System.InvalidCastException ||
				ex is System.FormatException ||
				ex is System.OverflowException
			)
			{
				logr.Warn(
					$"ObjUtils.GetField<{typeof(T).Name}> failed for {type.FullName}.{field} (actual type {value.GetType().FullName}); returning default."
				);
				return default;
			}
		}

		public static void SetField<T>(object obj, string field, T value)
		{
			if (obj == null || string.IsNullOrEmpty(field))
			{
				logr.Warn("ObjUtils.SetField called with null object or empty field name.");
				return;
			}

			var type = obj.GetType();
			logr.Log($"ObjUtils.SetField: objType={type.FullName} field={field} valueType={typeof(T).Name} value={(value == null ? "null" : value.ToString())}", 3);

			// Try property first
			var prop = type.GetProperty(field);
			if (prop != null && prop.CanWrite)
			{
				try
				{
					var targetType = prop.PropertyType;

					// If value is already assignable
					if (value == null)
					{
						prop.SetValue(obj, null, null);
						logr.Log($"ObjUtils.SetField: set property {type.FullName}.{field} = null", 3);
						return;
					}

					if (targetType.IsAssignableFrom(value.GetType()))
					{
						prop.SetValue(obj, value, null);
						logr.Log($"ObjUtils.SetField: set property {type.FullName}.{field} = '{value}' (direct assign)", 3);
						return;
					}

					// Special case UnityEngine.Object derived types: try direct cast
					if (typeof(UnityEngine.Object).IsAssignableFrom(targetType) && value is UnityEngine.Object)
					{
						prop.SetValue(obj, value, null);
						logr.Log($"ObjUtils.SetField: set property {type.FullName}.{field} = UnityEngine.Object '{value}'", 3);
						return;
					}

					// Try conversion for primitives and simple types
					var converted = System.Convert.ChangeType(value, targetType);
					prop.SetValue(obj, converted, null);
					logr.Log($"ObjUtils.SetField: set property {type.FullName}.{field} = '{converted}' (converted)", 3);
					return;
				}
				catch (Exception ex)
				{
					logr.Warn($"ObjUtils.SetField: failed to set property {type.FullName}.{field}: {ex.Message}");
					return;
				}
			}

			// Fallback to field
			var fi = type.GetField(field);
			if (fi != null && fi.FieldType == typeof(T))
			{
				fi.SetValue(obj, value);
			}
		}
		/*
		 * unity only
		public static T Clone<T>(T obj)
		 where T : UnityEngine.Object
		{
			if (obj == null)
			{
				return null;
			}

			return UnityEngine.Object.Instantiate(obj);
		}*/
		public static T Clone<T>(T obj)
		{
			// Unity types
			if (obj is UnityEngine.Object unity_obj)
			{
				logr.Log($"ObjUtils.Clone: cloning UnityEngine.Object of type {unity_obj.GetType().FullName}", 3);
				return (T)(object)UnityEngine.Object.Instantiate(unity_obj);
			}

			// Reference-type, non-Unity
			if (obj != null && !typeof(T).IsValueType)
			{
				var type = obj.GetType();
				var method = type.GetMethod("MemberwiseClone",
						BindingFlags.Instance | BindingFlags.NonPublic);

				if (method != null)
				{
					logr.Log($"ObjUtils.Clone: MemberwiseClone on {type.FullName}", 3);
					return (T)method.Invoke(obj, null);
				}
			}

			// Value types (like Quest) or null: just return
			// - structs are already copied by value
			// - null stays null
			return obj;
		}
		public static T CreateInstance<T>()
		{
			var type = typeof(T);

			// Unity ScriptableObject types
			if (typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type))
				return (T)(object)UnityEngine.ScriptableObject.CreateInstance(type);

			// Normal class with parameterless ctor
			var ctor = type.GetConstructor(Type.EmptyTypes);
			if (ctor != null)
				return (T)ctor.Invoke(null);

			// Last-resort fallback for pure data containers
			return (T)System.Runtime.Serialization.FormatterServices
				.GetUninitializedObject(type);
		}
	}
}
