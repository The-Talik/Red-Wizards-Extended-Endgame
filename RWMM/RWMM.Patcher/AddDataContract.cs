using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWMM.patcher
{
	internal class AddDataContract
	{
		// Adds [DataContract] to a type (class/struct). Returns 1 if applied, 0 if already present / not found.
		public static int Add(ModuleDefinition module, string type_full_name)
		{
			if (module == null || string.IsNullOrWhiteSpace(type_full_name))
				return 0;

			var td = module.GetType(type_full_name) ?? module.Types.FirstOrDefault(t => t.FullName == type_full_name);
			if (td == null)
				return 0;

			var full = "System.Runtime.Serialization.DataContractAttribute";

			if (td.CustomAttributes.Any(a => a.AttributeType.FullName == full))
				return 0;

			// ensure System.Runtime.Serialization reference
			var asm_ref = module.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Runtime.Serialization");
			if (asm_ref == null)
			{
				asm_ref = new AssemblyNameReference("System.Runtime.Serialization", new Version(4, 0, 0, 0));
				module.AssemblyReferences.Add(asm_ref);
			}

			var attr_type = new TypeReference("System.Runtime.Serialization", "DataContractAttribute", module, asm_ref);

			// public DataContractAttribute()
			var ctor_ref = new MethodReference(".ctor", module.TypeSystem.Void, attr_type) { HasThis = true };
			var ctor_import = module.ImportReference(ctor_ref);

			td.CustomAttributes.Add(new CustomAttribute(ctor_import));
			return 1;
		}
	}
}