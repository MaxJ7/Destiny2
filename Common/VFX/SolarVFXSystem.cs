using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminance.Core.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX
{
	/// <summary>
	/// Destiny 2-style Solar VFX: Incandescent on-kill bursts, Scorch build-up on enemies,
	/// and Ignition detonations. Uses Luminance's PrimitiveRenderer, ScreenShakeSystem, and shaders.
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
		private static Texture2D _bloomLobe;
		private static Texture2D _chaosBlob;
		private static Texture2D _ringTexture;
		private static Texture2D _flameWaveTexture;

		private const int DurationIncandescent = 40;
		private const int DurationIgnition = 55;

		// Incandescent: fiery Solar—white-hot core, orange body, deep red edge
		private static readonly Color IncandescentCore = new(255, 255, 220);
		private static readonly Color IncandescentBody = new(255, 140, 50);
		private static readonly Color IncandescentEdge = new(200, 60, 20);

		// Ignition: violent detonation, white-hot flash, red-orange plasma
		private static readonly Color IgnitionCore = new(255, 255, 255);
		private static readonly Color IgnitionBody = new(255, 90, 25);
		private static readonly Color IgnitionPlasma = new(200, 40, 15);
		private static readonly Color IgnitionEdge = new(140, 25, 8);

		public override void Load()
		{
			if (Main.dedServ) return;
		}

		public override void Unload()
		{
			_bloomLobe?.Dispose();
			_bloomLobe = null;
			_chaosBlob?.Dispose();
			_chaosBlob = null;
			_ringTexture?.Dispose();
			_ringTexture = null;
			_flameWaveTexture?.Dispose();
			_flameWaveTexture = null;
		}

		private static void EnsureTextures()
		{
			if (_bloomLobe != null || Main.instance?.GraphicsDevice == null) return;
			_bloomLobe = CreateLobeTexture(256);
			_chaosBlob = CreateChaosBlobTexture(256);
			_ringTexture = CreateRingTexture(256);
			_flameWaveTexture = CreateFlameWaveTexture(256);
		}

		/// <summary>Fiery texture: 12 distinct flame tongues radiating outward—elongated teardrops, bright tips, organic noise.</summary>
		private static Texture2D CreateFlameWaveTexture(int size)
		{
			var tex = new Texture2D(Main.instance.GraphicsDevice, size, size, false, SurfaceFormat.Color);
			var data = new Color[size * size];
			var center = new Vector2((size - 1) * 0.5f);
			float radius = size * 0.48f;

			static float Hash(float n)
			{
				double v = Math.Sin(n) * 43758.5453;
				return (float)(v - Math.Floor(v));
			}
			static float Noise(Vector2 p)
			{
				var i = new Vector2((float)Math.Floor(p.X), (float)Math.Floor(p.Y));
				var f = new Vector2(p.X - i.X, p.Y - i.Y);
				f = f * f * (new Vector2(3, 3) - f * 2f);
				float a = Hash(i.X + i.Y * 57f);
				float b = Hash(i.X + 1 + i.Y * 57f);
				float c = Hash(i.X + (i.Y + 1) * 57f);
				float d = Hash(i.X + 1 + (i.Y + 1) * 57f);
				return a * (1 - f.X) * (1 - f.Y) + b * f.X * (1 - f.Y) + c * (1 - f.X) * f.Y + d * f.X * f.Y;
			}

			// 12 flame tongues—elongated in radial direction, narrow perpendicular
			const int tongueCount = 12;
			for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				var p = new Vector2(x, y);
				float dist = Vector2.Distance(p, center);
				float angle = (float)Math.Atan2(p.Y - center.Y, p.X - center.X);

				float bestAlpha = 0f;
				for (int t = 0; t < tongueCount; t++)
				{
					float tongueAngle = t / (float)tongueCount * MathHelper.TwoPi;
					float angleDiff = Math.Abs(((angle - tongueAngle + MathHelper.Pi) % MathHelper.TwoPi) - MathHelper.Pi);
					// Tongue is elongated along its angle, narrow perpendicular (elliptical)
					float alongTongue = dist * (float)Math.Cos(angleDiff);
					float acrossTongue = dist * (float)Math.Sin(angleDiff);
					// Elongated: 1.2 radial, 0.4 perpendicular
					float ellipticalDist = (alongTongue / (radius * 1f)) * (alongTongue / (radius * 1f)) + (acrossTongue / (radius * 0.35f)) * (acrossTongue / (radius * 0.35f));
					float tongueT = (float)Math.Sqrt(ellipticalDist);

					if (tongueT <= 1f)
					{
						float n = Noise(new Vector2(angle * 8 + t * 7, dist * 0.06f));
						float n2 = Noise(new Vector2(angle * 13 + 41, dist * 0.04f));
						float irregular = 0.9f + 0.2f * n + 0.1f * n2;
						float edge = 1f - MathHelper.Clamp((tongueT - 0.5f) / (0.5f * irregular), 0f, 1f);
						float tip = 1f - MathHelper.Clamp(tongueT / 0.3f, 0f, 1f);
						float alpha = Math.Max(edge * 0.85f, tip * 0.95f) * (1f - tongueT * 0.4f);
						bestAlpha = Math.Max(bestAlpha, alpha);
					}
				}

				// Core glow
				float coreT = dist / (radius * 0.35f);
				float coreAlpha = (1f - MathHelper.Clamp(coreT, 0f, 1f)) * 0.9f;
				bestAlpha = Math.Max(bestAlpha, coreAlpha);

				data[y * size + x] = bestAlpha > 0.01f ? new Color(1f, 1f, 1f, MathHelper.Clamp(bestAlpha, 0f, 1f)) : Color.Transparent;
			}
			tex.SetData(data);
			return tex;
		}

		/// <summary>Luminance RenderQuad places quad's bottom-left at position; offset so visual center aligns.</summary>
		private static Vector2 GetCenteredDrawPos(Vector2 worldCenter, float scale)
		{
			const float halfTex = 128f;
			// If particles appear to cluster bottom-left, the explosion may be shifted; try UseAlternateOffset = true
			const bool UseAlternateOffset = false;
			if (UseAlternateOffset)
				return worldCenter - new Vector2(halfTex * scale, halfTex * scale);
			return worldCenter - new Vector2(halfTex * scale, -halfTex * scale);
		}

		/// <summary>Explicit radial direction—Terraria Y-down, angle 0 = right. No ToRotationVector2 to avoid framework quirks.</summary>
		private static Vector2 GetRadialDirection(float angle)
		{
			return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}

		/// <summary>Golden-ratio spaced angles for maximally uniform distribution—no clustering in any quadrant.</summary>
		private static float GetGoldenAngle(int index, int total)
		{
			const float goldenAngle = 2.39996323f; // 2*PI / phi
			return (index * goldenAngle) % MathHelper.TwoPi;
		}

		/// <summary>Single flame lobe with soft falloff—combined in multiples for irregular shape.</summary>
		private static Texture2D CreateLobeTexture(int size)
		{
			var tex = new Texture2D(Main.instance.GraphicsDevice, size, size, false, SurfaceFormat.Color);
			var data = new Color[size * size];
			var center = new Vector2((size - 1) * 0.5f);
			float radius = size * 0.48f;

			for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				var p = new Vector2(x, y);
				float dist = Vector2.Distance(p, center);
				float angle = (float)Math.Atan2(p.Y - center.Y, p.X - center.X);
				// Angular wobble: lobes and notches for irregular edge (not circular)
				float wobble = 0.65f + 0.25f * (float)Math.Sin(angle * 4) + 0.15f * (float)Math.Sin(angle * 9);
				float t = dist / (radius * wobble);
				if (t <= 1f)
				{
					float inner = 1f - MathHelper.Clamp(t / 0.25f, 0f, 1f);
					float outer = 1f - MathHelper.Clamp((t - 0.5f) / 0.5f, 0f, 1f);
					float alpha = Math.Max(inner, outer * 0.6f);
					data[y * size + x] = new Color(1f, 1f, 1f, MathHelper.Clamp(alpha, 0f, 1f));
				}
				else
					data[y * size + x] = Color.Transparent;
			}
			tex.SetData(data);
			return tex;
		}

		/// <summary>Chaotic blob with jagged, explosion-like edge—noise-driven irregularity.</summary>
		private static Texture2D CreateChaosBlobTexture(int size)
		{
			var tex = new Texture2D(Main.instance.GraphicsDevice, size, size, false, SurfaceFormat.Color);
			var data = new Color[size * size];
			var center = new Vector2((size - 1) * 0.5f);
			float radius = size * 0.45f;

			static float Hash(float n)
			{
				double v = Math.Sin(n) * 43758.5453;
				return (float)(v - Math.Floor(v));
			}
			static float Noise(Vector2 p)
			{
				var i = new Vector2((float)Math.Floor(p.X), (float)Math.Floor(p.Y));
				var f = new Vector2(p.X - i.X, p.Y - i.Y);
				f = f * f * (new Vector2(3, 3) - f * 2f);
				float a = Hash(i.X + i.Y * 57f);
				float b = Hash(i.X + 1 + i.Y * 57f);
				float c = Hash(i.X + (i.Y + 1) * 57f);
				float d = Hash(i.X + 1 + (i.Y + 1) * 57f);
				return a * (1 - f.X) * (1 - f.Y) + b * f.X * (1 - f.Y) + c * (1 - f.X) * f.Y + d * f.X * f.Y;
			}

			for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				var p = new Vector2(x, y);
				float dist = Vector2.Distance(p, center);
				float angle = (float)Math.Atan2(p.Y - center.Y, p.X - center.X);
				float n = Noise(new Vector2(angle * 8, dist * 0.05f));
				float n2 = Noise(new Vector2(angle * 3 + 17, dist * 0.08f));
				// Irregular edge: base radius + noise creates fingers and indentations
				float edgeRadius = radius * (0.7f + 0.35f * n + 0.2f * n2);
				float t = dist / edgeRadius;
				if (t <= 1f)
				{
					float core = 1f - MathHelper.Clamp(t / 0.2f, 0f, 1f);
					float mid = 1f - MathHelper.Clamp((t - 0.35f) / 0.4f, 0f, 1f);
					float rim = 1f - MathHelper.Clamp((t - 0.8f) / 0.2f, 0f, 1f);
					float alpha = Math.Max(core, Math.Max(mid * 0.9f, rim * 0.5f));
					data[y * size + x] = new Color(1f, 1f, 1f, MathHelper.Clamp(alpha, 0f, 1f));
				}
				else
					data[y * size + x] = Color.Transparent;
			}
			tex.SetData(data);
			return tex;
		}

		private static Texture2D CreateRingTexture(int size)
		{
			var tex = new Texture2D(Main.instance.GraphicsDevice, size, size, false, SurfaceFormat.Color);
			var data = new Color[size * size];
			var center = new Vector2((size - 1) * 0.5f);
			float radius = size * 0.5f;

			for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float dist = Vector2.Distance(new Vector2(x, y), center);
				float t = dist / radius;
				if (t <= 1f)
				{
					float ringPeak = 0.5f;
					float ringWidth = 0.15f;
					float alpha = 0f;
					if (t >= ringPeak - ringWidth && t <= ringPeak + ringWidth)
						alpha = 1f - Math.Abs(t - ringPeak) / ringWidth;
					else if (t > ringPeak + ringWidth)
						alpha = Math.Max(0f, 1f - (t - ringPeak - ringWidth) / 0.3f);
					alpha *= 1f - t * 0.5f;
					data[y * size + x] = new Color(1f, 1f, 1f, MathHelper.Clamp(alpha, 0f, 1f));
				}
				else
					data[y * size + x] = Color.Transparent;
			}
			tex.SetData(data);
			return tex;
		}

		public override void PostDrawTiles()
		{
			if (Main.gameMenu || Main.dedServ || ActiveExplosions.Count == 0) return;

			EnsureTextures();

			if (!ShaderManager.TryGetShader("Luminance.QuadRenderer", out var quadShader))
				return;

			int now = (int)Main.GameUpdateCount;
			for (int i = ActiveExplosions.Count - 1; i >= 0; i--)
			{
				var ex = ActiveExplosions[i];
				int duration = ex.IsIgnition ? DurationIgnition : DurationIncandescent;
				int elapsed = now - ex.SpawnFrame;
				if (elapsed >= duration)
				{
					ActiveExplosions.RemoveAt(i);
					continue;
				}

				float progress = elapsed / (float)duration;
				DrawExplosion(ex.Center, progress, ex.IsIgnition, elapsed, quadShader);

				if (progress < 0.7f && elapsed % 2 == 0)
				{
					SpawnSustainedEmber(ex.Center, ex.IsIgnition);
					SpawnSustainedEmber(ex.Center, ex.IsIgnition);
					if (elapsed % 4 == 0)
						SpawnSustainedEmber(ex.Center, ex.IsIgnition);
				}
			}
		}

		/// <summary>Triggers an Incandescent (on-kill) or Ignition explosion.</summary>
		public static void TriggerExplosion(Vector2 worldCenter, bool isIgnition)
		{
			if (Main.dedServ) return;

			ActiveExplosions.Add(new ActiveExplosion
			{
				Center = worldCenter,
				SpawnFrame = (int)Main.GameUpdateCount,
				IsIgnition = isIgnition
			});

			SpawnParticleBurst(worldCenter, isIgnition);
			SpawnFireMotesTowardEnemies(worldCenter, isIgnition);
			AddScreenShake(worldCenter, isIgnition);
		}

		private static void SpawnParticleBurst(Vector2 center, bool isIgnition)
		{
			if (isIgnition)
			{
				for (int i = 0; i < 110; i++)
				{
					float angle = GetGoldenAngle(i, 110);
					float speed = Main.rand.NextFloat(8f, 24f) * Main.rand.NextFloat(0.9f, 1.3f);
					var vel = GetRadialDirection(angle) * speed;

					if (i % 3 == 0)
					{
						var chunk = Dust.NewDustPerfect(center, DustID.Torch, vel * 0.6f, 120, new Color(255, 60, 20), Main.rand.NextFloat(2f, 3.5f));
						chunk.noGravity = true;
						chunk.fadeIn = 1f;
					}
					else
					{
						var spark = Dust.NewDustPerfect(center, DustID.WhiteTorch, vel * Main.rand.NextFloat(0.7f, 1.4f), 90, default, Main.rand.NextFloat(1.8f, 3f));
						spark.noGravity = true;
						spark.fadeIn = 1.5f;
					}
					var fire = Dust.NewDustPerfect(center, DustID.Torch, vel * Main.rand.NextFloat(0.4f, 1.1f), 150, new Color(255, 80, 25), Main.rand.NextFloat(1.2f, 2.2f));
					fire.noGravity = true;
					if (i % 2 == 0)
					{
						var smoke = Dust.NewDustPerfect(center, DustID.Smoke, vel * 0.3f, 200, new Color(80, 20, 5), Main.rand.NextFloat(1.5f, 2.5f));
						smoke.noGravity = true;
					}
				}
			}
			else
			{
				// Incandescent: pure radial, no enemy bias—golden-angle distribution
				for (int i = 0; i < 90; i++)
				{
					float angle = GetGoldenAngle(i, 90);
					float speed = Main.rand.NextFloat(6f, 18f);
					var vel = GetRadialDirection(angle) * speed;

					var fire = Dust.NewDustPerfect(center, DustID.Torch, vel * Main.rand.NextFloat(0.7f, 1.3f), 160, new Color(255, 130, 40), Main.rand.NextFloat(1.4f, 2.4f));
					fire.noGravity = true;
					if (i % 2 == 0)
					{
						var spark = Dust.NewDustPerfect(center, DustID.WhiteTorch, vel * Main.rand.NextFloat(0.9f, 1.5f), 95, new Color(255, 220, 150), Main.rand.NextFloat(1.2f, 2f));
						spark.noGravity = true;
					}
					if (i % 3 == 0)
					{
						var ember = Dust.NewDustPerfect(center, DustID.Torch, vel * 0.6f, 130, new Color(255, 180, 70), Main.rand.NextFloat(1.2f, 1.8f));
						ember.noGravity = true;
					}
				}
			}
		}

		/// <summary>Fire motes: pure radial burst, golden-angle spaced. No directional bias.</summary>
		private static void SpawnFireMotesTowardEnemies(Vector2 center, bool isIgnition)
		{
			int count = isIgnition ? 25 : 16;
			for (int i = 0; i < count; i++)
			{
				float angle = GetGoldenAngle(i, count);
				var vel = GetRadialDirection(angle) * Main.rand.NextFloat(isIgnition ? 10f : 6f, isIgnition ? 20f : 14f);
				Color c = isIgnition ? new Color(255, 100, 40) : new Color(255, 190, 80);
				var d = Dust.NewDustPerfect(center, DustID.Torch, vel, 95, c, Main.rand.NextFloat(1f, 1.6f));
				d.noGravity = true;
				d.fadeIn = 0.8f;
			}
		}

		private static void SpawnSustainedEmber(Vector2 center, bool isIgnition)
		{
			int idx = (int)(Main.GameUpdateCount % 36);
			float angle = GetGoldenAngle(idx, 36);
			float speed = Main.rand.NextFloat(2f, 8f) * (isIgnition ? 1.4f : 1f);
			var vel = GetRadialDirection(angle) * speed;
			Color c = isIgnition ? new Color(255, 100, 40) : new Color(255, 170, 70);
			float scale = isIgnition ? Main.rand.NextFloat(1.2f, 2f) : Main.rand.NextFloat(0.8f, 1.3f);
			var d = Dust.NewDustPerfect(center, DustID.Torch, vel, 85, c, scale);
			d.noGravity = true;
		}

		private static void AddScreenShake(Vector2 center, bool isIgnition)
		{
			float strength = isIgnition ? 12f : 6f;
			ScreenShakeSystem.StartShakeAtPoint(center, strength, MathHelper.TwoPi, null, 0.25f, 600f, 200f);
		}

		private static void DrawExplosion(Vector2 center, float progress, bool isIgnition, int elapsed, ManagedShader quadShader)
		{
			if (_bloomLobe == null || _flameWaveTexture == null || _ringTexture == null) return;

			float screenH = Main.screenHeight;
			float baseSize = isIgnition ? screenH * 0.38f : screenH * 0.2f;

			// Incandescent: quick ease-out. Ignition: sharper initial burst, longer sustain
			float expansion = isIgnition
				? 1f - (float)Math.Pow(1f - progress, 1.5f)
				: 1f - (1f - progress) * (1f - progress);

			float scaleBase = baseSize / _bloomLobe.Width * expansion;

			float opacity = (float)Math.Sin(MathHelper.PiOver2 + progress * MathHelper.PiOver2);
			if (progress > 0.5f) opacity *= (1f - progress) / 0.5f;

			// Bright initial flash—Destiny 2 style white-hot burst
			float flash = elapsed <= 6 ? (1f - elapsed / 6f) : 0f;

			var gd = Main.instance.GraphicsDevice;
			var prevBlend = gd.BlendState;
			gd.BlendState = BlendState.Additive;

			if (isIgnition)
			{
				DrawIgnitionExplosion(center, progress, scaleBase, opacity, flash, quadShader);
			}
			else
			{
				DrawIncandescentExplosion(center, progress, scaleBase, opacity, flash, quadShader);
			}

			// Warm fire lighting—Destiny 2 Solar orange-gold glow
			float lightIntensity = opacity * (1.2f + flash * 2.5f);
			Lighting.AddLight(center, 1f * lightIntensity, 0.5f * lightIntensity, 0.15f * lightIntensity);

			gd.BlendState = prevBlend;
		}

		/// <summary>Incandescent: fiery burst—12 flame tongues, orange-gold, expanding ring.</summary>
		private static void DrawIncandescentExplosion(Vector2 center, float progress, float scaleBase, float opacity, float flash, ManagedShader quadShader)
		{
			// Outer edge—deep orange-red
			float waveScale = scaleBase * 1.6f;
			var drawPos = GetCenteredDrawPos(center, waveScale);
			var edgeColor = IncandescentEdge with { A = (byte)(opacity * 140) };
			PrimitiveRenderer.RenderQuad(_flameWaveTexture, drawPos, new Vector2(waveScale), 0f, edgeColor, quadShader);

			// Mid layer—orange body
			drawPos = GetCenteredDrawPos(center, scaleBase * 1.2f);
			var bodyColor = IncandescentBody with { A = (byte)(opacity * 250) };
			PrimitiveRenderer.RenderQuad(_flameWaveTexture, drawPos, new Vector2(scaleBase * 1.2f), 0f, bodyColor, quadShader);

			// Core—white-hot
			drawPos = GetCenteredDrawPos(center, scaleBase);
			var coreColor = Color.Lerp(IncandescentCore, Color.White, flash * 0.7f) with { A = (byte)(opacity * (0.95f + flash * 0.5f) * 255) };
			PrimitiveRenderer.RenderQuad(_flameWaveTexture, drawPos, new Vector2(scaleBase), 0f, coreColor, quadShader);

			// Jagged chaos overlays for fiery irregularity
			for (int i = 0; i < 4; i++)
			{
				float rot = i / 4f * MathHelper.TwoPi + progress * 0.5f;
				float s = scaleBase * 1.3f * (0.9f + 0.15f * (float)Math.Sin(i * 2.3f));
				drawPos = GetCenteredDrawPos(center, s);
				var tongueColor = new Color(255, 80, 20) with { A = (byte)(opacity * 85) };
				PrimitiveRenderer.RenderQuad(_chaosBlob, drawPos, new Vector2(s), rot, tongueColor, quadShader);
			}

			// Expanding ring wave front
			float ringProgress = Math.Min(progress * 1.4f, 1f);
			float ringOpacity = (float)Math.Sin(ringProgress * MathHelper.Pi) * 0.75f;
			if (ringOpacity > 0.02f)
			{
				float ringScale = scaleBase * 1.4f * (0.9f + ringProgress * 0.45f);
				drawPos = GetCenteredDrawPos(center, ringScale);
				var ringColor = IncandescentBody with { A = (byte)(ringOpacity * 255) };
				PrimitiveRenderer.RenderQuad(_ringTexture, drawPos, new Vector2(ringScale), 0f, ringColor, quadShader);
			}
		}

		/// <summary>Ignition: violent wave of flames, white-hot core, red plasma, jagged flame tongues.</summary>
		private static void DrawIgnitionExplosion(Vector2 center, float progress, float scaleBase, float opacity, float flash, ManagedShader quadShader)
		{
			// Base flame wave
			float waveScale = scaleBase * 1.6f;
			var drawPos = GetCenteredDrawPos(center, waveScale);
			var edgeColor = IgnitionEdge with { A = (byte)(opacity * 110) };
			PrimitiveRenderer.RenderQuad(_flameWaveTexture, drawPos, new Vector2(waveScale), 0f, edgeColor, quadShader);

			drawPos = GetCenteredDrawPos(center, scaleBase * 1.25f);
			var plasmaColor = IgnitionPlasma with { A = (byte)(opacity * 210) };
			PrimitiveRenderer.RenderQuad(_flameWaveTexture, drawPos, new Vector2(scaleBase * 1.25f), 0f, plasmaColor, quadShader);

			drawPos = GetCenteredDrawPos(center, scaleBase);
			var bodyColor = IgnitionBody with { A = (byte)(opacity * 250) };
			PrimitiveRenderer.RenderQuad(_flameWaveTexture, drawPos, new Vector2(scaleBase), 0f, bodyColor, quadShader);

			drawPos = GetCenteredDrawPos(center, scaleBase * 0.7f);
			var coreColor = Color.Lerp(IgnitionCore, IgnitionBody, 1f - flash) with { A = (byte)(opacity * (0.95f + flash) * 255) };
			PrimitiveRenderer.RenderQuad(_flameWaveTexture, drawPos, new Vector2(scaleBase * 0.7f), 0f, coreColor, quadShader);

			// Jagged flame tongues: chaos blob at varied rotations for violent fire character
			for (int i = 0; i < 5; i++)
			{
				float rot = i / 5f * MathHelper.TwoPi + progress * 0.8f;
				float tongueScale = scaleBase * 1.35f * (0.85f + 0.2f * (float)Math.Sin(i * 2.1f));
				drawPos = GetCenteredDrawPos(center, tongueScale);
				var tongueColor = new Color(200, 50, 15) with { A = (byte)(opacity * 100) };
				PrimitiveRenderer.RenderQuad(_chaosBlob, drawPos, new Vector2(tongueScale), rot, tongueColor, quadShader);
			}

			// Expanding ring
			float ringProgress = Math.Min(progress * 2f, 1f);
			float ringOpacity = (float)Math.Sin(ringProgress * MathHelper.Pi) * 0.9f;
			if (ringOpacity > 0.02f)
			{
				float ringScale = scaleBase * 1.5f * (0.75f + ringProgress * 0.7f);
				drawPos = GetCenteredDrawPos(center, ringScale);
				var ringColor = IgnitionBody with { A = (byte)(ringOpacity * 255) };
				PrimitiveRenderer.RenderQuad(_ringTexture, drawPos, new Vector2(ringScale), 0f, ringColor, quadShader);
			}
		}
	}
}
