using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RW.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using static RW.Core.Logging;
namespace RW
{
	public static partial class JsonUtils
	{

/*		public static object AlternativeFromJson<T>(string json)
		{
			if (string.IsNullOrEmpty(json) || typeof(T) == null)
				return null;

			var serializer = new DataContractJsonSerializer(typeof(T),
				new DataContractJsonSerializerSettings
				{
					UseSimpleDictionaryFormat = true
				});

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
			{
				return serializer.ReadObject(ms);
			}
		}*/
/*		public static void Populate(object obj, string json)
		{
			JsonConvert.PopulateObject(json, obj);
		}*/
/*		public static T PopulateClone<T>(T obj, string json)
		{
			var clone = ObjUtils.Clone(obj);
			JsonConvert.PopulateObject(json, clone);
			return clone;
		}*/

		static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			Formatting = Newtonsoft.Json.Formatting.Indented,
			TypeNameHandling = TypeNameHandling.None,
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			NullValueHandling = NullValueHandling.Include,
			// Enums as strings, but still allow numeric values on read
			Converters =
			{
				new StringEnumConverter
				{
					AllowIntegerValues = true
				}
			}
		};

		public static string ToJson<T>(T obj)
		{
			if (obj == null)
				return string.Empty;

			try
			{
				return JsonConvert.SerializeObject(obj, JsonSettings);
			}
			catch (System.Exception ex)
			{
				logr.Warn($"JsonUtils.ToJson<{typeof(T).Name}> failed: {ex.Message}");
				throw;
			}
		}

/*		public static string ToJsonOld<T>(T obj)
		{
			if (obj == null)
				return string.Empty;
			var known_types = new List<Type>();


			// Patch for wrapped types from RWMM.Plugin.ResourceDump
			var wrap_type = obj.GetType();
			var field = wrap_type.GetField("obj",
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic);
			if (field != null)
			{
				Type inner_type = field.FieldType; // type of obj.obj
																					 // or instance:
				var inner_value = field.GetValue(obj);
				var inner_runtime_type = inner_value?.GetType();

				switch (inner_runtime_type.Name)
				{
					case "ShipModelData":
						known_types.Add(typeof(SB_WeaponDmg));
						known_types.Add(typeof(UnityEngine.Transform));
						break;
				}
			}
			var settings = new DataContractJsonSerializerSettings
			{
				KnownTypes = known_types,
				EmitTypeInformation = EmitTypeInformation.Never
				// UseSimpleDictionaryFormat = true,
			};
			var serializer = new DataContractJsonSerializer(typeof(List<T>), settings);
			using (var ms = new MemoryStream())
			{
				serializer.WriteObject(ms, obj);
				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}*/



		public static T FromJson<T>(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return default;
			return JsonConvert.DeserializeObject<T>(json, JsonSettings);
			/*		logr.Log("A");
					// create an empty instance of T and let PopulateObject fill it
					var obj = (T)Activator.CreateInstance(typeof(T));
					logr.Log("B");
					JsonConvert.PopulateObject(json, obj, JsonSettings);
					return obj;*/
		}

		/*		public static T FromJson<T>(string json)
				{
					if (string.IsNullOrEmpty(json))
						return default;
					var serializer = new DataContractJsonSerializer(typeof(T));
					using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
					{
						var obj = serializer.ReadObject(ms);
						return (T)obj;
					}
				}*/
		public static string ToPrettyJson(object obj)
		{
			return Pretty(ToJson(obj));
		}
		public static string Pretty(string json)
		{
			if (string.IsNullOrEmpty(json))
				return json;

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
			using (var reader = JsonReaderWriterFactory.CreateJsonReader(ms, new XmlDictionaryReaderQuotas()))
			{
				var doc = new XmlDocument();
				doc.Load(reader);

				using (var out_ms = new MemoryStream())
				using (var writer = JsonReaderWriterFactory.CreateJsonWriter(out_ms, Encoding.UTF8, false, true, "\t"))
				{
					doc.Save(writer);
					writer.Flush();
					return Encoding.UTF8.GetString(out_ms.ToArray());
				}
			}
		}
	}
}
