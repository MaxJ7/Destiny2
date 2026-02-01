using System;
using Destiny2.Common.Perks;
using Destiny2.Common.VFX;
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

		/// <summary>Thin flickering flame tongues and embers—controlled crackling, not explosive.</summary>
		private void SpawnScorchDust(NPC npc)
		{
			if (Main.dedServ)
				return;

			float stackRatio = ScorchStacks / (float)MaxScorchStacks;

			// Low stacks: sparse embers. High stacks: denser, faster embers, shift toward white-hot
			int dustCount = (int)MathHelper.Lerp(2f, 8f, stackRatio);
			float emberSpeed = MathHelper.Lerp(0.5f, 2f, stackRatio);

			for (int i = 0; i < dustCount; i++)
			{
				// Hug surface of model: spawn around limb/torso hitbox
				Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.45f, npc.height * 0.45f);
				Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.3f, emberSpeed));

				// Low: light orange. High: brighter, white-hot yellow at core
				Color dustColor = stackRatio < 0.5f
					? Color.Lerp(SolarColor, BrightSolarColor, stackRatio * 2f)
					: Color.Lerp(BrightSolarColor, new Color(255, 248, 200), (stackRatio - 0.5f) * 2f);

				Dust dust = Dust.NewDustPerfect(position, DustID.Torch, velocity, 90, dustColor, 0.9f + stackRatio);
				dust.noGravity = true;
				dust.fadeIn = 0.4f;
			}

			// Flame tongue wisps: occasional larger, flickering particles
			if (stackRatio > 0.3f && Main.rand.NextBool(3))
			{
				Vector2 pos = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.35f, npc.height * 0.35f);
				var d = Dust.NewDustPerfect(pos, DustID.Torch, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.8f, 1.5f)), 110,
					Color.Lerp(BrightSolarColor, Color.White, stackRatio * 0.3f), 1.2f + stackRatio * 0.5f);
				d.noGravity = true;
				d.fadeIn = 0.6f;
			}

			float lightIntensity = MathHelper.Lerp(0.15f, 0.55f, stackRatio);
			Lighting.AddLight(npc.Center, lightIntensity * 0.95f, lightIntensity * 0.35f, lightIntensity * 0.05f);
		}

		private static void SpawnIgniteExplosion(Vector2 center)
		{
			SolarVFXSystem.TriggerExplosion(center, isIgnition: true);
			if (!Main.dedServ)
				Lighting.AddLight(center, 1f, 0.4f, 0f);
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

			SolarVFXSystem.TriggerExplosion(center, isIgnition: false);
			if (!Main.dedServ)
			{
				Lighting.AddLight(center, 0.9f, 0.35f, 0f);
				SoundEngine.PlaySound(SoundID.Item14, center);
			}

			ApplyExplosionDamage(center, explosionDamage, IncandescentPerk.ExplosionRadius, excludeNpcId);
			ApplyScorchInRadius(center, IncandescentPerk.ExplosionRadius, IncandescentPerk.ScorchStacksApplied, weaponDamage, excludeNpcId);
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

			float stackRatio = ScorchStacks / (float)MaxScorchStacks;

			// Low: light orange glow. High: brighter, denser, shift to white-hot—"primed" look
			float tintStrength = MathHelper.Lerp(0.18f, 0.45f, stackRatio);
			float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.15f);
			tintStrength *= pulse;

			drawColor.R = (byte)MathHelper.Lerp(drawColor.R, 255, tintStrength);
			drawColor.G = (byte)MathHelper.Lerp(drawColor.G, (byte)MathHelper.Lerp(100, 220, stackRatio), tintStrength * 0.5f);
			drawColor.B = (byte)MathHelper.Lerp(drawColor.B, (byte)MathHelper.Lerp(0, 80, stackRatio * 0.5f), tintStrength * 0.3f);
		}
	}
}
