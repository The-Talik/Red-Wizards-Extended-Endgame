using System; 
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWEE
{
	static class RweeRand
	{
		public static int Range(int min, int maxExclusive, string seed)
		{
			seed = "RweeRand_" + seed;
			if (maxExclusive <= min) return min;

			int step = RweeData.IncInt(seed);
			ulong z = BaseSeed(seed) + GOLDEN * (ulong)(step);
			ulong rnd = SplitMix64(z);

			uint r32 = (uint)(rnd >> 32);
			uint span = (uint)(maxExclusive - min);
			// unbiased scale: high 32 bits of (r32 * span)
			uint scaled = (uint)(((ulong)r32 * span) >> 32);
			return min + (int)scaled;
		}
		public static float RangeF(float min, float max, string seed)
		{
			seed = "RweeRand_" + seed;
			if (max <= min) return min;

			int step = RweeData.IncInt(seed);
			// use a slightly different stream for float (xor a constant)
			ulong z = BaseSeed(seed) + GOLDEN * (ulong)(step) ^ 0xBF58476D1CE4E5B9UL;
			ulong rnd = SplitMix64(z);

			// top 24 bits → [0,1)
			double u = ((rnd >> 40) & 0xFFFFFFUL) / 16777216.0;
			return (float)(min + (max - min) * u);
		}
		static ulong BaseSeed(string seed)
		{
			// per-save identity + user seed → 64-bit seed
			string saveKey = (GameData.saveType ?? "unknown") + "#" + GameData.gameFileIndex;
			string s = "RWEE|" + saveKey + "|" + seed;

			// FNV-1a 64
			ulong h = 1469598103934665603UL;
			for (int i = 0; i < s.Length; i++)
			{
				h ^= (byte)s[i];
				h *= 1099511628211UL;
			}
			return h ^ 0xD1B54A32D192ED03UL; // extra stir
		}

		static ulong SplitMix64(ulong x)
		{
			x += 0x9E3779B97F4A7C15UL;
			x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
			x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
			return x ^ (x >> 31);
		}

		const ulong GOLDEN = 0x9E3779B97F4A7C15UL;
	}
}
