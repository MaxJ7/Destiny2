using Destiny2.Common.Items;
using Destiny2.Common.Perks;
using Destiny2.Content.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Items.Catalysts
{
	public sealed class MalfeasanceCatalyst : Destiny2CatalystItem
	{

		public override string CatalystPerkKey => nameof(VorpalWeaponPerk);

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 5);
		}

		public override bool CanApplyTo(Item item)
		{
			if (!base.CanApplyTo(item))
				return false;

			return item.type == ModContent.ItemType<Malfeasance>();
		}
	}
}
