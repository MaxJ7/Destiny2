using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.NPCs
{
	public sealed class ScorchGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		private const int MaxScorchStacks = 100;
		private const int IgniteThreshold = 100;
		private const int IgniteDamage = 90;
		private const float IgniteRadiusTiles = 6f;
		private const float IgniteRadius = IgniteRadiusTiles * 16f;
		private const int DecayRatePerSecond = 10;
		private const int DecayIntervalTicks = 6; // 0.1 seconds
		private const int DotIntervalTicks = 30; // 0.5 seconds

		// Solar color from Bullet.cs
		private static readonly Color SolarColor = new Color(236, 85, 0);
		private static readonly Color BrightSolarColor = new Color(255, 150, 50);

		public int ScorchStacks { get; private set; }
		private int decayTimer;
		private int dotTimer;
		private bool isIgniting;

		public void ApplyScorch(NPC npc, int stacks)
		{
			if (isIgniting)
				return;

			ScorchStacks = Math.Min(ScorchStacks + stacks, MaxScorchStacks);

			if (ScorchStacks >= IgniteThreshold)
			{
				TriggerIgnite(npc);
			}
		}

		private void TriggerIgnite(NPC npc)
		{
			if (isIgniting || npc == null || !npc.active)
				return;

			isIgniting = true;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 center = npc.Center;

				// Spawn ignite explosion visuals
				SpawnIgniteExplosion(center);

				ApplyScorchExplosion(center, IgniteDamage, IgniteRadius, 20, damageExcludeNpcId: -1, scorchExcludeNpcId: npc.whoAmI);
			}

			// Clear scorch stacks after ignite
			ScorchStacks = 0;
			isIgniting = false;
		}

		public override void AI(NPC npc)
		{
			if (ScorchStacks <= 0)
				return;

			// Decay scorch stacks
			decayTimer++;
			if (decayTimer >= DecayIntervalTicks)
			{
				decayTimer = 0;
				int decayAmount = DecayRatePerSecond / 10; // 10 ticks per second at 60fps
				ScorchStacks = Math.Max(0, ScorchStacks - decayAmount);
			}

			// DoT damage
			dotTimer++;
			if (dotTimer >= DotIntervalTicks)
			{
				dotTimer = 0;
				ApplyDotDamage(npc);
			}

			// Visual effects
			SpawnScorchDust(npc);
		}

		private void ApplyDotDamage(NPC npc)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Damage scales from 1 to 10 based on scorch stacks (1 at 0 stacks, 10 at 100 stacks)
			float stackRatio = ScorchStacks / (float)MaxScorchStacks;
			int damage = (int)MathHelper.Lerp(1f, 10f, stackRatio);

			if (damage > 0)
			{
				npc.SimpleStrikeNPC(damage, 0, false, 0f);
			}
		}

		private void SpawnScorchDust(NPC npc)
		{
			if (Main.dedServ)
				return;

			// Intensity increases with stacks
			float stackRatio = ScorchStacks / (float)MaxScorchStacks;
			int dustCount = (int)MathHelper.Lerp(1f, 4f, stackRatio);

			for (int i = 0; i < dustCount; i++)
			{
				Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f);
				Vector2 velocity = new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.5f));

				// Interpolate color based on stack intensity
				Color dustColor = Color.Lerp(SolarColor, BrightSolarColor, stackRatio);

				Dust dust = Dust.NewDustPerfect(position, DustID.Torch, velocity, 100, dustColor, 1.0f + stackRatio);
				dust.noGravity = true;
				dust.fadeIn = 0.5f;
			}

			// Add light that intensifies with stacks
			float lightIntensity = 0.2f + (0.4f * stackRatio);
			Lighting.AddLight(npc.Center, lightIntensity * 0.9f, lightIntensity * 0.3f, 0f);
		}

		private static void SpawnIgniteExplosion(Vector2 center)
		{
			if (Main.dedServ)
				return;

			// Large explosion burst
			for (int i = 0; i < 50; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f) * Main.rand.NextFloat(1f, 2f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Torch, velocity, 100, SolarColor, 2.5f);
				dust.noGravity = true;
				dust.fadeIn = 1.5f;
			}

			// Secondary flames
			for (int i = 0; i < 30; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f) * Main.rand.NextFloat(0.8f, 1.5f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Torch, velocity, 50, BrightSolarColor, 2f);
				dust.noGravity = true;
			}

			// Smoke
			for (int i = 0; i < 15; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f) * Main.rand.NextFloat(0.5f, 1f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Smoke, velocity, 150, Color.DarkGray, 1.5f);
				dust.noGravity = false;
			}

			// Intense lighting
			Lighting.AddLight(center, 1f, 0.4f, 0f);

			// Spawn some sparkles
			for (int i = 0; i < 20; i++)
			{
				float angle = MathHelper.TwoPi * (i / 20f);
				Vector2 dir = angle.ToRotationVector2();
				Vector2 pos = center + dir * Main.rand.NextFloat(10f, 40f);
				Vector2 vel = dir * Main.rand.NextFloat(2f, 4f);
				Dust dust = Dust.NewDustPerfect(pos, DustID.SparkForLightDisc, vel, 0, BrightSolarColor, 1f);
				dust.noGravity = true;
			}
		}

		internal static void SpawnIncandescentExplosionDust(Vector2 center)
		{
			if (Main.dedServ)
				return;

			// Main explosion burst
			for (int i = 0; i < 30; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f) * Main.rand.NextFloat(0.8f, 1.6f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Torch, velocity, 100, SolarColor, 2.0f);
				dust.noGravity = true;
				dust.fadeIn = 1.2f;
			}

			// Secondary sparks
			for (int i = 0; i < 20; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f) * Main.rand.NextFloat(0.5f, 1.2f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Torch, velocity, 50, BrightSolarColor, 1.5f);
				dust.noGravity = true;
			}

			// Smoke
			for (int i = 0; i < 10; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(2f, 2f) * Main.rand.NextFloat(0.3f, 0.8f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Smoke, velocity, 150, Color.DarkGray, 1.2f);
				dust.noGravity = false;
			}

			// Lighting
			Lighting.AddLight(center, 0.8f, 0.3f, 0f);
		}

		internal static void ApplyScorchExplosion(Vector2 center, int damage, float radius, int scorchStacks, int damageExcludeNpcId, int scorchExcludeNpcId)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			float radiusSq = radius * radius;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC target = Main.npc[i];
				if (!target.CanBeChasedBy())
					continue;

				if (Vector2.DistanceSquared(center, target.Center) > radiusSq)
					continue;

				if (damage > 0 && target.whoAmI != damageExcludeNpcId)
				{
					int direction = target.Center.X < center.X ? -1 : 1;
					target.SimpleStrikeNPC(damage, direction, false, 0f);
				}

				if (scorchStacks > 0 && target.whoAmI != scorchExcludeNpcId)
				{
					ScorchGlobalNPC scorchTarget = target.GetGlobalNPC<ScorchGlobalNPC>();
					scorchTarget?.ApplyScorch(target, scorchStacks);
				}
			}
		}

		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (ScorchStacks <= 0)
				return;

			// Apply orange tint to the NPC based on scorch intensity
			float stackRatio = ScorchStacks / (float)MaxScorchStacks;
			float tintStrength = 0.2f + (0.3f * stackRatio);

			// Blend towards solar orange
			drawColor.R = (byte)MathHelper.Lerp(drawColor.R, 255, tintStrength);
			drawColor.G = (byte)MathHelper.Lerp(drawColor.G, 100, tintStrength * 0.4f);
			drawColor.B = (byte)MathHelper.Lerp(drawColor.B, 0, tintStrength);
		}
	}
}
