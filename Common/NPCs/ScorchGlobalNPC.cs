using System;
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
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
		private const int IgniteDelayTicks = 60; // ~1 second delay before explosion
		private const int DecayRatePerSecond = 10;
		private const int DecayIntervalTicks = 6; // 0.1 seconds
		private const int DotIntervalTicks = 30; // 0.5 seconds
		private const float ScorchDamageRatioAtMaxStacks = 0.1f; // 1/10th of weapon damage at 100 stacks

		// Solar color from Bullet.cs
		private static readonly Color SolarColor = new Color(236, 85, 0);
		private static readonly Color BrightSolarColor = new Color(255, 150, 50);

		public int ScorchStacks { get; private set; }
		private int referenceWeaponDamage; // Used for DoT formula: damage = weaponDamage * 0.1 * (stacks/100)
		private int decayTimer;
		private int dotTimer;
		private int igniteDelayTimer; // Countdown to ignition explosion (~1 second after hitting 100 stacks)
		private bool isIgniting;

		/// <param name="referenceWeaponDamage">Weapon damage used for DoT formula; at 100 stacks, DoT = this * 0.1</param>
		public void ApplyScorch(NPC npc, int stacks, int referenceWeaponDamage = 30)
		{
			if (npc == null || !npc.active)
				return;

			if (isIgniting || igniteDelayTimer > 0)
				return;

			ScorchStacks = Math.Min(ScorchStacks + stacks, MaxScorchStacks);
			this.referenceWeaponDamage = Math.Max(this.referenceWeaponDamage, referenceWeaponDamage);

			if (ScorchStacks >= IgniteThreshold && igniteDelayTimer <= 0)
			{
				igniteDelayTimer = IgniteDelayTicks;
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

				ApplyExplosionDamage(center, IgniteDamage, IgniteRadius, excludeNpcId: -1);
			}

			// Clear scorch stacks and delay state after ignite
			ScorchStacks = 0;
			igniteDelayTimer = 0;
			isIgniting = false;
		}

		public override void AI(NPC npc)
		{
			if (ScorchStacks <= 0 && igniteDelayTimer <= 0)
				return;

			// Delayed ignition: countdown then explode
			if (igniteDelayTimer > 0)
			{
				igniteDelayTimer--;
				if (igniteDelayTimer <= 0)
				{
					TriggerIgnite(npc);
					return;
				}
				SpawnScorchDust(npc);
				return;
			}

			// Decay scorch stacks
			decayTimer++;
			if (decayTimer >= DecayIntervalTicks)
			{
				decayTimer = 0;
				int decayAmount = DecayRatePerSecond / 10; // 10 ticks per second at 60fps
				ScorchStacks = Math.Max(0, ScorchStacks - decayAmount);
			}

			// DoT damage: ramps to 1/10th of weapon damage at 100 stacks
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

			// Formula: damage = weaponDamage * 0.1 * (stacks/100), minimum 1 if stacks > 0
			if (ScorchStacks <= 0)
				return;

			float stackRatio = ScorchStacks / (float)MaxScorchStacks;
			int damage = Math.Max(1, (int)(referenceWeaponDamage * ScorchDamageRatioAtMaxStacks * stackRatio));
			npc.SimpleStrikeNPC(damage, 0, false, 0f);
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

		/// <summary>
		/// Triggers the Incandescent explosion on kill: 1/4 weapon damage, 4 tiles, 40 scorch stacks.
		/// Called from Destiny2PerkProjectile when a projectile with Incandescent kills an NPC.
		/// </summary>
		internal static void TriggerIncandescentExplosion(Vector2 center, int explosionDamage, int excludeNpcId)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || explosionDamage <= 0)
				return;

			int weaponDamage = explosionDamage * 4; // explosion is 1/4 weapon damage
			SpawnIncandescentExplosionDust(center);
			ApplyExplosionDamage(center, explosionDamage, IncandescentPerk.ExplosionRadius, excludeNpcId);
			ApplyScorchInRadius(center, IncandescentPerk.ExplosionRadius, IncandescentPerk.ScorchStacksApplied, weaponDamage, excludeNpcId);

			if (!Main.dedServ)
				SoundEngine.PlaySound(SoundID.Item14, center);
		}

		internal static void SpawnIncandescentExplosionDust(Vector2 center)
		{
			if (Main.dedServ)
				return;

			// Core flash
			for (int i = 0; i < 24; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f) * Main.rand.NextFloat(0.8f, 1.6f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Torch, velocity, 80, SolarColor, 1.8f);
				dust.noGravity = true;
				dust.fadeIn = 1.0f;
			}

			// Expanding ring
			const int ringCount = 28;
			for (int i = 0; i < ringCount; i++)
			{
				float angle = MathHelper.TwoPi * (i / (float)ringCount);
				Vector2 dir = angle.ToRotationVector2();
				Vector2 pos = center + dir * Main.rand.NextFloat(6f, 12f);
				Vector2 vel = dir * Main.rand.NextFloat(2f, 4f);
				Dust dust = Dust.NewDustPerfect(pos, DustID.Torch, vel, 60, BrightSolarColor, 1.4f);
				dust.noGravity = true;
			}

			// Embers
			for (int i = 0; i < 16; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f) * Main.rand.NextFloat(0.4f, 1.1f);
				Dust dust = Dust.NewDustPerfect(center, DustID.SparkForLightDisc, velocity, 0, BrightSolarColor, 1.1f);
				dust.noGravity = true;
			}

			// Smoke
			for (int i = 0; i < 12; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(2f, 2f) * Main.rand.NextFloat(0.3f, 0.8f);
				Dust dust = Dust.NewDustPerfect(center, DustID.Smoke, velocity, 140, Color.DarkGray, 1.1f);
				dust.noGravity = false;
			}

			Lighting.AddLight(center, 0.9f, 0.35f, 0f);
		}

		internal static void ApplyExplosionDamage(Vector2 center, int damage, float radius, int excludeNpcId)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || damage <= 0)
				return;

			float radiusSq = radius * radius;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC target = Main.npc[i];
				if (!target.CanBeChasedBy())
					continue;
				if (target.whoAmI == excludeNpcId)
					continue;
				if (Vector2.DistanceSquared(center, target.Center) > radiusSq)
					continue;

				int direction = target.Center.X < center.X ? -1 : 1;
				target.SimpleStrikeNPC(damage, direction, false, 0f);
			}
		}

		internal static void ApplyScorchInRadius(Vector2 center, float radius, int scorchStacks, int referenceWeaponDamage, int excludeNpcId)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || scorchStacks <= 0)
				return;

			float radiusSq = radius * radius;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC target = Main.npc[i];
				if (!target.CanBeChasedBy())
					continue;
				if (target.whoAmI == excludeNpcId)
					continue;
				if (Vector2.DistanceSquared(center, target.Center) > radiusSq)
					continue;

				ScorchGlobalNPC scorchTarget = target.GetGlobalNPC<ScorchGlobalNPC>();
				scorchTarget?.ApplyScorch(target, scorchStacks, referenceWeaponDamage);
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
