using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RWMM.patcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace RWMM.Patcher
{
	public static class RWMMPatcher
	{
		private static ManualLogSource _log;
		// Your loader likes the PROPERTY form.
		public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };
		static RWMMPatcher()
		{
			try { _log = Logger.CreateLogSource("RWMM.Preloader"); }
			catch { /* fallback will use Console */ }
		}

		static void Log(string s)
		{
			if (_log != null) _log.LogInfo(s);
			else { try { System.Console.WriteLine(s); } catch { } }
		}

		public static void Patch(AssemblyDefinition asm)
		{
			string modVersion = "0.0.1";
			Log("Patch() entered");

			var mod = asm.MainModule;
			int total = 0;

			total += EnsureOptionalField(mod, "GameDataInfo", "rweeItemMapJson", mod.TypeSystem.String);
			total += EnsureOptionalField(mod, "ShipModelData", "refName", mod.TypeSystem.String, false);
			total += EnsureOptionalField(mod, "TWeapon", "refName", mod.TypeSystem.String, false);
			total += EnsureOptionalField(mod, "CrewMember", "refName", mod.TypeSystem.String, false);

			//			total += AddDataContract.Add(mod,"Item");
			total += IgnoreDataMember.AddIgnoreDataMemberToNonserialized(mod, "Item");

//			total += DataMemberOrder.AddDataMemberOrder(mod, "Item", "id", 0);
			Log("Patch() done. Replacements/Appends: " + total);
		}

		// Rewrites all stores to declaringTypeName.fieldName and appends a final set at end of its .cctor
		static int ForceField(ModuleDefinition mod, string declaringTypeName, string fieldName, Instruction pushNewVal)
		{
			var declType = mod.Types.FirstOrDefault(t => t.Name == declaringTypeName);
			if (declType == null) { Log("[WARN] Declaring type not found: " + declaringTypeName); return 0; }

			var field = declType.Fields.FirstOrDefault(f => f.Name == fieldName);
			if (field == null) { Log("[WARN] Field not found: " + declaringTypeName + "." + fieldName); return 0; }

			int edits = 0;

			// 1) Rewrite every stsfld to this field anywhere
			foreach (var type in mod.Types)
			{
				foreach (var m in type.Methods)
				{
					if (!m.HasBody) continue;

					var il = m.Body.GetILProcessor();
					var ins = m.Body.Instructions;

					for (int i = 0; i < ins.Count; i++)
					{
						var inst = ins[i];
						if (inst.OpCode != OpCodes.Stsfld) continue;

						var fr = inst.Operand as FieldReference;
						if (fr == null) continue;
						if (fr.Name != fieldName) continue;
						if (fr.DeclaringType.FullName != declType.FullName) continue;

						// Idempotency: if the two instructions immediately before are (Pop, <our load>), skip
						var prev1 = (i > 0) ? ins[i - 1] : null;
						var prev2 = (i > 1) ? ins[i - 2] : null;

						bool prev1IsOurLoad =
								prev1 != null &&
								(prev1.OpCode == OpCodes.Ldc_I4 || prev1.OpCode == OpCodes.Ldc_R4 || prev1.OpCode == OpCodes.Ldstr);

						bool alreadyPatched =
								prev2 != null &&
								prev2.OpCode == OpCodes.Pop &&
								prev1IsOurLoad;

						if (alreadyPatched) continue;

						// Insert our replacement just before stsfld
						il.InsertBefore(inst, Instruction.Create(OpCodes.Pop));
						il.InsertBefore(inst, Clone(pushNewVal)); // clone so multiple inserts are safe
						edits++;
						Log("Rewrote " + declType.FullName + "." + fieldName + " in " + type.FullName + "::" + m.Name);

						// Skip over the two we inserted
						i += 2;
					}
				}
			}

			// 2) Append final assignment at end of declaring .cctor
			var cctor = declType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic && m.HasBody);
			if (cctor != null)
			{
				var il = cctor.Body.GetILProcessor();
				var ins = cctor.Body.Instructions;

				Instruction ret = null;
				for (int i = ins.Count - 1; i >= 0; i--)
				{
					if (ins[i].OpCode == OpCodes.Ret) { ret = ins[i]; break; }
				}

				if (ret != null)
				{
					// Check we didn't already append a set for this field
					var b1 = ret.Previous;
					var b2 = (b1 != null) ? b1.Previous : null;
					bool alreadyAppended = false;

					if (b1 != null && b1.OpCode == OpCodes.Stsfld)
					{
						var fr2 = b1.Operand as FieldReference;
						if (fr2 != null &&
								fr2.Name == fieldName &&
								fr2.DeclaringType.FullName == declType.FullName)
							alreadyAppended = true;
					}

					if (!alreadyAppended)
					{
						il.InsertBefore(ret, Clone(pushNewVal));
						il.InsertBefore(ret, Instruction.Create(OpCodes.Stsfld, field));
						edits++;
						Log("Appended final set for " + declType.FullName + "." + fieldName + " at end of .cctor");
					}
				}
				else
				{
					Log("[WARN] " + declType.FullName + "..cctor has no Ret");
				}
			}
			else
			{
				Log("[WARN] " + declType.FullName + " has no .cctor");
			}

			return edits;
		}

		// Cecil Instruction is mutable; use a simple cloner for the small set we emit
		static Instruction Clone(Instruction src)
		{
			// Only cloning the kinds we create above
			if (src.OpCode == OpCodes.Ldc_I4) return Instruction.Create(OpCodes.Ldc_I4, (int)src.Operand);
			if (src.OpCode == OpCodes.Ldc_R4) return Instruction.Create(OpCodes.Ldc_R4, (float)src.Operand);
			if (src.OpCode == OpCodes.Ldstr) return Instruction.Create(OpCodes.Ldstr, (string)src.Operand);
			if (src.OpCode == OpCodes.Pop) return Instruction.Create(OpCodes.Pop);
			// Fallback (shouldn't hit with our usage)
			return Instruction.Create(src.OpCode);
		}
		// Replace every *read* (ldsfld) of fieldOwnerTypeName.fieldName with a constant
		static int ReplaceFieldReadsWithConst(
				ModuleDefinition mod,
				string targetTypeName,
				string targetMethodName,
				string fieldOwnerTypeName,
				string fieldName,
				Instruction replacement,
				string[] paramTypeFullNames /* optional: null = all overloads */ = null)
		{
			var targetType = mod.Types.FirstOrDefault(t => t.Name == targetTypeName);
			if (targetType == null) { Log("[WARN] Type not found: " + targetTypeName); return 0; }

			var ownerType = mod.Types.FirstOrDefault(t => t.Name == fieldOwnerTypeName);
			if (ownerType == null) { Log("[WARN] Field owner type not found: " + fieldOwnerTypeName); return 0; }
			var ownerFull = ownerType.FullName;

			var methods = targetType.Methods.Where(m => m.Name == targetMethodName && m.HasBody).ToList();
			if (methods.Count == 0) { Log("[WARN] Method not found: " + targetTypeName + "." + targetMethodName); return 0; }

			// Optional: narrow to a specific overload by parameter type full names
			if (paramTypeFullNames != null)
			{
				methods = methods.Where(m =>
				{
					if (m.Parameters.Count != paramTypeFullNames.Length) return false;
					for (int i = 0; i < paramTypeFullNames.Length; i++)
						if (m.Parameters[i].ParameterType.FullName != paramTypeFullNames[i]) return false;
					return true;
				}).ToList();

				if (methods.Count == 0)
				{
					Log("[WARN] Overload not found: " + targetTypeName + "." + targetMethodName + "(" + string.Join(", ", paramTypeFullNames) + ")");
					return 0;
				}
			}

			int edits = 0;
			foreach (var m in methods)
			{
				var il = m.Body.GetILProcessor();
				var ins = m.Body.Instructions;
				int hitsHere = 0;

				for (int i = 0; i < ins.Count; i++)
				{
					var inst = ins[i];
					if (inst.OpCode != OpCodes.Ldsfld) continue;

					var fr = inst.Operand as FieldReference;
					if (fr == null) continue;
					if (fr.Name != fieldName) continue;
					if (fr.DeclaringType.FullName != ownerFull) continue;

					// Replace the field read with our constant
					il.Replace(inst, Clone(replacement));
					hitsHere++;
					edits++;
				}

				if (hitsHere > 0)
					Log("Replaced " + hitsHere + " read(s) of " + fieldOwnerTypeName + "." + fieldName + " in " + targetType.FullName + "::" + m.Name);
			}

			return edits;
		}

		static int EnsureOptionalField(ModuleDefinition mod, string typeName, string fieldName, TypeReference type, Boolean isSerializable = true, Boolean isStatic = false)
		{
			var t = mod.Types.FirstOrDefault(x => x.Name == typeName);
			if (t == null)
			{
				Log("[WARN] Type not found: " + typeName);
				return 0;
			}

			// Already present?
			if (t.Fields.Any(f => f.Name == fieldName))
			{
				Log("Field already exists: " + typeName + "." + fieldName);
				return 0;
			}

			// public [non]serialized <type> <fieldName>;
			var attrs = Mono.Cecil.FieldAttributes.Public;
			System.Reflection.ConstructorInfo ctor;
			if (isSerializable)
			{
				ctor = typeof(System.Runtime.Serialization.OptionalFieldAttribute).GetConstructor(Type.EmptyTypes);
			}
			else
			{
				ctor = typeof(System.NonSerializedAttribute).GetConstructor(Type.EmptyTypes);
				attrs |= FieldAttributes.NotSerialized;
			}
			if (isStatic)
			{
				attrs |= FieldAttributes.Static;
			}

			var fld = new FieldDefinition(fieldName, attrs, type);
			var optAttrCtorRef = mod.ImportReference(ctor);
			fld.CustomAttributes.Add(new CustomAttribute(optAttrCtorRef));

			Log("Added " + typeName + "." + fieldName + "");
			t.Fields.Add(fld);
			return 1;
		}
		static int ReplaceConstFloatInMethod(ModuleDefinition mod, string typeName, string methodName, float oldVal, float newVal)
		{
			var type = mod.Types.FirstOrDefault(t => t.Name == typeName);
			if (type == null) return 0;
			var m = type.Methods.FirstOrDefault(mm => mm.Name == methodName && mm.HasBody);
			if (m == null) return 0;

			int edits = 0;
			foreach (var ins in m.Body.Instructions)
			{
				if (ins.OpCode == OpCodes.Ldc_R4 && ins.Operand is float f && System.Math.Abs(f - oldVal) < 0.0001f)
				{
					ins.Operand = newVal;
					edits++;
				}
			}
			return edits;
		}
	}

}