using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
	public sealed class ExplosiveShadowGlobalItem : GlobalItem
	{
		public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			if (item.ModItem is not Destiny2WeaponItem weaponItem)
				return;

			foreach (Destiny2Perk perk in weaponItem.GetPerks())
			{
				if (perk is ExplosiveShadowPerk)
				{
					type = ModContent.ProjectileType<ExplosiveShadowSlug>();
					return;
				}
			}
		}
	}
}
