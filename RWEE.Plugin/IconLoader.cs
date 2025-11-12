using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace RWEE
{
	static class IconLoader
	{
		private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

		public static Sprite LoadSpriteFromPng(string path, float pixelsPerUnit)
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return null;

			Sprite cached;
			if (_cache.TryGetValue(path, out cached))
				return cached;

			byte[] bytes = File.ReadAllBytes(path);

			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

			if (!UnityEngine.ImageConversion.LoadImage(tex, bytes))
				return null;

			tex.wrapMode = TextureWrapMode.Clamp;
			tex.filterMode = FilterMode.Bilinear;

			if (pixelsPerUnit <= 0f) pixelsPerUnit = 100f;

			var spr = Sprite.Create(
				tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f),
				pixelsPerUnit
			);
			spr.name = Path.GetFileNameWithoutExtension(path);

			_cache[path] = spr;
			return spr;
		}
	}
}
