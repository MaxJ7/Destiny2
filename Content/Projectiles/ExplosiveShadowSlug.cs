using System;
using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
	public sealed class ExplosiveShadowSlug : ModProjectile
	{
		private const float MaxDistance = 1200f;
		private const float DustSpacing = 2.5f;
		private const float SwirlAmplitude = 6f;
		private const float SwirlCycles = 6f;
		private const int StickTime = 60 * 10;
		private const float StickyDustRadius = 6f;
		private const float Speed = 28f; 
		private const int ExtraUpdates = 3;

		private Vector2 spawnPosition;
		private bool initialized;
		private float maxDistance = MaxDistance;
		private float totalTraveledDistance;

		// localAI[0] and localAI[1] now used for previous position X/Y
		// timeLeft is used for sticky timer instead of localAI

		public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

		public override void SetDefaults()
		{
			Projectile.width = 6;
			Projectile.height = 6;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.timeLeft = 60;
			Projectile.aiStyle = -1;
			Projectile.extraUpdates = ExtraUpdates;
			Projectile.tileCollide = true;
			Projectile.hide = true;
			
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void OnSpawn(IEntitySource source)
		{
			if (source is EntitySource_ItemUse itemUse && itemUse.Item?.ModItem is Destiny2WeaponItem weaponItem)
			{
				float maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
				if (maxFalloffTiles > 0f)
					maxDistance = Math.Max(16f, maxFalloffTiles * 3f * 16f);
			}

			spawnPosition = Projectile.Center;
			totalTraveledDistance = 0f;
			
			// Initialize previous position to current (so first frame spawns no dust)
			Projectile.localAI[0] = Projectile.Center.X;
			Projectile.localAI[1] = Projectile.Center.Y;

			if (Projectile.velocity != Vector2.Zero)
				Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;

			initialized = true;
		}

		public bool IsStickingToTarget
		{
			get => Projectile.ai[0] == 1f;
			set => Projectile.ai[0] = value ? 1f : 0f;
		}

		public int TargetWhoAmI
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		public override void AI()
		{
			if (IsStickingToTarget)
			{
				StickyAI();
				return;
			}

			if (!initialized)
				return;

			// Read previous position from localAI (works correctly across extraUpdates)
			Vector2 lastPosition = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
			float segmentLength = Vector2.Distance(lastPosition, Projectile.Center);
			
			if (segmentLength > 0.1f)
			{
				SpawnEnergyDustSegment(lastPosition, Projectile.Center, totalTraveledDistance);
				totalTraveledDistance += segmentLength;
			}

			// Store current position for next frame/sub-update
			Projectile.localAI[0] = Projectile.Center.X;
			Projectile.localAI[1] = Projectile.Center.Y;

			if (totalTraveledDistance >= maxDistance)
			{
				Projectile.Kill();
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (IsStickingToTarget)
				return;

			ExplosiveShadowGlobalNPC data = target.GetGlobalNPC<ExplosiveShadowGlobalNPC>();
			if (data.IsExplosionActive || data.IsExplosionCoolingDown)
			{
				Projectile.Kill();
				return;
			}

			IsStickingToTarget = true;
			TargetWhoAmI = target.whoAmI;
			Projectile.tileCollide = false;
			Projectile.friendly = false;
			Projectile.timeLeft = StickTime; // Use timeLeft as timer
			Projectile.damage = 0;
			
			// Store offset from target center so we can stick to the exact hit location
			Projectile.velocity = Projectile.Center - target.Center;
			
			Projectile.netUpdate = true;
		}

		public override void Kill(int timeLeft)
		{
			// Only spawn end dust if we didn't stick (if we stuck, projectile keeps living)
			if (!IsStickingToTarget)
			{
				Vector2 lastPosition = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
				if (lastPosition != Projectile.Center)
				{
					SpawnEnergyDustSegment(lastPosition, Projectile.Center, totalTraveledDistance);
				}
			}
			base.OnKill(timeLeft);
		}

		private void StickyAI()
		{
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.friendly = false;

			int npcTarget = TargetWhoAmI;
			
			// Check if time expired (timeLeft counts down automatically)
			if (Projectile.timeLeft <= 0 || npcTarget < 0 || npcTarget >= Main.maxNPCs)
			{
				Projectile.Kill();
				return;
			}

			NPC target = Main.npc[npcTarget];
			if (!target.active || target.dontTakeDamage || target.friendly)
			{
				Projectile.Kill();
				return;
			}

			// Maintain relative offset to target (velocity stores the offset vector)
			Projectile.Center = target.Center + Projectile.velocity;
			Projectile.gfxOffY = target.gfxOffY;
			
			SpawnStickyDust(Projectile.Center);
		}

		private static void SpawnStickyDust(Vector2 center)
		{
			for (int i = 0; i < 3; i++)
			{
				float angle = Main.rand.NextFloat(MathHelper.TwoPi);
				float radius = Main.rand.NextFloat(1f, StickyDustRadius);
				Vector2 offset = new Vector2(radius, 0f).RotatedBy(angle);
				Vector2 swirl = offset.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY);

				Dust light = Dust.NewDustPerfect(center + offset, DustID.WhiteTorch, swirl * 0.6f, 120, default, 1.2f);
				light.noGravity = true;
				light.fadeIn = 1.1f;

				Dust dark = Dust.NewDustPerfect(center - offset * 0.3f, DustID.Wraith, -swirl * 0.5f, 200, new Color(0,177,255), 1.2f);
				dark.noGravity = true;
				dark.fadeIn = 0.9f;
			}

			if (Main.rand.NextBool(3))
				Lighting.AddLight(center, 0.08f, 0.08f, 0.08f);
		}

		private static void SpawnEnergyDustSegment(Vector2 start, Vector2 end, float distanceTraveled)
		{
			float segmentLength = Vector2.Distance(start, end);
			if (segmentLength <= 0f)
				return;

			Vector2 direction = (end - start) / segmentLength;
			Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
			
			int count = Math.Max(1, (int)(segmentLength / DustSpacing));
			float phasePerPixel = (SwirlCycles * MathHelper.TwoPi) / 240f;

			for (int i = 0; i < count; i++)
			{
				float localT = (i + 0.5f) / count;
				Vector2 pos = Vector2.Lerp(start, end, localT);
				float globalDist = distanceTraveled + (localT * segmentLength);
				float phase = globalDist * phasePerPixel;
				
				float amplitude = SwirlAmplitude * (0.6f + 0.4f * (float)Math.Sin(phase * 0.5f));
				Vector2 swirlOffset = perpendicular * (float)Math.Sin(phase) * amplitude;
				Vector2 dustPos = pos + swirlOffset;

				Dust light = Dust.NewDustPerfect(dustPos, DustID.WhiteTorch, Vector2.Zero, 100, default, 1.2f);
				light.noGravity = true;
				light.fadeIn = 1.1f;
				light.velocity = perpendicular * (float)Math.Cos(phase) * 0.4f + direction * 0.2f;

				Vector2 darkPos = dustPos - swirlOffset * 0.4f;
				Dust dark = Dust.NewDustPerfect(darkPos, DustID.Wraith, Vector2.Zero, 200, new Color(0,177,255), 1.4f);
				dark.noGravity = true;
				dark.fadeIn = 0.9f;
				dark.velocity = -perpendicular * (float)Math.Cos(phase) * 0.35f + direction * 0.15f;
				dark.noLight = false;

				if (i % 3 == 0)
					Lighting.AddLight(darkPos, 0.05f, 0.05f, 0.05f);
			}
		}
	}
}