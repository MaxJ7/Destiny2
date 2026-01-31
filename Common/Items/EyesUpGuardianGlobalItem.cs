using Destiny2.Common.Perks;
using Destiny2.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.Items
{
	public sealed class EyesUpGuardianGlobalItem : GlobalItem
	{
		public override void OnConsumeItem(Item item, Player player)
		{
			if (item == null || player == null || !item.consumable)
				return;

			if (!IsPotion(item))
				return;

			TryGrantStacks(player, "OnConsumeItem");
		}

		public override void GetHealLife(Item item, Player player, bool quickHeal, ref int healValue)
		{
			if (item != null && player != null && item.healLife > 0)
				TryGrantStacks(player, "GetHealLife");
		}

		public override void GetHealMana(Item item, Player player, bool quickHeal, ref int healValue)
		{
			if (item != null && player != null && item.healMana > 0)
				TryGrantStacks(player, "GetHealMana");
		}

		private static bool IsPotion(Item item)
		{
			if (item == null)
				return false;

			return item.potion || item.healLife > 0 || item.healMana > 0 || item.buffType > 0;
		}

		private static void TryGrantStacks(Player player, string source)
		{
			if (player == null)
				return;

			global::Destiny2.Destiny2.LogDiagnostic($"EyesUp hook triggered: {source} Player={player.whoAmI}");

			Destiny2Player modPlayer = player.GetModPlayer<Destiny2Player>();
			if (modPlayer != null)
			{
				// The duplicate check is in GrantEyesUpGuardianStacks - only one grant per frame
				modPlayer.GrantEyesUpGuardianStacks(EyesUpGuardianPerk.StacksGranted);
			}
		}
	}
}
