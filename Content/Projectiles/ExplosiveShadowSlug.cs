// ExplosiveShadowSlug.cs - Updated Sticky Visuals
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
		// Constants remain the same...
		private const float MaxDistance = 1200f;
		private const float DustSpacing = 2.5f;
		private const float SwirlAmplitude = 6f;
		private const float SwirlCycles = 6f;
		private const int StickTime = 60 * 10;
		
		// New constant for the sticky flare appearance
		private const float FlareLength = 24f;        // How long the stuck tail is (pixels)
		private const float FlareThickness = 3f;      // How "fat" the flare rod is
		private const int FlareDustCount = 6;         // Number of dust pairs along the line
		
		private const float Speed = 28f; 
		private const int ExtraUpdates = 3;

		private Vector2 spawnPosition;
		private bool initialized;
		private float maxDistance = MaxDistance;
		private float totalTraveledDistance;

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
			Item sourceItem = null;
			if (source is EntitySource_ItemUse itemUse)
				sourceItem = itemUse.Item;
			else if (source is EntitySource_ItemUse_WithAmmo itemUseWithAmmo)
				sourceItem = itemUseWithAmmo.Item;

			if (sourceItem?.ModItem is Destiny2WeaponItem weaponItem)
			{
				float maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
				if (maxFalloffTiles > 0f)
					maxDistance = Math.Max(16f, maxFalloffTiles * 3f * 16f);
			}

			spawnPosition = Projectile.Center;
			totalTraveledDistance = 0f;
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

			Vector2 lastPosition = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
			
			if (CheckNPCCollision(lastPosition, Projectile.Center, out NPC hitNPC, out float collisionDist))
			{
				Vector2 direction = (Projectile.Center - lastPosition).SafeNormalize(Vector2.UnitX);
				Vector2 hitPoint = lastPosition + direction * collisionDist;
				Projectile.Center = hitPoint;
				
				if (TryStickToNPC(hitNPC))
				{
					SpawnEnergyDustSegment(lastPosition, hitPoint, totalTraveledDistance);
					return;
				}
			}
			
			float segmentLength = Vector2.Distance(lastPosition, Projectile.Center);
			if (segmentLength > 0.1f)
			{
				SpawnEnergyDustSegment(lastPosition, Projectile.Center, totalTraveledDistance);
				totalTraveledDistance += segmentLength;
			}

			Projectile.localAI[0] = Projectile.Center.X;
			Projectile.localAI[1] = Projectile.Center.Y;

			if (totalTraveledDistance >= maxDistance)
			{
				Projectile.Kill();
			}
		}

		private bool TryStickToNPC(NPC target)
		{
			ExplosiveShadowGlobalNPC data = target.GetGlobalNPC<ExplosiveShadowGlobalNPC>();
			if (data.IsExplosionActive || data.IsExplosionCoolingDown)
			{
				Projectile.Kill();
				return false;
			}

			IsStickingToTarget = true;
			TargetWhoAmI = target.whoAmI;
			Projectile.tileCollide = false;
			Projectile.friendly = false;
			Projectile.timeLeft = StickTime;
			Projectile.damage = 0;
			
			// Store the offset from target center to impact point.
			// This vector points outward from the enemy's center toward the surface where we hit.
			// Since we hit the surface from the outside, this points BACK toward where we came from.
			Projectile.velocity = Projectile.Center - target.Center;
			
			Projectile.netUpdate = true;
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (IsStickingToTarget) return;
			TryStickToNPC(target);
		}

		[Obsolete]
		public override void Kill(int timeLeft)
		{
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

			// Update position to stick to the moving enemy
			Projectile.Center = target.Center + Projectile.velocity;
			Projectile.gfxOffY = target.gfxOffY;
			
			// -------------------------------------------------------------------------
			// FLARE VISUALS SETUP
			// -------------------------------------------------------------------------
			// The stored Projectile.velocity is actually the offset vector (center -> surface).
			// Normalizing it gives us the direction pointing OUT of the enemy toward the shooter.
			Vector2 flareDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			
			// Spawn the line of dust forming the "flare rod"
			SpawnStickyFlare(Projectile.Center, flareDirection);
		}

		// -------------------------------------------------------------------------
		// NEW METHOD: The Stuck Flare Appearance
		// -------------------------------------------------------------------------
		/// <summary>
		/// Spawns a line of dust particles resembling a vanilla flare bolt stuck in the target.
		/// The line extends from the hitPoint backward toward the shooter along the direction vector.
		/// </summary>
		/// <param name="hitPoint">Where the slug impacted the enemy (start of the line)</param>
		/// <param name="direction">Unit vector pointing back toward the shooter (along the line)</param>
		private void SpawnStickyFlare(Vector2 hitPoint, Vector2 direction)
		{
			// Create a perpendicular vector for thickness (rotated 90 degrees / Pi/2 radians)
			Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
			
			// Only spawn every 2nd frame to prevent dust spam, but keep it visually dense
			// "Projectile.timeLeft % 2" alternates between 0 and 1 each tick
			if (Projectile.timeLeft % 2 != 0) return;
			
			// Loop along the length of the flare to place dust particles
			for (int i = 0; i < FlareDustCount; i++)
			{
				// "t" goes from 0.0 (at surface) to 1.0 (at tip of flare)
				// We calculate this to evenly space dust along the line
				float t = i / (float)(FlareDustCount - 1);
				
				// Position along the center line of the flare rod
				// hitPoint + (direction * distance)
				Vector2 linePos = hitPoint + direction * (t * FlareLength);
				
				// Add randomness perpendicular to the line to give the rod "volume" (thickness)
				// This makes it look like a cylinder rather than a laser-thin line
				float randomOffset = Main.rand.NextFloat(-FlareThickness, FlareThickness);
				Vector2 finalPos = linePos + perpendicular * randomOffset;
				
				// Add slight upward drift to simulate heat/fire rising
				Vector2 driftVelocity = new Vector2(
					Main.rand.NextFloat(-0.1f, 0.1f), // Tiny horizontal jitter
					Main.rand.NextFloat(-0.3f, -0.05f) // Slow upward drift (negative Y is up)
				);
				
				// Scale fades slightly toward the tip (t=1) to make it look like it's burning away
				float scaleMult = 1f - (t * 0.25f); // 100% size at base, 75% at tip
				
				// -----------------------------------------------------------------
				// LIGHT DUST: The "Core" of the flare (white-hot center)
				// -----------------------------------------------------------------
				Dust light = Dust.NewDustPerfect(
					finalPos,
					DustID.WhiteTorch,      // Bright white glowing dust
					driftVelocity,          // Slight upward movement
					100,                    // Transparency (lower = more transparent)
					Color.White,            // Override to pure white for intensity
					1.3f * scaleMult        // Scale with taper
				);
				light.noGravity = true;     // Floats in air (burning)
				light.fadeIn = 1.1f;        // Briefly gets brighter when spawned
				light.shader = null;        // No special shader effects needed
				
				// -----------------------------------------------------------------
				// DARK DUST: The "Aura" (cyan energy wrapping the core)
				// Spawned slightly behind (-direction) and offset for volume
				// -----------------------------------------------------------------
				Vector2 darkPos = finalPos - direction * 2f + perpendicular * Main.rand.NextFloat(-1.5f, 1.5f);
				
				Dust dark = Dust.NewDustPerfect(
					darkPos,
					DustID.Wraith,          // Darker, shadowy dust type
					driftVelocity * 0.8f,   // Slightly slower drift than core
					180,                    // More transparent than core
					new Color(0, 177, 255), // Destiny 2 "Void/Arc" cyan-blue tint
					1.1f * scaleMult        // Slightly smaller than core
				);
				dark.noGravity = true;
				dark.fadeIn = 0.9f;
				dark.noLight = false;       // This dust emits a tiny bit of light
			}
			
			// Add a pulsing glow at the impact point where the flare enters the flesh/armor
			// The intensity flickers using Sine wave based on time
			float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.2f) * 0.03f + 0.08f;
			Lighting.AddLight(hitPoint, pulse, pulse, pulse * 1.2f); // Slightly blue-tinted light
		}

		private bool CheckNPCCollision(Vector2 start, Vector2 end, out NPC hitNPC, out float closestDist)
		{
			hitNPC = null;
			closestDist = float.MaxValue;
			bool found = false;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.active || npc.dontTakeDamage)
					continue;
				
				if (npc.friendly && npc.type != NPCID.TargetDummy)
					continue;

				float collisionPoint = 0f;
				if (Collision.CheckAABBvLineCollision(
					npc.Hitbox.TopLeft(), 
					npc.Hitbox.Size(), 
					start, 
					end, 
					Projectile.width, 
					ref collisionPoint))
				{
					if (collisionPoint < closestDist)
					{
						closestDist = collisionPoint;
						hitNPC = npc;
						found = true;
					}
				}
			}
			return found;
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
