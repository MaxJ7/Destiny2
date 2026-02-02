using System;
using Microsoft.Xna.Framework;

namespace Destiny2.Common.VFX
{
	/// <summary>
	/// Classic Perlin noise for procedural VFX—smooth, organic variation.
	/// Output in [0, 1]. Use octaves for fractal/turbulent noise.
	/// </summary>
	public static class PerlinNoise
	{
		private static readonly int[] Perm;
		private const int PermSize = 512;
		private const int PermMask = 255;

		static PerlinNoise()
		{
			Perm = new int[PermSize];
			int[] p = new int[256];
			for (int i = 0; i < 256; i++) p[i] = i;
			// Fixed seed for reproducibility
			var rng = new Random(4242);
			for (int i = 255; i >= 1; i--)
			{
				int j = rng.Next(i + 1);
				(p[i], p[j]) = (p[j], p[i]);
			}
			for (int i = 0; i < PermSize; i++)
				Perm[i] = p[i & PermMask];
		}

		/// <summary>2D Perlin noise, returns value in [0, 1].</summary>
		public static float Sample(float x, float y)
		{
			float n = Noise(x, y);
			return (n + 1f) * 0.5f;
		}

		/// <summary>Raw Perlin noise in [-1, 1].</summary>
		public static float Noise(float x, float y)
		{
			int X = (int)Math.Floor(x) & PermMask;
			int Y = (int)Math.Floor(y) & PermMask;
			x -= (float)Math.Floor(x);
			y -= (float)Math.Floor(y);

			float u = Fade(x);
			float v = Fade(y);

			int A = Perm[X] + Y;
			int B = Perm[X + 1] + Y;

			return Lerp(v,
				Lerp(u, Grad(Perm[A], x, y, 0), Grad(Perm[B], x - 1, y, 0)),
				Lerp(u, Grad(Perm[A + 1], x, y - 1, 0), Grad(Perm[B + 1], x - 1, y - 1, 0)));
		}

		/// <summary>Fractal Brownian motion—multiple octaves for turbulent/organic look.</summary>
		public static float Fbm(float x, float y, int octaves = 4, float lacunarity = 2f, float gain = 0.5f)
		{
			float sum = 0f;
			float freq = 1f;
			float amp = 1f;
			float maxAmp = 0f;
			for (int i = 0; i < octaves; i++)
			{
				sum += Noise(x * freq, y * freq) * amp;
				maxAmp += amp;
				amp *= gain;
				freq *= lacunarity;
			}
			return (sum / maxAmp + 1f) * 0.5f;
		}

		private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
		private static float Lerp(float t, float a, float b) => a + t * (b - a);

		private static float Grad(int hash, float x, float y, float z)
		{
			int h = hash & 15;
			float u = h < 8 ? x : y;
			float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
			return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
		}
	}
}
