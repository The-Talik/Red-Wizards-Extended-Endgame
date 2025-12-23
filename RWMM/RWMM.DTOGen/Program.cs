using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RWMM.DTOGen
{
	internal static class Program
	{
		// args:
		// --assembly "...\StarValor_Data\Managed\Assembly-CSharp.dll"
		// --output  "...\RWMM.Core\Generated\Dto"
		// --types   "Item,Equipment,Ship,Quest"   (optional; default tries Item,Equipment)
		// --namespace "RWMM.Dto"                 (optional; default RWMM.Dto)
		private static int Main(string[] args)
		{
			var assembly_path = GetArg(args, "--assembly");
			var output_dir = GetArg(args, "--output");
			var types_csv = GetArg(args, "--types");
			var ns = GetArg(args, "--namespace") ?? "RWMM.Dto";

			if (string.IsNullOrWhiteSpace(assembly_path) || string.IsNullOrWhiteSpace(output_dir))
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("  RWMM.DTOGen --assembly \"...\\Assembly-CSharp.dll\" --output \"...\\RWMM.Core\\Generated\\Dto\" --types \"Item,Equipment\"");
				return 1;
			}

			if (!File.Exists(assembly_path))
			{
				Console.WriteLine("Assembly not found: " + assembly_path);
				return 2;
			}

			Directory.CreateDirectory(output_dir);

			var resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(Path.GetDirectoryName(assembly_path));

			var rp = new ReaderParameters
			{
				AssemblyResolver = resolver,
				ReadSymbols = false
			};

			var module = ModuleDefinition.ReadModule(assembly_path, rp);

			var type_names = ParseTypes(types_csv);
			if (type_names.Count == 0)
			{
				type_names.Add("Item");
				type_names.Add("Equipment");
				type_names.Add("ShipModelData");
				type_names.Add("Quest");
				type_names.Add("TWeapon");
				type_names.Add("CrewMember");
				type_names.Add("Perk");
			}

			int files_written = 0;

			foreach (var type_name in type_names)
			{
				var td = FindType(module, type_name);
				if (td == null)
				{
					Console.WriteLine("[skip] Type not found: " + type_name);
					continue;
				}

				var dto_name = "_"+td.Name;
				var code = DtoEmitter.EmitDto(td, ns, dto_name);

				var out_path = Path.Combine(output_dir, dto_name + ".g.cs");
				File.WriteAllText(out_path, code, new UTF8Encoding(false));

				Console.WriteLine("[ok] " + dto_name + " -> " + out_path);
				files_written++;
			}

			Console.WriteLine("Done. Files written: " + files_written);
			return 0;
		}

		private static string GetArg(string[] args, string key)
		{
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
					return args[i + 1];
			}
			return null;
		}

		private static List<string> ParseTypes(string csv)
		{
			var list = new List<string>();
			if (string.IsNullOrWhiteSpace(csv))
				return list;

			var parts = csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++)
			{
				var s = parts[i].Trim();
				if (!string.IsNullOrWhiteSpace(s))
					list.Add(s);
			}
			return list;
		}

		private static TypeDefinition FindType(ModuleDefinition module, string name_or_fullname)
		{
			// exact full name
			var td = module.GetType(name_or_fullname);
			if (td != null)
				return td;

			// search top-level types by FullName or Name
			foreach (var t in module.Types)
			{
				if (t.FullName == name_or_fullname)
					return t;

				if (t.Name == name_or_fullname)
					return t;
			}

			return null;
		}
	}
}
