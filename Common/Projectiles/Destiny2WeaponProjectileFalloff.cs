using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Destiny2.Common.Projectiles
{
	public sealed class Destiny2WeaponProjectileFalloff : GlobalProjectile
	{
		private const float MinDamageMultiplier = 0.5f;

		private float falloffStartTiles;
		private float maxFalloffTiles;
		private Vector2 spawnPosition;
		private bool hasFalloff;

		public override bool InstancePerEntity => true;

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			hasFalloff = false;
			falloffStartTiles = 0f;
			maxFalloffTiles = 0f;
			spawnPosition = Vector2.Zero;

			Item sourceItem = GetSourceItem(source);
			if (sourceItem?.ModItem is Destiny2WeaponItem weaponItem)
			{
				falloffStartTiles = weaponItem.GetFalloffTiles();
				maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
				if (falloffStartTiles > 0f && maxFalloffTiles >= falloffStartTiles)
				{
					spawnPosition = projectile.Center;
					hasFalloff = true;
				}
			}
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (!hasFalloff)
				return;

			float multiplier = GetDamageMultiplier(target.Center);
			if (multiplier < 1f)
				modifiers.FinalDamage *= multiplier;
		}

		public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
		{
			if (!hasFalloff)
				return;

			float multiplier = GetDamageMultiplier(target.Center);
			if (multiplier < 1f)
				modifiers.FinalDamage *= multiplier;
		}

		private float GetDamageMultiplier(Vector2 targetCenter)
		{
			float distanceTiles = Vector2.Distance(spawnPosition, targetCenter) / 16f;
			if (distanceTiles <= falloffStartTiles)
				return 1f;

			float endDistance = maxFalloffTiles + (maxFalloffTiles * 0.5f);
			if (distanceTiles >= endDistance)
				return MinDamageMultiplier;

			float t = (distanceTiles - falloffStartTiles) / (endDistance - falloffStartTiles);
			return MathHelper.Lerp(1f, MinDamageMultiplier, MathHelper.Clamp(t, 0f, 1f));
		}

		private static Item GetSourceItem(IEntitySource source)
		{
			if (source is EntitySource_ItemUse itemUse)
				return itemUse.Item;
			if (source is EntitySource_ItemUse_WithAmmo itemUseWithAmmo)
				return itemUseWithAmmo.Item;

			return null;
		}
	}
}
