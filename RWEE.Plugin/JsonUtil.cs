using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace RWEE
{
	internal static class JsonUtil
	{
		internal static string ToJson<T>(T obj)
		{
			if (obj == null)
				return string.Empty;

			var serializer = new DataContractJsonSerializer(typeof(T));
			using (var ms = new MemoryStream())
			{
				serializer.WriteObject(ms, obj);
				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}

		internal static T FromJson<T>(string json)
		{
			if (string.IsNullOrEmpty(json))
				return default;

			var serializer = new DataContractJsonSerializer(typeof(T));
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
			{
				var obj = serializer.ReadObject(ms);
				return (T)obj;
			}
		}
	}
}
