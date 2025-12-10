using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RW
{
	public class DirUtils
	{
		public static DirectoryInfo FindPluginDir()
		{
			var asm_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			if (string.IsNullOrEmpty(asm_dir))
				return null;

			// Walk up until we hit "plugins"
			var dir = new DirectoryInfo(asm_dir);
			while (dir != null && !dir.Name.Equals("plugins", StringComparison.OrdinalIgnoreCase))
			{
				dir = dir.Parent;
			}
			return dir;
		}
		public static List<string> FindJsonFiles(string root_folder)
		{
			if (string.IsNullOrWhiteSpace(root_folder))
				return new List<string>();

			if (!Directory.Exists(root_folder))
				return new List<string>();

			return Directory
				.EnumerateFiles(root_folder, "*.json", SearchOption.AllDirectories)
				.ToList();
		}
	}

}
