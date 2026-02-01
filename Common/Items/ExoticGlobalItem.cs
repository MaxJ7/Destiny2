using Destiny2.Common.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Items
{
	public sealed class ExoticGlobalItem : GlobalItem
	{
		public override void ModifyTooltips(Item item, System.Collections.Generic.List<TooltipLine> tooltips)
		{
			if (item.rare != ModContent.RarityType<ExoticRarity>())
				return;

			// Use the same pulsating glow as the rarity for consistency
			Color exoticColor = RarityLoader.GetRarity(item.rare).RarityColor;
			tooltips.Add(new TooltipLine(Mod, "ExoticQualifier", "Exotic")
			{
				OverrideColor = exoticColor
			});
		}
	}
}
