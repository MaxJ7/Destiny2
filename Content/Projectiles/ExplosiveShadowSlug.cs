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
		private const float DustSpacing = 6f;
		private const float SwirlAmplitude = 6f;
		private const float SwirlCycles = 6f;
		private const int StickTime = 60 * 10;
		private const float StickyDustRadius = 6f;
		private const float CollisionWidth = 8f;

		private Vector2 hitStart;
		private Vector2 hitEnd;
		private bool lineReady;
		private float maxDistance = MaxDistance;

		public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

		public override void SetDefaults()
		{
			Projectile.width = 6;
			Projectile.height = 6;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.timeLeft = 2;
			Projectile.aiStyle = -1;
			Projectile.extraUpdates = 0;
			Projectile.tileCollide = false;
			Projectile.hide = true;
		}

		public override void OnSpawn(IEntitySource source)
		{
			if (source is EntitySource_ItemUse itemUse && itemUse.Item?.ModItem is Destiny2WeaponItem weaponItem)
			{
				float maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
				if (maxFalloffTiles > 0f)
					maxDistance = Math.Max(16f, maxFalloffTiles * 3f * 16f);
			}
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

		private float StickTimer
		{
			get => Projectile.localAI[1];
			set => Projectile.localAI[1] = value;
		}

		public override void AI()
		{
			if (IsStickingToTarget)
			{
				StickyAI();
				return;
			}

			if (Projectile.localAI[0] != 0f)
				return;

			Projectile.localAI[0] = 1f;
			Vector2 start = Projectile.Center;
			Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			float distance = GetTileCollisionDistance(start, direction, maxDistance);
			Vector2 end = start + direction * distance;
			end = TruncateToNpcHit(start, end);

			hitStart = start;
			hitEnd = end;
			lineReady = true;

			SpawnEnergyDust(start, end);

			Projectile.velocity = Vector2.Zero;
			Projectile.timeLeft = 1;
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
			Projectile.timeLeft = StickTime;
			Projectile.damage = 0;
			Projectile.netUpdate = true;

			if (lineReady)
				Projectile.Center = hitEnd;

			Projectile.velocity = target.Center - Projectile.Center;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (IsStickingToTarget)
				return false;

			if (!lineReady)
				return false;

			float collisionPoint = 0f;
			return Collision.CheckAABBvLineCollision(
				targetHitbox.TopLeft(),
				targetHitbox.Size(),
				hitStart,
				hitEnd,
				CollisionWidth,
				ref collisionPoint);
		}

		private void StickyAI()
		{
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.friendly = false;
			StickTimer += 1f;

			int npcTarget = TargetWhoAmI;
			if (StickTimer >= StickTime || npcTarget < 0 || npcTarget >= Main.maxNPCs)
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

			Projectile.Center = target.Center - Projectile.velocity;
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

		private static float GetTileCollisionDistance(Vector2 start, Vector2 direction, float maxDistance)
		{
			float[] samples = new float[3];
			Vector2 end = start + direction * maxDistance;
			Collision.LaserScan(start, end, 1f, samples.Length, samples);

			float distance = maxDistance;
			for (int i = 0; i < samples.Length; i++)
			{
				if (samples[i] < distance)
					distance = samples[i];
			}

			return distance;
		}

		private static Vector2 TruncateToNpcHit(Vector2 start, Vector2 end)
		{
			float closest = Vector2.Distance(start, end);
			bool found = false;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.active || npc.friendly || npc.dontTakeDamage)
					continue;

				float collisionPoint = 0f;
				if (Collision.CheckAABBvLineCollision(npc.Hitbox.TopLeft(), npc.Hitbox.Size(), start, end, 2f, ref collisionPoint))
				{
					if (collisionPoint < closest)
					{
						closest = collisionPoint;
						found = true;
					}
				}
			}

			if (!found)
				return end;

			Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
			return start + direction * closest;
		}

		private static void SpawnEnergyDust(Vector2 start, Vector2 end)
		{
			float length = Vector2.Distance(start, end);
			if (length <= 1f)
				return;

			Vector2 direction = (end - start) / length;
			Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
			int count = Math.Max(2, (int)(length / DustSpacing));
			float cycles = Math.Max(1f, SwirlCycles * (length / 240f));

			for (int i = 0; i < count; i++)
			{
				float t = i / (float)(count - 1);
				float phase = t * MathHelper.TwoPi * cycles;
				float amplitude = SwirlAmplitude * (0.6f + 0.4f * (float)Math.Sin(phase * 0.5f));
				Vector2 swirlOffset = perpendicular * (float)Math.Sin(phase) * amplitude;
				Vector2 pos = Vector2.Lerp(start, end, t) + swirlOffset;

				Dust light = Dust.NewDustPerfect(pos, DustID.WhiteTorch, Vector2.Zero, 100, default, 1.2f);
				light.noGravity = true;
				light.fadeIn = 1.1f;
				light.velocity = perpendicular * (float)Math.Cos(phase) * 0.4f + direction * 0.2f;

				Vector2 darkPos = pos - swirlOffset * 0.4f;
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
