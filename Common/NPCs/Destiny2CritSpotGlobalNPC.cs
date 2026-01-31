using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Destiny2.Common.NPCs
{
	public sealed class Destiny2CritSpotGlobalNPC : GlobalNPC
	{
		private const float MinRadius = 6f;
		private const float MaxRadius = 32f;
		private const float RadiusScalar = 0.2f;
		private const float JitterXScalar = 0.12f;
		private const float JitterYScalar = 0.08f;
		private const int HitFlashTicks = 10;
		private const int DustSpawnInterval = 2;
		private const float DrawRange = 1400f;

		private Vector2 critOffset;
		private float critRadius;
		private bool initialized;
		private int hitFlashTimer;

		public override bool InstancePerEntity => true;

		public override void OnSpawn(NPC npc, IEntitySource source)
		{
			Initialize(npc);
		}

		public override void AI(NPC npc)
		{
			if (!initialized)
				Initialize(npc);

			if (hitFlashTimer > 0)
				hitFlashTimer--;
		}

		public override void PostAI(NPC npc)
		{
			if (Main.dedServ || !IsActive(npc))
				return;

			if ((Main.GameUpdateCount % DustSpawnInterval) != 0)
				return;

			Player player = Main.LocalPlayer;
			if (player == null || !player.active)
				return;

			if (Vector2.DistanceSquared(npc.Center, player.Center) > DrawRange * DrawRange)
				return;

			SpawnCritSpotDust(npc);
		}

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
		{
			bitWriter.WriteBit(initialized);
			if (!initialized)
				return;

			writer.Write(critOffset.X);
			writer.Write(critOffset.Y);
			writer.Write(critRadius);
		}

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
		{
			initialized = bitReader.ReadBit();
			if (!initialized)
				return;

			critOffset.X = reader.ReadSingle();
			critOffset.Y = reader.ReadSingle();
			critRadius = reader.ReadSingle();
		}

		public bool IsHitInCritSpot(NPC npc, Vector2 hitPosition)
		{
			if (!IsActive(npc))
				return false;

			Vector2 center = GetWorldCenter(npc);
			center.Y += npc.gfxOffY;
			float radius = GetRadius(npc);
			return Vector2.DistanceSquared(hitPosition, center) <= radius * radius;
		}

		public void RegisterPrecisionHit()
		{
			hitFlashTimer = HitFlashTicks;
		}

		private void Initialize(NPC npc)
		{
			if (initialized)
				return;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			initialized = true;
			if (!IsValidTarget(npc))
			{
				critRadius = 0f;
				critOffset = Vector2.Zero;
				return;
			}

			float sizeScore = MathF.Sqrt(npc.width * npc.height) * npc.scale;
			critRadius = MathHelper.Clamp(sizeScore * RadiusScalar, MinRadius, MaxRadius);

			Vector2 baseOffset = GetBaseOffset(npc);
			float xJitter = Main.rand.NextFloat(-npc.width * JitterXScalar, npc.width * JitterXScalar);
			float yJitter = Main.rand.NextFloat(-npc.height * JitterYScalar, npc.height * JitterYScalar);
			critOffset = baseOffset + new Vector2(xJitter, yJitter);

			if (Main.netMode == NetmodeID.Server)
				npc.netUpdate = true;
		}

		private static Vector2 GetBaseOffset(NPC npc)
		{
			float x = npc.width * 0.1f;
			float y = -npc.height * 0.25f;

			switch (npc.aiStyle)
			{
				case 3: // Fighter
				case 26:
				case 27:
				case 31:
				case 32:
					y = -npc.height * 0.35f;
					break;
				case 2: // Slime
					y = -npc.height * 0.12f;
					break;
				case 5: // Bat
				case 14: // Flying
				case 22:
					y = -npc.height * 0.2f;
					break;
				case 6: // Worm-style segments
				case 7:
					y = 0f;
					break;
			}

			if (npc.noGravity && npc.aiStyle == 0)
				y = -npc.height * 0.2f;

			return new Vector2(x, y);
		}

		private bool IsActive(NPC npc)
		{
			return initialized && critRadius > 0f && IsValidTarget(npc);
		}

		private static bool IsValidTarget(NPC npc)
		{
			if (npc == null || !npc.active || npc.dontTakeDamage)
				return false;

			if (npc.friendly || npc.townNPC)
				return false;

			return npc.CanBeChasedBy();
		}

		private Vector2 GetWorldCenter(NPC npc)
		{
			Vector2 localOffset = critOffset * npc.scale;
			localOffset.X *= npc.spriteDirection;
			if (npc.rotation != 0f)
				localOffset = localOffset.RotatedBy(npc.rotation);

			return npc.Center + localOffset;
		}

		private float GetRadius(NPC npc)
		{
			return critRadius * npc.scale;
		}

		private void SpawnCritSpotDust(NPC npc)
		{
			Vector2 center = GetWorldCenter(npc);
			center.Y += npc.gfxOffY;
			float radius = GetRadius(npc);
			if (radius <= 0f)
				return;

			float flash = hitFlashTimer > 0 ? 1.35f : 1f;
			Color coreColor = new Color(120, 230, 255);
			Color rimColor = new Color(40, 140, 255);

			int coreCount = hitFlashTimer > 0 ? 4 : 2;
			for (int i = 0; i < coreCount; i++)
			{
				Vector2 offset = Main.rand.NextVector2Circular(radius * 0.6f, radius * 0.6f);
				float dist = offset.Length() / (radius * 0.6f);
				dist = MathHelper.Clamp(dist, 0f, 1f);
				float depth = MathF.Sqrt(1f - dist * dist);

				Color color = Color.Lerp(rimColor, coreColor, depth);
				float scale = MathHelper.Lerp(0.6f, 1.1f, depth) * flash;

				Dust dust = Dust.NewDustPerfect(center + offset, DustID.WhiteTorch, Vector2.Zero, 150, color, scale);
				dust.noGravity = true;
				dust.fadeIn = 1.1f;
				dust.velocity = Vector2.Zero;
			}

			if (Main.rand.NextBool(2))
			{
				Vector2 rim = Main.rand.NextVector2CircularEdge(radius, radius);
				Vector2 tangent = rim.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * 0.25f;
				float scale = 0.9f * flash;

				Dust dust = Dust.NewDustPerfect(center + rim, DustID.BlueTorch, tangent, 170, rimColor, scale);
				dust.noGravity = true;
				dust.fadeIn = 1.2f;
			}

			if (hitFlashTimer > 0)
				Lighting.AddLight(center, 0.12f * flash, 0.16f * flash, 0.2f * flash);
		}
	}
}
