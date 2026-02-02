using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminance.Core.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX
{
	/// <summary>
	/// Kinetic Tremors: pure force, ground-transmitted concussive pulses.
	/// Broken dust rings, chunky debris, shallow propagation. Zero glow, zero magic.
	/// </summary>
	public sealed class KineticTremorsVFXSystem : ModSystem
	{
		#region Parameter Block

		/// <summary>Tweakable parameters for Kinetic Tremors VFX.</summary>
		public static class Params
		{
			public static float DurationFramesPerPulse = 36;
			public static float RadiusGrowthExponent = 0.5f;
			public static float RingEdgeWidthMin = 6f;
			public static float RingEdgeWidthMax = 12f;
			public static float RingJaggedness = 0.18f;
			public static float Pulse2RadiusScale = 0.85f;
			public static float Pulse3RadiusScale = 0.7f;
			public static float Pulse2OpacityScale = 0.75f;
			public static float Pulse3OpacityScale = 0.5f;
			public static int ImpactBurstCount = 16;
			public static int DebrisCount = 9;
			public static int DustWakeCount = 8;
			public static float ScreenShakeStrength = 1.2f;
			public static float ScreenShakeDownBias = 0.6f;
			public static float DebrisShallowAngle = 0.35f;

			public static Color DustWhite = new(230, 230, 230);
			public static Color LightGray = new(189, 189, 189);
			public static Color RockBrown = new(140, 125, 110);
			public static Color Charcoal = new(60, 58, 55);
		}

		#endregion

		#region Instance

		private struct TremorPulse
		{
			public Vector2 Center;
			public int SpawnFrame;
			public int PulseIndex;
			public int Seed;
		}

		private static readonly List<TremorPulse> Pulses = new();

		#endregion

		#region Terrain Hook (stub for engine-agnostic)

		/// <summary>Sample ground Y at world X. Returns offset from center.Y for ring warping.</summary>
		private static float GetGroundOffset(Vector2 center, float angle, float radius)
		{
			Vector2 sample = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
			int tileX = (int)(sample.X / 16f);
			int tileY = (int)(sample.Y / 16f);
			if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
				return 0f;
			Tile t = Main.tile[tileX, tileY];
			if (t == null || !t.HasTile)
				return 0f;
			float groundY = tileY * 16f;
			return (sample.Y - groundY) * 0.02f;
		}

		#endregion

		#region Spawn API

		/// <summary>Spawn Kinetic Tremors: thump–thump–thump pulses.</summary>
		public static void SpawnKineticTremors(Vector2 origin, Vector2 forwardDir, float baseRadius, int pulses = 3, int seed = 0)
		{
			for (int i = 0; i < pulses; i++)
			{
				// Each pulse is triggered separately by the projectile; we only handle single-pulse VFX
				// The projectile calls TriggerPulse for each thump
			}
		}

		/// <summary>Spawn a single Kinetic aftershock.</summary>
		public static void SpawnKineticAftershock(Vector2 origin, float radius, float strength, int seed = 0)
		{
			TriggerPulse(origin, 2);
		}

		/// <summary>Legacy: triggers a single pulse (called per thump by KineticTremorsShockwave).</summary>
		public static void TriggerPulse(Vector2 center, int pulseIndex = 0)
		{
			if (Main.dedServ) return;
			int seed = Main.rand.Next();
			Pulses.Add(new TremorPulse { Center = center, SpawnFrame = (int)Main.GameUpdateCount, PulseIndex = pulseIndex, Seed = seed });
			SpawnImpactBurst(center, pulseIndex, seed);
			SpawnDebris(center, pulseIndex, seed);
			SpawnDustWake(center, pulseIndex, seed);
			ScreenShakeSystem.StartShakeAtPoint(center, Params.ScreenShakeStrength - pulseIndex * 0.3f, 0f, Vector2.UnitY * Params.ScreenShakeDownBias, 0.15f, 450f, 120f);
		}

		#endregion

		#region Particle Spawning

		private static readonly float GoldenAngle = 2.39996323f;

		private static void SpawnImpactBurst(Vector2 center, int pulseIndex, int seed)
		{
			int n = Math.Max(3, Params.ImpactBurstCount - pulseIndex * 4);
			Random rng = new Random(seed);
			for (int i = 0; i < n; i++)
			{
				float a = (i * GoldenAngle) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.2f;
				float spd = 3f + (float)rng.NextDouble() * 9f;
				Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
				float scale = 0.9f + (float)rng.NextDouble() * 1.4f;
				var d = Dust.NewDustPerfect(center, DustID.Smoke, dir * spd, 0, Color.Lerp(Params.DustWhite, Params.LightGray, (float)rng.NextDouble()), scale);
				d.noGravity = false;
				d.velocity.Y -= 0.3f + (float)rng.NextDouble() * 0.5f;
			}
		}

		private static void SpawnDebris(Vector2 center, int pulseIndex, int seed)
		{
			int n = Math.Max(2, Params.DebrisCount - pulseIndex);
			Random rng = new Random(seed + 1);
			for (int i = 0; i < n; i++)
			{
				float a = (i * GoldenAngle * 1.7f) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.4f;
				Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
				dir.Y -= Params.DebrisShallowAngle + (float)(rng.NextDouble() - 0.5) * 0.25f;
				dir.Normalize();
				int type = rng.Next(3) switch { 0 => DustID.Stone, 1 => DustID.Dirt, _ => DustID.Smoke };
				float spd = 2f + (float)rng.NextDouble() * 5.5f;
				float scale = 0.6f + (float)rng.NextDouble() * 0.9f;
				var d = Dust.NewDustPerfect(center, type, dir * spd, 0, Color.Lerp(Params.RockBrown, Params.Charcoal, (float)rng.NextDouble() * 0.6f), scale);
				d.noGravity = false;
			}
		}

		private static void SpawnDustWake(Vector2 center, int pulseIndex, int seed)
		{
			int n = Math.Max(2, Params.DustWakeCount - pulseIndex);
			Random rng = new Random(seed + 2);
			for (int i = 0; i < n; i++)
			{
				float a = (i * GoldenAngle * 0.9f + 0.3f) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.1f;
				float spd = 0.8f + (float)rng.NextDouble() * 2.8f;
				Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
				float scale = 0.4f + (float)rng.NextDouble() * 0.7f;
				var d = Dust.NewDustPerfect(center, DustID.Smoke, dir * spd, 100 + rng.Next(40), Params.LightGray with { A = (byte)(70 + rng.Next(50)) }, scale);
				d.noGravity = true;
			}
		}

		#endregion

		#region Draw

		public override void PostDrawTiles()
		{
			if (Main.gameMenu || Main.dedServ || Pulses.Count == 0) return;

			if (!ShaderManager.TryGetShader("Luminance.StandardPrimitiveShader", out var primShader) || primShader == null)
				return;

			float baseRadius = KineticTremorsPerk.ShockwaveRadiusTiles * 16f;
			float t = (float)Main.timeForVisualEffects * 0.02f;

			for (int i = Pulses.Count - 1; i >= 0; i--)
			{
				TremorPulse p = Pulses[i];
				int elapsed = (int)Main.GameUpdateCount - p.SpawnFrame;
				if (elapsed >= Params.DurationFramesPerPulse)
				{
					Pulses.RemoveAt(i);
					continue;
				}

				float progress = elapsed / Params.DurationFramesPerPulse;
				float expansion = 1f - (float)Math.Pow(1f - Math.Min(progress * 1.2f, 1f), Params.RadiusGrowthExponent);

				float radiusScale = p.PulseIndex switch { 0 => 1f, 1 => Params.Pulse2RadiusScale, _ => Params.Pulse3RadiusScale };
				float opacityScale = p.PulseIndex switch { 0 => 1f, 1 => Params.Pulse2OpacityScale, _ => Params.Pulse3OpacityScale };

				float radius = baseRadius * expansion * radiusScale;
				float opacity = (1f - progress) * opacityScale;
				if (opacity < 0.03f) continue;

				Color ringColor = Color.Lerp(Params.LightGray, Params.RockBrown, p.PulseIndex * 0.2f) with { A = (byte)(opacity * 210) };
				float edgeWidth = Params.RingEdgeWidthMin + (Params.RingEdgeWidthMax - Params.RingEdgeWidthMin) * (float)Math.Sin(t + p.PulseIndex * 1.5f) * 0.5f + (Params.RingEdgeWidthMax - Params.RingEdgeWidthMin) * 0.5f;

				var settings = new PrimitiveSettingsCircleEdge(
					interpolant => edgeWidth * (0.7f + 0.3f * (float)Math.Sin(interpolant * MathHelper.TwoPi * 8f + t + p.Seed * 0.02f)),
					_ => ringColor,
					interpolant =>
					{
						float angle = interpolant * MathHelper.TwoPi;
						float j1 = (float)Math.Sin(angle * 10f + t * 2.5f + p.Seed * 0.01f) * Params.RingJaggedness * radius;
						float j2 = (float)Math.Sin(angle * 18f - t * 1.5f + p.Seed * 0.015f) * Params.RingJaggedness * 0.6f * radius;
						float groundOffset = GetGroundOffset(p.Center, angle, radius) * 12f;
						return radius * (0.88f + j1 / radius + j2 / radius) + groundOffset;
					},
					false, primShader);
				PrimitiveRenderer.RenderCircleEdge(p.Center, settings, 88);
			}
		}

		#endregion
	}
}
