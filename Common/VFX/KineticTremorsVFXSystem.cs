using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
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
	/// Kinetic Tremors: expanding thin circle followed by a violent shockwave.
	/// 3-tile radius. Clean leading ring, then a broader pressure-wave band.
	/// </summary>
	public sealed class KineticTremorsVFXSystem : ModSystem
	{
		private struct ActivePulse
		{
			public Vector2 Center;
			public int SpawnFrame;
		}

		private static readonly List<ActivePulse> ActivePulses = new();
		private const int DurationFrames = 28;

		private static readonly Color RingColor = new(255, 255, 255);
		private static readonly Color ShockwaveInner = new(235, 245, 255);
		private static readonly Color ShockwaveOuter = new(180, 205, 245);

		private static float GoldenAngle(int i, int n) => (i * 2.39996323f) % MathHelper.TwoPi;
		private static Vector2 RadialDir(float a) => new((float)Math.Cos(a), (float)Math.Sin(a));

		public static void TriggerPulse(Vector2 worldCenter)
		{
			if (Main.dedServ) return;

			ActivePulses.Add(new ActivePulse { Center = worldCenter, SpawnFrame = (int)Main.GameUpdateCount });
			SpawnParticles(worldCenter);
			ScreenShakeSystem.StartShakeAtPoint(worldCenter, 2f, MathHelper.TwoPi, null, 0.15f, 350f, 120f);
		}

		private static void SpawnParticles(Vector2 center)
		{
			if (Main.dedServ) return;

			const int dustType = DustID.WhiteTorch;
			Color c = new Color(220, 235, 255);
			int n = 16;
			for (int i = 0; i < n; i++)
			{
				float a = GoldenAngle(i, n);
				var dir = RadialDir(a);
				float spd = Main.rand.NextFloat(2f, 4f);
				var d = Dust.NewDustPerfect(center, dustType, dir * spd, 45, c, Main.rand.NextFloat(1f, 1.4f));
				d.noGravity = true;
			}
		}

		public override void PostDrawTiles()
		{
			if (Main.gameMenu || Main.dedServ || ActivePulses.Count == 0) return;
			if (!ShaderManager.TryGetShader("Luminance.QuadRenderer", out var quadShader)) return;
			if (!ShaderManager.TryGetShader("Luminance.StandardPrimitiveShader", out var primShader)) return;

			Texture2D bloomTex;
			try { bloomTex = MiscTexturesRegistry.BloomCircleSmall.Value; } catch { return; }
			if (bloomTex == null || bloomTex.IsDisposed) return;

			int now = (int)Main.GameUpdateCount;
			for (int i = ActivePulses.Count - 1; i >= 0; i--)
			{
				var p = ActivePulses[i];
				int elapsed = now - p.SpawnFrame;
				if (elapsed >= DurationFrames)
				{
					ActivePulses.RemoveAt(i);
					continue;
				}

				DrawPulse(p.Center, elapsed / (float)DurationFrames, quadShader, primShader, bloomTex);
			}
		}

		private static void DrawPulse(Vector2 center, float progress, ManagedShader quadShader, ManagedShader primShader, Texture2D bloomTex)
		{
			float baseRadiusPx = KineticTremorsPerk.ShockwaveRadiusTiles * 16f;
			float opacity = (float)Math.Sin(progress * MathHelper.Pi) * 0.92f;

			// 1) THIN CIRCLE—expands first, crisp leading edge
			float ringExpansion = 1f - (float)Math.Pow(1f - Math.Min(progress * 1.25f, 1f), 0.55f);
			float ringRadius = baseRadiusPx * ringExpansion;

			// 2) VIOLENT SHOCKWAVE—emanates after, thick pressure band behind the thin circle
			float shockExpansion = 1f - (float)Math.Pow(1f - Math.Min(progress * 1.05f, 1f), 0.7f);
			float shockRadius = baseRadiusPx * shockExpansion * 0.85f; // Lags behind the thin circle
			float shockOpacity = (float)Math.Sin(progress * MathHelper.Pi) * 0.9f;
			if (progress < 0.2f) shockOpacity *= progress / 0.2f;

			var gd = Main.instance.GraphicsDevice;
			var prevBlend = gd.BlendState;
			gd.BlendState = BlendState.Additive;

			float t = progress * 20f;
			if (primShader != null && opacity > 0.01f)
			{
				// 2) VIOLENT SHOCKWAVE—Perlin-modulated radius for organic seismic crack feel
				if (shockOpacity > 0.02f)
				{
					float shockWidth = 22f;
					var shockSettings = new PrimitiveSettingsCircleEdge(
						interpolant => shockWidth + 4f * PerlinNoise.Sample(interpolant * 8f + t * 0.3f, t * 0.2f),
						_ => ShockwaveOuter with { A = (byte)(shockOpacity * 140) },
						interpolant => shockRadius * (0.92f + 0.08f * PerlinNoise.Fbm(interpolant * 6f + t * 0.5f, t, 2, 2f, 0.5f)),
						false, primShader);
					PrimitiveRenderer.RenderCircleEdge(center, shockSettings, 72);

					var shockInnerSettings = new PrimitiveSettingsCircleEdge(
						interpolant => 10f + 2f * PerlinNoise.Sample(interpolant * 4f, t * 0.15f),
						_ => ShockwaveInner with { A = (byte)(shockOpacity * 200) },
						interpolant => shockRadius * (0.92f + 0.08f * PerlinNoise.Fbm(interpolant * 6f + t * 0.5f, t, 2, 2f, 0.5f)) - shockWidth * 0.35f,
						false, primShader);
					PrimitiveRenderer.RenderCircleEdge(center, shockInnerSettings, 64);
				}

				// 1) THIN CIRCLE—Perlin adds subtle irregularity to leading edge
				float thinWidth = 3f;
				var thinSettings = new PrimitiveSettingsCircleEdge(
					interpolant => thinWidth + 1f * PerlinNoise.Sample(interpolant * 12f + t * 0.4f, t * 0.1f),
					_ => RingColor with { A = (byte)(opacity * 255) },
					interpolant => ringRadius * (0.96f + 0.04f * PerlinNoise.Fbm(interpolant * 8f + t * 0.3f, t * 0.2f, 2, 2f, 0.5f)),
					false, primShader);
				PrimitiveRenderer.RenderCircleEdge(center, thinSettings, 64);
			}

			// Subtle center flash
			float flashScale = baseRadiusPx * 0.4f * (1f - progress);
			float tw = bloomTex.Width;
			float th = bloomTex.Height;
			float half = Math.Max(tw, th) * 0.5f * (flashScale / 128f);
			var drawPos = center - new Vector2(half, -half);
			PrimitiveRenderer.RenderQuad(bloomTex, drawPos, new Vector2(flashScale / tw, flashScale / th), 0f,
				RingColor with { A = (byte)(opacity * 60) }, quadShader);

			Lighting.AddLight(center, 0.6f * opacity * 0.45f, 0.7f * opacity * 0.45f, 1f * opacity * 0.45f);
			gd.BlendState = prevBlend;
		}
	}
}
