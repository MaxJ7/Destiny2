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
		private const float DustSpacing = 4f;
		private const float Speed = 32f; 
		private const int ExtraUpdates = 3;

		private Vector2 spawnPosition;
		private bool initialized;
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
			Projectile.timeLeft = 60;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = true;
			Projectile.hide = true;
			Projectile.extraUpdates = ExtraUpdates;
			
			// CRITICAL: Ensures rapid fire doesn't cause immunity frame sharing
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1; // -1 allows hit, then projectile dies (penetrate=1)
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
			}

			spawnPosition = Projectile.Center;
			
			// Initialize "last position" to current so first frame spawns no dust
			Projectile.localAI[0] = Projectile.Center.X;
			Projectile.localAI[1] = Projectile.Center.Y;

			if (Projectile.velocity != Vector2.Zero)
				Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;

			initialized = true;
			Projectile.netUpdate = true;
		}

		public override void AI()
		{
			if (!initialized)
				return;

			// Read last position from localAI (persisted across extraUpdates)
			Vector2 lastPosition = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
			
			// Spawn dust for the segment traveled this frame
			SpawnDustSegment(lastPosition, Projectile.Center, GetWeaponElement());
			
			// Update last position for next frame
			Projectile.localAI[0] = Projectile.Center.X;
			Projectile.localAI[1] = Projectile.Center.Y;

			// Kill if exceeded max range
			if (Vector2.Distance(spawnPosition, Projectile.Center) >= maxDistance)
			{
				Projectile.Kill();
			}
		}

		public override void Kill(int timeLeft)
		{
			// Ensures dust is spawned for the final segment if the projectile 
			// collided and died mid-frame (during extraUpdates)
			Vector2 lastPosition = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
			if (lastPosition != Projectile.Center)
			{
				SpawnDustSegment(lastPosition, Projectile.Center, GetWeaponElement());
			}
			base.OnKill(timeLeft);
		}

		private static void SpawnDustSegment(Vector2 start, Vector2 end, Destiny2WeaponElement element)
		{
			if (Main.dedServ)
				return;

			GetDustStyle(element, out int dustType, out Color dustColor);
			Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
			Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
			float length = Vector2.Distance(start, end);
			
			// If no movement (e.g., first frame or stuck), don't spawn
			if (length < 0.1f) 
				return;
				
			int count = Math.Max(1, (int)(length / DustSpacing));

			for (int i = 0; i < count; i++)
			{
				float t = (i + 0.5f) / count;
				Vector2 pos = Vector2.Lerp(start, end, t);
				int dustCount = Main.rand.Next(2, 4);
				
				for (int j = 0; j < dustCount; j++)
				{
					float hueShift = (j - 1) * 0.03f;
					Color shifted = ShiftHue(dustColor, hueShift);
					Vector2 offset = perpendicular * Main.rand.NextFloat(-2f, 2f) + direction * Main.rand.NextFloat(-1f, 1f);
					float scale = Main.rand.NextFloat(1.0f, 1.45f);
					
					Dust dust = Dust.NewDustDirect(pos + offset - new Vector2(2f), 4, 4, dustType, 0f, 0f, 40, shifted, scale);
					dust.noGravity = true;
					dust.noLight = false;
					dust.velocity *= 0.3f;
					dust.color = shifted;
				}
			}
		}

		private static void GetDustStyle(Destiny2WeaponElement element, out int dustType, out Color dustColor)
		{
			dustType = DustID.WhiteTorch;
			dustColor = GetElementColor(element);
		}

		private static Color GetElementColor(Destiny2WeaponElement element)
		{
			return element switch
			{
				Destiny2WeaponElement.Void => new Color(196, 0, 240),
				Destiny2WeaponElement.Strand => new Color(55, 218, 100),
				Destiny2WeaponElement.Stasis => new Color(51, 91, 196),
				Destiny2WeaponElement.Solar => new Color(236, 85, 0),
				Destiny2WeaponElement.Arc => new Color(7, 208, 255),
				Destiny2WeaponElement.Kinetic => new Color(255, 248, 163),
				_ => new Color(255, 248, 163)
			};
		}

		private static Color ShiftHue(Color color, float shift)
		{
			Vector3 rgb = color.ToVector3();
			float max = Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z));
			float min = Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
			float delta = max - min;

			float hue = 0f;
			if (delta > 0.0001f)
			{
				if (max == rgb.X)
					hue = (rgb.Y - rgb.Z) / delta;
				else if (max == rgb.Y)
					hue = 2f + (rgb.Z - rgb.X) / delta;
				else
					hue = 4f + (rgb.X - rgb.Y) / delta;
				hue /= 6f;
			}

			if (hue < 0f)
				hue += 1f;

			float saturation = max <= 0f ? 0f : delta / max;
			float value = max;
			float shiftedHue = hue + shift;
			if (shiftedHue < 0f)
				shiftedHue += 1f;
			else if (shiftedHue >= 1f)
				shiftedHue -= 1f;

			return ColorFromHsv(shiftedHue, saturation, value);
		}

		private static Color ColorFromHsv(float hue, float saturation, float value)
		{
			float c = value * saturation;
			float x = c * (1f - Math.Abs((hue * 6f) % 2f - 1f));
			float m = value - c;

			float r;
			float g;
			float b;
			float h = hue * 6f;

			if (h < 1f)
			{
				r = c;
				g = x;
				b = 0f;
			}
			else if (h < 2f)
			{
				r = x;
				g = c;
				b = 0f;
			}
			else if (h < 3f)
			{
				r = 0f;
				g = c;
				b = x;
			}
			else if (h < 4f)
			{
				r = 0f;
				g = x;
				b = c;
			}
			else if (h < 5f)
			{
				r = x;
				g = 0f;
				b = c;
			}
			else
			{
				r = c;
				g = 0f;
				b = x;
			}

			return new Color(r + m, g + m, b + m);
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