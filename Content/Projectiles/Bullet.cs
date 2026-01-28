using System;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
	public sealed class Bullet : ModProjectile
	{
		private const float MaxDistance = 1200f;
		private const float DustSpacing = 8f;
		private const float CollisionWidth = 6f;

		private Vector2 hitStart;
		private Vector2 hitEnd;
		private bool lineReady;
		private float maxDistance = MaxDistance;
		private Destiny2WeaponElement weaponElement = Destiny2WeaponElement.Kinetic;

		public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

		public override void SetDefaults()
		{
			Projectile.width = 4;
			Projectile.height = 4;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.DamageType = Destiny2WeaponElement.Kinetic.GetDamageClass();
			Projectile.timeLeft = 2;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = false;
			Projectile.hide = true;
		}

		public override void OnSpawn(IEntitySource source)
		{
			weaponElement = Destiny2WeaponElement.Kinetic;
			Projectile.ai[0] = (int)weaponElement;

			if (source is EntitySource_ItemUse itemUse && itemUse.Item?.ModItem is Destiny2WeaponItem weaponItem)
			{
				float maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
				if (maxFalloffTiles > 0f)
					maxDistance = Math.Max(16f, maxFalloffTiles * 3f * 16f);

				weaponElement = weaponItem.WeaponElement;
				Projectile.ai[0] = (int)weaponElement;
				Projectile.DamageType = weaponElement.GetDamageClass();
				Projectile.netUpdate = true;
			}
		}

		public override void AI()
		{
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

			SpawnDust(start, end, GetWeaponElement());

			Projectile.velocity = Vector2.Zero;
			Projectile.timeLeft = 1;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
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

		private static void SpawnDust(Vector2 start, Vector2 end, Destiny2WeaponElement element)
		{
			GetDustStyle(element, out int dustType, out Color dustColor);
			float length = Vector2.Distance(start, end);
			int count = Math.Max(2, (int)(length / DustSpacing));
			for (int i = 0; i < count; i++)
			{
				float t = i / (float)(count - 1);
				Vector2 pos = Vector2.Lerp(start, end, t);
				Dust dust = Dust.NewDustDirect(pos - new Vector2(2f), 4, 4, dustType, 0f, 0f, 80, dustColor, 1.2f);
				dust.noGravity = true;
				dust.velocity *= 0.2f;
			}
		}

		private static void GetDustStyle(Destiny2WeaponElement element, out int dustType, out Color dustColor)
		{
			switch (element)
			{
				case Destiny2WeaponElement.Stasis:
					dustType = DustID.BlueTorch;
					dustColor = new Color(30, 60, 160);
					break;
				case Destiny2WeaponElement.Strand:
					dustType = DustID.GreenTorch;
					dustColor = new Color(60, 240, 90);
					break;
				case Destiny2WeaponElement.Solar:
					dustType = DustID.OrangeTorch;
					dustColor = new Color(255, 120, 40);
					break;
				case Destiny2WeaponElement.Arc:
					dustType = DustID.Electric;
					dustColor = new Color(120, 200, 255);
					break;
				case Destiny2WeaponElement.Void:
					dustType = DustID.PurpleTorch;
					dustColor = new Color(170, 80, 220);
					break;
				default:
					dustType = DustID.FireworkFountain_Yellow;
					dustColor = new Color(255, 150, 70);
					break;
			}
		}

		private Destiny2WeaponElement GetWeaponElement()
		{
			int elementId = (int)Projectile.ai[0];
			if (elementId < 0 || elementId > (int)Destiny2WeaponElement.Void)
				return weaponElement;

			return (Destiny2WeaponElement)elementId;
		}
	}
}
