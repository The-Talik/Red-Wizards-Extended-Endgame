using Mono.Cecil;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace RWMM.Patcher
{

	public static class IgnoreDataMember
	{
		// Pass e.g. "Item" or "Some.Namespace.Item" (use dnSpy FullName)
		public static int AddIgnoreDataMemberToNonserialized(ModuleDefinition module, string type_full_name)
		{
			if (module == null || string.IsNullOrWhiteSpace(type_full_name))
				return 0;

			var td = module.GetType(type_full_name) ?? module.Types.FirstOrDefault(t => t.FullName == type_full_name);
			if (td == null)
				return 0;

			// Ensure assembly reference exists (Unity/NET4x usually has this available)
			var asm_ref = module.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Runtime.Serialization");
			if (asm_ref == null)
			{
				asm_ref = new AssemblyNameReference("System.Runtime.Serialization", new Version(4, 0, 0, 0));
				module.AssemblyReferences.Add(asm_ref);
			}

			var ignore_attr_type = new TypeReference("System.Runtime.Serialization", "IgnoreDataMemberAttribute", module, asm_ref);

			// public IgnoreDataMemberAttribute()
			var ctor_ref = new MethodReference(".ctor", module.TypeSystem.Void, ignore_attr_type)
			{
				HasThis = true
			};

			var ignore_attr_full = "System.Runtime.Serialization.IgnoreDataMemberAttribute";
			int changed = 0;

			for (int i = 0; i < td.Fields.Count; i++)
			{
				var f = td.Fields[i];

				if (!f.IsNotSerialized)
					continue;

				if (f.CustomAttributes.Any(a => a.AttributeType.FullName == ignore_attr_full))
					continue;

				f.CustomAttributes.Add(new CustomAttribute(module.ImportReference(ctor_ref)));
				changed++;
			}

			return changed;
		}
	}

}