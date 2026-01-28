using Destiny2.Common.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Items
{
	public abstract class Destiny2CatalystItem : ModItem
	{
		public abstract string CatalystPerkKey { get; }

		public virtual bool CanApplyTo(Item item)
		{
			if (item?.ModItem is not Destiny2WeaponItem weaponItem)
				return false;

			if (!weaponItem.HasCatalystSlot || weaponItem.HasCatalyst)
				return false;

			return true;
		}
	}
}
