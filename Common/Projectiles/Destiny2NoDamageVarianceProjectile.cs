using Destiny2.Common.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Destiny2.Common.Projectiles
{
	public sealed class Destiny2NoDamageVarianceProjectile : GlobalProjectile
	{
		private bool isDestinyWeaponProjectile;

		public override bool InstancePerEntity => true;

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			isDestinyWeaponProjectile = IsDestinyWeaponSource(projectile, source);
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (isDestinyWeaponProjectile)
				modifiers.DamageVariationScale *= 0f;
		}

		private bool IsDestinyWeaponSource(Projectile projectile, IEntitySource source)
		{
			if (source is EntitySource_ItemUse itemUse && itemUse.Item?.ModItem is Destiny2WeaponItem)
				return true;
			if (source is EntitySource_ItemUse_WithAmmo itemUseWithAmmo && itemUseWithAmmo.Item?.ModItem is Destiny2WeaponItem)
				return true;

			if (source is EntitySource_Parent parent)
			{
				if (parent.Entity is Item parentItem && parentItem.ModItem is Destiny2WeaponItem)
					return true;

				if (parent.Entity is Projectile parentProjectile)
				{
					Destiny2NoDamageVarianceProjectile parentData = parentProjectile.GetGlobalProjectile<Destiny2NoDamageVarianceProjectile>();
					if (parentData != null && parentData.isDestinyWeaponProjectile)
						return true;
				}
			}

			if (projectile.ModProjectile?.Mod == Mod)
				return true;

			return projectile.DamageType is Destiny2ElementDamageClass;
		}
	}
}
