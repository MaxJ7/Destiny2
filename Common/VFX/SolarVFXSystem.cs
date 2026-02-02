using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX
{
	/// <summary>
	/// Solar flare / explosive fire VFX: Incandescent and Ignition.
	/// Procedural flame textures, Luminance noise overlays, TurbulentNoise, BloomFlare. Expanding explosive fire.
	/// </summary>
	public sealed class SolarVFXSystem : ModSystem
	{
		private struct ActiveExplosion
		{
			public Vector2 Center;
			public int SpawnFrame;
			public bool IsIgnition;
		}

		private static readonly List<ActiveExplosion> ActiveExplosions = new();

		private const int DurationIncandescent = 48;
		private const int DurationIgnition = 64;

		// Procedural textures—solar flare / explosive fire
		private static Texture2D _flameLobeTex;
		private static Texture2D _flareRaysTex;
		private static Texture2D _flameCoreTex;

		private static readonly Color IncandescentCore = new(255, 255, 250);
		private static readonly Color IncandescentBody = new(255, 190, 80);
		private static readonly Color IncandescentEdge = new(255, 120, 35);

		private static readonly Color IgnitionCore = new(255, 255, 255);
		private static readonly Color IgnitionBody = new(255, 150, 55);
		private static readonly Color IgnitionEdge = new(255, 85, 25);

		public override void Load()
		{
			if (Main.dedServ) return;
			// Procedural textures created lazily in PostDrawTiles (must run on main thread)
		}

		public override void Unload()
		{
			_flameLobeTex?.Dispose();
			_flameLobeTex = null;
			_flareRaysTex?.Dispose();
			_flareRaysTex = null;
			_flameCoreTex?.Dispose();
			_flameCoreTex = null;
		}

		/// <summary>Creates procedural textures on first use. Must run on main thread (FNA3D).</summary>
		private static void EnsureProceduralTextures()
		{
			if (_flameLobeTex != null || Main.instance?.GraphicsDevice == null) return;
			var gd = Main.instance.GraphicsDevice;
			const int size = 256;
			var center = new Vector2((size - 1) * 0.5f);
			float maxR = size * 0.48f;

			// Flame lobe—Perlin-driven irregular edges, flame-tongue feel
			_flameLobeTex = new Texture2D(gd, size, size, false, SurfaceFormat.Color);
			var lobeData = new Color[size * size];
			for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				var p = new Vector2(x, y);
				float dist = Vector2.Distance(p, center);
				float angle = (float)Math.Atan2(p.Y - center.Y, p.X - center.X);
				float nx = (x - center.X) / size * 4f;
				float ny = (y - center.Y) / size * 4f;
				float perlin = PerlinNoise.Fbm(nx, ny, 3, 2.2f, 0.55f);
				float lobe = 0.6f + 0.35f * perlin + 0.12f * PerlinNoise.Sample(angle * 2f, dist / maxR * 2f);
				float r = maxR * MathHelper.Clamp(lobe, 0.5f, 1.4f);
				float t = dist / r;
				float alpha = t <= 1f ? (float)(1f - Math.Pow(t, 1.1f)) * (0.35f + 0.65f * (1f - t)) : 0f;
				float edgeNoise = PerlinNoise.Sample(nx * 6f, ny * 6f);
				alpha *= 0.85f + 0.15f * edgeNoise;
				lobeData[y * size + x] = new Color(1f, 1f, 1f, MathHelper.Clamp(alpha, 0f, 1f));
			}
			_flameLobeTex.SetData(lobeData);

			// Flare rays—Perlin modulates ray intensity for organic variation
			_flareRaysTex = new Texture2D(gd, size, size, false, SurfaceFormat.Color);
			var raysData = new Color[size * size];
			for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				var p = new Vector2(x, y);
				float dist = Vector2.Distance(p, center);
				float angle = (float)Math.Atan2(p.Y - center.Y, p.X - center.X);
				float t = dist / maxR;
				float ray = (float)(Math.Sin(angle * 14f) * 0.5 + 0.5);
				float perlinRays = PerlinNoise.Fbm(angle * 3f, t * 4f, 2, 2f, 0.5f);
				ray = ray * (0.7f + 0.3f * perlinRays);
				float core = (float)Math.Exp(-t * t * 4);
				float rayFalloff = (1f - t) * (0.45f + 0.55f * ray);
				float turb = PerlinNoise.Sample(x * 0.03f, y * 0.03f);
				float alpha = MathHelper.Clamp(core * 1.5f + rayFalloff * 0.85f + turb * 0.1f, 0f, 1f);
				raysData[y * size + x] = new Color(1f, 1f, 1f, alpha);
			}
			_flareRaysTex.SetData(raysData);

			// Flame core—Perlin adds subtle turbulence to hot center
			_flameCoreTex = new Texture2D(gd, size, size, false, SurfaceFormat.Color);
			var coreData = new Color[size * size];
			for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float dist = Vector2.Distance(new Vector2(x, y), center);
				float t = dist / maxR;
				float baseAlpha = t <= 1f ? (float)(1f - Math.Pow(t, 0.75f)) : 0f;
				float perlin = PerlinNoise.Fbm(x * 0.04f, y * 0.04f, 2, 2f, 0.5f);
				float alpha = baseAlpha * (0.9f + 0.1f * perlin);
				coreData[y * size + x] = new Color(1f, 1f, 1f, MathHelper.Clamp(alpha, 0f, 1f));
			}
			_flameCoreTex.SetData(coreData);
		}

		private static Vector2 QuadDrawPos(Vector2 worldCenter, float texSize, float scale)
		{
			float half = texSize * 0.5f * scale;
			return worldCenter - new Vector2(half, -half);
		}

		private static Vector2 GetRadialDirection(float angle) => new((float)Math.Cos(angle), (float)Math.Sin(angle));
		private static float GoldenAngle(int i, int n) => (i * 2.39996323f) % MathHelper.TwoPi;

		public override void PostDrawTiles()
		{
			if (Main.gameMenu || Main.dedServ) return;
			EnsureProceduralTextures();
			if (ActiveExplosions.Count == 0) return;
			if (!ShaderManager.TryGetShader("Luminance.QuadRenderer", out var quadShader)) return;
			if (!ShaderManager.TryGetShader("Luminance.StandardPrimitiveShader", out var primShader)) return;

			Texture2D bloomTex = null;
			Texture2D bloomFlare = null;
			Texture2D turbulentNoise = null;
			Texture2D wavyNoise = null;
			try
			{
				bloomTex = MiscTexturesRegistry.BloomCircleSmall.Value;
				bloomFlare = MiscTexturesRegistry.BloomFlare.Value;
				turbulentNoise = MiscTexturesRegistry.TurbulentNoise.Value;
				wavyNoise = MiscTexturesRegistry.WavyBlotchNoise.Value;
			}
			catch { }
			if (bloomTex == null || _flameLobeTex == null) return;

			int now = (int)Main.GameUpdateCount;
			for (int i = ActiveExplosions.Count - 1; i >= 0; i--)
			{
				var ex = ActiveExplosions[i];
				int dur = ex.IsIgnition ? DurationIgnition : DurationIncandescent;
				int elapsed = now - ex.SpawnFrame;
				if (elapsed >= dur)
				{
					ActiveExplosions.RemoveAt(i);
					continue;
				}

				float progress = elapsed / (float)dur;
				DrawExplosion(ex.Center, progress, ex.IsIgnition, elapsed, quadShader, primShader, bloomTex, bloomFlare, turbulentNoise, wavyNoise);

				if (progress < 0.9f)
				{
					for (int j = 0; j < 6; j++) SpawnSustainedEmber(ex.Center, ex.IsIgnition);
					for (int j = 0; j < 4; j++) SpawnFlareWisp(ex.Center, ex.IsIgnition, progress);
				}
			}
		}

		public static void TriggerExplosion(Vector2 worldCenter, bool isIgnition)
		{
			if (Main.dedServ) return;
			ActiveExplosions.Add(new ActiveExplosion { Center = worldCenter, SpawnFrame = (int)Main.GameUpdateCount, IsIgnition = isIgnition });
			SpawnParticleBurst(worldCenter, isIgnition);
			SpawnFireMotes(worldCenter, isIgnition);
		}

		private static void SpawnParticleBurst(Vector2 center, bool isIgnition)
		{
			int n = isIgnition ? 65 : 55;
			for (int i = 0; i < n; i++)
			{
				float a = GoldenAngle(i, n);
				var dir = GetRadialDirection(a);
				dir.Y -= 0.18f;
				dir.Normalize();
				float speed = isIgnition ? Main.rand.NextFloat(7f, 16f) : Main.rand.NextFloat(6f, 13f);
				var v = dir * speed;
				var f = Dust.NewDustPerfect(center, DustID.Torch, v, 160, isIgnition ? new Color(255, 95, 35) : new Color(255, 180, 75), Main.rand.NextFloat(1.3f, 2.2f));
				f.noGravity = true;
				if (i % 2 == 0)
				{
					var s = Dust.NewDustPerfect(center, DustID.WhiteTorch, v * 0.95f, 110, new Color(255, 250, 210), Main.rand.NextFloat(1.2f, 1.8f));
					s.noGravity = true;
				}
				if (i % 3 == 0)
				{
					var e = Dust.NewDustPerfect(center, DustID.Torch, v * 0.8f, 130, isIgnition ? new Color(255, 140, 60) : new Color(255, 210, 120), Main.rand.NextFloat(1.1f, 1.7f));
					e.noGravity = true;
				}
			}
		}

		private static void SpawnFireMotes(Vector2 center, bool isIgnition)
		{
			int n = isIgnition ? 20 : 16;
			for (int i = 0; i < n; i++)
			{
				float a = GoldenAngle(i, n);
				var dir = GetRadialDirection(a);
				dir.Y -= 0.14f;
				dir.Normalize();
				var v = dir * Main.rand.NextFloat(isIgnition ? 8f : 7f, isIgnition ? 16f : 13f);
				var d = Dust.NewDustPerfect(center, DustID.Torch, v, 115, isIgnition ? new Color(255, 125, 50) : new Color(255, 230, 130), Main.rand.NextFloat(1.2f, 1.6f));
				d.noGravity = true;
			}
		}

		private static void SpawnSustainedEmber(Vector2 center, bool isIgnition)
		{
			int idx = (int)(Main.GameUpdateCount % 36);
			float a = GoldenAngle(idx, 36);
			var dir = GetRadialDirection(a);
			dir.Y -= 0.12f;
			dir.Normalize();
			var v = dir * Main.rand.NextFloat(3f, 7f) * (isIgnition ? 1.25f : 1f);
			var d = Dust.NewDustPerfect(center, DustID.Torch, v, 105, isIgnition ? new Color(255, 135, 55) : new Color(255, 210, 110), Main.rand.NextFloat(0.95f, 1.25f));
			d.noGravity = true;
		}

		private static void SpawnFlareWisp(Vector2 center, bool isIgnition, float progress)
		{
			float a = GoldenAngle((int)(Main.GameUpdateCount * 0.6f + progress * 25f) % 36, 36) + Main.rand.NextFloat(-0.5f, 0.5f);
			var dir = GetRadialDirection(a);
			dir.Y -= 0.18f + Main.rand.NextFloat(-0.12f, 0.18f);
			dir.Normalize();
			float spd = Main.rand.NextFloat(5f, 10f) * (1f + progress * 0.7f) * (isIgnition ? 1.2f : 1f);
			var v = dir * spd;
			var d = Dust.NewDustPerfect(center, DustID.Torch, v, 150, isIgnition ? new Color(255, 90, 30) : new Color(255, 200, 100), Main.rand.NextFloat(1.2f, 1.9f));
			d.noGravity = true;
		}

		private static void DrawExplosion(Vector2 center, float progress, bool isIgnition, int elapsed, ManagedShader quadShader, ManagedShader primShader, Texture2D bloomTex, Texture2D bloomFlare, Texture2D turbulentNoise, Texture2D wavyNoise)
		{
			float radiusPx = (isIgnition ? 5f : 3f) * 16f;
			float opacity = (float)Math.Sin(MathHelper.PiOver2 + progress * MathHelper.PiOver2);
			if (progress > 0.5f) opacity *= (1f - progress) / 0.5f;
			float flash = elapsed <= 10 ? (1f - elapsed / 10f) : 0f;
			float circleExpansion = 1f - (float)Math.Pow(1f - Math.Min(progress * 1.5f, 1f), 0.6f);
			float wispExpansion = 1f - (float)Math.Pow(1f - Math.Min(progress * 1.25f, 1f), 0.85f);
			float t = (float)Main.timeForVisualEffects * 0.02f;

			var gd = Main.instance.GraphicsDevice;
			var prevBlend = gd.BlendState;
			gd.BlendState = BlendState.Additive;

			float tw = 256f;
			float th = 256f;

			if (isIgnition)
				DrawIgnition(center, progress, radiusPx, circleExpansion, wispExpansion, opacity, flash, t, quadShader, primShader, bloomTex, bloomFlare, turbulentNoise, wavyNoise, tw, th);
			else
				DrawIncandescent(center, progress, radiusPx, circleExpansion, wispExpansion, opacity, flash, t, quadShader, primShader, bloomTex, bloomFlare, turbulentNoise, wavyNoise, tw, th);

			float light = opacity * (1.5f + flash * 3f);
			Lighting.AddLight(center, 1f * light, 0.6f * light, 0.2f * light);
			gd.BlendState = prevBlend;
		}

		private static float ChaosRadius(float interpolant, float baseRadius, float expansion, float t)
		{
			float angle = interpolant * MathHelper.TwoPi;
			float perlin = PerlinNoise.Fbm(angle * 2f + t * 0.5f, t * 3f, 3, 2f, 0.5f);
			float sine = 0.5f + 0.3f * (float)Math.Sin(angle * 6f + t) + 0.2f * (float)Math.Sin(angle * 9f + t * 1.2f);
			float chaos = perlin * 0.6f + sine * 0.4f;
			return baseRadius * expansion * (0.3f + 0.7f * MathHelper.Clamp(chaos, 0f, 1f));
		}

		private static void DrawIncandescent(Vector2 center, float progress, float radiusPx, float circleExpansion, float wispExpansion, float opacity, float flash, float t, ManagedShader quadShader, ManagedShader primShader, Texture2D bloomTex, Texture2D bloomFlare, Texture2D turbulentNoise, Texture2D wavyNoise, float tw, float th)
		{
			float circleR = radiusPx * circleExpansion * 1.08f;

			// TurbulentNoise overlay—flame turbulence
			if (turbulentNoise != null)
			{
				float noiseScale = circleR * 1.2f;
				var noisePos = QuadDrawPos(center, 256f, noiseScale / 256f);
				PrimitiveRenderer.RenderQuad(turbulentNoise, noisePos, new Vector2(noiseScale / turbulentNoise.Width, noiseScale / turbulentNoise.Height), t * 2f, IncandescentBody with { A = (byte)(opacity * 85) }, quadShader);
			}

			// Procedural flame lobe—main expanding fire shape
			float lobeScale = circleR * 1.15f;
			var lobePos = QuadDrawPos(center, tw, lobeScale / tw);
			PrimitiveRenderer.RenderQuad(_flameLobeTex, lobePos, new Vector2(lobeScale / tw, lobeScale / th), t * 0.5f, IncandescentEdge with { A = (byte)(opacity * 120) }, quadShader);

			// BloomFlare—starburst rays (if available)
			if (bloomFlare != null)
			{
				float flareScale = circleR * 0.9f;
				var flarePos = QuadDrawPos(center, bloomFlare.Width, flareScale / bloomFlare.Width);
				PrimitiveRenderer.RenderQuad(bloomFlare, flarePos, new Vector2(flareScale / bloomFlare.Width, flareScale / bloomFlare.Height), t * 0.3f, IncandescentBody with { A = (byte)(opacity * 130) }, quadShader);
			}

			// Procedural flare rays
			float raysScale = circleR * 0.85f;
			var raysPos = QuadDrawPos(center, tw, raysScale / tw);
			PrimitiveRenderer.RenderQuad(_flareRaysTex, raysPos, new Vector2(raysScale / tw, raysScale / th), progress * 0.5f, IncandescentBody with { A = (byte)(opacity * 150) }, quadShader);

			// Core—white hot
			float coreScale = circleR * 0.45f;
			var corePos = QuadDrawPos(center, tw, coreScale / tw);
			PrimitiveRenderer.RenderQuad(_flameCoreTex, corePos, new Vector2(coreScale / tw, coreScale / th), 0f, Color.Lerp(IncandescentCore, Color.White, flash * 0.8f) with { A = (byte)(opacity * (0.95f + flash * 0.5f) * 255) }, quadShader);

			// WavyBlotchNoise—extra flame chaos
			if (wavyNoise != null)
			{
				float wavyScale = circleR * 0.7f;
				var wavyPos = QuadDrawPos(center, wavyNoise.Width, wavyScale / wavyNoise.Width);
				PrimitiveRenderer.RenderQuad(wavyNoise, wavyPos, new Vector2(wavyScale / wavyNoise.Width, wavyScale / wavyNoise.Height), t * 1.5f, IncandescentEdge with { A = (byte)(opacity * 55) }, quadShader);
			}

			// Chaos wisps—RenderCircle
			if (primShader != null && wispExpansion > 0.05f)
			{
				var wispSettings = new PrimitiveSettingsCircle(
					interpolant => ChaosRadius(interpolant, radiusPx, wispExpansion, t),
					interpolant =>
					{
						float r = ChaosRadius(interpolant, radiusPx, wispExpansion, t);
						float dist = MathHelper.Clamp(r / (radiusPx * wispExpansion), 0f, 1f);
						byte a = (byte)(opacity * (0.25f + 0.75f * dist) * 180);
						return (dist > 0.6f ? IncandescentEdge : dist > 0.35f ? IncandescentBody : IncandescentCore) with { A = a };
					},
					false, primShader);
				PrimitiveRenderer.RenderCircle(center, wispSettings, 80);

				float edgeOp = (float)Math.Sin(progress * MathHelper.Pi) * 0.45f;
				if (edgeOp > 0.02f)
				{
					var edgeSettings = new PrimitiveSettingsCircleEdge(
						interpolant => 6f + 4f * (float)Math.Sin(interpolant * MathHelper.TwoPi * 5f + t * 0.5f),
						_ => IncandescentBody with { A = (byte)(edgeOp * 255) },
						interpolant => ChaosRadius(interpolant, radiusPx, wispExpansion, t) * 0.98f,
						false, primShader);
					PrimitiveRenderer.RenderCircleEdge(center, edgeSettings, 84);
				}
			}
		}

		private static void DrawIgnition(Vector2 center, float progress, float radiusPx, float circleExpansion, float wispExpansion, float opacity, float flash, float t, ManagedShader quadShader, ManagedShader primShader, Texture2D bloomTex, Texture2D bloomFlare, Texture2D turbulentNoise, Texture2D wavyNoise, float tw, float th)
		{
			float circleR = radiusPx * circleExpansion * 1.1f;

			if (turbulentNoise != null)
			{
				float noiseScale = circleR * 1.3f;
				var noisePos = QuadDrawPos(center, 256f, noiseScale / 256f);
				PrimitiveRenderer.RenderQuad(turbulentNoise, noisePos, new Vector2(noiseScale / turbulentNoise.Width, noiseScale / turbulentNoise.Height), t * 2.5f, IgnitionBody with { A = (byte)(opacity * 95) }, quadShader);
			}

			PrimitiveRenderer.RenderQuad(_flameLobeTex, QuadDrawPos(center, tw, circleR * 1.2f / tw), new Vector2(circleR * 1.2f / tw, circleR * 1.2f / th), t * 0.6f, IgnitionEdge with { A = (byte)(opacity * 135) }, quadShader);

			if (bloomFlare != null)
			{
				float flareScale = circleR * 1f;
				var flarePos = QuadDrawPos(center, bloomFlare.Width, flareScale / bloomFlare.Width);
				PrimitiveRenderer.RenderQuad(bloomFlare, flarePos, new Vector2(flareScale / bloomFlare.Width, flareScale / bloomFlare.Height), t * 0.4f, IgnitionBody with { A = (byte)(opacity * 145) }, quadShader);
			}

			PrimitiveRenderer.RenderQuad(_flareRaysTex, QuadDrawPos(center, tw, circleR * 0.95f / tw), new Vector2(circleR * 0.95f / tw, circleR * 0.95f / th), progress * 0.6f, IgnitionBody with { A = (byte)(opacity * 170) }, quadShader);

			PrimitiveRenderer.RenderQuad(_flameCoreTex, QuadDrawPos(center, tw, circleR * 0.5f / tw), new Vector2(circleR * 0.5f / tw, circleR * 0.5f / th), 0f, Color.Lerp(IgnitionCore, IgnitionBody, 1f - flash) with { A = (byte)(opacity * (0.95f + flash * 0.6f) * 255) }, quadShader);

			if (wavyNoise != null)
			{
				float wavyScale = circleR * 0.8f;
				var wavyPos = QuadDrawPos(center, wavyNoise.Width, wavyScale / wavyNoise.Width);
				PrimitiveRenderer.RenderQuad(wavyNoise, wavyPos, new Vector2(wavyScale / wavyNoise.Width, wavyScale / wavyNoise.Height), t * 1.8f, IgnitionEdge with { A = (byte)(opacity * 65) }, quadShader);
			}

			if (primShader != null && wispExpansion > 0.05f)
			{
				var wispSettings = new PrimitiveSettingsCircle(
					interpolant => ChaosRadius(interpolant, radiusPx, wispExpansion, t),
					interpolant =>
					{
						float r = ChaosRadius(interpolant, radiusPx, wispExpansion, t);
						float dist = MathHelper.Clamp(r / (radiusPx * wispExpansion), 0f, 1f);
						byte a = (byte)(opacity * (0.3f + 0.7f * dist) * 200);
						return (dist > 0.55f ? IgnitionEdge : dist > 0.3f ? IgnitionBody : IgnitionCore) with { A = a };
					},
					false, primShader);
				PrimitiveRenderer.RenderCircle(center, wispSettings, 90);

				var wisp2 = new PrimitiveSettingsCircle(
					interpolant => ChaosRadius(interpolant + 0.35f, radiusPx * 0.85f, wispExpansion * 0.9f, t + 6f),
					interpolant => IgnitionBody with { A = (byte)(opacity * 95) },
					false, primShader);
				PrimitiveRenderer.RenderCircle(center, wisp2, 72);

				float edgeOp = (float)Math.Sin(progress * MathHelper.Pi) * 0.5f;
				if (edgeOp > 0.02f)
				{
					var edgeSettings = new PrimitiveSettingsCircleEdge(
						interpolant => 8f + 5f * (float)Math.Sin(interpolant * MathHelper.TwoPi * 6f + t * 0.6f),
						_ => IgnitionBody with { A = (byte)(edgeOp * 255) },
						interpolant => ChaosRadius(interpolant, radiusPx, wispExpansion, t) * 0.97f,
						false, primShader);
					PrimitiveRenderer.RenderCircleEdge(center, edgeSettings, 96);
				}
			}
		}
	}
}
