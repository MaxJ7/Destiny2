using System;
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
	public sealed class KineticTremorsShockwave : ModProjectile
	{
		private const float ShockwaveRadius = KineticTremorsPerk.ShockwaveRadiusTiles * 16f;

		public override string Texture => "Terraria/Images/MagicPixel";

		public override void SetDefaults()
		{
			Projectile.width = 2;
			Projectile.height = 2;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.hide = true;
			Projectile.timeLeft = 600;
		}

		public override void OnSpawn(IEntitySource source)
		{
			if (Projectile.ai[1] <= 0f)
				Projectile.ai[1] = KineticTremorsPerk.PulseCount;
			if (Projectile.ai[0] <= 0f)
				Projectile.ai[0] = KineticTremorsPerk.InitialDelayTicks;
			if (Projectile.localAI[0] <= 0f)
				Projectile.localAI[0] = KineticTremorsPerk.PulseIntervalTicks;
		}

		public override void AI()
		{
			if (Projectile.ai[1] <= 0f)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.ai[0] > 0f)
			{
				Projectile.ai[0]--;
				return;
			}

			Pulse();
			Projectile.ai[1]--;
			if (Projectile.ai[1] <= 0f)
			{
				Projectile.Kill();
				return;
			}

			Projectile.ai[0] = Projectile.localAI[0];
		}

		private void Pulse()
		{
			Vector2 center = Projectile.Center;
			float radiusSq = ShockwaveRadius * ShockwaveRadius;
			int damage = Projectile.damage;

			if (Main.netMode != NetmodeID.Server)
				SpawnPulseDust(center);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.CanBeChasedBy())
					continue;

				if (Vector2.DistanceSquared(center, npc.Center) > radiusSq)
					continue;

				int direction = npc.Center.X < center.X ? -1 : 1;
				npc.SimpleStrikeNPC(damage, direction, false, 0f);
			}
		}

		private static void SpawnPulseDust(Vector2 center)
		{
			if (Main.dedServ)
				return;

			const int dustType = DustID.WhiteTorch;
			Color baseColor = new Color(255, 248, 163);
			int count = Main.rand.Next(18, 26);
			for (int i = 0; i < count; i++)
			{
				float angle = MathHelper.TwoPi * (i / (float)count);
				Vector2 direction = angle.ToRotationVector2();
				float speed = Main.rand.NextFloat(1.2f, 2.6f);
				float hueShift = Main.rand.NextFloat(-0.04f, 0.04f);
				Color shifted = ShiftHue(baseColor, hueShift);
				Vector2 spawnPos = center + direction * Main.rand.NextFloat(4f, 16f);
				Dust dust = Dust.NewDustPerfect(spawnPos, dustType, direction * speed, 40, shifted, Main.rand.NextFloat(1.0f, 1.4f));
				dust.noGravity = true;
				dust.noLight = false;
				dust.velocity *= 0.6f;
				dust.color = shifted;
			}
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
	}
}
