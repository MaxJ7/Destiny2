using Destiny2.Common.Perks;
using Destiny2.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Items
{
	public sealed class EyesUpGuardianGlobalItem : GlobalItem
	{
		public override void OnConsumeItem(Item item, Player player)
		{
			if (item == null || player == null || !item.consumable)
				return;

			bool isPotion = item.potion || item.healLife > 0 || item.healMana > 0 || item.buffType > 0;
			if (!isPotion)
				return;

			Destiny2Player modPlayer = player.GetModPlayer<Destiny2Player>();
			modPlayer.GrantEyesUpGuardianStacks(EyesUpGuardianPerk.StacksGranted);
		}
	}
}
