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
	/// Solar VFX: Destiny-style. Ignition flash, turbulent flame tongues, shard embers, fast burn-out.
	/// Color ramp: #FFF6E5 → yellow → orange → ember red → charcoal. Additive + alpha layers.
	/// </summary>
	public sealed class SolarVFXSystem : ModSystem
	{
		#region Parameter Block

		/// <summary>Tweakable parameters for Solar VFX.</summary>
		public static class Params
		{
			public static float ImpactDurationFrames = 24;   // ~0.4s
			public static float IgnitionDurationFrames = 36; // ~0.6s
			public static float ImpactRadiusTiles = 3f;
			public static float IgnitionRadiusTiles = 5f;

			public static Color CoreWhite = new(255, 246, 229);  // #FFF6E5
			public static Color HotYellow = new(255, 210, 74);
			public static Color HotOrange = new(255, 154, 31);
			public static Color EmberRed = new(201, 42, 0);
			public static Color CharcoalResidue = new(80, 60, 50);

			public static int FlashParticleCount = 12;
			public static int FlameTongueCount = 28;
			public static int EmberShardCount = 22;
			public static int SmokeWispCount = 9;
			public static int FlashFrames = 2;
			public static float EmberUpwardBias = 0.35f;
			public static float LightFlickerAmount = 0.18f;
			public static float ExplosionShapeIrregularityImpact = 0.24f;
			public static float ExplosionShapeIrregularityIgnition = 0.32f;
		}

		#endregion

		#region Instance

		private struct SolarInstance
		{
			public Vector2 Center;
			public int SpawnFrame;
			public bool IsIgnition;
			public int Seed;
		}

		private static readonly List<SolarInstance> Instances = new();

		#endregion

		#region Spawn API

		/// <summary>Spawn a Solar impact (small explosion).</summary>
		public static void SpawnSolarImpact(Vector2 center, Vector2 normal, float scale, int seed = 0)
		{
			if (Main.dedServ) return;
			Instances.Add(new SolarInstance { Center = center, SpawnFrame = (int)Main.GameUpdateCount, IsIgnition = false, Seed = seed != 0 ? seed : Main.rand.Next() });
			SpawnParticles(center, false, seed);
		}

		/// <summary>Spawn a Solar ignition (larger chain reaction).</summary>
		public static void SpawnSolarIgnition(Vector2 center, float radius, float intensity, int seed = 0)
		{
			if (Main.dedServ) return;
			Instances.Add(new SolarInstance { Center = center, SpawnFrame = (int)Main.GameUpdateCount, IsIgnition = true, Seed = seed != 0 ? seed : Main.rand.Next() });
			SpawnParticles(center, true, seed);
		}

		/// <summary>Legacy: triggers impact or ignition.</summary>
		public static void TriggerExplosion(Vector2 center, bool isIgnition)
		{
			if (isIgnition)
				SpawnSolarIgnition(center, Params.IgnitionRadiusTiles * 16f, 1f);
			else
				SpawnSolarImpact(center, Vector2.UnitY, 1f);
		}

		#endregion

		#region Particle Archetypes

		private static void SpawnParticles(Vector2 center, bool isIgnition, int seed)
		{
			Random rng = seed != 0 ? new Random(seed) : new Random(Main.rand.Next());
			int flashN = Params.FlashParticleCount;
			int flameN = isIgnition ? Params.FlameTongueCount : Params.FlameTongueCount - 4;
			int emberN = isIgnition ? Params.EmberShardCount : Params.EmberShardCount - 4;

			float goldenAngle = 2.39996323f;
			for (int i = 0; i < flashN; i++)
			{
				float a = (i * goldenAngle) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.2f;
				Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
				dir.Y -= 0.25f + (float)rng.NextDouble() * 0.2f;
				dir.Normalize();
				float spd = 6f + (float)rng.NextDouble() * 14f;
				float scale = 1.4f + (float)rng.NextDouble() * 1.2f;
				var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 0, Params.CoreWhite, scale);
				d.noGravity = true;
				d.alpha = 30 + rng.Next(60);
			}

			for (int i = 0; i < flameN; i++)
			{
				float a = (i * goldenAngle * 0.7f) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.5f;
				Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
				dir.Y -= Params.EmberUpwardBias + (float)(rng.NextDouble() - 0.5) * 0.25f;
				dir.Normalize();
				float spd = isIgnition ? 4f + (float)rng.NextDouble() * 14f : 3f + (float)rng.NextDouble() * 11f;
				Color c = Color.Lerp(Params.HotYellow, Params.HotOrange, (float)rng.NextDouble() * 0.85f);
				float scale = 0.9f + (float)rng.NextDouble() * 1.4f;
				var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 70 + rng.Next(40), c, scale);
				d.noGravity = true;
			}

			for (int i = 0; i < emberN; i++)
			{
				float a = (i * goldenAngle * 1.3f + 0.5f) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.6f;
				Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
				dir.Y -= 0.35f - (float)rng.NextDouble() * 0.2f;
				dir.Normalize();
				float spd = 4f + (float)rng.NextDouble() * 14f;
				Color c = Color.Lerp(Params.HotOrange, Params.EmberRed, (float)rng.NextDouble() * 0.8f);
				float scale = 0.6f + (float)rng.NextDouble() * 0.9f;
				var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 50 + rng.Next(40), c, scale);
				d.noGravity = true;
			}

			if (isIgnition && Params.SmokeWispCount > 0)
			{
				for (int i = 0; i < Params.SmokeWispCount; i++)
				{
					float a = (float)rng.NextDouble() * MathHelper.TwoPi;
					Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
					dir.Y -= 0.15f;
					dir.Normalize();
					float spd = 2f + (float)rng.NextDouble() * 4f;
					var d = Dust.NewDustPerfect(center, DustID.Smoke, dir * spd, 140, Params.CharcoalResidue, 0.6f + (float)rng.NextDouble() * 0.4f);
					d.noGravity = true;
				}
			}
		}

		private static void SpawnSustainedEmbers(Vector2 center, bool isIgnition, float progress, int frame)
		{
			if (progress > 0.55f) return;
			int n = Main.rand.Next(2, 5);
			float baseA = frame * 0.8f + Main.rand.NextFloat(0, MathHelper.TwoPi);
			for (int i = 0; i < n; i++)
			{
				float a = baseA + i * 1.4f + Main.rand.NextFloat(-0.8f, 0.8f);
				Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
				dir.Y -= Params.EmberUpwardBias + Main.rand.NextFloat(-0.2f, 0.15f);
				dir.Normalize();
				float spd = Main.rand.NextFloat(4f, 12f) * (1f - progress * 0.8f);
				Color c = Color.Lerp(Params.HotOrange, Params.EmberRed, progress + Main.rand.NextFloat(-0.2f, 0.3f));
				var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 55 + Main.rand.Next(30), c, Main.rand.NextFloat(0.6f, 1.3f));
				d.noGravity = true;
			}
		}

		#endregion

		#region Draw

		public override void PostDrawTiles()
		{
			if (Main.gameMenu || Main.dedServ || Instances.Count == 0) return;

			bool useShader = ShaderManager.TryGetShader("Destiny2.SolarExplosionShader", out var shader);
			Texture2D fireNoise = null, edgeNoise = null, invisiblePixel = null, bloomTex = null;
			if (useShader)
			{
				try
				{
					fireNoise = MiscTexturesRegistry.TurbulentNoise.Value;
					edgeNoise = MiscTexturesRegistry.WavyBlotchNoise.Value;
					invisiblePixel = MiscTexturesRegistry.InvisiblePixel.Value;
				}
				catch { useShader = false; }
				if (fireNoise == null || edgeNoise == null || invisiblePixel == null) useShader = false;
			}
			try { bloomTex = MiscTexturesRegistry.BloomCircleSmall.Value; } catch { }

			int now = (int)Main.GameUpdateCount;
			float t = (float)Main.timeForVisualEffects * 0.04f;

			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			for (int i = Instances.Count - 1; i >= 0; i--)
			{
				var inst = Instances[i];
				float dur = inst.IsIgnition ? Params.IgnitionDurationFrames : Params.ImpactDurationFrames;
				int elapsed = now - inst.SpawnFrame;
				if (elapsed >= dur)
				{
					Instances.RemoveAt(i);
					continue;
				}
				float progress = elapsed / dur;
				float radiusPx = (inst.IsIgnition ? Params.IgnitionRadiusTiles : Params.ImpactRadiusTiles) * 16f;

				bool inFlashPhase = elapsed < Params.FlashFrames;
				float flash = elapsed <= 6 ? (1f - elapsed / 6f) : 0f;
				float opacity = (1f - progress) * (1f - progress) * 0.9f * (1f + flash * 0.5f);
				float flicker = 1f + (float)Math.Sin(t * 20f + inst.Seed) * Params.LightFlickerAmount;
				opacity *= flicker;

				Color accent = inFlashPhase || flash > 0.5f ? Params.CoreWhite : flash > 0.4f ? Params.HotYellow : Color.Lerp(Params.HotOrange, Params.EmberRed, progress * 0.7f);

				if (useShader && shader != null && fireNoise != null && edgeNoise != null && invisiblePixel != null)
				{
					shader.TrySetParameter("time", t + inst.Seed * 0.001f);
					shader.TrySetParameter("lifetimeRatio", progress);
					shader.TrySetParameter("explosionShapeIrregularity", inst.IsIgnition ? Params.ExplosionShapeIrregularityIgnition : Params.ExplosionShapeIrregularityImpact);
					shader.TrySetParameter("accentColor", accent.ToVector4());
					shader.SetTexture(fireNoise, 1, SamplerState.LinearWrap);
					shader.SetTexture(edgeNoise, 2, SamplerState.LinearWrap);
					shader.Apply();
					Vector2 origin = new Vector2(invisiblePixel.Width, invisiblePixel.Height) * 0.5f;
					Main.spriteBatch.Draw(invisiblePixel, inst.Center - Main.screenPosition, null, Color.White * opacity, 0f, origin, radiusPx * 2.4f, SpriteEffects.None, 0f);
				}
				else if (bloomTex != null)
				{
					float expansion = 1f - (float)Math.Pow(1f - Math.Min(progress * 1.2f, 1f), 0.55f);
					Vector2 origin = new Vector2(bloomTex.Width, bloomTex.Height) * 0.5f;
					Vector2 pos = inst.Center - Main.screenPosition;
					float scale1 = radiusPx * expansion * 2.2f / bloomTex.Width;
					Main.spriteBatch.Draw(bloomTex, pos, null, accent * opacity, 0f, origin, scale1, SpriteEffects.None, 0f);
					float scale2 = radiusPx * expansion * (1.85f + (float)Math.Sin(inst.Seed * 0.01f) * 0.15f) / bloomTex.Width;
					Main.spriteBatch.Draw(bloomTex, pos + new Vector2((float)Math.Sin(inst.Seed * 0.03f) * 3f, (float)Math.Cos(inst.Seed * 0.02f) * 2f), null, accent * opacity * 0.5f, 0f, origin, scale2, SpriteEffects.None, 0f);
				}

				if (elapsed <= 14 && elapsed % 2 == 0)
					SpawnSustainedEmbers(inst.Center, inst.IsIgnition, progress, elapsed);

				float light = opacity * (1.5f + flash * 2.2f) * flicker;
				Lighting.AddLight(inst.Center, 1f * light, 0.55f * light, 0.18f * light);
			}

			Main.spriteBatch.End();
		}

		#endregion
	}
}
