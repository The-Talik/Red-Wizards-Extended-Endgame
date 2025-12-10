using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
namespace RW
{
	public static partial class JsonUtils
	{
		/*		public static string ToJson<T>(T obj) => ToJson((object)obj, typeof(T));

				public static T FromJson<T>(string json)
				{
					var obj = FromJson(json, typeof(T));
					return obj == null ? default : (T)obj;
				}

				public static string ToJson(object obj, Type type)
				{
					if (obj == null || type == null)
						return string.Empty;

					var serializer = new DataContractJsonSerializer(type,
						new DataContractJsonSerializerSettings
						{
							UseSimpleDictionaryFormat = true
						});

					using (var ms = new MemoryStream())
					{
						serializer.WriteObject(ms, obj);
						return Encoding.UTF8.GetString(ms.ToArray());
					}
				}
		*/
		public static object AlternativeFromJson<T>(string json)
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
		}
		public static void Populate(object obj, string json)
		{
			JsonConvert.PopulateObject(json, obj);
		}
		public static T PopulateClone<T>(T obj, string json)
		{
			var clone = ObjUtils.Clone(obj);
			JsonConvert.PopulateObject(json, clone);
			return clone;
		}


		public static string ToJson<T>(T obj)
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
				// UseSimpleDictionaryFormat = true,  // if you were using this before
			};
			var serializer = new DataContractJsonSerializer(typeof(List<T>), settings);
			using (var ms = new MemoryStream())
			{
				serializer.WriteObject(ms, obj);
				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}


		static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.None,
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
			NullValueHandling = NullValueHandling.Include
		};

		public static T FromJson<T>(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return default;
			return JsonConvert.DeserializeObject<T>(json, JsonSettings);
			/*		Main.log("A");
					// create an empty instance of T and let PopulateObject fill it
					var obj = (T)Activator.CreateInstance(typeof(T));
					Main.log("B");
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
