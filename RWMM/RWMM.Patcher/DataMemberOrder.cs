using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWMM.patcher
{
	public static class DataMemberOrder
	{
		// Adds [DataMember(Order = <order>)] to a FIELD or PROPERTY named member_name on type_full_name.
		// Returns 1 if applied, 0 if not found / already present.
		public static int AddDataMemberOrder(ModuleDefinition module, string type_full_name, string member_name, int order)
		{
			if (module == null || string.IsNullOrWhiteSpace(type_full_name) || string.IsNullOrWhiteSpace(member_name))
				return 0;

			var td = FindType(module, type_full_name);
			if (td == null)
				return 0;

			// ensure System.Runtime.Serialization reference
			var asm_ref = module.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Runtime.Serialization");
			if (asm_ref == null)
			{
				asm_ref = new AssemblyNameReference("System.Runtime.Serialization", new Version(4, 0, 0, 0));
				module.AssemblyReferences.Add(asm_ref);
			}

			var attr_type = new TypeReference("System.Runtime.Serialization", "DataMemberAttribute", module, asm_ref);

			// public DataMemberAttribute()
			var ctor_ref = new MethodReference(".ctor", module.TypeSystem.Void, attr_type) { HasThis = true };
			var ctor_import = module.ImportReference(ctor_ref);

			var member = (object)td.Fields.FirstOrDefault(f => f.Name == member_name)
				?? (object)td.Properties.FirstOrDefault(p => p.Name == member_name);

			if (member == null)
				return 0;

			var attrs = member is FieldDefinition fd ? fd.CustomAttributes
				: member is PropertyDefinition pd ? pd.CustomAttributes
				: null;

			if (attrs == null)
				return 0;

			var full = "System.Runtime.Serialization.DataMemberAttribute";

			// If already has [DataMember], update Order if possible
			var existing = attrs.FirstOrDefault(a => a.AttributeType.FullName == full);
			if (existing != null)
			{
				SetOrReplaceOrder(existing, module, order);
				return 1;
			}

			var attr = new CustomAttribute(ctor_import);
			attr.Properties.Add(new CustomAttributeNamedArgument(
				"Order",
				new CustomAttributeArgument(module.TypeSystem.Int32, order)
			));

			attrs.Add(attr);
			return 1;
		}

		private static void SetOrReplaceOrder(CustomAttribute attr, ModuleDefinition module, int order)
		{
			for (int i = 0; i < attr.Properties.Count; i++)
			{
				if (attr.Properties[i].Name == "Order")
				{
					attr.Properties[i] = new CustomAttributeNamedArgument(
						"Order",
						new CustomAttributeArgument(module.TypeSystem.Int32, order)
					);
					return;
				}
			}

			attr.Properties.Add(new CustomAttributeNamedArgument(
				"Order",
				new CustomAttributeArgument(module.TypeSystem.Int32, order)
			));
		}

		private static TypeDefinition FindType(ModuleDefinition module, string full_name)
		{
			var td = module.GetType(full_name);
			if (td != null) return td;

			// fallback (nested types not handled per your note)
			return module.Types.FirstOrDefault(t => t.FullName == full_name);
		}
	}

}