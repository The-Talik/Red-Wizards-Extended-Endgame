using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
namespace RWMM
{

	public static class ObjectCopy
	{
		
		public static void Apply<TSource, TTarget>(TSource source, TTarget target)
		{
			if (source == null || target == null)
				return;

			var source_type = typeof(TSource);
			var target_type = typeof(TTarget);

			// Copy fields
			var source_fields = source_type.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var sf in source_fields)
			{
				var tf = target_type.GetField(sf.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (tf == null)
					continue;

				if (!tf.FieldType.IsAssignableFrom(sf.FieldType))
					continue;

				var value = sf.GetValue(source);
				tf.SetValue(target, value);
			}

			// Copy properties (public get -> public/private set, same name/type, no indexer)
			var source_props = source_type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var sp in source_props)
			{
				if (!sp.CanRead)
					continue;
				if (sp.GetIndexParameters().Length != 0)
					continue;

				var tp = target_type.GetProperty(sp.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (tp == null || !tp.CanWrite || tp.GetIndexParameters().Length != 0)
					continue;

				if (!tp.PropertyType.IsAssignableFrom(sp.PropertyType))
					continue;

				var value = sp.GetValue(source, null);
				tp.SetValue(target, value, null);
			}
		}
	}

}
